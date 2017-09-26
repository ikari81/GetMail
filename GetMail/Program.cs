using OpenPop.Mime;
using OpenPop.Pop3;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace GetMail
{

    class Program
    {
        public struct CtlMsg
        {
            public string from;
            public string ctlmsg;
        }
        static Queue<CtlMsg> QueueCommand = new Queue<CtlMsg>();
        static public string LocalIp = "";

        public static void FetchAllMessages(string hostname, int port, bool useSsl, string username, string password)
        {
            // The client disconnects from the server when being disposed
            using (Pop3Client client = new Pop3Client())
            {
                // Connect to the server
                client.Connect(hostname, port, useSsl);
                // Authenticate ourselves towards the server
                client.Authenticate(username, password, AuthenticationMethod.Auto);
                // Get the number of messages in the inbox
                int messageCount = client.GetMessageCount();
                // We want to download all messages
                List<Message> allMessages = new List<Message>(messageCount);
                // Messages are numbered in the interval: [1, messageCount]
                // Ergo: message numbers are 1-based.
                // Most servers give the latest message the highest number
                for (int i = 1; i < messageCount + 1; i++)
                {
                    allMessages.Add(client.GetMessage(i));
                    CtlMsg tmp = new CtlMsg();
                    {
                        {
                            tmp.from = client.GetMessageHeaders(i).From.Address;
                            tmp.ctlmsg = (client.GetMessage(i).FindFirstPlainTextVersion() == null) ? "null" : client.GetMessage(i).FindFirstPlainTextVersion().GetBodyAsText();
                            QueueCommand.Enqueue(tmp);
                        }
                        foreach (IPAddress ipaddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                            if (tmp.ctlmsg.Contains(ipaddress.ToString()))
                                client.DeleteMessage(i);
                    }
                     //catch { }
                    }
                client.Disconnect();
                    // Now return the fetched messages
                    return;
                }
        }
        static void Main(string[] args)
        {
            
            foreach (IPAddress ipaddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                LocalIp += ipaddress.ToString();

            new Thread(()=> 
            {
                while (true)
                {
                    FetchAllMessages("172.30.1.47", 110, false, "telemex", "teletele");
                    Thread.Sleep(2000);
                }
            }
            ).Start();

            Worker work = new Worker();
            Thread Main = new Thread(() => { work.QueueThread(ref QueueCommand); });
            Main.Start();
        }
    }
}
