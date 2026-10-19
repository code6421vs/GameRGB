using System.ComponentModel;
namespace GameRgb;
public sealed class DeviceItem : INotifyPropertyChanged
{
 private bool selected=true;
 public required int Id {get;init;} public required string Name {get;init;} public required int LedCount {get;init;}
 public bool IsSelected {get=>selected;set{if(selected==value)return;selected=value;PropertyChanged?.Invoke(this,new(nameof(IsSelected)));}}
 public string DisplayName=>$"{Name}  ·  {LedCount} LED";
 public event PropertyChangedEventHandler? PropertyChanged;
}
public sealed record ProcessItem(int? Id,string Name){public string DisplayName=>Id is null?Name:$"{Name}  (PID {Id})";}
public readonly record struct AudioBands(float Bass,float Mid,float Treble,float Level){public static AudioBands Silent=>new(0,0,0,0);}
public readonly record struct LightingColor(byte Red,byte Green,byte Blue);
public readonly record struct LightingPaint(LightingColor Start,LightingColor End,bool IsGradient);
public enum LightingMode{ThreeBand,VuMeter,SolidPulse,MonoRing,StaticColor}
public enum LightingDirection{LeftToRight,RightToLeft}
public enum AppLanguage{TraditionalChinese,English}
