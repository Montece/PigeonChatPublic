using System;
using System.Net;

namespace Pigeon_Server
{
    public class User
    {
        public UserInfo Info;
        public IPEndPoint CommandsIpAddress;
        public IPEndPoint VoiceIpAddress;
        public Server CurrentServer;
    }

    [Serializable]
    public struct UserInfo
    {
        public string Nickname;
        public string Email;
        public string Login;
        public string Password;
        public string RecoveryCode;
    }
}
