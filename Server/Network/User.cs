using AppServer.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AppServer.Network
{
    public class User
    {  
        public TcpClient client {  get; private set; }

        public string login { get; private set; }     

        public DataService.AccessLevel accessLevel { get; private set; }

        public User(TcpClient client)
        {
            this.client = client;
            this.login = string.Empty;
            this.accessLevel = DataService.AccessLevel.VIEWER;
        }

        public User(TcpClient client, string login, DataService.AccessLevel accessLevel)
        {
            this.client = client;
            this.login = login;       
            this.accessLevel = accessLevel;       
        }             
       
        public void Login(DataService.AccessLevel accessLevel, string login)
        {
            this.accessLevel = accessLevel;
            this.login = login;            
        }

        public void Logout()
        {
            this.accessLevel = DataService.AccessLevel.VIEWER;
            this.login = string.Empty;        
        }
    }
}
