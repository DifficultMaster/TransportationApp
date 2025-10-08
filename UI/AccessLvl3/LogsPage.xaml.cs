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
    public partial class LogsPage : Page
    {
        private readonly Client client;
        private bool isInitializing = true;

        public LogsPage(Client client)
        {
            this.client = client;
            InitializeComponent();
            SubscribeEventHandlers();

            this.LogsMinDatePicker.SelectedDate = DateTime.Now.AddDays(-7);
            this.LogsMaxDatePicker.SelectedDate = DateTime.Now.AddDays(1);
            this.isInitializing = false;
            TransmitLogRange();
        }

        ~LogsPage()
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
            if (message.ToUpper() == DataService.TableName.EVENTS.ToString())
            {
                client.Transmit($"SELECT~EVENTS~LOGSRANGE~{LogsMinDatePicker.SelectedDate.ToString()}~{LogsMaxDatePicker.SelectedDate.ToString()}~");
            }
        }

        private void PassGrid(object sender, string message)
        {
            LogsMinDatePicker.IsEnabled = true;
            LogsMaxDatePicker.IsEnabled = true;

            List<Event> logs = JsonSerializer.Deserialize<List<Event>>(message);
            if (logs != null)
            {
                LogsGrid.ItemsSource = logs;

                LogsGrid.Columns[0].Header = "Ідентифікатор події";
                LogsGrid.Columns[1].Header = "Ідентифікатор ініціатора";
                LogsGrid.Columns[2].Header = "Назва таблиці";
                LogsGrid.Columns[3].Header = "Тип дії";
                LogsGrid.Columns[4].Header = "Дата та час";
                LogsGrid.Columns[5].Header = "Директива";
                LogsGrid.Columns[LogsGrid.Columns.Count - 1].Visibility = Visibility.Hidden;

                LogsGrid.Items.Refresh();                
            }
            else
            {
                MessageBox.Show("Не вдалося відобразити журнал подій.", "Журнал подій", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FailGrid(object sender, string message)
        {
            LogsMinDatePicker.IsEnabled = true;
            LogsMaxDatePicker.IsEnabled = true;

            MessageBox.Show($"Не вдалося відобразити журнал подій.\nПомилка: {message}", "Журнал подій", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void PassEdit(object sender, string message)
        {
            RefreshGrid(sender, DataService.TableName.EVENTS.ToString());
        }

        private void FailEdit(object sender, string message)
        {
            MessageBox.Show($"Не вдалося очистити журнал подій.\nПомилка: {message}", "Журнал подій", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void LogsDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isInitializing) return;

            LogsMinDatePicker.IsEnabled = false;
            LogsMaxDatePicker.IsEnabled = false;
            TransmitLogRange();
        }

        private void TransmitLogRange()
        {
            if (LogsMinDatePicker.SelectedDate.HasValue && LogsMaxDatePicker.SelectedDate.HasValue)
            {
                client.Transmit($"SELECT~EVENTS~LOGSRANGE~{LogsMinDatePicker.SelectedDate.Value:yyyy-MM-dd}~{LogsMaxDatePicker.SelectedDate.Value:yyyy-MM-dd}~");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataService.GenerateReport($"Журнал подій за період з {LogsMinDatePicker.SelectedDate.Value.ToString("yyyy.MM.dd")} " +
                    $"до {LogsMaxDatePicker.SelectedDate.Value.ToString("yyyy.MM.dd")}", LogsGrid);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не вдалося зберегти звіт.\nПомилка: {ex.Message}", "Журнал подій", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    "УВАГА! Після очистки журналу подій ЗМІНИ ВІДМІНИТИ НЕМОЖЛИВО!\n\nБуде видалено всі події, старші за останні 48 годин. Продовжити?",
                    "Журнал подій",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            client.Transmit($"EDIT~LOGSPURGE~");
        }
    }
}
