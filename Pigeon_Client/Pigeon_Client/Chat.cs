using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace Pigeon_Client
{
    public static class Chat
    {
        public const int SERVER_COMMANDS_PORT = 52535;
        public const int SERVER_VOICE_PORT = 52536;
        public const int CLIENT_FILE_PORT = 52533;

        public const int FILE_PACKAGE_SIZE = 1024;

        public static ServerInfo CurrentServer { get; private set; }
        public static List<ServerInfo> Servers = new List<ServerInfo>();
        static string Login { get; set; } = "";
        static string Nickname { get; set; } = "";

        public static UdpClient CommandsUdp = new UdpClient();
        public static UdpClient VoiceUdp = new UdpClient();
        public static IPEndPoint CommandsEnd = new IPEndPoint(IPAddress.Any, SERVER_COMMANDS_PORT);
        public static IPEndPoint VoiceEnd = new IPEndPoint(IPAddress.Any, SERVER_VOICE_PORT);

        static XmlSerializer VoiceXML = new XmlSerializer(typeof(VoiceCommand));

        static List<byte> request = new List<byte>();
        static int RequestType;
        static ClientCommand JSONcommand;
        static List<string> Params;
        static List<byte> Voice;

        public static void SetIP(string serverIP)
        {
            if (!IPAddress.TryParse(serverIP, out IPAddress ipAddress)) ipAddress = Dns.GetHostEntry(serverIP).AddressList[0];
            CommandsEnd = new IPEndPoint(ipAddress, SERVER_COMMANDS_PORT);
            VoiceEnd = new IPEndPoint(ipAddress, SERVER_VOICE_PORT);
        }

        public static string GetServerVersion()
        {
            SendCommand(ClientCommands.GetServerVersion, Library.Version);
            bool status = RecieveCommand(out Params, out Voice);
            if (!status) return "ERROR";    
            return Params[0];
        }

        public static bool SetCurrentServer(int i)
        {
            if (i >= 0 && i < Servers.Count)
            {
                CurrentServer = Servers[i];
                return true;
            }
            return false;
        }

        public static bool LogIn(string login, string password)
        {
            Login = login;
            SendCommand(ClientCommands.LogIn, login, password);
            RecieveCommand(out Params, out Voice);
            Nickname = Params[1];
            return bool.Parse(Params[0]);
        }

        public static bool Register(string login, string password, string nickname, string email)
        {
            Login = login;
            Nickname = nickname;
            SendCommand(ClientCommands.Register, nickname, login, password, email);
            RecieveCommand(out Params, out Voice);
            return bool.Parse(Params[0]);
        }

        public static void GetServers()
        {
            SendCommand(ClientCommands.GetAllServers, null);
            RecieveCommand(out Params, out Voice);

            XmlSerializer ServersXML = new XmlSerializer(typeof(ServersSavingData));
            StringReader writer = new StringReader(Params[0]);
            ServersSavingData data = (ServersSavingData)ServersXML.Deserialize(writer);
            Servers = data.Servers;
        }                   

        public static bool ConnectToServer()
        {
            SendCommand(ClientCommands.ConnectToServer, Login, CurrentServer.SID);
            RecieveCommand(out Params, out Voice);
            bool success = bool.Parse(Params[0]);
            return success;
        }

        public static string LoadHistory()
        {
            SendCommand(ClientCommands.LoadHistory,/* null,*/ CurrentServer.SID);
            RecieveCommand(out Params, out Voice);
            return Params[0];
        }

        public static List<ShortUserInfo> GetUsers()
        {
            SendCommand(ClientCommands.GetServerUsers,/* null,*/ CurrentServer.SID);
            RecieveCommand(out Params, out Voice);
            var a = Params[0];
            XmlSerializer UsersXML = new XmlSerializer(typeof(UsersSavingData2));
            StringReader writer = new StringReader(Params[0]);
            UsersSavingData2 data = (UsersSavingData2)UsersXML.Deserialize(writer);
            return data.Users;
        }

        public static void SendTextMessage(string text)
        {
            SendCommand(ClientCommands.TextMessage,/* null,*/ CurrentServer.SID, text, Login);
        }

        static bool RecieveCommand(out List<string> result, out List<byte> voice)
        {
            result = null;
            voice = null;
            request.Clear();
            JSONcommand = null;

            try
            {
                request = CommandsUdp.Receive(ref CommandsEnd).ToList();
            }
            catch (SocketException x)
            {
                return false;
            }

            if (RequestType == 0)
            {
                request.RemoveAt(0);
                try
                {
                    JSONcommand = JsonConvert.DeserializeObject<ClientCommand>(Encoding.UTF8.GetString(request.ToArray()));
                    result = JSONcommand.Parameters;                   
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            else
            if (RequestType == 1)
            {
                request.RemoveAt(0);
                voice = request;
                return true;
            }
            return false;
        }

        static List<byte> RecieveVoice()
        {
            List<byte> VoiceRequest = new List<byte>();

            try
            {
                request = VoiceUdp.Receive(ref VoiceEnd).ToList();
            }
            catch
            {
                return new List<byte>();
            }

            int length = request[0];
            StringReader reader = new StringReader(Encoding.UTF8.GetString(request.GetRange(1, length).ToArray()));
            VoiceCommand command = (VoiceCommand)VoiceXML.Deserialize(reader);
            VoiceRequest.RemoveRange(0, length);

            return VoiceRequest;
        }

        public static void SendCommand(ClientCommands id, params string[] parameters)
        {
            ClientCommand command = new ClientCommand() { CommandID = id, Parameters = parameters?.ToList() };
            List<byte> request = new List<byte>();
            byte[] json = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(command, Formatting.Indented));
            request.AddRange(json);
            byte[] array = request.ToArray();
            CommandsUdp.Send(array, array.Length, CommandsEnd);
        }

        public static void SendVoice(byte[] voice)
        {
            XmlSerializer VoiceXML = new XmlSerializer(typeof(VoiceCommand));
            VoiceCommand command = new VoiceCommand
            {
                SID = CurrentServer.SID,
                Login = Login,
                Init = false
            };
            List<byte> request = new List<byte>();

            StringWriter writer = new StringWriter();
            VoiceXML.Serialize(writer, command);
            byte[] commandBYTE = Encoding.UTF8.GetBytes(writer.ToString());
            request.Add((byte)commandBYTE.Length);
            request.AddRange(commandBYTE);
            request.AddRange(voice);
            byte[] array = request.ToArray();
            VoiceUdp.Send(array, array.Length, VoiceEnd);
        }

        public static void InitVoice()
        {
            XmlSerializer VoiceXML = new XmlSerializer(typeof(VoiceCommand));
            VoiceCommand command = new VoiceCommand
            {
                SID = "",
                Login = Login,
                Init = true
            };
            List<byte> request = new List<byte>();

            StringWriter writer = new StringWriter();
            VoiceXML.Serialize(writer, command);
            byte[] commandBYTE = Encoding.UTF8.GetBytes(writer.ToString());
            request.Add((byte)commandBYTE.Length);
            request.AddRange(commandBYTE);
            byte[] array = request.ToArray();
            VoiceUdp.Send(array, array.Length, VoiceEnd);
        }

        public static void Stop()
        {
            SendCommand(ClientCommands.Disconnect, Login, CurrentServer.SID);
        }
    }

    [Serializable]
    public class ServerInfo
    {
        public string Title { get; set; }
        public string SID { get; set; }
        public int MaxUsersCount { get; set; }
        public string Password { get; set; }
    }

    [Serializable]
    public class ServersSavingData
    {
        public List<ServerInfo> Servers { get; set; }

        public ServersSavingData()
        {

        }
    }

    [Serializable]
    public class UsersSavingData2
    {
        public List<ShortUserInfo> Users { get; set; }

        public UsersSavingData2()
        {

        }
    }

    [Serializable]
    public class ShortUserInfo
    {
        public string Nickname { get; set; }
        public string Login { get; set; }

        public ShortUserInfo()
        {

        }
    }
}
