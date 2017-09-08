using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Mail;
using System.Net.Sockets;
using System.Net;

namespace GetMail
{
    class Messaging
    {
        public static void SendMail(string email, string from, string subj, string message, string smtpIP)
        {
            //return;
            try
            {
                SmtpClient Smtp = new SmtpClient(smtpIP, 25);
                MailMessage Message = new MailMessage();
                Message.From = new MailAddress(from);
                Message.To.Add(new MailAddress(email));
                Message.Subject = subj;
                Message.Body = message;
                Smtp.Send(Message);
            }
            catch(Exception e)
            {
                //Console.WriteLine(e.ToString());
            }
        }

        public static void SendSMS(string num, string message, string gsmIP)
        {
            //return;
            {
                Socket connection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint ipEndpoint;
                try
                {
                    ipEndpoint = new IPEndPoint(IPAddress.Parse(gsmIP), 2200);
                    connection.Connect(ipEndpoint);
                    connection.Send(Encoding.Unicode.GetBytes("SMS" + "@" + num + "@" + message + "@"));
                }
                catch
                {
                }
                connection.Close();
            }
        }
    }
}
