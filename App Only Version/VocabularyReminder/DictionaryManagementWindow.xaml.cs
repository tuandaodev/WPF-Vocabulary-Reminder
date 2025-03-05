using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VR.Domain;
using VR.Domain.Models;

namespace VR
{
    public partial class DictionaryManagementWindow : Window
    {
        private readonly VocaDbContext _dbContext;

        public DictionaryManagementWindow()
        {
            InitializeComponent();
            _dbContext = new VocaDbContext();
            LoadDictionaries();
        }

        private void LoadDictionaries()
        {
            dgDictionaries.ItemsSource = _dbContext.Dictionaries.ToList();
        }

        private void btnAddDictionary_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDictionaryName.Text))
            {
                MessageBox.Show("Please enter a dictionary name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newDictionary = new Dictionary
            {
                Name = txtDictionaryName.Text.Trim(),
                Description = txtDictionaryDescription.Text.Trim(),
                Status = 1
            };

            _dbContext.Dictionaries.Add(newDictionary);
            _dbContext.SaveChanges();

            txtDictionaryName.Clear();
            txtDictionaryDescription.Clear();
            LoadDictionaries();
        }

        private void dgDictionaries_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var dictionary = e.Row.Item as Dictionary;
                if (dictionary != null)
                {
                    _dbContext.SaveChanges();
                    LoadDictionaries();
                }
            }
        }

        private void btnDeleteDictionary_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var dictionary = button.DataContext as Dictionary;

            if (dictionary != null)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete dictionary '{dictionary.Name}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _dbContext.Dictionaries.Remove(dictionary);
                    _dbContext.SaveChanges();
                    LoadDictionaries();
                }
            }
        }
    }
}