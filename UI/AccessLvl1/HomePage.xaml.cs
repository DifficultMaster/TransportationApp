using AppClient.Data;
using AppServer.Models;
using BD4Client.Network;
using iText.Kernel.Pdf.Canvas.Parser.ClipperLib;
using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace AppClient.UI.AccessLvl1
{  
    public partial class HomePage : Page
    {
        private readonly Client client;
        private DispatcherTimer timer;
        private readonly DataService.AccessLevel accessLevel;

        private bool isPersonRequested;

        public HomePage(Client client, DataService.AccessLevel accessLevel)
        {
            this.client = client;
            this.accessLevel = accessLevel;
            this.isPersonRequested = false;

            InitializeComponent();
            SubscribeEventHandlers();
            RequestPerson();            

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += Timer_Tick;
            timer.Start();
            UpdateTime();
        }
        ~HomePage()
        {
            UnsubscribeEventHandlers();
        }

        private void SubscribeEventHandlers()
        {
            client.Refresh += Refresh;

            client.SelectSuccess += PassSelect;
            client.SelectFail += FailSelect;

            client.EditSuccess += PassEdit;
            client.EditFail += FailEdit;
        }

        private void UnsubscribeEventHandlers()
        {
            client.Refresh -= Refresh;

            client.SelectSuccess -= PassSelect;
            client.SelectFail -= FailSelect;

            client.EditSuccess -= PassEdit;
            client.EditFail -= FailEdit;
        }

        private void Refresh(object sender, string message)
        {
            Enum.TryParse(message.ToUpper(), out DataService.TableName tableName);

            switch (tableName)
            {
                case DataService.TableName.PERSONS:
                    {
                        RequestPerson();
                    }
                    break;

                default:
                    break;
            }            
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateTime();
        }

        private void UpdateTime()
        {
            LivetimeLabel.Content = DateTime.Now.ToString("HH:mm");            
        }

        private string GetAccessLevelString(DataService.AccessLevel accessLevel)
        {
            switch (accessLevel)
            {
                case DataService.AccessLevel.DRIVER:
                    return "Водій";

                case DataService.AccessLevel.DISPATCHER:
                    return "Диспетчер";

                case DataService.AccessLevel.ADMIN:
                    return "Адміністратор";

                default:
                    return "";
            }
        }

        private void PassSelect(object sender, string message)
        {
            if (!isPersonRequested)
            {
                try
                {
                    Person person = System.Text.Json.JsonSerializer.Deserialize<Person>(message);

                    if (person != null)
                    {
                        WelcomeLabel.Content = $"{person.FirstName}!";
                        AccessLevelLabel.Content = GetAccessLevelString(accessLevel);

                        FirstnameLabel.Content = person.FirstName;
                        SurnameLabel.Content = person.SurName;
                        LastnameLabel.Content = person.LastName;

                        ContactNumberTextbox.Text = person.ContactNumber;
                    }
                    else
                    {
                        FailSelect(sender, message);
                    }
                }
                catch (Exception)
                {
                    FailSelect(sender, message);
                }
                finally
                {
                    isPersonRequested = true;
                    RequestDepot();
                }
            }
            else
            {
                try
                {
                    Depot depot = System.Text.Json.JsonSerializer.Deserialize<Depot>(message);

                    if (depot != null)
                    {
                        NameLabel.Content = depot.Name;
                        AddressLabel.Content = depot.Address;

                        TypeLabel.Content = depot.Type;
                        OperatorLabel.Content = depot.Operator;
                        ContactNumberLabel.Content = depot.ContactNumber;                        

                        DepotGrid.Visibility = Visibility.Visible;
                        if (AddressLabel.Content == null)
                        {
                            AddressInformationLabel.Visibility = Visibility.Collapsed;
                            AddressLabel.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            AddressInformationLabel.Visibility = Visibility.Visible;
                            AddressLabel.Visibility = Visibility.Visible;
                        }

                        if (ContactNumberLabel.Content == null)
                        {
                            ContactNumberInformationLabel.Visibility = Visibility.Collapsed;
                            ContactNumberLabel.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            ContactNumberInformationLabel.Visibility = Visibility.Visible;
                            ContactNumberLabel.Visibility = Visibility.Visible;
                        }
                    }
                    else
                    {
                        FailSelect(sender, message);
                    }
                }
                catch (Exception)
                {
                    FailSelect(sender, message);
                }
                finally
                {
                    isPersonRequested = false;
                }
            }
        }

        private void FailSelect(object sender, string message)
        {
            //MessageBox.Show($"Не вдалося відобразити інформацію користувача.\nПомилка: {message}", "Головна", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void PassEdit(object sender, string message)
        {
            ContactNumberTextbox.IsEnabled = false;
            EditButton.IsEnabled = true;
            ContactNumberTextbox.SetValue(TextboxStyleHelper.IsTextValidProperty, true);
        }

        private void FailEdit(object sender, string message)
        {
            ContactNumberTextbox.IsEnabled = true;
            EditButton.IsEnabled = true;
            ContactNumberTextbox.SetValue(TextboxStyleHelper.IsTextValidProperty, false);
        }

        private void RequestPerson()
        {
            client.Transmit($"SELECT~{DataService.TableName.PERSONS.ToString()}~MYINFO~");                
        }

        private void RequestDepot()
        {
            if (accessLevel == DataService.AccessLevel.DRIVER || accessLevel == DataService.AccessLevel.DISPATCHER)
            {
                client.Transmit($"SELECT~{DataService.TableName.DEPOTS.ToString()}~MYDEPOT~");                
            }
            else
                isPersonRequested = false;
        }

        private void ContactNumberTextbox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (ContactNumberTextbox.Text.Length != 13)
            {
                ContactNumberTextbox.SetValue(TextboxStyleHelper.IsTextValidProperty, false);
            }
            else
            {
                ContactNumberTextbox.SetValue(TextboxStyleHelper.IsTextValidProperty, true);
                ContactNumberTextbox.IsEnabled = false;
                EditButton.IsEnabled = false;
                client.Transmit($"EDIT~MYCONTACT~{ContactNumberTextbox.Text}~");                
            }
        }

        private void ContactNumberTextbox_KeyDown(object sender, KeyEventArgs e)
        {
            bool isNumber = (e.Key >= Key.D0 && e.Key <= Key.D9) ||
                            (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9);
            bool isAllowedControl = e.Key == Key.OemPlus || e.Key == Key.Back || e.Key == Key.Delete ||
                                   e.Key == Key.Left || e.Key == Key.Right ||
                                   e.Key == Key.Tab;

            if (!isNumber && !isAllowedControl)
            {
                e.Handled = true;
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            ContactNumberTextbox.IsEnabled = !ContactNumberTextbox.IsEnabled;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            UnsubscribeEventHandlers();
            Unloaded -= Page_Unloaded;
        }
    }
}
