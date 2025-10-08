using AppServer.Data;
using AppServer.Models;
using AppServer.Network;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace AppServer
{
    public class Program
    {  
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Clear();
            Console.WriteLine("!!!!!!!!!!!!!SERVER!!!!!!!!!!!!!! --- Starting server...");

            var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

            var optionsBuilder = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));

            DataService.SetConnectionParams(new AppDbContext(optionsBuilder.Options), 
                configuration.GetSection("ServerSettings:IpAddress").Value,
                int.Parse(configuration.GetSection("ServerSettings:Port").Value));

            Server server = new Server(DataService.ipAddress, DataService.port);
            server.Start();

            Console.WriteLine("!!!!!!!!!!!!!SERVER!!!!!!!!!!!!!! --- Server running. Type 'stop' to shutdown...");
            while (true)
            {
                if (Console.ReadLine()?.ToLower() == "stop")
                {
                    server.Stop();
                    break;
                }
            }

            Console.WriteLine("!!!!!!!!!!!!!SERVER!!!!!!!!!!!!!! --- Server stopped. Press any key to exit...");
            Console.ReadKey();
        }
    }
}
