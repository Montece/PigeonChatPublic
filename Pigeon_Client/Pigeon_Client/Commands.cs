using System;
using System.Collections.Generic;

namespace Pigeon_Client
{
    [Serializable]
    public class ClientCommand
    {
        public ClientCommands CommandID { get; set; }
        public string Sender { get; set; }
        public List<string> Parameters { get; set; }

        public ClientCommand()
        {

        }
    }

    [Serializable]
    public class ServerCommand
    {
        public ServerCommands CommandID { get; set; }
        public List<string> Parameters { get; set; }

        public ServerCommand()
        {

        }
    }

    [Serializable]
    public class VoiceCommand
    {
        public string SID { get; set; }
        public string Login { get; set; }
        public bool Init { get; set; }
    }

    public enum ClientCommands
    {
        GetServerVersion,
        LogIn,
        Register,
        GetAllServers,
        ConnectToServer,
        LoadHistory,
        GetServerUsers,
        Disconnect,
        TextMessage,
        GetUpdate,
        HasFile,
        DownloadFile
    }

    public enum ServerCommands
    {
        SendServerVersion,
        SendLogInResult,
        SendRegisterResult,
        SendAllServers,
        ConnectToServerAnswer,
        Kick,
        ServerHistory,
        SendServerUsers,
        TextMessage,
        OnUserConnect,
        OnUserDisconnect,
        SendFileStatus
    }
}