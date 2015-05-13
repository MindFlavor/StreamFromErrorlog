using System;

namespace MindFlavor.SQLServer.Errorlog.Entries
{
    public class GenericEntry
    {
        public ServerInfo ServerInfo { get; set; }
        public DateTime EventTime { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }

        #region Const
        public const string AUTH_SUCCESS = "Login succeeded for user ";
        public const string AUTH_FAILED = "Login failed for user ";

        public const string AUTH_CLIENT_START = "[CLIENT: ";
        public const string AUTH_CLIENT_END = "]";
        #endregion

        public static GenericEntry Factory(GenericEntry sele)
        {
            if (sele.Description.StartsWith(AUTH_SUCCESS) || sele.Description.StartsWith(AUTH_FAILED))
            {
                var le = new LoginEntry()
                {
                    ServerInfo = sele.ServerInfo,
                    Type = sele.Type,
                    EventTime = sele.EventTime,
                    Description = sele.Description,
                    Failed = sele.Description.StartsWith(AUTH_FAILED)
                };

                int idxStart = sele.Description.IndexOf('\'');
                int idxEnd = sele.Description.IndexOf('\'', idxStart + 1);

                le.Login = sele.Description.Substring(idxStart + 1, idxEnd - idxStart - 1);

                idxStart = sele.Description.IndexOf(AUTH_CLIENT_START);
                idxEnd = sele.Description.IndexOf(AUTH_CLIENT_END, idxStart + 1);

                le.Client = sele.Description.Substring(idxStart + AUTH_CLIENT_START.Length, idxEnd - (idxStart + AUTH_CLIENT_START.Length) - (AUTH_CLIENT_END.Length - 1));

                return le;
            }

            return sele;
        }
    }
}
