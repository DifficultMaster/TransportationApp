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
    public partial class VehiclePage : Page
    {
        private readonly Client client;
        private int tableRequestNumber;
        private DataService.TableName focusedTable;
        private DataGridCellInfo? _lastSelectedCell;

        private bool firstKeyReady;
        private bool secondKeyReady;
        private bool tertiaryKeyReady;

        public VehiclePage(Client client)
        {
            this.client = client;
            this.tableRequestNumber = 0;
            this.focusedTable = DataService.TableName.NONE;

            this.firstKeyReady = false;
            this.secondKeyReady = false;
            this.tertiaryKeyReady = false;

            InitializeComponent();
            SubscribeEventHandlers();
            RequestVehicles();

            KeyInformationLabel.Visibility = Visibility.Hidden;
            TableInformationLabel.Visibility = Visibility.Hidden;
            EditInformationLabel.Visibility = Visibility.Hidden;

            KeyLabel.Visibility = Visibility.Hidden;
            TableLabel.Visibility = Visibility.Hidden;
            EditButton.Visibility = Visibility.Hidden;
            AddButton.Visibility = Visibility.Hidden;
            RemoveButton.Visibility = Visibility.Hidden;
        }

        ~VehiclePage()
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
            if (message.ToUpper() == DataService.TableName.VEHICLES.ToString() 
                || message.ToUpper() == DataService.TableName.VEHICLETYPES.ToString() 
                || message.ToUpper() == DataService.TableName.ENGINES.ToString())
            {
                RequestVehicles();
            }
        }

        private void PassSelect(object sender, string message)
        {
            if (tableRequestNumber == 0)
            {
                try
                {
                    List<Vehicle> vehicles = System.Text.Json.JsonSerializer.Deserialize<List<Vehicle>>(message);

                    if (vehicles != null)
                    {
                        VehiclesGrid.ItemsSource = vehicles;

                        VehiclesGrid.Columns[0].Header = "ID";
                        VehiclesGrid.Columns[1].Header = "ID типу";
                        VehiclesGrid.Columns[2].Header = "ID депо";
                        VehiclesGrid.Columns[3].Header = "Активний";
                        VehiclesGrid.Columns[4].Header = "Початок роботи";
                        VehiclesGrid.Columns[5].Header = "Посилання на галерею";
                        VehiclesGrid.Columns[6].Header = "Нотатки";

                        VehiclesGrid.Columns[7].Visibility = Visibility.Hidden;
                        VehiclesGrid.Columns[8].Visibility = Visibility.Hidden;
                        VehiclesGrid.Columns[9].Visibility = Visibility.Hidden;

                        VehiclesGrid.Columns[0].IsReadOnly = true;
                        VehiclesGrid.Columns[1].IsReadOnly = true;
                        VehiclesGrid.Columns[2].IsReadOnly = true;

                        VehiclesGrid.Items.Refresh();
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
                    RequestVehicleTypes();
                }
            }
            else if (tableRequestNumber == 1)
            {
                try
                {
                    List<VehicleType> vehicleTypes = System.Text.Json.JsonSerializer.Deserialize<List<VehicleType>>(message);

                    if (vehicleTypes != null)
                    {
                        VehicleTypesGrid.ItemsSource = vehicleTypes;

                        VehicleTypesGrid.Columns[0].Header = "ID";
                        VehicleTypesGrid.Columns[1].Header = "Тип";
                        VehicleTypesGrid.Columns[2].Header = "Країна";
                        VehicleTypesGrid.Columns[3].Header = "Рік";
                        VehicleTypesGrid.Columns[4].Header = "Марка";
                        VehicleTypesGrid.Columns[5].Header = "Модель";
                        VehicleTypesGrid.Columns[6].Header = "ID двигуна";
                        VehicleTypesGrid.Columns[7].Header = "Пасажиромісткість";

                        VehicleTypesGrid.Columns[8].Visibility = Visibility.Hidden;
                        VehicleTypesGrid.Columns[9].Visibility = Visibility.Hidden;

                        VehicleTypesGrid.Columns[0].IsReadOnly = true;
                        VehicleTypesGrid.Columns[6].IsReadOnly = true;

                        VehiclesGrid.Items.Refresh();
                        VehicleTypesGrid.Items.Refresh();
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
                    RequestEngines();
                }
            }
            else if (tableRequestNumber == 2)
            {
                try
                {
                    List<Engine> engines = System.Text.Json.JsonSerializer.Deserialize<List<Engine>>(message);

                    if (engines != null)
                    {
                        EnginesGrid.ItemsSource = engines;

                        EnginesGrid.Columns[0].Header = "ID";
                        EnginesGrid.Columns[1].Header = "Марка";
                        EnginesGrid.Columns[2].Header = "Модель";
                        EnginesGrid.Columns[3].Header = "Рушій";
                        EnginesGrid.Columns[4].Header = "Вартість роботи (на 100 км, грн)";                        

                        EnginesGrid.Columns[5].Visibility = Visibility.Hidden;            

                        EnginesGrid.Columns[0].IsReadOnly = true;

                        VehiclesGrid.Items.Refresh();
                        VehicleTypesGrid.Items.Refresh();
                        EnginesGrid.Items.Refresh();
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
            //MessageBox.Show($"Не вдалося відобразити інформацію.\nПомилка: {message}", "Транспорт", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void PassEdit(object sender, string message)
        {
            VehiclesGrid.SelectedItem = null;
            VehiclesGrid.SelectedCells.Clear();
            VehiclesGrid.IsEnabled = true;

            VehicleTypesGrid.SelectedItem = null;
            VehicleTypesGrid.SelectedCells.Clear();
            VehicleTypesGrid.IsEnabled = true;

            EnginesGrid.SelectedItem = null;
            EnginesGrid.SelectedCells.Clear();
            EnginesGrid.IsEnabled = true;

            EditButton.IsEnabled = true;
            AddButton.IsEnabled = true;
            RemoveButton.IsEnabled = true;          

            EditButton_Click(sender, new RoutedEventArgs());
            RequestVehicles();
        }

        private void FailEdit(object sender, string message)
        {
            MessageBox.Show($"Не вдалося зберегти зміни.\nПомилка: {message}", "Транспорт", MessageBoxButton.OK, MessageBoxImage.Error);

            VehiclesGrid.SelectedItem = null;
            VehiclesGrid.SelectedCells.Clear();
            VehiclesGrid.IsEnabled = true;

            VehicleTypesGrid.SelectedItem = null;
            VehicleTypesGrid.SelectedCells.Clear();
            VehicleTypesGrid.IsEnabled = true;

            EditButton.IsEnabled = true;
            AddButton.IsEnabled = true;
            RemoveButton.IsEnabled = true;

            EditButton_Click(sender, new RoutedEventArgs());
            RequestVehicles();
        }

        private void RequestVehicles()
        {
            client.Transmit($"SELECT~VEHICLES~ALLVEHICLES~");
        }

        private void RequestVehicleTypes()
        {
            client.Transmit($"SELECT~VEHICLETYPES~ALLVEHICLETYPES~");
        }

        private void RequestEngines()
        {
            client.Transmit($"SELECT~ENGINES~ALLENGINES~");
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                switch (focusedTable)
                {
                    case DataService.TableName.VEHICLES:
                        {
                            DataService.GenerateReport($"Перелік всіх транспортних засобів", VehiclesGrid);
                            break;
                        }

                    case DataService.TableName.VEHICLETYPES:
                        {
                            DataService.GenerateReport($"Перелік всіх типів транспорту", VehicleTypesGrid);
                            break;
                        }

                    case DataService.TableName.ENGINES:
                        {
                            DataService.GenerateReport($"Перелік всіх двигунів", EnginesGrid);
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
            VehiclesGrid.IsReadOnly = !VehiclesGrid.IsReadOnly;
            VehicleTypesGrid.IsReadOnly = !VehicleTypesGrid.IsReadOnly;
            EnginesGrid.IsReadOnly = !EnginesGrid.IsReadOnly;

            AddButton.IsEnabled = !AddButton.IsEnabled;
            RemoveButton.IsEnabled = !RemoveButton.IsEnabled;

            AddPopupGrid.Visibility = Visibility.Collapsed;
            TertiaryKeyTextbox.Text = string.Empty;
            SecondaryKeyTextbox.Text = string.Empty;
            PrimaryKeyTextbox.Text = string.Empty;

            RemovePopupGrid.Visibility = Visibility.Collapsed;
            ConfirmButton.Visibility = Visibility.Collapsed;
        }

        private void VehiclesGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            var currentCell = VehiclesGrid.CurrentCell;

            if (currentCell != null && currentCell.Item != null && currentCell.Item != DependencyProperty.UnsetValue)
            {
                if (_lastSelectedCell.HasValue &&
                    _lastSelectedCell.Value.Item == currentCell.Item &&
                    _lastSelectedCell.Value.Column == currentCell.Column)
                {
                    return;
                }

                _lastSelectedCell = currentCell;

                VehiclesGrid.SelectedItem = null;
                VehiclesGrid.SelectedCells.Clear();
                VehiclesGrid.IsEnabled = true;
                focusedTable = DataService.TableName.VEHICLES;

                if (!VehiclesGrid.IsReadOnly)
                {
                    EditButton_Click(sender, new RoutedEventArgs());
                }

                Vehicle selectedVehicle = (Vehicle)currentCell.Item;

                KeyLabel.Content = selectedVehicle.VehicleId;
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

        private void VehicleTypesGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            var currentCell = VehicleTypesGrid.CurrentCell;

            if (currentCell != null && currentCell.Item != null && currentCell.Item != DependencyProperty.UnsetValue)
            {
                if (_lastSelectedCell.HasValue &&
                    _lastSelectedCell.Value.Item == currentCell.Item &&
                    _lastSelectedCell.Value.Column == currentCell.Column)
                {
                    return;
                }

                _lastSelectedCell = currentCell;

                VehicleTypesGrid.SelectedItem = null;
                VehicleTypesGrid.SelectedCells.Clear();
                VehicleTypesGrid.IsEnabled = true;
                focusedTable = DataService.TableName.VEHICLETYPES;

                if (!VehicleTypesGrid.IsReadOnly)
                {
                    EditButton_Click(sender, new RoutedEventArgs());
                }

                VehicleType selectedVehicleType = (VehicleType)currentCell.Item;

                KeyLabel.Content = selectedVehicleType.VehicleTypeId;
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

        private void EnginesGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            var currentCell = EnginesGrid.CurrentCell;

            if (currentCell != null && currentCell.Item != null && currentCell.Item != DependencyProperty.UnsetValue)
            {
                if (_lastSelectedCell.HasValue &&
                    _lastSelectedCell.Value.Item == currentCell.Item &&
                    _lastSelectedCell.Value.Column == currentCell.Column)
                {
                    return;
                }

                _lastSelectedCell = currentCell;

                EnginesGrid.SelectedItem = null;
                EnginesGrid.SelectedCells.Clear();
                EnginesGrid.IsEnabled = true;
                focusedTable = DataService.TableName.ENGINES;

                if (!EnginesGrid.IsReadOnly)
                {
                    EditButton_Click(sender, new RoutedEventArgs());
                }

                Engine selectedEngine = (Engine)currentCell.Item;

                KeyLabel.Content = selectedEngine.EngineId;
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

        private void VehiclesGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            AddButton.IsEnabled = false;
            RemoveButton.IsEnabled = false;
            EditButton.IsEnabled = false;

            var selectedVehicle = (Vehicle)VehiclesGrid.CurrentCell.Item;
            if (selectedVehicle == null) return;

            Vehicle newVehicle = new Vehicle
            {
                VehicleTypeId = selectedVehicle.VehicleTypeId,
                VehicleId = selectedVehicle.VehicleId,
                DepotId = selectedVehicle.DepotId,
                IsActive = selectedVehicle.IsActive,
                BeginOperationYear = selectedVehicle.BeginOperationYear,
                GalleryUrl = selectedVehicle.GalleryUrl,
                Notes = selectedVehicle.Notes
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
                        case nameof(Vehicle.IsActive):
                            if (newValue.ToLower() == "так" || newValue.ToLower() == "ні")
                            {
                                newVehicle.IsActive = newValue;
                            }
                            else
                            {
                                MessageBox.Show("Введіть коректний статус активності ТЗ.\nПоле має містити 'так' або 'ні'.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                            break;
                        case nameof(Vehicle.BeginOperationYear):
                            if (int.TryParse(newValue, out int year) && year > 1900 && year <= DateTime.Now.Year)
                            {
                                newVehicle.BeginOperationYear = year;
                            }
                            else
                            {
                                MessageBox.Show("Введіть коректний рік початку роботи.\nРік має бути пізніший за 1900, але не пізніший за поточний", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }                            
                            break;
                        case nameof(Vehicle.GalleryUrl):
                            newVehicle.GalleryUrl = newValue;
                            break;
                        case nameof(Vehicle.Notes):
                            newVehicle.Notes = newValue;
                            break;
                    }
                }
            }

            string jsonObject = JsonSerializer.Serialize(newVehicle);
            if (newVehicle != null)
            {
                client.Transmit($"EDIT~UPDATEVEHICLE~{newVehicle.VehicleId}~{jsonObject}~");
            }
        }

        private void VehicleTypesGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            AddButton.IsEnabled = false;
            RemoveButton.IsEnabled = false;
            EditButton.IsEnabled = false;

            var selectedVehicleType = (VehicleType)VehicleTypesGrid.CurrentCell.Item;
            if (selectedVehicleType == null) return;

            VehicleType newVehicleType = new VehicleType
            {
                VehicleTypeId = selectedVehicleType.VehicleTypeId,
                Type = selectedVehicleType.Type,
                ManufactureCountry = selectedVehicleType.ManufactureCountry,
                ModelYear = selectedVehicleType.ModelYear,
                Make = selectedVehicleType.Make,
                Model = selectedVehicleType.Model,
                EngineId = selectedVehicleType.EngineId,
                Capacity = selectedVehicleType.Capacity
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
                        case nameof(VehicleType.Type):
                            newVehicleType.Type = newValue;
                            break;
                        case nameof(VehicleType.ManufactureCountry):
                            newVehicleType.ManufactureCountry = newValue;
                            break;
                        case nameof(VehicleType.ModelYear):
                            if (int.TryParse(newValue, out int year) && year > 1900 && year <= DateTime.Now.Year)
                            {
                                newVehicleType.ModelYear = year;
                            }
                            else
                            {
                                MessageBox.Show("Введіть коректний рік моделі.\nРік має бути пізніший за 1900, але не пізніший за поточний", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }                           
                            break;
                        case nameof(VehicleType.Make):
                            newVehicleType.Make = newValue;
                            break;
                        case nameof(VehicleType.Model):
                            newVehicleType.Model = newValue;
                            break;                        
                        case nameof(VehicleType.Capacity):
                            if (int.TryParse(newValue, out int capacity) && capacity >= 0)
                            {
                                newVehicleType.Capacity = capacity;
                            }
                            else
                            {
                               MessageBox.Show("Введіть коректну пасажиромісткість.\nПасажиромісткість має бути більше 0", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                            break;
                    }
                }
            }

            string jsonObject = JsonSerializer.Serialize(newVehicleType);
            if (newVehicleType != null)
            {
                client.Transmit($"EDIT~UPDATEVEHICLETYPE~{newVehicleType.VehicleTypeId}~{jsonObject}~");
            }
        }

        private void EnginesGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            AddButton.IsEnabled = false;
            RemoveButton.IsEnabled = false;
            EditButton.IsEnabled = false;

            var selectedEngine = (Engine)EnginesGrid.CurrentCell.Item;
            if (selectedEngine == null) return;

            Engine newEngine = new Engine
            {
                EngineId = selectedEngine.EngineId,
                Make = selectedEngine.Make,
                Model = selectedEngine.Model,
                Propulsion = selectedEngine.Propulsion,
                CostOfOperationPer100km = selectedEngine.CostOfOperationPer100km
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
                        case nameof(Engine.Make):
                            newEngine.Make = newValue;
                            break;
                        case nameof(Engine.Model):
                            newEngine.Model = newValue;
                            break;                        
                        case nameof(Engine.Propulsion):
                            newEngine.Propulsion = newValue;
                            break;
                        case nameof(Engine.CostOfOperationPer100km):
                            if (decimal.TryParse(newValue.Replace('.', ','), out decimal cost) && cost >= 0)
                            {
                                newEngine.CostOfOperationPer100km = cost;
                            }
                            else
                            {
                                MessageBox.Show("Введіть коректну вартість роботи.\nВартість має бути більше 0", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }                            
                            break;                                         
                    }
                }
            }

            string jsonObject = JsonSerializer.Serialize(newEngine);
            if (newEngine != null)
            {
                client.Transmit($"EDIT~UPDATEENGINE~{newEngine.EngineId}~{jsonObject}~");
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddButton.Visibility = Visibility.Collapsed;
            RemoveButton.Visibility = Visibility.Collapsed;

            switch (focusedTable)
            {
                case DataService.TableName.VEHICLES:
                    {
                        PrimaryKeyInformationLabel.Visibility = Visibility.Visible;
                        PrimaryKeyTextbox.Visibility = Visibility.Visible;
                        PrimaryKeyTextbox.IsEnabled = true;
                        firstKeyReady = false;

                        SecondaryKeyInformationLabel.Content = "Ключ типу ТЗ:";
                        SecondaryKeyInformationLabel.Visibility = Visibility.Visible;
                        SecondaryKeyTextbox.Visibility = Visibility.Visible;
                        SecondaryKeyTextbox.IsEnabled = true;
                        secondKeyReady = false;

                        TertiaryKeyInformationLabel.Content = "Ключ депо:";
                        TertiaryKeyInformationLabel.Visibility = Visibility.Visible;
                        TertiaryKeyTextbox.Visibility = Visibility.Visible;
                        TertiaryKeyTextbox.IsEnabled = true;
                        tertiaryKeyReady = false;

                        break;
                    }

                case DataService.TableName.VEHICLETYPES:
                    {
                        PrimaryKeyInformationLabel.Visibility = Visibility.Visible;
                        PrimaryKeyTextbox.Visibility = Visibility.Visible;
                        PrimaryKeyTextbox.IsEnabled = true;
                        firstKeyReady = false;

                        SecondaryKeyInformationLabel.Content = "Ключ двигуну:";
                        SecondaryKeyInformationLabel.Visibility = Visibility.Visible;
                        SecondaryKeyTextbox.Visibility = Visibility.Visible;
                        SecondaryKeyTextbox.IsEnabled = true;
                        secondKeyReady = false;
                       
                        TertiaryKeyInformationLabel.Visibility = Visibility.Collapsed;
                        TertiaryKeyTextbox.Visibility = Visibility.Collapsed;
                        TertiaryKeyTextbox.IsEnabled = false;
                        tertiaryKeyReady = true;                     

                        break;
                    }

                case DataService.TableName.ENGINES:
                    {
                        PrimaryKeyInformationLabel.Visibility = Visibility.Visible;
                        PrimaryKeyTextbox.Visibility = Visibility.Visible;
                        PrimaryKeyTextbox.IsEnabled = true;
                        firstKeyReady = false;
                      
                        SecondaryKeyInformationLabel.Visibility = Visibility.Collapsed;
                        SecondaryKeyTextbox.Visibility = Visibility.Collapsed;
                        SecondaryKeyTextbox.IsEnabled = false;
                        secondKeyReady = true;

                        TertiaryKeyInformationLabel.Visibility = Visibility.Collapsed;
                        TertiaryKeyTextbox.Visibility = Visibility.Collapsed;
                        TertiaryKeyTextbox.IsEnabled = false;
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
            else if ((focusedTable == DataService.TableName.VEHICLETYPES || focusedTable == DataService.TableName.ENGINES) && PrimaryKeyTextbox.Text.Length > 5)
            {
                PrimaryKeyTextbox.SetValue(TextboxStyleHelper.IsTextValidProperty, false);
                firstKeyReady = false;
            }
            else if ((focusedTable == DataService.TableName.VEHICLES && VehiclesGrid.Items.Cast<Vehicle>().Any(v => v.VehicleId == PrimaryKeyTextbox.Text)) 
                || (focusedTable == DataService.TableName.VEHICLETYPES && VehicleTypesGrid.Items.Cast<VehicleType>().Any(vt => vt.VehicleTypeId == PrimaryKeyTextbox.Text)) 
                || (focusedTable == DataService.TableName.ENGINES && EnginesGrid.Items.Cast<Engine>().Any(e => e.EngineId == PrimaryKeyTextbox.Text)))
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
            else if ((focusedTable == DataService.TableName.VEHICLES && !VehicleTypesGrid.Items.Cast<VehicleType>().Any(vt => vt.VehicleTypeId == SecondaryKeyTextbox.Text))
                || (focusedTable == DataService.TableName.VEHICLETYPES && !EnginesGrid.Items.Cast<Engine>().Any(e => e.EngineId == SecondaryKeyTextbox.Text)))
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

        private void TertiaryKeyTextbox_KeyUp(object sender, KeyEventArgs e)
        {
            if(TertiaryKeyTextbox.Text.Length == 0)
            {
                TertiaryKeyTextbox.SetValue(TextboxStyleHelper.IsTextValidProperty, false);
                tertiaryKeyReady = false;
            }            
            else
            {
                TertiaryKeyTextbox.SetValue(TextboxStyleHelper.IsTextValidProperty, true);
                tertiaryKeyReady = true;
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
                case DataService.TableName.VEHICLES:
                    {
                        if (RemovePopupGrid.IsVisible)
                        {
                            client.Transmit($"EDIT~REMOVEVEHICLE~{KeyLabel.Content}~");
                        }
                        else
                        {
                            Vehicle newVehicle = new Vehicle
                            {
                                VehicleId = PrimaryKeyTextbox.Text,
                                VehicleTypeId = SecondaryKeyTextbox.Text,
                                DepotId = TertiaryKeyTextbox.Text
                            };
                            string jsonObject = JsonSerializer.Serialize(newVehicle);

                            client.Transmit($"EDIT~ADDVEHICLE~{jsonObject}~");
                        }
                        break;
                    }

                case DataService.TableName.VEHICLETYPES:
                    {
                        if (RemovePopupGrid.IsVisible)
                        {
                            client.Transmit($"EDIT~REMOVEVEHICLETYPE~{KeyLabel.Content}~");
                        }
                        else
                        {
                            VehicleType newVehicleType = new VehicleType
                            {
                                VehicleTypeId = PrimaryKeyTextbox.Text,
                                EngineId = SecondaryKeyTextbox.Text
                            };
                            string jsonObject = JsonSerializer.Serialize(newVehicleType);

                            client.Transmit($"EDIT~ADDVEHICLETYPE~{jsonObject}~");
                        }
                        break;
                    }

                case DataService.TableName.ENGINES:
                    {
                        if (RemovePopupGrid.IsVisible)
                        {
                            client.Transmit($"EDIT~REMOVEENGINE~{KeyLabel.Content}~");
                        }
                        else
                        {
                            Engine newEngine = new Engine
                            {
                                EngineId = PrimaryKeyTextbox.Text
                            };
                            string jsonObject = JsonSerializer.Serialize(newEngine);

                            client.Transmit($"EDIT~ADDENGINE~{jsonObject}~");
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
