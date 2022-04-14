using FileManager.Events;
using FileManager.Exceptions;
using FileManager.Models.Serializable;
using FileManager.SocketLib.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.SocketLib.SocketServer
{
    public partial class SocketServer : SocketServerBase
    {

        /// <summary>
        /// 在获取 Responder 后, 向 client 端的 session 请求做出响应
        /// 这里的 session 响应不应改变 SessionBytes, 仅反馈 client 端的 SessionBytes 是否有效
        /// </summary>
        /// <param name="responder"></param>
        /// <returns> server 端新建立的或查找到的 session 对象, 不成功返回 null </returns>
        private SocketSession ResponseSession(SocketResponder responder, byte[] bytes)
        {
            SessionRequest request = SessionRequest.FromBytes(bytes);
            if (request.Type == SessionRequest.BytesType.KeyBytes)
            {
                SessionsLock.EnterWriteLock();
                try
                {
                    SocketSession ss = CreateSession(request.Bytes);
                    Sessions.Add(ss.BytesInfo.Index, ss);
                    SessionResponse response = new SessionResponse()
                    {
                        Type = SessionResponse.ResponseType.NewSessionBytes,
                        Bytes = ss.BytesInfo.ToBytes()
                    };
                    responder.SendBytes(SocketPacketFlag.SessionResponse, response.ToBytes());
                    return ss;
                }
                finally
                {
                    SessionsLock.ExitWriteLock();
                }
            }
            else if (request.Type == SessionRequest.BytesType.SessionBytes)
            {
                SessionsLock.EnterReadLock();
                try
                {
                    SessionBytesInfo sessionBytesInfo = SessionBytesInfo.FromBytes(bytes);
                    if (Sessions.ContainsKey(sessionBytesInfo.Index))
                    {
                        SocketSession ss = Sessions[sessionBytesInfo.Index];
                        if (sessionBytesInfo.Equals(ss.BytesInfo))
                        {
                            /// SessionBytes 校验通过
                            SessionResponse response = new SessionResponse()
                            {
                                Type = SessionResponse.ResponseType.NoModify,
                                Bytes = new byte[0]
                            };
                            responder.SendBytes(SocketPacketFlag.SessionResponse, response.ToBytes());
                            return ss;
                        }
                        else
                        {
                            /// client 和 server 端内容不一致
                            throw new ServerInternalException("Content mismatch");
                        }
                    }
                    else
                    {
                        /// client 和 server 端内容不一致, index 不存在
                        throw new ServerInternalException("Invalid index");
                    }
                }
                catch (ServerInternalException ex)
                {
                    SessionResponse response = new SessionResponse()
                    {
                        Type = SessionResponse.ResponseType.SessionException,
                        Bytes = Encoding.UTF8.GetBytes(ex.Message)
                    };
                    responder.SendBytes(SocketPacketFlag.SessionResponse, response.ToBytes());
                    return null;
                }
                finally
                {
                    SessionsLock.ExitReadLock();
                }
            }
            else
            {
                return null;
            }
        }



        private SocketSession CreateSession(byte[] key_bytes)
        {
            /// SessionBytesInfo
            int sid;
            Random rd = new Random();
            for (sid = rd.Next(1, 2 << 30 - 1); ; sid = rd.Next(1, 2 << 30 - 1))
            {
                if (Sessions.ContainsKey(sid))
                {
                    continue;
                }
                else
                {
                    break;
                }
            }
            SocketIdentityCheckEventArgs e = new SocketIdentityCheckEventArgs(key_bytes);
            CheckIdentity(this, e);
            SessionBytesInfo bytes_info = new SessionBytesInfo(sid, e.Identity);
            bytes_info.UpdateVerificationBytes();
            /// SocketSession
            SocketSession ss = new SocketSession(bytes_info);
            return ss;
        }



        /*

        private SocketIdentity GetIdentity(byte[] KeyBytes)
        {
            SocketIdentityCheckEventArgs e = new SocketIdentityCheckEventArgs(KeyBytes);
            CheckIdentity(this, e);
            return e.Identity;
        }
        */


        private void ResponseSetSession(SocketResponder responder, byte[] bytes, SocketSession session)
        {
            try
            {
                SetSessionRequest request = SetSessionRequest.FromBytes(bytes);
                switch (request.Type)
                {
                    case SetSessionRequest.RequestType.FileReadOpen:
                        ResponseSetSessionForFileReadOpen(responder, request.Bytes, session);
                        break;
                    default:
                        throw new ServerInternalException("Invalid SetSession Type");
                }

            }
            catch (ServerInternalException ex)
            {

            }
        }

        private void ResponseSetSessionForFileReadOpen(SocketResponder responder, byte[] request_bytes, SocketSession session)
        {

        }






    }
}
