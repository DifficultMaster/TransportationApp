using AppClient.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static AppClient.Data.DataService;

namespace BD4Client.Network
{
    public class Client
    {
        private TcpClient tcpClient;
        private NetworkStream stream;
        private bool isRunning = false;

        private Thread listenThread;
        private CancellationTokenSource token;

        private string ipAddress;
        private int port;

        public EventHandler<string> LoginSuccess;
        public EventHandler<string> LoginFail;

        public EventHandler<string> SelectSuccess;
        public EventHandler<string> SelectFail;

        public EventHandler<string> EditSuccess;
        public EventHandler<string> EditFail;

        public EventHandler<string> Refresh;

        public EventHandler<string> OtherSuccess;
        public EventHandler<string> OtherFail;

        public void SetConnectionData(string ipAddress, int port)
        {
            this.ipAddress = ipAddress;
            this.port = port;
        }

        private void Listen(CancellationToken token)
        {
            while (isRunning && tcpClient.Connected && !token.IsCancellationRequested)
            {
                try
                {
                    NetworkStream stream = tcpClient.GetStream();
                    byte[] lengthBuffer = new byte[4];
                    int bytesRead = stream.Read(lengthBuffer, 0, 4);
                    if (bytesRead != 4) // Incomplete length prefix
                    {
                        Console.WriteLine("Incomplete length prefix received");
                        continue;
                    }

                    int messageLength = BitConverter.ToInt32(lengthBuffer, 0);
                    if (messageLength <= 0 || messageLength > 1024 * 1024) // Reject empty or overly large messages
                    {
                        Console.WriteLine($"Invalid message length: {messageLength}");
                        continue;
                    }

                    byte[] dataBuffer = new byte[messageLength];
                    int totalRead = 0;
                    while (totalRead < messageLength)
                    {
                        int read = stream.Read(dataBuffer, totalRead, messageLength - totalRead);
                        if (read == 0) // Connection closed
                        {
                            Console.WriteLine("Connection closed by server");
                            break;
                        }
                        totalRead += read;
                    }

                    if (totalRead != messageLength) // Incomplete message
                    {
                        Console.WriteLine($"Incomplete message received: {totalRead}/{messageLength} bytes");
                        continue;
                    }

                    string message = Encoding.UTF8.GetString(dataBuffer, 0, totalRead);
                    if (string.IsNullOrWhiteSpace(message) || !message.Contains("~")) // Invalid format
                    {
                        Console.WriteLine($"Invalid message format: {message}");
                        continue;
                    }

                    Process(message);
                }
                catch (Exception e)
                {
                    if (e is IOException)
                    {                        
                        break;
                    }
                    Console.WriteLine($"Client error: {e.Message}");
                    Application.Current.Dispatcher.Invoke(() =>
                        MessageBox.Show($"Client error: {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
                }
            }
        }

        private void Process(string message)
        {
            // Index interpretation
            // 
            // [0] DIRECTIVE TYPE
            // [1] DIRECTIVE OUTCOME (0 for positive, [1...i] for negative)
            // [2...i] OUTCOME PARAMETERS

            string[] messageParts = message.Split('~');

            Application.Current.Dispatcher.Invoke(() => // ASYNC Client-side running (required for WPF functionality)
            {
                switch (messageParts[0])
                {
                    case "SERVER":
                        {
                            switch (messageParts[1])
                            {
                                case "0":
                                    Connect(); // SERVER~0~
                                    break;

                                case "1":
                                    Disconnect(); // SERVER~1~
                                    break;

                                default:
                                    break;
                            }
                            break;
                        }

                    case "LOGIN":
                        {
                            switch (messageParts[1])
                            {
                                case "0":
                                    LoginSuccess?.Invoke(this, messageParts[2]); // LOGIN~0~3~
                                    break;

                                case "1":
                                    LoginFail?.Invoke(this, messageParts[2]); // LOGIN~1~password~
                                    break;

                                default:
                                    break;
                            }
                            break;
                        }
                   
                    case "EDIT":
                        {
                            switch (messageParts[2])
                            {
                                case "0":
                                    EditSuccess?.Invoke(this, ""); // EDIT~VALIDATION~0~
                                    break;

                                case "1":
                                    EditFail?.Invoke(this, messageParts[3]); // EDIT~VALIDATION~1~Access denied~
                                    break;

                                default:
                                    break;
                            }
                            break;
                        }                   

                    case "SELECT":
                        {
                            switch (messageParts[2])
                            {
                                case "0":
                                    SelectSuccess?.Invoke(this, messageParts[3]); // SELECT~ROUTES~0~...~
                                    break;

                                case "1":
                                    SelectFail?.Invoke(this, messageParts[3]); // SELECT~ROUTES~1~...~
                                    break;

                                default:
                                    break;
                            }
                            break;
                        }

                    case "REFRESH":
                        {
                            Refresh?.Invoke(this, messageParts[1]); // REFRESH~VALIDATIONS~
                            break;
                        }

                    case "OTHER":
                        {
                            switch (messageParts[2])
                            {
                                case "0":
                                    OtherSuccess?.Invoke(this, message); // OTHER~DBDOWN~0~
                                    break;

                                case "1":
                                    OtherFail?.Invoke(this, message); // OTHER~DBDOWN~1~...~
                                    break;

                                default:
                                    break;
                            }
                            break;
                        }

                    default:                       
                        break;
                }
            });            
        }

        public void Connect()
        {           
            tcpClient = new TcpClient();
            tcpClient.Connect(ipAddress, port);

            stream = tcpClient.GetStream();          

            token = new CancellationTokenSource();
            listenThread = new Thread(() => Listen(token.Token));
            listenThread.Start();

            isRunning = true;
        }

        public void Disconnect()
        {
            try
            {
                isRunning = false;
                token?.Cancel();
            }
            catch { }

            try
            {
                stream?.Close();
            }
            catch { }

            try
            {
                tcpClient?.Close();
            }
            catch { }

            try
            {
                if (listenThread != null && listenThread.IsAlive)
                {
                    listenThread.Join(500);
                }
            }
            catch { }

            isRunning = false;
        }

        public void Transmit(string message)
        {
            if (tcpClient.Connected)
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                byte[] lengthPrefix = BitConverter.GetBytes(data.Length);
                stream.Write(lengthPrefix, 0, 4);
                stream.Write(data, 0, data.Length);
            }
        }
    }
}
