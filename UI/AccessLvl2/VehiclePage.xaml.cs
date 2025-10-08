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
    public partial class VehiclePage : Page
    {
        private readonly Client client;
        private Depot depot;

        private bool isDepotRequested;
        private bool isProgrammaticCheckChange;

        public VehiclePage(Client client)
        {
            this.client = client;
            this.isDepotRequested = false;
            this.isProgrammaticCheckChange = false;

            InitializeComponent();
            SubscribeEventHandlers();
            RequestDepot();

            KeyInformationLabel.Visibility = Visibility.Hidden;
            GalleryUrlInformationLabel.Visibility = Visibility.Hidden;
            NotesInformationLabel.Visibility = Visibility.Hidden;
            EditInformationLabel.Visibility = Visibility.Hidden;

            KeyLabel.Visibility = Visibility.Hidden;
            IsActiveCheckbox.Visibility = Visibility.Hidden;
            GalleryUrlTextbox.Visibility = Visibility.Hidden;
            NotesTextbox.Visibility = Visibility.Hidden;
            EditButton.Visibility = Visibility.Hidden;
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

                    var vehicles = root.EnumerateArray().Select(v => new
                    {
                        VehicleId = v.GetProperty("vehicleId").GetString(),
                        VehicleTypeId = v.GetProperty("vehicleTypeId").GetString(),
                        DepotId = v.GetProperty("depotId").GetString(),
                        Make = v.GetProperty("make").GetString(),
                        Model = v.GetProperty("model").GetString(),
                        IsActive = v.GetProperty("isActive").GetString(),
                        BeginOperationYear = v.GetProperty("beginOperationYear").GetInt32(),
                        GalleryUrl = v.GetProperty("galleryUrl").GetString(),
                        Notes = v.GetProperty("notes").GetString(),
                        Type = v.GetProperty("type").GetString(),
                        ManufactureCountry = v.GetProperty("manufactureCountry").GetString(),
                        ModelYear = v.GetProperty("modelYear").GetInt32(),                       
                        EngineId = v.GetProperty("engineId").GetString(),
                        Capacity = v.GetProperty("capacity").GetInt32()
                    }).ToList();

                    VehiclesGrid.ItemsSource = vehicles;

                    VehiclesGrid.Columns[0].Header = "ID транспорту";
                    VehiclesGrid.Columns[1].Header = "ID виду транспорту";
                    VehiclesGrid.Columns[2].Header = "ID депо";
                    VehiclesGrid.Columns[3].Header = "Марка";
                    VehiclesGrid.Columns[4].Header = "Модель";
                    VehiclesGrid.Columns[5].Header = "Активний";
                    VehiclesGrid.Columns[6].Header = "Початок операції";
                    VehiclesGrid.Columns[7].Header = "Посилання на галерею";
                    VehiclesGrid.Columns[8].Header = "Нотатки";
                    VehiclesGrid.Columns[9].Header = "Тип";
                    VehiclesGrid.Columns[10].Header = "Виробник";
                    VehiclesGrid.Columns[11].Header = "Модельний рік";
                    VehiclesGrid.Columns[12].Header = "ID двигуна";
                    VehiclesGrid.Columns[13].Header = "Пасажиромісткість";

                    VehiclesGrid.Columns[0].Visibility = Visibility.Collapsed;
                    VehiclesGrid.Columns[1].Visibility = Visibility.Collapsed;
                    VehiclesGrid.Columns[2].Visibility = Visibility.Collapsed;
                    VehiclesGrid.Columns[7].Visibility = Visibility.Collapsed;
                    VehiclesGrid.Columns[8].Visibility = Visibility.Collapsed;
                    VehiclesGrid.Columns[12].Visibility = Visibility.Hidden;                  

                    VehiclesGrid.Items.Refresh();
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
            //MessageBox.Show($"Не вдалося відобразити інформацію транспорту депо.\nПомилка: {message}", "Транспорт", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void PassEdit(object sender, string message)
        {
            VehiclesGrid.SelectedItem = null;
            VehiclesGrid.SelectedCells.Clear();
            VehiclesGrid.IsEnabled = true;
            EditButton.IsEnabled = true;
            RequestDepot();
        }

        private void FailEdit(object sender, string message)
        {
            MessageBox.Show($"Не вдалося зберегти зміни.\nПомилка: {message}", "Транспорт", MessageBoxButton.OK, MessageBoxImage.Error);
            VehiclesGrid.SelectedItem = null;
            VehiclesGrid.SelectedCells.Clear();
            VehiclesGrid.IsEnabled = true;
            EditButton.IsEnabled = true;
            RequestDepot();
        }

        private void RequestDepot()
        {
            client.Transmit($"SELECT~DEPOTS~MYDEPOT~");
        }

        private void RequestVehicles()
        {
            client.Transmit($"SELECT~VEHICLES~MYDEPOTVEHICLES~");
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataService.GenerateReport($"Перелік транспорту депо '{depot.Name}'", VehiclesGrid);
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

        private void GalleryUrlTextbox_LostFocus(object sender, RoutedEventArgs e)
        {
            IsActiveCheckbox.IsEnabled = false;
            GalleryUrlTextbox.IsEnabled = false;
            NotesTextbox.IsEnabled = false;
            EditButton.IsEnabled = false;

            if (VehiclesGrid.SelectedItem is not null)
            {
                var selectedVehicle = VehiclesGrid.SelectedItem;
                string id = (selectedVehicle as dynamic)?.VehicleId;
                id = id.Replace("\"", "");

                Vehicle newVehicle = new Vehicle
                {
                    VehicleId = id,
                    VehicleTypeId = (selectedVehicle as dynamic)?.VehicleTypeId,
                    DepotId = (selectedVehicle as dynamic)?.DepotId,
                    IsActive = (selectedVehicle as dynamic)?.IsActive,
                    BeginOperationYear = (selectedVehicle as dynamic)?.BeginOperationYear,
                    GalleryUrl = GalleryUrlTextbox.Text,
                    Notes = (selectedVehicle as dynamic)?.Notes,
                };

                string jsonObject = JsonSerializer.Serialize(newVehicle);

                if (id != null)
                {
                    client.Transmit($"EDIT~UPDATEVEHICLE~{id}~{jsonObject}~");
                }
            }
        }

        private void NotesTextbox_LostFocus(object sender, RoutedEventArgs e)
        {
            IsActiveCheckbox.IsEnabled = false;
            GalleryUrlTextbox.IsEnabled = false;
            NotesTextbox.IsEnabled = false;
            EditButton.IsEnabled = false;

            if (VehiclesGrid.SelectedItem is not null)
            {
                var selectedVehicle = VehiclesGrid.SelectedItem;
                string id = (selectedVehicle as dynamic)?.VehicleId;
                id = id.Replace("\"", "");

                Vehicle newVehicle = new Vehicle
                {
                    VehicleId = id,
                    VehicleTypeId = (selectedVehicle as dynamic)?.VehicleTypeId,
                    DepotId = (selectedVehicle as dynamic)?.DepotId,
                    IsActive = (selectedVehicle as dynamic)?.IsActive,
                    BeginOperationYear = (selectedVehicle as dynamic)?.BeginOperationYear,
                    GalleryUrl = (selectedVehicle as dynamic)?.GalleryUrl,
                    Notes = NotesTextbox.Text,
                };

                string jsonObject = JsonSerializer.Serialize(newVehicle);

                if (id != null)
                {
                    client.Transmit($"EDIT~UPDATEVEHICLE~{id}~{jsonObject}~");
                }
            }
        }

        private void IsActiveCheckbox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (isProgrammaticCheckChange)
            {
                isProgrammaticCheckChange = false; 
                return; 
            }

            IsActiveCheckbox.IsEnabled = false;
            GalleryUrlTextbox.IsEnabled = false;
            NotesTextbox.IsEnabled = false;
            EditButton.IsEnabled = false;

            if (VehiclesGrid.SelectedItem is not null)
            {
                var selectedVehicle = VehiclesGrid.SelectedItem;
                string id = (selectedVehicle as dynamic)?.VehicleId;
                id = id.Replace("\"", "");

                Vehicle newVehicle = new Vehicle
                {
                    VehicleId = id,
                    VehicleTypeId = (selectedVehicle as dynamic)?.VehicleTypeId,
                    DepotId = (selectedVehicle as dynamic)?.DepotId,
                    IsActive = IsActiveCheckbox.IsChecked ?? false ? "Так" : "Ні",
                    BeginOperationYear = (selectedVehicle as dynamic)?.BeginOperationYear,
                    GalleryUrl = (selectedVehicle as dynamic)?.GalleryUrl,
                    Notes = (selectedVehicle as dynamic)?.Notes,
                };

                string jsonObject = JsonSerializer.Serialize(newVehicle);

                if (id != null)
                {
                    client.Transmit($"EDIT~UPDATEVEHICLE~{id}~{jsonObject}~");
                }
            }
        }        

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            IsActiveCheckbox.IsEnabled = !IsActiveCheckbox.IsEnabled;
            VehiclesGrid.IsEnabled = !VehiclesGrid.IsEnabled;
            GalleryUrlTextbox.IsEnabled = !GalleryUrlTextbox.IsEnabled;
            NotesTextbox.IsEnabled = !NotesTextbox.IsEnabled;
        }

        private void VehiclesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VehiclesGrid.SelectedItem is not null)
            {
                var selectedDriver = VehiclesGrid.SelectedItem;

                KeyLabel.Content = (selectedDriver as dynamic)?.VehicleId;
                isProgrammaticCheckChange = (selectedDriver as dynamic)?.IsActive == "Так" ? true : false;
                IsActiveCheckbox.IsChecked = (selectedDriver as dynamic)?.IsActive == "Так" ? true : false;
                GalleryUrlTextbox.Text = (selectedDriver as dynamic)?.GalleryUrl;
                NotesTextbox.Text = (selectedDriver as dynamic)?.Notes;

                KeyInformationLabel.Visibility = Visibility.Visible;
                GalleryUrlInformationLabel.Visibility = Visibility.Visible;
                NotesInformationLabel.Visibility = Visibility.Visible;
                EditInformationLabel.Visibility = Visibility.Visible;

                KeyLabel.Visibility = Visibility.Visible;
                IsActiveCheckbox.Visibility = Visibility.Visible;
                GalleryUrlTextbox.Visibility = Visibility.Visible;
                NotesTextbox.Visibility = Visibility.Visible;
                EditButton.Visibility = Visibility.Visible;
            }
            else
            {
                KeyInformationLabel.Visibility = Visibility.Hidden;
                GalleryUrlInformationLabel.Visibility = Visibility.Hidden;
                NotesInformationLabel.Visibility = Visibility.Hidden;
                EditInformationLabel.Visibility = Visibility.Hidden;

                KeyLabel.Visibility = Visibility.Hidden;
                IsActiveCheckbox.Visibility = Visibility.Hidden;
                GalleryUrlTextbox.Visibility = Visibility.Hidden;
                NotesTextbox.Visibility = Visibility.Hidden;
                EditButton.Visibility = Visibility.Hidden;
            }
        }        
    }
}
