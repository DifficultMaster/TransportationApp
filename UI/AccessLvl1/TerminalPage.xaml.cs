using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Resources;
using System.Windows.Shapes;
using System.Windows.Threading;
using AppClient.Data;
using AppServer.Models;
using BD4Client.Network;

namespace AppClient.UI
{
    public partial class TerminalPage : Page
    {
        public enum TerminalState
        {
            DEFAULT,
            SUCCESS,
            FAILURE,
            EXIT
        };

        private Client client;
        private decimal price;

        public TerminalState state { get; private set; }

        private bool isLogoutFormDisplayed;

        private readonly string password;
        private readonly SoundPlayer successPlayer;
        private readonly SoundPlayer failurePlayer;
        private readonly DispatcherTimer timer;

        private readonly BitmapImage successBitmap;
        private readonly BitmapImage failBitmap;
        private readonly BitmapImage terminalBitmap;

        public TerminalPage(Client client, DataService.AccessLevel accessLevel, string login, string password)
        {
            this.client = client;
            this.price = 0;
            this.state = TerminalState.DEFAULT;
            this.isLogoutFormDisplayed = false;

            this.password = password;

            StreamResourceInfo successStream = Application.GetResourceStream(new Uri(@"pack://application:,,,/UI/Resources/Audio/successPing.wav"));
            this.successPlayer = new SoundPlayer(successStream.Stream);
            this.successPlayer.Load();

            StreamResourceInfo failStream = Application.GetResourceStream(new Uri(@"pack://application:,,,/UI/Resources/Audio/failPing.wav"));
            this.failurePlayer = new SoundPlayer(failStream.Stream);
            this.failurePlayer.Load();

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            timer.Tick += Timer_Tick;

            this.successBitmap = new BitmapImage();
            this.failBitmap = new BitmapImage();
            this.terminalBitmap = new BitmapImage();
             
            successBitmap.BeginInit();
            successBitmap.UriSource = new Uri("pack://application:,,,/UI/Resources/Images/Backgrounds/success.png", UriKind.Absolute);
            successBitmap.EndInit();

            failBitmap.BeginInit();
            failBitmap.UriSource = new Uri("pack://application:,,,/UI/Resources/Images/Backgrounds/fail.png", UriKind.Absolute);
            failBitmap.EndInit();

            terminalBitmap.BeginInit();
            terminalBitmap.UriSource = new Uri("pack://application:,,,/UI/Resources/Images/Backgrounds/terminal.png", UriKind.Absolute);
            terminalBitmap.EndInit();

            SubscribeEventHandlers();
            RequestPrice();

            InitializeComponent();           
        }

        ~TerminalPage()
        {
            UnsubscribeEventHandlers();
        }

        private void SubscribeEventHandlers()
        {
            client.EditSuccess += PassValidation;
            client.EditFail += FailValidation;

            client.SelectSuccess += PassPrice;
            client.SelectFail += FailPrice;         
        }

        private void UnsubscribeEventHandlers()
        {
            client.EditSuccess -= PassValidation;
            client.EditFail -= FailValidation;

            client.SelectSuccess -= PassPrice;
            client.SelectFail -= FailPrice;
        }

        private void FailPrice(object sender, string message)
        {
            MessageSecondaryLabel.Visibility = Visibility.Hidden;
        }

        private void PassPrice(object sender, string message)
        {
            try
            {          
                Route route = System.Text.Json.JsonSerializer.Deserialize<Route>(message, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true              
                });

                if (route != null)
                {
                    price = route.Price;

                    MessageSecondaryLabel.Content = $"До сплати {Math.Round(price, 2)} грн";
                    MessageSecondaryLabel.Visibility = Visibility.Visible;
                }
                else
                {
                    FailPrice(sender, message);
                }
            }
            catch (Exception)
            {
                FailPrice(sender, message);
            }
        }
        
        public async void FailValidation(object sender, string message)
        {
            await DisplayFailValidation();
        }

        public async void PassValidation(object sender, string message)
        {
            await DisplayPassValidation();
        }

        private void DisplayDefault()
        {
            state = TerminalState.DEFAULT;

            ExitButton.Visibility = Visibility.Visible;
            this.Background = (Brush)FindResource("PrimaryDarkBlue");

            BackgroundImage.Source = terminalBitmap;

            MessagePrimaryLabel.Visibility = Visibility.Visible;
            MessageSecondaryLabel.Visibility = Visibility.Visible;

            MessageTertiaryLabel.Content = "";
            MessageTertiaryLabel.Visibility = Visibility.Hidden;
        }


        private async Task DisplayFailValidation()
        {
            state = TerminalState.FAILURE;

            ExitButton.Visibility = Visibility.Hidden;
            this.Background = (Brush)FindResource("InvalidState");

            BackgroundImage.Source = failBitmap;

            MessagePrimaryLabel.Visibility = Visibility.Hidden;
            MessageSecondaryLabel.Visibility = Visibility.Hidden;
            
            MessageTertiaryLabel.Content = "ОПЛАТА НЕУСПІШНА";
            MessageTertiaryLabel.Visibility = Visibility.Visible;
         
            failurePlayer.Play();

            await Task.Delay(3000);
            DisplayDefault();
        }
       
        private async Task DisplayPassValidation()
        {
            state = TerminalState.SUCCESS;

            ExitButton.Visibility = Visibility.Hidden;
            this.Background = (Brush)FindResource("ValidState");
                   
            BackgroundImage.Source = successBitmap;

            MessagePrimaryLabel.Visibility = Visibility.Hidden;
            MessageSecondaryLabel.Visibility = Visibility.Hidden;

            MessageTertiaryLabel.Content = "ОПЛАТА УСПІШНА";
            MessageTertiaryLabel.Visibility = Visibility.Visible;
        
            successPlayer.Play();

            await Task.Delay(3000);
            DisplayDefault();
        }

        private void RequestPrice()
        {
            client.Transmit($"SELECT~{DataService.TableName.ROUTES.ToString()}~MYROUTE~");
        }

        public void RequestValidation()
        {
            client.Transmit($"EDIT~VALIDATE~");
        }

        private void DisplayLogoutForm()
        {
            PasswordTextbox.Visibility = Visibility.Visible;
            LogoutButton.Visibility = Visibility.Visible;
            ExitButton.Visibility = Visibility.Hidden;
            isLogoutFormDisplayed = true;           
        }

        private void UndoLogoutForm()
        {
            PasswordTextbox.Visibility = Visibility.Collapsed;
            LogoutButton.Visibility = Visibility.Collapsed;
            ExitButton.Visibility = Visibility.Visible;
            isLogoutFormDisplayed = false;          
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            if (state == TerminalState.EXIT)
            {
                state = TerminalState.DEFAULT;
                UndoLogoutForm();
            }
            else
            {
                state = TerminalState.EXIT;
                DisplayLogoutForm();
            }
        }

        //for testing purposes
        private void TerminalPage_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (state == TerminalState.DEFAULT)
            {
                // S to pass validation
                if (e.Key == Key.S)
                {
                    RequestValidation();
                    e.Handled = true;
                }
                // F to fail validation
                else if (e.Key == Key.F)
                {
                    FailValidation(sender, string.Empty);
                    e.Handled = true;
                }
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (PasswordTextbox.Password == password)
            {
                client.Transmit($"LOGOUT~");
                client.Disconnect();
                Application.Current.Shutdown();
            }
            else
            {
                UndoLogoutForm();
                state = TerminalState.DEFAULT;
            }
        }

        private void PasswordTextbox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (isLogoutFormDisplayed)
            {
                timer.Stop(); 
                timer.Start();
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {      
            timer.Stop();
            UndoLogoutForm();
            state = TerminalState.DEFAULT;
        }
    }
}
