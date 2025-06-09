using System.Threading.Tasks;
using VR.Domain.Models;
using VR.Services;

namespace VocabularyReminder.VR.Services
{
    public class GlobalVocabularyService
    {
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
