using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using JayoOBSPlugin.OBSWebsocketDotNet;

namespace JayoOBSPlugin
{
    public class ObsManager : MonoBehaviour
    {

        public OBSWebsocket obs;
        private bool connected;
        
        public string serverAddress;
        public string serverPort;
        public string serverPassword;

        private void Start()
        {

        }

        private void Awake()
        {
            Debug.Log("[OBS Plugin] OBS Manager Awake");
            serverAddress = "127.0.0.1";
            serverPort = "4455";
            serverPassword = "";

            obs = new OBSWebsocket();
            obs.Connected += OnObsConnect;
            obs.Disconnected += OnObsDisconnect;
        }

        private void ModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                Debug.Log("[OBS Plugin] Exiting Play Mode");
                deInitObs();
            }
        }

        private void OnDestroy()
        {
            deInitObs();
        }

        private void OnApplicationQuit()
        {
            deInitObs();
        }

        private void OnObsConnect(object sender, EventArgs e)
        {
            connected = true;
        }

        private void OnObsDisconnect(object sender, OBSWebsocketDotNet.Communication.ObsDisconnectionInfo e)
        {
            connected = false;
            Debug.Log($"[OBS Plugin] obs disconnected: {e.ObsCloseCode} ; {e.DisconnectReason}");
        }

        public void initObs()
        {
            if(connected) { return; }
            obs.ConnectAsync($"ws://{serverAddress}:{serverPort}", serverPassword);
        }
        
        public void deInitObs()
        {
            if (!connected) { return; }
            obs.Disconnect();
        }

        public bool isConnected()
        {
            return connected;
        }

    }
}
