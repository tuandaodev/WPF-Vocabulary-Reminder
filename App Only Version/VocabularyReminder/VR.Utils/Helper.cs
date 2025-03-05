using System;

namespace VR.Utils
{
    public class Helper
    {
        public static string GetCambridgeWordUrl(string word)
        {
            return $"https://dictionary.cambridge.org/vi/dictionary/english-vietnamese/{Uri.EscapeDataString(word.ToLower())}";
        }

        public static string GetOxfordWordUrl(string word)
        {
            return $"https://www.oxfordlearnersdictionaries.com/definition/english/{Uri.EscapeDataString(word.ToLower())}";
        }
    }
}
