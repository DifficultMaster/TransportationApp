using System.Configuration;
using System.Data;
using System.Globalization;
using System.Windows;
using AppClient.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace AppClient
{     
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {            
            CultureInfo ukrainianCulture = new CultureInfo("uk-UA");
            Thread.CurrentThread.CurrentCulture = ukrainianCulture;
            Thread.CurrentThread.CurrentUICulture = ukrainianCulture;
            CultureInfo.DefaultThreadCurrentCulture = ukrainianCulture;
            CultureInfo.DefaultThreadCurrentUICulture = ukrainianCulture;

            base.OnStartup(e);
        }

        public App()
        {
            IConfigurationRoot configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                   .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();

            DataService.SetConnectionParams(configuration.GetSection("ServerSettings:IpAddress").Value,
                int.Parse(configuration.GetSection("ServerSettings:Port").Value));


        }
    }
}
