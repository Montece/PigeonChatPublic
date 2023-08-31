using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Pigeon_Server
{
    public class Chat : IDisposable
    {
        public const int SERVER_COMMANDS_PORT = 52535;
        public const int SERVER_VOICE_PORT = 52536;
        public const int CLIENT_FILE_PORT = 52534;
        public const int FILE_PACKAGE_SIZE = 1024;

        public static UdpClient CommandsUdp;
        public static UdpClient VoiceUdp;
        private static IPEndPoint CommandsClientEnd;
        private static IPEndPoint VoiceClientEnd;

        private ServerWindow window;
        private Thread CommandsRecieverThread;
        private Thread VoiceRecieverThread;

        public Chat(ServerWindow sw)
        {
            Database.Init();
            window = sw;
            int a = Database.GetUsersCount();
            window.AddLog("SERVER", "Чат загружен.");
            string text1 = Database.GetUsersCount() > 0 ? $"База данных пользователей загружена ({Database.GetUsersCount()})": $"База данных пользователей не была загружена, создана новая.";
            window.AddLog("SERVER", text1);
            string text2 = Database.GetServersCount() > 0 ? $"База данных серверов загружена ({Database.GetServersCount()})" : $"База данных серверов не была загружена, создана новая.";
            window.AddLog("SERVER", text2);
            window.ShowUsers_async();
            window.ShowServers_async();
            CommandsRecieverThread = new Thread(CommandsReciever) { Name = "Commands Reciever" };
            CommandsRecieverThread.Start();
            VoiceRecieverThread = new Thread(VoiceReciever) { Name = "Voice Reciever" };
            VoiceRecieverThread.Start();
        }

        void CommandsReciever()
        {
            CommandsUdp = new UdpClient(SERVER_COMMANDS_PORT);

            List<byte> request = new List<byte>();
            ClientCommand JSONcommand;

            while (true)
            {
                request.Clear();
                JSONcommand = null;

                try
                {
                    request = CommandsUdp.Receive(ref CommandsClientEnd).ToList();
                }
                catch
                {
                    continue;
                }

                try
                {
                    JSONcommand = JsonConvert.DeserializeObject<ClientCommand>(Encoding.UTF8.GetString(request.ToArray()));
                }
                catch
                {
                    continue;
                }     

                if (JSONcommand == null) continue;
                if (JSONcommand.Sender == null) AddLog_async(CommandsClientEnd.Address.ToString(), JSONcommand.CommandID.ToString());
                else AddLog_async(JSONcommand.Sender, JSONcommand.CommandID.ToString());

                switch (JSONcommand.CommandID)
                {
                    case ClientCommands.GetServerVersion: 
                        SendCommand(ServerCommands.SendServerVersion, null, ServerWindow.Version); break;
                    case ClientCommands.LogIn:
                        bool LogInResult = (Database.HasUser(JSONcommand.Parameters[0], JSONcommand.Parameters[1]));
                        if (LogInResult)
                        {
                            SendCommand(ServerCommands.SendLogInResult, null, true.ToString(), Database.GetUser(JSONcommand.Parameters[0]).Info.Nickname);
                            Database.SetUserCommandsIP(JSONcommand.Parameters[0], CommandsClientEnd);
                        }
                        else SendCommand(ServerCommands.SendLogInResult, null, false.ToString(), "");
                        break;
                    case ClientCommands.Register:
                        bool RegisterResult = false;
                        if (!Database.HasUser(JSONcommand.Parameters[0], JSONcommand.Parameters[1]))
                        {
                            RegisterResult = Database.CreateUser(JSONcommand.Parameters[0], JSONcommand.Parameters[1], JSONcommand.Parameters[2], JSONcommand.Parameters[3]);
                        }
                        SendCommand(ServerCommands.SendRegisterResult, null, RegisterResult.ToString());
                        break;
                    case ClientCommands.GetAllServers:
                        string AllServers = Database.GetServers();
                        SendCommand(ServerCommands.SendAllServers, null, AllServers);
                        break;
                    case ClientCommands.ConnectToServer:
                        bool Connected = Database.ConnectToServer(JSONcommand.Parameters[0], JSONcommand.Parameters[1], CommandsClientEnd);
                        SendCommand(ServerCommands.ConnectToServerAnswer, null, Connected.ToString());
                        Database.OnConnect(JSONcommand.Parameters[0], JSONcommand.Parameters[1], CommandsClientEnd);
                        break;
                    case ClientCommands.LoadHistory:
                        string history = Database.GetServerHistory(JSONcommand.Parameters[0]);
                        SendCommand(ServerCommands.ServerHistory, null, history);
                        break;
                    case ClientCommands.GetServerUsers:
                        string AllUsers = Database.GetUsers(JSONcommand.Parameters[0]);
                        SendCommand(ServerCommands.SendServerUsers, null, AllUsers);
                        break;
                    case ClientCommands.Disconnect:
                        Database.OnDisconnect(JSONcommand.Parameters[0], JSONcommand.Parameters[1]);
                        Database.DisconnectFromServer(JSONcommand.Parameters[0], JSONcommand.Parameters[1], CommandsClientEnd);
                        break;
                    case ClientCommands.TextMessage:
                        Database.SendTextMessage(JSONcommand.Parameters[0], JSONcommand.Parameters[1], JSONcommand.Parameters[2]);
                        break;
                    case ClientCommands.GetUpdate:
                        new Thread(UpdateClient).Start();
                        break;
                    case ClientCommands.HasFile:
                        SendHasFile(JSONcommand.Parameters[0]);
                        break;
                    case ClientCommands.DownloadFile:
                        string fp = JSONcommand.Parameters[0];
                        new Thread(() => SendFile(CommandsClientEnd, fp)).Start();
                        break;
                    default:
                        AddLog_async("SERVER", "Неизвестная команда " + JSONcommand.CommandID);
                        break;
                }
                window.ShowUsers_async();
                window.ShowServers_async();
            }
        }

        void VoiceReciever()
        {
            VoiceUdp = new UdpClient(SERVER_VOICE_PORT);

            List<byte> request = new List<byte>();

            while (true)
            {
                request.Clear();

                try
                {
                    request = VoiceUdp.Receive(ref VoiceClientEnd).ToList();
                }
                catch
                {
                    continue;
                }

                int length = request[0];
                StringReader reader = new StringReader(Encoding.UTF8.GetString(request.GetRange(1, length).ToArray()));
                VoiceCommand command = (VoiceCommand)Database.VoiceXML.Deserialize(reader);

                if (command.Init) Database.SetUserVoiceIP(command.Login, VoiceClientEnd);
                else Database.SendVoice(command, request.ToArray());
            }
        }

        public void SendHasFile(string filename)
        {
            string[] files = Directory.GetFiles(Database.FilesPath, filename);
            bool status = files.Length == 1;
            SendCommand(ServerCommands.SendFileStatus, null, status.ToString(), filename);
        }

        void UpdateClient()
        {
            SendFile(CommandsClientEnd, Database.ClientFilePath);
        }

        void SendFile(IPEndPoint end, string path)
        {
            using (TcpClient tcp = new TcpClient())
            {
                try
                {
                    tcp.ReceiveBufferSize = FILE_PACKAGE_SIZE;
                    tcp.SendBufferSize = FILE_PACKAGE_SIZE;
                    tcp.Connect(new IPEndPoint(end.Address.Address, CLIENT_FILE_PORT));
                    
                }
                catch (Exception ex)
                {
                    AddLog_async("SERVER", ex.Message);
                    return;
                }

                using (NetworkStream netStream = tcp.GetStream())
                {
                    List<byte> file_data = File.ReadAllBytes($"Files/{path}").ToList();
                    int packages_count = (int)Math.Ceiling((double)file_data.Count / FILE_PACKAGE_SIZE);
                    byte[] packages_count_data = BitConverter.GetBytes(packages_count);
                    netStream.Write(packages_count_data, 0, packages_count_data.Length);

                    int to_send_length = file_data.Count;

                    for (int i = 0; i < packages_count; i++)
                    {
                        byte[] to_send = file_data.GetRange(FILE_PACKAGE_SIZE * i, to_send_length > FILE_PACKAGE_SIZE ? FILE_PACKAGE_SIZE : to_send_length).ToArray();
                        to_send_length -= FILE_PACKAGE_SIZE;
                        netStream.Write(to_send, 0, to_send.Length);
                    }

                    /*byte[] file_data = File.ReadAllBytes(path);
                    byte[] length = BitConverter.GetBytes(file_data.Length);
                    byte[] package = new byte[4 + file_data.Length];
                    length.CopyTo(package, 0);
                    file_data.CopyTo(package, 4);

                    int bytesSent = 0;
                    int bytesLeft = package.Length;

                    while (bytesLeft > 0)
                    {
                        int nextPacketSize = (bytesLeft > FILE_PACKAGE_SIZE) ? FILE_PACKAGE_SIZE : bytesLeft;
                        netStream.Write(package, bytesSent, nextPacketSize);
                        bytesSent += nextPacketSize;
                        bytesLeft -= nextPacketSize;
                    }*/

                    netStream.Close(); 
                }

                tcp.Close();
            }
        }

        public static void SendCommand(ServerCommands id, IPEndPoint otherEnd, params string[] parameters)
        {
            ServerCommand command = new ServerCommand() { CommandID = id, Parameters = parameters.ToList() };
            List<byte> request = new List<byte>() { 0 };
            byte[] json = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(command, Formatting.Indented));
            request.AddRange(json);
            byte[] array = request.ToArray();
            if (otherEnd != null) CommandsUdp.Send(array, array.Length, new IPEndPoint(otherEnd.Address.Address, otherEnd.Port));
            else CommandsUdp.Send(array, array.Length, new IPEndPoint(CommandsClientEnd.Address.Address, CommandsClientEnd.Port));
        }

        public static void Kick(IPEndPoint currentEnd)
        {
            SendCommand(ServerCommands.Kick, null);
        }

        public string GetRecieverInfo()
        {
            string name = "Имя потока: " + CommandsRecieverThread.Name;
            string priority = "Приоритет потока: " + CommandsRecieverThread.Priority;
            string status = "Статус потока: " + CommandsRecieverThread.ThreadState;
            return name + Environment.NewLine + priority + Environment.NewLine + status;
        }

        void AddLog(string sender, string text)
        {
            window.AddLog(sender, text);
        }

        void AddLog_async(string sender, string text)
        {
            window.AddLog_delegate(sender, text);
        }

        public void Dispose()
        {
            if (CommandsRecieverThread != null) CommandsRecieverThread.Abort();
            Database.Stop();
        }
    }
}
