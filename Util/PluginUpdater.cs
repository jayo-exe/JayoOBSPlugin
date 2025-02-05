using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace JayoOBSPlugin.Util
{
    class PluginUpdater
    {
        public event Action<string> OpenUrlRequested;
        
        private string repoName;
        private string currentVersion;
        private string updateLink;
        private string latestVersion;

        private bool updateAvailable = false;

        public PluginUpdater(string repoName, string currentVersion, string updateLink)
        {
            this.repoName = repoName;
            this.currentVersion = currentVersion;
            this.updateLink = updateLink;
        }

        public void CheckForUpdates()
        {
            try
            {
                HttpWebRequest Request = (HttpWebRequest)WebRequest.Create($"https://api.github.com/repos/{repoName}/releases");
                Request.UserAgent = "request";
                HttpWebResponse response = (HttpWebResponse)Request.GetResponse();
                StreamReader Reader = new StreamReader(response.GetResponseStream());
                string JsonResponse = Reader.ReadToEnd();
                JArray Releases = JArray.Parse(JsonResponse);
                latestVersion = Releases[0]["tag_name"].ToString();
                updateAvailable = currentVersion != latestVersion;
            }
            catch (Exception e)
            {
                Logger.LogInfo($"Couldn't check for updates: {e.Message}");
            }
        }

        public void PrepareUpdateUI(GameObject versionText, GameObject updateText, GameObject updateButton)
        {

            versionText.GetComponent<Text>().text = currentVersion;
            updateText.GetComponent<Text>().text = $"New Update Available: {latestVersion}";
            updateButton.GetComponent<Button>().onClick.AddListener(() => { OpenUrlRequested.Invoke(updateLink); });

            if (!updateAvailable)
            {
                updateText.SetActive(false);
                updateButton.SetActive(false);
            }
        }

    }
}
