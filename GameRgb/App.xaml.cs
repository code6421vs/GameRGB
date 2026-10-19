namespace GameRgb;
public partial class App : System.Windows.Application
{
 public App(){DispatcherUnhandledException+=(_,e)=>DiagnosticsLog.Write("UI 未處理例外："+e.Exception);}
}
