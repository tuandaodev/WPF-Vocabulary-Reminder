using System;
using System.Threading.Tasks;
using VocabularyReminder.VR.Common;

namespace VR.Services
{
    public class AIApiTestService
    {
        public async Task<ApiTestResult> TestApiAsync(AIProvider provider, string apiKey)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return new ApiTestResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "API key cannot be empty"
                    };
                }

                // Use the factory to get the appropriate provider
                var llmProvider = LLMProviderFactory.GetProvider(provider);
                return await llmProvider.TestConnectionAsync(apiKey);
            }
            catch (Exception ex)
            {
                return new ApiTestResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }

    public class ApiTestResult
    {
        public bool IsSuccess { get; set; }
        public string ResponseMessage { get; set; }
        public string ErrorMessage { get; set; }
    }
}