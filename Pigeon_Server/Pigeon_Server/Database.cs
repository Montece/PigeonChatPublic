using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Pigeon_Server
{
    public static class Database
    {
        public static Dictionary<string, User> Users { get; private set; } = new Dictionary<string, User>();
        public static Dictionary<string, Server> Servers{ get; private set; } = new Dictionary<string, Server>();

        public static string UsersFolderPath = Environment.CurrentDirectory + @"/Data/users";
        public static string UsersFilePath;
        public static string UsersFilePath_backup;

        public static string ServersFolderPath = Environment.CurrentDirectory + @"/Data/servers";
        public static string ServersFilePath;
        public static string ServersFilePath_backup;

        public static string ServersHistoryFolder = Environment.CurrentDirectory + @"/Data/histories";

        public const string FilesPath = @"Files/";
        public const string ClientFilePath = @"Pigeon_Client.exe";

        static FileStream savingStreamU;
        static FileStream savingStream_backupU;
        static FileStream loadingStreamU;
        static FileStream savingStreamS;
        static FileStream savingStream_backupS;
        static FileStream loadingStreamS;
        static XmlSerializer UsersXML = new XmlSerializer(typeof(UsersSavingData));
        static XmlSerializer ServersXML = new XmlSerializer(typeof(ServersSavingData));
        static XmlSerializer UsersXML2 = new XmlSerializer(typeof(UsersSavingData2));
        public static XmlSerializer VoiceXML = new XmlSerializer(typeof(VoiceCommand));

        public static void Init()
        {
            UsersFilePath = UsersFolderPath + @"/Users.pigeon";
            UsersFilePath_backup = UsersFolderPath + @"/Users (backup).pigeon";
            ServersFilePath = ServersFolderPath + @"/Servers.pigeon";
            ServersFilePath_backup = ServersFolderPath + @"/Servers (backup).pigeon";
            if (!Directory.Exists(FilesPath)) Directory.CreateDirectory(FilesPath);

            LoadUsers();
            LoadServers();
            if (GetServersCount() == 0)
            {
                CreateServer("Developer server", "dev", 10);
            }
        }

        public static int GetUsersCount()
        {
            return Users.Count;
        }

        public static int GetServersCount()
        {
            return Servers.Count;
        }

        public static string GetServers()
        {
            ServersSavingData data = new ServersSavingData
            {
                Servers = new List<ServerInfo>()
            };
            foreach (Server server in Servers.Values) data.Servers.Add(server.Info);

            StringWriter writer = new StringWriter();
            ServersXML.Serialize(writer, data);
            return writer.ToString();
        }

        public static string GetUsers(string sid)
        {
            if (Servers.TryGetValue(sid, out Server server))
            {
                UsersSavingData2 data = new UsersSavingData2
                {
                    Users = new List<ShortUserInfo>()
                };
                foreach (User user in server.Users) data.Users.Add(new ShortUserInfo() { Nickname = user.Info.Nickname, Login = user.Info.Login });

                StringWriter writer = new StringWriter();
                UsersXML2.Serialize(writer, data);
                return writer.ToString();
            }
            return "";
        }

        public static bool CreateUser(string nickname, string login, string password, string email)
        {
            string recoverycode = "228"; //RANDOM 10 chars
            //SEND RECOVERY CODE TO EMAIL
            if (nickname.Length <= 2 || login.Length <= 2 || password.Length < 6 || !email.Contains("@")) return false;
            User user = new User()
            {
                Info = new UserInfo()
                {
                    Nickname = nickname,
                    Email = email,
                    Login = login,
                    Password = password,
                    RecoveryCode = recoverycode
                },
                CurrentServer = null,
                CommandsIpAddress = null
            };
            try
            {
                Users.Add(login, user);
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        public static bool CreateServer(string title, string sid, int maxuserscount, string password = "")
        {
            Server server = new Server(new ServerInfo()
            {
                Title = title,
                SID = sid,
                MaxUsersCount = maxuserscount,
                Password = password
            });
            Servers.Add(sid, server);
            SaveServers();
            return true;
        }

        public static void SetUserCommandsIP(string login, IPEndPoint end)
        {
            if (Users.TryGetValue(login, out User user))
            {
                user.CommandsIpAddress = end;
            }
        }

        public static void SetUserVoiceIP(string login, IPEndPoint end)
        {
            if (Users.TryGetValue(login, out User user))
            {
                user.VoiceIpAddress = end;
            }
        }

        public static bool HasUser(string login, string password)
        {
            if (!Users.ContainsKey(login)) return false;
            if (Users[login].Info.Password == password) return true;
            return false;
        }

        public static User GetUser(string login)
        {
            if (!Users.ContainsKey(login)) return null;
            else return Users[login];
        }

        public static User GetUser(int i)
        {
            if (i < Users.Count && i >= 0) return Users.ElementAt(i).Value;
            return null;
        }

        public static Server GetServer(int i)
        {
            if (i < Servers.Count && i >= 0) return Servers.ElementAt(i).Value;
            return null;
        }

        public static bool ConnectToServer(string login, string sid, IPEndPoint end)
        {
            if (Servers.TryGetValue(sid, out Server server))
            {
                return server.Connect(GetUser(login), "", end);
            }
            return false;
        }

        public static void OnConnect(string login, string sid, IPEndPoint end)
        {
            if (Servers.TryGetValue(sid, out Server server))
            {
                server.OnConnect(GetUser(login), end);
            }
        }

        public static void OnDisconnect(string login, string sid)
        {
            if (Servers.TryGetValue(sid, out Server server))
            {
                server.OnDisconnect(GetUser(login));
            }
        }

        public static bool DisconnectFromServer(string login, string sid, IPEndPoint end)
        {
            if (Servers.TryGetValue(sid, out Server server))
            {
                return server.Disconnet(GetUser(login), end);
            }
            return false;
        }

        public static void SendTextMessage(string sid, string text, string login)
        {
            if (Servers.TryGetValue(sid, out Server server))
            {
                server.SendTextMessage(GetUser(login), text);
            }
        }

        public static void SendVoice(VoiceCommand command, byte[] package)
        {
            if (Servers.TryGetValue(command.SID, out Server server))
            {
                foreach (User user in server.Users)
                {
                    if (user.Info.Login.Equals(command.Login)) continue;

                    Chat.VoiceUdp.Send(package, package.Length, user.VoiceIpAddress);
                }                
            }
        }

        public static string GetServerHistory(string sid)
        {
            if (Servers.TryGetValue(sid, out Server server)) return server.GetHistory();
            return "";
        }

        static void SaveUsers()
        {
            UsersSavingData data = new UsersSavingData() { Users = new List<UserInfo>() };
            foreach (User user in Users.Values) data.Users.Add(user.Info);
            if (!Directory.Exists(UsersFolderPath)) Directory.CreateDirectory(UsersFolderPath);

            if (File.Exists(UsersFilePath))
            {
                DoUsersBackup(data);
                File.Delete(UsersFilePath);
            }       
            if (savingStreamU == null) savingStreamU = new FileStream(UsersFilePath, FileMode.OpenOrCreate, FileAccess.Write);
            UsersXML.Serialize(savingStreamU, data);
        }

        static void SaveServers()
        {
            ServersSavingData data = new ServersSavingData() { Servers = new List<ServerInfo>() };
            foreach (Server server in Servers.Values) data.Servers.Add(server.Info);
            if (!Directory.Exists(ServersFolderPath)) Directory.CreateDirectory(ServersFolderPath);

            if (File.Exists(ServersFilePath))
            {
                DoServersBackup(data);
                File.Delete(ServersFilePath);
            }
            if (savingStreamS == null) savingStreamS = new FileStream(ServersFilePath, FileMode.OpenOrCreate, FileAccess.Write);
            ServersXML.Serialize(savingStreamS, data);
        }

        static void DoUsersBackup(UsersSavingData data)
        {
            if (File.Exists(UsersFilePath_backup)) File.Delete(UsersFilePath_backup);
            savingStream_backupU = new FileStream(UsersFilePath_backup, FileMode.OpenOrCreate, FileAccess.Write);
            UsersXML.Serialize(savingStream_backupU, data);
            savingStream_backupU.Close();
        }

        static void DoServersBackup(ServersSavingData data)
        {
            if (File.Exists(ServersFilePath_backup)) File.Delete(ServersFilePath_backup);
            savingStream_backupS = new FileStream(ServersFilePath_backup, FileMode.OpenOrCreate, FileAccess.Write);
            ServersXML.Serialize(savingStream_backupS, data);
            savingStream_backupS.Close();
        }

        static bool LoadUsers()
        {
            if (!Directory.Exists(UsersFolderPath)) Directory.CreateDirectory(UsersFolderPath);

            if (!File.Exists(UsersFilePath)) return false;
            if (loadingStreamU == null) loadingStreamU = new FileStream(UsersFilePath, FileMode.OpenOrCreate, FileAccess.Read);
            UsersSavingData data = (UsersSavingData)UsersXML.Deserialize(loadingStreamU);
            foreach (UserInfo user in data.Users) Users.Add(user.Login, new User() { Info = user } );
            loadingStreamU.Close();
            return true;
        }

        static bool LoadServers()
        {
            if (!Directory.Exists(ServersFolderPath)) Directory.CreateDirectory(ServersFolderPath);

            if (!File.Exists(ServersFilePath)) return false;
            if (loadingStreamS == null) loadingStreamS = new FileStream(ServersFilePath, FileMode.OpenOrCreate, FileAccess.Read);
            ServersSavingData data = (ServersSavingData)ServersXML.Deserialize(loadingStreamS);
            foreach (ServerInfo server in data.Servers) Servers.Add(server.SID, new Server(server));
            loadingStreamS.Close();
            return true;
        }

        public static void Stop()
        {
            SaveUsers();
            SaveServers();
            if (savingStreamU != null) savingStreamU.Close();
            savingStreamU = null;
            if (loadingStreamU != null) loadingStreamU.Close();
            loadingStreamU = null;
            if (savingStreamS != null) savingStreamS.Close();
            savingStreamS = null;
            if (loadingStreamS != null) loadingStreamS.Close();
            loadingStreamS = null;
        }
    }

    [Serializable]
    public class UsersSavingData
    {
        public List<UserInfo> Users { get; set; }

        public UsersSavingData()
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

    [Serializable]
    public class ServersSavingData
    {
        public List<ServerInfo> Servers { get; set; }

        public ServersSavingData()
        {

        }
    }
}
