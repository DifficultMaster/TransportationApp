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
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;

namespace AppClient.UI.AccessLvl3
{
    public partial class DepotPage : Page
    {
        private readonly Client client;
        private bool isFirstTableRequested;
        private DataService.TableName focusedTable;
        private DataGridCellInfo? _lastSelectedCell;

        private bool firstKeyReady;
        private bool secondKeyReady;

        public DepotPage(Client client)
        {
            this.client = client;
            this.isFirstTableRequested = false;
            this.focusedTable = DataService.TableName.NONE;

            this.firstKeyReady = false;
            this.secondKeyReady = false;

            InitializeComponent();
            SubscribeEventHandlers();
            RequestDepots();
            
            KeyInformationLabel.Visibility = Visibility.Hidden;  
            TableInformationLabel.Visibility = Visibility.Hidden;
            EditInformationLabel.Visibility = Visibility.Hidden;

            KeyLabel.Visibility = Visibility.Hidden;        
            TableLabel.Visibility = Visibility.Hidden;
            EditButton.Visibility = Visibility.Hidden;
            AddButton.Visibility = Visibility.Hidden;
            RemoveButton.Visibility = Visibility.Hidden;
        }

        ~DepotPage()
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
            if (message.ToUpper() == DataService.TableName.DEPOTS.ToString() || message.ToUpper() == DataService.TableName.ADDRESSES.ToString())
            {
                RequestDepots();
            }
        }

        private void PassSelect(object sender, string message)
        {
            if (!isFirstTableRequested)
            {
                try
                {
                    List<Depot> depot = System.Text.Json.JsonSerializer.Deserialize<List<Depot>>(message);

                    if (depot != null)
                    {
                        DepotsGrid.ItemsSource = depot;

                        DepotsGrid.Columns[0].Header = "ID";
                        DepotsGrid.Columns[1].Header = "Назва";
                        DepotsGrid.Columns[2].Header = "Тип";
                        DepotsGrid.Columns[3].Header = "Оператор";
                        DepotsGrid.Columns[4].Header = "ID адреси";
                        DepotsGrid.Columns[5].Header = "Контактний номер";

                        DepotsGrid.Columns[6].Visibility = Visibility.Hidden;
                        DepotsGrid.Columns[7].Visibility = Visibility.Hidden;
                        DepotsGrid.Columns[8].Visibility = Visibility.Hidden;
                        DepotsGrid.Columns[9].Visibility = Visibility.Hidden;

                        DepotsGrid.Columns[0].IsReadOnly = true;
                        DepotsGrid.Columns[4].IsReadOnly = true;

                        DepotsGrid.Items.Refresh();
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
                    isFirstTableRequested = true;
                    RequestAddresses();
                }
            }
            else
            {
                try
                {
                    List<Address> depot = System.Text.Json.JsonSerializer.Deserialize<List<Address>>(message);

                    if (depot != null)
                    {
                        AddressesGrid.ItemsSource = depot;

                        AddressesGrid.Columns[0].Header = "ID";
                        AddressesGrid.Columns[1].Header = "Місто";
                        AddressesGrid.Columns[2].Header = "Район";
                        AddressesGrid.Columns[3].Header = "Вулиця";
                        AddressesGrid.Columns[4].Header = "Будинок";                    

                        AddressesGrid.Columns[5].Visibility = Visibility.Hidden;

                        AddressesGrid.Columns[0].IsReadOnly = true;

                        DepotsGrid.Items.Refresh();
                        AddressesGrid.Items.Refresh();
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
                    isFirstTableRequested = false;
                }
            }
        }

        private void FailSelect(object sender, string message)
        {
            //MessageBox.Show($"Не вдалося відобразити інформацію.\nПомилка: {message}", "Депо", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void PassEdit(object sender, string message)
        {
            DepotsGrid.SelectedItem = null;
            DepotsGrid.SelectedCells.Clear();
            DepotsGrid.IsEnabled = true;

            AddressesGrid.SelectedItem = null;
            AddressesGrid.SelectedCells.Clear();
            AddressesGrid.IsEnabled = true;

            EditButton.IsEnabled = true;
            AddButton.IsEnabled = true;
            RemoveButton.IsEnabled = true;

            EditButton_Click(sender, new RoutedEventArgs());
            RequestDepots();
        }

        private void FailEdit(object sender, string message)
        {
            MessageBox.Show($"Не вдалося зберегти зміни.\nПомилка: {message}", "Депо", MessageBoxButton.OK, MessageBoxImage.Error);

            DepotsGrid.SelectedItem = null;
            DepotsGrid.SelectedCells.Clear();
            DepotsGrid.IsEnabled = true;

            AddressesGrid.SelectedItem = null;
            AddressesGrid.SelectedCells.Clear();
            AddressesGrid.IsEnabled = true;

            EditButton.IsEnabled = true;
            AddButton.IsEnabled = true;
            RemoveButton.IsEnabled = true;

            EditButton_Click(sender, new RoutedEventArgs());
            RequestDepots();
        }

        private void RequestDepots()
        {
            client.Transmit($"SELECT~DEPOTS~ALLDEPOTS~");
        }

        private void RequestAddresses()
        {
            client.Transmit($"SELECT~ADDRESSES~ALLADDRESSES~");
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {           
            try
            {
                switch (focusedTable)
                {
                    case DataService.TableName.DEPOTS:
                        {
                            DataService.GenerateReport($"Перелік всіх депо", DepotsGrid);
                            break;
                        }

                    case DataService.TableName.ADDRESSES:
                        {
                            DataService.GenerateReport($"Перелік всіх адрес депо", AddressesGrid);
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
                MessageBox.Show($"Не вдалося зберегти звіт.\nПомилка: {ex.Message}", "Транспорт", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            UnsubscribeEventHandlers();
            Unloaded -= Page_Unloaded;
        }       

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            DepotsGrid.IsReadOnly = !DepotsGrid.IsReadOnly;
            AddressesGrid.IsReadOnly = !AddressesGrid.IsReadOnly;

            AddButton.IsEnabled = !AddButton.IsEnabled;
            RemoveButton.IsEnabled = !RemoveButton.IsEnabled;

            AddPopupGrid.Visibility = Visibility.Collapsed;
            AddressKeyTextbox.Text = string.Empty;
            PrimaryKeyTextbox.Text = string.Empty;

            RemovePopupGrid.Visibility = Visibility.Collapsed;
            ConfirmButton.Visibility = Visibility.Collapsed;
        }

        private void DepotsGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            var currentCell = DepotsGrid.CurrentCell;

            if (currentCell != null && currentCell.Item != null && currentCell.Item != DependencyProperty.UnsetValue)
            {
                if (_lastSelectedCell.HasValue &&
                    _lastSelectedCell.Value.Item == currentCell.Item &&
                    _lastSelectedCell.Value.Column == currentCell.Column)
                {
                    return; 
                }

                _lastSelectedCell = currentCell;

                AddressesGrid.SelectedItem = null;
                AddressesGrid.SelectedCells.Clear();
                AddressesGrid.IsEnabled = true;
                focusedTable = DataService.TableName.DEPOTS;

                if (!DepotsGrid.IsReadOnly)
                {
                    EditButton_Click(sender, new RoutedEventArgs());
                }

                Depot selectedDepot = (Depot)currentCell.Item;

                KeyLabel.Content = selectedDepot.DepotId;
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

        private void AddressesGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            var currentCell = AddressesGrid.CurrentCell;

            if (currentCell != null && currentCell.Item != null && currentCell.Item != DependencyProperty.UnsetValue)
            {
                if (_lastSelectedCell.HasValue &&
                    _lastSelectedCell.Value.Item == currentCell.Item &&
                    _lastSelectedCell.Value.Column == currentCell.Column)
                {
                    return;
                }

                _lastSelectedCell = currentCell;

                DepotsGrid.SelectedItem = null;
                DepotsGrid.SelectedCells.Clear();
                DepotsGrid.IsEnabled = true;
                focusedTable = DataService.TableName.ADDRESSES;

                if (!AddressesGrid.IsReadOnly)
                {
                    EditButton_Click(sender, new RoutedEventArgs());
                }

                Address selectedAddress = (Address)currentCell.Item;

                KeyLabel.Content = selectedAddress.AddressId;
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

        private void AddressesGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {    
            AddButton.IsEnabled = false;
            RemoveButton.IsEnabled = false;
            EditButton.IsEnabled = false;            

            var selectedAddress = (Address)AddressesGrid.CurrentCell.Item;
            if (selectedAddress == null) return;
         
            Address newAddress = new Address
            {
                AddressId = selectedAddress.AddressId,
                City = selectedAddress.City,
                District = selectedAddress.District,
                Street = selectedAddress.Street,
                BuildingNo = selectedAddress.BuildingNo
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
                        case nameof(Address.City):
                            newAddress.City = newValue;
                            break;
                        case nameof(Address.District):
                            newAddress.District = newValue;
                            break;
                        case nameof(Address.Street):
                            newAddress.Street = newValue;
                            break;
                        case nameof(Address.BuildingNo):
                            newAddress.BuildingNo = newValue;
                            break;                           
                    }
                }
            }
            
            string jsonObject = JsonSerializer.Serialize(newAddress);
            if (newAddress != null)
            {
                client.Transmit($"EDIT~UPDATEADDRESS~{newAddress.AddressId}~{jsonObject}~");
            }
        }

        private void DepotsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            AddButton.IsEnabled = false;
            RemoveButton.IsEnabled = false;
            EditButton.IsEnabled = false;

            var selectedAddress = (Depot)DepotsGrid.CurrentCell.Item;
            if (selectedAddress == null) return;

            Depot newDepot = new Depot
            {
                AddressId = selectedAddress.AddressId,
                DepotId = selectedAddress.DepotId,
                Name = selectedAddress.Name,
                Type = selectedAddress.Type,
                Operator = selectedAddress.Operator,
                ContactNumber = selectedAddress.ContactNumber
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
                        case nameof(Depot.Name):
                            newDepot.Name = newValue;
                            break;
                        case nameof(Depot.Type):
                            newDepot.Type = newValue;
                            break;
                        case nameof(Depot.Operator):
                            newDepot.Operator = newValue;
                            break;
                        case nameof(Depot.ContactNumber):
                            newDepot.ContactNumber = newValue;
                            break;
                    }
                }
            }

            string jsonObject = JsonSerializer.Serialize(newDepot);
            if (newDepot != null)
            {
                client.Transmit($"EDIT~UPDATEDEPOT~{newDepot.DepotId}~{jsonObject}~");
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddButton.Visibility = Visibility.Collapsed;
            RemoveButton.Visibility = Visibility.Collapsed;

            switch (focusedTable)
            {
                case DataService.TableName.DEPOTS:
                    {
                        PrimaryKeyInformationLabel.Visibility = Visibility.Visible;
                        PrimaryKeyTextbox.Visibility = Visibility.Visible;
                        PrimaryKeyTextbox.IsEnabled = true;
                        firstKeyReady = false;

                        AddressKeyInformationLabel.Visibility = Visibility.Visible;
                        AddressKeyTextbox.Visibility = Visibility.Visible;
                        AddressKeyTextbox.IsEnabled = true;
                        secondKeyReady = false;

                        break;
                    }

                case DataService.TableName.ADDRESSES:
                    {
                        PrimaryKeyInformationLabel.Visibility = Visibility.Visible;
                        PrimaryKeyTextbox.Visibility = Visibility.Visible;
                        PrimaryKeyTextbox.IsEnabled = true;
                        firstKeyReady = false;

                        AddressKeyInformationLabel.Visibility = Visibility.Collapsed;
                        AddressKeyTextbox.Visibility = Visibility.Collapsed;
                        AddressKeyTextbox.IsEnabled = false;
                        secondKeyReady = true;

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

        private void AddressKeyTextbox_KeyUp(object sender, KeyEventArgs e)
        {
            if (AddressKeyTextbox.Text.Length == 0)
            {
                AddressKeyTextbox.SetValue(TextboxStyleHelper.IsTextValidProperty, false);                
                secondKeyReady = false;
            }
            else if (!AddressesGrid.Items.Cast<Address>().Any(a => a.AddressId == AddressKeyTextbox.Text))
            {
                AddressKeyTextbox.SetValue(TextboxStyleHelper.IsTextValidProperty, false);
                secondKeyReady = false;
            }            
            else
            {
                AddressKeyTextbox.SetValue(TextboxStyleHelper.IsTextValidProperty, true);
                secondKeyReady = true;
            }

            if (firstKeyReady && secondKeyReady)
            {
                ConfirmButton.IsEnabled = true;
            }
            else
            {
                ConfirmButton.IsEnabled = false;
            }
        }

        private void PrimaryKeyTextbox_KeyUp(object sender, KeyEventArgs e)
        {
            if (PrimaryKeyTextbox.Text.Length == 0)
            {
                PrimaryKeyTextbox.SetValue(TextboxStyleHelper.IsTextValidProperty, false);
                firstKeyReady = false;
            }
            else if (focusedTable == DataService.TableName.DEPOTS && DepotsGrid.Items.Cast<Depot>().Any(d => d.DepotId == PrimaryKeyTextbox.Text))
            {
                PrimaryKeyTextbox.SetValue(TextboxStyleHelper.IsTextValidProperty, false);
                firstKeyReady = false;
            }
            else if (focusedTable == DataService.TableName.ADDRESSES && AddressesGrid.Items.Cast<Address>().Any(a => a.AddressId == PrimaryKeyTextbox.Text))
            {
                PrimaryKeyTextbox.SetValue(TextboxStyleHelper.IsTextValidProperty, false);
                firstKeyReady = false;
            }
            else
            {
                PrimaryKeyTextbox.SetValue(TextboxStyleHelper.IsTextValidProperty, true);
                firstKeyReady = true;                
            }

            if (firstKeyReady && secondKeyReady)
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
            AddressKeyTextbox.IsEnabled = false;

            switch (focusedTable)
            {
                case DataService.TableName.DEPOTS:
                    {
                        if (RemovePopupGrid.IsVisible)
                        {
                            client.Transmit($"EDIT~REMOVEDEPOT~{KeyLabel.Content}~");
                        }
                        else
                        {
                            Depot newDepot = new Depot
                            {
                                DepotId = PrimaryKeyTextbox.Text,
                                AddressId = AddressKeyTextbox.Text
                            };
                            string jsonObject = JsonSerializer.Serialize(newDepot);

                            client.Transmit($"EDIT~ADDDEPOT~{jsonObject}~");
                        }
                        break;
                    }

                case DataService.TableName.ADDRESSES:
                    {
                        if (RemovePopupGrid.IsVisible)
                        {
                            client.Transmit($"EDIT~REMOVEADDRESS~{KeyLabel.Content}~");
                        }
                        else
                        {
                            Address newAddress = new Address
                            {                                
                                AddressId = PrimaryKeyTextbox.Text
                            };
                            string jsonObject = JsonSerializer.Serialize(newAddress);

                            client.Transmit($"EDIT~ADDADDRESS~{jsonObject}~");
                        }
                        break;
                    }

                default:
                    break;
            }

            PrimaryKeyTextbox.IsEnabled = true;
            AddressKeyTextbox.IsEnabled = true;
        }        
    }
}
