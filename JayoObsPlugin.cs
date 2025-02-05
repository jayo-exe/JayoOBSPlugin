using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Linq.Expressions;
using VNyanInterface;
using JayoOBSPlugin.Util;
using TMPro;

namespace JayoOBSPlugin
{
    public class JayoObsPlugin : MonoBehaviour, VNyanInterface.IButtonClickedHandler
    {
        public GameObject windowPrefab;
        
        private static JayoObsPlugin _instance;
        private GameObject window;

        private ObsTriggerHandler triggerHandler;
        private string[] sep;

        private PluginUpdater updater;

        private TMP_InputField AddressInput;
        private TMP_InputField PortInput;
        private TMP_InputField PasswordInput;
        private Button ConnectButton;
        private Button DisconnectButton;
        private TMP_Text StatusText;

        private string currentVersion = "v0.4.0";
        private string repoName = "jayo-exe/JayoOBSPlugin";
        private string updateLink = "https://jayo-exe.itch.io/obs-plugin-for-vnyan";

        public void Start()
        {
        }

        private void OnObsSceneChanged(object sender, OBSWebsocketDotNet.Types.Events.ProgramSceneChangedEventArgs e)
        {
            Logger.LogInfo("[OBS Plugin] Scene changed");
            setStringParam("_xjo_currentScene", e.SceneName);
            callTrigger("_xjo_sceneChanged", 0, 0, 0, "", "", "");
        }

        private void OnObsVolumeMeters(object sender, OBSWebsocketDotNet.Types.Events.InputVolumeMetersEventArgs e)
        {
            setStringParam("_xjo_volumeMeters", e.inputs.First<JObject>().ToString());
        }

        private void OnObsVirtualcamStateChanged(object sender, OBSWebsocketDotNet.Types.Events.VirtualcamStateChangedEventArgs e)
        {
            setFloatParam("_xjo_vCamActive", e.OutputState.IsActive ? 1 : 0);
            callTrigger(e.OutputState.IsActive ? "_xjo_vCamStarted" : "_xjo_vCamStopped", 0, 0, 0, "", "", "");
        }

        private void OnObsRecordStateChanged(object sender, OBSWebsocketDotNet.Types.Events.RecordStateChangedEventArgs e)
        {
            setFloatParam("_xjo_recordActive", e.OutputState.IsActive ? 1 : 0);
            callTrigger(e.OutputState.IsActive ? "_xjo_recordStarted" : "_xjo_recordStopped", 0, 0, 0, "", "", "");
            setFloatParam("_xjo_recordPaused", ObsManager.obs.GetRecordStatus().IsRecordingPaused ? 1 : 0);
        }

        private void OnObsStreamStateChanged(object sender, OBSWebsocketDotNet.Types.Events.StreamStateChangedEventArgs e)
        {
            setFloatParam("_xjo_streamActive", e.OutputState.IsActive ? 1 : 0);
            callTrigger(e.OutputState.IsActive ? "_xjo_streamStarted" : "_xjo_streamStopped", 0, 0, 0, "", "", "");
        }

        private void OnObsConnected(object sender, EventArgs e)
        {
            setStatusTitle("Connected To OBS");
            callTrigger("_xjo_obsConnected", 0, 0, 0, "", "", "");

            string scene = ObsManager.obs.GetCurrentProgramScene();
            setStringParam("_xjo_currentScene", scene);

            OBSWebsocketDotNet.Types.VirtualCamStatus vCamActive = ObsManager.obs.GetVirtualCamStatus();
            setFloatParam("_xjo_vCamActive", vCamActive.IsActive ? 1 : 0);

            OBSWebsocketDotNet.Types.RecordingStatus recordingActive = ObsManager.obs.GetRecordStatus();
            setFloatParam("_xjo_recordActive", recordingActive.IsRecording ? 1 : 0);
            setFloatParam("_xjo_recordPaused", recordingActive.IsRecordingPaused ? 1 : 0);

            OBSWebsocketDotNet.Types.OutputStatus streamActive = ObsManager.obs.GetStreamStatus();
            setFloatParam("_xjo_streamActive", streamActive.IsActive ? 1 : 0);
        }

        private void OnObsDisconnected(object sender, OBSWebsocketDotNet.Communication.ObsDisconnectionInfo e)
        {
            setStatusTitle($"Disconnected From OBS:\n{e.DisconnectReason}");
            callTrigger("_xjo_obsDisconnected", 0, 0, 0, "", "", "");

            setStringParam("_xjo_currentScene", "");
            setFloatParam("_xjo_vCamActive", 0);
            setFloatParam("_xjo_recordActive", 0);
            setFloatParam("_xjo_recordPaused", 0);
            setFloatParam("_xjo_streamActive", 0);

            deInitObs();
        }

        public void Awake()
        {

            if (_instance != null && _instance != this)
            {
                Logger.LogWarning("Plugin instance already exists, destroying this duplicate.");
                Destroy(gameObject);
                return;
            }
            else
            {
                _instance = this;
            }

            Logger.LogInfo($"Plugin is Awake!");
            sep = new string[] { ";;" };

            updater = new PluginUpdater(repoName, currentVersion, updateLink);
            updater.OpenUrlRequested += (url) => MainThreadDispatcher.Enqueue(() => { Application.OpenURL(url); });

            Logger.LogInfo($"Loading Settings");
            loadPluginSettings();
            updater.CheckForUpdates();

            ObsManager.obs.Connected += OnObsConnected;
            ObsManager.obs.Disconnected += OnObsDisconnected;
            ObsManager.obs.CurrentProgramSceneChanged += OnObsSceneChanged;
            ObsManager.obs.InputVolumeMeters += OnObsVolumeMeters;
            ObsManager.obs.VirtualcamStateChanged += OnObsVirtualcamStateChanged;
            ObsManager.obs.RecordStateChanged += OnObsRecordStateChanged;
            ObsManager.obs.StreamStateChanged += OnObsStreamStateChanged;

            triggerHandler = new ObsTriggerHandler();
            VNyanInterface.VNyanInterface.VNyanTrigger.registerTriggerListener(triggerHandler);
            Logger.LogInfo($"Beginning Plugin Setup");

            try
            {
                VNyanInterface.VNyanInterface.VNyanUI.registerPluginButton("Jayo's OBS Plugin", this);
                window = (GameObject)VNyanInterface.VNyanInterface.VNyanUI.instantiateUIPrefab(windowPrefab);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }

            // Hide the window by default
            if (window != null)
            {

                window.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
                window.SetActive(false);

                AddressInput = window.transform.Find("Panel/OBSServerInfo/Address/AddressField").GetComponent<TMP_InputField>();
                PortInput = window.transform.Find("Panel/OBSServerInfo/Port/PortField").GetComponent<TMP_InputField>();
                PasswordInput = window.transform.Find("Panel/OBSServerInfo/Password/PasswordField").GetComponent<TMP_InputField>();
                ConnectButton = window.transform.Find("Panel/OBSServerInfo/ConnectButton").GetComponent<Button>();
                DisconnectButton = window.transform.Find("Panel/OBSServerInfo/DisconnectButton").GetComponent<Button>();
                StatusText = window.transform.Find("Panel/StatusControls/Status Indicator").GetComponent<TMP_Text>();

                setStatusTitle("Initializing");

                try
                {
                    Logger.LogInfo($"Preparing Plugin Window");

                    updater.PrepareUpdateUI(
                        window.transform.Find("Panel/VersionText").gameObject,
                        window.transform.Find("Panel/UpdateText").gameObject,
                        window.transform.Find("Panel/UpdateButton").gameObject
                    );

                    AddressInput.onValueChanged.AddListener((v) => { ObsManager.serverAddress = v; });
                    AddressInput.SetTextWithoutNotify(ObsManager.serverAddress);
                    
                    PortInput.onValueChanged.AddListener((v) => { ObsManager.serverPort = v; });
                    PortInput.SetTextWithoutNotify(ObsManager.serverPort);
                    
                    PasswordInput.onValueChanged.AddListener((v) => { ObsManager.serverPassword = v; });
                    PasswordInput.SetTextWithoutNotify(ObsManager.serverPassword);

                    window.transform.Find("Panel/TitleBar/CloseButton").GetComponent<Button>().onClick.AddListener(() => { closePluginWindow(); });
                    ConnectButton.onClick.AddListener(() => {
                        initObs();
                    });
                    DisconnectButton.onClick.AddListener(() => {
                        deInitObs();
                    });

                }
                catch (Exception e)
                {
                    Logger.LogError($"Couldn't prepare Plugin Window: {e.Message}");
                }

                try
                {
                    initObs();
                }
                catch (Exception e)
                {
                    setStatusTitle($"Couldn't auto-initialize OBS Connection:\n{e.Message}");
                    deInitObs();
                }
            }
        }

        private string getVNyanParameterString(string name) { return VNyanInterface.VNyanInterface.VNyanParameter.getVNyanParameterString(name); }
        private float getVNyanParameterFloat(string name) { return VNyanInterface.VNyanInterface.VNyanParameter.getVNyanParameterFloat(name); }

        private void setVNyanParameterString(string name, string value) { VNyanInterface.VNyanInterface.VNyanParameter.setVNyanParameterString(name, value); }
        private void setVNyanParameterFloat(string name, float value) { VNyanInterface.VNyanInterface.VNyanParameter.setVNyanParameterFloat(name, value); }

        private void Update()
        {
            if (!ObsManager.isConnected) return;

            //check for items to handle
            //scene to switch
            string sceneToSwitch = getVNyanParameterString("_xjo_scenetoswitch");
            if (sceneToSwitch != "")
            {
                setStringParam("_xjo_scenetoswitch", "");
                ObsManager.obs.SetCurrentProgramScene(sceneToSwitch);
            }
            //Hotkey name to activate
            string hotkeyToFire = getVNyanParameterString("_xjo_hotkeytofire");
            if (hotkeyToFire != "")
            {
                setStringParam("_xjo_hotkeytofire", "");
                ObsManager.obs.TriggerHotkeyByName(hotkeyToFire);
            }

            //audio input to mute
            string audioToMute = getVNyanParameterString("_xjo_audiotomute");
            if (audioToMute != "")
            {
                setStringParam("_xjo_audiotomute", "");
                ObsManager.obs.SetInputMute(audioToMute, true);
            }

            //audio input to unmute
            string audioToUnmute = getVNyanParameterString("_xjo_audiotounmute");
            if (audioToUnmute != "")
            {
                setStringParam("_xjo_audiotounmute", "");
                ObsManager.obs.SetInputMute(audioToUnmute, false);

            }

            //audio input to set volume
            string audioToSet = getVNyanParameterString("_xjo_audiotoset");
            if (audioToSet != "")
            {
                setStringParam("_xjo_audiotoset", "");
                string[] audioParts = audioToSet.Split(sep, StringSplitOptions.None);
                ObsManager.obs.SetInputVolume(audioParts[0], float.Parse(audioParts[1]));

            }

            //item to enable
            string itemToEnable = getVNyanParameterString("_xjo_itemtoenable");
            if (itemToEnable != "")
            {
                setStringParam("_xjo_itemtoenable", "");
                string[] itemParts = itemToEnable.Split(sep, StringSplitOptions.None);
                int itemId = ObsManager.obs.GetSceneItemId(itemParts[0], itemParts[1], 0);
                ObsManager.obs.SetSceneItemEnabled(itemParts[0], itemId, true);

            }

            //item to disable
            string itemToDisable = getVNyanParameterString("_xjo_itemtodisable");
            if (itemToDisable != "")
            {
                setStringParam("_xjo_itemtodisable", "");
                string[] itemParts = itemToDisable.Split(sep, StringSplitOptions.None);
                int itemId = ObsManager.obs.GetSceneItemId(itemParts[0], itemParts[1], 0);
                ObsManager.obs.SetSceneItemEnabled(itemParts[0], itemId, false);

            }

            //filter to enable
            string filterToEnable = getVNyanParameterString("_xjo_filtertoenable");
            if (filterToEnable != "")
            {
                setStringParam("_xjo_filtertoenable", "");
                string[] filterParts = filterToEnable.Split(sep, StringSplitOptions.None);
                ObsManager.obs.SetSourceFilterEnabled(filterParts[0], filterParts[1], true);

            }

            //filter to disable
            string filterToDisable = getVNyanParameterString("_xjo_filtertodisable");
            if (filterToDisable != "")
            {
                setStringParam("_xjo_filtertodisable", "");
                string[] filterParts = filterToDisable.Split(sep, StringSplitOptions.None);
                ObsManager.obs.SetSourceFilterEnabled(filterParts[0], filterParts[1], false);

            }

            //Start Virtual Cam
            float startVirtualCam = getVNyanParameterFloat("_xjo_startvcam");
            if (startVirtualCam == 1)
            {
                setFloatParam("_xjo_startvcam", 0);
                ObsManager.obs.StartVirtualCam();

            }

            //Stop Virtual Cam
            float stopVirtualCam = getVNyanParameterFloat("_xjo_stopvcam");
            if (stopVirtualCam == 1)
            {
                setFloatParam("_xjo_stopvcam", 0);
                ObsManager.obs.StopVirtualCam();

            }

            //Start Record
            float startRecord = getVNyanParameterFloat("_xjo_startrecord");
            if (startRecord == 1)
            {
                setFloatParam("_xjo_startrecord", 0);
                ObsManager.obs.StartRecord();

            }

            //Stop Record
            float stopRecord = getVNyanParameterFloat("_xjo_stoprecord");
            if (stopRecord == 1)
            {
                setFloatParam("_xjo_stoprecord", 0);
                ObsManager.obs.StopRecord();

            }

            //Start Stream
            float startStream = getVNyanParameterFloat("_xjo_startstream");
            if (startStream == 1)
            {
                setFloatParam("_xjo_startstream", 0);
                ObsManager.obs.StartStream();

            }

            //Stop Stream
            float stopStream = getVNyanParameterFloat("_xjo_stopstream");
            if (stopStream == 1)
            {
                setFloatParam("_xjo_stopstream", 0);
                ObsManager.obs.StopStream();
            }

            //Input to fetch settings for
            string inputToFetch = getVNyanParameterString("_xjo_inputtofetch");
            if (inputToFetch != "")
            {
                setStringParam("_xjo_inputtofetch", "");
                string[] inputParts = inputToFetch.Split(sep, StringSplitOptions.None);

                OBSWebsocketDotNet.Types.InputSettings inputSet = ObsManager.obs.GetInputSettings(inputParts[0]);
                Debug.Log(inputSet.Settings.ToString());
                setStringParam("_xjo_inputvalue", inputSet.Settings[inputParts[1]].ToString());

            }

            //Input to change settings for
            string inputToUpdate = getVNyanParameterString("_xjo_inputtoupdate");
            string inputNewValue = getVNyanParameterString("_xjo_inputnewvalue");
            if (inputToUpdate != "")
            {
                setStringParam("_xjo_inputtoupdate", "");
                setStringParam("_xjo_inputnewvalue", "");

                string[] inputParts = inputToUpdate.Split(sep, StringSplitOptions.None);

                OBSWebsocketDotNet.Types.InputSettings inputSet = ObsManager.obs.GetInputSettings(inputParts[0]);

                setStringParam("_xjo_inputtype", inputSet.Settings[inputParts[1]].Type.ToString());
                switch (inputSet.Settings[inputParts[1]].Type)
                {
                    case JTokenType.Boolean:
                        inputSet.Settings[inputParts[1]] = Convert.ToBoolean(inputNewValue);
                        break;
                    case JTokenType.Integer:
                        inputSet.Settings[inputParts[1]] = Convert.ToInt32(inputNewValue);
                        break;
                    case JTokenType.Float:
                        inputSet.Settings[inputParts[1]] = Convert.ToSingle(inputNewValue);
                        break;
                    default:
                        inputSet.Settings[inputParts[1]] = inputNewValue;
                        break;
                }

                ObsManager.obs.SetInputSettings(inputSet);
            }
        }

        public void initObs()
        {
            if (ObsManager.serverAddress == "" || ObsManager.serverPort == null || Int32.Parse(ObsManager.serverPort) <= 0)
            {
                setStatusTitle("OBS IP and Port required");
                deInitObs();
                return;
            }
            MainThreadDispatcher.Enqueue(() => {
                ObsManager.initObs();
                ConnectButton.gameObject.SetActive(false);
                DisconnectButton.gameObject.SetActive(true);
            });
        }

        public void deInitObs()
        {
            MainThreadDispatcher.Enqueue(() => {
                ObsManager.deInitObs();
                ConnectButton.gameObject.SetActive(true);
                DisconnectButton.gameObject.SetActive(false);
            });
        }

        private void OnApplicationQuit()
        {
            // Save settings
            savePluginSettings();
        }

        public void loadPluginSettings()
        {
            // Get settings in dictionary
            Dictionary<string, string> settings = VNyanInterface.VNyanInterface.VNyanSettings.loadSettings("JayoOBSPlugin.cfg");
            if (settings != null)
            {
                // Read string value
                settings.TryGetValue("OBSServerAddress", out ObsManager.serverAddress);
                settings.TryGetValue("OBSServerPort", out ObsManager.serverPort);
                settings.TryGetValue("OBSServerPassword", out ObsManager.serverPassword);
            }
            else
            {
                ObsManager.serverAddress = "127.0.0.1";
                ObsManager.serverPort = "4455";
                ObsManager.serverPassword = "";
            }
        }

        public void savePluginSettings()
        {
            Dictionary<string, string> settings = new Dictionary<string, string>();
            settings["OBSServerAddress"] = ObsManager.serverAddress;
            settings["OBSServerPort"] = ObsManager.serverPort;
            settings["OBSServerPassword"] = ObsManager.serverPassword;

            VNyanInterface.VNyanInterface.VNyanSettings.saveSettings("JayoOBSPlugin.cfg", settings);
        }

        public void pluginButtonClicked()
        {
            // Flip the visibility of the window when plugin window button is clicked
            if (window != null)
            {
                window.SetActive(!window.activeSelf);
                if (window.activeSelf)
                    window.transform.SetAsLastSibling();
            }

        }

        public void closePluginWindow()
        {
            window.SetActive(false);
        }

        public void setStatusTitle(string titleText)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                StatusText.text = titleText;
            });
        }

        public void setStringParam(string paramName, string value)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                setVNyanParameterString(paramName, value);
            });
        }

        public void setFloatParam(string paramName, float value)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                setVNyanParameterFloat(paramName, value);
            });
        }

        public void callTrigger(string triggerName, int value1, int value2, int value3, string text1, string text2, string text3)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                VNyanInterface.VNyanInterface.VNyanTrigger.callTrigger(triggerName, value1, value2, value3, text1, text2, text3);
            });
        }
    }
}