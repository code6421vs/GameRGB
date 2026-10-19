using OpenRGB.NET;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Rgb=OpenRGB.NET.Color;
namespace GameRgb;
public sealed class OpenRgbService:IDisposable
{
 readonly record struct ZoneSpan(string Name,int Start,int Count);
 readonly record struct SingleLedGradient(float Amount,long ChangeAt);
 const double SingleLedGradientSeconds=15;
 OpenRgbClient? client;string? lastHost;int lastPort;string? restoreProfile;readonly Dictionary<int,int> counts=[];readonly Dictionary<int,ZoneSpan[]> zones=[];readonly Dictionary<int,SingleLedGradient> singleLedGradients=[];readonly HashSet<int> loggedUpdates=[];
 public IReadOnlyList<DeviceItem> Connect(string host,int port)
 {
  Disconnect();lastHost=host;lastPort=port;var c=new OpenRgbClient(host,port,"GameRGB",true,2500);var devices=c.GetAllControllerData();
  if(ApplySavedZoneSizes(c,devices)){Thread.Sleep(150);devices=c.GetAllControllerData();}
  var list=new List<DeviceItem>();
  for(int id=0;id<devices.Length;id++)
  {
   int n=devices[id].Leds.Length;counts[id]=n;int offset=0;
   zones[id]=devices[id].Zones.Select(zone=>{int count=checked((int)zone.LedCount);var span=new ZoneSpan(zone.Name,offset,count);offset+=count;return span;}).ToArray();
   list.Add(new(){Id=id,Name=devices[id].Name,LedCount=n});
   DiagnosticsLog.Write($"偵測裝置 {id}: {devices[id].Name}，{n} LED");
   foreach(var zone in zones[id])DiagnosticsLog.Write($"  Zone: {zone.Name}，起點 {zone.Start}，{zone.Count} LED");
  }
  client=c;return list;
 }
 static bool ApplySavedZoneSizes(OpenRgbClient c,Device[] devices)
 {
  var path=Path.Combine(AppContext.BaseDirectory,"OpenRGB","service_config","Configuration.json");
  if(!File.Exists(path))return false;
  try
  {
   using var document=JsonDocument.Parse(File.ReadAllText(path));
   if(!document.RootElement.TryGetProperty("controllers",out var controllers))return false;
   bool changed=false;
   for(int deviceId=0;deviceId<devices.Length;deviceId++)
   {
    var device=devices[deviceId];JsonElement saved=default;bool found=false;
    foreach(var controller in controllers.EnumerateArray())
     if(controller.TryGetProperty("name",out var name)&&string.Equals(name.GetString(),device.Name,StringComparison.OrdinalIgnoreCase)){saved=controller;found=true;break;}
    if(!found||!saved.TryGetProperty("zones",out var savedZones))continue;
    foreach(var zone in device.Zones)
    {
     if(zone.LedsMax<=zone.LedsMin)continue;
     int desired=-1;
     foreach(var savedZone in savedZones.EnumerateArray())
      if(savedZone.TryGetProperty("name",out var zoneName)&&string.Equals(zoneName.GetString(),zone.Name,StringComparison.OrdinalIgnoreCase)&&savedZone.TryGetProperty("leds_count",out var count)){desired=count.GetInt32();break;}
     if(desired<zone.LedsMin||desired>zone.LedsMax||desired==zone.LedCount)continue;
     c.ResizeZone(deviceId,zone.Index,desired);changed=true;
     DiagnosticsLog.Write($"套用 Zone 長度：{device.Name} / {zone.Name} = {desired} LED");
    }
   }
   return changed;
  }
  catch(Exception ex){DiagnosticsLog.Write("讀取 OpenRGB Zone 設定失敗："+ex.Message);return false;}
 }
 public void Prepare(IEnumerable<int> ids)
 {
  var c=client??throw new InvalidOperationException("尚未連線至 OpenRGB。");
  if(restoreProfile is null)
  {
   var profile="GameRGB_AutoRestore_"+Guid.NewGuid().ToString("N");
   c.SaveProfile(profile);restoreProfile=profile;
  }
  loggedUpdates.Clear();singleLedGradients.Clear();foreach(var id in ids)c.SetCustomMode(id);
 }
 public IReadOnlyList<string> Restore()
 {
  var errors=new List<string>();var profile=Interlocked.Exchange(ref restoreProfile,null);if(profile is null)return errors;
  var c=client;
  if(c is null&&lastHost is not null)try{c=new OpenRgbClient(lastHost,lastPort,"GameRGB Restore",true,2500);client=c;}catch(Exception ex){errors.Add("重新連線失敗："+ex.Message);return errors;}
  if(c is null){errors.Add("OpenRGB 連線已中斷");return errors;}
  try{c.LoadProfile(profile);}catch(Exception ex){errors.Add("載入還原設定檔："+ex.Message);return errors;}
  try{c.DeleteProfile(profile);}catch(Exception ex){errors.Add("清理臨時設定檔："+ex.Message);}
  return errors;
 }
 public void Update(IEnumerable<int> ids,AudioBands b,LightingMode mode,LightingPaint paint,LightingDirection direction)
 {
  var c=client??throw new InvalidOperationException("OpenRGB 連線已中斷。");
  foreach(var id in ids)
  {
   if(!counts.TryGetValue(id,out int n)||n==0)continue;
   var effectivePaint=SingleLedPaint(id,n,paint);
   Rgb[] Render(int count)=>mode switch
   {
    LightingMode.ThreeBand=>Bands(count,b,direction),
    LightingMode.VuMeter=>Meter(count,b.Level,direction),
    LightingMode.SolidPulse=>Pulse(count,b.Level,effectivePaint,direction),
    LightingMode.MonoRing=>MonoRing(count,b.Level,effectivePaint,direction),
    LightingMode.StaticColor=>Paint(count,effectivePaint,1,direction),
    _=>Pulse(count,b.Level,effectivePaint,direction)
   };
   var colors=Render(n);
   if(zones.TryGetValue(id,out var deviceZones))foreach(var zone in deviceZones)
   {
    if(zone.Count<=0||!zone.Name.Contains("Addressable RGB Header",StringComparison.OrdinalIgnoreCase))continue;
    var zoneColors=Render(zone.Count);int copyCount=Math.Min(zone.Count,Math.Min(zoneColors.Length,colors.Length-zone.Start));
    if(copyCount>0)Array.Copy(zoneColors,0,colors,zone.Start,copyCount);
   }
   c.UpdateLeds(id,colors);
   if(loggedUpdates.Add(id))DiagnosticsLog.Write($"開始更新裝置 {id}: {n} LED，模式 {mode}，整機連續輸出");
  }
 }
 public Task AbortConnectionAsync()
 {
  var c=Interlocked.Exchange(ref client,null);if(c is null)return Task.CompletedTask;
  return Task.Run(()=>{try{c.Dispose();}catch{}});
 }
 public void AbortConnection(){var c=Interlocked.Exchange(ref client,null);try{c?.Dispose();}catch{}}
 public void Disconnect(){AbortConnection();counts.Clear();zones.Clear();singleLedGradients.Clear();loggedUpdates.Clear();restoreProfile=null;lastHost=null;lastPort=0;}public void Dispose()=>Disconnect();
 LightingPaint SingleLedPaint(int id,int count,LightingPaint paint)
 {
  if(count!=1||!paint.IsGradient){singleLedGradients.Remove(id);return paint;}
  long now=Stopwatch.GetTimestamp();
  if(!singleLedGradients.TryGetValue(id,out var state)||now>=state.ChangeAt)
  {
   state=new((float)Random.Shared.NextDouble(),now+(long)(Stopwatch.Frequency*SingleLedGradientSeconds));singleLedGradients[id]=state;
  }
  var color=Blend(paint.Start,paint.End,state.Amount);return new(color,color,false);
 }
 static Rgb[] Bands(int n,AudioBands b,LightingDirection direction)
 {
  var a=new Rgb[n];float bright=ReactiveBrightness(b.Level);if(bright==0)return a;if(n<=2)return Paint(n,new(DominantColor(b),default,false),bright,direction);
  for(int i=0;i<n;i++)
  {
   int position=direction==LightingDirection.LeftToRight?i:n-1-i;
   int band=Math.Min(2,position*3/n);float strength=band switch{0=>b.Bass,1=>b.Mid,_=>b.Treble};
   a[i]=Scale(band switch{0=>new Rgb(255,48,32),1=>new Rgb(32,255,80),_=>new Rgb(48,112,255)},bright*strength);
  }
  return a;
 }
 static Rgb[] Meter(int n,float level,LightingDirection direction)
 {
  var a=new Rgb[n];if(level<=.02f)return a;int lit=(int)MathF.Ceiling(level*n);for(int i=0;i<lit;i++){float p=(float)i/Math.Max(1,n-1);int target=direction==LightingDirection.LeftToRight?i:n-1-i;a[target]=p<.5f?new(B(p*510),255,0):new(255,B((1-p)*510),0);}return a;
 }
 static Rgb[] Pulse(int n,float level,LightingPaint paint,LightingDirection direction)=>Paint(n,paint,ReactiveBrightness(level),direction);
 static Rgb[] MonoRing(int n,float level,LightingPaint paint,LightingDirection direction)
 {
  var a=new Rgb[n];int lit=(int)MathF.Round(Math.Clamp(level,0,1)*n,MidpointRounding.AwayFromZero);for(int i=0;i<lit;i++){int target=direction==LightingDirection.LeftToRight?i:n-1-i;a[target]=PaintColor(i,n,paint);}return a;
 }
 static Rgb[] Paint(int n,LightingPaint paint,float brightness,LightingDirection direction)
 {
  var a=new Rgb[n];for(int i=0;i<n;i++){int source=direction==LightingDirection.LeftToRight?i:n-1-i;a[i]=Scale(PaintColor(source,n,paint),brightness);}return a;
 }
 static Rgb PaintColor(int index,int count,LightingPaint paint)
 {
  if(!paint.IsGradient||count<=1)return ToRgb(paint.Start);
  float amount=(float)index/(count-1);
  return new(B(paint.Start.Red+(paint.End.Red-paint.Start.Red)*amount),B(paint.Start.Green+(paint.End.Green-paint.Start.Green)*amount),B(paint.Start.Blue+(paint.End.Blue-paint.Start.Blue)*amount));
 }
 static Rgb ToRgb(LightingColor color)=>new(color.Red,color.Green,color.Blue);
 static LightingColor Blend(LightingColor start,LightingColor end,float amount)=>new(B(start.Red+(end.Red-start.Red)*amount),B(start.Green+(end.Green-start.Green)*amount),B(start.Blue+(end.Blue-start.Blue)*amount));
 static LightingColor DominantColor(AudioBands b)=>b.Bass>=b.Mid&&b.Bass>=b.Treble?new(255,48,32):b.Mid>=b.Treble?new(32,255,80):new(48,112,255);
 static Rgb Scale(Rgb color,float amount)=>new(B(color.R*amount),B(color.G*amount),B(color.B*amount));
 static byte B(float v)=>(byte)Math.Clamp(v,0,255);
 static float ReactiveBrightness(float level)=>level<=.02f?0:MathF.Pow(Math.Clamp((level-.02f)/.98f,0,1),1.15f);
}
