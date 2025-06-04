using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace VR
{
    public partial class ContentDisplayWindow : Window
    {
        private string _rawContent;

        public ContentDisplayWindow()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        public ContentDisplayWindow(string title, string content) : this()
        {
            TitleText.Text = title;
            _rawContent = content;
            DisplayContent(content);
        }

        private void DisplayContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return;

            // Use MdXaml to render markdown content
            ContentDisplay.Markdown = content;
        }

    }
}