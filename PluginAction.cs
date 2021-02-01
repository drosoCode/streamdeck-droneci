using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Drawing;
using System.Timers;
using System.IO;

namespace droneci
{
    [PluginActionId("com.drosocode.droneci")]
    public class PluginAction : PluginBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings();
                instance.repoOwner = String.Empty;
                instance.repoName = String.Empty;
                instance.url = String.Empty;
                instance.token = null;
                instance.target = String.Empty;
                instance.mode = String.Empty;
                instance.timer = "5";
                return instance;
            }

            [JsonProperty(PropertyName = "repoOwner")]
            public string repoOwner { get; set; }

            [JsonProperty(PropertyName = "repoName")]
            public string repoName { get; set; }

            [JsonProperty(PropertyName = "url")]
            public string url { get; set; }

            [JsonProperty(PropertyName = "token")]
            public string token { get; set; }

            [JsonProperty(PropertyName = "target")]
            public string target { get; set; }

            [JsonProperty(PropertyName = "mode")]
            public string mode { get; set; }

            [JsonProperty(PropertyName = "timer")]
            public string timer { get; set; }
        }

        #region Private Members

        private PluginSettings settings;
        private Timer timer;
        private bool timer_short = false;
        private int status = 2;


        #endregion
        public PluginAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "INIT");
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                this.settings = PluginSettings.CreateDefaultSettings();
            }
            else
            {
                this.settings = payload.Settings.ToObject<PluginSettings>();
            }

            // Create a timer (in minutes)
            timer = new Timer(Int16.Parse(settings.timer) * 60000);
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;
            timer.Enabled = true;
            updateStatus();
        }

        public override void Dispose()
        {
            timer.Enabled = false;
            timer.Dispose();
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Destructor called");
        }

        public async override void KeyPressed(KeyPayload payload)
        {
        }

        public async override void KeyReleased(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Key Released");
            String baseUrl = settings.url + "/api/repos/" + settings.repoOwner + "/" + settings.repoName + "/builds/";

            if (settings.mode == "status")
            {
                updateStatus();
            }
            else if(settings.mode == "promote")
            {
                if(this.status == 3)
                {
                    updateTimer(true);
                    dynamic list = apiRequest(baseUrl, "GET", settings.token);
                    dynamic resp = apiRequest(baseUrl + list[0].number + "/promote?target=" + settings.target, "POST", settings.token);
                }
                updateStatus();
            }
        }


        public override void OnTick() { }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(settings, payload.Settings);
            SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }


        #region Private Methods

        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }
        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            updateStatus();
        }

        private void updateTimer(bool ts=false)
        {
            timer.Enabled = false;
            if(!this.timer_short && ts)
            {
                timer.Interval = 30000; //30 sec
                this.timer_short = true;
            }
            else if(this.timer_short && !ts)
            {
                timer.Interval = Int16.Parse(settings.timer) * 60000; //settings in minutes
                this.timer_short = false;
            }
            timer.Enabled = true;
        }

        private void updateStatus()
        {
            if (settings.mode == "status")
            {
                dynamic resp = apiRequest(settings.url + "/api/repos/" + settings.repoOwner + "/" + settings.repoName + "/builds", "GET", settings.token);
                if (resp[0].status == "running")
                {
                    updateTimer(true);
                    setImage(1);
                }
                else
                {
                    updateTimer(false);
                    if (resp[0].status == "success")
                        setImage(0);
                    else
                        setImage(2);
                }
            }
            else if (settings.mode == "promote")
            {
                dynamic resp = apiRequest(settings.url + "/api/repos/" + settings.repoOwner + "/" + settings.repoName + "/builds", "GET", settings.token);
                if (resp[0].status == "running")
                {
                    updateTimer(true);
                    setImage(1);
                }
                else
                {
                    updateTimer(false);
                    if (resp[0].status == "success")
                    {
                        if (resp[0]["event"] == "promote")
                            setImage(0);
                        else
                            setImage(3);
                    }
                    else
                    {
                        setImage(2);
                    }
                }
            }
        }

        private void setImage(int status)
        {
            this.status = status;
            String imgName = "";

            switch(status)
            {
                case 0:
                    imgName = "ok.png";
                break;
                case 1:
                    imgName = "progress.png";
                break;
                case 2:
                    imgName = "error.png";
                break;
                case 3:
                    imgName = "deploy.png";
                break;
            }
            String path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", imgName);
            Connection.SetImageAsync(Tools.FileToBase64(path, true));
        }

        private dynamic apiRequest(String url, String method, String token = null, String data=null)
        {
            var httpClient = new HttpClient();
            if(token != null)
                httpClient.DefaultRequestHeaders.Add("Authorization", String.Format("Bearer {0}", token));
            httpClient.DefaultRequestHeaders.Add("Contant-Type", "application/json");
            var request = new HttpRequestMessage(new HttpMethod(method), url);
            if(data != null)
                request.Content = new StringContent(data);
            var response = httpClient.SendAsync(request).Result;
            String resp = response.Content.ReadAsStringAsync().Result;
            return JsonConvert.DeserializeObject(resp);
        }

        #endregion
    }
}