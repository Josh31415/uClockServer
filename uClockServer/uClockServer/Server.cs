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
        int g = 0;

        try
        {
            while (i < bytesAsInts.Length) {
                g = bytesAsInts[i];
                if (g != 0) i++;
                else break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Packet Error");
            Console.WriteLine(ex);
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
        DataSet ds = new DataSet();
        MySqlDataAdapter sda;
        int pacNum = 0;
        
        // Get the number of packets
        byte[] byteStream = new byte[200];
        stream.Read(byteStream, 0, byteStream.Length);
        try
        {
            pacNum = Int32.Parse(this.GetPacket(byteStream));
            parameters = new string[pacNum - 2];
            stream.Write(byteStream, 0, byteStream.Length);
        }
        
        catch(Exception ex)
        {
            tcpThread.Abort();
            conn.Close();
            mClient.Close();
        }

        // Get the sql query string
        byteStream = new byte[500];
        stream.Read(byteStream, 0, byteStream.Length);
        command = this.GetPacket(byteStream);
        stream.Write(byteStream, 0, byteStream.Length);

        // Get the rest of the packets
        for (int i = 0; i < pacNum - 2; i++)
        {
            byteStream = new byte[200];
            stream.Read(byteStream, 0, byteStream.Length);
            stream.Write(byteStream, 0, byteStream.Length);
            parameters[i] = this.GetPacket(byteStream);
        }

        try { 
            //Query from the SQL server
            conn = new MySqlConnection(myConnectionString);
            conn.Open();

            cmd = new MySqlCommand(command, conn);

            //cmd = new MySqlCommand(command);

            // Adds parameters to the sql command
            for(int i = 0; i < parameters.Length; i++)
            {
                cmd.Parameters.AddWithValue("@"+ parameters[i], parameters[i]);
            }

            MySqlDataReader rd = cmd.ExecuteReader();
            ds.Tables.Add(dt);
            ds.EnforceConstraints = false;
            dt.Load(rd);
            
            this.returnData(dt, stream);

            conn.Close();
            rd.Close();
            dt.Dispose();
            cmd.Dispose();
            mClient.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Connection Error");
        }

    }
}