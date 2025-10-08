using AppClient.Data;
using AppServer.Models;
using BD4Client.Network;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Globalization;
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
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;

namespace AppClient.UI.AccessLvl3
{
    public partial class StaffPage : Page
    {
        private readonly Client client;
        private int tableRequestNumber;
        private DataService.TableName focusedTable;
        private DataGridCellInfo? _lastSelectedCell;

        private bool firstKeyReady;
        private bool secondKeyReady;
        private bool tertiaryKeyReady;

        public StaffPage(Client client)
        {
            this.client = client;
            this.tableRequestNumber = 0;
            this.focusedTable = DataService.TableName.NONE;

            this.firstKeyReady = false;
            this.secondKeyReady = false;
            this.tertiaryKeyReady = false;

            InitializeComponent();
            SubscribeEventHandlers();
            RequestDispatchers();

            KeyInformationLabel.Visibility = Visibility.Hidden;
            TableInformationLabel.Visibility = Visibility.Hidden;
            EditInformationLabel.Visibility = Visibility.Hidden;

            KeyLabel.Visibility = Visibility.Hidden;
            TableLabel.Visibility = Visibility.Hidden;
            EditButton.Visibility = Visibility.Hidden;
            AddButton.Visibility = Visibility.Hidden;
            RemoveButton.Visibility = Visibility.Hidden;
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
            if (message.ToUpper() == DataService.TableName.DISPATCHERS.ToString() 
                || message.ToUpper() == DataService.TableName.DRIVERS.ToString() 
                || message.ToUpper() == DataService.TableName.PERSONS.ToString())
            {
                RequestDispatchers();
            }
        }

        private void PassSelect(object sender, string message)
        {
            if (tableRequestNumber == 0)
            {
                try
                {
                    List<Dispatcher> Dispatchers = System.Text.Json.JsonSerializer.Deserialize<List<Dispatcher>>(message);

                    if (Dispatchers != null)
                    {
                        DispatchersGrid.ItemsSource = Dispatchers;

                        DispatchersGrid.Columns[0].Header = "ID";
                        DispatchersGrid.Columns[1].Header = "ID депо";
                        DispatchersGrid.Columns[2].Header = "Посада";
                        DispatchersGrid.Columns[3].Header = "Початок роботи";
                        DispatchersGrid.Columns[4].Header = "Нотатки";

                        if (DispatchersGrid.Columns[3] is DataGridTextColumn employmentDateColumn)
                        {
                            employmentDateColumn.Binding = new Binding("EmploymentDate")
                            {
                                StringFormat = "dd.MM.yyyy",
                                ConverterCulture = new CultureInfo("uk-UA")
                            };
                        }

                        DispatchersGrid.Columns[5].Visibility = Visibility.Hidden;
                        DispatchersGrid.Columns[6].Visibility = Visibility.Hidden;                     

                        DispatchersGrid.Columns[0].IsReadOnly = true;
                        DispatchersGrid.Columns[1].IsReadOnly = true;                     

                        DispatchersGrid.Items.Refresh();
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
                    tableRequestNumber++;
                    RequestDrivers();
                }
            }
            else if (tableRequestNumber == 1)
            {
                try
                {
                    List<Driver> Drivers = System.Text.Json.JsonSerializer.Deserialize<List<Driver>>(message);

                    if (Drivers != null)
                    {
                        DriversGrid.ItemsSource = Drivers;

                        DriversGrid.Columns[0].Header = "ID";
                        DriversGrid.Columns[1].Header = "ID депо";
                        DriversGrid.Columns[2].Header = "Посада";
                        DriversGrid.Columns[3].Header = "Початок роботи";
                        DriversGrid.Columns[4].Header = "Нотатки";

                        if (DriversGrid.Columns[3] is DataGridTextColumn employmentDateColumn)
                        {
                            employmentDateColumn.Binding = new Binding("EmploymentDate")
                            {
                                StringFormat = "dd.MM.yyyy",
                                ConverterCulture = new CultureInfo("uk-UA")
                            };
                        }

                        DriversGrid.Columns[5].Visibility = Visibility.Hidden;
                        DriversGrid.Columns[6].Visibility = Visibility.Hidden;
                        DriversGrid.Columns[7].Visibility = Visibility.Hidden;

                        DriversGrid.Columns[0].IsReadOnly = true;
                        DriversGrid.Columns[1].IsReadOnly = true;

                        DispatchersGrid.Items.Refresh();
                        DriversGrid.Items.Refresh();
                    }
                    else
                    {
                        FailSelect(sender, message);
                    }
                }
                catch (Exception ex)
                {
                    FailSelect(sender, ex.Message);
                }
                finally
                {
                    tableRequestNumber++;
                    RequestPersons();
                }
            }
            else if (tableRequestNumber == 2)
            {
                try
                {
                    List<Person> Persons = System.Text.Json.JsonSerializer.Deserialize<List<Person>>(message);

                    if (Persons != null)
                    {
                        PersonsGrid.ItemsSource = Persons;

                        PersonsGrid.Columns[0].Header = "ID";
                        PersonsGrid.Columns[1].Header = "Ім'я";
                        PersonsGrid.Columns[2].Header = "Прізвище";
                        PersonsGrid.Columns[3].Header = "По-батькові";
                        PersonsGrid.Columns[4].Header = "Контактний номер";
                        PersonsGrid.Columns[5].Header = "Логін";
                        PersonsGrid.Columns[6].Header = "Пароль (хеш)";

                        PersonsGrid.Columns[7].Visibility = Visibility.Hidden;
                        PersonsGrid.Columns[8].Visibility = Visibility.Hidden;
                        PersonsGrid.Columns[9].Visibility = Visibility.Hidden;

                        PersonsGrid.Columns[0].IsReadOnly = true;

                        DispatchersGrid.Items.Refresh();
                        DriversGrid.Items.Refresh();
                        PersonsGrid.Items.Refresh();
                    }
                    else
                    {
                        FailSelect(sender, message);
                    }
                }
                catch (Exception ex)
                {
                    FailSelect(sender, ex.Message);
                }
                finally
                {
                    tableRequestNumber = 0;
                }
            }
            else
                tableRequestNumber = 0;
        }

        private void FailSelect(object sender, string message)
        {
            //MessageBox.Show($"Не вдалося відобразити інформацію.\nПомилка: {message}", "Персонал", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void PassEdit(object sender, string message)
        {
            DispatchersGrid.SelectedItem = null;
            DispatchersGrid.SelectedCells.Clear();
            DispatchersGrid.IsEnabled = true;

            DriversGrid.SelectedItem = null;
            DriversGrid.SelectedCells.Clear();
            DriversGrid.IsEnabled = true;

            PersonsGrid.SelectedItem = null;
            PersonsGrid.SelectedCells.Clear();
            PersonsGrid.IsEnabled = true;

            EditButton.IsEnabled = true;
            AddButton.IsEnabled = true;
            RemoveButton.IsEnabled = true;

            EditButton_Click(sender, new RoutedEventArgs());
            RequestDispatchers();
        }

        private void FailEdit(object sender, string message)
        {
            MessageBox.Show($"Не вдалося зберегти зміни.\nПомилка: {message}", "Персонал", MessageBoxButton.OK, MessageBoxImage.Error);

            DispatchersGrid.SelectedItem = null;
            DispatchersGrid.SelectedCells.Clear();
            DispatchersGrid.IsEnabled = true;

            DriversGrid.SelectedItem = null;
            DriversGrid.SelectedCells.Clear();
            DriversGrid.IsEnabled = true;

            EditButton.IsEnabled = true;
            AddButton.IsEnabled = true;
            RemoveButton.IsEnabled = true;

            EditButton_Click(sender, new RoutedEventArgs());
            RequestDispatchers();
        }

        private void RequestDispatchers()
        {
            client.Transmit($"SELECT~DISPATCHERS~ALLDISPATCHERS~");
        }

        private void RequestDrivers()
        {
            client.Transmit($"SELECT~DRIVERS~ALLDRIVERS~");
        }

        private void RequestPersons()
        {
            client.Transmit($"SELECT~PERSONS~ALLPERSONS~");
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                switch (focusedTable)
                {
                    case DataService.TableName.DISPATCHERS:
                        {
                            DataService.GenerateReport($"Перелік всіх диспетчерів", DispatchersGrid);
                            break;
                        }

                    case DataService.TableName.DRIVERS:
                        {
                            DataService.GenerateReport($"Перелік всіх водіїв", DriversGrid);
                            break;
                        }
                        
                    case DataService.TableName.PERSONS:
                        {
                            DataService.GenerateReport($"Перелік всіх співробітників", PersonsGrid);
                            break;
                        }

                    default:
                        {
                            throw new Exception("Необроблений тип таблиці.");
                        }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не вдалося зберегти звіт.\nПомилка: {ex.Message}", "Персонал", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            UnsubscribeEventHandlers();
            Unloaded -= Page_Unloaded;
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            DispatchersGrid.IsReadOnly = !DispatchersGrid.IsReadOnly;
            DriversGrid.IsReadOnly = !DriversGrid.IsReadOnly;
            PersonsGrid.IsReadOnly = !PersonsGrid.IsReadOnly;

            AddButton.IsEnabled = !AddButton.IsEnabled;
            RemoveButton.IsEnabled = !RemoveButton.IsEnabled;

            AddPopupGrid.Visibility = Visibility.Collapsed;
            SecondaryKeyTextbox.Text = string.Empty;
            PrimaryKeyTextbox.Text = string.Empty;

            RemovePopupGrid.Visibility = Visibility.Collapsed;
            ConfirmButton.Visibility = Visibility.Collapsed;
        }

        private void DispatchersGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            var currentCell = DispatchersGrid.CurrentCell;

            if (currentCell != null && currentCell.Item != null && currentCell.Item != DependencyProperty.UnsetValue)
            {
                if (_lastSelectedCell.HasValue &&
                    _lastSelectedCell.Value.Item == currentCell.Item &&
                    _lastSelectedCell.Value.Column == currentCell.Column)
                {
                    return;
                }

                _lastSelectedCell = currentCell;

                DispatchersGrid.SelectedItem = null;
                DispatchersGrid.SelectedCells.Clear();
                DispatchersGrid.IsEnabled = true;
                focusedTable = DataService.TableName.DISPATCHERS;

                if (!DispatchersGrid.IsReadOnly)
                {
                    EditButton_Click(sender, new RoutedEventArgs());
                }

                Dispatcher selectedDispatcher = (Dispatcher)currentCell.Item;

                KeyLabel.Content = selectedDispatcher.PersonId;
                TableLabel.Content = DataService.GetTableNameToUaString(focusedTable);

                KeyInformationLabel.Visibility = Visibility.Visible;
                TableInformationLabel.Visibility = Visibility.Visible;
                KeyLabel.Visibility = Visibility.Visible;
                TableLabel.Visibility = Visibility.Visible;
                EditInformationLabel.Visibility = Visibility.Visible;
                EditButton.Visibility = Visibility.Visible;
                AddButton.Visibility = Visibility.Visible;
                RemoveButton.Visibility = Visibility.Visible;

                AddPopupGrid.IsEnabled = false;
                RemovePopupGrid.IsEnabled = false;

                AddPopupGrid.Visibility = Visibility.Collapsed;
                RemovePopupGrid.Visibility = Visibility.Collapsed;
                ConfirmButton.Visibility = Visibility.Collapsed;
            }
        }

        private void DriversGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            var currentCell = DriversGrid.CurrentCell;

            if (currentCell != null && currentCell.Item != null && currentCell.Item != DependencyProperty.UnsetValue)
            {
                if (_lastSelectedCell.HasValue &&
                    _lastSelectedCell.Value.Item == currentCell.Item &&
                    _lastSelectedCell.Value.Column == currentCell.Column)
                {
                    return;
                }

                _lastSelectedCell = currentCell;

                DriversGrid.SelectedItem = null;
                DriversGrid.SelectedCells.Clear();
                DriversGrid.IsEnabled = true;
                focusedTable = DataService.TableName.DRIVERS;

                if (!DriversGrid.IsReadOnly)
                {
                    EditButton_Click(sender, new RoutedEventArgs());
                }

                Driver selectedDriver = (Driver)currentCell.Item;

                KeyLabel.Content = selectedDriver.PersonId;
                TableLabel.Content = DataService.GetTableNameToUaString(focusedTable);

                KeyInformationLabel.Visibility = Visibility.Visible;
                TableInformationLabel.Visibility = Visibility.Visible;
                KeyLabel.Visibility = Visibility.Visible;
                TableLabel.Visibility = Visibility.Visible;
                EditInformationLabel.Visibility = Visibility.Visible;
                EditButton.Visibility = Visibility.Visible;
                AddButton.Visibility = Visibility.Visible;
                RemoveButton.Visibility = Visibility.Visible;

                AddPopupGrid.IsEnabled = false;
                RemovePopupGrid.IsEnabled = false;

                AddPopupGrid.Visibility = Visibility.Collapsed;
                RemovePopupGrid.Visibility = Visibility.Collapsed;
                ConfirmButton.Visibility = Visibility.Collapsed;
            }
        }

        private void PersonsGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            var currentCell = PersonsGrid.CurrentCell;

            if (currentCell != null && currentCell.Item != null && currentCell.Item != DependencyProperty.UnsetValue)
            {
                if (_lastSelectedCell.HasValue &&
                    _lastSelectedCell.Value.Item == currentCell.Item &&
                    _lastSelectedCell.Value.Column == currentCell.Column)
                {
                    return;
                }

                _lastSelectedCell = currentCell;

                PersonsGrid.SelectedItem = null;
                PersonsGrid.SelectedCells.Clear();
                PersonsGrid.IsEnabled = true;
                focusedTable = DataService.TableName.PERSONS;

                if (!PersonsGrid.IsReadOnly)
                {
                    EditButton_Click(sender, new RoutedEventArgs());
                }

                Person selectedPerson = (Person)currentCell.Item;

                KeyLabel.Content = selectedPerson.PersonId;
                TableLabel.Content = DataService.GetTableNameToUaString(focusedTable);

                KeyInformationLabel.Visibility = Visibility.Visible;
                TableInformationLabel.Visibility = Visibility.Visible;
                KeyLabel.Visibility = Visibility.Visible;
                TableLabel.Visibility = Visibility.Visible;
                EditInformationLabel.Visibility = Visibility.Visible;
                EditButton.Visibility = Visibility.Visible;
                AddButton.Visibility = Visibility.Visible;
                RemoveButton.Visibility = Visibility.Visible;

                AddPopupGrid.IsEnabled = false;
                RemovePopupGrid.IsEnabled = false;

                AddPopupGrid.Visibility = Visibility.Collapsed;
                RemovePopupGrid.Visibility = Visibility.Collapsed;
                ConfirmButton.Visibility = Visibility.Collapsed;
            }
        }

        private void DispatchersGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            AddButton.IsEnabled = false;
            RemoveButton.IsEnabled = false;
            EditButton.IsEnabled = false;

            var selectedDispatcher = (Dispatcher)DispatchersGrid.CurrentCell.Item;
            if (selectedDispatcher == null) return;

            Dispatcher newDispatcher = new Dispatcher
            {                
                PersonId = selectedDispatcher.PersonId,
                DepotId = selectedDispatcher.DepotId,
                Position = selectedDispatcher.Position,
                EmploymentDate = selectedDispatcher.EmploymentDate,
                Notes = selectedDispatcher.Notes
            };

            var newValue = (e.EditingElement as TextBox)?.Text;
            if (newValue != null)
            {
                var column = e.Column as DataGridBoundColumn;
                if (column != null)
                {
                    var bindingPath = (column.Binding as Binding)?.Path.Path;

                    switch (bindingPath)
                    {
                        case nameof(AppServer.Models.Dispatcher.Position):
                            newDispatcher.Position = newValue;
                            break;
                        case nameof(AppServer.Models.Dispatcher.EmploymentDate):
                            if (DateOnly.TryParse(newValue, out DateOnly date) && date.Year > 1900 && date.Year <= DateTime.Now.Year)
                            {
                                newDispatcher.EmploymentDate = date;
                            }
                            else
                            {
                                MessageBox.Show("Введіть коректний рік початку роботи.\nРік має бути пізніший за 1900, але не пізніший за поточний", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                            break;
                        case nameof(AppServer.Models.Dispatcher.Notes):
                            newDispatcher.Notes = newValue;
                            break;                    
                    }
                }
            }

            string jsonObject = JsonSerializer.Serialize(newDispatcher);
            if (newDispatcher != null)
            {
                client.Transmit($"EDIT~UPDATEDISPATCHER~{newDispatcher.PersonId}~{jsonObject}~");
            }
        }

        private void DriversGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            AddButton.IsEnabled = false;
            RemoveButton.IsEnabled = false;
            EditButton.IsEnabled = false;

            var selectedDriver = (Driver)DriversGrid.CurrentCell.Item;
            if (selectedDriver == null) return;

            Driver newDriver = new Driver
            {
                PersonId = selectedDriver.PersonId,
                DepotId = selectedDriver.DepotId,
                Position = selectedDriver.Position,
                EmploymentDate = selectedDriver.EmploymentDate,
                Notes = selectedDriver.Notes
            };

            var newValue = (e.EditingElement as TextBox)?.Text;
            if (newValue != null)
            {
                var column = e.Column as DataGridBoundColumn;
                if (column != null)
                {
                    var bindingPath = (column.Binding as Binding)?.Path.Path;
                    switch (bindingPath)
                    {
                        case nameof(AppServer.Models.Driver.Position):
                            newDriver.Position = newValue;
                            break;
                        case nameof(AppServer.Models.Driver.EmploymentDate):
                            if (DateOnly.TryParse(newValue, out DateOnly date) && date.Year > 1900 && date.Year <= DateTime.Now.Year)
                            {
                                newDriver.EmploymentDate = date;
                            }
                            else
                            {
                                MessageBox.Show("Введіть коректний рік початку роботи.\nРік має бути пізніший за 1900, але не пізніший за поточний", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                            break;
                        case nameof(AppServer.Models.Driver.Notes):
                            newDriver.Notes = newValue;
                            break;
                    }
                }
            }

            string jsonObject = JsonSerializer.Serialize(newDriver);
            if (newDriver != null)
            {
                client.Transmit($"EDIT~UPDATEDRIVER~{newDriver.PersonId}~{jsonObject}~");
            }
        }

        private void PersonsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            AddButton.IsEnabled = false;
            RemoveButton.IsEnabled = false;
            EditButton.IsEnabled = false;

            var selectedPerson = (Person)PersonsGrid.CurrentCell.Item;
            if (selectedPerson == null) return;

            Person newPerson = new Person
            {
                PersonId = selectedPerson.PersonId,
                FirstName = selectedPerson.FirstName,
                SurName = selectedPerson.SurName,
                LastName = selectedPerson.LastName,
                ContactNumber = selectedPerson.ContactNumber,
                Login = selectedPerson.Login,
                HashedPassword = selectedPerson.HashedPassword
            };

            var newValue = (e.EditingElement as TextBox)?.Text;
            if (newValue != null)
            {
                var column = e.Column as DataGridBoundColumn;
                if (column != null)
                {
                    var bindingPath = (column.Binding as Binding)?.Path.Path;
                    switch (bindingPath)
                    {
                        case nameof(Person.FirstName):
                            newPerson.FirstName = newValue;
                            break;
                        case nameof(Person.SurName):
                            newPerson.SurName = newValue;
                            break;
                        case nameof(Person.LastName):
                            newPerson.LastName = newValue;
                            break;
                        case nameof(Person.ContactNumber):
                            newPerson.ContactNumber = newValue;
                            break;
                        case nameof(Person.Login):
                            newPerson.Login = newValue;
                            break;
                        case nameof(Person.HashedPassword):
                            newPerson.HashedPassword = AppServer.Data.PasswordHandler.GetHashedPassword(newValue);
                            break;
                    }
                }
            }

            string jsonObject = JsonSerializer.Serialize(newPerson);
            if (newPerson != null)
            {
                client.Transmit($"EDIT~UPDATEPERSON~{newPerson.PersonId}~{jsonObject}~");
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddButton.Visibility = Visibility.Collapsed;
            RemoveButton.Visibility = Visibility.Collapsed;

            switch (focusedTable)
            {
                case DataService.TableName.DISPATCHERS:
                    {
                        PrimaryKeyInformationLabel.Visibility = Visibility.Visible;
                        PrimaryKeyTextbox.Visibility = Visibility.Visible;
                        PrimaryKeyTextbox.IsEnabled = true;
                        firstKeyReady = false;

                        SecondaryKeyInformationLabel.Content = "Ключ депо:";
                        SecondaryKeyInformationLabel.Visibility = Visibility.Visible;
                        SecondaryKeyTextbox.Visibility = Visibility.Visible;
                        SecondaryKeyTextbox.IsEnabled = true;
                        secondKeyReady = false;

                        tertiaryKeyReady = true;

                        break;
                    }

                case DataService.TableName.DRIVERS:
                    {
                        PrimaryKeyInformationLabel.Visibility = Visibility.Visible;
                        PrimaryKeyTextbox.Visibility = Visibility.Visible;
                        PrimaryKeyTextbox.IsEnabled = true;
                        firstKeyReady = false;

                        SecondaryKeyInformationLabel.Content = "Ключ депо:";
                        SecondaryKeyInformationLabel.Visibility = Visibility.Visible;
                        SecondaryKeyTextbox.Visibility = Visibility.Visible;
                        SecondaryKeyTextbox.IsEnabled = true;
                        secondKeyReady = false;

                        tertiaryKeyReady = true;

                        break;
                    }

                case DataService.TableName.PERSONS:
                    {
                        PrimaryKeyInformationLabel.Visibility = Visibility.Visible;
                        PrimaryKeyTextbox.Visibility = Visibility.Visible;
                        PrimaryKeyTextbox.IsEnabled = true;
                        firstKeyReady = false;

                        SecondaryKeyInformationLabel.Visibility = Visibility.Collapsed;
                        SecondaryKeyTextbox.Visibility = Visibility.Collapsed;
                        SecondaryKeyTextbox.IsEnabled = false;
                        secondKeyReady = true;

                        tertiaryKeyReady = true;

                        break;
                    }

                default:
                    return;
            }

            AddPopupGrid.Visibility = Visibility.Visible;
            AddPopupGrid.IsEnabled = true;

            ConfirmButton.Visibility = Visibility.Visible;
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            AddButton.Visibility = Visibility.Collapsed;
            RemoveButton.Visibility = Visibility.Collapsed;

            RemovePopupGrid.Visibility = Visibility.Visible;
            RemovePopupGrid.IsEnabled = true;

            ConfirmButton.Visibility = Visibility.Visible;
            ConfirmButton.IsEnabled = true;
        }

        private void PrimaryKeyTextbox_KeyUp(object sender, KeyEventArgs e)
        {
            if (PrimaryKeyTextbox.Text.Length == 0)
            {
                PrimaryKeyTextbox.SetValue(TextboxStyleHelper.IsTextValidProperty, false);
                firstKeyReady = false;
            }            
            else if ((focusedTable == DataService.TableName.DISPATCHERS && DispatchersGrid.Items.Cast<Dispatcher>().Any(d => d.PersonId == PrimaryKeyTextbox.Text))
                || (focusedTable == DataService.TableName.DRIVERS && DriversGrid.Items.Cast<Driver>().Any(d => d.PersonId == PrimaryKeyTextbox.Text))
                || (focusedTable == DataService.TableName.PERSONS && PersonsGrid.Items.Cast<Person>().Any(p => p.PersonId == PrimaryKeyTextbox.Text)))
            {
                PrimaryKeyTextbox.SetValue(TextboxStyleHelper.IsTextValidProperty, false);
                firstKeyReady = false;
            }
            else if ((focusedTable == DataService.TableName.DISPATCHERS && !PersonsGrid.Items.Cast<Person>().Any(p => p.PersonId == PrimaryKeyTextbox.Text))
                || (focusedTable == DataService.TableName.DRIVERS && !PersonsGrid.Items.Cast<Person>().Any(p => p.PersonId == PrimaryKeyTextbox.Text)))
            {
                PrimaryKeyTextbox.SetValue(TextboxStyleHelper.IsTextValidProperty, false);
                firstKeyReady = false;
            }
            else
            {
                PrimaryKeyTextbox.SetValue(TextboxStyleHelper.IsTextValidProperty, true);
                firstKeyReady = true;
            }

            if (firstKeyReady && secondKeyReady && tertiaryKeyReady)
            {
                ConfirmButton.IsEnabled = true;
            }
            else
            {
                ConfirmButton.IsEnabled = false;
            }
        }

        private void SecondaryKeyTextbox_KeyUp(object sender, KeyEventArgs e)
        {
            if (SecondaryKeyTextbox.Text.Length == 0)
            {
                SecondaryKeyTextbox.SetValue(TextboxStyleHelper.IsTextValidProperty, false);
                secondKeyReady = false;
            }            
            else
            {
                SecondaryKeyTextbox.SetValue(TextboxStyleHelper.IsTextValidProperty, true);
                secondKeyReady = true;
            }

            if (firstKeyReady && secondKeyReady && tertiaryKeyReady)
            {
                ConfirmButton.IsEnabled = true;
            }
            else
            {
                ConfirmButton.IsEnabled = false;
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            PrimaryKeyTextbox.IsEnabled = false;
            SecondaryKeyTextbox.IsEnabled = false;

            switch (focusedTable)
            {
                case DataService.TableName.DISPATCHERS:
                    {
                        if (RemovePopupGrid.IsVisible)
                        {
                            client.Transmit($"EDIT~REMOVEDISPATCHER~{KeyLabel.Content}~");
                        }
                        else
                        {
                            Dispatcher newDispatcher = new Dispatcher
                            {
                                PersonId = PrimaryKeyTextbox.Text,
                                DepotId = SecondaryKeyTextbox.Text,
                            };
                            string jsonObject = JsonSerializer.Serialize(newDispatcher);

                            client.Transmit($"EDIT~ADDDISPATCHER~{jsonObject}~");
                        }
                        break;
                    }

                case DataService.TableName.DRIVERS:
                    {
                        if (RemovePopupGrid.IsVisible)
                        {
                            client.Transmit($"EDIT~REMOVEDRIVER~{KeyLabel.Content}~");
                        }
                        else
                        {
                            Driver newDriver = new Driver
                            {
                                PersonId = PrimaryKeyTextbox.Text,
                                DepotId = SecondaryKeyTextbox.Text,
                            };
                            string jsonObject = JsonSerializer.Serialize(newDriver);

                            client.Transmit($"EDIT~ADDDRIVER~{jsonObject}~");
                        }
                        break;
                    }

                case DataService.TableName.PERSONS:
                    {
                        if (RemovePopupGrid.IsVisible)
                        {
                            client.Transmit($"EDIT~REMOVEPERSON~{KeyLabel.Content}~");
                        }
                        else
                        {
                            Person newPerson = new Person
                            {
                                PersonId = PrimaryKeyTextbox.Text,
                                Login = PrimaryKeyTextbox.Text,
                                HashedPassword = 1234.ToString(),
                            };
                            string jsonObject = JsonSerializer.Serialize(newPerson);

                            client.Transmit($"EDIT~ADDPERSON~{jsonObject}~");
                        }
                        break;
                    }

                default:
                    break;
            }

            PrimaryKeyTextbox.IsEnabled = true;
            SecondaryKeyTextbox.IsEnabled = true;
        }
    }
}
