using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using VocabularyReminder.VR.Common;

namespace VR.Services
{
    public class GeminiProvider : ILLMProvider
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string BASE_URL = "https://generativelanguage.googleapis.com/v1beta";

        static GeminiProvider()
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public AIProvider ProviderType => AIProvider.Gemini;

        public async Task<ApiTestResult> TestConnectionAsync(string apiKey)
        {
            try
            {
                var result = await GenerateTextAsync("Hello, this is a test message.", apiKey);
                return new ApiTestResult
                {
                    IsSuccess = true,
                    ResponseMessage = "Gemini API connection successful!"
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
            const string url = BASE_URL + "/models/gemini-2.0-flash:generateContent";
            
            var request = new HttpRequestMessage(HttpMethod.Post, $"{url}?key={apiKey}");
            request.Headers.Add("User-Agent", "VocabularyReminder/1.0");

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    maxOutputTokens = 1000,
                    temperature = 0.7
                }
            };

            var jsonContent = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                if (jsonResponse.TryGetProperty("candidates", out var candidates) && 
                    candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var content) &&
                        content.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0)
                    {
                        var firstPart = parts[0];
                        if (firstPart.TryGetProperty("text", out var text))
                        {
                            return text.GetString()?.Trim() ?? string.Empty;
                        }
                    }
                }
                
                throw new Exception("Unexpected response format from Gemini API");
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
            var errorMessage = $"Gemini API error ({statusCode}): {errorContent}";
            
            try
            {
                var errorJson = JsonSerializer.Deserialize<JsonElement>(errorContent);
                if (errorJson.TryGetProperty("error", out var error))
                {
                    if (error.TryGetProperty("message", out var message))
                    {
                        errorMessage = $"Gemini API error: {message.GetString()}";
                    }
                    
                    if (error.TryGetProperty("code", out var code))
                    {
                        var errorCode = code.GetInt32();
                        switch (errorCode)
                        {
                            case 400:
                                if (errorContent.Contains("API_KEY_INVALID"))
                                {
                                    errorMessage = "Invalid Gemini API key. Please check your API key and try again.";
                                }
                                else
                                {
                                    errorMessage = "Bad request to Gemini API. Please check your input.";
                                }
                                break;
                            case 403:
                                errorMessage = "Gemini API access forbidden. Please check your API key permissions.";
                                break;
                            case 429:
                                errorMessage = "Gemini API rate limit exceeded. Please wait and try again.";
                                break;
                            case 500:
                                errorMessage = "Gemini API internal server error. Please try again later.";
                                break;
                        }
                    }
                    
                    // Check for specific error types in status field
                    if (error.TryGetProperty("status", out var status))
                    {
                        var statusValue = status.GetString();
                        switch (statusValue)
                        {
                            case "PERMISSION_DENIED":
                                errorMessage = "Gemini API permission denied. Please check your API key.";
                                break;
                            case "RESOURCE_EXHAUSTED":
                                errorMessage = "Gemini API quota exhausted. Please check your Google Cloud account.";
                                break;
                            case "INVALID_ARGUMENT":
                                errorMessage = "Invalid argument provided to Gemini API.";
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