using System;
using System.Text;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using MindFlavor.SQLServer.Errorlog;
using Newtonsoft.Json;

namespace StreamFromErrorlog
{
    class Program
    {
        static EventHubClient _eventHub = null;
        static int icnt = 0;


        static void Main(string[] args)
        {
            Console.WriteLine("Starting");

            string eventHubName = null;
            string connectionString = null;
            {
                connectionString = System.Configuration.ConfigurationManager.AppSettings["Microsoft.ServiceBus.ConnectionString"];
                if (string.IsNullOrEmpty(connectionString))
                    throw new ArgumentException("Did not find Service Bus connections string in appsettings (app.config)");
                ServiceBusConnectionStringBuilder builder = new ServiceBusConnectionStringBuilder(connectionString);
                builder.TransportType = TransportType.Amqp;
                connectionString = builder.ToString();

                eventHubName = System.Configuration.ConfigurationManager.AppSettings["EventHubName"];
                if (string.IsNullOrEmpty(eventHubName))
                    throw new ArgumentException("Did not find event hub name in appsettings (app.config)");
            }

            _eventHub = EventHubClient.CreateFromConnectionString(connectionString, eventHubName);

            int i = 0;
            while (!string.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["Server_Name_" + i]))
            {
                ServerInfo si = new ServerInfo()
                {
                    Name = System.Configuration.ConfigurationManager.AppSettings["Server_Name_" + i],
                    LogPath = System.Configuration.ConfigurationManager.AppSettings["Server_Path_" + i]
                };

                i++;

                var els = new ErrorLogScanner(si, 1000);
                els.EntryParsed += Els_EntryParsed;
                els.Start();
            }

            Console.ReadLine();
        }

        private static void Els_EntryParsed(ErrorLogScanner scanner, MindFlavor.SQLServer.Errorlog.Entries.GenericEntry entry)
        {
            if (entry is MindFlavor.SQLServer.Errorlog.Entries.LoginEntry)
            {
                MindFlavor.SQLServer.Errorlog.Entries.LoginEntry le = entry as MindFlavor.SQLServer.Errorlog.Entries.LoginEntry;
                Console.WriteLine(icnt++ + "-" + le.Login + " from " + le.Client + "(" + le.Failed + ") " + le.Description);

                string ser = JsonConvert.SerializeObject(le);
                EventData ed = new EventData(Encoding.UTF8.GetBytes(ser))
                {
                    PartitionKey = le.Client
                };

                _eventHub.Send(ed);
            }
        }
    }
}