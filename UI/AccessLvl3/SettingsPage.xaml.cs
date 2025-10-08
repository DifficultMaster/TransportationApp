using BD4Client.Network;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    public partial class SettingsPage : Page
    {
        private readonly Client client;
        private string jsonPath = string.Empty;

        public SettingsPage(Client client)
        {
            this.client = client;            
            InitializeComponent();
            SubscribeEventHandlers();
        }

        ~SettingsPage()
        {
            UnsubscribeEventHandlers();
        }

        private void SubscribeEventHandlers()
        {
            client.OtherSuccess += PassSetting;
            client.OtherFail += FailSetting;
        }

        private void UnsubscribeEventHandlers()
        {
            client.OtherSuccess -= PassSetting;
            client.OtherFail -= FailSetting;
        }

        private void PassSetting(object sender, string message)
        {
            string[] messageParts = message.Split('~');

            switch(messageParts[1])
            {
                case "DBDOWN":
                    {
                        if (messageParts.Length > 2)
                        {
                            string json = messageParts[3];                            
                            File.WriteAllText(jsonPath, json);

                            MessageBox.Show("Копію бази даних успішно збережено.", "Архівація", MessageBoxButton.OK, MessageBoxImage.Information);
                        }                       
                        break;
                    }                    

                case "DBUP":
                    {
                        if (messageParts.Length > 2)
                        {
                            MessageBox.Show("Копію бази даних успішно відновлено.", "Архівація", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        break;
                    }

                case "TEST":
                    {                       
                        if (messageParts.Length > 2)
                        {
                            TestLabel.Content = $"Час виконання {TestTextbox.Text} записів: {messageParts[3]} секунд";
                        }
                        TestButton.IsEnabled = true;
                        TestTextbox.IsEnabled = true;
                        break;
                    }

                default:
                    break;
            }
        }

        private void FailSetting(object sender, string message)
        {
            string[] messageParts = message.Split('~');

            switch (messageParts[1])
            {
                case "DBDOWN":
                    {
                        if (messageParts.Length > 2)
                        {
                            MessageBox.Show($"Не вдалося зберегти копію бази даних.\nПомилка: {messageParts[3]}", "Архівація", MessageBoxButton.OK, MessageBoxImage.Error);
                        }                       
                        break;
                    }

                case "DBUP":
                    {
                        if (messageParts.Length > 2)
                        {
                            MessageBox.Show($"Не вдалося відновити базу даних.\nПомилка: {messageParts[3]}", "Архівація", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        break;
                    }

                case "TEST":
                    {
                        TestButton.IsEnabled = true;
                        TestTextbox.IsEnabled = true;

                        if (messageParts.Length > 2)
                        {
                            MessageBox.Show($"Тести перервано.\nПомилка: {messageParts[3]}", "Архівація", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        break;
                    }

                default:
                    break;
            }           
        }
       
        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "JSON Backup Files (*.json)|*.json|All Files (*.*)|*.*",
                DefaultExt = "json",
                FileName = $"Backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json",
                Title = "Завантажити копію бази даних"
            };

            if (saveDialog.ShowDialog() != true) return;

            jsonPath = saveDialog.FileName;
            client.Transmit($"OTHER~DBDOWN~");                 
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                    "УВАГА! Після відновлення бази даних ЗМІНИ ВІДМІНИТИ НЕМОЖЛИВО!\n\nПродовжити?",
                    "Архівація",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            var openDialog = new OpenFileDialog
            {
                Filter = "JSON Backup Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Оберіть файл для відновлення"
            };

            if (openDialog.ShowDialog() != true) return;

            string json = File.ReadAllText(openDialog.FileName);
            client.Transmit($"OTHER~DBUP~{json}~");
        }

        private void TestTextbox_KeyDown(object sender, KeyEventArgs e)
        {
            bool isNumber = (e.Key >= Key.D0 && e.Key <= Key.D9) ||
                            (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9);
            bool isAllowedControl = e.Key == Key.Back || e.Key == Key.Delete ||
                                   e.Key == Key.Left || e.Key == Key.Right ||
                                   e.Key == Key.Tab;

            if (!isNumber && !isAllowedControl)
            {
                e.Handled = true;         
            }           
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            TestButton.IsEnabled = false;
            TestTextbox.IsEnabled = false;
            TestLabel.Content = $"Триває виконання тесту...";
            client.Transmit($"OTHER~TEST~{TestTextbox.Text}~");
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            UnsubscribeEventHandlers();
            Unloaded -= Page_Unloaded;
        }
    }
}
