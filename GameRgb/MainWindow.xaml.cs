using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
namespace GameRgb;
public partial class MainWindow:Window
{
 const int DwmUseImmersiveDarkMode=20,DwmWindowCornerPreference=33,DwmSystemBackdropType=38;const int DwmRound=2,DwmMica=2;
 [DllImport("dwmapi.dll")]static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
 readonly AudioCaptureService audio=new();readonly OpenRgbService rgb=new();readonly RgbConflictManager conflicts=new();readonly OpenRgbHostManager openRgbHost=new();readonly HardwarePrerequisiteService prerequisites=new();readonly LightingEngine engine;
 readonly AppSettingsStore settingsStore=new();readonly DispatcherTimer settingsSaveTimer=new(){Interval=TimeSpan.FromMilliseconds(350)};
 readonly CancellationTokenSource lifetimeCancellation=new();LightingColor primaryColor=new(59,130,246),secondaryColor=new(168,85,247);float liveGain=2f,liveSmoothing=.8f;CancellationTokenSource? cancellation;Task? running;bool closing,settingsReady,hasSavedWindowBounds,restoreMaximized;readonly SemaphoreSlim stopGate=new(1,1);
 public ObservableCollection<DeviceItem> Devices{get;}=[];public ObservableCollection<ProcessItem> Processes{get;}=[];
 public MainWindow()
 {
  InitializeComponent();engine=new(audio,rgb);DataContext=this;settingsSaveTimer.Tick+=SettingsSaveTimer_Tick;
  RefreshProcesses();RestoreSettings(settingsStore.Load());settingsReady=true;ApplyColorStyleVisibility();UpdateColorPreview();
  SourceInitialized+=Window_SourceInitialized;LocationChanged+=WindowBounds_Changed;SizeChanged+=WindowBounds_Changed;StateChanged+=WindowBounds_Changed;Loaded+=Window_Loaded;
 }
 void Window_SourceInitialized(object? sender,EventArgs e)
 {
  var handle=new WindowInteropHelper(this).Handle;
  try
  {
   int dark=1;DwmSetWindowAttribute(handle,DwmUseImmersiveDarkMode,ref dark,sizeof(int));
   if(!OperatingSystem.IsWindowsVersionAtLeast(10,0,22000))return;
   int preference=DwmRound;DwmSetWindowAttribute(handle,DwmWindowCornerPreference,ref preference,sizeof(int));
   int backdrop=DwmMica;DwmSetWindowAttribute(handle,DwmSystemBackdropType,ref backdrop,sizeof(int));
  }
  catch{}
 }
 async void Window_Loaded(object sender,RoutedEventArgs e)
 {
  var workArea=System.Windows.SystemParameters.WorkArea;
  MinWidth=Math.Min(MinWidth,workArea.Width);MinHeight=Math.Min(MinHeight,workArea.Height);
  MaxWidth=workArea.Width;MaxHeight=workArea.Height;
  if(!hasSavedWindowBounds)
  {
   Width=Math.Min(1400,Math.Max(MinWidth,workArea.Width*.78));
   Height=Math.Min(950,Math.Max(MinHeight,workArea.Height*.84));
  }
  if(restoreMaximized)WindowState=WindowState.Maximized;
  ConnectButton.IsEnabled=false;Status("正在準備硬體…","#FBBF24");
  try
  {
   var prerequisiteMessage=await prerequisites.EnsurePawnIoAsync(this);if(closing)return;
   Status("正在釋放 RGB 控制權…","#FBBF24");
   var errors=await Task.Run(()=>conflicts.SuspendAsync());
   if(errors.Count>0)throw new InvalidOperationException("無法暫停衝突的 RGB 軟體："+string.Join("；",errors));
   if(!int.TryParse(PortBox.Text,out int port)||port is<1 or>65535)throw new InvalidOperationException("連接埠必須介於 1 到 65535。");
   var host=HostBox.Text.Trim();Status("正在啟動 OpenRGB…","#FBBF24");
   var hostMessage=await openRgbHost.EnsureServerAsync(host,port,lifetimeCancellation.Token);if(closing)return;
   var found=ConnectCore(host,port);Status($"已連線 · {found.Count} 個裝置","#22C55E");
   MessageText.Text=$"{prerequisiteMessage} {hostMessage} 已自動掃描 {found.Count} 個 RGB 裝置。";
  }
  catch(OperationCanceledException)when(closing){}
  catch(Exception ex){Status("啟動失敗","#EF4444");MessageText.Text="無法準備 RGB 硬體："+ex.Message;}
  finally{ConnectButton.IsEnabled=true;}
 }
 async void Connect_Click(object sender,RoutedEventArgs e)
 {
  try
  {
   if(!int.TryParse(PortBox.Text,out int port)||port is<1 or>65535)throw new InvalidOperationException("連接埠必須介於 1 到 65535。");
   ConnectButton.IsEnabled=false;Status("正在啟動 OpenRGB…","#FBBF24");
   await openRgbHost.EnsureServerAsync(HostBox.Text.Trim(),port,lifetimeCancellation.Token);
   var found=ConnectCore(HostBox.Text.Trim(),port);
   StartButton.IsEnabled=TestButton.IsEnabled=Devices.Count>0;Status($"已連線 · {Devices.Count} 個裝置","#22C55E");
   var conflict=RgbConflictWarning();
   MessageText.Text=conflict??(Devices.Count>0?"可先執行燈效測試，再選擇 Spotify 或其他音訊來源。":"OpenRGB 已連線，但沒有偵測到 RGB 裝置。");
  }catch(Exception ex){StartButton.IsEnabled=TestButton.IsEnabled=false;Status("連線失敗","#EF4444");MessageText.Text="無法連到 OpenRGB："+ex.Message;}
  finally{ConnectButton.IsEnabled=true;}
 }
 IReadOnlyList<DeviceItem> ConnectCore(string host,int port)
 {
  var found=rgb.Connect(host,port);Devices.Clear();foreach(var d in found)Devices.Add(d);
  StartButton.IsEnabled=TestButton.IsEnabled=Devices.Count>0;return found;
 }
 async void Start_Click(object sender,RoutedEventArgs e)
 {
  var ids=SelectedIds();if(ids is null)return;
  if(ProcessCombo.SelectedItem is not ProcessItem process)return;
  try
  {
   Controls(true);var sessionCancellation=new CancellationTokenSource();cancellation=sessionCancellation;var token=sessionCancellation.Token;
   Status("正在接管 RGB…","#FBBF24");var conflictErrors=await Task.Run(()=>conflicts.SuspendAsync());token.ThrowIfCancellationRequested();
   if(conflictErrors.Count>0)throw new InvalidOperationException(string.Join("；",conflictErrors));
   Status("啟動中…","#FBBF24");await Task.Run(()=>rgb.Prepare(ids));token.ThrowIfCancellationRequested();
   await Task.Run(async()=>await audio.StartAsync(process.Id));token.ThrowIfCancellationRequested();
   running=engine.RunAsync(ids,SelectedFps(),()=>Volatile.Read(ref liveGain),()=>Volatile.Read(ref liveSmoothing),SelectedMode(),SelectedPaint(),SelectedDirection(),UpdateMeters,token);
   Status("燈效運作中","#22C55E");MessageText.Text=process.Id is null?"正在分析整台電腦輸出。":$"正在分析 {process.Name} 的音訊。";
   _=WatchAsync(running);
  }catch(Exception ex){await StopAsync(true);Status("啟動失敗","#EF4444");MessageText.Text="無法啟動燈效："+ex.Message;}
 }
 async void Test_Click(object sender,RoutedEventArgs e)
 {
  var ids=SelectedIds();if(ids is null)return;
  try
  {
   Controls(true);var sessionCancellation=new CancellationTokenSource();cancellation=sessionCancellation;var token=sessionCancellation.Token;
   Status("正在接管 RGB…","#FBBF24");var conflictErrors=await Task.Run(()=>conflicts.SuspendAsync());token.ThrowIfCancellationRequested();
   if(conflictErrors.Count>0)throw new InvalidOperationException(string.Join("；",conflictErrors));
   Status("測試模式","#A78BFA");await Task.Run(()=>rgb.Prepare(ids));token.ThrowIfCancellationRequested();
   running=engine.RunTestAsync(ids,SelectedFps(),SelectedMode(),SelectedPaint(),SelectedDirection(),UpdateMeters,token);
   MessageText.Text="測試循環：低頻紅 → 中頻綠 → 高頻藍 → 全頻彩色。";
   _=WatchAsync(running);
  }catch(Exception ex){await StopAsync(true);Status("測試失敗","#EF4444");MessageText.Text="無法啟動測試："+ex.Message;}
 }
 int[]? SelectedIds()
 {
  var ids=Devices.Where(x=>x.IsSelected).Select(x=>x.Id).ToArray();
  if(ids.Length>0)return ids;MessageText.Text="請至少勾選一個 RGB 裝置。";return null;
 }
 int SelectedFps()=>int.Parse(((ComboBoxItem)FpsCombo.SelectedItem).Tag.ToString()!);
 LightingMode SelectedMode()=>Enum.Parse<LightingMode>(((ComboBoxItem)ModeCombo.SelectedItem).Tag.ToString()!);
 LightingPaint SelectedPaint()=>new(primaryColor,secondaryColor,GradientColorStyle.IsChecked==true);
 LightingDirection SelectedDirection()=>RightToLeftDirection.IsChecked==true?LightingDirection.RightToLeft:LightingDirection.LeftToRight;
 void ColorSwatch_Click(object sender,RoutedEventArgs e)
 {
  bool secondary=sender is System.Windows.Controls.Button button&&Equals(button.Tag,"Secondary");
  var selected=secondary?secondaryColor:primaryColor;
  using var dialog=new System.Windows.Forms.ColorDialog
  {
   FullOpen=true,AnyColor=true,Color=System.Drawing.Color.FromArgb(selected.Red,selected.Green,selected.Blue)
  };
  if(dialog.ShowDialog()!=System.Windows.Forms.DialogResult.OK)return;
  var color=dialog.Color;SetSelectedColor(color.R,color.G,color.B,secondary);
 }
 void SetSelectedColor(byte red,byte green,byte blue,bool secondary)
 {
  if(secondary)secondaryColor=new(red,green,blue);else primaryColor=new(red,green,blue);
  UpdateColorPreview();ScheduleSettingsSave();
 }
 void ColorStyle_Checked(object sender,RoutedEventArgs e)
 {
  ApplyColorStyleVisibility();
  if(SingleColorPreview is not null&&GradientStartPreview is not null&&GradientEndPreview is not null)UpdateColorPreview();
 }
 void ApplyColorStyleVisibility()
 {
  if(SingleColorPanel is null||GradientColorPanel is null)return;
  bool gradient=GradientColorStyle.IsChecked==true;
  SingleColorPanel.Visibility=gradient?Visibility.Collapsed:Visibility.Visible;
  GradientColorPanel.Visibility=gradient?Visibility.Visible:Visibility.Collapsed;
  ScheduleSettingsSave();
 }
 void UpdateColorPreview()
 {
  if(SingleColorPreview is null||SingleColorText is null||GradientStartPreview is null||GradientStartText is null||GradientEndPreview is null||GradientEndText is null)return;
  var start=System.Windows.Media.Color.FromRgb(primaryColor.Red,primaryColor.Green,primaryColor.Blue);
  var end=System.Windows.Media.Color.FromRgb(secondaryColor.Red,secondaryColor.Green,secondaryColor.Blue);
  SingleColorPreview.Background=new SolidColorBrush(start);
  SingleColorText.Text=$"主色  #{primaryColor.Red:X2}{primaryColor.Green:X2}{primaryColor.Blue:X2}";
  GradientStartPreview.Background=new SolidColorBrush(start);
  GradientStartText.Text=$"起始  #{primaryColor.Red:X2}{primaryColor.Green:X2}{primaryColor.Blue:X2}";
  GradientEndPreview.Background=new SolidColorBrush(end);
  GradientEndText.Text=$"結束  #{secondaryColor.Red:X2}{secondaryColor.Green:X2}{secondaryColor.Blue:X2}";
 }
 void GainSlider_ValueChanged(object sender,RoutedPropertyChangedEventArgs<double> e){Volatile.Write(ref liveGain,(float)e.NewValue);ScheduleSettingsSave();}
 void SmoothSlider_ValueChanged(object sender,RoutedPropertyChangedEventArgs<double> e){Volatile.Write(ref liveSmoothing,(float)e.NewValue);ScheduleSettingsSave();}
 void Settings_Changed(object sender,RoutedEventArgs e)=>ScheduleSettingsSave();
 void WindowBounds_Changed(object? sender,EventArgs e)=>ScheduleSettingsSave();
 void ScheduleSettingsSave()
 {
  if(!settingsReady)return;settingsSaveTimer.Stop();settingsSaveTimer.Start();
 }
 void SettingsSaveTimer_Tick(object? sender,EventArgs e){settingsSaveTimer.Stop();SaveSettingsNow();}
 void RestoreSettings(AppSettings settings)
 {
  HostBox.Text=string.IsNullOrWhiteSpace(settings.Host)?"127.0.0.1":settings.Host;
  PortBox.Text=(settings.Port is>=1 and<=65535?settings.Port:6742).ToString();
  SelectByTag(ModeCombo,Enum.IsDefined(settings.Mode)?settings.Mode.ToString():LightingMode.ThreeBand.ToString());
  SelectByTag(FpsCombo,new[]{10,20,30,60}.Contains(settings.Fps)?settings.Fps.ToString():"20");
  LeftToRightDirection.IsChecked=settings.Direction!=LightingDirection.RightToLeft;
  RightToLeftDirection.IsChecked=settings.Direction==LightingDirection.RightToLeft;
  SolidColorStyle.IsChecked=!settings.IsGradient;GradientColorStyle.IsChecked=settings.IsGradient;
  primaryColor=settings.PrimaryColor;secondaryColor=settings.SecondaryColor;
  GainSlider.Value=Math.Clamp(settings.Gain,.5f,8f);SmoothSlider.Value=Math.Clamp(settings.Smoothing,0,.95f);
  ProcessCombo.SelectedItem=Processes.FirstOrDefault(x=>string.Equals(x.Name,settings.ProcessName,StringComparison.OrdinalIgnoreCase))??Processes[0];
  RestoreWindowBounds(settings);
 }
 void RestoreWindowBounds(AppSettings settings)
 {
  if(settings.WindowLeft is not double left||settings.WindowTop is not double top||settings.WindowWidth is not double width||settings.WindowHeight is not double height)return;
  if(!double.IsFinite(left)||!double.IsFinite(top)||!double.IsFinite(width)||!double.IsFinite(height)||width<MinWidth||height<MinHeight)return;
  var virtualScreen=new Rect(SystemParameters.VirtualScreenLeft,SystemParameters.VirtualScreenTop,SystemParameters.VirtualScreenWidth,SystemParameters.VirtualScreenHeight);
  var visible=Rect.Intersect(virtualScreen,new Rect(left,top,width,height));
  if(visible.IsEmpty||visible.Width<80||visible.Height<80)return;
  Width=Math.Min(width,virtualScreen.Width);Height=Math.Min(height,virtualScreen.Height);
  Left=Math.Clamp(left,virtualScreen.Left,virtualScreen.Right-Math.Min(80,Width));
  Top=Math.Clamp(top,virtualScreen.Top,virtualScreen.Bottom-Math.Min(80,Height));
  WindowStartupLocation=WindowStartupLocation.Manual;hasSavedWindowBounds=true;restoreMaximized=settings.WindowMaximized;
 }
 static void SelectByTag(System.Windows.Controls.ComboBox combo,string tag)
 {
  combo.SelectedItem=combo.Items.OfType<ComboBoxItem>().FirstOrDefault(x=>string.Equals(x.Tag?.ToString(),tag,StringComparison.OrdinalIgnoreCase))??combo.Items[0];
 }
 void SaveSettingsNow()
 {
  if(!settingsReady)return;
  int port=int.TryParse(PortBox.Text,out var parsed)&&parsed is>=1 and<=65535?parsed:6742;
  var bounds=WindowState==WindowState.Normal?new Rect(Left,Top,ActualWidth,ActualHeight):RestoreBounds;
  settingsStore.Save(new(){Host=HostBox.Text.Trim(),Port=port,Mode=SelectedMode(),Direction=SelectedDirection(),IsGradient=GradientColorStyle.IsChecked==true,PrimaryColor=primaryColor,SecondaryColor=secondaryColor,Gain=(float)GainSlider.Value,Smoothing=(float)SmoothSlider.Value,Fps=SelectedFps(),ProcessName=(ProcessCombo.SelectedItem as ProcessItem)?.Name,WindowLeft=bounds.Left,WindowTop=bounds.Top,WindowWidth=bounds.Width,WindowHeight=bounds.Height,WindowMaximized=WindowState==WindowState.Maximized});
 }
 async Task WatchAsync(Task task)
 {
  try{await task;}catch(OperationCanceledException){}catch(Exception ex)
  {
   Status("燈效錯誤","#EF4444");MessageText.Text=$"更新失敗：{ex.Message}。正在還原原始燈效。";
   await StopAsync(true);
  }
 }
 async void Stop_Click(object sender,RoutedEventArgs e)=>await StopAsync(true);
 void Refresh_Click(object sender,RoutedEventArgs e)=>RefreshProcesses();
 void RefreshProcesses()
 {
  int? old=(ProcessCombo.SelectedItem as ProcessItem)?.Id;var list=new List<ProcessItem>{new(null,"整台電腦輸出（所有程式）")};
  foreach(var p in Process.GetProcesses())try{if(p.Id!=Environment.ProcessId&&!string.IsNullOrWhiteSpace(p.MainWindowTitle))list.Add(new(p.Id,p.ProcessName));}catch{}finally{p.Dispose();}
  Processes.Clear();foreach(var p in list.DistinctBy(x=>x.Id).OrderBy(x=>x.Id is not null).ThenBy(x=>x.Name))Processes.Add(p);
  ProcessCombo.SelectedItem=Processes.FirstOrDefault(x=>x.Id==old)??Processes[0];
 }
 async Task StopAsync(bool restore)
 {
  DiagnosticsLog.Write("收到停止要求");await stopGate.WaitAsync();
  try
  {
   StopButton.IsEnabled=false;Status(restore?"停止並還原中…":"停止中…","#FBBF24");
   var errors=new List<string>();var c=Interlocked.Exchange(ref cancellation,null);var task=Interlocked.Exchange(ref running,null);
   try{c?.Cancel();DiagnosticsLog.Write("已送出取消要求");}catch(ObjectDisposedException){}
   if(task is not null)
   {
    var completed=await Task.WhenAny(task,Task.Delay(TimeSpan.FromSeconds(3)));
    if(completed==task)try{await task;}catch(OperationCanceledException){}catch(Exception ex){errors.Add("燈效工作："+ex.Message);}
    else
    {
     var abortTask=rgb.AbortConnectionAsync();await Task.WhenAny(abortTask,Task.Delay(TimeSpan.FromSeconds(2)));
     errors.Add("停止更新逾時，已重設 OpenRGB 連線");await Task.WhenAny(task,Task.Delay(TimeSpan.FromMilliseconds(500)));
    }
   }
   c?.Dispose();
   try
   {
    DiagnosticsLog.Write("開始停止音訊");var audioTask=Task.Run(async()=>await audio.StopAsync());var completed=await Task.WhenAny(audioTask,Task.Delay(TimeSpan.FromSeconds(4)));
    if(completed==audioTask){await audioTask;DiagnosticsLog.Write("音訊已停止");}else{errors.Add("停止音訊擷取逾時");DiagnosticsLog.Write("停止音訊逾時");}
   }
   catch(Exception ex){errors.Add("音訊擷取："+ex.Message);}
   if(restore)try
   {
    DiagnosticsLog.Write("開始載入 OpenRGB 還原 Profile");var restoreTask=Task.Run(rgb.Restore);var completed=await Task.WhenAny(restoreTask,Task.Delay(TimeSpan.FromSeconds(8)));
    if(completed==restoreTask){errors.AddRange(await restoreTask);DiagnosticsLog.Write("OpenRGB Profile 還原工作完成");}
    else
    {
     var abortTask=rgb.AbortConnectionAsync();await Task.WhenAny(abortTask,Task.Delay(TimeSpan.FromSeconds(2)));
     errors.Add("OpenRGB Profile 還原逾時，已中斷連線");
    }
   }
   catch(Exception ex){errors.Add("OpenRGB 還原："+ex.Message);}
   if(restore)try
   {
    DiagnosticsLog.Write("開始恢復廠商 RGB 軟體");errors.AddRange(await Task.Run(()=>conflicts.RestoreAsync()));
   }
   catch(Exception ex){errors.Add("RGB 軟體恢復："+ex.Message);}
   Controls(false);BassMeter.Value=MidMeter.Value=TrebleMeter.Value=0;
   if(errors.Count==0){Status(Devices.Count>0?"已還原":"尚未連線",Devices.Count>0?"#22C55E":"#64748B");if(restore)MessageText.Text="燈效已停止並還原；OpenRGB 連線會保留到視窗關閉，可直接再次開始。";}
   else{Status("部分還原失敗","#EF4444");MessageText.Text="無法完整還原："+string.Join("；",errors);}
  }
  finally{DiagnosticsLog.Write("停止流程結束");stopGate.Release();}
 }
 void UpdateMeters(AudioBands b)=>Dispatcher.BeginInvoke(()=>{BassMeter.Value=b.Bass;MidMeter.Value=b.Mid;TrebleMeter.Value=b.Treble;});
 void Controls(bool active){StartButton.IsEnabled=TestButton.IsEnabled=!active&&Devices.Count>0;StopButton.IsEnabled=active;ConnectButton.IsEnabled=!active;ProcessCombo.IsEnabled=!active;FpsCombo.IsEnabled=!active;ModeCombo.IsEnabled=ColorStylePanel.IsEnabled=ColorPalettePanel.IsEnabled=DirectionPanel.IsEnabled=!active;}
 void Status(string text,string color){StatusText.Text=text;StatusDot.Fill=(System.Windows.Media.Brush)new BrushConverter().ConvertFromString(color)!;}
 static string? RgbConflictWarning()
 {
  static int Count(string name){var items=Process.GetProcessesByName(name);try{return items.Length;}finally{foreach(var item in items)item.Dispose();}}
  var warnings=new List<string>();int openRgb=Count("OpenRGB");if(openRgb>1)warnings.Add($"偵測到 {openRgb} 個 OpenRGB 程序");
  if(Count("lghub_agent")>0)warnings.Add("Logitech G HUB 仍在執行");
  return warnings.Count==0?null:"可能發生 RGB 控制衝突："+string.Join("；",warnings)+"。G502 若維持預設燈效，請只保留一個 OpenRGB，並完全退出 G HUB。";
 }
 protected override async void OnClosing(CancelEventArgs e)
 {
  settingsSaveTimer.Stop();SaveSettingsNow();
  if(closing){base.OnClosing(e);return;}
  e.Cancel=true;lifetimeCancellation.Cancel();
  try
  {
   if(running is not null||conflicts.IsSuspended)await StopAsync(true);
   await rgb.AbortConnectionAsync();await openRgbHost.StopAsync();
  }
  finally{closing=true;Close();}
 }
 protected override void OnClosed(EventArgs e){lifetimeCancellation.Dispose();base.OnClosed(e);}
}
