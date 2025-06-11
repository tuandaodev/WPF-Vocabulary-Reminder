using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VR.Domain.Models;
using VR.Services;

namespace VR
{
    /// <summary>
    /// Interaction logic for DictionarySelectionWindow.xaml
    /// </summary>
    public partial class DictionarySelectionWindow : Window
    {
        private Vocabulary _vocabulary;
        private string _currentDictionaryName;
        public Dictionary SelectedDictionary { get; private set; }

        public DictionarySelectionWindow(Vocabulary vocabulary, string currentDictionaryName, List<Dictionary> availableDictionaries)
        {
            InitializeComponent();
            
            _vocabulary = vocabulary ?? throw new ArgumentNullException(nameof(vocabulary));
            _currentDictionaryName = currentDictionaryName ?? "Unknown";
            
            InitializeWindow(availableDictionaries);
        }

        private void InitializeWindow(List<Dictionary> availableDictionaries)
        {
            // Set window properties
            this.Owner = Application.Current.MainWindow;
            
            // Update UI with vocabulary information
            TextBlock_WordName.Text = _vocabulary.Word;
            TextBlock_CurrentDictionary.Text = _currentDictionaryName;
            
            // Update subtitle with vocabulary name
            TextBlock_Subtitle.Text = $"Choose a dictionary to move '{_vocabulary.Word}' to:";
            
            // Populate dictionary list
            if (availableDictionaries != null && availableDictionaries.Any())
            {
                ListBox_Dictionaries.ItemsSource = availableDictionaries;
                
                // Select first item by default
                if (availableDictionaries.Count > 0)
                {
                    ListBox_Dictionaries.SelectedIndex = 0;
                }
            }
            else
            {
                // No dictionaries available
                ListBox_Dictionaries.ItemsSource = new List<Dictionary>();
                Btn_Move.IsEnabled = false;
                
                var noDictMessage = new TextBlock
                {
                    Text = "No available dictionaries to move to.",
                    Foreground = System.Windows.Media.Brushes.Orange,
                    FontStyle = FontStyles.Italic,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                
                ListBox_Dictionaries.Items.Add(noDictMessage);
            }
            
            // Set focus to the list
            ListBox_Dictionaries.Focus();
        }

        private void Btn_Move_Click(object sender, RoutedEventArgs e)
        {
            var selectedDict = ListBox_Dictionaries.SelectedItem as Dictionary;
            
            if (selectedDict == null)
            {
                MessageBox.Show(
                    "Please select a dictionary from the list.",
                    "No Dictionary Selected",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
            
            // Confirm the move
            var result = MessageBox.Show(
                $"Move '{_vocabulary.Word}' from '{_currentDictionaryName}' to '{selectedDict.Name}' dictionary?",
                "Confirm Move",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                SelectedDictionary = selectedDict;
                DialogResult = true;
                Close();
            }
        }

        private void Btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            SelectedDictionary = null;
            DialogResult = false;
            Close();
        }

        private void ListBox_Dictionaries_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Double-click to move
            if (ListBox_Dictionaries.SelectedItem is Dictionary)
            {
                Btn_Move_Click(sender, null);
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.Enter:
                    if (Btn_Move.IsEnabled)
                        Btn_Move_Click(sender, null);
                    break;
                case System.Windows.Input.Key.Escape:
                    Btn_Cancel_Click(sender, null);
                    break;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Ensure window is properly positioned and focused
            this.Activate();
            this.Focus();
            
            // Set keyboard event handler
            this.KeyDown += Window_KeyDown;
            
            // Add double-click handler to ListBox
            ListBox_Dictionaries.MouseDoubleClick += ListBox_Dictionaries_MouseDoubleClick;
        }
    }
}