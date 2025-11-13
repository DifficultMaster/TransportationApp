using AppClient.Data;
using AppClient.UI.Windows;
using BD4Client.Network;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Reflection;

namespace AppClient.UI
{
    public partial class PasswordChangePage : Page
    {
        private Client client;

        private string oldPassword;
        private string newPassword;

        private bool isPasswordIncorrect = false;
        private List<string> incorrectPasswords;

        private const int minLength = 5;
        private const int maxLength = 100;

        public PasswordChangePage(Client client)
        {
            this.client = client;

            this.oldPassword = string.Empty;
            this.newPassword = string.Empty;

            this.incorrectPasswords = new List<string>();        

            SubscribeEventHandlers();
            InitializeComponent();
        }

        ~PasswordChangePage()
        {
            UnsubscribeEventHandlers();
        }

        private void SubscribeEventHandlers()
        {
            client.OtherFail += FailPasswordChange;
            client.OtherSuccess += PassPasswordChange;
        }

        private void UnsubscribeEventHandlers()
        {
            client.OtherFail -= FailPasswordChange;
            client.OtherSuccess -= PassPasswordChange;
        }

        private void FailPasswordChange(object sender, string message)
        {   
            if (message.ToUpper().Contains("CHANGEPASSWORD"))
            {
                if (message.ToLower().Contains("old"))
                {
                    isPasswordIncorrect = true;
                }
                else if (message.ToLower().Contains("new"))
                {
                    incorrectPasswords.Add(newPassword);
                }
            }

            Textbox_LostFocus(sender, new RoutedEventArgs());
        }

        private void PassPasswordChange(object sender, string message)
        {
            if (message.ToUpper().Contains("CHANGEPASSWORD"))
            {
                try
                {
                    UnsubscribeEventHandlers();
                    try { client.Disconnect(); } catch { }

                    Application.Current.Shutdown();
                }
                catch { }

                
            }            
        }        

        private void Textbox_LostFocus(object sender, RoutedEventArgs e)
        {
            bool isOldPasswordIncorrect = false;
            bool isNewPasswordIncorrect = false;

            string errorMsg = string.Empty;          

            if (OldPasswordTextbox.Password.Length < minLength || NewPasswordTextbox.Password.Length < minLength)
            {
                isOldPasswordIncorrect = true;
                isNewPasswordIncorrect = true;
                errorMsg = $"Довжина паролю має бути більшою за {minLength} символи";
            }
            else if (NewPasswordTextbox.Password.Length > maxLength || OldPasswordTextbox.Password.Length > maxLength)
            {
                isOldPasswordIncorrect = true;
                isNewPasswordIncorrect = true;
                errorMsg = $"Довжина паролю має не перевищувати {maxLength} символів";
            }
            else if (this.isPasswordIncorrect)
            {
                isOldPasswordIncorrect = true;
                errorMsg = $"Старий пароль введено неправильно";
            }
            else if (incorrectPasswords.Contains(NewPasswordTextbox.Password))
            {
                isNewPasswordIncorrect = true;
                errorMsg = $"Новий пароль занадто схожий";
            }

            if (isOldPasswordIncorrect)
            {
                OldPasswordTextbox.SetValue(TextboxStyleHelper.IsTextValidProperty, false);
                ErrorLabel.Content = errorMsg;
                ErrorLabel.Visibility = Visibility.Visible;
            }
            else
            {
                OldPasswordTextbox.SetValue(TextboxStyleHelper.IsTextValidProperty, true);
                ErrorLabel.Content = String.Empty;
                ErrorLabel.Visibility = Visibility.Hidden;
            }

            if (isNewPasswordIncorrect)
            {
                NewPasswordTextbox.SetValue(TextboxStyleHelper.IsTextValidProperty, false);
                ErrorLabel.Content = errorMsg;
                ErrorLabel.Visibility = Visibility.Visible;
            }
            else
            {
                NewPasswordTextbox.SetValue(TextboxStyleHelper.IsTextValidProperty, true);
                ErrorLabel.Content = String.Empty;
                ErrorLabel.Visibility = Visibility.Hidden;
            }

            if (isOldPasswordIncorrect || isNewPasswordIncorrect)
            {
                ConfirmButton.IsEnabled = false;
            }
            else
            {
                ConfirmButton.IsEnabled = true;
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            ConfirmButton.IsEnabled = false;            

            this.oldPassword = OldPasswordTextbox.Password;
            this.newPassword = NewPasswordTextbox.Password;

            client.Transmit($"OTHER~CHANGEPASSWORD~{oldPassword}~{newPassword}~");
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
