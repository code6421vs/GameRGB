using System.Globalization;
using System.Windows;

namespace GameRgb;

public static class Loc
{
 public static AppLanguage Language{get;private set;}=AppLanguage.TraditionalChinese;
 public static bool IsEnglish=>Language==AppLanguage.English;
 public static string T(string traditionalChinese,string english)=>IsEnglish?english:traditionalChinese;

 public static void SetLanguage(AppLanguage language)
 {
  Language=language;
  var culture=CultureInfo.GetCultureInfo(IsEnglish?"en-US":"zh-TW");
  CultureInfo.CurrentCulture=culture;CultureInfo.CurrentUICulture=culture;
  if(System.Windows.Application.Current is null)return;
  var dictionaries=System.Windows.Application.Current.Resources.MergedDictionaries;
  var old=dictionaries.FirstOrDefault(x=>x.Source?.OriginalString.Contains("Resources/Strings.",StringComparison.OrdinalIgnoreCase)==true);
  if(old is not null)dictionaries.Remove(old);
  dictionaries.Insert(0,new ResourceDictionary{Source=new Uri(IsEnglish?"Resources/Strings.en.xaml":"Resources/Strings.zh-TW.xaml",UriKind.Relative)});
 }
}
