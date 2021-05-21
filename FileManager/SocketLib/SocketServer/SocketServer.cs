using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using FileManager.Events;
using FileManager.SocketLib.Enums;


namespace FileManager.SocketLib.SocketServer
{

    public partial class SocketServer : SocketServerBase
    {
        public event SocketIdentityCheckEventHandler CheckIdentity;

        public SocketServerConfig Config { get; set; } = new SocketServerConfig();

        public SocketServer(IPAddress ip):base(ip)
        {

        }

        private readonly Dictionary<Socket, SocketIdentity> ClientIdentities = new Dictionary<Socket, SocketIdentity>();
        private readonly object ClientIdentitiesLock = new object();


        /// <summary>
        /// 在 Socket.accept() 获取到的 client 在这里处理
        /// 这个函数为 client 的整个生存周期
        /// </summary>
        /// <param name="acceptSocketObject">client socket</param>
        public override void ReceiveData(object acceptSocketObject)
        {
            Socket client = (Socket)acceptSocketObject;
            try
            {
                client.SendTimeout = Config.SocketSendTimeOut;
                client.ReceiveTimeout = Config.SocketReceiveTimeOut;
                this.ReceiveBytes(client, out HB32Header ac_header, out byte[] ac_bytes);
                SocketIdentityCheckEventArgs e = new SocketIdentityCheckEventArgs(ac_header, ac_bytes);
                CheckIdentity(this, e);
                SocketIdentity identity = e.CheckedIndentity;
                ClientIdentities.Add(client, identity);
                this.SendHeader(client, SocketPacketFlag.AuthenticationResponse, (int)identity);
                int error_count = 0;
                while (flag_receive & error_count < 5)
                {
                    try
                    {
                        ReceiveBytes(client, out HB32Header header, out byte[] bytes);
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
                                ResponseFileStreamId(client, header, bytes);
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
                                ResponseFileStreamId(client, header, bytes);
                                break;
                            case SocketPacketFlag.UploadPacketRequest:
                                ResponseTransferPacket(client, header, bytes);
                                break;
                            #endregion

                            case SocketPacketFlag.StatusReport:
                                RecordStatusReport(client, bytes);
                                break;


                            case SocketPacketFlag.DisconnectRequest:
                                DisposeClient(client);
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
                                DisposeClient(client);
                                Log("Connection closed (client closed). " + ex.Message, LogLevel.Info);
                                return;
                            // Socket 超时
                            case 10060:
                                Thread.Sleep(200);
                                Log("Socket timeout. " + ex.Message, LogLevel.Trace);
                                continue;
                            default:
                                Log("Server receive data :" + ex.Message, LogLevel.Warn);
                                continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        error_count++;
                        if (ex.Message.Contains("Buffer receive error: cannot receive package"))
                        {
                            DisposeClient(client);
                            Log(ex.Message, LogLevel.Trace);
                            return;
                        }
                        if (ex.Message.Contains("Invalid socket header"))
                        {
                            DisposeClient(client);
                            Log("Connection closed : " + ex.Message, LogLevel.Warn);
                            return;
                        }
                        Log("Server exception :" + ex.Message, LogLevel.Warn);
                        Thread.Sleep(200);
                        continue;
                    }
                }
                Log("Connection closed.", LogLevel.Warn);
            }
            catch (Exception ex)
            {
                Log("Identity authentication exception :" + ex.Message, LogLevel.Error);
                ClientIdentities.Remove(client);
            }
        }



        private void DisposeClient(Socket client)
        {
            try
            {
                client.Close();
            }
            catch (Exception) { }
            finally
            {
                ClientIdentities.Remove(client);
            }
        }


        private void RecordStatusReport(Socket client, byte[] bytes)
        {
            Log(Encoding.UTF8.GetString(bytes), LogLevel.Info);
        }

        private SocketIdentity GetIdentity(Socket socket)
        {
            lock (ClientIdentitiesLock)
            {
                try
                {
                    return ClientIdentities[socket];
                }
                catch (Exception)
                {
                    return SocketIdentity.None;
                }
            }
        }



    }
}
