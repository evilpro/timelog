using System.IO;
using timelog.Exceptions;

namespace timelog.Models
{
    public class WorkSession
    {
        public int StartTime { get; }
        public int EndTime { get; private set; }
        public string Subject { get; }
        public string Summary { get; private set; }
        public int Duration
        {
            get
            {
                if (StartTime == 0 || EndTime == 0)
                    return 0;
                else
                    return EndTime - StartTime;
            }
        }

        public WorkSession(string subject)
        {
            //Parse time from the web
            if (TimeData.FetchTimeData(out TimeData data))
                StartTime = data.unixtime;
            else
                throw new RemoteTimeUnavailableException("Remote time data is not available");
            //Set subject
            Subject = subject;
        }

        private WorkSession(int start, string subject)
        {
            StartTime = start;
            Subject = subject;
        }

        public bool FinishSession(string summary)
        {
            //Update finalization time
            if (TimeData.FetchTimeData(out TimeData timeData))
                EndTime = timeData.unixtime;
            else
                return false;
            //Set summary
            Summary = summary;

            return true;
        }

        public void SaveSession(string path)
        {
            using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(path)))
            {
                writer.Write(StartTime);
                writer.Write(Subject);
                writer.Close();
            }
        }

        public static bool TryParse(string path, out WorkSession session)
        {
            using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
            {
                try
                {
                    //Read information from session
                    int start = reader.ReadInt32();
                    string subject = reader.ReadString();
                    reader.Close();

                    session = new WorkSession(start, subject);
                    return true;
                }
                catch
                {
                    session = null;
                    return false;
                }
            }
        }
    }
}
