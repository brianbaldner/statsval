using System.Collections.Generic;
using System.Text;

namespace uselessapp
{
    public class Match
    {
        public string map;
        public string mode;
        public List<Team> teams;
    }
    public class Team
    {
        public int score;
        public string teamid;
        public List<Player> players;
    }
    public class Player
    {
        public int rank;
        public string name;
        public int kills;
        public int deaths;
        public int assists;
        public int score;
    }

    public class PuuidData
    {
        public string puuid { get; set; }
    }

    public class Puuid
    {
        public string status { get; set; }
        public PuuidData data { get; set; }
    }
    public class UserData
    {
        public string subject;
        public string gamename;
        public string gametag;
    }
    public class Settings
    {
        public bool TrackLiveGames = true;
        public bool MinimizeToTray = true;
    }
    public class performance
    {
        public static string Faststring(string one, string two)
        {
            StringBuilder ret = new StringBuilder(one);
            ret.Append(two);
            return ret.ToString();
        }
    }
}
