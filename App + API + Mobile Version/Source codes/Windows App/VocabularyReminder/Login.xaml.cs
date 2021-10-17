using System.Windows;
using System.Windows.Input;
using VocabularyReminder.Services;

namespace VocabularyReminder
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
        }

        private async System.Threading.Tasks.Task DoLoginAsync()
        {
            this.btn_Login.IsEnabled = false;
            var username = this.inpEmail.Text;
            var password = this.inpPassword.Password;

            var user = await AccountService.LoginAsync(username, password);
            this.btn_Login.IsEnabled = true;
            if (user != null)
            {
                App.User = user;

                var frm = new MainWindow();
                frm.Show();

                Close();
            }
        }

        private async void inpPassword_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await DoLoginAsync();
            }
        }

        private async void btn_Login_Click(object sender, RoutedEventArgs e)
        {
            await DoLoginAsync();
        }

        private void btn_Register_Click(object sender, RoutedEventArgs e)
        {
            var frm = new RegisterForm();
            frm.Show();

            Close();
        }
    }
}
