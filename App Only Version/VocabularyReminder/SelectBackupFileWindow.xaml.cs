using System.Collections.Generic;
using System.Windows;
using Google.Apis.Drive.v3.Data;

namespace VR
{
    public partial class SelectBackupFileWindow : Window
    {
        public File SelectedFile => ComboFiles.SelectedItem as File;

        public SelectBackupFileWindow(IList<File> files)
        {
            InitializeComponent();
            ComboFiles.ItemsSource = files;
            if (files.Count > 0)
                ComboFiles.SelectedIndex = 0;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (ComboFiles.SelectedItem != null)
                DialogResult = true;
        }
    }
}