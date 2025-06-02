using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using VocabularyReminder.VR.Common;

namespace VR.Services
{
    public class ChatGPTProvider : ILLMProvider
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string BASE_URL = "https://api.openai.com/v1";

        static ChatGPTProvider()
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public AIProvider ProviderType => AIProvider.ChatGPT;

        public async Task<ApiTestResult> TestConnectionAsync(string apiKey)
        {
            try
            {
                var result = await GenerateTextAsync("Hello, this is a test message.", apiKey);
                return new ApiTestResult
                {
                    IsSuccess = true,
                    ResponseMessage = "ChatGPT API connection successful!"
                };
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

        public async Task<string> GenerateTextAsync(string prompt, string apiKey)
        {
            const string url = BASE_URL + "/chat/completions";
            
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", $"Bearer {apiKey}");
            request.Headers.Add("User-Agent", "VocabularyReminder/1.0");

            var payload = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = 1000,
                temperature = 0.7
            };

            var jsonContent = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                if (jsonResponse.TryGetProperty("choices", out var choices) && 
                    choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    if (firstChoice.TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var content))
                    {
                        return content.GetString()?.Trim() ?? string.Empty;
                    }
                }
                
                throw new Exception("Unexpected response format from ChatGPT API");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                await HandleApiError(response.StatusCode, errorContent);
                return string.Empty; // This line won't be reached due to exception above
            }
        }

        public async Task<string> TranslateAsync(string text, string targetLanguage, string apiKey)
        {
            var prompt = $"Translate the following text to {targetLanguage}. Provide only the translation, no additional text:\n\n{text}";
            return await GenerateTextAsync(prompt, apiKey);
        }

        public async Task<string> GetDefinitionAsync(string word, string apiKey)
        {
            var prompt = $"Provide a clear, concise definition of the word '{word}'. Include the part of speech and a simple example sentence. Format your response as: [Part of Speech] Definition. Example: [example sentence]";
            return await GenerateTextAsync(prompt, apiKey);
        }

        private async Task HandleApiError(System.Net.HttpStatusCode statusCode, string errorContent)
        {
            var errorMessage = $"ChatGPT API error ({statusCode}): {errorContent}";
            
            try
            {
                var errorJson = JsonSerializer.Deserialize<JsonElement>(errorContent);
                if (errorJson.TryGetProperty("error", out var error))
                {
                    if (error.TryGetProperty("message", out var message))
                    {
                        errorMessage = $"ChatGPT API error: {message.GetString()}";
                    }
                    
                    if (error.TryGetProperty("code", out var code))
                    {
                        var errorCode = code.GetString();
                        switch (errorCode)
                        {
                            case "invalid_api_key":
                                errorMessage = "Invalid ChatGPT API key. Please check your API key and try again.";
                                break;
                            case "insufficient_quota":
                                errorMessage = "ChatGPT API quota exceeded. Please check your OpenAI account.";
                                break;
                            case "model_not_found":
                                errorMessage = "ChatGPT model not found. The specified model may not be available.";
                                break;
                            case "rate_limit_exceeded":
                                errorMessage = "ChatGPT API rate limit exceeded. Please wait and try again.";
                                break;
                        }
                    }
                }
            }
            catch
            {
                // Use the default error message if JSON parsing fails
            }
            
            throw new Exception(errorMessage);
        }
    }
}