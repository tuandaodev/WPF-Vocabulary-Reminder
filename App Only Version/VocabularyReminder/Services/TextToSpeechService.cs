using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Windows.Media.Core;
using DesktopNotifications.Services;

namespace VocabularyReminder.Services
{
    public class TextToSpeechService
    {
        private static readonly string GoogleTtsUrl = "https://translate.google.com/translate_tts";

        public static async Task SpeakTextAsync(string text)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // Add required headers to mimic browser request
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
                    client.DefaultRequestHeaders.Add("Accept", "*/*");

                    // Build the URL with parameters
                    var queryParams = new[]
                    {
                        $"ie=UTF-8",
                        $"q={HttpUtility.UrlEncode(text)}",
                        "tl=en", // Target language: English
                        "client=tw-ob", // Client parameter required by Google
                        "ttsspeed=1" // Normal speed
                    };

                    var url = $"{GoogleTtsUrl}?{string.Join("&", queryParams)}";

                    // Get the audio stream
                    var response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    // Get the audio stream URL
                    var audioBytes = await response.Content.ReadAsByteArrayAsync();
                    var tempFile = System.IO.Path.Combine(
                        System.IO.Path.GetTempPath(), 
                        $"tts_{Guid.NewGuid()}.mp3"
                    );

                    // Save to temp file
                    System.IO.File.WriteAllBytes(tempFile, audioBytes);

                    // Play using existing PlaybackService
                    var player = PlaybackService.Instance.Player;
                    player.Source = MediaSource.CreateFromUri(new Uri(tempFile));
                    player.Play();

                    // Delete temp file after a delay
                    await Task.Delay(10000); // Wait 10 seconds
                    try
                    {
                        System.IO.File.Delete(tempFile);
                    }
                    catch { /* Ignore deletion errors */ }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error getting text-to-speech audio: " + ex.Message, ex);
            }
        }
    }
}