using AppClient.Data;
using AppClient.UI.AccessLvl3;
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
using System.Windows.Shapes;

namespace AppClient.UI.Windows
{
    public partial class TerminalWindow : Window
    {
        private Client client;

        public TerminalWindow()
        {
            client = new Client();
            client.SetConnectionData(Data.DataService.ipAddress, Data.DataService.port);
            client.Connect();            

            InitializeComponent();

            TerminalFrame.Navigate(new LoginPage(client));            
        }

        private void TerminalFrame_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (TerminalFrame.Content is TerminalPage page)
            {           
                if (page.state == TerminalPage.TerminalState.DEFAULT)
                {
                    // S to pass validation
                    if (e.Key == Key.S)
                    {
                        page.RequestValidation();
                        e.Handled = true;
                    }
                    // F to fail validation
                    else if (e.Key == Key.F)
                    {
                        page.FailValidation(sender, string.Empty);
                        e.Handled = true;
                    }
                }
            }
        }
    }
}
