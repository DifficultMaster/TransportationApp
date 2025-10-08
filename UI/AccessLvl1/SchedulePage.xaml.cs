using AppClient.Data;
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
using System.Windows.Threading;

namespace AppClient.UI.AccessLvl1
{
    public partial class SchedulePage : Page
    {
        private readonly Client client;
        private DispatcherTimer timer;
        private DateTime beginTime;
        private DateTime endTime;

        public SchedulePage(Client client)
        {
            this.client = client;
            this.beginTime = DateTime.Now;
            this.endTime = DateTime.Now.AddHours(1);
            InitializeComponent();
            SubscribeEventHandlers();
            RequestRoute();

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += Timer_Tick;
            timer.Start();
            UpdateTime();
        }

        private void SubscribeEventHandlers()
        {
            client.Refresh += RefreshRoute;

            client.SelectSuccess += PassSelect;
            client.SelectFail += FailSelect;
        }

        private void UnsubscribeEventHandlers()
        {
            client.Refresh -= RefreshRoute;

            client.SelectSuccess -= PassSelect;
            client.SelectFail -= FailSelect;
        }

        private void RefreshRoute(object sender, string message)
        {
            if (message.ToUpper() == DataService.TableName.ROUTES.ToString())
            {
                RequestRoute();
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateTime();
        }        

        private void UpdateTime()
        {
            LivetimeLabel.Content = DateTime.Now.ToString("HH:mm");                   

            if (beginTime > DateTime.Now)
            {
                RemainingtimeInfoLabel.Content = "Початок:";
                RemainingtimeLabel.Content = $"{(beginTime - DateTime.Now).TotalMinutes:F0} хв.";                
            }
            else if (endTime > DateTime.Now)
            {
                RemainingtimeInfoLabel.Content = "Завершення:";
                RemainingtimeLabel.Content = $"{(endTime - DateTime.Now).TotalMinutes:F0} хв.";
            }
            else
            {
                RemainingtimeInfoLabel.Content = "Завершено:";
                RemainingtimeLabel.Content = $"{(DateTime.Now - endTime).TotalMinutes:F0} хв.";
            }

            RemainingtimeInfoLabel.Visibility = Visibility.Visible;
            RemainingtimeLabel.Visibility = Visibility.Visible;
        }

        private void RequestRoute()
        {
            client.Transmit($"SELECT~{DataService.TableName.ROUTES}~MYROUTE+~");
        }

        private void PassSelect(object sender, string message)
        {
            try
            {
                using var document = JsonDocument.Parse(message);
                var root = document.RootElement;

                var schedule = root.GetProperty("schedule");
                var route = root.GetProperty("route");
                var vehicle = root.GetProperty("vehicle");
                var vehicleType = root.GetProperty("vehicleType");
                var depot = root.GetProperty("depot");
                var firstStop = root.GetProperty("firstStop");
                var lastStop = root.GetProperty("lastStop");

                RouteLabel.Content = $"{route.GetProperty("type")} № {route.GetProperty("routeId")}";
                DepotLabel.Content = $"{depot.GetProperty("name")}";
                DistanceLabel.Content = $"{route.GetProperty("length")} км";

                VehicleNameLabel.Content = new TextBlock { Text = $"{vehicleType.GetProperty("make")} {vehicleType.GetProperty("model")}", TextWrapping = TextWrapping.WrapWithOverflow };
                VehicleIdLabel.Content = $"{vehicle.GetProperty("vehicleId")}";

                RoutestartLabel.Content = new TextBlock { Text = firstStop.GetProperty("name").GetString(), TextWrapping = TextWrapping.WrapWithOverflow };
                RouteendLabel.Content = new TextBlock { Text = lastStop.GetProperty("name").GetString(), TextWrapping = TextWrapping.WrapWithOverflow };
                RoutestopsLabel.Content = $"{route.GetProperty("numberOfStops")} зупинок";

                this.beginTime = DateTime.Parse(schedule.GetProperty("startDate").GetString());
                this.endTime = DateTime.Parse(schedule.GetProperty("endDate").GetString());

                if (route.TryGetProperty("mapUrl", out var mapUrlElement))
                {
                    string mapUrl = mapUrlElement.GetString();
                    if (!string.IsNullOrEmpty(mapUrl) && Uri.TryCreate(mapUrl, UriKind.Absolute, out var uri))
                    {
                        try
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.UriSource = uri;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            MapImage.Source = bitmap;
                        }
                        catch (Exception ex)
                        {
                            MapImage.Source = null;
                            MapImage.Tag = "Некоретне або відсутнє зображення.";
                        }
                    }
                    else
                    {
                        MapImage.Source = null;
                        MapImage.Tag = "Некоретне або відсутнє зображення.";
                    }
                }
                else
                {
                    MapImage.Source = null;
                    MapImage.Tag = "Зображення відсутнє.";
                }
            }
            catch (Exception ex)
            {
                FailSelect(sender, ex.Message);
            }
        }

        private void FailSelect(object sender, string message)
        {
            //MessageBox.Show($"Не вдалося відобразити інформацію маршруту.\nПомилка:{message}", "Маршрут", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {           
            UnsubscribeEventHandlers();
            Unloaded -= Page_Unloaded;
        }
    }
}
