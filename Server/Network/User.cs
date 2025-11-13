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

        public void Register(string login, string password)
        {
            // Security flaw, every user not in Drivers or Dispatchers is an Admin, I should probably scratch this whole thing and make a new one on Postgre or something
            this.accessLevel = DataService.AccessLevel.VIEWER;
            string id = login;

            if (id.Length > 14)
                id = id.Remove(14);

            // The following only works if the first 15 characters of a login are unique, but db has to be refactored for this (table drop),
            // so let us just pray this doesn't cause issues for now
            DataService.RegisterUser(this, new Models.Person { PersonId = id, Login = login, HashedPassword = PasswordHandler.GetHashedPassword(password) }, this.accessLevel);
        }
    }
}
