namespace MindFlavor.SQLServer.Errorlog.Entries
{
    public class LoginEntry : GenericEntry
    {
        public bool Failed { get; set; }

        public string Login { get; set; }
        public string Client { get; set; }
    }
}
