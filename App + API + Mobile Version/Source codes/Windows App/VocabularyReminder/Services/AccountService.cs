using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using VocabularyReminder.Models;

namespace VocabularyReminder.Services
{
    class LoginDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class RegisterDto
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class AccountService
    {
        public static async Task<LoginResponseDto> LoginAsync(string username, string password)
        {
            try
            {
                var data = new LoginDto
                {
                    Username = username,
                    Password = password,
                };
                using (var client = new HttpClient())
                {
                    var apiUrl = App.API_URL + "/Authenticate/login";
                    var response = await client.PostAsJsonAsync(apiUrl, data);

                    var result = await response.Content.ReadAsStringAsync();

                    //LoginResponseDto
                    var loginResult = JsonConvert.DeserializeObject<LoginResponseDto>(result);
                    if (loginResult != null && !string.IsNullOrEmpty(loginResult.UserId))
                        return loginResult;

                    MessageBox.Show("Username hoặc mật khẩu không đúng. Vui lòng thử lại.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                MessageBox.Show("Username hoặc mật khẩu không đúng. Vui lòng thử lại.");
            }

            return null;
        }

        public static async Task<ResponseDto> RegisterAsync(RegisterDto data)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var apiUrl = App.API_URL + "/Authenticate/register";
                    var response = await client.PostAsJsonAsync(apiUrl, data);

                    var result = await response.Content.ReadAsStringAsync();

                    //LoginResponseDto
                    var loginResult = JsonConvert.DeserializeObject<ResponseDto>(result);
                    if (loginResult != null)
                        return loginResult;
                }
            }
            catch (Exception)
            {
            }
            return null;
        }


        
    }

}
