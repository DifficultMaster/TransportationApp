using AppClient.Data;
using AppServer.Models;
using BD4Client.Network;
using Microsoft.EntityFrameworkCore.Storage;
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

namespace AppClient.UI.AccessLvl3
{
    public partial class SchedulePage : Page
    {
        private readonly Client client;
        private bool isInitializing = true;

        public SchedulePage(Client client)
        {
            this.client = client;
            InitializeComponent();
            SubscribeEventHandlers();

            this.LogsMinDatePicker.SelectedDate = DateTime.Now.AddMonths(-1);
            this.LogsMaxDatePicker.SelectedDate = DateTime.Now.AddDays(1);
            this.isInitializing = false;
            RequestTable();
        }

        ~SchedulePage()
        {
            UnsubscribeEventHandlers();
        }

        private void SubscribeEventHandlers()
        {
            client.Refresh += RefreshGrid;

            client.SelectSuccess += PassGrid;
            client.SelectFail += FailGrid;

            client.EditSuccess += PassEdit;
            client.EditFail += FailEdit;
        }

        private void UnsubscribeEventHandlers()
        {
            client.Refresh -= RefreshGrid;

            client.SelectSuccess -= PassGrid;
            client.SelectFail -= FailGrid;

            client.EditSuccess -= PassEdit;
            client.EditFail -= FailEdit;
        }

        private void RefreshGrid(object sender, string message)
        {
            if (message.ToUpper() == DataService.TableName.VALIDATIONS.ToString())
            {
                RequestTable();
            }
        }        

        private void PassGrid(object sender, string message)
        {
            LogsMinDatePicker.IsEnabled = true;
            LogsMaxDatePicker.IsEnabled = true;

            try
            {
                using var document = JsonDocument.Parse(message);
                var root = document.RootElement;

                var reports = root.EnumerateArray().Select(r => new
                {
                    RouteId = r.GetProperty("RouteId").GetString(),
                    RouteType = r.GetProperty("RouteType").GetString(),
                    ValidationCount = r.GetProperty("ValidationCount").GetInt32(),
                    Revenue = r.GetProperty("Revenue").GetDecimal(),
                    OperatingCost = r.GetProperty("OperatingCost").GetDecimal(),
                    Profit = r.GetProperty("Profit").GetDecimal(),
                    TotalHours = r.GetProperty("TotalHours").GetDouble()
                }).ToList();

                ValidationsGrid.ItemsSource = reports;

                ValidationsGrid.Columns[0].Header = "№ маршруту";
                ValidationsGrid.Columns[1].Header = "Тип маршруту";
                ValidationsGrid.Columns[2].Header = "К-ть валідацій";
                ValidationsGrid.Columns[3].Header = "Виручка (грн)";
                ValidationsGrid.Columns[4].Header = "Витрати (грн)";
                ValidationsGrid.Columns[5].Header = "Прибуток (грн)";
                ValidationsGrid.Columns[6].Header = "К-ть годин";            

                ValidationsGrid.Items.Refresh();
            }
            catch (Exception ex)
            {
                FailGrid(sender, ex.Message);
            }
        }

        private void FailGrid(object sender, string message)
        {
            LogsMinDatePicker.IsEnabled = true;
            LogsMaxDatePicker.IsEnabled = true;

            MessageBox.Show($"Не вдалося відобразити звітності.\nПомилка: {message}", "Звітності", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void PassEdit(object sender, string message)
        {
            RefreshGrid(sender, DataService.TableName.EVENTS.ToString());
        }

        private void FailEdit(object sender, string message)
        {
            MessageBox.Show($"Не вдалося очистити звітності.\nПомилка: {message}", "Звітності", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void LogsDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isInitializing) return;

            LogsMinDatePicker.IsEnabled = false;
            LogsMaxDatePicker.IsEnabled = false;
            RequestTable();
        }

        private void RequestTable()
        {
            if (LogsMinDatePicker.SelectedDate.HasValue && LogsMaxDatePicker.SelectedDate.HasValue)
            {
                client.Transmit($"SELECT~VALIDATIONS~FINANCIALRANGE~{LogsMinDatePicker.SelectedDate.Value:yyyy-MM-dd}~{LogsMaxDatePicker.SelectedDate.Value:yyyy-MM-dd}~");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataService.GenerateReport($"Звітності за період з {LogsMinDatePicker.SelectedDate.Value.ToString("yyyy.MM.dd")} " +
                    $"до {LogsMaxDatePicker.SelectedDate.Value.ToString("yyyy.MM.dd")}", ValidationsGrid);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не вдалося зберегти звіт.\nПомилка: {ex.Message}", "Звітності", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            UnsubscribeEventHandlers();
            Unloaded -= Page_Unloaded;
        }

        private void PurgeButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                    "УВАГА! Після очистки звітностей ЗМІНИ ВІДМІНИТИ НЕМОЖЛИВО!\n\nБуде видалено всі валідації, старші за останні 7 днів. Продовжити?",
                    "Звітності",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            client.Transmit($"EDIT~FINANCIALPURGE~");
        }
    }
}
