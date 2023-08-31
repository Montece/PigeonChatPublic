using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Xml.Serialization;

namespace Pigeon_Server
{
    [Serializable]
    public struct ServerInfo
    {
        public string Title { get; set; }
        public string SID { get; set; }
        public int MaxUsersCount { get; set; }
        public string Password { get; set; }
    }

    public class Server
    {
        const int GetHistoryLength = 128;
        public ServerInfo Info { get; private set; }
        public List<User> Users { get; private set; } = new List<User>();

        FileStream fs;

        public Server(ServerInfo info)
        {
            Info = info;
            if (!Directory.Exists(Database.ServersHistoryFolder)) Directory.CreateDirectory(Database.ServersHistoryFolder);
            string FilePath = Database.ServersHistoryFolder + @"/" + Info.SID + ".pigeon";
            fs = new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        }

        public User GetUser(int i)
        {
            if (i < Users.Count && i >= 0) return Users[i];
            return null;
        }

        void Alert(string text, IPEndPoint odd = null)
        {
            text += Environment.NewLine;
            byte[] ByteMessage = Encoding.UTF8.GetBytes(text);
            fs.Write(ByteMessage, 0, ByteMessage.Length);
            SendMessage(text, odd);
        }

        public string GetHistory()
        {
            if (fs.Length < 3) return "";
            byte[] history;
            int offset;

            if (fs.Length < GetHistoryLength)
            {
                offset = 0;
                history = new byte[fs.Length];
                fs.Read(history, offset, (int)fs.Length);
            }
            else
            {
                offset = (int)(fs.Length - GetHistoryLength - 1);
                history = new byte[GetHistoryLength];
                fs.Seek(offset, SeekOrigin.Begin);
                fs.Read(history, 0, GetHistoryLength - 1);
                fs.Seek(fs.Length, SeekOrigin.Begin);
            }         
           
            string toReturn = Encoding.UTF8.GetString(history);
            if (toReturn == null) return "";
            else return toReturn;
        }

        public void SendTextMessage(User sender, string text)
        {
            string message = $"{sender.Info.Nickname}: {text}";                  
            Alert(message);
        }

        void SendMessage(string text, IPEndPoint odd = null)
        {
            foreach (User user in Users)
            {
                if (odd == null) Chat.SendCommand(ServerCommands.TextMessage, user.CommandsIpAddress, text);
                else if (odd.ToString() != user.CommandsIpAddress.ToString()) Chat.SendCommand(ServerCommands.TextMessage, user.CommandsIpAddress, text);
            }
        }

        public bool Connect(User user, string password, IPEndPoint currentEnd)
        {
            if (user != null)
            {
                if (user.CurrentServer == null)
                {
                    if (Users.Count < Info.MaxUsersCount)
                    {
                        if (Info.Password == password)
                        {            
                            user.CurrentServer = this;
                            Users.Add(user);
                            return true;
                        }
                    }
                }
                else
                {
                    user.CurrentServer.Disconnet(user, currentEnd);
                    user.CurrentServer = null;
                    return Connect(user, "", currentEnd);
                }
            }           
            return false;
        }

        public void OnConnect(User user, IPEndPoint end)
        {
            ShortUserInfo info = new ShortUserInfo
            {
                Login = user.Info.Login,
                Nickname = user.Info.Nickname
            };
            Alert($"{user.Info.Nickname} присоединился", end);

            foreach (User u in Users)
            {
                if (u.CommandsIpAddress.ToString() != end.ToString()) Chat.SendCommand(ServerCommands.OnUserConnect, u.CommandsIpAddress, CreateStringFromShortInfo(info));
            }         
        }

        public void OnDisconnect(User user)
        {
            try
            {
                if (user == null)
                    return;
                ShortUserInfo info = new ShortUserInfo
                {
                    Login = user.Info.Login,
                    Nickname = user.Info.Nickname
                };
                Alert($"{user.Info.Nickname} отсоединился");
                
                foreach (User u in user.CurrentServer?.Users)
                {
                    if (u.Info.Login != user.Info.Login) Chat.SendCommand(ServerCommands.OnUserDisconnect, u.CommandsIpAddress, CreateStringFromShortInfo(info));
                }
            }
            catch(Exception ex) { }
            
        }

        string CreateStringFromShortInfo(ShortUserInfo info)
        {
            XmlSerializer UsersXML2 = new XmlSerializer(typeof(ShortUserInfo));
            StringWriter writer = new StringWriter();
            UsersXML2.Serialize(writer, info);
            return writer.ToString();
        }

        public bool Disconnet(User user, IPEndPoint currentEnd)
        {
            if (Users.Contains(user))
            {               
                if (user.CommandsIpAddress.Address.ToString() != currentEnd.Address.ToString()) Chat.Kick(currentEnd);
                user.CurrentServer = null;
                Users.Remove(user);
                return true;
            }
            return false;
        }

        ~Server()
        {
            if (fs != null) fs.Close();
        }
    }
}
