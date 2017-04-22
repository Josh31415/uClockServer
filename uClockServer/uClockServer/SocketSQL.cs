using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace uClockServer
{
    public class SocketSQL
    {
       
        string ip;
        int tcp;
        TcpClient client = new TcpClient();

        public void connect(string ip1, int tcp1)
        {
            ip = ip1;
            tcp = tcp1;
            Thread mythread = new Thread(new ThreadStart(clientConn));
            mythread.Start();
        }

        private void clientConn()
        {
            //68.55.112.39

            client.Connect(IPAddress.Parse(ip), tcp);

        }

        public string NetSQLCommand(string comm)
        {
            NetworkStream stream = client.GetStream();


            byte[] message = Encoding.ASCII.GetBytes(comm);
            stream.Write(message, 0, message.Length);


            byte[] byteStream = new byte[1040];
            stream.Read(byteStream, 0, byteStream.Length);
            while (true)
            {


                int[] bytesAsInts = byteStream.Select(x => (int)x).ToArray();
                int i = 0;
                int g;
                try
                {
                    while (true)
                    {
                        g = bytesAsInts[i];

                        if (g > 0)
                        {
                            i++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {

                }
                Array.Resize(ref byteStream, i);

                //int[i] byte2 = new int[i](bytesAsInts);
                //string JSONString = string.Empty;

                //byte[] bytes = bytesAsInts.Select(x => (byte)x).ToArray();
                //string cmd = BitConverter.ToString(byteStream);
                string cmd = Encoding.ASCII.GetString(byteStream);
                return cmd;
                stream.Close();
                client.Close();

            }
            
        }
    }
}
