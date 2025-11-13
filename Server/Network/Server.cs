using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Data.SqlClient;
using System.Runtime.InteropServices;
using System.Data;
using Microsoft.EntityFrameworkCore.Storage.Json;
using Newtonsoft.Json;
using System.Xml.Serialization;
using System.Xml;
using Formatting = System.Xml.Formatting;
using AppServer.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Reflection.Metadata;
using AppServer.Models;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Azure.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.IdentityModel.Tokens;

namespace AppServer.Network
{
    public class Server
    {
        public const string ConnectionString = "Data Source=CEZANNIUS\\MSSQLSERVER01;Initial Catalog=Radio;" +
            "Integrated Security=True;Connect Timeout=30;Encrypt=True;Persist Security Info=False;Trust Server Certificate=True;" +
            "Application Intent=ReadWrite;Multi Subnet Failover=False"; // CONNECTION STRING CHANGE ONLY IF REQUIRED

        private TcpListener tcpListener;      
        private bool isRunning = false;

        private Thread clientThread;
        private CancellationTokenSource token;        

        public Server(string ip, int port)
        {           
            try
            {
                tcpListener = new TcpListener(IPAddress.Parse(ip), port);               
                DataService.DownloadCredentials();          
            }
            catch (Exception e)
            {
                DataService.LogToConsole($"!!!!!!!!!!!!!SERVER!!!!!!!!!!!!!! --- Error while attempting to connect client: {e.Message}");
            }            
        }                    

        private void Listen()
        {
            while (isRunning)
            {
                try
                {
                    TcpClient client = tcpListener.AcceptTcpClient();

                    DataService.users[new User(client)] = DataService.TableName.NONE;              

                    token = new CancellationTokenSource();
                    clientThread = new Thread(() => Handle(client, token.Token));
                    clientThread.Start();
                    DataService.LogToConsole($"-------------{client.Client.RemoteEndPoint?.ToString()}------------- --- Connected");
                }
                catch (Exception e)
                {
                    DataService.LogToConsole($"!!!!!!!!!!!!!SERVER!!!!!!!!!!!!!! --- Error while attempting to connect client: {e.Message}");
                }
            }
        }

        private void Handle(TcpClient client, CancellationToken token)
        {
            NetworkStream stream = client.GetStream();
            byte[] lengthBuffer = new byte[4];

            while (isRunning && !token.IsCancellationRequested)
            {
                try
                {                 
                    int totalBytesRead = 0;
                    while (totalBytesRead < 4 && !token.IsCancellationRequested)
                    {
                        int bytesRead = stream.Read(
                            lengthBuffer,
                            totalBytesRead,
                            4 - totalBytesRead
                        );
                        if (bytesRead == 0) throw new IOException("Connection closed");
                        totalBytesRead += bytesRead;
                    }

                    int messageLength = BitConverter.ToInt32(lengthBuffer, 0);
                    byte[] dataBuffer = new byte[messageLength];
                  
                    totalBytesRead = 0;
                    while (totalBytesRead < messageLength && !token.IsCancellationRequested)
                    {
                        int bytesRead = stream.Read(
                            dataBuffer,
                            totalBytesRead,
                            messageLength - totalBytesRead
                        );
                        if (bytesRead == 0) throw new IOException("Connection closed");
                        totalBytesRead += bytesRead;
                    }

                    string message = Encoding.UTF8.GetString(dataBuffer, 0, totalBytesRead);
                    DataService.LogToConsole($"-------------{client.Client.RemoteEndPoint}------------- --- Sent message '{message}'");
                    Process(client, message);
                }
                catch (IOException ex)
                {            
                    DataService.LogToConsole($"-------------{client.Client.RemoteEndPoint}------------- --- Disconnected: {ex.Message}");
                    break;
                }
                catch (Exception ex)
                {
                    DataService.LogToConsole($"-------------{client.Client.RemoteEndPoint}------------- --- Error: {ex.Message}");
                }
            }

            client.Close();
            DataService.users.Remove(DataService.users.First(user => user.Key.client == client).Key);
        }        

        private void Process(TcpClient client, string message)
        {           
            string[] messageParts = message.Split('~');

            switch (messageParts[0])
            {
                case "LOGIN": // LOGIN~difficultmaster~K9$mPx&n2jLq~
                    {
                        var user = DataService.users.First(user => user.Key.client == client);
                        string loginResult = DataService.GetCredentialsValidity(messageParts[1], messageParts[2]);

                        if (loginResult.StartsWith("lockout:"))
                        {
                            string remainingTime = loginResult.Split(':')[1];
                            TransmitTo(client, $"LOGIN~1~lockout:{remainingTime}~");
                            DataService.LogToConsole($"-------------{client.Client.RemoteEndPoint?.ToString()}------------- --- Login attempt blocked, {remainingTime} seconds remaining");
                        }
                        else if (Enum.TryParse(loginResult.ToUpper(), out DataService.AccessLevel accessResult))
                        {
                            user.Key.Login(accessResult, messageParts[1]);
                            TransmitTo(client, $"LOGIN~0~{DataService.GetAccessLevel(user.Key)}~");
                            DataService.LogToConsole($"-------------{client.Client.RemoteEndPoint?.ToString()}------------- --- Logged in as '{user.Key.login}'");
                        }
                        else
                        {
                            TransmitTo(client, $"LOGIN~1~{loginResult}~");
                            DataService.LogToConsole($"-------------{client.Client.RemoteEndPoint?.ToString()}------------- --- Failed to log in due to error '{loginResult}'");
                        }
                        break;                                                          
                    }

                case "REGISTER": // REGISTER~difficultmaster~K9$mPx&n2jLq~
                    {
                        var user = DataService.users.First(user => user.Key.client == client);                        

                        try
                        {
                            if (DataService.IsLoginNew(messageParts[1]))
                            {
                                user.Key.Register(messageParts[1], messageParts[2]);
                                string loginResult = DataService.GetCredentialsValidity(messageParts[1], messageParts[2]);

                                if (Enum.TryParse(loginResult.ToUpper(), out DataService.AccessLevel accessResult))
                                {
                                    user.Key.Login(accessResult, messageParts[1]);
                                    TransmitTo(client, $"LOGIN~0~{DataService.GetAccessLevel(user.Key)}~");
                                    DataService.LogToConsole($"-------------{client.Client.RemoteEndPoint?.ToString()}------------- --- Registered new user '{messageParts[1]}'");
                                }
                                else
                                {
                                    throw new Exception("Registration failed");
                                }                                
                            }
                            else
                            {
                                TransmitTo(client, $"LOGIN~1~Login already exists~");
                                DataService.LogToConsole($"-------------{client.Client.RemoteEndPoint?.ToString()}------------- --- Failed to register new user due to error 'Login already exists'");
                            }
                        }
                        catch (Exception ex)
                        {
                            TransmitTo(client, $"LOGIN~1~{ex.Message}~");
                            DataService.LogToConsole($"-------------{client.Client.RemoteEndPoint?.ToString()}------------- --- Failed to register new user due to error '{ex.Message}'");
                        }
                        break;
                    }

                case "LOGOUT": // LOGOUT~difficultmaster~
                    {
                        var user = DataService.users.First(user => user.Key.client == client);

                        if (user.Key.login == messageParts[1])
                        {                        
                            user.Key.Logout();
                            user.Key.client.Close();
                            DataService.users.Remove(user.Key);
                            DataService.LogToConsole($"-------------{client.Client.RemoteEndPoint?.ToString()}------------- --- Logged out as '{user.Key.login}'");
                        }
                        else
                        {
                            DataService.LogToConsole($"-------------{client.Client.RemoteEndPoint?.ToString()}------------- --- Failed to log out due to error");
                        }
                        break;
                    }

                case "EDIT": // EDIT~...~ or EDIT~...~key~ or EDIT~...~newValue~ or EDIT~...~key~newValue~
                    {
                        var user = DataService.users.First(user => user.Key.client == client);
                        DataService.TableName tableName = DataService.TableName.NONE;

                        try
                        {
                            switch (messageParts.Length)
                            {
                                case 2:
                                    return;

                                case 3:
                                    {
                                        tableName = DataService.Edit(user.Key, messageParts[1]);
                                        break;
                                    }

                                case 4:
                                    {
                                        tableName = DataService.Edit(user.Key, messageParts[1], messageParts[2]);
                                        break;
                                    }

                                case 5:
                                    {
                                        tableName = DataService.Edit(user.Key, messageParts[1], messageParts[2], messageParts[3]);
                                        break;
                                    }

                                default:
                                    throw new Exception("Incorrect format");
                            }
                            
                            TransmitTo(client, $"EDIT~{messageParts[1]}~0~");
                            DataService.LogToConsole($"-------------{client.Client.RemoteEndPoint?.ToString()}------------- --- Edit operation '{messageParts[1]}' successful");

                            foreach (var userPair in DataService.users)
                            {
                                if (userPair.Value == tableName && userPair.Key != user.Key)
                                {
                                    TransmitTo(client, $"REFRESH~{tableName.ToString()}~");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            TransmitTo(client, $"EDIT~{messageParts[1]}~1~{ex.Message}~");
                            DataService.LogToConsole($"-------------{client.Client.RemoteEndPoint?.ToString()}------------- --- Edit operation '{messageParts[1]}' unsuccessful due to error: {ex.Message}");                           
                        }  
                        finally
                        {
                            DataService.context.ChangeTracker.Clear();
                        }
                        break;
                    }                

                case "SELECT": // SELECT~ROUTES~MYROUTE~
                    {
                        var user = DataService.users.First(user => user.Key.client == client);
                        string jsonData = string.Empty;

                        try
                        {
                            DataService.TableName tableName = (DataService.TableName)Enum.Parse(typeof(DataService.TableName), messageParts[1].ToUpper());
                            jsonData = DataService.Select(user.Key, tableName, message);
                            DataService.users[user.Key] = tableName;

                            TransmitTo(client, $"SELECT~{messageParts[1]}~0~{jsonData}~");
                            DataService.LogToConsole($"-------------{client.Client.RemoteEndPoint?.ToString()}------------- --- Select operation '{messageParts[2]}' successful");
                        }
                        catch (Exception ex)
                        {
                            TransmitTo(client, $"SELECT~{messageParts[1]}~1~{ex.Message}~");
                            DataService.LogToConsole($"-------------{client.Client.RemoteEndPoint?.ToString()}------------- --- Select operation '{messageParts[2]}' unsuccessful due to error: {ex.Message}");
                        }           
                        finally
                        {
                            DataService.context.ChangeTracker.Clear();
                        }
                        break;
                    }

                case "OTHER": // OTHER~DBDOWN~
                    {
                        var user = DataService.users.First(user => user.Key.client == client);
                        string msg = string.Empty;

                        try
                        {
                            switch (messageParts[1])
                            {
                                case "DBDOWN": // OTHER~DBDOWN~
                                    {
                                        msg = DataService.GetDatabase(user.Key);                                                                              
                                        break;
                                    }
                                    
                                case "DBUP": // OTHER~DBUP~...~
                                    {
                                        DataService.SetDatabase(user.Key, messageParts[2]);

                                        foreach (var userPair in DataService.users)
                                        {
                                            TransmitTo(client, $"REFRESH~{DataService.users[userPair.Key].ToString()}~");
                                        }

                                        break;
                                    }

                                case "TEST": // OTHER~TEST~10000~
                                    {
                                        if (!int.TryParse(messageParts[2], out int writeSpeed))
                                            throw new Exception("Invalid write speed");

                                        msg = DataService.GetDatabaseWriteSpeed(user.Key, writeSpeed);
                                        break;
                                    }

                                default:
                                    throw new Exception("Invalid directive");
                            }

                            TransmitTo(client, $"OTHER~{messageParts[1]}~0~{msg}~");
                            DataService.LogToConsole($"-------------{client.Client.RemoteEndPoint?.ToString()}------------- --- Other operation '{messageParts[1]}' successful");
                        }
                        catch (Exception ex)
                        {
                            TransmitTo(client, $"OTHER~{messageParts[1]}~1~{ex.Message}");
                            DataService.LogToConsole($"-------------{client.Client.RemoteEndPoint?.ToString()}------------- --- Other operation '{messageParts[1]}' unsuccessful");
                        }
                        finally
                        {
                            DataService.context.ChangeTracker.Clear();
                        }
                        break;
                    }

                default:
                    break;
            }
        }       

        public void Start()
        {            
            tcpListener.Start();            

            Thread listenThread = new Thread(Listen);
            listenThread.Start();  

            isRunning = true;     

            TransmitToAll("SERVER~ONLINE~");
        }

        public void Stop()
        {
            TransmitToAll("SERVER~OFFLINE~");

            token?.Cancel();
     
            clientThread?.Join();  

            tcpListener.Stop();       

            isRunning = false;          
        }

        public void TransmitTo(TcpClient client, string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            byte[] lengthPrefix = BitConverter.GetBytes(data.Length);

            if (client.Connected)
            {
                NetworkStream stream = client.GetStream();
                stream.Write(lengthPrefix, 0, lengthPrefix.Length);
                stream.Write(data, 0, data.Length);
                DataService.LogToConsole($"-------------SERVER------------- --- Transmitted '{message}' to {client.Client.RemoteEndPoint?.ToString()}");
            }
        }

        public void TransmitToAll(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            byte[] lengthPrefix = BitConverter.GetBytes(data.Length);

            foreach (var client in DataService.users)
            {
                if (client.Key.client.Connected)
                {
                    NetworkStream stream = client.Key.client.GetStream();
                    stream.Write(lengthPrefix, 0, lengthPrefix.Length);
                    stream.Write(data, 0, data.Length);
                    DataService.LogToConsole($"-------------SERVER------------- --- Transmitted '{message}' to {client.Key.client.Client.RemoteEndPoint?.ToString()}");
                }
            }
        }
    }
}
