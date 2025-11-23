using FileManager.Models.EncryptLib;
using FileManager.Models.Serializable.Crypto;
using FileManager.Models.SocketLib.Enums;
using FileManager.Models.SocketLib.SocketIO;
using FileManager.Services.Certificate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Models.SocketLib.SocketServer.Main
{
    public partial class SocketServer : SocketServerBase
    {
        private void ResponseKeyExchange(SocketResponder responder, KeyExchangeRequest request)
        {
            /// Check client public key
            if (!CertificateService.IsTrustedEndPoint(request.Message.IdentityPublicKey, CertificateService.Side.Client)) 
            {
                var response = new KeyExchangeResponse()
                {
                    ResponseStatus = KeyExchangeResponse.Status.UnauthedClient,
                    Message = KeyExchangeMessage.Empty
                };
                Response(responder, PacketType.KeyExchangeResponse, response, encrypt: false);
                return;
            }
            /// Check message
            if (!CertificateService.VerifyKeyExchangeMessage(request.Message))
            {
                var response = new KeyExchangeResponse()
                {
                    ResponseStatus = KeyExchangeResponse.Status.VerifyMessageFailed,
                    Message = KeyExchangeMessage.Empty
                };
                Response(responder, PacketType.KeyExchangeResponse, response, encrypt: false);
                return;
            }
            /// Exchange keys
            else
            {
                var message = CertificateService.CreateKeyExchangeMessage(CertificateService.Side.Server);
                var response = new KeyExchangeResponse()
                {
                    ResponseStatus = KeyExchangeResponse.Status.Success,
                    Message = message
                };
                var key = CertificateService.DeriveAes256Key(request.Message, CertificateService.Side.Server, request.Message.Salt);
                responder.SetSymmetricKeys(key);
                Response(responder, PacketType.KeyExchangeResponse, response, encrypt: false);
                return;
            }



        }


    }
}
