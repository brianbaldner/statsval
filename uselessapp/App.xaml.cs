using System.Collections.Generic;
using System.Windows;

namespace uselessapp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
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
        public class returndata
        {
            public string mapid;
            public string matchid;
            public string playerid;
            public List<playerdata> allyteam;
            public List<playerdata> enemyteam;
            public int localPlayerteam;
            public int allyscore;
            public int enemyscore;
            public string mode;
            public string map;
        }
        public class match
        {
            public string mapname;
            public string mode;
            public string score;
            public string kda;
            public string kd;
            public string plyscore;
            public bool win;
            public string matchid;
        }
        public class playerdata
        {
            public string playerid;
            public string gamename;
            public string gametag;
            public string characterid;
            public int headshot;
            public float kd;
            public int winpercent;
            public int rank;
            public string rankformatted;
            public int rankrating = 0;
            public int games = 0;
            public int score = 0;
            public bool anonymous = false;
            public List<match> matches = null;
        }
    }
}
