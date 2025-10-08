using AppClient.Data;
using BD4Client.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace AppClient.UI.AccessLvl1
{   
    public partial class ExitPage : Page
    {
        private readonly Client client;
        private readonly DataService.AccessLevel accessLevel;

        public ExitPage(Client client, DataService.AccessLevel accessLevel)
        {
            this.client = client;
            this.accessLevel = accessLevel;
            InitializeComponent();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {            
            Unloaded -= Page_Unloaded;
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            client.Disconnect();
            Application.Current.Shutdown();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
