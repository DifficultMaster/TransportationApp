using AppClient.Data;
using AppClient.UI.Windows;
using BD4Client.Network;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace AppClient.UI
{
    public partial class LoginPage : Page
    {
        private Client client;

        private string login;
        private string password;

        private List<string> incorrectLogins;
        private List<string> incorrectPasswords;

        private const int minLength = 5;
        private const int maxLength = 100;

        public LoginPage(Client client)
        {
            this.client = client;

            this.login = string.Empty;
            this.password = string.Empty;

            this.incorrectLogins = new List<string>();
            this.incorrectPasswords = new List<string>();        

            SubscribeEventHandlers();
            InitializeComponent();
        }

        ~LoginPage()
        {
            UnsubscribeEventHandlers();
        }

        private void SubscribeEventHandlers()
        {
            client.LoginFail += FailLogin;
            client.LoginSuccess += PassLogin;
        }

        private void UnsubscribeEventHandlers()
        {
            client.LoginFail -= FailLogin;
            client.LoginSuccess -= PassLogin;
        }

        private void FailLogin(object sender, string message)
        {
            IsTerminalModeCheckbox.IsEnabled = true;

            if (message.ToLower().Contains("password"))
            {
                incorrectPasswords.Add(password);
            }
            else if (message.ToLower().Contains("login"))
            {
                incorrectLogins.Add(login);
            }            

            Textbox_LostFocus(sender, new RoutedEventArgs());
        }

        private void PassLogin(object sender, string message)
        {
            DataService.AccessLevel accessLevel = DataService.AccessLevel.VIEWER;

            if (int.TryParse(message, out int intAccessLevel))
            {
                switch (intAccessLevel)
                {
                    case 1:
                        accessLevel = DataService.AccessLevel.DRIVER;
                        break;

                    case 2:
                        accessLevel = DataService.AccessLevel.DISPATCHER;
                        break;

                    case 3:
                        accessLevel = DataService.AccessLevel.ADMIN;
                        break;

                    case 0:
                    default:
                        break;
                }
            }

            if (IsTerminalModeCheckbox.IsChecked == true)
            {
                NavigationService.Navigate(new TerminalPage(client, accessLevel, login, password));                             
            }
            else
            {
                Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                Window.GetWindow(this)?.Close();

                MainWindow mainWindow = new MainWindow(client, accessLevel, login);
                mainWindow.Show();
                Application.Current.MainWindow = mainWindow;
            }
        }        

        private void Textbox_LostFocus(object sender, RoutedEventArgs e)
        {
            bool isLoginIncorrect = false;
            bool isPasswordIncorrect = false;

            string errorMsg = string.Empty;          

            if (PasswordTextbox.Password.Length < minLength)
            {
                isPasswordIncorrect = true;
                errorMsg = $"Довжина паролю має бути більшою за {minLength} символи";
            }
            else if (LoginTextbox.Text.Length > maxLength)
            {
                isPasswordIncorrect = true;
                errorMsg = $"Довжина паролю має не перевищувати {maxLength} символів";
            }
            else if (incorrectPasswords.Contains(PasswordTextbox.Password))
            {
                isPasswordIncorrect = true;
                errorMsg = $"Пароль невірний";
            }

            if (LoginTextbox.Text.Length < minLength)
            {
                isLoginIncorrect = true;
                errorMsg = $"Довжина логіну має бути більшою за {minLength} символи";
            }
            else if (LoginTextbox.Text.Length > maxLength)
            {
                isLoginIncorrect = true;
                errorMsg = $"Довжина логіну має не перевищувати {maxLength} символів";
            }
            else if (incorrectLogins.Contains(LoginTextbox.Text))
            {
                isLoginIncorrect = true;
                errorMsg = $"Логін невірний";
            }

            if (isLoginIncorrect)
            {
                LoginTextbox.SetValue(TextboxStyleHelper.IsTextValidProperty, false);
                ErrorLabel.Content = errorMsg;
                ErrorLabel.Visibility = Visibility.Visible;
            }
            else
            {
                LoginTextbox.SetValue(TextboxStyleHelper.IsTextValidProperty, true);
                ErrorLabel.Content = string.Empty;
                ErrorLabel.Visibility = Visibility.Hidden;
            }            

            if (isPasswordIncorrect)
            {
                PasswordTextbox.SetValue(TextboxStyleHelper.IsTextValidProperty, false);
                ErrorLabel.Content = errorMsg;
                ErrorLabel.Visibility = Visibility.Visible;
            }
            else
            {
                PasswordTextbox.SetValue(TextboxStyleHelper.IsTextValidProperty, true);
                ErrorLabel.Content = string.Empty;
                ErrorLabel.Visibility = Visibility.Hidden;
            }       
            
            if (isLoginIncorrect || isPasswordIncorrect)
            {
                LoginButton.IsEnabled = false;
            }
            else
            {
                LoginButton.IsEnabled = true;
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginButton.IsEnabled = false;
            IsTerminalModeCheckbox.IsEnabled = false;

            this.login = LoginTextbox.Text;
            this.password = PasswordTextbox.Password;           

            client.Transmit($"LOGIN~{login}~{password}~");
        }

        private void KeyboardButton_Click(object sender, RoutedEventArgs e)
        {
            VirtualKeyboard.ShowTouchKeyboard();
        }

        private void Image_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
            Textbox_LostFocus(sender, e);
        }
    }
}
