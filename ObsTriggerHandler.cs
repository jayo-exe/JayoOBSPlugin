using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using JayoOBSPlugin.OBSWebsocketDotNet;
using UnityEngine;
using System.Globalization;

namespace JayoOBSPlugin
{
    class ObsTriggerHandler : VNyanInterface.ITriggerHandler
    {
        private static Dictionary<string, Action<int, int, int, string, string, string>> actionHandlers = new Dictionary<string, Action<int, int, int, string, string, string>>
        {
            ["switch_scene"] = handleSceneSwitchRequest,
            ["fire_hotkey"] = handleFireHotkeyRequest,
            ["audio_mute"] = handleAudioMuteRequest,
            ["audio_unmute"] = handleAudioUnmuteRequest,
            ["audio_setvolume"] = handleAudioSetVolumeRequest,
            ["item_enable"] = handleItemEnableRequest,
            ["item_disable"] = handleItemDisableRequest,
            ["filter_enable"] = handleSourceFilterEnableRequest,
            ["filter_disable"] = handleSourceFilterDisableRequest,
            ["vcam_start"] = handleVCamStartRequest,
            ["vcam_stop"] = handleVCamStopRequest,
            ["record_start"] = handleRecordStartRequest,
            ["record_stop"] = handleRecordStopRequest,
            ["stream_start"] = handleStreamStartRequest,
            ["stream_stop"] = handleStreamStopRequest,
            ["get_input_setting"] = handleGetInputSettingRequest,
            ["set_input_setting"] = handleSetInputSettingRequest,
            ["get_filter_setting"] = handleGetFilterSettingRequest,
            ["set_filter_setting"] = handleSetFilterSettingRequest,

        };

        public void triggerCalled(string triggerName, int value1, int value2, int value3, string text1, string text2, string text3)
        {
            if (!triggerName.StartsWith("_xjo_")) return;
            if (ObsManager.obs == null) return;
            if (!ObsManager.isConnected) return;

            string triggerAction = triggerName.Substring(5);
            if (actionHandlers.ContainsKey(triggerAction)) actionHandlers[triggerAction](value1, value2, value3, text1, text2, text3);
        }

        private static string getVNyanParameterString(string name) { return VNyanInterface.VNyanInterface.VNyanParameter.getVNyanParameterString(name); }
        
        private static float getVNyanParameterFloat(string name) { return VNyanInterface.VNyanInterface.VNyanParameter.getVNyanParameterFloat(name); }

        private static void setVNyanParameterString(string name, string value) { VNyanInterface.VNyanInterface.VNyanParameter.setVNyanParameterString(name, value); }
        
        private static void setVNyanParameterFloat(string name, float value) { VNyanInterface.VNyanInterface.VNyanParameter.setVNyanParameterFloat(name, value); }

        public static string parseStringArgument(string arg)
        {
            if (arg.StartsWith("<") && arg.EndsWith(">"))
            {
                return getVNyanParameterString(arg.Substring(1, arg.Length - 2));
            }
            return arg;
        }

        public static float parseFloatArgument(string arg)
        {
            if (arg.StartsWith("[") && arg.EndsWith("]"))
            {
                //float param, just get and return the value directly
                return getVNyanParameterFloat(arg.Substring(1, arg.Length - 2));
            }

            if (arg.StartsWith("<") && arg.EndsWith(">"))
            {
                //string param, set arg to the value from the parameters list before parsing
                arg = getVNyanParameterString(arg.Substring(1, arg.Length - 2));
            }

            //parse the value of arg into a float
            float returnVal = 0f;
            float.TryParse(arg, NumberStyles.Any, CultureInfo.InvariantCulture, out returnVal);
            return returnVal;
        }

        public static void setObsSocket(OBSWebsocket socket)
        {
            ObsManager.obs = socket;
        }

        public static void handleSceneSwitchRequest(int value1, int value2, int value3, string text1, string text2, string text3)
        {
            string newSceneName = parseStringArgument(text1);

            ObsManager.obs.SetCurrentProgramScene(newSceneName);
        }

        public static void handleFireHotkeyRequest(int value1, int value2, int value3, string text1, string text2, string text3)
        {
            string hotkeyName = parseStringArgument(text1);

            ObsManager.obs.TriggerHotkeyByName(hotkeyName);
        }

        public static void handleAudioMuteRequest(int value1, int value2, int value3, string text1, string text2, string text3)
        {
            string audioSourceName = parseStringArgument(text1);

            ObsManager.obs.SetInputMute(audioSourceName, true);
        }

        public static void handleAudioUnmuteRequest(int value1, int value2, int value3, string text1, string text2, string text3)
        {
            string audioSourceName = parseStringArgument(text1);

            ObsManager.obs.SetInputMute(audioSourceName, false);
        }

        public static void handleAudioSetVolumeRequest(int value1, int value2, int value3, string text1, string text2, string text3)
        {
            string audioSourceName = parseStringArgument(text1);
            float newVolume = parseFloatArgument(text3);

            ObsManager.obs.SetInputVolume(audioSourceName, newVolume);
        }

        public static void handleItemEnableRequest(int value1, int value2, int value3, string text1, string text2, string text3)
        {
            string[] targetParts = parseStringArgument(text1).Split(new string[] { ";;" }, StringSplitOptions.None);
            if (targetParts.Length < 2) return;
            string sceneName = targetParts[0];
            string sourceName = targetParts[1];

            int itemId = ObsManager.obs.GetSceneItemId(sceneName, sourceName, 0);
            ObsManager.obs.SetSceneItemEnabled(sceneName, itemId, true);
        }

        public static void handleItemDisableRequest(int value1, int value2, int value3, string text1, string text2, string text3)
        {
            string[] targetParts = parseStringArgument(text1).Split(new string[] { ";;" }, StringSplitOptions.None);
            if (targetParts.Length < 2) return;
            string sceneName = targetParts[0];
            string sourceName = targetParts[1];

            int itemId = ObsManager.obs.GetSceneItemId(sceneName, sourceName, 0);
            ObsManager.obs.SetSceneItemEnabled(sceneName, itemId, false);
        }

        public static void handleSourceFilterEnableRequest(int value1, int value2, int value3, string text1, string text2, string text3)
        {
            string[] targetParts = parseStringArgument(text1).Split(new string[] { ";;" }, StringSplitOptions.None);
            if (targetParts.Length < 2) return;
            string sourceName = targetParts[0];
            string filterName = targetParts[1];

            ObsManager.obs.SetSourceFilterEnabled(sourceName, filterName, true);
        }

        public static void handleSourceFilterDisableRequest(int value1, int value2, int value3, string text1, string text2, string text3)
        {
            string[] targetParts = parseStringArgument(text1).Split(new string[] { ";;" }, StringSplitOptions.None);
            if (targetParts.Length < 2) return;
            string sourceName = targetParts[0];
            string filterName = targetParts[1];

            ObsManager.obs.SetSourceFilterEnabled(sourceName, filterName, false);
        }

        public static void handleVCamStartRequest(int value1, int value2, int value3, string text1, string text2, string text3)
        {
            ObsManager.obs.StartVirtualCam();
        }

        public static void handleVCamStopRequest(int value1, int value2, int value3, string text1, string text2, string text3)
        {
            ObsManager.obs.StopVirtualCam();
        }

        public static void handleRecordStartRequest(int value1, int value2, int value3, string text1, string text2, string text3)
        {
            ObsManager.obs.StartRecord();
        }

        public static void handleRecordStopRequest(int value1, int value2, int value3, string text1, string text2, string text3)
        {
            ObsManager.obs.StopRecord();
        }

        public static void handleStreamStartRequest(int value1, int value2, int value3, string text1, string text2, string text3)
        {
            ObsManager.obs.StartStream();
        }

        public static void handleStreamStopRequest(int value1, int value2, int value3, string text1, string text2, string text3)
        {
            ObsManager.obs.StopStream();
        }

        public static void handleGetInputSettingRequest(int value1, int value2, int value3, string text1, string text2, string text3)
        {
            string inputName = parseStringArgument(text1);
            string settingName = parseStringArgument(text2);
            string targetParameterName = parseStringArgument(text3);

            OBSWebsocketDotNet.Types.InputSettings inputSet = ObsManager.obs.GetInputSettings(inputName);
            Logger.LogInfo(ObsManager.obs.GetInputDefaultSettings(inputSet.InputKind).ToString());
            JObject baseSettings = ObsManager.obs.GetInputDefaultSettings(inputSet.InputKind).ToObject<JObject>();
            baseSettings.Merge(inputSet.Settings);

            VNyanInterface.VNyanInterface.VNyanParameter.setVNyanParameterString(targetParameterName, baseSettings[settingName].ToString());
        }

        public static void handleSetInputSettingRequest(int value1, int value2, int value3, string text1, string text2, string text3)
        {
            
            string inputName = parseStringArgument(text1);
            string settingName = parseStringArgument(text2);
            string newValue = parseStringArgument(text3);

            OBSWebsocketDotNet.Types.InputSettings inputSet = ObsManager.obs.GetInputSettings(inputName);
            JObject baseSettings = ObsManager.obs.GetInputDefaultSettings(inputSet.InputKind).ToObject<JObject>();
            baseSettings.Merge(inputSet.Settings);

            switch (baseSettings[settingName].Type)
            {
                case JTokenType.Boolean:
                    baseSettings[settingName] = Convert.ToBoolean(newValue);
                    break;
                case JTokenType.Integer:
                    baseSettings[settingName] = Convert.ToInt32(newValue);
                    break;
                case JTokenType.Float:
                    baseSettings[settingName] = Convert.ToSingle(newValue);
                    break;
                default:
                    baseSettings[settingName] = newValue;
                    break;
            }

            inputSet.Settings.Merge(baseSettings);
            ObsManager.obs.SetInputSettings(inputSet);
        }

        public static void handleGetFilterSettingRequest(int value1, int value2, int value3, string text1, string text2, string text3)
        {
            string[] targetParts = parseStringArgument(text1).Split(new string[] { ";;" }, StringSplitOptions.None);
            if (targetParts.Length < 2) return;
            string sourceName = targetParts[0];
            string filterName = targetParts[1];
            string settingName = parseStringArgument(text2);
            string targetParameterName = parseStringArgument(text3);


            OBSWebsocketDotNet.Types.FilterSettings filterSet = ObsManager.obs.GetSourceFilter(sourceName, filterName);
            JObject baseSettings = ObsManager.obs.GetSourceFilterDefaultSettings(filterSet.Kind)["defaultFilterSettings"].ToObject<JObject>();
            baseSettings.Merge(filterSet.Settings);
            
            VNyanInterface.VNyanInterface.VNyanParameter.setVNyanParameterString(targetParameterName, baseSettings[settingName].ToString());
            
        }

        public static void handleSetFilterSettingRequest(int value1, int value2, int value3, string text1, string text2, string text3)
        {
            string[] targetParts = parseStringArgument(text1).Split(new string[] { ";;" }, StringSplitOptions.None);
            if (targetParts.Length < 2) return;
            string sourceName = targetParts[0];
            string filterName = targetParts[1];
            string settingName = parseStringArgument(text2);
            string newValue = parseStringArgument(text3);

            OBSWebsocketDotNet.Types.FilterSettings filterSet = ObsManager.obs.GetSourceFilter(sourceName, filterName);
            JObject baseSettings = ObsManager.obs.GetSourceFilterDefaultSettings(filterSet.Kind)["defaultFilterSettings"].ToObject<JObject>();
            baseSettings.Merge(filterSet.Settings);

            switch (baseSettings[settingName].Type)
            {
                case JTokenType.Boolean:
                    baseSettings[settingName] = Convert.ToBoolean(newValue);
                    break;
                case JTokenType.Integer:
                    baseSettings[settingName] = Convert.ToInt32(newValue);
                    break;
                case JTokenType.Float:
                    baseSettings[settingName] = Convert.ToSingle(newValue);
                    break;
                default:
                    baseSettings[settingName] = newValue;
                    break;
            }

            ObsManager.obs.SetSourceFilterSettings(sourceName, filterName, baseSettings);
        }

    }
}
