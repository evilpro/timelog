using Newtonsoft.Json;
using System;
using System.Net;

namespace timelog.Models
{
    public class TimeData
    {
        //TODO: Do not hardcode the time source url
        [JsonIgnore]
        public static readonly string TimeSource = "http://worldtimeapi.org/api/timezone/Europe/Vienna.json";
        public int week_number { get; set; }
        public string utc_offset { get; set; }
        public DateTime utc_datetime { get; set; }
        public int unixtime { get; set; }
        public string timezone { get; set; }
        public int raw_offset { get; set; }
        public DateTime dst_until { get; set; }
        public int dst_offset { get; set; }
        public DateTime dst_from { get; set; }
        public bool dst { get; set; }
        public int day_of_year { get; set; }
        public int day_of_week { get; set; }
        public DateTime datetime { get; set; }
        public string client_ip { get; set; }
        public string abbreviation { get; set; }

        //Boolean reflects the success of the time query
        public static bool FetchTimeData(out TimeData timeData)
        {
            using (WebClient webClient = new WebClient())
            {
                try
                {
                    string timeJson = webClient.DownloadString(TimeData.TimeSource);
                    timeData = JsonConvert.DeserializeObject<TimeData>(timeJson);
                    return true;
                }
                catch
                {
                    timeData = null;
                    return false;
                }
            }
        }
    }

    
}
