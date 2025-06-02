using System;
using System.Threading.Tasks;
using VocabularyReminder.VR.Common;

namespace VR.Services
{
    public class LLMService
    {
        private readonly ILLMProvider _provider;
        private readonly string _apiKey;

        public LLMService(ILLMProvider provider, string apiKey)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new ArgumentException("API key cannot be empty", nameof(apiKey));
            }
        }

        /// <summary>
        /// Gets the provider type
        /// </summary>
        public AIProvider ProviderType => _provider.ProviderType;

        /// <summary>
        /// Tests the connection to the LLM provider
        /// </summary>
        /// <returns>Test result</returns>
        public async Task<ApiTestResult> TestConnectionAsync()
        {
            return await _provider.TestConnectionAsync(_apiKey);
        }

        /// <summary>
        /// Generates text using the LLM
        /// </summary>
        /// <param name="prompt">The prompt to send to the LLM</param>
        /// <returns>Generated text response</returns>
        public async Task<string> GenerateTextAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("Prompt cannot be empty", nameof(prompt));
            }

            return await _provider.GenerateTextAsync(prompt, _apiKey);
        }

        /// <summary>
        /// Translates text to the specified target language
        /// </summary>
        /// <param name="text">Text to translate</param>
        /// <param name="targetLanguage">Target language (e.g., "Vietnamese", "Spanish")</param>
        /// <returns>Translated text</returns>
        public async Task<string> TranslateAsync(string text, string targetLanguage = "Vietnamese")
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Text cannot be empty", nameof(text));
            }

            if (string.IsNullOrWhiteSpace(targetLanguage))
            {
                throw new ArgumentException("Target language cannot be empty", nameof(targetLanguage));
            }

            return await _provider.TranslateAsync(text, targetLanguage, _apiKey);
        }

        /// <summary>
        /// Gets the definition of a word
        /// </summary>
        /// <param name="word">The word to define</param>
        /// <returns>Definition of the word</returns>
        public async Task<string> GetDefinitionAsync(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                throw new ArgumentException("Word cannot be empty", nameof(word));
            }

            return await _provider.GetDefinitionAsync(word, _apiKey);
        }

        /// <summary>
        /// Gets examples of how to use a word in sentences
        /// </summary>
        /// <param name="word">The word to get examples for</param>
        /// <param name="count">Number of examples to generate</param>
        /// <returns>Example sentences</returns>
        public async Task<string> GetExamplesAsync(string word, int count = 3)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                throw new ArgumentException("Word cannot be empty", nameof(word));
            }

            if (count <= 0)
            {
                throw new ArgumentException("Count must be positive", nameof(count));
            }

            var prompt = $"Provide {count} clear, simple example sentences using the word '{word}'. Number each sentence and make them diverse in context.";
            return await _provider.GenerateTextAsync(prompt, _apiKey);
        }

        /// <summary>
        /// Gets a single example sentence for a word with specific meaning context
        /// </summary>
        /// <param name="word">The word to get an example for</param>
        /// <param name="meaning">The specific meaning/definition to use as context</param>
        /// <returns>A single example sentence</returns>
        public async Task<string> GetExampleAsync(string word, string meaning = null)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                throw new ArgumentException("Word cannot be empty", nameof(word));
            }

            var prompt = string.IsNullOrWhiteSpace(meaning)
                ? $"Create one simple example sentence using the word '{word}'. Return only the sentence, no numbering, no explanation, no additional text."
                : $"Create one simple example sentence using the word '{word}' with this specific meaning: '{meaning}'. Return only the sentence, no numbering, no explanation, no additional text.";
            
            var response = await _provider.GenerateTextAsync(prompt, _apiKey);
            return response?.Trim();
        }

        /// <summary>
        /// Gets synonyms for a word
        /// </summary>
        /// <param name="word">The word to find synonyms for</param>
        /// <returns>List of synonyms</returns>
        public async Task<string> GetSynonymsAsync(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                throw new ArgumentException("Word cannot be empty", nameof(word));
            }

            var prompt = $"List 5-10 synonyms for the word '{word}'. Provide only the synonyms separated by commas, no additional text.";
            return await _provider.GenerateTextAsync(prompt, _apiKey);
        }

        /// <summary>
        /// Generates a mnemonic device to help remember a word
        /// </summary>
        /// <param name="word">The word to create a mnemonic for</param>
        /// <returns>Mnemonic device</returns>
        public async Task<string> GenerateMnemonicAsync(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                throw new ArgumentException("Word cannot be empty", nameof(word));
            }

            var prompt = $"Create a simple, memorable mnemonic device to help remember the word '{word}' and its meaning. Make it creative but easy to remember.";
            return await _provider.GenerateTextAsync(prompt, _apiKey);
        }

        /// <summary>
        /// Creates a vocabulary exercise for a word
        /// </summary>
        /// <param name="word">The word to create an exercise for</param>
        /// <returns>Vocabulary exercise</returns>
        public async Task<string> CreateVocabularyExerciseAsync(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                throw new ArgumentException("Word cannot be empty", nameof(word));
            }

            var prompt = $"Create a fill-in-the-blank exercise using the word '{word}'. Provide a sentence with a blank where '{word}' should go, and include the answer. Make it educational and at an intermediate level.";
            return await _provider.GenerateTextAsync(prompt, _apiKey);
        }
    }
}