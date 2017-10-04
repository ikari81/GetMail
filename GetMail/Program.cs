using OpenPop.Mime;
using OpenPop.Pop3;
using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;

namespace GetMail
{

    class Program
    {
        class reboot
        {
            //импортируем API функцию InitiateSystemShutdown
            [DllImport("advapi32.dll", EntryPoint = "InitiateSystemShutdownEx")]
            static extern int InitiateSystemShutdown(string lpMachineName, string lpMessage, int dwTimeout, bool bForceAppsClosed, bool bRebootAfterShutdown);
            //импортируем API функцию AdjustTokenPrivileges
            [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
            internal static extern bool AdjustTokenPrivileges(IntPtr htok, bool disall,
            ref TokPriv1Luid newst, int len, IntPtr prev, IntPtr relen);
            //импортируем API функцию GetCurrentProcess
            [DllImport("kernel32.dll", ExactSpelling = true)]
            internal static extern IntPtr GetCurrentProcess();
            //импортируем API функцию OpenProcessToken
            [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
            internal static extern bool OpenProcessToken(IntPtr h, int acc, ref IntPtr phtok);
            //импортируем API функцию LookupPrivilegeValue
            [DllImport("advapi32.dll", SetLastError = true)]
            internal static extern bool LookupPrivilegeValue(string host, string name, ref long pluid);
            //импортируем API функцию LockWorkStation
            [DllImport("user32.dll", EntryPoint = "LockWorkStation")]
            static extern bool LockWorkStation();
            //объявляем структуру TokPriv1Luid для работы с привилегиями
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            internal struct TokPriv1Luid
            {
                public int Count;
                public long Luid;
                public int Attr;
            }

            //объявляем необходимые, для API функций, константые значения, согласно MSDN
            internal const int SE_PRIVILEGE_ENABLED = 0x00000002;
            internal const int TOKEN_QUERY = 0x00000008;
            internal const int TOKEN_ADJUST_PRIVILEGES = 0x00000020;
            internal const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";

            //функция SetPriv для повышения привилегий процесса
            private void SetPriv()
            {
                TokPriv1Luid tkp; //экземпляр структуры TokPriv1Luid 
                IntPtr htok = IntPtr.Zero;
                //открываем "интерфейс" доступа для своего процесса
                if (OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, ref htok))
                {
                    //заполняем поля структуры
                    tkp.Count = 1;
                    tkp.Attr = SE_PRIVILEGE_ENABLED;
                    tkp.Luid = 0;
                    //получаем системный идентификатор необходимой нам привилегии
                    LookupPrivilegeValue(null, SE_SHUTDOWN_NAME, ref tkp.Luid);
                    //повышаем привилегию своему процессу
                    AdjustTokenPrivileges(htok, false, ref tkp, 0, IntPtr.Zero, IntPtr.Zero);
                }
            }

            //публичный метод для перезагрузки/выключения машины
            public int halt(bool RSh, bool Force)
            {
                SetPriv(); //получаем привилегии
                           //вызываем функцию InitiateSystemShutdown, передавая ей необходимые параметры
                return InitiateSystemShutdown(null, null, 0, Force, RSh);
            }

            //публичный метод для блокировки операционной системы
            public int Lock()
            {
                if (LockWorkStation())
                    return 1;
                else
                    return 0;
            }
        }


        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;
        public const int SW_MINIMIZE = 6;

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
                    //client.DeleteMessage(i);
                    CtlMsg tmp = new CtlMsg();
                    {
                        tmp.from = client.GetMessageHeaders(i).From.Address;
                        if (client.GetMessage(i).FindFirstPlainTextVersion() == null)
                            tmp.ctlmsg = client.GetMessage(i).FindFirstHtmlVersion().GetBodyAsText();
                        else
                            tmp.ctlmsg = client.GetMessage(i).FindFirstPlainTextVersion().GetBodyAsText();


                        foreach (IPAddress ipaddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                            if (tmp.ctlmsg.Contains(ipaddress.ToString()))
                            {
                                client.DeleteMessage(i);
                                QueueCommand.Enqueue(tmp);
                            }
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
            //ShowWindow(GetConsoleWindow(), SW_HIDE);
            foreach (IPAddress ipaddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                LocalIp += ipaddress.ToString();

            new Thread(()=> 
            {
                while (true)
                {
                    try
                    {
                        FetchAllMessages("172.30.1.47", 110, false, "telemex", "teletele");
                    }
                    catch { }
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
