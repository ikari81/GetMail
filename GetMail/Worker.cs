using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Drawing;
using static GetMail.Program;
using System.Windows.Forms;
using System.Diagnostics;

namespace GetMail
{
    class Worker
    {
        void GetPrintScreen()
        {
            Bitmap printscreen = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            Graphics graphics = Graphics.FromImage(printscreen as Image);
            graphics.CopyFromScreen(0, 0, 0, 0, printscreen.Size);
            printscreen.Save(@"D:\printscreen.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
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
                    
                    CtlMsg command = q.Dequeue();
                    foreach (IPAddress ipaddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                        if (command.ctlmsg.Contains(ipaddress.ToString()))
                        {
                            //Если это тот компьютер
                            if (command.ctlmsg.Contains("screenshot"))
                            {
                                GetPrintScreen();
                                Console.WriteLine("Отправляем: " + command.from);
                                Messaging.SendMail(command.from, "telemex@om.mrsks.ru", "Скриншот", "", @"D:\printscreen.jpg", "telemex", "teletele", "172.30.1.47");
                            }

                            if (command.ctlmsg.Contains("process"))
                            {
                                Console.WriteLine("Отправляем список процессов: " + command.from);
                                Messaging.SendMail(command.from, "telemex@om.mrsks.ru", "Список процессов", GetProcList(), "telemex", "teletele", "172.30.1.47");
                            }

                            if (command.ctlmsg.Contains("netsrv"))
                            {
                                Console.WriteLine("Получена команда перезагрузки телемеханики, выполняю: " + command.from);
                                Messaging.SendMail(command.from, "telemex@om.mrsks.ru", "Список процессов", GetProcList(), "telemex", "teletele", "172.30.1.47");
                            }


                        }
                }
            }
        }
    }
}
