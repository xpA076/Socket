using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using SocketLib.Enums;


namespace SocketLib.SocketServer
{
    public partial class SocketServer : SocketServerBase
    {
        public SocketServer(IPAddress ip):base(ip)
        {
            
        }


        public override void ReceiveData(object acceptSocketObject)
        {
            try
            {
                Socket client = (Socket)acceptSocketObject;
                client.SendTimeout = Config.SocketSendTimeOut;
                client.ReceiveTimeout = Config.SocketReceiveTimeOut;
                int error_count = 0;
                while (flag_receive & error_count < 5)
                {
                    try
                    {
                        ReceiveBytes(client, out HB32Header header, out byte[] bytes);
                        //Display.TimeWriteLine(header.Flag.ToString());
                        switch (header.Flag)
                        {
                            case SocketPacketFlag.DirectoryRequest:
                                ResponseDirectory(client, bytes);
                                break;
                            case SocketPacketFlag.DirectorySizeRequest:
                                ResponseDirectorySize(client, bytes);
                                break;

                            case SocketPacketFlag.CreateDirectoryRequest:
                                ResponseCreateDirectory(client, bytes);
                                break;

                            #region download
                            case SocketPacketFlag.DownloadRequest:
                                ResponseDownloadSmallFile(client, bytes);
                                break;
                            case SocketPacketFlag.DownloadFileStreamIdRequest:
                                ResponseFileStreamId(client, header, bytes, false);
                                break;
                            case SocketPacketFlag.DownloadPacketRequest:
                                ResponseTransferPacket(client, header, bytes);
                                break;
                            #endregion

                            #region upload
                            case SocketPacketFlag.UploadRequest:
                                ResponseUploadSmallFile(client, bytes);
                                break;
                            case SocketPacketFlag.UploadFileStreamIdRequest:
                                ResponseFileStreamId(client, header, bytes, true);
                                break;
                            case SocketPacketFlag.UploadPacketRequest:
                                ResponseTransferPacket(client, header, bytes);
                                break;
                            #endregion

                            case SocketPacketFlag.StatusReport:
                                RecordStatusReport(client, bytes);
                                break;


                            case SocketPacketFlag.DisconnectRequest:
                                client.Close();
                                return;
                            default:
                                throw new Exception("Invalid socket header in receiving: " + header.Flag.ToString());
                        }
                        error_count = 0;
                    }
                    catch (SocketException ex)
                    {
                        error_count++;
                        switch (ex.ErrorCode)
                        {
                            // 远程 client 主机关闭连接
                            case 10054:
                                client.Close();
                                Log("Connection closed (client closed). " + ex.Message, LogLevel.Info);
                                return;
                            // Socket 超时
                            case 10060:
                                Thread.Sleep(200);
                                Log("Socket timeout. " + ex.Message, LogLevel.Trace);
                                continue;
                            default:
                                //System.Windows.Forms.MessageBox.Show("Server receive data :" + ex.Message);
                                Log("Server receive data :" + ex.Message, LogLevel.Warn);
                                continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        error_count++;
                        if (ex.Message.Contains("Buffer receive error: cannot receive package"))
                        {
                            client.Close();
                            Log(ex.Message, LogLevel.Trace);
                            return;
                        }
                        if (ex.Message.Contains("Invalid socket header"))
                        {
                            client.Close();
                            Log("Connection closed : " + ex.Message, LogLevel.Warn);
                            return;
                        }
                        Log("Server exception :" + ex.Message, LogLevel.Warn);
                        Thread.Sleep(200);
                        continue;
                    }
                }
                Log("Connection closed: error count 5", LogLevel.Warn);
            }
            catch (Exception ex)
            {
                Log("WTF ReceiveData exception :" + ex.Message, LogLevel.Error);
            }

        }




        private void RecordStatusReport(Socket client, byte[] bytes)
        {
            Log(Encoding.UTF8.GetString(bytes), LogLevel.Info);
        }


        private bool CheckKey(byte[] bytes)
        {
            return true;
        }



        public void Close()
        {
            server.Close();
        }
    }
}
