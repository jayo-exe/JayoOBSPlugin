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
using JayoOBSPlugin.VNyanPluginHelper;
using JJayoOBSPlugin.VNyanPluginHelper;
using TMPro;

namespace JayoOBSPlugin
{
    public class JayoObsPlugin : MonoBehaviour, VNyanInterface.IButtonClickedHandler
    {
        public GameObject windowPrefab;
        public GameObject window;

        public MainThreadDispatcher mainThread;
        private ObsManager obsManager;
        private ObsTriggerHandler triggerHandler;
        private string[] sep;

        private VNyanHelper _VNyanHelper;
        private VNyanPluginUpdater updater;

        private string currentVersion = "v0.4.0";
        private string repoName = "jayo-exe/JayoOBSPlugin";
        private string updateLink = "https://jayo-exe.itch.io/obs-plugin-for-vnyan";

        public void Start()
        {
        }

        private void OnObsSceneChanged(object sender, OBSWebsocketDotNet.Types.Events.ProgramSceneChangedEventArgs e)
        {
            //Debug.Log("[OBS Plugin] Scene changed");
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
            setFloatParam("_xjo_recordPaused", obsManager.obs.GetRecordStatus().IsRecordingPaused ? 1 : 0);
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

            string scene = obsManager.obs.GetCurrentProgramScene();
            setStringParam("_xjo_currentScene", scene);

            OBSWebsocketDotNet.Types.VirtualCamStatus vCamActive = obsManager.obs.GetVirtualCamStatus();
            setFloatParam("_xjo_vCamActive", vCamActive.IsActive ? 1 : 0);

            OBSWebsocketDotNet.Types.RecordingStatus recordingActive = obsManager.obs.GetRecordStatus();
            setFloatParam("_xjo_recordActive", recordingActive.IsRecording ? 1 : 0);
            setFloatParam("_xjo_recordPaused", recordingActive.IsRecordingPaused ? 1 : 0);

            OBSWebsocketDotNet.Types.OutputStatus streamActive = obsManager.obs.GetStreamStatus();
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

        /*
        private void OnVNyanTrigger(string triggerValue)
        {
           setStringParam("_xjo_last_trigger", triggerValue);
        }
        */

        public void Awake()
        {

            Debug.Log($"[OBS Plugin] OBS is Awake!");
            sep = new string[] { ";;" };
            _VNyanHelper = new VNyanHelper();

            updater = new VNyanPluginUpdater(repoName, currentVersion, updateLink);
            updater.OpenUrlRequested += (url) => mainThread.Enqueue(() => { Application.OpenURL(url); });

            obsManager = gameObject.AddComponent<ObsManager>();
            mainThread = gameObject.AddComponent<MainThreadDispatcher>();
            //_VNyanHelper.registerTriggerListener(obsManager);

            Debug.Log($"[OBS Plugin] Loading Settings");
            // Load settings
            loadPluginSettings();
            updater.CheckForUpdates();

            obsManager.obs.Connected += OnObsConnected;
            obsManager.obs.Disconnected += OnObsDisconnected;
            obsManager.obs.CurrentProgramSceneChanged += OnObsSceneChanged;
            obsManager.obs.InputVolumeMeters += OnObsVolumeMeters;
            obsManager.obs.VirtualcamStateChanged += OnObsVirtualcamStateChanged;
            obsManager.obs.RecordStateChanged += OnObsRecordStateChanged;
            obsManager.obs.StreamStateChanged += OnObsStreamStateChanged;


            ObsTriggerHandler.setObsSocket(obsManager.obs);
            ObsTriggerHandler.setVNyanHelper(_VNyanHelper);
            triggerHandler = new ObsTriggerHandler();
            _VNyanHelper.registerTriggerListener(triggerHandler);
            Debug.Log($"[OBS Plugin] Beginning Plugin Setup");


            try
            {
                window = _VNyanHelper.pluginSetup(this, "Jayo's OBS Plugin", windowPrefab);
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }



            // Hide the window by default
            if (window != null)
            {

                window.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
                window.SetActive(false);

                TMP_InputField AddressInput = window.transform.Find("Panel/OBSServerInfo/Address/AddressField").GetComponent<TMP_InputField>();
                AddressInput?.onValueChanged.AddListener((v) => { obsManager.serverAddress = v; });
                AddressInput?.SetTextWithoutNotify(obsManager.serverAddress);

                TMP_InputField PortInput = window.transform.Find("Panel/OBSServerInfo/Port/PortField").GetComponent<TMP_InputField>();
                PortInput?.onValueChanged.AddListener((v) => { obsManager.serverPort = v; });
                PortInput?.SetTextWithoutNotify(obsManager.serverPort);

                TMP_InputField PasswordInput = window.transform.Find("Panel/OBSServerInfo/Password/PasswordField").GetComponent<TMP_InputField>();
                PasswordInput?.onValueChanged.AddListener((v) => { obsManager.serverPassword = v; });
                PasswordInput?.SetTextWithoutNotify(obsManager.serverPassword);

                setStatusTitle("Initializing");

                try
                {
                    Debug.Log($"[OBS Plugin] Preparing Plugin Window");

                    updater.PrepareUpdateUI(
                        window.transform.Find("Panel/VersionText").gameObject,
                        window.transform.Find("Panel/UpdateText").gameObject,
                        window.transform.Find("Panel/UpdateButton").gameObject
                    );

                    window.transform.Find("Panel/TitleBar/CloseButton").GetComponent<Button>().onClick.AddListener(() => { closePluginWindow(); });
                    window.transform.Find("Panel/OBSServerInfo/ConnectButton").GetComponent<Button>().onClick.AddListener(() => {
                        initObs();
                    });
                    window.transform.Find("Panel/OBSServerInfo/DisconnectButton").GetComponent<Button>().onClick.AddListener(() => {
                        deInitObs();
                    });

                }
                catch (Exception e)
                {
                    Debug.Log($"[OBS Plugin] Couldn't prepare Plugin Window: {e.Message}");
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

        private void Update()
        {
            if (!obsManager.isConnected()) return;

            //check for items to handle
            //scene to switch
            string sceneToSwitch = _VNyanHelper.getVNyanParameterString("_xjo_scenetoswitch");
            if (sceneToSwitch != "")
            {
                setStringParam("_xjo_scenetoswitch", "");
                obsManager.obs.SetCurrentProgramScene(sceneToSwitch);
            }
            //Hotkey name to activate
            string hotkeyToFire = _VNyanHelper.getVNyanParameterString("_xjo_hotkeytofire");
            if (hotkeyToFire != "")
            {
                setStringParam("_xjo_hotkeytofire", "");
                obsManager.obs.TriggerHotkeyByName(hotkeyToFire);
            }

            //audio input to mute
            string audioToMute = _VNyanHelper.getVNyanParameterString("_xjo_audiotomute");
            if (audioToMute != "")
            {
                setStringParam("_xjo_audiotomute", "");
                obsManager.obs.SetInputMute(audioToMute, true);
            }

            //audio input to unmute
            string audioToUnmute = _VNyanHelper.getVNyanParameterString("_xjo_audiotounmute");
            if (audioToUnmute != "")
            {
                setStringParam("_xjo_audiotounmute", "");
                obsManager.obs.SetInputMute(audioToUnmute, false);

            }

            //audio input to set volume
            string audioToSet = _VNyanHelper.getVNyanParameterString("_xjo_audiotoset");
            if (audioToSet != "")
            {
                setStringParam("_xjo_audiotoset", "");
                string[] audioParts = audioToSet.Split(sep, StringSplitOptions.None);
                obsManager.obs.SetInputVolume(audioParts[0], float.Parse(audioParts[1]));

            }

            //item to enable
            string itemToEnable = _VNyanHelper.getVNyanParameterString("_xjo_itemtoenable");
            if (itemToEnable != "")
            {
                setStringParam("_xjo_itemtoenable", "");
                string[] itemParts = itemToEnable.Split(sep, StringSplitOptions.None);
                int itemId = obsManager.obs.GetSceneItemId(itemParts[0], itemParts[1], 0);
                obsManager.obs.SetSceneItemEnabled(itemParts[0], itemId, true);

            }

            //item to disable
            string itemToDisable = _VNyanHelper.getVNyanParameterString("_xjo_itemtodisable");
            if (itemToDisable != "")
            {
                setStringParam("_xjo_itemtodisable", "");
                string[] itemParts = itemToDisable.Split(sep, StringSplitOptions.None);
                int itemId = obsManager.obs.GetSceneItemId(itemParts[0], itemParts[1], 0);
                obsManager.obs.SetSceneItemEnabled(itemParts[0], itemId, false);

            }

            //filter to enable
            string filterToEnable = _VNyanHelper.getVNyanParameterString("_xjo_filtertoenable");
            if (filterToEnable != "")
            {
                setStringParam("_xjo_filtertoenable", "");
                string[] filterParts = filterToEnable.Split(sep, StringSplitOptions.None);
                obsManager.obs.SetSourceFilterEnabled(filterParts[0], filterParts[1], true);

            }

            //filter to disable
            string filterToDisable = _VNyanHelper.getVNyanParameterString("_xjo_filtertodisable");
            if (filterToDisable != "")
            {
                setStringParam("_xjo_filtertodisable", "");
                string[] filterParts = filterToDisable.Split(sep, StringSplitOptions.None);
                obsManager.obs.SetSourceFilterEnabled(filterParts[0], filterParts[1], false);

            }

            //Start Virtual Cam
            float startVirtualCam = _VNyanHelper.getVNyanParameterFloat("_xjo_startvcam");
            if (startVirtualCam == 1)
            {
                setFloatParam("_xjo_startvcam", 0);
                obsManager.obs.StartVirtualCam();

            }

            //Stop Virtual Cam
            float stopVirtualCam = _VNyanHelper.getVNyanParameterFloat("_xjo_stopvcam");
            if (stopVirtualCam == 1)
            {
                setFloatParam("_xjo_stopvcam", 0);
                obsManager.obs.StopVirtualCam();

            }

            //Start Record
            float startRecord = _VNyanHelper.getVNyanParameterFloat("_xjo_startrecord");
            if (startRecord == 1)
            {
                setFloatParam("_xjo_startrecord", 0);
                obsManager.obs.StartRecord();

            }

            //Stop Record
            float stopRecord = _VNyanHelper.getVNyanParameterFloat("_xjo_stoprecord");
            if (stopRecord == 1)
            {
                setFloatParam("_xjo_stoprecord", 0);
                obsManager.obs.StopRecord();

            }

            //Start Stream
            float startStream = _VNyanHelper.getVNyanParameterFloat("_xjo_startstream");
            if (startStream == 1)
            {
                setFloatParam("_xjo_startstream", 0);
                obsManager.obs.StartStream();

            }

            //Stop Stream
            float stopStream = _VNyanHelper.getVNyanParameterFloat("_xjo_stopstream");
            if (stopStream == 1)
            {
                setFloatParam("_xjo_stopstream", 0);
                obsManager.obs.StopStream();
            }

            //Input to fetch settings for
            string inputToFetch = _VNyanHelper.getVNyanParameterString("_xjo_inputtofetch");
            if (inputToFetch != "")
            {
                setStringParam("_xjo_inputtofetch", "");
                string[] inputParts = inputToFetch.Split(sep, StringSplitOptions.None);

                OBSWebsocketDotNet.Types.InputSettings inputSet = obsManager.obs.GetInputSettings(inputParts[0]);
                //Debug.Log(inputSet.Settings.ToString());
                setStringParam("_xjo_inputvalue", inputSet.Settings[inputParts[1]].ToString());

            }

            //Input to change settings for
            string inputToUpdate = _VNyanHelper.getVNyanParameterString("_xjo_inputtoupdate");
            string inputNewValue = _VNyanHelper.getVNyanParameterString("_xjo_inputnewvalue");
            if (inputToUpdate != "")
            {
                setStringParam("_xjo_inputtoupdate", "");
                setStringParam("_xjo_inputnewvalue", "");

                string[] inputParts = inputToUpdate.Split(sep, StringSplitOptions.None);

                OBSWebsocketDotNet.Types.InputSettings inputSet = obsManager.obs.GetInputSettings(inputParts[0]);

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

                obsManager.obs.SetInputSettings(inputSet);
            }
        }

        public void initObs()
        {
            if (obsManager.serverAddress == "" || Int32.Parse(obsManager.serverPort) <= 0)
            {
                setStatusTitle("OBS IP and Port required");
                deInitObs();
                return;
            }
            mainThread.Enqueue(() => {
                obsManager.initObs();
                window.transform.Find("Panel/StatusControls/ConnectButton").gameObject.SetActive(false);
                window.transform.Find("Panel/StatusControls/DisconnectButton").gameObject.SetActive(true);
            });
        }

        public void deInitObs()
        {
            mainThread.Enqueue(() => {
                obsManager.deInitObs();
                window.transform.Find("Panel/StatusControls/ConnectButton").gameObject.SetActive(true);
                window.transform.Find("Panel/StatusControls/DisconnectButton").gameObject.SetActive(false);
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
            Dictionary<string, string> settings = _VNyanHelper.loadPluginSettingsData("JayoOBSPlugin.cfg");
            if (settings != null)
            {
                // Read string value
                settings.TryGetValue("OBSServerAddress", out obsManager.serverAddress);
                settings.TryGetValue("OBSServerPort", out obsManager.serverPort);
                settings.TryGetValue("OBSServerPassword", out obsManager.serverPassword);
            }
            else
            {
                obsManager.serverAddress = "127.0.0.1";
                obsManager.serverPort = "4455";
                obsManager.serverPassword = "";
            }
        }

        public void savePluginSettings()
        {
            Dictionary<string, string> settings = new Dictionary<string, string>();
            settings["OBSServerAddress"] = obsManager.serverAddress;
            settings["OBSServerPort"] = obsManager.serverPort;
            settings["OBSServerPassword"] = obsManager.serverPassword;

            _VNyanHelper.savePluginSettingsData("JayoOBSPlugin.cfg", settings);
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
            mainThread.Enqueue(() =>
            {
                TMP_Text StatusTitle = window.transform.Find("Panel/StatusControls/Status Indicator").GetComponent<TMP_Text>();
                StatusTitle.text = titleText;
            });
        }

        public void setStringParam(string paramName, string value)
        {
            mainThread.Enqueue(() =>
            {
                _VNyanHelper.setVNyanParameterString(paramName, value);
            });
        }

        public void setFloatParam(string paramName, float value)
        {
            mainThread.Enqueue(() =>
            {
                _VNyanHelper.setVNyanParameterFloat(paramName, value);
            });
        }

        public void callTrigger(string triggerName, int value1, int value2, int value3, string text1, string text2, string text3)
        {
            mainThread.Enqueue(() =>
            {
                _VNyanHelper.callTrigger(triggerName, value1, value2, value3, text1, text2, text3);
            });
        }

        public void callSimpleTrigger(string triggerName)
        {
            mainThread.Enqueue(() =>
            {
                _VNyanHelper.callTrigger(triggerName, 0, 0, 0, "", "", "");
            });
        }

        public Dictionary<string, Dictionary<string, string>> GetTriggerList()
        {
            return new Dictionary<string, Dictionary<string, string>>
            {
                {

                    "_xjo_scenetoswitch",
                    new Dictionary<string, string>
                    {
                        {"name", "Change Scene"},
                        {"description", "The name of the Scene to switch over to"},
                        {"example", "MySceneName"},
                    }
                },
                {

                   "_xjo_hotkeytofire",
                    new Dictionary<string, string>
                    {
                        {"name", "Fire a Hotkey"},
                        {"description", "The name of the Hotkey to activate"},
                        {"example", "ObsBrowser.Refresh"},
                    }
                },
                {

                    "_xjo_audiotomute",
                    new Dictionary<string, string>
                    {
                        {"name", "Mute Audio Input"},
                        {"description", "The name of the Audio Input to mute"},
                        {"example", "MyInputName"},
                    }
                },
                {

                    "_xjo_audiotounmute",
                    new Dictionary<string, string>
                    {
                        {"name", "Unmute Autdio Input"},
                        {"description", "The name of the Audio Input to unmute"},
                        {"example", "MyInputName"},
                    }
                },
                {

                    "_xjo_audiotoset",
                    new Dictionary<string, string>
                    {
                        {"name", "Set Input Volume"},
                        {"description", "The name and desired volume of the audio input to adjust"},
                        {"example", "MyInputName;;69"},
                    }
                },
                {

                    "_xjo_itemtoenable",
                    new Dictionary<string, string>
                    {
                        {"name", "Enable Scene Item"},
                        {"description", "The name of the Scene and Source of the item to enable"},
                        {"example", "MySceneName;;MySourceName"},
                    }
                },
                {

                    "_xjo_itemtodisable",
                    new Dictionary<string, string>
                    {
                        {"name", "Disable Scene Item"},
                        {"description", "The name of the Scene and Source of the item to disable"},
                        {"example", "MySceneName;;MySourceName"},
                    }
                },
                {

                    "_xjo_filtertoenable",
                    new Dictionary<string, string>
                    {
                        {"name", "Enable Source Filter"},
                        {"description", "The name of the Source and Filter to enable"},
                        {"example", "MySourceName::MyFilterName"},
                    }
                },
                {

                    "_xjo_filtertodisable",
                    new Dictionary<string, string>
                    {
                        {"name", "Disable Source Filter"},
                        {"description", "The name of the Source and Filter to disable"},
                        {"example", "MySourceName::MyFilterName"},
                    }
                },
                {

                    "_xjo_startvcam",
                    new Dictionary<string, string>
                    {
                        {"name", "Enable Virtual Cam"},
                        {"description", "set to 1 to enable the OBS virtual camera"},
                        {"example", "1"},
                    }
                },
                {

                    "_xjo_stopvcam",
                    new Dictionary<string, string>
                    {
                        {"name", "Disable Virtual Cam"},
                        {"description", "set to 1 to disable the OBS virtual camera"},
                        {"example", "1"},
                    }
                },
                {

                    "_xjo_startrecord",
                    new Dictionary<string, string>
                    {
                        {"name", "Enable Recording"},
                        {"description", "set to 1 to start recording"},
                        {"example", "1"},
                    }
                },
                {

                    "_xjo_stoprecord",
                    new Dictionary<string, string>
                    {
                         {"name", "Disable Recording"},
                        {"description", "set to 1 to stop recording"},
                        {"example", "1"},
                        {"example", "1"},
                    }
                },
                {

                    "_xjo_startstream",
                    new Dictionary<string, string>
                    {
                        {"name", "Enable Streaming"},
                        {"description", "set to 1 to start streaming"},
                        {"example", "1"},
                    }
                },
                {

                    "_xjo_stopstream",
                    new Dictionary<string, string>
                    {
                         {"name", "Disable Streaming"},
                        {"description", "set to 1 to stop streaming"},
                        {"example", "1"},
                        {"example", "1"},
                    }
                },
            };

        }

    }
}