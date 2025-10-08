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

namespace AppClient.UI.AccessLvl2
{
    public partial class RoutePage : Page
    {
        private readonly Client client;
        private Depot depot;

        private bool isDepotRequested;
        private bool isProgrammaticCheckChange;

        public RoutePage(Client client)
        {
            this.client = client;
            this.isDepotRequested = false;            

            InitializeComponent();
            SubscribeEventHandlers();
            RequestDepot();

            KeyInformationLabel.Visibility = Visibility.Hidden;
            MapUrlInformationLabel.Visibility = Visibility.Hidden;
            NotesInformationLabel.Visibility = Visibility.Hidden;
            EditInformationLabel.Visibility = Visibility.Hidden;

            KeyLabel.Visibility = Visibility.Hidden;       
            MapUrlTextbox.Visibility = Visibility.Hidden;
            NotesTextbox.Visibility = Visibility.Hidden;
            EditButton.Visibility = Visibility.Hidden;
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
            if (message.ToUpper() == DataService.TableName.VEHICLES.ToString())
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
                    RequestVehicles();
                }
            }
            else
            {
                try
                {                   
                    using var document = JsonDocument.Parse(message);
                    var root = document.RootElement;

                    var routes = root.EnumerateArray().Select(v => new
                    {
                        RouteId = v.GetProperty("routeId").GetString(),
                        FirstStopId = v.GetProperty("firstStopId").GetString(),
                        LastStopId = v.GetProperty("lastStopId").GetString(),
                        FirstStopName = v.GetProperty("firstStopName").GetString(),
                        LastStopName = v.GetProperty("lastStopName").GetString(),
                        NumberOfStops = v.GetProperty("numberOfStops").GetInt32(),
                        Length = v.GetProperty("length").GetDecimal(),
                        Type = v.GetProperty("type").GetString(),
                        Price = v.GetProperty("price").GetDecimal(),
                        Capacity = v.GetProperty("capacity").GetInt32(),
                        MapUrl = v.GetProperty("mapUrl").GetString(),
                        Notes = v.GetProperty("notes").GetString()
                    }).ToList();

                    RoutesGrid.ItemsSource = routes;

                    RoutesGrid.Columns[0].Header = "№";
                    RoutesGrid.Columns[1].Header = "ID першої зупинки";
                    RoutesGrid.Columns[2].Header = "ID останньої зупинки";
                    RoutesGrid.Columns[3].Header = "Перша зупинка";
                    RoutesGrid.Columns[4].Header = "Остання зупинка";
                    RoutesGrid.Columns[5].Header = "К-ть зупинок";
                    RoutesGrid.Columns[6].Header = "Довжина (км)";
                    RoutesGrid.Columns[7].Header = "Тип";
                    RoutesGrid.Columns[8].Header = "Ціна (грн)";
                    RoutesGrid.Columns[9].Header = "Пасажиромісткість";
                    RoutesGrid.Columns[10].Header = "Посилання на мапу";
                    RoutesGrid.Columns[11].Header = "Нотатки";                  
                   
                    RoutesGrid.Columns[1].Visibility = Visibility.Collapsed;
                    RoutesGrid.Columns[2].Visibility = Visibility.Collapsed;
                    RoutesGrid.Columns[10].Visibility = Visibility.Collapsed;
                    RoutesGrid.Columns[11].Visibility = Visibility.Collapsed;                  

                    RoutesGrid.Items.Refresh();
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
            //MessageBox.Show($"Не вдалося відобразити інформацію маршрутів депо.\nПомилка: {message}", "Маршрути", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void PassEdit(object sender, string message)
        {
            RoutesGrid.SelectedItem = null;
            RoutesGrid.SelectedCells.Clear();
            RoutesGrid.IsEnabled = true;
            EditButton.IsEnabled = true;
            RequestDepot();
        }

        private void FailEdit(object sender, string message)
        {
            MessageBox.Show($"Не вдалося зберегти зміни.\nПомилка: {message}", "Маршрути", MessageBoxButton.OK, MessageBoxImage.Error);
            RoutesGrid.SelectedItem = null;
            RoutesGrid.SelectedCells.Clear();
            RoutesGrid.IsEnabled = true;
            EditButton.IsEnabled = true;
            RequestDepot();
        }

        private void RequestDepot()
        {
            client.Transmit($"SELECT~DEPOTS~MYDEPOT~");
        }

        private void RequestVehicles()
        {
            client.Transmit($"SELECT~ROUTES~MYDEPOTROUTES~");
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataService.GenerateReport($"Перелік маршрутів депо '{depot.Name}'", RoutesGrid);
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

        private void MapUrlTextbox_LostFocus(object sender, RoutedEventArgs e)
        {        
            MapUrlTextbox.IsEnabled = false;
            NotesTextbox.IsEnabled = false;
            EditButton.IsEnabled = false;

            if (RoutesGrid.SelectedItem is not null)
            {
                var selectedRoute = RoutesGrid.SelectedItem;
                string id = (selectedRoute as dynamic)?.RouteId;
                id = id.Replace("\"", "");

                Route newRoute = new Route()
                {
                    RouteId = id,
                    FirstStopId = (selectedRoute as dynamic)?.FirstStopId,
                    LastStopId = (selectedRoute as dynamic)?.LastStopId,                   
                    NumberOfStops = (selectedRoute as dynamic)?.NumberOfStops,
                    Length = (selectedRoute as dynamic)?.Length,
                    Type = (selectedRoute as dynamic)?.Type,
                    Price = (selectedRoute as dynamic)?.Price,
                    Capacity = (selectedRoute as dynamic)?.Capacity,
                    MapUrl = MapUrlTextbox.Text,
                    Notes = (selectedRoute as dynamic)?.Notes
                };

                string jsonObject = JsonSerializer.Serialize(newRoute);

                if (id != null)
                {
                    client.Transmit($"EDIT~UPDATEROUTE~{id}~{jsonObject}~");
                }
            }
        }

        private void NotesTextbox_LostFocus(object sender, RoutedEventArgs e)
        {
            MapUrlTextbox.IsEnabled = false;
            NotesTextbox.IsEnabled = false;
            EditButton.IsEnabled = false;

            if (RoutesGrid.SelectedItem is not null)
            {
                var selectedRoute = RoutesGrid.SelectedItem;
                string id = (selectedRoute as dynamic)?.RouteId;
                id = id.Replace("\"", "");

                Route newRoute = new Route()
                {
                    RouteId = id,
                    FirstStopId = (selectedRoute as dynamic)?.FirstStopId,
                    LastStopId = (selectedRoute as dynamic)?.LastStopId,
                    NumberOfStops = (selectedRoute as dynamic)?.NumberOfStops,
                    Length = (selectedRoute as dynamic)?.Length,
                    Type = (selectedRoute as dynamic)?.Type,
                    Price = (selectedRoute as dynamic)?.Price,
                    Capacity = (selectedRoute as dynamic)?.Capacity,
                    MapUrl = (selectedRoute as dynamic)?.MapUrl,
                    Notes = NotesTextbox.Text
                };

                string jsonObject = JsonSerializer.Serialize(newRoute);

                if (id != null)
                {
                    client.Transmit($"EDIT~UPDATEROUTE~{id}~{jsonObject}~");
                }
            }
        }        

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {           
            RoutesGrid.IsEnabled = !RoutesGrid.IsEnabled;
            MapUrlTextbox.IsEnabled = !MapUrlTextbox.IsEnabled;
            NotesTextbox.IsEnabled = !NotesTextbox.IsEnabled;
        }

        private void VehiclesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RoutesGrid.SelectedItem is not null)
            {
                var selectedDriver = RoutesGrid.SelectedItem;

                KeyLabel.Content = (selectedDriver as dynamic)?.RouteId;               
                MapUrlTextbox.Text = (selectedDriver as dynamic)?.MapUrl;
                NotesTextbox.Text = (selectedDriver as dynamic)?.Notes;

                KeyInformationLabel.Visibility = Visibility.Visible;
                MapUrlInformationLabel.Visibility = Visibility.Visible;
                NotesInformationLabel.Visibility = Visibility.Visible;
                EditInformationLabel.Visibility = Visibility.Visible;

                KeyLabel.Visibility = Visibility.Visible;                
                MapUrlTextbox.Visibility = Visibility.Visible;
                NotesTextbox.Visibility = Visibility.Visible;
                EditButton.Visibility = Visibility.Visible;
            }
            else
            {
                KeyInformationLabel.Visibility = Visibility.Hidden;
                MapUrlInformationLabel.Visibility = Visibility.Hidden;
                NotesInformationLabel.Visibility = Visibility.Hidden;
                EditInformationLabel.Visibility = Visibility.Hidden;

                KeyLabel.Visibility = Visibility.Hidden;               
                MapUrlTextbox.Visibility = Visibility.Hidden;
                NotesTextbox.Visibility = Visibility.Hidden;
                EditButton.Visibility = Visibility.Hidden;
            }
        }
    }
}
