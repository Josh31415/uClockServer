using System;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

class Server
{
    public string myConnectionString = "";
    TcpListener listener = new TcpListener(IPAddress.Any, 19023);
    Thread tcpThread;
    TcpClient client;

    public void startListener()
    {
        this.listener.Start();
    }

    public void serverRun()
    {

        if (!this.listener.Pending())
        {
            client = listener.AcceptTcpClient();
            tcpThread = new Thread(new ParameterizedThreadStart(tcpHandler));
            tcpThread.Start(client);
        }

    }

    public string GetPacket(byte[] byteStream)
    {
        int[] bytesAsInts = byteStream.Select(x => (int)x).ToArray();
        int i = 0;
        int g;
        try
        {
            while (true)
            {
                g = bytesAsInts[i];
                if (g > 0) i++;
                else break;      
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Packet Error");
            tcpThread.Abort();
        }

        Array.Resize(ref byteStream, i);
        string command = Encoding.ASCII.GetString(byteStream);

        return command;
    }

    public void returnData(DataTable d, NetworkStream stream)
    {
        //Serialize Query Results
        string JSONString = string.Empty;
        JSONString = JsonConvert.SerializeObject(d);
        byte[] JSONbytes = Encoding.ASCII.GetBytes(JSONString);
        Console.WriteLine(JSONString);
        Console.WriteLine(JSONbytes);
        stream.Write(JSONbytes, 0, JSONbytes.Length);
    }

    private void tcpHandler(object client)
    {
        string command = "";
        var parameters = new string[2];

        TcpClient mClient = (TcpClient)client;
        NetworkStream stream = mClient.GetStream();
        MySqlConnection conn = new MySqlConnection(myConnectionString);
        DataTable dt = new DataTable();
        MySqlCommand cmd;
        MySqlDataAdapter sda;
        
        // Get the number of packets
        byte[] byteStream = new byte[1];
        stream.Read(byteStream, 0, byteStream.Length);

        if(Int32.TryParse(this.GetPacket(byteStream), out int pacNum))
        {
            parameters = new string[pacNum - 2];
        }
        else
        {
            tcpThread.Abort();
            conn.Close();

            mClient.Close();
        }

        // Get the sql query string
        stream.Read(byteStream, 0, byteStream.Length);
        command = this.GetPacket(byteStream);

        // Get the rest of the packets
        for (int i = 2; i < byteStream[0]; i++)
        {
            stream.Read(byteStream, 0, byteStream.Length);
            parameters[i] = this.GetPacket(byteStream);
        }

        try { 
            //Query from the SQL server
            conn = new MySqlConnection(myConnectionString);
            conn.Open();

            //cmd = new MySqlCommand(command, conn);

            cmd = new MySqlCommand(command);

            // Adds parameters to the sql command
            for(int i = 0; i < parameters.Length; i++)
            {
                cmd.Parameters.AddWithValue("@"+ parameters[i], parameters[i]);
            }

            cmd.ExecuteReader();
            
            sda = new MySqlDataAdapter(cmd);
            sda.Fill(dt);

            this.returnData(dt, stream);

            tcpThread.Abort();
            conn.Close();

            mClient.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Connection Error");
        }

    }
}