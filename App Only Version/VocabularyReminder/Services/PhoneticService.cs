using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;

namespace VocabularyReminder.Services
{
    public static class PhoneticService
    {
        private static readonly HttpClient client = new HttpClient();
        private const string API_BASE_URL = "https://api.dictionaryapi.dev/api/v2/entries/en/";

        public static async Task<string> GetPhonetics(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return null;

            try
            {
                var response = await client.GetAsync($"{API_BASE_URL}{word.Trim().ToLower()}");
                if (!response.IsSuccessStatusCode)
                    return null;

                var content = await response.Content.ReadAsStringAsync();
                using (JsonDocument document = JsonDocument.Parse(content))
                {
                    var root = document.RootElement[0];
                    var phonetics = root.GetProperty("phonetics");
                    
                    foreach (var phonetic in phonetics.EnumerateArray())
                    {
                        if (phonetic.TryGetProperty("text", out var text))
                        {
                            string phoneticText = text.GetString();
                            return phoneticText.Trim('[', ']');
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Silently fail on API errors
                return null;
            }
            return null;
        }
    }
}