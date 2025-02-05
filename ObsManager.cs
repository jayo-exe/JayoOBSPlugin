using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using JayoOBSPlugin.OBSWebsocketDotNet;

namespace JayoOBSPlugin
{
    public static class ObsManager
    {
        private static bool connected;

        public static OBSWebsocket obs;
        public static string serverAddress;
        public static string serverPort;
        public static string serverPassword;

        public static bool isConnected {  get { return connected; } }

        static ObsManager()
        {
            Logger.LogInfo("OBS Manager Awake");
            serverAddress = "127.0.0.1";
            serverPort = "4455";
            serverPassword = "";

            obs = new OBSWebsocket();
            obs.Connected += OnObsConnect;
            obs.Disconnected += OnObsDisconnect;
        }

        private static void OnObsConnect(object sender, EventArgs e)
        {
            connected = true;
        }

        private static void OnObsDisconnect(object sender, OBSWebsocketDotNet.Communication.ObsDisconnectionInfo e)
        {
            connected = false;
            Logger.LogInfo($"obs disconnected: {e.ObsCloseCode} ; {e.DisconnectReason}");
        }

        public static void initObs()
        {
            if(connected) { return; }
            obs.ConnectAsync($"ws://{serverAddress}:{serverPort}", serverPassword);
        }
        
        public static void deInitObs()
        {
            if (!connected) { return; }
            obs.Disconnect();
        }
    }
}
