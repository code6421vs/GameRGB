using System.IO;
namespace GameRgb;
public static class DiagnosticsLog
{
 public static string FilePath{get;}=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"GameRGB","GameRGB.log");
 public static void Write(string message)
 {
  try{Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);File.AppendAllText(FilePath,$"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");}catch{}
 }
}
