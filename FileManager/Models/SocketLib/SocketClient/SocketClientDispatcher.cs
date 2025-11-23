using FileManager.Exceptions;
using FileManager.Models.EncryptLib;
using FileManager.Models.Serializable;
using FileManager.Models.Serializable.Crypto;
using FileManager.Models.SocketLib.Enums;
using FileManager.Models.SocketLib.Models;
using FileManager.Models.SocketLib.SocketIO;
using FileManager.Services.Certificate;
using FileManager.Services.Config;
using FileManager.Utils.Bytes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FileManager.Models.SocketLib.SocketClient
{
    public class SocketClientDispatcher : IDisposable
    {
        private class SendItem
        {

            public required Guid RequestId { get; set; }

            public required byte[] Data {  get; set; }

            public required CancellationToken CancellationToken { get; set; }
        }


        private readonly ConfigService configService = Program.Provider.GetRequiredService<ConfigService>();
        private readonly CertificateService certificateService = Program.Provider.GetRequiredService<CertificateService>();

        public TCPAddress HostAddress { get; private set; }

        /// some private utils
        private SocketRequester? _requester;
        private readonly object _connectionLock = new object();
        private bool _initialized = false;
        private bool _isConnected = false;
        private bool _disposed = false;

        /// Send & receive queue
        private BlockingCollection<SendItem> _sendQueue;
        private ConcurrentDictionary<Guid, TaskCompletionSource<byte[]>> _pendingRequests;

        /// Working thread
        private Thread _sendThread;
        private Thread _receiveThread;
        private CancellationTokenSource _cancellationTokenSource;

        public DateTime LastHandShake { get; private set; } = DateTime.MinValue;

        public SocketClientDispatcher()
        {

        }

        public void SetHostAddress(TCPAddress address)
        {
            this.HostAddress = address;
        }

        public void Initialize()
        {
            if (_initialized) { return; }
            /// Initailize parameters
            _sendQueue = new BlockingCollection<SendItem>();
            _pendingRequests = new ConcurrentDictionary<Guid, TaskCompletionSource<byte[]>>();

            /// Start worker threads
            _cancellationTokenSource = new CancellationTokenSource();
            _sendThread = new Thread(SendWorker)
            {
                Name = "SocketSendWorker",
                IsBackground = true
            };
            _receiveThread = new Thread(ReceiveWorker)
            {
                Name = "SocketReceiveWorker",
                IsBackground = true
            };
            _sendThread.Start();
            _receiveThread.Start();
            _initialized = true;
        }

        public void ShutDown()
        {
            _initialized = false;
            _cancellationTokenSource?.Cancel();

            // 完成所有等待的请求
            foreach (var pending in _pendingRequests)
            {
                pending.Value.TrySetCanceled();
            }
            _pendingRequests.Clear();

            _sendQueue?.CompleteAdding();

            // 等待工作线程结束
            _sendThread?.Join(1000);
            _receiveThread?.Join(1000);

            Disconnect();

            _sendQueue?.Dispose();
            _cancellationTokenSource?.Dispose();
        }


        public async Task<byte[]> RequestAsync(ISocketSerializable request)
        {
            /// assertion
            if (!_initialized)
            {
                throw new InvalidOperationException("Dispatcher not initialized");
            }
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(request);

            /// Connection
            EnsureConnected();

            /// Generate content bytes
            var requestId = Guid.NewGuid();
            var (requestBytes, aimType) = BuildRequestBytes(requestId, request);

            /// Create request
            var tcs = new TaskCompletionSource<byte[]>();
            if (!_pendingRequests.TryAdd(requestId, tcs))
            {
                throw new InvalidOperationException("Failed to create request");
            }

            try
            {
                var sendItem = new SendItem
                {
                    RequestId = requestId,
                    Data = requestBytes,
                    CancellationToken = CancellationToken.None
                };

                _sendQueue.Add(sendItem);

                /// 设置超时和完成监控
                using var timeoutCts = new CancellationTokenSource(configService.SocketReceiveTimeout);
                using var completionCts = new CancellationTokenSource();

                /// 当任务完成时取消完成令牌
                _ = tcs.Task.ContinueWith(_ => completionCts.Cancel(), TaskContinuationOptions.ExecuteSynchronously);

                /// 创建链接令牌：超时或任务完成都会触发
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    timeoutCts.Token,
                    completionCts.Token
                );

                /// 注册取消回调：只有超时才会取消任务
                using (linkedCts.Token.Register(() =>
                {
                    /// 检查是否是超时导致的取消
                    if (timeoutCts.Token.IsCancellationRequested && !tcs.Task.IsCompleted)
                        tcs.TrySetCanceled();
                }))
                {
                    return await tcs.Task;
                }
            }
            catch (OperationCanceledException)
            {
                _pendingRequests.TryRemove(requestId, out _);
                throw new TimeoutException("Request timed out");
            }
            catch (Exception)
            {
                _pendingRequests.TryRemove(requestId, out _);
                throw;
            }
        }

        private (byte[], PacketType) BuildRequestBytes(Guid guid, ISocketSerializable request)
        {
            BytesBuilder bb = new BytesBuilder();
            bb.Concatenate(guid.ToByteArray());
            PacketType aimType = PacketType.None;
            switch (request.GetType().Name)
            {
                case "KeyExchangeRequest":
                    bb.Append((int)PacketType.KeyExchangeRequest);
                    aimType = PacketType.KeyExchangeResponse;
                    break;
                case "SessionRequest":
                    bb.Append((int)PacketType.SessionRequest);
                    aimType = PacketType.SessionResponse;
                    break;
                case "DirectoryRequest":
                    bb.Append((int)PacketType.DirectoryRequest);
                    aimType = PacketType.DirectoryResponse;
                    break;
                case "DownloadRequest":
                    bb.Append((int)PacketType.DownloadRequest);
                    aimType = PacketType.DownloadResponse;
                    break;
                case "UploadRequest":
                    bb.Append((int)PacketType.UploadRequest);
                    aimType = PacketType.UploadResponse;
                    break;
                case "ReleaseFileRequest":
                    bb.Append((int)PacketType.ReleaseFileRequest);
                    aimType = PacketType.ReleaseFileResponse;
                    break;
                case "HeartBeatRequest":
                    bb.Append((int)PacketType.HeartBeatRequest);
                    aimType = PacketType.HeartBeatResponse;
                    break;
            }
            bb.Concatenate(request.ToBytes());
            return (bb.GetBytes(), aimType);
        }


        private void SendWorker()
        {
            try
            {
                foreach (var sendItem in _sendQueue.GetConsumingEnumerable(_cancellationTokenSource.Token))
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                        break;

                    try
                    {
                        EnsureConnected();
                        lock (_connectionLock)
                        {
                            if (_isConnected && _requester != null)
                            {
                                _requester.SendBytes(sendItem.Data);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // 发送失败，通知等待的请求
                        if (_pendingRequests.TryRemove(sendItem.RequestId, out var tcs))
                        {
                            tcs.TrySetException(new InvalidOperationException("Send failed", ex));
                        }

                        // 断开连接，触发重连
                        Disconnect();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常退出
            }
        }

        private void ReceiveWorker()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    EnsureConnected();

                    if (_requester != null && _requester.Connected)
                    {
                        var allResponseData = _requester.ReceiveBytes();
                        var requestId = new Guid((new ArraySegment<byte>(allResponseData, 0, 16)).ToArray());
                        var responseData = (new ArraySegment<byte>(allResponseData, 16, allResponseData.Length - 16)).ToArray();
                        /// 找到对应的请求并设置结果
                        if (_pendingRequests.TryRemove(requestId, out var tcs))
                        {
                            tcs.TrySetResult(responseData);
                            this.LastHandShake = DateTime.Now;
                        }
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Receive worker error: {ex.Message}");
                    Disconnect();
                    Thread.Sleep(1000);
                }
            }
        }

        #region Connection

        private void EnsureConnected()
        {
            lock (_connectionLock)
            {
                if (!_isConnected || this._requester == null || !this._requester.Connected)
                {
                    Connect();
                }
            }
        }

        /// <summary>
        /// 建立连接
        /// </summary>
        private void Connect()
        {
            Disconnect();
            try
            {
                _requester = new SocketRequester(HostAddress);
                _requester.ConnectWithTimeout(this.configService.BuildConnectionTimeout);
                /// Exchange keys
                ExchangeKeys();
                _isConnected = true;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _requester?.Dispose();
                _requester = null;
                throw new InvalidOperationException($"Failed to connect", ex);
            }
        }

        public async Task ConnectAsync()
        {
            await Task.Run(() => { Connect(); });
        }

        private void ExchangeKeys()
        {
            ArgumentNullException.ThrowIfNull(_requester);
            /// Build request
            var clientMesage = certificateService.CreateKeyExchangeMessage(CertificateService.Side.Client);
            var request = new KeyExchangeRequest() { Message = clientMesage };
            var requestId = Guid.NewGuid();
            var (requestBytes, _) = BuildRequestBytes(requestId, request);
            _requester.SendBytes(requestBytes, encrypt: false);

            /// Get response
            var allResponseData = _requester.ReceiveBytes();
            //Thread.Sleep(600 * 1000);
            //var allResponseData = new byte[500];
            var response = KeyExchangeResponse.Build(allResponseData.AsSpan(16 + 4));

            /// Check response
            if (response.ResponseStatus != KeyExchangeResponse.Status.Success)
            {
                throw new ArgumentException(response.ResponseStatus.ToString());
            }
            if (!certificateService.IsTrustedEndPoint(response.Message.IdentityPublicKey, CertificateService.Side.Server))
            {
                throw new Exception("server untrusted");
            }
            if (!CertificateService.VerifyKeyExchangeMessage(response.Message))
            {
                throw new Exception("server message unverified");
            }

            /// Setup keys
            var key = certificateService.DeriveAes256Key(response.Message, CertificateService.Side.Client, request.Message.Salt);
            _requester.SetSymmetricKeys(key);

        }

        /// <summary>
        /// 断开连接
        /// </summary>
        private void Disconnect()
        {
            lock (_connectionLock)
            {
                if (_requester != null)
                {
                    try
                    {
                        if (_requester.Connected)
                        {
                            _requester.Shutdown();
                        }
                        _requester.Dispose();
                    }
                    catch
                    {
                        // 忽略异常
                    }
                    finally
                    {
                        _requester = null;
                        _isConnected = false;
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _cancellationTokenSource?.Cancel();

                // 完成所有等待的请求
                foreach (var pending in _pendingRequests)
                {
                    pending.Value.TrySetCanceled();
                }
                _pendingRequests.Clear();

                _sendQueue?.CompleteAdding();

                // 等待工作线程结束
                _sendThread?.Join(1000);
                _receiveThread?.Join(1000);

                Disconnect();

                _sendQueue?.Dispose();
                _cancellationTokenSource?.Dispose();
            }
        }

    }
}
