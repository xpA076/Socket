using FileManager.Exceptions.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.IO;
using FileManager.Models.Serializable.HeartBeat;
using FileManager.Models.SocketLib.SocketIO;
using FileManager.Models.SocketLib.Enums;

namespace FileManager.Models.SocketLib.SocketServer.Main
{
    public partial class SocketServer : SocketServerBase
    {
        private void ResponseCustomizedPacket(SocketResponder responder, byte[] bytes)
        {
            
            try
            {
                string s = Encoding.UTF8.GetString(bytes);
                Log("Customized packet : " + s, LogLevel.Warn);
                if (s.StartsWith("netease_"))
                {
                    PostNeteaseRequest(s.Substring(8));
                }
            }
            catch (Exception ex)
            {
                throw new ServerInternalException(ex.Message);
            }
        }

        private string PostNeteaseRequest(string command)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:9980/keyboard/netease?command=" + command);
            request.Method = "POST";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using (StreamReader reader = new StreamReader(response.GetResponseStream(), System.Text.Encoding.GetEncoding("UTF-8")))
            {
                return reader.ReadToEnd();
            }

        }

        private void ResponseHeartBeat(SocketResponder responder, HeartBeatRequest request)
        {
            HeartBeatResponse response = new HeartBeatResponse();
            this.Response(responder, response);
        }

    }
}
