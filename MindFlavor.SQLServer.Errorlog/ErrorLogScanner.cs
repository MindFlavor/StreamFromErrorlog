using System;
using System.Collections.Generic;
using System.Text;
using MindFlavor.SQLServer.Errorlog.Entries;

namespace MindFlavor.SQLServer.Errorlog
{
    public delegate void EntryParsedDelegate(ErrorLogScanner scanner, GenericEntry entry);
    public class ErrorLogScanner
    {
        public ServerInfo ServerInfo { get; set; }
        public int SleepMilliseconds { get; set; }

        protected System.Threading.Thread _thread = null;

        public event EntryParsedDelegate EntryParsed;
  

        public ErrorLogScanner(ServerInfo si, int SleepMilliseconds)
        {
            this.ServerInfo = si;
            this.SleepMilliseconds = SleepMilliseconds;
        }

        public void Start()
        {
            System.Threading.ThreadStart pts = new System.Threading.ThreadStart(ProcessLogFolder);

            _thread = new System.Threading.Thread(pts);
            _thread.Priority = System.Threading.ThreadPriority.BelowNormal;
            _thread.IsBackground = true;
            _thread.Start();
        }

        private void ProcessLogFolder()
        {
            long lFilePosition = 0;

            byte[] bBuffer = new byte[1024 * 1024 * 8]; // 8 MB buffer
            int iBufferPosition = 0;

            Console.WriteLine(ServerInfo.LogPath);

            System.IO.FileInfo fi = new System.IO.FileInfo(ServerInfo.LogPath + System.IO.Path.DirectorySeparatorChar + "ERRORLOG");
            lFilePosition = fi.Length; // from now on

            while (true)
            {
                fi = new System.IO.FileInfo(ServerInfo.LogPath + System.IO.Path.DirectorySeparatorChar + "ERRORLOG");
                // file changed, reinitialize
                if (fi.Length < lFilePosition)
                    lFilePosition = 0;

                List<string> lLines = new List<string>();

                while (lFilePosition < fi.Length)
                {
                    using (System.IO.FileStream fs = new System.IO.FileStream(fi.FullName, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))
                    {
                        if (lFilePosition > 0)
                            fs.Seek(lFilePosition, System.IO.SeekOrigin.Begin);

                        int iRead = fs.Read(bBuffer, iBufferPosition, bBuffer.Length - iBufferPosition);

                        iBufferPosition += iRead;
                        lFilePosition += iRead;

                        lLines.AddRange(ProcessBuffer(ref bBuffer, ref iBufferPosition));
                        //Console.WriteLine("added {0:N0} rows", lLines.Count);
                    }

                    var entries = EntriesFromLines(ServerInfo, ref lLines);

                    foreach(var entry in entries)
                    {
                        OnEntryParsed(GenericEntry.Factory(entry));
                    }
                }

                System.Threading.Thread.Sleep(SleepMilliseconds);
            }
        }

        private static List<GenericEntry> EntriesFromLines(ServerInfo si, ref List<string> lLines)
        {
            List<GenericEntry> lEntries = new List<GenericEntry>();
            int iLastConsumed = 0;
            GenericEntry sInProgess = null;

            for (int i = 0; i < lLines.Count; i++)
            {
                if (lLines[i].Length >= 23)
                {
                    string strD = lLines[i].Substring(0, 23);
                    DateTime? dt = null;

                    if ((dt = ParseDateTime(strD)).HasValue)
                    {
                        string strType = lLines[i].Substring(22, 13).Trim();
                        string strDesc = lLines[i].Substring(35);
                        // good one
                        if (sInProgess != null)
                        {
                            lEntries.Add(sInProgess);
                            iLastConsumed = i;
                        }

                        sInProgess = new GenericEntry()
                        {
                            ServerInfo = si,
                            EventTime = dt.Value,
                            Type = strType,
                            Description = strDesc
                        };
                    }
                    else // continuation
                    {
                        if (sInProgess != null)
                            sInProgess.Description += "\n\r" + lLines[i];

                    }
                }
            }

            // remove processed entries
            lLines.RemoveRange(0, iLastConsumed);

            return lEntries;
        }

        private static List<string> ProcessBuffer(ref byte[] bBuffer, ref int iLength)
        {
            List<string> lSplittedByCRLF = new List<string>();
            int iPos = 0;
            int iLastProcessedByte = 0;

            for (int i = iPos; i < (iLength - 3); i++)
            {
                if (bBuffer[i] == 13 && bBuffer[i + 1] == 0 && bBuffer[i + 2] == 10 && bBuffer[i + 3] == 0)
                {
                    //// old style
                    //byte[] btemp = new byte[(i) - iPos];
                    //Array.Copy(bBuffer, iPos, btemp, 0, (i) - iPos);

                    //string s = Encoding.Unicode.GetString(btemp);
                    ////

                    //// test
                    //byte[] bb = Encoding.Unicode.GetBytes(s);
                    ////

                    string s = Encoding.Unicode.GetString(bBuffer, iPos, i - iPos);

                    lSplittedByCRLF.Add(s);
                    iPos = i + 4;

                    if (bBuffer[iPos] == 13 && bBuffer[iPos + 1] == 0 && bBuffer[iPos + 2] == 10 && bBuffer[iPos + 3] == 0)
                    {
                        iPos += 4;
                    }
                    i = iPos;

                    iLastProcessedByte = iPos;
                }
            }

            // now remove processed bytes
            if (iLastProcessedByte <= iLength)
            {
                int iToCopy = iLength - iLastProcessedByte;

                Array.Copy(bBuffer, iLastProcessedByte, bBuffer, 0, iLength - iLastProcessedByte);
                iLength = iLength - iLastProcessedByte;
            }


            return lSplittedByCRLF;
        }

        public static DateTime? ParseDateTime(string s)
        {
            s = s.Trim();
            string sYear = s.Substring(0, 4);
            int year;
            if (!int.TryParse(sYear, out year))
                return null;

            string sMonth = s.Substring(5, 2);
            int month;
            if (!int.TryParse(sMonth, out month))
                return null;

            string sDay = s.Substring(8, 2);
            int day;
            if (!int.TryParse(sDay, out day))
                return null;

            string sHour = s.Substring(11, 2);
            int hour;
            if (!int.TryParse(sHour, out hour))
                return null;

            string sMinute = s.Substring(14, 2);
            int minute;
            if (!int.TryParse(sMinute, out minute))
                return null;

            string sSecond = s.Substring(17, 2);
            int second;
            if (!int.TryParse(sSecond, out second))
                return null;

            int iMSLen = s.Length - 20;
            string sMS = s.Substring(20, iMSLen);
            int ms;
            if (!int.TryParse(sMS, out ms))
                return null;

            return new DateTime(year, month, day, hour, minute, second, ms, DateTimeKind.Local);
        }

        #region Event handlers
        protected void OnEntryParsed(GenericEntry ge)
        {
            if (EntryParsed != null)
                EntryParsed(this, ge);
        }
        #endregion
    }
}
