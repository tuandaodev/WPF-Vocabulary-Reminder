using System.Threading.Tasks;
using VR.Domain.Models;
using VR.Services;

namespace VocabularyReminder.VR.Services
{
    public class GlobalVocabularyService
    {

        /// <summary>
        /// Global function to play audio, try to play from mp3 from oxford, then callback to Google Translate voice
        /// </summary>
        public static async Task PlaySoundAsync(Vocabulary item)
        {
            if (!string.IsNullOrEmpty(item.PlayURL2))
            {
                Mp3Service.PlayFile(item);
            } else
            {
                await TextToSpeechService.SpeakTextAsync(item.Word);
            }
        }
    }
}
