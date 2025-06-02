using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using VR.Infrastructure;
using VR.Services;
using VocabularyReminder.VR.Common;

namespace VR
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private Dictionary<string, object> _settings;
        private bool _hasUnsavedChanges = false;

        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
            UpdateApiKeyInfo();
        }

        private void LoadSettings()
        {
            _settings = new Dictionary<string, object>();
            string settingsPath = ApplicationIO.GetSettingsPath();

            if (File.Exists(settingsPath))
            {
                try
                {
                    var json = File.ReadAllText(settingsPath);
                    _settings = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            // Load AI Provider
            if (_settings.ContainsKey("aiProvider"))
            {
                string provider = ((JsonElement)_settings["aiProvider"]).GetString();
                if (Enum.TryParse<AIProvider>(provider, out AIProvider providerEnum))
                {
                    foreach (ComboBoxItem item in cmbAiProvider.Items)
                    {
                        if (item.Tag.ToString() == providerEnum.ToString())
                        {
                            cmbAiProvider.SelectedItem = item;
                            break;
                        }
                    }
                }
                else
                {
                    cmbAiProvider.SelectedIndex = 0; // Default to ChatGPT
                }
            }
            else
            {
                cmbAiProvider.SelectedIndex = 0; // Default to ChatGPT
            }

            // Load API Key with security
            if (_settings.ContainsKey("apiKey"))
            {
                string storedApiKey = ((JsonElement)_settings["apiKey"]).GetString();
                if (!string.IsNullOrEmpty(storedApiKey))
                {
                    try
                    {
                        // Try to decrypt the API key if it's encrypted
                        if (SecurityService.IsEncrypted(storedApiKey))
                        {
                            string decryptedApiKey = SecurityService.DecryptString(storedApiKey);
                            txtApiKey.Password = decryptedApiKey ?? string.Empty;
                        }
                        else
                        {
                            // Handle legacy unencrypted API keys - encrypt them on next save
                            txtApiKey.Password = storedApiKey;
                        }
                    }
                    catch (SecurityException ex)
                    {
                        MessageBox.Show($"Failed to decrypt API key: {ex.Message}\nPlease re-enter your API key.", 
                                      "Security Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtApiKey.Password = string.Empty;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading API key: {ex.Message}\nPlease re-enter your API key.", 
                                      "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtApiKey.Password = string.Empty;
                    }
                }
            }

            _hasUnsavedChanges = false;
        }

        private void SaveSettings()
        {
            try
            {
                // Update settings with current values
                var selectedItem = (ComboBoxItem)cmbAiProvider.SelectedItem;
                if (selectedItem?.Tag != null && Enum.TryParse<AIProvider>(selectedItem.Tag.ToString(), out AIProvider provider))
                {
                    _settings["aiProvider"] = provider.ToString();
                }
                else
                {
                    _settings["aiProvider"] = AIProvider.ChatGPT.ToString();
                }
                
                // Encrypt API key before saving
                string apiKey = txtApiKey.Password;
                if (!string.IsNullOrEmpty(apiKey))
                {
                    try
                    {
                        string encryptedApiKey = SecurityService.EncryptString(apiKey);
                        _settings["apiKey"] = encryptedApiKey;
                    }
                    catch (SecurityException ex)
                    {
                        MessageBox.Show($"Failed to encrypt API key: {ex.Message}", "Security Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    _settings["apiKey"] = string.Empty;
                }

                // Save to file
                string settingsPath = ApplicationIO.GetSettingsPath();
                
                // Ensure directory exists
                string settingsDir = Path.GetDirectoryName(settingsPath);
                if (!Directory.Exists(settingsDir))
                {
                    Directory.CreateDirectory(settingsDir);
                }
                
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsPath, json);

                _hasUnsavedChanges = false;
                MessageBox.Show("Settings saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateApiKeyInfo()
        {
            var selectedItem = (ComboBoxItem)cmbAiProvider.SelectedItem;
            if (selectedItem?.Tag != null && Enum.TryParse<AIProvider>(selectedItem.Tag.ToString(), out AIProvider provider))
            {
                switch (provider)
                {
                    case AIProvider.ChatGPT:
                        lblApiKeyInfo.Text = "Enter your OpenAI API key. You can get one from https://platform.openai.com/api-keys";
                        break;
                    case AIProvider.Gemini:
                        lblApiKeyInfo.Text = "Enter your Google AI Studio API key. You can get one from https://aistudio.google.com/app/apikey";
                        break;
                    default:
                        lblApiKeyInfo.Text = "Enter your API key for the selected AI provider";
                        break;
                }
            }
            else
            {
                lblApiKeyInfo.Text = "Enter your API key for the selected AI provider";
            }
        }

        private async void TestApiConnection()
        {
            var selectedItem = (ComboBoxItem)cmbAiProvider.SelectedItem;
            var apiKey = txtApiKey.Password;

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                lblConnectionStatus.Text = "Please enter an API key before testing";
                lblConnectionStatus.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            if (selectedItem?.Tag == null || !Enum.TryParse<AIProvider>(selectedItem.Tag.ToString(), out AIProvider provider))
            {
                lblConnectionStatus.Text = "Please select a valid AI provider";
                lblConnectionStatus.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            btnTestConnection.IsEnabled = false;
            btnTestConnection.Content = "Testing...";
            lblConnectionStatus.Text = "Testing API connection...";
            lblConnectionStatus.Foreground = System.Windows.Media.Brushes.Blue;

            try
            {
                var apiTestService = new AIApiTestService();
                var result = await apiTestService.TestApiAsync(provider, apiKey);

                if (result.IsSuccess)
                {
                    lblConnectionStatus.Text = $"✓ {result.ResponseMessage}";
                    lblConnectionStatus.Foreground = System.Windows.Media.Brushes.Green;
                }
                else
                {
                    lblConnectionStatus.Text = $"✗ {result.ErrorMessage}";
                    lblConnectionStatus.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                lblConnectionStatus.Text = $"✗ Connection failed: {ex.Message}";
                lblConnectionStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
            finally
            {
                btnTestConnection.IsEnabled = true;
                btnTestConnection.Content = "Test API Connection";
            }
        }

        private void cmbAiProvider_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateApiKeyInfo();
            _hasUnsavedChanges = true;
            lblConnectionStatus.Text = "API provider changed. Please test the connection.";
            lblConnectionStatus.Foreground = System.Windows.Media.Brushes.Gray;
        }

        private void txtApiKey_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _hasUnsavedChanges = true;
            lblConnectionStatus.Text = "API key changed. Please test the connection.";
            lblConnectionStatus.Foreground = System.Windows.Media.Brushes.Gray;
        }

        private void btnTestConnection_Click(object sender, RoutedEventArgs e)
        {
            TestApiConnection();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_hasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Are you sure you want to close without saving?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                    return;
            }

            this.Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_hasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Are you sure you want to close without saving?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            base.OnClosing(e);
        }
    }
}