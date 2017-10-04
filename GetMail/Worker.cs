using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Drawing;
using static GetMail.Program;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;

namespace GetMail
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

    class Worker
    {
        MemoryStream l = new MemoryStream();
        void GetPrintScreen()
        {
            Bitmap printscreen = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            Graphics graphics = Graphics.FromImage(printscreen as Image);
            graphics.CopyFromScreen(0, 0, 0, 0, printscreen.Size);
            printscreen.Save(Path.GetTempPath() +"printscreen.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
            //Console.WriteLine("J1R");
            //printscreen.Save(l, System.Drawing.Imaging.ImageFormat.Jpeg);
            //Console.WriteLine("JR");
        }

        string GetProcList()
        {
            Process[] procList = Process.GetProcesses();
            string message = "";
            for (int i=0; i< procList.Length;i++)
            {
                message += procList[i].ProcessName + " " + "\n";
            }
            return message;
        }

        public void QueueThread(ref Queue<CtlMsg> q)
        {
            while (true)
            {
                
                Thread.Sleep(2000);
                if (q.Count > 0)
                {
                    
                    CtlMsg command = q.Peek();
                    Console.WriteLine(command.from + " " + command.ctlmsg);
                    foreach (IPAddress ipaddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                        if (command.ctlmsg.Contains(ipaddress.ToString()))
                        {
                            //Если это тот компьютер
                            if (command.ctlmsg.Contains("screenshot"))
                            {
                                GetPrintScreen();
                                Console.WriteLine("Отправляем: " + command.from);
                                Messaging.SendMail(command.from, "telemex@om.mrsks.ru", "Скриншот", "", Path.GetTempPath() + "printscreen.jpg", "telemex", "teletele", "172.30.1.47");
                            }

                            if (command.ctlmsg.Contains("process"))
                            {
                                Console.WriteLine("Отправляем список процессов: " + command.from);
                                Messaging.SendMail(command.from, "telemex@om.mrsks.ru", "Список процессов", GetProcList(), "telemex", "teletele", "172.30.1.47");
                            }

                            if (command.ctlmsg.Contains("netsrv"))
                            {
                                Console.WriteLine("Получена команда перезагрузки телемеханики, выполняю: " + command.from);
                                //Messaging.SendMail(command.from, "telemex@om.mrsks.ru", "Список процессов", GetProcList(), "telemex", "teletele", "172.30.1.47");
                                Process.Start(@"C:\ntsrv.cmd");
                            }
                            if (command.ctlmsg.Contains("om2000"))
                            {
                                Console.WriteLine("Получена команда перезагрузки телемеханики, выполняю: " + command.from);
                                //Messaging.SendMail(command.from, "telemex@om.mrsks.ru", "Список процессов", GetProcList(), "telemex", "teletele", "172.30.1.47");
                                Process.Start(@"C:\om2000.cmd");
                            }
                            if (command.ctlmsg.Contains("telemex"))
                            {
                                Console.WriteLine("Получена команда перезагрузки телемеханики, выполняю: " + command.from);
                                //Messaging.SendMail(command.from, "telemex@om.mrsks.ru", "Список процессов", GetProcList(), "telemex", "teletele", "172.30.1.47");
                                Process.Start(@"C:\telemex.cmd");
                            }
                            if (command.ctlmsg.Contains("reboot"))
                            {
                                reboot var = new reboot();
                                var.halt(true, false);
                            }
                            if (command.ctlmsg.Contains("freboot"))
                            {
                                reboot var = new reboot();
                                var.halt(true, true);
                            }
                            if (q.Count > 0)
                                q.Dequeue();
                        }
                }
            }
        }
    }
}
