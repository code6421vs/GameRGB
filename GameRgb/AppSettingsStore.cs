using System.IO;
using System.Text.Json;

namespace GameRgb;

public sealed class AppSettings
{
 public AppLanguage Language{get;set;}=System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("zh",StringComparison.OrdinalIgnoreCase)?AppLanguage.TraditionalChinese:AppLanguage.English;
 public string Host{get;set;}="127.0.0.1";
 public int Port{get;set;}=6742;
 public LightingMode Mode{get;set;}=LightingMode.ThreeBand;
 public LightingDirection Direction{get;set;}=LightingDirection.LeftToRight;
 public bool IsGradient{get;set;}
 public LightingColor PrimaryColor{get;set;}=new(59,130,246);
 public LightingColor SecondaryColor{get;set;}=new(168,85,247);
 public float Gain{get;set;}=2;
 public float Smoothing{get;set;}=.8f;
 public int Fps{get;set;}=20;
 public string? ProcessName{get;set;}
 public double? WindowLeft{get;set;}
 public double? WindowTop{get;set;}
 public double? WindowWidth{get;set;}
 public double? WindowHeight{get;set;}
 public bool WindowMaximized{get;set;}
}

public sealed class AppSettingsStore
{
 readonly string path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"GameRGB","settings.json");
 readonly JsonSerializerOptions options=new(){WriteIndented=true};

 public AppSettings Load()
 {
  try{return File.Exists(path)?JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path),options)??new():new();}
  catch(Exception ex){DiagnosticsLog.Write("讀取設定失敗："+ex.Message);return new();}
 }

 public void Save(AppSettings settings)
 {
  try
  {
   Directory.CreateDirectory(Path.GetDirectoryName(path)!);
   File.WriteAllText(path,JsonSerializer.Serialize(settings,options));
  }
  catch(Exception ex){DiagnosticsLog.Write("儲存設定失敗："+ex.Message);}
 }
}
