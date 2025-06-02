using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using VocabularyReminder.VR.Common;

namespace VR.Services
{
    public class AIApiTestService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        static AIApiTestService()
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<ApiTestResult> TestApiAsync(AIProvider provider, string apiKey)
        {
            try
            {
                switch (provider)
                {
                    case AIProvider.ChatGPT:
                        return await TestChatGptApiAsync(apiKey);
                    case AIProvider.Gemini:
                        return await TestGeminiApiAsync(apiKey);
                    default:
                        return new ApiTestResult
                        {
                            IsSuccess = false,
                            ErrorMessage = "Unsupported AI provider"
                        };
                }
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

        private async Task<ApiTestResult> TestChatGptApiAsync(string apiKey)
        {
            const string url = "https://api.openai.com/v1/chat/completions";
            
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", $"Bearer {apiKey}");
            request.Headers.Add("User-Agent", "VocabularyReminder/1.0");

            var payload = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new { role = "user", content = "Hello, this is a test message." }
                },
                max_tokens = 10
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
                    return new ApiTestResult
                    {
                        IsSuccess = true,
                        ResponseMessage = "ChatGPT API connection successful!"
                    };
                }
                else
                {
                    return new ApiTestResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Unexpected response format from ChatGPT API"
                    };
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorMessage = $"ChatGPT API error ({response.StatusCode}): {errorContent}";
                
                try
                {
                    var errorJson = JsonSerializer.Deserialize<JsonElement>(errorContent);
                    if (errorJson.TryGetProperty("error", out var error) &&
                        error.TryGetProperty("message", out var message))
                    {
                        errorMessage = $"ChatGPT API error: {message.GetString()}";
                    }
                }
                catch
                {
                    // Use the default error message if JSON parsing fails
                }
                
                return new ApiTestResult
                {
                    IsSuccess = false,
                    ErrorMessage = errorMessage
                };
            }
        }

        private async Task<ApiTestResult> TestGeminiApiAsync(string apiKey)
        {
            const string url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";
            
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
                            new { text = "Hello, this is a test message." }
                        }
                    }
                },
                generationConfig = new
                {
                    maxOutputTokens = 10
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
                    return new ApiTestResult
                    {
                        IsSuccess = true,
                        ResponseMessage = "Gemini API connection successful!"
                    };
                }
                else
                {
                    return new ApiTestResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Unexpected response format from Gemini API"
                    };
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorMessage = $"Gemini API error ({response.StatusCode}): {errorContent}";
                
                try
                {
                    var errorJson = JsonSerializer.Deserialize<JsonElement>(errorContent);
                    if (errorJson.TryGetProperty("error", out var error) &&
                        error.TryGetProperty("message", out var message))
                    {
                        errorMessage = $"Gemini API error: {message.GetString()}";
                    }
                }
                catch
                {
                    // Use the default error message if JSON parsing fails
                }
                
                return new ApiTestResult
                {
                    IsSuccess = false,
                    ErrorMessage = errorMessage
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