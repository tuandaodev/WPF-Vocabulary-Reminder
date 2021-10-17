using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using VocabularyReminder.Services;

namespace VocabularyReminder
{
    /// <summary>
    /// Interaction logic for RegisterForm.xaml
    /// </summary>
    public partial class RegisterForm : Window
    {
        public RegisterForm()
        {
            InitializeComponent();
        }

        private void btn_Login_Click(object sender, RoutedEventArgs e)
        {
            OpenLogin();
        }

        private void OpenLogin()
        {
            var frm = new Login();
            frm.Show();
            Close();
        }

        private async void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (inp_Email.Text.Length == 0)
            {
                labelErrorMessage.Text = "Enter an email.";
                inp_Email.Focus();
            }
            else if (!Regex.IsMatch(inp_Email.Text, @"^[a-zA-Z][\w\.-]*[a-zA-Z0-9]@[a-zA-Z0-9][\w\.-]*[a-zA-Z0-9]\.[a-zA-Z][a-zA-Z\.]*[a-zA-Z]$"))
            {
                labelErrorMessage.Text = "Enter a valid email.";
                inp_Email.Select(0, inp_Email.Text.Length);
                inp_Email.Focus();
            }
            else
            {
                if (inp_Password.Password.Length == 0)
                {
                   labelErrorMessage.Text = "Enter password.";
                    inp_Password.Focus();
                }
                else if (inp_PasswordConfirm.Password.Length == 0)
                {
                   labelErrorMessage.Text = "Enter Confirm password.";
                    inp_PasswordConfirm.Focus();
                }
                else if (inp_Password.Password != inp_PasswordConfirm.Password)
                {
                   labelErrorMessage.Text = "Confirm password must be same as password.";
                    inp_PasswordConfirm.Focus();
                }
                else
                {
                    string userName = inp_Username.Text;
                    string email = inp_Email.Text;
                    string password = inp_Password.Password;

                    var data = new RegisterDto
                    {
                        Username = userName,
                        Email = email,
                        Password = password
                    };
                    await DoRegisterAsync(data);
                }
            }
        }

        private void btn_Reset_Click(object sender, RoutedEventArgs e)
        {
            inp_Username.Text = "";
            inp_Email.Text = "";
            inp_Password.Password = "";
            inp_PasswordConfirm.Password = "";
        }

        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            OpenLogin();
        }

        private async Task DoRegisterAsync(RegisterDto dto)
        {
            this.btnSubmit.IsEnabled = false;
            var result = await AccountService.RegisterAsync(dto);
            this.btnSubmit.IsEnabled = true;
            if (result != null)
            {
                if (result.Status == "Success")
                    MessageBox.Show("Đăng ký thành công. Vui lòng đăng nhập.");
                else
                    MessageBox.Show(result.Message);
            } else
            {
                MessageBox.Show("Đăng ký thất bại. Vui lòng thử lại.");
            }
        }
    }
}
