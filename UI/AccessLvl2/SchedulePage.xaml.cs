using AppClient.Data;
using AppServer.Models;
using BD4Client.Network;
using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace AppClient.UI.AccessLvl2
{
    public partial class SchedulePage : Page
    {
        private readonly Client client;
        private int tableRequestNumber;      
        private DataGridCellInfo? _lastSelectedCell;

        private bool firstKeyReady;
        private bool secondKeyReady;
        private bool tertiaryKeyReady;
      
        private List<Route> routes;
        private List<Vehicle> vehicles;
        private List<Driver> drivers;

        public SchedulePage(Client client)
        {
            this.client = client;
            this.tableRequestNumber = 0;          

            this.firstKeyReady = false;
            this.secondKeyReady = false;
            this.tertiaryKeyReady = false;
            
            this.routes = new List<Route>();
            this.vehicles = new List<Vehicle>();
            this.drivers = new List<Driver>();

            InitializeComponent();
            SubscribeEventHandlers();
            RequestSchedule();

            KeyInformationLabel.Visibility = Visibility.Hidden;            
            EditInformationLabel.Visibility = Visibility.Hidden;

            KeyLabel.Visibility = Visibility.Hidden;         
            EditButton.Visibility = Visibility.Hidden;
            AddButton.Visibility = Visibility.Hidden;
            RemoveButton.Visibility = Visibility.Hidden;
        }

        ~SchedulePage()
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
                RequestSchedule();
            }
        }

        private void PassSelect(object sender, string message)
        {
            if (tableRequestNumber == 0)
            {
                try
                {
                    List<Schedule> schedules = System.Text.Json.JsonSerializer.Deserialize<List<Schedule>>(message);

                    if (schedules != null)
                    {
                        ScheduleGrid.ItemsSource = schedules;

                        ScheduleGrid.Columns[0].Header = "ID";
                        ScheduleGrid.Columns[1].Header = "ID маршруту";
                        ScheduleGrid.Columns[2].Header = "ID транспорту";
                        ScheduleGrid.Columns[3].Header = "ID водія";
                        ScheduleGrid.Columns[4].Header = "Початок";
                        ScheduleGrid.Columns[5].Header = "Завершення";

                        if (ScheduleGrid.Columns[4] is DataGridTextColumn startColumn)
                        {
                            startColumn.Binding = new Binding("StartDate")
                            {
                                StringFormat = "dd.MM.yyyy HH:mm",
                                ConverterCulture = new CultureInfo("uk-UA")
                            };
                        }

                        if (ScheduleGrid.Columns[5] is DataGridTextColumn endColumn)
                        {
                            endColumn.Binding = new Binding("EndDate")
                            {
                                StringFormat = "dd.MM.yyyy HH:mm",
                                ConverterCulture = new CultureInfo("uk-UA")
                            };
                        }
                        ScheduleGrid.Columns[6].Visibility = Visibility.Hidden;
                        ScheduleGrid.Columns[7].Visibility = Visibility.Hidden;
                        ScheduleGrid.Columns[8].Visibility = Visibility.Hidden;
                        ScheduleGrid.Columns[9].Visibility = Visibility.Hidden;

                        ScheduleGrid.Columns[0].IsReadOnly = true;
                        ScheduleGrid.Columns[1].IsReadOnly = true;
                        ScheduleGrid.Columns[2].IsReadOnly = true;
                        ScheduleGrid.Columns[3].IsReadOnly = true;

                        ScheduleGrid.Items.Refresh();
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
                    RequestRoutes();
                }
            }
            else if (tableRequestNumber == 1)
            {
                try
                {
                    using var document = JsonDocument.Parse(message);
                    var root = document.RootElement;

                    routes = root.EnumerateArray().Select(y => new Route
                    {
                        RouteId = y.GetProperty("routeId").GetString(),
                        FirstStopId = y.GetProperty("firstStopId").GetString(),
                        LastStopId = y.GetProperty("lastStopId").GetString(),
                        NumberOfStops = y.GetProperty("numberOfStops").GetInt32(),
                        Length = y.GetProperty("length").GetDecimal(),
                        Type = y.GetProperty("type").GetString(),
                        Price = y.GetProperty("price").GetDecimal(),
                        Capacity = y.GetProperty("capacity").GetInt32(),
                        MapUrl = y.GetProperty("mapUrl").GetString(),
                        Notes = y.GetProperty("notes").GetString()
                    }).ToList();

                    RoutesCombobox.SelectionChanged -= Combobox_SelectionChanged;
                    RoutesCombobox.Items.Clear();
                    RoutesCombobox.Items.Add(new { RouteId = (string)null, Display = "Всі" });
                    foreach (Route route in routes)
                    {
                        RoutesCombobox.Items.Add(new { RouteId = route.RouteId, Display = route.RouteId });
                    }                    
                    RoutesCombobox.SelectedValuePath = "RouteId";
                    RoutesCombobox.SelectedIndex = 0;
                    RoutesCombobox.SelectionChanged += Combobox_SelectionChanged;
                }
                catch (Exception ex)
                {
                    FailSelect(sender, ex.Message);
                }
                finally
                {
                    tableRequestNumber++;
                    RequestVehicles();
                }
            }
            else if (tableRequestNumber == 2)
            {
                try
                {
                    using var document = JsonDocument.Parse(message);
                    var root = document.RootElement;

                    vehicles = root.EnumerateArray().Select(y => new Vehicle
                    {
                        VehicleId = y.GetProperty("vehicleId").GetString(),
                        VehicleTypeId = y.GetProperty("vehicleTypeId").GetString(),
                        DepotId = y.GetProperty("depotId").GetString(),
                        IsActive = y.GetProperty("isActive").GetString(),
                        BeginOperationYear = y.GetProperty("beginOperationYear").GetInt32(),
                        GalleryUrl = y.GetProperty("galleryUrl").GetString(),
                        Notes = y.GetProperty("notes").GetString()
                    }).ToList();

                    VehiclesCombobox.SelectionChanged -= Combobox_SelectionChanged;
                    VehiclesCombobox.Items.Clear();
                    VehiclesCombobox.Items.Add(new { VehicleId = (string)null, Display = "Всі" });
                    foreach (Vehicle vehicle in vehicles)
                    {
                        VehiclesCombobox.Items.Add(new { VehicleId = vehicle.VehicleId, Display = vehicle.VehicleId });
                    }
                    VehiclesCombobox.SelectedValuePath = "VehicleId";
                    VehiclesCombobox.SelectedIndex = 0;
                    VehiclesCombobox.SelectionChanged += Combobox_SelectionChanged;
                }
                catch (Exception ex)
                {
                    FailSelect(sender, ex.Message);
                }
                finally
                {
                    tableRequestNumber++;
                    RequestDrivers();
                }
            }
            else if (tableRequestNumber == 3)
            {
                try
                {
                    using var document = JsonDocument.Parse(message);
                    var root = document.RootElement;                    

                    drivers = root.EnumerateArray().Select(y => new Driver
                    {
                        PersonId = y.GetProperty("personId").GetString(),
                        DepotId = y.GetProperty("depotId").GetString(),
                        Position = y.GetProperty("position").GetString(),
                        EmploymentDate = DateOnly.Parse(y.GetProperty("employmentDate").GetString()),
                        Notes = y.GetProperty("notes").GetString(),
                    }).ToList();

                    DriversCombobox.SelectionChanged -= Combobox_SelectionChanged;                                  
                    DriversCombobox.Items.Clear();
                    DriversCombobox.Items.Add(new { PersonId = (string)null, Display = "Всі" });
                    foreach (Driver driver in drivers)
                    {
                        DriversCombobox.Items.Add(new { PersonId = driver.PersonId, Display = driver.PersonId });
                    }
                    DriversCombobox.SelectedValuePath = "PersonId";
                    DriversCombobox.SelectedIndex = 0;
                    DriversCombobox.SelectionChanged += Combobox_SelectionChanged;
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
            ScheduleGrid.SelectedItem = null;
            ScheduleGrid.SelectedCells.Clear();
            ScheduleGrid.IsEnabled = true;

            EditButton.IsEnabled = true;
            AddButton.IsEnabled = true;
            RemoveButton.IsEnabled = true;
            FilterGrid.IsEnabled = true;

            RoutesCombobox.SelectedIndex = 0;
            VehiclesCombobox.SelectedIndex = 0;
            DriversCombobox.SelectedIndex = 0;

            EditButton_Click(sender, new RoutedEventArgs());
            RequestSchedule();
        }

        private void FailEdit(object sender, string message)
        {
            MessageBox.Show($"Не вдалося зберегти зміни.\nПомилка: {message}", "Розклад", MessageBoxButton.OK, MessageBoxImage.Error);

            ScheduleGrid.SelectedItem = null;
            ScheduleGrid.SelectedCells.Clear();
            ScheduleGrid.IsEnabled = true;         

            EditButton.IsEnabled = true;
            AddButton.IsEnabled = true;
            RemoveButton.IsEnabled = true;
            FilterGrid.IsEnabled = true;

            RoutesCombobox.SelectedIndex = 0;
            VehiclesCombobox.SelectedIndex = 0;
            DriversCombobox.SelectedIndex = 0;

            EditButton_Click(sender, new RoutedEventArgs());
            RequestSchedule();
        }

        private void RequestSchedule()
        {
            client.Transmit($"SELECT~SCHEDULE~ALLSCHEDULES~");
        }

        private void RequestRoutes()
        {
            client.Transmit($"SELECT~ROUTES~MYDEPOTROUTES~");
        }

        private void RequestVehicles()
        {
            client.Transmit($"SELECT~VEHICLES~MYDEPOTVEHICLES~");
        }

        private void RequestDrivers()
        {
            client.Transmit($"SELECT~DRIVERS~MYDEPOTDRIVERS~");
        }        

        private void Combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateGrid();
        }

        private void UpdateGrid()
        {
            var selectedRouteId = RoutesCombobox.SelectedValue as string;
            var selectedVehicleId = VehiclesCombobox.SelectedValue as string;
            var selectedDriverId = DriversCombobox.SelectedValue as string;

            var collectionView = CollectionViewSource.GetDefaultView(ScheduleGrid.ItemsSource);
            if (collectionView != null)
            {
                collectionView.Filter = obj =>
                {
                    if (!(obj is Schedule schedule))
                        return false;

                    bool routeMatch = string.IsNullOrEmpty(selectedRouteId) || schedule.RouteId == selectedRouteId;
                    bool vehicleMatch = string.IsNullOrEmpty(selectedVehicleId) || schedule.VehicleId == selectedVehicleId;
                    bool driverMatch = string.IsNullOrEmpty(selectedDriverId) || schedule.DriverId == selectedDriverId;

                    return routeMatch && vehicleMatch && driverMatch;
                };
            }

            ScheduleGrid.Columns[0].Header = "ID";
            ScheduleGrid.Columns[1].Header = "ID маршруту";
            ScheduleGrid.Columns[2].Header = "ID транспорту";
            ScheduleGrid.Columns[3].Header = "ID водія";
            ScheduleGrid.Columns[4].Header = "Початок";
            ScheduleGrid.Columns[5].Header = "Завершення";

            ScheduleGrid.Columns[6].Visibility = Visibility.Hidden;
            ScheduleGrid.Columns[7].Visibility = Visibility.Hidden;
            ScheduleGrid.Columns[8].Visibility = Visibility.Hidden;

            ScheduleGrid.Columns[0].IsReadOnly = true;
            ScheduleGrid.Columns[1].IsReadOnly = true;
            ScheduleGrid.Columns[2].IsReadOnly = true;
            ScheduleGrid.Columns[3].IsReadOnly = true;

            ScheduleGrid.Items.Refresh();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataService.GenerateReport($"Розклад", ScheduleGrid);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не вдалося зберегти звіт.\nПомилка: {ex.Message}", "Розклад", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            UnsubscribeEventHandlers();
            Unloaded -= Page_Unloaded;
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            ScheduleGrid.IsReadOnly = !ScheduleGrid.IsReadOnly;            

            AddButton.IsEnabled = !AddButton.IsEnabled;
            RemoveButton.IsEnabled = !RemoveButton.IsEnabled;           

            AddPopupGrid.Visibility = Visibility.Collapsed;            
            PrimaryKeyTextbox.Text = string.Empty;

            RemovePopupGrid.Visibility = Visibility.Collapsed;
            ConfirmButton.Visibility = Visibility.Collapsed;
        }

        private void ScheduleGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            var currentCell = ScheduleGrid.CurrentCell;

            if (currentCell != null && currentCell.Item != null && currentCell.Item != DependencyProperty.UnsetValue)
            {
                if (_lastSelectedCell.HasValue &&
                    _lastSelectedCell.Value.Item == currentCell.Item &&
                    _lastSelectedCell.Value.Column == currentCell.Column)
                {
                    return;
                }

                _lastSelectedCell = currentCell;

                ScheduleGrid.SelectedItem = null;
                ScheduleGrid.SelectedCells.Clear();
                ScheduleGrid.IsEnabled = true;                

                if (!ScheduleGrid.IsReadOnly)
                {
                    EditButton_Click(sender, new RoutedEventArgs());
                }

                Schedule selectedSchedule = (Schedule)currentCell.Item;

                KeyLabel.Content = selectedSchedule.ScheduleId;               

                KeyInformationLabel.Visibility = Visibility.Visible;               
                KeyLabel.Visibility = Visibility.Visible;          
                EditInformationLabel.Visibility = Visibility.Visible;
                EditButton.Visibility = Visibility.Visible;
                AddButton.Visibility = Visibility.Visible;
                RemoveButton.Visibility = Visibility.Visible;
                FilterGrid.Visibility = Visibility.Visible;

                AddPopupGrid.IsEnabled = false;
                RemovePopupGrid.IsEnabled = false;

                AddPopupGrid.Visibility = Visibility.Collapsed;
                RemovePopupGrid.Visibility = Visibility.Collapsed;
                ConfirmButton.Visibility = Visibility.Collapsed;
            }
        }
             

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddButton.Visibility = Visibility.Collapsed;
            RemoveButton.Visibility = Visibility.Collapsed;

            PrimaryKeyInformationLabel.Visibility = Visibility.Visible;
            PrimaryKeyTextbox.Visibility = Visibility.Visible;
            PrimaryKeyTextbox.IsEnabled = true;
            firstKeyReady = false;

            MinInformationLabel.Content = "Дата початку:";
            MinInformationLabel.Visibility = Visibility.Visible;
            MinDatePicker.Visibility = Visibility.Visible;
            MinDatePicker.IsEnabled = true;
            secondKeyReady = false;

            MaxInformationLabel.Content = "Дата завершення:";
            MaxInformationLabel.Visibility = Visibility.Visible;
            MaxDatePicker.Visibility = Visibility.Visible;
            MaxDatePicker.IsEnabled = true;
            tertiaryKeyReady = false;

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
            else if (ScheduleGrid.Items.Cast<Schedule>().Any(s => s.ScheduleId == PrimaryKeyTextbox.Text))
            {
                PrimaryKeyTextbox.SetValue(TextboxStyleHelper.IsTextValidProperty, false);
                firstKeyReady = false;
            }
            else
            {
                PrimaryKeyTextbox.SetValue(TextboxStyleHelper.IsTextValidProperty, true);
                firstKeyReady = true;
            }

            if (firstKeyReady && secondKeyReady && tertiaryKeyReady && RoutesCombobox.SelectedIndex > 0 && VehiclesCombobox.SelectedIndex > 0 && DriversCombobox.SelectedIndex > 0)
            {
                ConfirmButton.IsEnabled = true;
            }
            else
            {
                ConfirmButton.IsEnabled = false;
            }
        }

        private void MinDatePicker_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!IsInitialized)
                return;

            if (MinDatePicker.Value >= DateTime.Now)
            {
                secondKeyReady = true;
            }
            else
            {
                secondKeyReady = false;
            }

            if (firstKeyReady && secondKeyReady && tertiaryKeyReady && RoutesCombobox.SelectedIndex > 0 && VehiclesCombobox.SelectedIndex > 0 && DriversCombobox.SelectedIndex > 0)
            {
                ConfirmButton.IsEnabled = true;
            }
            else
            {
                ConfirmButton.IsEnabled = false;
            }
        }

        private void MaxDatePicker_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!IsInitialized)
                return;

            if (MaxDatePicker.Value >= MinDatePicker.Value)
            {
                tertiaryKeyReady = true;
            }
            else
            {
                tertiaryKeyReady = false;
            }

            if (firstKeyReady && secondKeyReady && tertiaryKeyReady && RoutesCombobox.SelectedIndex > 0 && VehiclesCombobox.SelectedIndex > 0 && DriversCombobox.SelectedIndex > 0)
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
            MinDatePicker.IsEnabled = false;
            MaxDatePicker.IsEnabled = false;

            if (RemovePopupGrid.IsVisible)
            {
                client.Transmit($"EDIT~REMOVESCHEDULE~{KeyLabel.Content}~");
            }
            else
            {
                Schedule newSchedule = new Schedule
                {
                    ScheduleId = PrimaryKeyTextbox.Text,
                    RouteId = (string)RoutesCombobox.SelectedValue,
                    VehicleId = (string)VehiclesCombobox.SelectedValue,
                    DriverId = (string)DriversCombobox.SelectedValue,
                    StartDate = (DateTime)MinDatePicker.Value,
                    EndDate = (DateTime)MaxDatePicker.Value 
                };

                var schedules = ScheduleGrid.ItemsSource as List<Schedule>;
                if (schedules != null)
                {
                    foreach (var existing in schedules)
                    {                      
                        if (existing.ScheduleId == newSchedule.ScheduleId)
                            continue;

                        bool overlaps = newSchedule.StartDate <= existing.EndDate && newSchedule.EndDate >= existing.StartDate;

                        if (overlaps)
                        {                      
                            if (existing.DriverId == newSchedule.DriverId)
                            {
                                string errorMessage = $"Водій {newSchedule.DriverId} вже зайнятий у період {existing.StartDate:dd.MM.yyyy HH:mm} - {existing.EndDate:dd.MM.yyyy HH:mm}.";
                                FailEdit(this, errorMessage);
                                PrimaryKeyTextbox.IsEnabled = true;
                                MinDatePicker.IsEnabled = true;
                                MaxDatePicker.IsEnabled = true;
                                return;
                            }
                            
                            if (existing.VehicleId == newSchedule.VehicleId)
                            {
                                string errorMessage = $"Транспорт {newSchedule.VehicleId} вже зайнятий у період {existing.StartDate:dd.MM.yyyy HH:mm} - {existing.EndDate:dd.MM.yyyy HH:mm}.";
                                FailEdit(this, errorMessage);
                                PrimaryKeyTextbox.IsEnabled = true;
                                MinDatePicker.IsEnabled = true;
                                MaxDatePicker.IsEnabled = true;
                                return;
                            }
                        }
                    }
                }

                string jsonObject = JsonSerializer.Serialize(newSchedule);                

                client.Transmit($"EDIT~ADDSCHEDULE~{jsonObject}~");
            }

            PrimaryKeyTextbox.IsEnabled = true;
            MinDatePicker.IsEnabled = true;
            MaxDatePicker.IsEnabled = true;
        }        
    }
}
