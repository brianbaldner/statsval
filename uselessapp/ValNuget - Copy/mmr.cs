using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace ValAPINet
{
    public class MMR
    {
        public int CompetitiveTier { get; set; }
        public int RankedRating { get; set; }
        public string responce { get; set; }
        public int StatusCode { get; set; }
        public static MMR GetMMR(Auth au, string playerid = "useauth")
        {
            MMR ret = new MMR();
            if (playerid == "useauth")
            {
                playerid = au.subject;
            }
            RestClient client = new RestClient("https://pd." + au.region + ".a.pvp.net/mmr/v1/players/" + playerid);
            client.CookieContainer = au.cookies;

            RestRequest request = new RestRequest(Method.GET);
            request.AddHeader("Authorization", $"Bearer {au.AccessToken}");
            request.AddHeader("X-Riot-Entitlements-JWT", $"{au.EntitlementToken}");
            request.AddHeader("X-Riot-ClientPlatform", $"ew0KCSJwbGF0Zm9ybVR5cGUiOiAiUEMiLA0KCSJwbGF0Zm9ybU9TIjogIldpbmRvd3MiLA0KCSJwbGF0Zm9ybU9TVmVyc2lvbiI6ICIxMC4wLjE5MDQyLjEuMjU2LjY0Yml0IiwNCgkicGxhdGZvcm1DaGlwc2V0IjogIlVua25vd24iDQp9");
            request.AddHeader("X-Riot-ClientVersion", $"{au.version}");
            //request.AddJsonBody("{}");
            var responce = client.Execute(request);
            string responcecontent = responce.Content;
            JObject obj = JObject.FromObject(JsonConvert.DeserializeObject(responcecontent));
            string season = "52e9749a-429b-7060-99fe-4595426a0cf7";
            ret.CompetitiveTier = obj["QueueSkills"]["competitive"]["SeasonalInfoBySeasonID"][season].Value<int>("CompetitiveTier");
            ret.RankedRating = obj["QueueSkills"]["competitive"]["SeasonalInfoBySeasonID"][season].Value<int>("RankedRating");
            ret.StatusCode = (int)responce.StatusCode;
            ret.responce = responcecontent;
            return ret;
        }
    }
}
