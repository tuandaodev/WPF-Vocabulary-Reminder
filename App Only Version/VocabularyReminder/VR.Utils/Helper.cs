using System;

namespace VR.Utils
{
    public class Helper
    {
        public static string GetCambridgeWordUrl(string word)
        {
            string parseWord = word.Trim().Replace(" ", "-");
            return $"https://dictionary.cambridge.org/vi/dictionary/english-vietnamese/{Uri.EscapeDataString(parseWord.ToLower())}";
        }

        public static string GetOxfordWordUrl(string word)
        {
            string parseWord = word.Trim().Replace(" ", "-");
            return $"https://www.oxfordlearnersdictionaries.com/definition/english/{Uri.EscapeDataString(parseWord.ToLower())}";
        }
    }
}
