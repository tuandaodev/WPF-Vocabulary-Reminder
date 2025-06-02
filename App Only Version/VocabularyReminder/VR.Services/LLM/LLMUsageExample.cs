using System;
using System.Threading.Tasks;
using VocabularyReminder.VR.Common;

namespace VR.Services
{
    /// <summary>
    /// Example class showing how to use the LLM factory and services
    /// This can be used as a reference for integrating LLM functionality throughout the application
    /// </summary>
    public static class LLMUsageExample
    {
        /// <summary>
        /// Example: Get a translation using the configured LLM provider
        /// </summary>
        public static async Task<string> TranslateWordExample(string word, string targetLanguage = "Vietnamese")
        {
            try
            {
                // Check if configuration is valid
                if (!LLMProviderFactory.IsCurrentConfigurationValid())
                {
                    throw new InvalidOperationException("LLM provider is not properly configured. Please check your settings.");
                }

                // Get the configured LLM service
                var llmService = LLMProviderFactory.GetLLMService();
                
                // Translate the word
                return await llmService.TranslateAsync(word, targetLanguage);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to translate word '{word}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Example: Get definition for a vocabulary word
        /// </summary>
        public static async Task<string> GetWordDefinitionExample(string word)
        {
            try
            {
                if (!LLMProviderFactory.IsCurrentConfigurationValid())
                {
                    throw new InvalidOperationException("LLM provider is not properly configured. Please check your settings.");
                }

                var llmService = LLMProviderFactory.GetLLMService();
                return await llmService.GetDefinitionAsync(word);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get definition for '{word}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Example: Generate example sentences for a word
        /// </summary>
        public static async Task<string> GenerateExamplesExample(string word, int count = 3)
        {
            try
            {
                if (!LLMProviderFactory.IsCurrentConfigurationValid())
                {
                    throw new InvalidOperationException("LLM provider is not properly configured. Please check your settings.");
                }

                var llmService = LLMProviderFactory.GetLLMService();
                return await llmService.GetExamplesAsync(word, count);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to generate examples for '{word}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Example: Create a learning exercise for a vocabulary word
        /// </summary>
        public static async Task<string> CreateLearningExerciseExample(string word)
        {
            try
            {
                if (!LLMProviderFactory.IsCurrentConfigurationValid())
                {
                    throw new InvalidOperationException("LLM provider is not properly configured. Please check your settings.");
                }

                var llmService = LLMProviderFactory.GetLLMService();
                return await llmService.CreateVocabularyExerciseAsync(word);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create exercise for '{word}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Example: Test connection to a specific provider
        /// </summary>
        public static async Task<ApiTestResult> TestProviderConnectionExample(AIProvider provider, string apiKey)
        {
            try
            {
                var llmProvider = LLMProviderFactory.GetProvider(provider);
                return await llmProvider.TestConnectionAsync(apiKey);
            }
            catch (Exception ex)
            {
                return new ApiTestResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Failed to test {provider} connection: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Example: Get available providers
        /// </summary>
        public static void ShowAvailableProvidersExample()
        {
            Console.WriteLine("Available LLM Providers:");
            foreach (var provider in LLMProviderFactory.GetAvailableProviders())
            {
                Console.WriteLine($"- {provider}");
            }
        }

        /// <summary>
        /// Example: Enhanced vocabulary processing with multiple LLM features
        /// </summary>
        public static async Task<VocabularyEnhancementResult> EnhanceVocabularyExample(string word)
        {
            try
            {
                if (!LLMProviderFactory.IsCurrentConfigurationValid())
                {
                    throw new InvalidOperationException("LLM provider is not properly configured. Please check your settings.");
                }

                var llmService = LLMProviderFactory.GetLLMService();
                
                // Run multiple LLM operations in parallel for efficiency
                var definitionTask = llmService.GetDefinitionAsync(word);
                var translationTask = llmService.TranslateAsync(word, "Vietnamese");
                var synonymsTask = llmService.GetSynonymsAsync(word);
                var examplesTask = llmService.GetExamplesAsync(word, 2);
                var mnemonicTask = llmService.GenerateMnemonicAsync(word);

                await Task.WhenAll(definitionTask, translationTask, synonymsTask, examplesTask, mnemonicTask);

                return new VocabularyEnhancementResult
                {
                    Word = word,
                    Definition = await definitionTask,
                    Translation = await translationTask,
                    Synonyms = await synonymsTask,
                    Examples = await examplesTask,
                    Mnemonic = await mnemonicTask,
                    ProviderUsed = llmService.ProviderType.ToString()
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to enhance vocabulary for '{word}': {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Result class for enhanced vocabulary processing
    /// </summary>
    public class VocabularyEnhancementResult
    {
        public string Word { get; set; }
        public string Definition { get; set; }
        public string Translation { get; set; }
        public string Synonyms { get; set; }
        public string Examples { get; set; }
        public string Mnemonic { get; set; }
        public string ProviderUsed { get; set; }
    }
}