using AutoUpdaterDotNET;
using ControlzEx.Theming;
using IniParser;
using IniParser.Model;
using Newtonsoft.Json;
using RestSharp;
using RunOnStartup;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using uselessapp.ValNuget___Copy;
using ValAPINet;
using static uselessapp.App;

namespace uselessapp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public class data
        {
            public bool loggedin = false;
            public bool ingame = false;
            public Auth auth = null;
        }
        public Settings settings;
        public data AppData = new data();
        public bool runningLM = false;
        NotifyIcon icon = new NotifyIcon();
        public UserData userdata = null;
        public playerdata playerdata = null;
        public BackgroundWorker mainbw;
        public MainWindow()
        {
            InitializeComponent();
            ThemeManager.Current.ChangeTheme(this, "Dark.Red");
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
            if (File.Exists(performance.Faststring(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "/StatsVal/Settings.stval")))
            {
                string usrdata = File.ReadAllText(performance.Faststring(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "/StatsVal/Settings.stval"));
                settings = JsonConvert.DeserializeObject<Settings>(Secure.Decrypt(usrdata));
            }
            else
            {
                settings = new Settings();
                DirectoryInfo di = Directory.CreateDirectory(performance.Faststring(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "/StatsVal/"));
                File.WriteAllText(performance.Faststring(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "/StatsVal/Settings.stval"), Secure.encrypt(JsonConvert.SerializeObject(settings)));
                Startup.RunOnStartup();
            }
            AutoUpdater.InstalledVersion = new Version(System.Windows.Forms.Application.ProductVersion);
            AutoUpdater.Start("https://www.statsval.com/update.xml");
            AppData.ingame = false;
            AppData.loggedin = false;
            AppData.auth = null;
            icon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetEntryAssembly().ManifestModule.Name);
            icon.MouseDoubleClick += MainMouseDoubleClick;
            icon.ContextMenuStrip = new ContextMenuStrip();
            icon.ContextMenuStrip.Items.Add("Quit", null, this.quit);
            icon.Text = "StatsVal Companion";
            icon.Visible = true;
            if (File.Exists(performance.Faststring(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "/StatsVal/UserData.stval")))
            {
                string usrdata = File.ReadAllText(performance.Faststring(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "/StatsVal/UserData.stval"));
                userdata = JsonConvert.DeserializeObject<UserData>(Secure.Decrypt(usrdata));
                PlayerStats(userdata.subject, true);
            }
            mainbw = new BackgroundWorker
            {

                // this allows our worker to report progress during work
                WorkerReportsProgress = true
            };

            // what to do in the background thread
            mainbw.DoWork += new DoWorkEventHandler(
            delegate (object o, DoWorkEventArgs args)
            {
            start:;
                BackgroundWorker b = o as BackgroundWorker;
                //playerart.Image = null;
                data apdata = new data
                {
                    loggedin = false,
                    ingame = false,
                    auth = null
                };
                while (!runningLM)
                {
                    if (Process.GetProcessesByName("VALORANT-Win64-Shipping").Length > 0)
                    {
                        apdata.auth = Websocket.GetAuthLocal(false);
                    }
                    Thread.Sleep(2000);
                    if (apdata.auth != null)
                    {
                        apdata.loggedin = true;
                        while (apdata.ingame == false)
                        {
                            PregameGetPlayer pre = PregameGetPlayer.GetPlayer(apdata.auth);
                            if (pre.MatchID != null)
                            {
                                apdata.ingame = true;
                                Debug.WriteLine("In Game");
                                b.ReportProgress(1, apdata);
                            }
                            else
                            {
                                b.ReportProgress(0, apdata);
                                Debug.WriteLine("Logged In");
                                if (Process.GetProcessesByName("VALORANT-Win64-Shipping").Length == 0)
                                {
                                    goto start;
                                }
                            }
                            Thread.Sleep(3000);
                        }
                    }
                    else
                    {
                        Thread.Sleep(5000);
                    }
                }
            });

            // what to do when progress changed (update the progress bar for example)
            mainbw.ProgressChanged += new ProgressChangedEventHandler(
            delegate (object o, ProgressChangedEventArgs args)
            {
                data newAppData = (data)args.UserState;
                if (newAppData.ingame && !runningLM && settings.TrackLiveGames)
                {
                    LiveMatch(newAppData.auth);
                }
                if (newAppData.loggedin == true)
                {
                    UserData data = new UserData
                    {
                        subject = newAppData.auth.subject
                    };
                    if (userdata == null || userdata.subject != data.subject)
                    {
                        DirectoryInfo di = Directory.CreateDirectory(performance.Faststring(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "/StatsVal/"));
                        File.WriteAllText(performance.Faststring(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "/StatsVal/UserData.stval"), Secure.encrypt(JsonConvert.SerializeObject(data)));
                        userdata = data;
                        PlayerStats(data.subject, true);
                    }
                }
                AppData = newAppData;
            });

            // what to do when worker completes its task (notify the user)
            mainbw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
            delegate (object o, RunWorkerCompletedEventArgs args)
            {
                
            });

            mainbw.RunWorkerAsync();
        }
        private void MainMouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            this.Show();
            this.Focus();
        }

        private void quit(object sender, EventArgs e)
        {
            icon.Dispose();
            Environment.Exit(0);
        }
        public IniData ParceData;
        public FileIniDataParser parser;
        public string subject;
        private void LoadSettings()
        {
            parser = new FileIniDataParser();
            ParceData = parser.ReadFile(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\VALORANT\\Saved\\Config\\Windows\\RiotLocalMachine.ini");
            subject = ParceData["UserInfo"]["LastKnownUser"];
            ParceData = parser.ReadFile(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\VALORANT\\Saved\\Config\\" + subject + "\\Windows\\GameUserSettings.ini");
            string currentrez = ParceData["ScalabilityGroups"]["sg.ResolutionQuality"];
            float rez = float.Parse(currentrez);
            RezSlider.Value = rez;
            int res = (int)rez;
            RezText.Text = performance.Faststring(res.ToString(), "%");
            if (Startup.IsInStartup())
            {
                StartupSwitch.IsOn = true;
            }
            LiveGamesSwitch.IsOn = settings.TrackLiveGames;
            TraySwitch.IsOn = settings.MinimizeToTray;
            UpdateButton.Content = "Check";
            VersionText.Text = performance.Faststring("v", Assembly.GetExecutingAssembly().GetName().Version.ToString().Replace(".0", ""));
        }
        private void SaveSettings(object sender, RoutedEventArgs e)
        {
            float value = (float)RezSlider.Value;
            ParceData["ScalabilityGroups"]["sg.ResolutionQuality"] = value.ToString();
            parser.WriteFile(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\VALORANT\\Saved\\Config\\" + subject + "\\Windows\\GameUserSettings.ini", ParceData);
            settings.MinimizeToTray = TraySwitch.IsOn;
            settings.TrackLiveGames = LiveGamesSwitch.IsOn;
            if (StartupSwitch.IsOn == true)
            {
                Startup.RunOnStartup();
            }
            else
            {
                Startup.RemoveFromStartup();
            }
            File.WriteAllText(performance.Faststring(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "/StatsVal/Settings.stval"), Secure.encrypt(JsonConvert.SerializeObject(settings)));
        }
        private void RezSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            RezText.Text = (int)RezSlider.Value + "%";
        }

        public class Request
        {
            public string game_name { get; set; }
            public string game_tag { get; set; }
            public string name { get; set; }
            public string note { get; set; }
            public string pid { get; set; }
            public string puuid { get; set; }
            public string region { get; set; }
            public string subscription { get; set; }
        }

        public class Root
        {
            public List<Request> requests { get; set; }
        }
        public class Friend
        {
            public string displayGroup { get; set; }
            public string game_name { get; set; }
            public string game_tag { get; set; }
            public string group { get; set; }
            public object last_online_ts { get; set; }
            public string name { get; set; }
            public string note { get; set; }
            public string pid { get; set; }
            public string puuid { get; set; }
            public string region { get; set; }
        }

        public class Friends
        {
            public List<Friend> friends { get; set; }
        }
        public string usertoid(string name, string tag)
        {
            string lockfile = null;
            try
            {
                using (var fs = new FileStream(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Riot Games\\Riot Client\\Config\\lockfile", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs, Encoding.Default))
                {
                    lockfile = sr.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                return null;
            }
            string[] lf = lockfile.Split(':');
            RestClient AddFriendClient = new RestClient(new Uri($"https://127.0.0.1:{lf[2]}/chat/v4/friendrequests"))
            {
                RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
            };
            RestRequest AddFriendRequest = new RestRequest(Method.POST);
            AddFriendRequest.AddHeader("Authorization", $"Basic {Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"riot:{lf[3]}"))}");
            //AddFriendRequest.AddHeader("X-Riot-ClientPlatform", "ew0KCSJwbGF0Zm9ybVR5cGUiOiAiUEMiLA0KCSJwbGF0Zm9ybU9TIjogIldpbmRvd3MiLA0KCSJwbGF0Zm9ybU9TVmVyc2lvbiI6ICIxMC4wLjE5MDQyLjEuMjU2LjY0Yml0IiwNCgkicGxhdGZvcm1DaGlwc2V0IjogIlVua25vd24iDQp9");
            //AddFriendRequest.AddHeader("X-Riot-ClientVersion", auth.version);
            var obj = new
            {
                game_name = name,
                game_tag = tag
            };
            AddFriendRequest.AddJsonBody(obj);
            IRestResponse addResp = AddFriendClient.Post(AddFriendRequest);
            RestClient GetFriendClient = new RestClient(new Uri($"https://127.0.0.1:{lf[2]}/chat/v4/friendrequests"))
            {
                RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
            };
            RestRequest GetFriendRequest = new RestRequest(Method.GET);
            GetFriendRequest.AddHeader("Authorization", $"Basic {Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"riot:{lf[3]}"))}");
            //GetFriendRequest.AddHeader("X-Riot-ClientPlatform", "ew0KCSJwbGF0Zm9ybVR5cGUiOiAiUEMiLA0KCSJwbGF0Zm9ybU9TIjogIldpbmRvd3MiLA0KCSJwbGF0Zm9ybU9TVmVyc2lvbiI6ICIxMC4wLjE5MDQyLjEuMjU2LjY0Yml0IiwNCgkicGxhdGZvcm1DaGlwc2V0IjogIlVua25vd24iDQp9");
            //GetFriendRequest.AddHeader("X-Riot-ClientVersion", auth.version);
            IRestResponse checkResp = GetFriendClient.Get(GetFriendRequest);
            Root requests = JsonConvert.DeserializeObject<Root>(checkResp.Content);
            string puuid = null;
            foreach (Request req in requests.requests)
            {
                if (name.IndexOf(req.game_name, StringComparison.OrdinalIgnoreCase) >= 0 && tag.IndexOf(req.game_tag, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                restart:;
                    puuid = req.puuid;
                    RestClient RemoveFriendClient = new RestClient(new Uri($"https://127.0.0.1:{lf[2]}/chat/v4/friendrequests/"));
                    RemoveFriendClient.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
                    RestRequest RemoveFriendRequest = new RestRequest(Method.DELETE);
                    RemoveFriendRequest.AddHeader("Authorization", $"Basic {Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"riot:{lf[3]}"))}");
                    //RemoveFriendRequest.AddHeader("X-Riot-ClientPlatform", "ew0KCSJwbGF0Zm9ybVR5cGUiOiAiUEMiLA0KCSJwbGF0Zm9ybU9TIjogIldpbmRvd3MiLA0KCSJwbGF0Zm9ybU9TVmVyc2lvbiI6ICIxMC4wLjE5MDQyLjEuMjU2LjY0Yml0IiwNCgkicGxhdGZvcm1DaGlwc2V0IjogIlVua25vd24iDQp9");
                    //RemoveFriendRequest.AddHeader("X-Riot-ClientVersion", auth.version);
                    var objj = new
                    {
                        puuid
                    };
                    RemoveFriendRequest.AddJsonBody(objj);
                    IRestResponse deleteResp = RemoveFriendClient.Delete(RemoveFriendRequest);
                    return puuid;
                }
            }
            RestClient CheckFriendClient = new RestClient(new Uri($"https://127.0.0.1:{lf[2]}/chat/v4/friends"));
            CheckFriendClient.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            RestRequest CheckFriendRequest = new RestRequest(Method.GET);
            CheckFriendRequest.AddHeader("Authorization", $"Basic {Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"riot:{lf[3]}"))}");
            //CheckFriendRequest.AddHeader("X-Riot-ClientPlatform", "ew0KCSJwbGF0Zm9ybVR5cGUiOiAiUEMiLA0KCSJwbGF0Zm9ybU9TIjogIldpbmRvd3MiLA0KCSJwbGF0Zm9ybU9TVmVyc2lvbiI6ICIxMC4wLjE5MDQyLjEuMjU2LjY0Yml0IiwNCgkicGxhdGZvcm1DaGlwc2V0IjogIlVua25vd24iDQp9");
            //CheckFriendRequest.AddHeader("X-Riot-ClientVersion", auth.version);
            IRestResponse checkkResp = CheckFriendClient.Get(CheckFriendRequest);
            Friends friends = JsonConvert.DeserializeObject<Friends>(checkkResp.Content);
            foreach (Friend req in friends.friends)
            {
                if (name.IndexOf(req.game_name, StringComparison.OrdinalIgnoreCase) >= 0 && tag.IndexOf(req.game_tag, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    puuid = req.puuid;
                    return puuid;
                }
            }

            return null;
        }
        private void GetStats(object sender, RoutedEventArgs e)
        {
            if (!playername0.Text.Contains("#"))
            {
                playername0.Text = "";
                return;
            }
            string[] par = playername0.Text.Split('#');
            if (par[0] == "" || par[1] == "")
            {
                playername0.Text = "";
                return;
            }
            RestClient client = new RestClient($"https://api.henrikdev.xyz/valorant/v1/puuid/{par[0]}/{par[1]}");

            RestRequest request = new RestRequest(Method.GET);
            var responce = client.Execute(request);
            Puuid data = JsonConvert.DeserializeObject<Puuid>(responce.Content);
            if (data.data.puuid != null)
            {
                Home.Visibility = Visibility.Collapsed;
                Loader.Visibility = Visibility.Visible;
                HomeTitle.Foreground = Brushes.LightGray;
                StatsTitle.Foreground = Brushes.White;
                PlayerStats(data.data.puuid, false, true);
            }
            else
            {
                playername0.Text = "";
            }
        }
        private void PlayerStats(string playerid, bool isUser, bool isloading = false)
        {
            BackgroundWorker bw = new BackgroundWorker
            {

                // this allows our worker to report progress during work
                WorkerReportsProgress = true
            };

            // what to do in the background thread
            bw.DoWork += new DoWorkEventHandler(
            delegate (object o, DoWorkEventArgs args)
            {
                RestClient client = new RestClient(GetServer() + "/api/PlayerDatas/" + playerid);

                RestRequest request = new RestRequest(Method.GET);
                var responce = client.Execute(request);
                playerdata data = JsonConvert.DeserializeObject<playerdata>(responce.Content);
                args.Result = data;
            });

            bw.ProgressChanged += new ProgressChangedEventHandler(
            delegate (object o, ProgressChangedEventArgs args)
            {
                returndata result = (returndata)args.UserState;

            });

            // what to do when worker completes its task (notify the user)
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
            delegate (object o, RunWorkerCompletedEventArgs args)
            {
                playerdata data = (playerdata)args.Result;
                Name.Text = data.gamename;
                AgentImage.Source = new BitmapImage(new Uri($"Images/{data.characterid}.png", UriKind.Relative));
                RankIcon.Source = new BitmapImage(new Uri($"Images/TX_CompetitiveTier_Large_{data.rank}.png", UriKind.Relative));
                RankIcon.ToolTip = data.rankformatted;
                RRText.Text = data.rankrating + " RR";
                RRBar.Value = data.rankrating;
                StatTitle.Text = "Last " + data.games + " Comp Games";
                KD.Text = "KD: " + data.kd;
                Win.Text = "Win%: " + data.winpercent;
                Score.Text = "Score: " + data.score;
                int x = 0;
                Games.Children.Clear();
                if (data.matches != null)
                {
                    foreach (match mat in data.matches)
                    {
                        System.Windows.Controls.Button newBtn = new System.Windows.Controls.Button();
                        newBtn.ContentStringFormat = "Competitive";
                        if (mat.win)
                        {
                            newBtn.Background = new BrushConverter().ConvertFromString("#00554D") as SolidColorBrush;
                        }
                        else
                        {
                            newBtn.Background = new BrushConverter().ConvertFromString("#52222F") as SolidColorBrush;
                        }
                        newBtn.Uid = mat.plyscore;
                        newBtn.Content = mat.kd;
                        newBtn.Tag = mat.score;
                        newBtn.Name = mat.mapname;
                        newBtn.CommandParameter = mat.kda;
                        newBtn.Template = (ControlTemplate)this.FindResource("MatchData");
                        Grid.SetRow(newBtn, x);
                        Games.Children.Add(newBtn);
                        newBtn.DataContext = mat.matchid;
                        newBtn.Click += NewBtn_Click;
                        x++;
                    }
                }
                if (isUser)
                {
                    playerdata = data;
                    if (NoStats.Visibility == Visibility.Visible)
                    {
                        Stats.Visibility = Visibility.Visible;
                        NoStats.Visibility = Visibility.Collapsed;
                    }
                }
                if (isloading)
                {
                    Loader.Visibility = Visibility.Collapsed;
                    Stats.Visibility = Visibility.Visible;
                }
            });

            bw.RunWorkerAsync();
        }

        private void NewBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button button = (System.Windows.Controls.Button)sender;
            string matchid = (string)button.DataContext;
            Stats.Visibility = Visibility.Collapsed;
            GetMatchData(matchid);
        }

        private void LiveMatch(Auth auth)
        {
            BackgroundWorker bw = new BackgroundWorker();
            runningLM = true;
            // this allows our worker to report progress during work
            bw.WorkerReportsProgress = true;
            // what to do in the background thread
            bw.DoWork += new DoWorkEventHandler(
            delegate (object o, DoWorkEventArgs args)
            {
                BackgroundWorker b = o as BackgroundWorker;
                returndata rt = new returndata();
                rt.allyteam = new List<playerdata>();
                PregameGetPlayer ply = PregameGetPlayer.GetPlayer(auth);
                PregameGetMatch mth = PregameGetMatch.GetMatch(auth, ply.MatchID);
                foreach (PregameGetMatch.Player play in mth.AllyTeam.Players)
                {
                    playerdata dt = new playerdata();
                    try
                    {
                        MMR mmr = MMR.GetMMR(auth, play.Subject);
                        dt.rank = mmr.CompetitiveTier;
                    }
                    catch
                    {
                        dt.rank = play.CompetitiveTier;
                    }
                    dt.characterid = play.CharacterID;
                    dt.playerid = play.Subject;
                    dt.rankformatted = Ranks.GetRankFormatted(dt.rank);
                    if (play.PlayerIdentity.Incognito)
                    {
                        dt.anonymous = true;
                    }
                    CompHistory ch = CompHistory.GetCompHistory(auth, 0, 20, dt.playerid);
                    int wins = 0;
                    int games = 0;
                    string matchid = "";
                    foreach (CompHistory.Match dtaa in ch.Matches)
                    {
                        if (dtaa.RankedRatingEarned > 0)
                        {
                            wins++;
                            games++;
                            if (matchid == "")
                            {
                                matchid = dtaa.MatchID;
                            }
                        }
                        else if (dtaa.RankedRatingEarned < 0)
                        {
                            games++;
                            if (matchid == "")
                            {
                                matchid = dtaa.MatchID;
                            }
                        }
                    }
                    if (matchid == "")
                    {
                        matchid = ch.Matches[0].MatchID;
                    }
                    int kills = 0;
                    int deaths = 0;
                    MatchData dta = MatchData.GetMatchData(auth, ch.Matches[0].MatchID);
                    if (dta.matchInfo.provisioningFlowID != "CustomGame")
                    {
                        foreach (MatchData.Kill kill in dta.kills)
                        {
                            if (kill.killer == play.Subject)
                            {
                                kills++;
                            }
                            if (kill.victim == play.Subject)
                            {
                                deaths++;
                            }
                        }
                    }
                    else
                    {
                        kills = 1;
                        deaths = 1;
                    }
                    float killdeathrat = (float)kills / deaths;
                    dt.kd = (float)Math.Round(killdeathrat, 2);
                    if (games == 0)
                    {
                        dt.winpercent = wins * 100;
                    }
                    else
                    {
                        dt.winpercent = (int)Math.Round((float)(wins * 100) / games);
                    }
                    rt.allyteam.Add(dt);
                    Thread.Sleep(1000);
                }
                List<string> tags = new List<string>();
                foreach (playerdata dataa in rt.allyteam)
                {
                    tags.Add(dataa.playerid);
                }
                List<Username> usrs = Username.GetUsername(auth, tags);
                foreach (Username name in usrs)
                {
                    int playercount = 0;
                    foreach (playerdata playerdata in rt.allyteam)
                    {
                        playercount++;
                        if (name.Subject == playerdata.playerid && !playerdata.anonymous)
                        {
                            playerdata.gamename = name.GameName;
                            playerdata.gametag = name.TagLine;
                            break;
                        }
                        else if (name.Subject == playerdata.playerid && playerdata.anonymous)
                        {
                            playerdata.gamename = "Player " + playercount;
                            playerdata.gametag = "anon";
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
                rt.mapid = mth.MapID;
                rt.matchid = ply.MatchID;
                rt.enemyteam = null;
                if (mth.IsRanked)
                {
                    rt.mode = "Competitive";
                }
                else
                {
                    rt.mode = GetModeName(mth.Mode);
                    if (rt.mode == null || rt.mode == "")
                    {
                        rt.mode = "Custom Game";
                    }
                }
                rt.map = GetMapName(mth.MapID);
                rt.allyscore = 0;
                rt.enemyscore = 0;
                b.ReportProgress(0, rt);
                Thread.Sleep(15000);
                CoreGetMatch match = null;
                bool dodged = false;
                while (match == null && !dodged)
                {
                    CoreGetPlayer player = CoreGetPlayer.GetPlayer(auth);
                    UserPresence.Presence prese = UserPresence.GetUserPresence(auth.subject);
                    if (player.MatchID == null && prese.privinfo.sessionLoopState == "MENUS")
                    {
                        dodged = true;
                        Debug.WriteLine("Dodge detected");
                    }
                    else if (player.MatchID != null)
                    {
                        match = CoreGetMatch.GetMatch(auth, player.MatchID);
                    }
                    Thread.Sleep(3000);
                }
                if (match != null)
                {
                    rt.enemyteam = new List<playerdata>();
                    string[] ids = new string[] { "5F8D3A7F-467B-97F3-062C-13ACF203C006", "f94c3b30-42be-e959-889c-5aa313dba261", "6f2a04ca-43e0-be17-7f36-b3908627744d", "117ed9e3-49f3-6512-3ccf-0cada7e3823b", "ded3520f-4264-bfed-162d-b080e2abccf9", "1e58de9c-4950-5125-93e9-a0aee9f98746", "707eab51-4836-f488-046a-cda6bf494859", "eb93336a-449b-9c1b-0a54-a891f7921d69", "41fb69c1-4189-7b37-f117-bcaf1e96f1bf", "9f0d8ba9-4140-b941-57d3-a7ad57c6b417", "7f94d92c-4234-0a36-9646-3a87eb8b5c89", "569fdd95-4d10-43ab-ca70-79becc718b46", "a3bfb853-43b2-7238-a4f1-ad90e9e46bcc", "8e253930-4c05-31dd-1b6c-968525494517", "add6443a-41bd-e414-f6ad-e58d267f4e95" };
                    string[] agents = new string[] { "Breach", "Raze", "Skye", "Cypher", "Sova", "Killjoy", "Viper", "Phoenix", "Astra", "Brimstone", "Yoru", "Sage", "Reyna", "Omen", "Jett" };
                    if (match.Players != null)
                    {
                        foreach (CoreGetMatch.Player play in match.Players)
                        {
                            foreach (playerdata playdata in rt.allyteam)
                            {
                                if (playdata.playerid == play.PlayerIdentity.Subject && playdata.anonymous)
                                {
                                    int num = Array.IndexOf(ids, play.CharacterID);
                                    playdata.gamename = agents[num];
                                    goto done;
                                }
                                else if (playdata.playerid == play.PlayerIdentity.Subject)
                                {
                                    goto done;
                                }
                            }
                            playerdata dt = new playerdata();
                            try
                            {
                                MMR mmr = MMR.GetMMR(auth, play.Subject);
                                dt.rank = mmr.CompetitiveTier;
                            }
                            catch
                            {
                                dt.rank = 0;
                            }
                            dt.characterid = play.CharacterID;
                            dt.playerid = play.PlayerIdentity.Subject;
                            dt.rankformatted = Ranks.GetRankFormatted(dt.rank);
                            if (play.PlayerIdentity.Incognito)
                            {
                                dt.anonymous = true;
                            }
                            CompHistory ch = CompHistory.GetCompHistory(auth, 0, 20, dt.playerid);
                            int wins = 0;
                            int games = 0;
                            string matchid = "";
                            foreach (CompHistory.Match dtaa in ch.Matches)
                            {
                                if (dtaa.RankedRatingEarned > 0)
                                {
                                    wins++;
                                    games++;
                                    if (matchid == "")
                                    {
                                        matchid = dtaa.MatchID;
                                    }
                                }
                                else if (dtaa.RankedRatingEarned < 0)
                                {
                                    games++;
                                    if (matchid == "")
                                    {
                                        matchid = dtaa.MatchID;
                                    }
                                }
                            }
                            if (matchid == "")
                            {
                                matchid = ch.Matches[0].MatchID;
                            }
                            int kills = 0;
                            int deaths = 0;
                            MatchData dta = MatchData.GetMatchData(auth, matchid);
                            if (dta.kills != null)
                            {
                                foreach (MatchData.Kill kill in dta.kills)
                                {
                                    if (kill.killer == play.Subject)
                                    {
                                        kills++;
                                    }
                                    if (kill.victim == play.Subject)
                                    {
                                        deaths++;
                                    }
                                }
                            }
                            else
                            {
                                kills = 1;
                                deaths = 1;
                            }
                            float killdeathrat = (float)kills / deaths;
                            dt.kd = (float)Math.Round(killdeathrat, 2);
                            if (games == 0)
                            {
                                dt.winpercent = wins * 100;
                            }
                            else
                            {
                                dt.winpercent = (int)Math.Round((float)(wins * 100) / games);
                            }
                            rt.enemyteam.Add(dt);
                            Thread.Sleep(1000);
                        done:;
                        }
                    }
                    List<string> tags1 = new List<string>();
                    if (rt.enemyteam.Count != 0)
                    {
                        foreach (playerdata dataa in rt.enemyteam)
                        {
                            if (dataa.anonymous)
                            {
                                int num = Array.IndexOf(ids, dataa.characterid);
                                dataa.gamename = agents[num];

                            }
                            else
                            {
                                tags1.Add(dataa.playerid);
                            }
                        }
                        List<Username> usrs1 = Username.GetUsername(auth, tags1);
                        foreach (Username name in usrs1)
                        {
                            foreach (playerdata playerdata in rt.enemyteam)
                            {
                                if (name.Subject == playerdata.playerid && !playerdata.anonymous)
                                {
                                    playerdata.gamename = name.GameName;
                                    playerdata.gametag = name.TagLine;
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }
                    }
                    b.ReportProgress(0, rt);
                    Thread.Sleep(2000);
                    bool gameover = false;
                    int totalscore = 0;
                    while (gameover == false)
                    {
                        UserPresence.Presence score = UserPresence.GetUserPresence(auth.subject);
                        if (score.privinfo.sessionLoopState == "MENUS")
                        {
                            gameover = true;
                            Debug.WriteLine("Game ended");
                        }
                        else if (score.privinfo.partyOwnerMatchScoreAllyTeam + score.privinfo.partyOwnerMatchScoreEnemyTeam > totalscore)
                        {
                            rt.allyscore = score.privinfo.partyOwnerMatchScoreAllyTeam;
                            rt.enemyscore = score.privinfo.partyOwnerMatchScoreEnemyTeam;
                            totalscore = score.privinfo.partyOwnerMatchScoreAllyTeam + score.privinfo.partyOwnerMatchScoreEnemyTeam;
                            b.ReportProgress(0, rt);
                        }
                        Thread.Sleep(3000);
                    }
                }
            });
            int updateStatus = 0;
            bw.ProgressChanged += new ProgressChangedEventHandler(
            delegate (object o, ProgressChangedEventArgs args)
            {
                returndata result = (returndata)args.UserState;
                if (NoMatch.Visibility == Visibility.Visible)
                {
                    LiveMatchPage.Visibility = Visibility.Visible;
                    NoMatch.Visibility = Visibility.Collapsed;
                }
                if (updateStatus == 0)
                {
                    RankIcon10.Source = null;
                    Name10.Text = "";
                    Win10.Text = "";
                    KD10.Text = "";
                    RankIcon11.Source = null;
                    Name11.Text = "";
                    Win11.Text = "";
                    KD11.Text = "";
                    RankIcon12.Source = null;
                    Name12.Text = "";
                    Win12.Text = "";
                    KD12.Text = "";
                    RankIcon13.Source = null;
                    Name13.Text = "";
                    Win13.Text = "";
                    KD13.Text = "";
                    RankIcon14.Source = null;
                    Name14.Text = "";
                    Win14.Text = "";
                    KD14.Text = "";
                    if (result.allyteam.Count >= 1)
                    {
                        RankIcon00.Source = new BitmapImage(new Uri($"Images/TX_CompetitiveTier_Large_{result.allyteam[0].rank}.png", UriKind.Relative));
                        Name00.Text = result.allyteam[0].gamename;
                        Win00.Text = result.allyteam[0].winpercent + "%";
                        KD00.Text = result.allyteam[0].kd.ToString();
                    }
                    else
                    {
                        RankIcon00.Source = null;
                        Name00.Text = "";
                        Win00.Text = "";
                        KD00.Text = "";
                    }
                    if (result.allyteam.Count >= 2)
                    {
                        RankIcon01.Source = new BitmapImage(new Uri($"Images/TX_CompetitiveTier_Large_{result.allyteam[1].rank}.png", UriKind.Relative));
                        Name01.Text = result.allyteam[1].gamename;
                        Win01.Text = result.allyteam[1].winpercent + "%";
                        KD01.Text = result.allyteam[1].kd.ToString();
                    }
                    else
                    {
                        RankIcon01.Source = null;
                        Name01.Text = "";
                        Win01.Text = "";
                        KD01.Text = "";
                    }
                    if (result.allyteam.Count >= 3)
                    {
                        RankIcon02.Source = new BitmapImage(new Uri($"Images/TX_CompetitiveTier_Large_{result.allyteam[2].rank}.png", UriKind.Relative));
                        Name02.Text = result.allyteam[2].gamename;
                        Win02.Text = result.allyteam[2].winpercent + "%";
                        KD02.Text = result.allyteam[2].kd.ToString();
                    }
                    else
                    {
                        RankIcon02.Source = null;
                        Name02.Text = "";
                        Win02.Text = "";
                        KD02.Text = "";
                    }
                    if (result.allyteam.Count >= 4)
                    {
                        RankIcon03.Source = new BitmapImage(new Uri($"Images/TX_CompetitiveTier_Large_{result.allyteam[3].rank}.png", UriKind.Relative));
                        Name03.Text = result.allyteam[3].gamename;
                        Win03.Text = result.allyteam[3].winpercent + "%";
                        KD03.Text = result.allyteam[3].kd.ToString();
                    }
                    else
                    {
                        RankIcon03.Source = null;
                        Name03.Text = "";
                        Win03.Text = "";
                        KD03.Text = "";
                    }
                    if (result.allyteam.Count >= 5)
                    {
                        RankIcon04.Source = new BitmapImage(new Uri($"Images/TX_CompetitiveTier_Large_{result.allyteam[4].rank}.png", UriKind.Relative));
                        Name04.Text = result.allyteam[4].gamename;
                        Win04.Text = result.allyteam[4].winpercent + "%";
                        KD04.Text = result.allyteam[4].kd.ToString();
                    }
                    else
                    {
                        RankIcon04.Source = null;
                        Name04.Text = "";
                        Win04.Text = "";
                        KD04.Text = "";
                    }
                    Mode.Text = result.mode;
                    Map.Text = result.map;
                    BackgroundImg.ImageSource = new BitmapImage(new Uri($"pack://application:,,/Images/{result.map}.png"));
                    updateStatus++;
                }
                else if (updateStatus == 1)
                {
                    if (result.enemyteam.Count >= 1)
                    {
                        RankIcon10.Source = new BitmapImage(new Uri($"Images/TX_CompetitiveTier_Large_{result.enemyteam[0].rank}.png", UriKind.Relative));
                        Name10.Text = result.enemyteam[0].gamename;
                        Win10.Text = result.enemyteam[0].winpercent + "%";
                        KD10.Text = result.enemyteam[0].kd.ToString();
                    }
                    else
                    {
                        RankIcon10.Source = null;
                        Name10.Text = "";
                        Win10.Text = "";
                        KD10.Text = "";
                    }
                    if (result.enemyteam.Count >= 2)
                    {
                        RankIcon11.Source = new BitmapImage(new Uri($"Images/TX_CompetitiveTier_Large_{result.enemyteam[1].rank}.png", UriKind.Relative));
                        Name11.Text = result.enemyteam[1].gamename;
                        Win11.Text = result.enemyteam[1].winpercent + "%";
                        KD11.Text = result.enemyteam[1].kd.ToString();
                    }
                    else
                    {
                        RankIcon11.Source = null;
                        Name11.Text = "";
                        Win11.Text = "";
                        KD11.Text = "";
                    }
                    if (result.enemyteam.Count >= 3)
                    {
                        RankIcon12.Source = new BitmapImage(new Uri($"Images/TX_CompetitiveTier_Large_{result.enemyteam[2].rank}.png", UriKind.Relative));
                        Name12.Text = result.enemyteam[2].gamename;
                        Win12.Text = result.enemyteam[2].winpercent + "%";
                        KD12.Text = result.enemyteam[2].kd.ToString();
                    }
                    else
                    {
                        RankIcon12.Source = null;
                        Name12.Text = "";
                        Win12.Text = "";
                        KD12.Text = "";
                    }
                    if (result.enemyteam.Count >= 4)
                    {
                        RankIcon13.Source = new BitmapImage(new Uri($"Images/TX_CompetitiveTier_Large_{result.enemyteam[3].rank}.png", UriKind.Relative));
                        Name13.Text = result.enemyteam[3].gamename;
                        Win13.Text = result.enemyteam[3].winpercent + "%";
                        KD13.Text = result.enemyteam[3].kd.ToString();
                    }
                    else
                    {
                        RankIcon13.Source = null;
                        Name13.Text = "";
                        Win13.Text = "";
                        KD13.Text = "";
                    }
                    if (result.enemyteam.Count >= 5)
                    {
                        RankIcon14.Source = new BitmapImage(new Uri($"Images/TX_CompetitiveTier_Large_{result.enemyteam[4].rank}.png", UriKind.Relative));
                        Name14.Text = result.enemyteam[4].gamename;
                        Win14.Text = result.enemyteam[4].winpercent + "%";
                        KD14.Text = result.enemyteam[4].kd.ToString();
                    }
                    else
                    {
                        RankIcon14.Source = null;
                        Name14.Text = "";
                        Win14.Text = "";
                        KD14.Text = "";
                    }
                    updateStatus++;
                }
                else
                {
                    Score0.Text = result.allyscore.ToString();
                    Score1.Text = result.enemyscore.ToString();
                }
            done:;
            });

            // what to do when worker completes its task (notify the user)
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
            delegate (object o, RunWorkerCompletedEventArgs args)
            {
                runningLM = false;
                if (LiveMatchPage.Visibility == Visibility.Visible)
                {
                    LiveMatchPage.Visibility = Visibility.Collapsed;
                    NoMatch.Visibility = Visibility.Visible;
                }
                PlayerStats(AppData.auth.subject, true);
                mainbw.RunWorkerAsync();
            });
            bw.RunWorkerAsync();
        }
        public string GetServer()
        {
            string[] servers = { "https://statsvalapi.azurewebsites.net", "https://statsvalapi1.azurewebsites.net", "https://statsvalapi2.azurewebsites.net" };
            Random rand = new Random();
            // Generate a random index less than the size of the array.  
            int index = rand.Next(servers.Length);
            return servers[index];
        }
        public void GetMatchData(string matchid)
        {
            BackgroundWorker bw = new BackgroundWorker();

            // this allows our worker to report progress during work
            bw.WorkerReportsProgress = true;
            Loader.Visibility = Visibility.Visible;
            // what to do in the background thread
            bw.DoWork += new DoWorkEventHandler(
            delegate (object o, DoWorkEventArgs args)
            {
                RestClient client = new RestClient(GetServer() + "/api/Matches/" + matchid);

                RestRequest request = new RestRequest(Method.GET);
                var responce = client.Execute(request);
                Match match = JsonConvert.DeserializeObject<Match>(responce.Content);
                args.Result = match;
            });

            // what to do when progress changed (update the progress bar for example)
            bw.ProgressChanged += new ProgressChangedEventHandler(
            delegate (object o, ProgressChangedEventArgs args)
            {

            });

            // what to do when worker completes its task (notify the user)
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
            delegate (object o, RunWorkerCompletedEventArgs args)
            {
                Match match = (Match)args.Result;
                MatchMode.Text = match.mode;
                MatchMap.Text = match.map;
                MatchScore0.Text = match.teams[0].score.ToString();
                MatchScore1.Text = match.teams[1].score.ToString();
                MatchBackgroundImg.ImageSource = new BitmapImage(new Uri($"pack://application:,,/Images/{match.map}.png"));

                MatchRankIcon00.Source = new BitmapImage(new Uri($"Images/TX_CompetitiveTier_Large_{match.teams[0].players[0].rank}.png", UriKind.Relative));
                MatchName00.Text = match.teams[0].players[0].name;
                MatchKDA00.Text = match.teams[0].players[0].kills + "/" + match.teams[0].players[0].deaths + "/" + match.teams[0].players[0].assists;
                MatchScore00.Text = match.teams[0].players[0].score.ToString();

                MatchRankIcon01.Source = new BitmapImage(new Uri($"Images/TX_CompetitiveTier_Large_{match.teams[0].players[1].rank}.png", UriKind.Relative));
                MatchName01.Text = match.teams[0].players[1].name;
                MatchKDA01.Text = match.teams[0].players[1].kills + "/" + match.teams[0].players[1].deaths + "/" + match.teams[0].players[1].assists;
                MatchScore01.Text = match.teams[0].players[1].score.ToString();

                MatchRankIcon02.Source = new BitmapImage(new Uri($"Images/TX_CompetitiveTier_Large_{match.teams[0].players[2].rank}.png", UriKind.Relative));
                MatchName02.Text = match.teams[0].players[2].name;
                MatchKDA02.Text = match.teams[0].players[2].kills + "/" + match.teams[0].players[2].deaths + "/" + match.teams[0].players[2].assists;
                MatchScore02.Text = match.teams[0].players[2].score.ToString();

                MatchRankIcon03.Source = new BitmapImage(new Uri($"Images/TX_CompetitiveTier_Large_{match.teams[0].players[3].rank}.png", UriKind.Relative));
                MatchName03.Text = match.teams[0].players[3].name;
                MatchKDA03.Text = match.teams[0].players[3].kills + "/" + match.teams[0].players[3].deaths + "/" + match.teams[0].players[3].assists;
                MatchScore03.Text = match.teams[0].players[3].score.ToString();

                MatchRankIcon04.Source = new BitmapImage(new Uri($"Images/TX_CompetitiveTier_Large_{match.teams[0].players[4].rank}.png", UriKind.Relative));
                MatchName04.Text = match.teams[0].players[4].name;
                MatchKDA04.Text = match.teams[0].players[4].kills + "/" + match.teams[0].players[4].deaths + "/" + match.teams[0].players[4].assists;
                MatchScore04.Text = match.teams[0].players[4].score.ToString();

                MatchRankIcon10.Source = new BitmapImage(new Uri($"Images/TX_CompetitiveTier_Large_{match.teams[1].players[0].rank}.png", UriKind.Relative));
                MatchName10.Text = match.teams[1].players[0].name;
                MatchKDA10.Text = match.teams[1].players[0].kills + "/" + match.teams[1].players[0].deaths + "/" + match.teams[1].players[0].assists;
                MatchScore10.Text = match.teams[1].players[0].score.ToString();

                MatchRankIcon11.Source = new BitmapImage(new Uri($"Images/TX_CompetitiveTier_Large_{match.teams[1].players[1].rank}.png", UriKind.Relative));
                MatchName11.Text = match.teams[1].players[1].name;
                MatchKDA11.Text = match.teams[1].players[1].kills + "/" + match.teams[1].players[1].deaths + "/" + match.teams[1].players[1].assists;
                MatchScore11.Text = match.teams[1].players[1].score.ToString();

                MatchRankIcon12.Source = new BitmapImage(new Uri($"Images/TX_CompetitiveTier_Large_{match.teams[1].players[2].rank}.png", UriKind.Relative));
                MatchName12.Text = match.teams[1].players[2].name;
                MatchKDA12.Text = match.teams[1].players[2].kills + "/" + match.teams[1].players[2].deaths + "/" + match.teams[1].players[2].assists;
                MatchScore12.Text = match.teams[1].players[2].score.ToString();

                MatchRankIcon13.Source = new BitmapImage(new Uri($"Images/TX_CompetitiveTier_Large_{match.teams[1].players[3].rank}.png", UriKind.Relative));
                MatchName13.Text = match.teams[1].players[3].name;
                MatchKDA13.Text = match.teams[1].players[3].kills + "/" + match.teams[1].players[3].deaths + "/" + match.teams[1].players[3].assists;
                MatchScore13.Text = match.teams[1].players[3].score.ToString();

                MatchRankIcon14.Source = new BitmapImage(new Uri($"Images/TX_CompetitiveTier_Large_{match.teams[1].players[4].rank}.png", UriKind.Relative));
                MatchName14.Text = match.teams[1].players[4].name;
                MatchKDA14.Text = match.teams[1].players[4].kills + "/" + match.teams[1].players[4].deaths + "/" + match.teams[1].players[4].assists;
                MatchScore14.Text = match.teams[1].players[4].score.ToString();

                Loader.Visibility = Visibility.Collapsed;
                MatchDataPage.Visibility = Visibility.Visible;
            });

            bw.RunWorkerAsync();


        }

        public string GetMapName(string mapid)
        {
            if (mapid == "/Game/Maps/Ascent/Ascent")
            {
                return "Ascent";
            }
            else if (mapid == "/Game/Maps/Bonsai/Bonsai")
            {
                return "Split";
            }
            else if (mapid == "/Game/Maps/Duality/Duality")
            {
                return "Bind";
            }
            else if (mapid == "/Game/Maps/Port/Port")
            {
                return "Icebox";
            }
            else if (mapid == "/Game/Maps/Triad/Triad")
            {
                return "Haven";
            }
            else if (mapid == "/Game/Maps/Breeze/Breeze")
            {
                return "Breeze";
            }
            else if (mapid == "/Game/Maps/Foxtrot/Foxtrot")
            {
                return "Breeze";
            }
            return null;
        }
        public static string GetModeName(string mode)
        {
            if (mode == "/Game/GameModes/Bomb/BombGameMode.BombGameMode_C")
            {
                return "Standard";
            }
            else if (mode == "/Game/GameModes/Deathmatch/DeathmatchGameMode.DeathmatchGameMode_C")
            {
                return "Deathmatch";
            }
            else if (mode == "/Game/GameModes/GunGame/GunGameTeamsGameMode.GunGameTeamsGameMode_C")
            {
                return "Escalation";
            }
            else if (mode == "/Game/GameModes/OneForAll/OneForAll_GameMode.OneForAll_GameMode_C")
            {
                return "Replication";
            }
            else if (mode == "/Game/GameModes/QuickBomb/QuickBombGameMode.QuickBombGameMode_C")
            {
                return "Spike Rush";
            }
            return null;
        }
        private void TextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            HomeTitle.Foreground = Brushes.White;
            StatsTitle.Foreground = Brushes.LightGray;
            LMTitle.Foreground = Brushes.LightGray;
            Home.Visibility = Visibility.Visible;
            Stats.Visibility = Visibility.Collapsed;
            LiveMatchPage.Visibility = Visibility.Collapsed;
            NoMatch.Visibility = Visibility.Collapsed;
            NoStats.Visibility = Visibility.Collapsed;
            MatchDataPage.Visibility = Visibility.Collapsed;
            SettingsPage.Visibility = Visibility.Collapsed;
            SettingsTitle.Foreground = Brushes.LightGray;
        }

        private void StatsTitle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (playerdata != null)
            {
                HomeTitle.Foreground = Brushes.LightGray;
                StatsTitle.Foreground = Brushes.White;
                LMTitle.Foreground = Brushes.LightGray;
                Home.Visibility = Visibility.Collapsed;
                Stats.Visibility = Visibility.Visible;
                LiveMatchPage.Visibility = Visibility.Collapsed;
                NoMatch.Visibility = Visibility.Collapsed;
                NoStats.Visibility = Visibility.Collapsed;
                MatchDataPage.Visibility = Visibility.Collapsed;
                SettingsPage.Visibility = Visibility.Collapsed;
                SettingsTitle.Foreground = Brushes.LightGray;
                playerdata data = playerdata;
                Name.Text = data.gamename;
                AgentImage.Source = new BitmapImage(new Uri($"Images/{data.characterid}.png", UriKind.Relative));
                RankIcon.Source = new BitmapImage(new Uri($"Images/TX_CompetitiveTier_Large_{data.rank}.png", UriKind.Relative));
                RRText.Text = data.rankrating + " RR";
                RRBar.Value = data.rankrating;
                StatTitle.Text = "Last " + data.games + " Comp Games";
                KD.Text = "KD: " + data.kd;
                Win.Text = "Win%: " + data.winpercent;
                Score.Text = "Score: " + data.score;
                int x = 0;
                foreach (match mat in data.matches)
                {
                    System.Windows.Controls.Button newBtn = new System.Windows.Controls.Button();
                    newBtn.ContentStringFormat = "Competitive";
                    if (mat.win)
                    {
                        newBtn.Background = new BrushConverter().ConvertFromString("#00554D") as SolidColorBrush;
                    }
                    else
                    {
                        newBtn.Background = new BrushConverter().ConvertFromString("#52222F") as SolidColorBrush;
                    }
                    newBtn.Uid = mat.plyscore;
                    newBtn.Content = mat.kd;
                    newBtn.Tag = mat.score;
                    newBtn.Name = mat.mapname;
                    newBtn.CommandParameter = mat.kda;
                    newBtn.Template = (ControlTemplate)this.FindResource("MatchData");
                    Grid.SetRow(newBtn, x);
                    newBtn.DataContext = mat.matchid;
                    newBtn.Click += NewBtn_Click;
                    Games.Children.Add(newBtn);
                    x++;
                }
            }
            else
            {
                HomeTitle.Foreground = Brushes.LightGray;
                StatsTitle.Foreground = Brushes.White;
                LMTitle.Foreground = Brushes.LightGray;
                Home.Visibility = Visibility.Collapsed;
                Stats.Visibility = Visibility.Collapsed;
                LiveMatchPage.Visibility = Visibility.Collapsed;
                NoMatch.Visibility = Visibility.Collapsed;
                NoStats.Visibility = Visibility.Visible;
                MatchDataPage.Visibility = Visibility.Collapsed;
                SettingsPage.Visibility = Visibility.Collapsed;
                SettingsTitle.Foreground = Brushes.LightGray;
            }

        }

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void Image_MouseLeftButtonUp_1(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void TextBlock_MouseLeftButtonUp_1(object sender, MouseButtonEventArgs e)
        {
            if (runningLM)
            {
                HomeTitle.Foreground = Brushes.LightGray;
                StatsTitle.Foreground = Brushes.LightGray;
                LMTitle.Foreground = Brushes.White;
                Home.Visibility = Visibility.Collapsed;
                Stats.Visibility = Visibility.Collapsed;
                LiveMatchPage.Visibility = Visibility.Visible;
                NoMatch.Visibility = Visibility.Collapsed;
                NoStats.Visibility = Visibility.Collapsed;
                MatchDataPage.Visibility = Visibility.Collapsed;
                SettingsPage.Visibility = Visibility.Collapsed;
                SettingsTitle.Foreground = Brushes.LightGray;
            }
            else
            {
                HomeTitle.Foreground = Brushes.LightGray;
                StatsTitle.Foreground = Brushes.LightGray;
                LMTitle.Foreground = Brushes.White;
                Home.Visibility = Visibility.Collapsed;
                Stats.Visibility = Visibility.Collapsed;
                LiveMatchPage.Visibility = Visibility.Collapsed;
                NoMatch.Visibility = Visibility.Visible;
                NoStats.Visibility = Visibility.Collapsed;
                MatchDataPage.Visibility = Visibility.Collapsed;
                SettingsPage.Visibility = Visibility.Collapsed;
                SettingsTitle.Foreground = Brushes.LightGray;
            }
        }

        private void Button_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("Dragging");
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void ResSave(object sender, System.Windows.DragEventArgs e)
        {

        }

        private void TextBlock_MouseLeftButtonUp_2(object sender, MouseButtonEventArgs e)
        {
            LoadSettings();
            HomeTitle.Foreground = Brushes.LightGray;
            StatsTitle.Foreground = Brushes.LightGray;
            LMTitle.Foreground = Brushes.LightGray;
            Home.Visibility = Visibility.Collapsed;
            Stats.Visibility = Visibility.Collapsed;
            LiveMatchPage.Visibility = Visibility.Collapsed;
            NoMatch.Visibility = Visibility.Collapsed;
            NoStats.Visibility = Visibility.Collapsed;
            MatchDataPage.Visibility = Visibility.Collapsed;
            SettingsPage.Visibility = Visibility.Visible;
            SettingsTitle.Foreground = Brushes.White;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AutoUpdater.InstalledVersion = new Version(System.Windows.Forms.Application.ProductVersion);
            AutoUpdater.Start("https://www.statsval.com/update.xml");
            UpdateButton.Content = "No Updates";
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (settings.MinimizeToTray == true)
            {
                this.Hide();
                e.Cancel = true;
            }
            else
            {
                icon.Dispose();
                Environment.Exit(0);
            }
        }
    }
}
