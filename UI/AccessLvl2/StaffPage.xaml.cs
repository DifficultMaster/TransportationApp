using AppClient.Data;
using AppServer.Models;
using BD4Client.Network;
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
using System.Xml;

namespace AppClient.UI.AccessLvl2
{    
    public partial class StaffPage : Page
    {
        private readonly Client client;
        private Depot depot;

        private bool isDepotRequested;

        public StaffPage(Client client)
        {
            this.client = client;
            this.isDepotRequested = false;

            InitializeComponent();
            SubscribeEventHandlers();
            RequestDepot();

            KeyInformationLabel.Visibility = Visibility.Hidden;
            ContactNumberInformationLabel.Visibility = Visibility.Hidden;
            NotesInformationLabel.Visibility = Visibility.Hidden;
            EditInformationLabel.Visibility = Visibility.Hidden;

            KeyLabel.Visibility = Visibility.Hidden;
            ContactNumberTextbox.Visibility = Visibility.Hidden;
            NotesTextbox.Visibility = Visibility.Hidden;
            EditButton.Visibility = Visibility.Hidden;
        }

        ~StaffPage()
        {
            UnsubscribeEventHandlers();
        }

        private void SubscribeEventHandlers()
        {
            client.Refresh += RefreshGrid;

            client.SelectSuccess += PassSelect;
            client.SelectFail += FailSelect;

            client.EditSuccess += PassEdit;
            client.EditFail += FailEdit;
        }

        private void UnsubscribeEventHandlers()
        {
            client.Refresh -= RefreshGrid;

            client.SelectSuccess -= PassSelect;
            client.SelectFail -= FailSelect;

            client.EditSuccess -= PassEdit;
            client.EditFail -= FailEdit;
        }

        private void RefreshGrid(object sender, string message)
        {
            if (message.ToUpper() == DataService.TableName.DRIVERS.ToString())
            {
                RequestDepot();
            }
        }

        private void PassSelect(object sender, string message)
        {
            if (!isDepotRequested)
            {
                try
                {
                    Depot depot = System.Text.Json.JsonSerializer.Deserialize<Depot>(message);

                    if (depot != null)
                    {
                        DepotLabel.Content = depot.Name;
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
                    isDepotRequested = true;
                    RequestDrivers();
                }
            }
            else
            {
                try
                {
                    using var document = JsonDocument.Parse(message);
                    var root = document.RootElement;

                    var drivers = root.EnumerateArray().Select(d => new
                    {                       
                        PersonId = d.GetProperty("personId").GetRawText(),
                        DepotId = d.GetProperty("depotId").GetRawText(),
                        FirstName = d.GetProperty("firstName").GetString(),
                        SurName = d.GetProperty("surName").GetString(),
                        LastName = d.GetProperty("lastName").GetString(),
                        Position = d.GetProperty("position").GetString(),
                        EmploymentDate = d.GetProperty("employmentDate").GetString(),
                        ContactNumber = d.GetProperty("contactNumber").GetString(),
                        Notes = d.GetProperty("notes").GetString(),
                        Login = d.GetProperty("login").GetString(),
                        HashedPassword = d.GetProperty("hashedPassword").GetString()
                    }).ToList();
                    
                    DriversGrid.ItemsSource = drivers;

                    DriversGrid.Columns[0].Header = "ID особи";
                    DriversGrid.Columns[1].Header = "ID депо";
                    DriversGrid.Columns[2].Header = "Ім'я";
                    DriversGrid.Columns[3].Header = "Прізвище";
                    DriversGrid.Columns[4].Header = "По-батькові";
                    DriversGrid.Columns[5].Header = "Посада";
                    DriversGrid.Columns[6].Header = "Дата прийняття";
                    DriversGrid.Columns[7].Header = "Контактний номер";
                    DriversGrid.Columns[8].Header = "Нотатки";

                    DriversGrid.Columns[0].Visibility = Visibility.Collapsed;
                    DriversGrid.Columns[1].Visibility = Visibility.Collapsed;
                    DriversGrid.Columns[7].Visibility = Visibility.Collapsed;
                    DriversGrid.Columns[8].Visibility = Visibility.Collapsed;
                    DriversGrid.Columns[9].Visibility = Visibility.Hidden;
                    DriversGrid.Columns[10].Visibility = Visibility.Hidden;

                    DriversGrid.Items.Refresh();
                }
                catch (Exception ex)
                {
                    FailSelect(sender, ex.Message);
                }
                finally
                {
                    isDepotRequested = false;
                }
            }
        }

        private void FailSelect(object sender, string message)
        {
            //MessageBox.Show($"Не вдалося відобразити інформацію водіїв депо.\nПомилка: {message}", "Водії", MessageBoxButton.OK, MessageBoxImage.Error);
        }  
        
        private void PassEdit(object sender, string message)
        {
            DriversGrid.SelectedItem = null;
            DriversGrid.SelectedCells.Clear();
            DriversGrid.IsEnabled = true;
            EditButton.IsEnabled = true;
            RequestDepot();
        }

        private void FailEdit(object sender, string message)
        {
            MessageBox.Show($"Не вдалося зберегти зміни.\nПомилка: {message}", "Водії", MessageBoxButton.OK, MessageBoxImage.Error);
            DriversGrid.SelectedItem = null;
            DriversGrid.SelectedCells.Clear();
            DriversGrid.IsEnabled = true;
            EditButton.IsEnabled = true;
            RequestDepot();
        }

        private void RequestDepot()
        {
            client.Transmit($"SELECT~DEPOTS~MYDEPOT~");
        }   

        private void RequestDrivers()
        {
            client.Transmit($"SELECT~DRIVERS~MYDEPOTDRIVERS~");
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataService.GenerateReport($"Перелік водіїв депо '{depot.Name}'", DriversGrid);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не вдалося зберегти звіт.\nПомилка: {ex.Message}", "Водії", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            UnsubscribeEventHandlers();
            Unloaded -= Page_Unloaded;
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
                NotesTextbox.IsEnabled = false;
                EditButton.IsEnabled = false;
                
                if (DriversGrid.SelectedItem is not null)
                {
                    var selectedDriver = DriversGrid.SelectedItem;
                    string id = (selectedDriver as dynamic)?.PersonId;
                    id = id.Replace("\"", "");

                    Person newPerson = new Person
                    {
                        PersonId = id,
                        FirstName = (selectedDriver as dynamic)?.FirstName,
                        SurName = (selectedDriver as dynamic)?.SurName,
                        LastName = (selectedDriver as dynamic)?.LastName,                        
                        ContactNumber = ContactNumberTextbox.Text, 
                        Login = (selectedDriver as dynamic)?.Login,
                        HashedPassword = (selectedDriver as dynamic)?.HashedPassword
                    };

                    string jsonObject = JsonSerializer.Serialize(newPerson);

                    if (id != null)
                    {
                        client.Transmit($"EDIT~UPDATEPERSON~{id}~{jsonObject}~");
                    }
                }
            }
        }

        private void NotesTextbox_LostFocus(object sender, RoutedEventArgs e)
        {
            ContactNumberTextbox.IsEnabled = false;
            NotesTextbox.IsEnabled = false;
            EditButton.IsEnabled = false;

            if (DriversGrid.SelectedItem is not null)
            {
                var selectedDriver = DriversGrid.SelectedItem;
                string id = (selectedDriver as dynamic)?.PersonId;
                id = id.Replace("\"", "");

                Driver newDriver = new Driver
                {
                    PersonId = id,
                    DepotId = (selectedDriver as dynamic)?.DepotId,
                    Position = (selectedDriver as dynamic)?.Position,
                    EmploymentDate = (selectedDriver as dynamic)?.EmploymentDate != null && (selectedDriver as dynamic).EmploymentDate != ""
                        ? DateOnly.Parse((selectedDriver as dynamic).EmploymentDate)
                        : default(DateOnly),
                    Notes = NotesTextbox.Text                    
                };

                string jsonObject = JsonSerializer.Serialize(newDriver);

                if (id != null)
                {
                    client.Transmit($"EDIT~UPDATEDRIVER~{id}~{jsonObject}~");
                }
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
            DriversGrid.IsEnabled = !DriversGrid.IsEnabled;
            ContactNumberTextbox.IsEnabled = !ContactNumberTextbox.IsEnabled;
            NotesTextbox.IsEnabled = !NotesTextbox.IsEnabled;
        }

        private void DriversGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DriversGrid.SelectedItem is not null)
            {
                var selectedDriver = DriversGrid.SelectedItem;

                KeyLabel.Content = (selectedDriver as dynamic)?.PersonId;
                ContactNumberTextbox.Text = (selectedDriver as dynamic)?.ContactNumber;
                NotesTextbox.Text = (selectedDriver as dynamic)?.Notes;

                KeyInformationLabel.Visibility = Visibility.Visible;
                ContactNumberInformationLabel.Visibility = Visibility.Visible;
                NotesInformationLabel.Visibility = Visibility.Visible;
                EditInformationLabel.Visibility = Visibility.Visible;

                KeyLabel.Visibility = Visibility.Visible;
                ContactNumberTextbox.Visibility = Visibility.Visible;
                NotesTextbox.Visibility = Visibility.Visible;
                EditButton.Visibility = Visibility.Visible;
            }
            else
            {
                KeyInformationLabel.Visibility = Visibility.Hidden;
                ContactNumberInformationLabel.Visibility = Visibility.Hidden;
                NotesInformationLabel.Visibility = Visibility.Hidden;
                EditInformationLabel.Visibility = Visibility.Hidden;    

                KeyLabel.Visibility = Visibility.Hidden;
                ContactNumberTextbox.Visibility = Visibility.Hidden;
                NotesTextbox.Visibility = Visibility.Hidden;
                EditButton.Visibility = Visibility.Hidden;
            }
        }
    }
}
