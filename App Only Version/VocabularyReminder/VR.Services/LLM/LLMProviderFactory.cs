using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using VR.Infrastructure;
using VocabularyReminder.VR.Common;

namespace VR.Services
{
    public class LLMProviderFactory
    {
        private static readonly Dictionary<AIProvider, ILLMProvider> _providers = new Dictionary<AIProvider, ILLMProvider>();
        private static AIProvider? _currentProvider = null;
        private static string _currentApiKey = string.Empty;

        static LLMProviderFactory()
        {
            // Register all available providers
            RegisterProvider(new ChatGPTProvider());
            RegisterProvider(new GeminiProvider());
        }

        /// <summary>
        /// Registers a new LLM provider
        /// </summary>
        /// <param name="provider">The provider to register</param>
        public static void RegisterProvider(ILLMProvider provider)
        {
            _providers[provider.ProviderType] = provider;
        }

        /// <summary>
        /// Gets the current active LLM provider based on application settings
        /// </summary>
        /// <returns>The current LLM provider</returns>
        public static ILLMProvider GetCurrentProvider()
        {
            LoadCurrentSettings();
            
            if (!_currentProvider.HasValue || !_providers.ContainsKey(_currentProvider.Value))
            {
                throw new InvalidOperationException("No LLM provider is configured. Please configure an AI provider in settings.");
            }

            return _providers[_currentProvider.Value];
        }

        /// <summary>
        /// Gets a specific LLM provider by type
        /// </summary>
        /// <param name="providerType">The type of provider to get</param>
        /// <returns>The requested LLM provider</returns>
        public static ILLMProvider GetProvider(AIProvider providerType)
        {
            if (_providers.ContainsKey(providerType))
            {
                return _providers[providerType];
            }
            
            throw new NotSupportedException($"LLM provider '{providerType}' is not supported.");
        }

        /// <summary>
        /// Gets the current API key from settings
        /// </summary>
        /// <returns>The current API key</returns>
        public static string GetCurrentApiKey()
        {
            LoadCurrentSettings();
            return _currentApiKey;
        }

        /// <summary>
        /// Gets all available provider types
        /// </summary>
        /// <returns>List of available provider types</returns>
        public static IEnumerable<AIProvider> GetAvailableProviders()
        {
            return _providers.Keys;
        }

        /// <summary>
        /// Checks if a provider is available
        /// </summary>
        /// <param name="providerType">The provider type to check</param>
        /// <returns>True if the provider is available</returns>
        public static bool IsProviderAvailable(AIProvider providerType)
        {
            return _providers.ContainsKey(providerType);
        }

        /// <summary>
        /// Validates if the current configuration is valid
        /// </summary>
        /// <returns>True if the current configuration is valid</returns>
        public static bool IsCurrentConfigurationValid()
        {
            LoadCurrentSettings();
            
            return _currentProvider.HasValue && 
                   _providers.ContainsKey(_currentProvider.Value) && 
                   !string.IsNullOrWhiteSpace(_currentApiKey);
        }

        /// <summary>
        /// Gets the LLM service configured for a specific task
        /// </summary>
        /// <returns>A configured LLM service instance</returns>
        public static LLMService GetLLMService()
        {
            var provider = GetCurrentProvider();
            var apiKey = GetCurrentApiKey();
            
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("No API key is configured. Please configure your API key in settings.");
            }
            
            return new LLMService(provider, apiKey);
        }

        /// <summary>
        /// Refreshes the current settings from storage
        /// </summary>
        public static void RefreshSettings()
        {
            _currentProvider = null;
            _currentApiKey = string.Empty;
            LoadCurrentSettings();
        }

        private static void LoadCurrentSettings()
        {
            if (_currentProvider.HasValue && !string.IsNullOrEmpty(_currentApiKey))
            {
                return; // Already loaded
            }

            try
            {
                string settingsPath = ApplicationIO.GetSettingsPath();
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    var settings = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                    // Load AI Provider
                    if (settings.ContainsKey("aiProvider"))
                    {
                        string providerString = ((JsonElement)settings["aiProvider"]).GetString();
                        if (Enum.TryParse<AIProvider>(providerString, out AIProvider provider))
                        {
                            _currentProvider = provider;
                        }
                    }

                    // Load API Key with decryption
                    if (settings.ContainsKey("apiKey"))
                    {
                        string storedApiKey = ((JsonElement)settings["apiKey"]).GetString() ?? string.Empty;
                        if (!string.IsNullOrEmpty(storedApiKey))
                        {
                            try
                            {
                                // Try to decrypt the API key if it's encrypted
                                if (SecurityService.IsEncrypted(storedApiKey))
                                {
                                    _currentApiKey = SecurityService.DecryptString(storedApiKey) ?? string.Empty;
                                }
                                else
                                {
                                    // Handle legacy unencrypted API keys
                                    _currentApiKey = storedApiKey;
                                }
                            }
                            catch (SecurityException)
                            {
                                // If decryption fails, clear the API key
                                _currentApiKey = string.Empty;
                            }
                            catch
                            {
                                // If any other error occurs, clear the API key
                                _currentApiKey = string.Empty;
                            }
                        }
                        else
                        {
                            _currentApiKey = string.Empty;
                        }
                    }
                }
            }
            catch
            {
                // If settings can't be loaded, use defaults
                _currentProvider = null;
                _currentApiKey = string.Empty;
            }

            // Set defaults if nothing was loaded
            if (!_currentProvider.HasValue)
            {
                _currentProvider = AIProvider.ChatGPT; // Default to ChatGPT
            }
        }
    }
}