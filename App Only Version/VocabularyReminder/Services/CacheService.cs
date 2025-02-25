using System.Collections.Concurrent;

namespace VocabularyReminder.Services
{
    public static class CacheService
    {
        private static readonly ConcurrentDictionary<string, string> TranslationCache = new ConcurrentDictionary<string, string>();
        private static readonly ConcurrentDictionary<string, byte[]> TtsCache = new ConcurrentDictionary<string, byte[]>();

        public static bool TryGetTranslation(string text, out string translation)
        {
            return TranslationCache.TryGetValue(text, out translation);
        }

        public static void CacheTranslation(string text, string translation)
        {
            TranslationCache.TryAdd(text, translation);
        }

        public static bool TryGetTtsAudio(string text, out byte[] audio)
        {
            return TtsCache.TryGetValue(text, out audio);
        }

        public static void CacheTtsAudio(string text, byte[] audio)
        {
            TtsCache.TryAdd(text, audio);
        }

        public static void Clear()
        {
            TranslationCache.Clear();
            TtsCache.Clear();
        }
    }
}