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
    public partial class RoutePage : Page
    {
        private readonly Client client;
        private int tableRequestNumber;
        private DataService.TableName focusedTable;
        private DataGridCellInfo? _lastSelectedCell;

        private bool isFirstTableRequested;

        private bool firstKeyReady;
        private bool secondKeyReady;
        private bool tertiaryKeyReady;

        public RoutePage(Client client)
        {
            this.client = client;
            this.tableRequestNumber = 0;
            this.focusedTable = DataService.TableName.NONE;

            this.isFirstTableRequested = false;          

            this.firstKeyReady = false;
            this.secondKeyReady = false;
            this.tertiaryKeyReady = false;

            InitializeComponent();
            SubscribeEventHandlers();      
            RequestRoutes();

            KeyInformationLabel.Visibility = Visibility.Hidden;
            TableInformationLabel.Visibility = Visibility.Hidden;
            EditInformationLabel.Visibility = Visibility.Hidden;

            KeyLabel.Visibility = Visibility.Hidden;
            TableLabel.Visibility = Visibility.Hidden;
            EditButton.Visibility = Visibility.Hidden;
            AddButton.Visibility = Visibility.Hidden;
            RemoveButton.Visibility = Visibility.Hidden;
        }

        ~RoutePage()
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
            if (message.ToUpper() == DataService.TableName.ROUTES.ToString()
                || message.ToUpper() == DataService.TableName.STOPS.ToString())
            {              
                RequestRoutes();
            }
        }

        private void PassSelect(object sender, string message)
        {
            if (!isFirstTableRequested)
            {
                try
                {
                    List<Route> Routes = System.Text.Json.JsonSerializer.Deserialize<List<Route>>(message);

                    if (Routes != null)
                    {                       
                        RoutesGrid.ItemsSource = Routes;

                        RoutesGrid.Columns[0].Header = "ID";
                        RoutesGrid.Columns[1].Header = "ID першої зупинки";
                        RoutesGrid.Columns[2].Header = "ID останньої зупинки";
                        RoutesGrid.Columns[3].Header = "К-ть зупинок";
                        RoutesGrid.Columns[4].Header = "Довжина (км)";
                        RoutesGrid.Columns[5].Header = "Тип";
                        RoutesGrid.Columns[6].Header = "Ціна (грн)";
                        RoutesGrid.Columns[7].Header = "Пасажиромісткість";
                        RoutesGrid.Columns[8].Header = "Посилання на мапу";
                        RoutesGrid.Columns[9].Header = "Нотатки";

                        RoutesGrid.Columns[10].Visibility = Visibility.Hidden;
                        RoutesGrid.Columns[11].Visibility = Visibility.Hidden;
                        RoutesGrid.Columns[12].Visibility = Visibility.Hidden;

                        RoutesGrid.Columns[0].IsReadOnly = true;
                        RoutesGrid.Columns[1].IsReadOnly = true;
                        RoutesGrid.Columns[2].IsReadOnly = true;

                        RoutesGrid.Items.Refresh();
                    }
                    else
                    {
                        FailSelect(sender, message);
                    }
                }
                catch (Exception ex)
                {
                    FailSelect(sender, message);
                }
                finally
                {
                    isFirstTableRequested = true;
                    RequestStops();
                }
            }
            else
            {
                try
                {
                    List<Stop> Stops = System.Text.Json.JsonSerializer.Deserialize<List<Stop>>(message);

                    if (Stops != null)
                    {               
                        StopsGrid.ItemsSource = Stops;

                        StopsGrid.Columns[0].Header = "ID";
                        StopsGrid.Columns[1].Header = "Район";
                        StopsGrid.Columns[2].Header = "Назва";
                        StopsGrid.Columns[3].Header = "Тип";
                        StopsGrid.Columns[4].Header = "Оператор";

                        StopsGrid.Columns[5].Visibility = Visibility.Hidden;
                        StopsGrid.Columns[6].Visibility = Visibility.Hidden;

                        StopsGrid.Columns[0].IsReadOnly = true;

                        RoutesGrid.Items.Refresh();
                        StopsGrid.Items.Refresh();
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
            //MessageBox.Show($"Не вдалося відобразити інформацію.\nПомилка: {message}", "Маршрути", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void PassEdit(object sender, string message)
        {
            RoutesGrid.SelectedItem = null;
            RoutesGrid.SelectedCells.Clear();
            RoutesGrid.IsEnabled = true;

            StopsGrid.SelectedItem = null;
            StopsGrid.SelectedCells.Clear();
            StopsGrid.IsEnabled = true;          

            EditButton.IsEnabled = true;
            AddButton.IsEnabled = true;
            RemoveButton.IsEnabled = true;

            EditButton_Click(sender, new RoutedEventArgs());        
            RequestRoutes();
        }

        private void FailEdit(object sender, string message)
        {
            MessageBox.Show($"Не вдалося зберегти зміни.\nПомилка: {message}", "Маршрути", MessageBoxButton.OK, MessageBoxImage.Error);

            RoutesGrid.SelectedItem = null;
            RoutesGrid.SelectedCells.Clear();
            RoutesGrid.IsEnabled = true;

            StopsGrid.SelectedItem = null;
            StopsGrid.SelectedCells.Clear();
            StopsGrid.IsEnabled = true;

            EditButton.IsEnabled = true;
            AddButton.IsEnabled = true;
            RemoveButton.IsEnabled = true;

            EditButton_Click(sender, new RoutedEventArgs());         
            RequestRoutes();
        }

        private async void RequestRoutes()
        {
            client.Transmit($"SELECT~ROUTES~ALLROUTES~");
        }

        private async void RequestStops()
        {
            client.Transmit($"SELECT~STOPS~ALLSTOPS~");
        }        

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                switch (focusedTable)
                {
                    case DataService.TableName.ROUTES:
                        {
                            DataService.GenerateReport($"Перелік всіх маршрутів", RoutesGrid);
                            break;
                        }

                    case DataService.TableName.STOPS:
                        {
                            DataService.GenerateReport($"Перелік всіх зупинок", StopsGrid);
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
                MessageBox.Show($"Не вдалося зберегти звіт.\nПомилка: {ex.Message}", "Маршрути", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            UnsubscribeEventHandlers();
            Unloaded -= Page_Unloaded;
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            RoutesGrid.IsReadOnly = !RoutesGrid.IsReadOnly;
            StopsGrid.IsReadOnly = !StopsGrid.IsReadOnly;     

            AddButton.IsEnabled = !AddButton.IsEnabled;
            RemoveButton.IsEnabled = !RemoveButton.IsEnabled;

            AddPopupGrid.Visibility = Visibility.Collapsed;
            TertiaryKeyTextbox.Text = string.Empty;
            SecondaryKeyTextbox.Text = string.Empty;
            PrimaryKeyTextbox.Text = string.Empty;

            RemovePopupGrid.Visibility = Visibility.Collapsed;
            ConfirmButton.Visibility = Visibility.Collapsed;
        }

        private void RoutesGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            var currentCell = RoutesGrid.CurrentCell;

            if (currentCell != null && currentCell.Item != null && currentCell.Item != DependencyProperty.UnsetValue)
            {
                if (_lastSelectedCell.HasValue &&
                    _lastSelectedCell.Value.Item == currentCell.Item &&
                    _lastSelectedCell.Value.Column == currentCell.Column)
                {
                    return;
                }

                _lastSelectedCell = currentCell;

                RoutesGrid.SelectedItem = null;
                RoutesGrid.SelectedCells.Clear();
                RoutesGrid.IsEnabled = true;
                focusedTable = DataService.TableName.ROUTES;

                if (!RoutesGrid.IsReadOnly)
                {
                    EditButton_Click(sender, new RoutedEventArgs());
                }

                Route selectedRoute = (Route)currentCell.Item;

                KeyLabel.Content = selectedRoute.RouteId;
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

        private void StopsGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            var currentCell = StopsGrid.CurrentCell;

            if (currentCell != null && currentCell.Item != null && currentCell.Item != DependencyProperty.UnsetValue)
            {
                if (_lastSelectedCell.HasValue &&
                    _lastSelectedCell.Value.Item == currentCell.Item &&
                    _lastSelectedCell.Value.Column == currentCell.Column)
                {
                    return;
                }

                _lastSelectedCell = currentCell;

                StopsGrid.SelectedItem = null;
                StopsGrid.SelectedCells.Clear();
                StopsGrid.IsEnabled = true;
                focusedTable = DataService.TableName.STOPS;

                if (!StopsGrid.IsReadOnly)
                {
                    EditButton_Click(sender, new RoutedEventArgs());
                }

                Stop selectedStop = (Stop)currentCell.Item;

                KeyLabel.Content = selectedStop.StopId;
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
        
        private void RoutesGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            AddButton.IsEnabled = false;
            RemoveButton.IsEnabled = false;
            EditButton.IsEnabled = false;

            var selectedRoute = (Route)RoutesGrid.CurrentCell.Item;
            if (selectedRoute == null) return;

            Route newRoute = new Route
            {
                RouteId = selectedRoute.RouteId,
                FirstStopId = selectedRoute.FirstStopId,
                LastStopId = selectedRoute.LastStopId,
                NumberOfStops = selectedRoute.NumberOfStops,
                Length = selectedRoute.Length,
                Type = selectedRoute.Type,
                Price = selectedRoute.Price,
                Capacity = selectedRoute.Capacity,
                MapUrl = selectedRoute.MapUrl,
                Notes = selectedRoute.Notes
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
                        case nameof(Route.NumberOfStops):
                            if (int.TryParse(newValue, out int number) && number > 2)
                            {
                                newRoute.NumberOfStops = number;
                            }
                            else
                            {
                                MessageBox.Show("Введіть коректну кількість зупинок.\nКількість зупинок має бути більша за 2", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                            break;
                        case nameof(Route.Length):
                            if (decimal.TryParse(newValue.Replace('.', ','), out decimal length) && length > 0)
                            {
                                newRoute.Length = length;
                            }
                            else
                            {
                                MessageBox.Show("Введіть коректну довжину маршруту.\nДовжина має бути більшою за 0 км", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                            break;
                        case nameof(Route.Type):
                            newRoute.Type = newValue;
                            break;
                        case nameof(Route.Price):
                            if (decimal.TryParse(newValue.Replace('.', ','), out decimal price) && price > 0)
                            {
                                newRoute.Price = price;
                            }
                            else
                            {
                                MessageBox.Show("Введіть коректну ціну проїзду.\nЦіна має бути більшою за 0 грн", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                            break;
                        case nameof(Route.Capacity):
                            if (int.TryParse(newValue, out int capacity) && capacity > 0)
                            {
                                newRoute.Capacity = capacity;
                            }
                            else
                            {
                                MessageBox.Show("Введіть коректну кількість пасажиромісткості.\nПасажиромісткість має бути більшю за 0", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                            break;
                        case nameof(Route.MapUrl):
                            newRoute.MapUrl = newValue;
                            break;
                        case nameof(Route.Notes):
                            newRoute.Notes = newValue;
                            break;
                    }
                }
            }

            string jsonObject = JsonSerializer.Serialize(newRoute);
            if (newRoute != null)
            {
                client.Transmit($"EDIT~UPDATEROUTE~{newRoute.RouteId}~{jsonObject}~");
            }
        }

        private void StopsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            AddButton.IsEnabled = false;
            RemoveButton.IsEnabled = false;
            EditButton.IsEnabled = false;

            var selectedStop = (Stop)StopsGrid.CurrentCell.Item;
            if (selectedStop == null) return;

            Stop newStop = new Stop
            {
                StopId = selectedStop.StopId,
                District = selectedStop.District,
                Name = selectedStop.Name,
                Type = selectedStop.Type,
                Operator = selectedStop.Operator
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
                        case nameof(Stop.District):
                            newStop.District = newValue;
                            break;
                        case nameof(Stop.Name):
                            newStop.Name = newValue;
                            break;
                        case nameof(Stop.Type):
                            newStop.Type = newValue;
                            break;
                        case nameof(Stop.Operator):
                            newStop.Operator = newValue;
                            break;                        
                    }
                }
            }

            string jsonObject = JsonSerializer.Serialize(newStop);
            if (newStop != null)
            {
                client.Transmit($"EDIT~UPDATESTOP~{newStop.StopId}~{jsonObject}~");
            }
        }        

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddButton.Visibility = Visibility.Collapsed;
            RemoveButton.Visibility = Visibility.Collapsed;

            switch (focusedTable)
            {
                case DataService.TableName.ROUTES:
                    {
                        PrimaryKeyInformationLabel.Visibility = Visibility.Visible;
                        PrimaryKeyTextbox.Visibility = Visibility.Visible;
                        PrimaryKeyTextbox.IsEnabled = true;
                        firstKeyReady = false;

                        SecondaryKeyInformationLabel.Content = "Ключ першої зупинки:";
                        SecondaryKeyInformationLabel.Visibility = Visibility.Visible;
                        SecondaryKeyTextbox.Visibility = Visibility.Visible;
                        SecondaryKeyTextbox.IsEnabled = true;
                        secondKeyReady = false;

                        TertiaryKeyInformationLabel.Content = "Ключ останньої зупинки:";
                        TertiaryKeyInformationLabel.Visibility = Visibility.Visible;
                        TertiaryKeyTextbox.Visibility = Visibility.Visible;
                        TertiaryKeyTextbox.IsEnabled = true;
                        tertiaryKeyReady = false;

                        break;
                    }

                case DataService.TableName.STOPS:
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
            else if (focusedTable == DataService.TableName.ROUTES && PrimaryKeyTextbox.Text.Length > 4)
            {
                PrimaryKeyTextbox.SetValue(TextboxStyleHelper.IsTextValidProperty, false);
                firstKeyReady = false;
            }               
            else if ((focusedTable == DataService.TableName.ROUTES && RoutesGrid.Items.Cast<Route>().Any(r => r.RouteId == PrimaryKeyTextbox.Text))
                || (focusedTable == DataService.TableName.STOPS && StopsGrid.Items.Cast<Stop>().Any(s => s.StopId == PrimaryKeyTextbox.Text)))
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
            else if (focusedTable == DataService.TableName.ROUTES && !StopsGrid.Items.Cast<Stop>().Any(r => r.StopId == SecondaryKeyTextbox.Text))
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
            if (TertiaryKeyTextbox.Text.Length == 0)
            {
                TertiaryKeyTextbox.SetValue(TextboxStyleHelper.IsTextValidProperty, false);
                tertiaryKeyReady = false;
            }
            else if (focusedTable == DataService.TableName.ROUTES && !StopsGrid.Items.Cast<Stop>().Any(r => r.StopId == TertiaryKeyTextbox.Text))
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
                case DataService.TableName.ROUTES:
                    {
                        if (RemovePopupGrid.IsVisible)
                        {
                            client.Transmit($"EDIT~REMOVEROUTE~{KeyLabel.Content}~");
                        }
                        else
                        {
                            Route newRoute = new Route
                            {
                                RouteId = PrimaryKeyTextbox.Text,
                                FirstStopId = SecondaryKeyTextbox.Text,
                                LastStopId = TertiaryKeyTextbox.Text
                            };
                            string jsonObject = JsonSerializer.Serialize(newRoute);

                            client.Transmit($"EDIT~ADDROUTE~{jsonObject}~");
                        }
                        break;
                    }

                case DataService.TableName.STOPS:
                    {
                        if (RemovePopupGrid.IsVisible)
                        {
                            client.Transmit($"EDIT~REMOVESTOP~{KeyLabel.Content}~");
                        }
                        else
                        {
                            Stop newStop = new Stop
                            {
                                StopId = PrimaryKeyTextbox.Text                               
                            };
                            string jsonObject = JsonSerializer.Serialize(newStop);

                            client.Transmit($"EDIT~ADDSTOP~{jsonObject}~");
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
