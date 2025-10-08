using AppClient.Data;
using BD4Client.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AppClient.UI.Windows
{   
    public partial class MainWindow : Window
    {
        private readonly string login;
        private readonly DataService.AccessLevel accessLevel;
        private readonly Client client;

        private readonly string darkBlueColorString = "#FF0F398C";
        private readonly string lightBlueColorString = "#4679A6";

        public MainWindow(Client client, DataService.AccessLevel accessLevel, string login)
        {
            InitializeComponent();

            switch(accessLevel)
            {
                case DataService.AccessLevel.DRIVER:
                    VerticalToolbar.Visibility = Visibility.Visible;
                    HomeButton.Visibility = Visibility.Visible;
                    ScheduleButton.Visibility = Visibility.Visible;
                    StaffButton.Visibility = Visibility.Collapsed;
                    VehicleButton.Visibility = Visibility.Collapsed;
                    RouteButton.Visibility = Visibility.Collapsed;
                    DepotButton.Visibility = Visibility.Collapsed;
                    LogsButton.Visibility = Visibility.Collapsed;
                    SettingsButton.Visibility = Visibility.Collapsed;
                    ExitButton.Visibility = Visibility.Visible; 
                    break;

                case DataService.AccessLevel.DISPATCHER:
                    VerticalToolbar.Visibility = Visibility.Visible;
                    HomeButton.Visibility = Visibility.Visible;
                    ScheduleButton.Visibility = Visibility.Visible;
                    StaffButton.Visibility = Visibility.Visible;
                    VehicleButton.Visibility = Visibility.Visible;
                    RouteButton.Visibility = Visibility.Visible;
                    DepotButton.Visibility = Visibility.Collapsed;
                    LogsButton.Visibility = Visibility.Collapsed;
                    SettingsButton.Visibility = Visibility.Collapsed;
                    ExitButton.Visibility = Visibility.Visible;
                    break;

                case DataService.AccessLevel.ADMIN:
                    VerticalToolbar.Visibility = Visibility.Visible;
                    HomeButton.Visibility = Visibility.Visible;
                    ScheduleButton.Visibility = Visibility.Visible;
                    StaffButton.Visibility = Visibility.Visible;
                    VehicleButton.Visibility = Visibility.Visible;
                    RouteButton.Visibility = Visibility.Visible;
                    DepotButton.Visibility = Visibility.Visible;
                    LogsButton.Visibility = Visibility.Visible;
                    SettingsButton.Visibility = Visibility.Visible;
                    ExitButton.Visibility = Visibility.Visible;
                    break;

                case DataService.AccessLevel.VIEWER:
                default:
                    VerticalToolbar.Visibility = Visibility.Collapsed;
                    break;                   
            }

            this.client = client;
            this.login = login;
            this.accessLevel = accessLevel;

            HomeButton_Click(this, new RoutedEventArgs());
        }

        private void NavigateToPage(Page page)
        {
          
            if (MainFrame.Content is FrameworkElement currentPage)
            {
                currentPage.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, currentPage));
            }
        
            while (MainFrame.NavigationService.RemoveBackEntry() != null);           
            MainFrame.Navigate(page);
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            if (accessLevel != DataService.AccessLevel.VIEWER)
            {
                this.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(darkBlueColorString);
                NavigateToPage(new AccessLvl1.HomePage(client, accessLevel));
            }                
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (accessLevel == DataService.AccessLevel.ADMIN)
            {
                this.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(darkBlueColorString);
                NavigateToPage(new AccessLvl3.SettingsPage(client));
            }                
        }

        private void LogsButton_Click(object sender, RoutedEventArgs e)
        {
            if (accessLevel == DataService.AccessLevel.ADMIN)
            {
                this.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(darkBlueColorString);
               NavigateToPage(new AccessLvl3.LogsPage(client));
            }                
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(darkBlueColorString);
            NavigateToPage(new AccessLvl1.ExitPage(client, accessLevel));
        }

        private void DepotButton_Click(object sender, RoutedEventArgs e)
        {
            if (accessLevel == DataService.AccessLevel.ADMIN)
            {
                this.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(lightBlueColorString);
                NavigateToPage(new AccessLvl3.DepotPage(client));
            }
                
        }

        private void RouteButton_Click(object sender, RoutedEventArgs e)
        {
            switch (accessLevel)
            {
                case DataService.AccessLevel.DISPATCHER:
                    this.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(lightBlueColorString);
                    NavigateToPage(new AccessLvl2.RoutePage(client));
                    break;

                case DataService.AccessLevel.ADMIN:
                    this.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(lightBlueColorString);
                    NavigateToPage(new AccessLvl3.RoutePage(client));
                    break;

                default:
                    break;
            }
        }

        private void VehicleButton_Click(object sender, RoutedEventArgs e)
        {
            switch (accessLevel)
            {
                case DataService.AccessLevel.DISPATCHER:
                    this.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(lightBlueColorString);
                    NavigateToPage(new AccessLvl2.VehiclePage(client));
                    break;

                case DataService.AccessLevel.ADMIN:
                    this.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(lightBlueColorString);
                    NavigateToPage(new AccessLvl3.VehiclePage(client));
                    break;

                default:
                    break;
            }
        }

        private void StaffButton_Click(object sender, RoutedEventArgs e)
        {
            switch (accessLevel)
            {                

                case DataService.AccessLevel.DISPATCHER:
                    this.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(lightBlueColorString);
                    NavigateToPage(new AccessLvl2.StaffPage(client));
                    break;

                case DataService.AccessLevel.ADMIN:
                    this.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(lightBlueColorString);
                    NavigateToPage(new AccessLvl3.StaffPage(client));
                    break;
            
                default:
                    break;
            }
        }

        private void ScheduleButton_Click(object sender, RoutedEventArgs e)
        {
            switch(accessLevel)
            {
                case DataService.AccessLevel.DRIVER:
                    this.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(darkBlueColorString);
                    NavigateToPage(new AccessLvl1.SchedulePage(client));
                    break;

                case DataService.AccessLevel.DISPATCHER:
                    this.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(lightBlueColorString);
                    NavigateToPage(new AccessLvl2.SchedulePage(client));
                    break;

                case DataService.AccessLevel.ADMIN:
                    this.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(darkBlueColorString);
                    NavigateToPage(new AccessLvl3.SchedulePage(client));
                    break;
             
                default:
                    break;
            }
        }

        private void VerticalToolbar_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }        
    }
}
