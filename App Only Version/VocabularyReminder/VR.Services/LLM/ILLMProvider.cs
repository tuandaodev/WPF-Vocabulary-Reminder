using System.Threading.Tasks;
using VocabularyReminder.VR.Common;

namespace VR.Services
{
    public interface ILLMProvider
    {
        AIProvider ProviderType { get; }
        Task<ApiTestResult> TestConnectionAsync(string apiKey);
        Task<string> GenerateTextAsync(string prompt, string apiKey);
        Task<string> TranslateAsync(string text, string targetLanguage, string apiKey);
        Task<string> GetDefinitionAsync(string word, string apiKey);
    }
}