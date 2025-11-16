# Socket 长连接实现详解

在 C# 中实现 Socket 长连接需要处理连接保持、心跳检测、重连机制等。以下是完整的解决方案：

## 1. 基础长连接实现

### 客户端长连接类
```csharp
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class LongLiveSocketClient : IDisposable
{
    private Socket _socket;
    private readonly string _host;
    private readonly int _port;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private bool _isConnected = false;
    private bool _isDisposed = false;
    private readonly TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _reconnectInterval = TimeSpan.FromSeconds(5);
    private readonly object _lockObject = new object();

    // 事件定义
    public event Action<string> MessageReceived;
    public event Action Connected;
    public event Action Disconnected;
    public event Action<Exception> ErrorOccurred;

    public LongLiveSocketClient(string host, int port)
    {
        _host = host;
        _port = port;
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public async Task ConnectAsync()
    {
        if (_isConnected) return;

        try
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
            // 设置Socket选项以保持连接
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            _socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 60); // 60秒后开始发送KeepAlive
            _socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 10); // 每10秒发送一次
            _socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, 5); // 重试5次

            await _socket.ConnectAsync(_host, _port);
            
            lock (_lockObject)
            {
                _isConnected = true;
            }

            Connected?.Invoke();
            Console.WriteLine($"连接到服务器 {_host}:{_port} 成功");

            // 启动接收任务
            _ = Task.Run(() => ReceiveLoopAsync(_cancellationTokenSource.Token));
            
            // 启动心跳任务
            _ = Task.Run(() => HeartbeatLoopAsync(_cancellationTokenSource.Token));
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex);
            Console.WriteLine($"连接失败: {ex.Message}");
            await TryReconnectAsync();
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        
        while (!cancellationToken.IsCancellationRequested && _isConnected)
        {
            try
            {
                if (_socket == null || !_socket.Connected)
                {
                    await TryReconnectAsync();
                    continue;
                }

                int bytesReceived = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                if (bytesReceived == 0)
                {
                    // 连接被对端关闭
                    Console.WriteLine("服务器关闭了连接");
                    await TryReconnectAsync();
                    continue;
                }

                string message = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                MessageReceived?.Invoke(message);
                Console.WriteLine($"收到消息: {message}");
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
            {
                // 接收超时，继续循环
                continue;
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset)
            {
                Console.WriteLine("连接被重置");
                await TryReconnectAsync();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex);
                Console.WriteLine($"接收数据时出错: {ex.Message}");
                await TryReconnectAsync();
            }
        }
    }

    private async Task HeartbeatLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _isConnected)
        {
            try
            {
                await Task.Delay(_heartbeatInterval, cancellationToken);
                
                if (_isConnected && _socket?.Connected == true)
                {
                    await SendAsync("PING");
                    Console.WriteLine("发送心跳包");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送心跳包失败: {ex.Message}");
            }
        }
    }

    public async Task SendAsync(string message)
    {
        if (!_isConnected || _socket?.Connected != true)
        {
            throw new InvalidOperationException("Socket未连接");
        }

        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            await _socket.SendAsync(new ArraySegment<byte>(data), SocketFlags.None);
            Console.WriteLine($"发送消息: {message}");
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex);
            Console.WriteLine($"发送消息失败: {ex.Message}");
            await TryReconnectAsync();
            throw;
        }
    }

    private async Task TryReconnectAsync()
    {
        lock (_lockObject)
        {
            _isConnected = false;
        }
        
        Disconnected?.Invoke();
        Console.WriteLine("连接断开，尝试重连...");

        // 清理旧连接
        SafeCloseSocket(_socket);
        _socket = null;

        int retryCount = 0;
        const int maxRetries = 10;

        while (retryCount < maxRetries && !_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_reconnectInterval * (retryCount + 1), _cancellationTokenSource.Token);
                
                Console.WriteLine($"第 {retryCount + 1} 次重连尝试...");
                await ConnectAsync();
                
                if (_isConnected)
                {
                    Console.WriteLine("重连成功");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"第 {retryCount + 1} 次重连失败: {ex.Message}");
            }
            
            retryCount++;
        }

        Console.WriteLine($"重连失败，已尝试 {maxRetries} 次");
    }

    private void SafeCloseSocket(Socket socket)
    {
        if (socket == null) return;

        try
        {
            if (socket.Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
            }
        }
        catch (SocketException)
        {
            // 忽略关闭时的异常
        }
        finally
        {
            socket.Close();
            socket.Dispose();
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        
        _isDisposed = true;
        _cancellationTokenSource.Cancel();
        
        lock (_lockObject)
        {
            _isConnected = false;
        }
        
        SafeCloseSocket(_socket);
        _cancellationTokenSource.Dispose();
        
        Console.WriteLine("Socket客户端已释放");
    }
}
```

### 服务器端长连接处理
```csharp
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class LongLiveSocketServer : IDisposable
{
    private Socket _listener;
    private readonly ConcurrentDictionary<string, ClientSession> _clients;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private bool _isDisposed = false;
    private readonly int _port;

    public LongLiveSocketServer(int port)
    {
        _port = port;
        _clients = new ConcurrentDictionary<string, ClientSession>();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public async Task StartAsync()
    {
        _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _listener.Bind(new IPEndPoint(IPAddress.Any, _port));
        _listener.Listen(100);

        Console.WriteLine($"服务器启动，监听端口 {_port}");

        // 启动客户端清理任务
        _ = Task.Run(() => CleanupDeadClientsAsync(_cancellationTokenSource.Token));

        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                var clientSocket = await _listener.AcceptAsync();
                _ = Task.Run(() => HandleClientAsync(clientSocket, _cancellationTokenSource.Token));
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"接受客户端连接时出错: {ex.Message}");
            }
        }
    }

    private async Task HandleClientAsync(Socket clientSocket, CancellationToken cancellationToken)
    {
        var sessionId = Guid.NewGuid().ToString();
        var clientSession = new ClientSession(sessionId, clientSocket);

        // 设置KeepAlive
        clientSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        clientSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 60);
        clientSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 10);

        if (_clients.TryAdd(sessionId, clientSession))
        {
            Console.WriteLine($"客户端连接: {sessionId}, 当前连接数: {_clients.Count}");

            try
            {
                var buffer = new byte[4096];
                
                while (!cancellationToken.IsCancellationRequested && clientSocket.Connected)
                {
                    int bytesReceived = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                    if (bytesReceived == 0)
                    {
                        Console.WriteLine($"客户端 {sessionId} 断开连接");
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                    Console.WriteLine($"收到来自 {sessionId} 的消息: {message}");

                    // 处理心跳包
                    if (message == "PING")
                    {
                        await SendToClientAsync(sessionId, "PONG");
                        clientSession.UpdateLastActivity();
                        continue;
                    }

                    // 广播消息给所有客户端
                    await BroadcastAsync($"{sessionId}: {message}", sessionId);
                    
                    clientSession.UpdateLastActivity();
                }
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset)
            {
                Console.WriteLine($"客户端 {sessionId} 连接被重置");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理客户端 {sessionId} 时出错: {ex.Message}");
            }
            finally
            {
                _clients.TryRemove(sessionId, out _);
                SafeCloseSocket(clientSocket);
                Console.WriteLine($"客户端断开: {sessionId}, 当前连接数: {_clients.Count}");
            }
        }
    }

    private async Task SendToClientAsync(string sessionId, string message)
    {
        if (_clients.TryGetValue(sessionId, out var clientSession))
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                await clientSession.Socket.SendAsync(new ArraySegment<byte>(data), SocketFlags.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"向客户端 {sessionId} 发送消息失败: {ex.Message}");
                _clients.TryRemove(sessionId, out _);
            }
        }
    }

    private async Task BroadcastAsync(string message, string excludeSessionId = null)
    {
        var tasks = new List<Task>();
        byte[] data = Encoding.UTF8.GetBytes(message);

        foreach (var client in _clients)
        {
            if (client.Key != excludeSessionId)
            {
                tasks.Add(SendToClientAsync(client.Key, message));
            }
        }

        await Task.WhenAll(tasks);
    }

    private async Task CleanupDeadClientsAsync(CancellationToken cancellationToken)
    {
        var cleanupInterval = TimeSpan.FromMinutes(5);
        var maxInactivityTime = TimeSpan.FromMinutes(10);

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(cleanupInterval, cancellationToken);

            var deadClients = _clients.Values
                .Where(client => DateTime.UtcNow - client.LastActivity > maxInactivityTime)
                .ToList();

            foreach (var deadClient in deadClients)
            {
                if (_clients.TryRemove(deadClient.SessionId, out var client))
                {
                    Console.WriteLine($"清理空闲客户端: {deadClient.SessionId}");
                    SafeCloseSocket(client.Socket);
                }
            }
        }
    }

    private void SafeCloseSocket(Socket socket)
    {
        if (socket == null) return;

        try
        {
            if (socket.Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
            }
        }
        catch (SocketException)
        {
            // 忽略
        }
        finally
        {
            socket.Close();
            socket.Dispose();
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        
        _isDisposed = true;
        _cancellationTokenSource.Cancel();

        SafeCloseSocket(_listener);

        foreach (var client in _clients.Values)
        {
            SafeCloseSocket(client.Socket);
        }
        _clients.Clear();

        _cancellationTokenSource.Dispose();
        Console.WriteLine("Socket服务器已释放");
    }
}

public class ClientSession
{
    public string SessionId { get; }
    public Socket Socket { get; }
    public DateTime LastActivity { get; private set; }

    public ClientSession(string sessionId, Socket socket)
    {
        SessionId = sessionId;
        Socket = socket;
        LastActivity = DateTime.UtcNow;
    }

    public void UpdateLastActivity()
    {
        LastActivity = DateTime.UtcNow;
    }
}
```

## 2. 高级长连接特性

### 连接状态监控
```csharp
public class ConnectionMonitor
{
    private readonly Timer _monitorTimer;
    private readonly LongLiveSocketClient _client;
    private DateTime _lastSuccessfulCommunication;

    public ConnectionMonitor(LongLiveSocketClient client)
    {
        _client = client;
        _monitorTimer = new Timer(CheckConnectionHealth, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        _lastSuccessfulCommunication = DateTime.UtcNow;

        // 订阅事件
        _client.MessageReceived += OnMessageReceived;
        _client.Connected += OnConnected;
    }

    private void OnMessageReceived(string message)
    {
        _lastSuccessfulCommunication = DateTime.UtcNow;
        
        if (message == "PONG")
        {
            Console.WriteLine("收到心跳响应，连接健康");
        }
    }

    private void OnConnected()
    {
        _lastSuccessfulCommunication = DateTime.UtcNow;
    }

    private void CheckConnectionHealth(object state)
    {
        var timeSinceLastCommunication = DateTime.UtcNow - _lastSuccessfulCommunication;
        
        if (timeSinceLastCommunication > TimeSpan.FromMinutes(2))
        {
            Console.WriteLine($"连接可能已断开，最后通信时间: {_lastSuccessfulCommunication}");
            // 可以触发重连或其他恢复操作
        }
    }

    public void Dispose()
    {
        _monitorTimer?.Dispose();
    }
}
```

### 消息协议处理
```csharp
public class MessageProtocol
{
    public static byte[] EncodeMessage(string message)
    {
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        byte[] lengthBytes = BitConverter.GetBytes(messageBytes.Length);
        
        var result = new byte[lengthBytes.Length + messageBytes.Length];
        Buffer.BlockCopy(lengthBytes, 0, result, 0, lengthBytes.Length);
        Buffer.BlockCopy(messageBytes, 0, result, lengthBytes.Length, messageBytes.Length);
        
        return result;
    }

    public static (string message, int bytesRead) DecodeMessage(byte[] buffer, int offset, int count)
    {
        if (count < 4) return (null, 0); // 长度字段至少4字节
        
        int messageLength = BitConverter.ToInt32(buffer, offset);
        
        if (count < 4 + messageLength) return (null, 0); // 数据不完整
        
        string message = Encoding.UTF8.GetString(buffer, offset + 4, messageLength);
        return (message, 4 + messageLength);
    }
}

// 在接收循环中使用协议
private async Task ReceiveWithProtocolAsync(CancellationToken cancellationToken)
{
    var buffer = new byte[4096];
    var messageBuffer = new List<byte>();
    int bytesInBuffer = 0;

    while (!cancellationToken.IsCancellationRequested && _isConnected)
    {
        try
        {
            int bytesReceived = await _socket.ReceiveAsync(
                new ArraySegment<byte>(buffer, bytesInBuffer, buffer.Length - bytesInBuffer), 
                SocketFlags.None);

            if (bytesReceived == 0)
            {
                await TryReconnectAsync();
                continue;
            }

            bytesInBuffer += bytesReceived;
            int offset = 0;

            while (bytesInBuffer - offset >= 4)
            {
                var (message, bytesRead) = MessageProtocol.DecodeMessage(buffer, offset, bytesInBuffer - offset);
                
                if (message != null)
                {
                    MessageReceived?.Invoke(message);
                    offset += bytesRead;
                }
                else
                {
                    break; // 数据不完整，等待更多数据
                }
            }

            // 移动剩余数据到缓冲区开头
            if (offset > 0)
            {
                Buffer.BlockCopy(buffer, offset, buffer, 0, bytesInBuffer - offset);
                bytesInBuffer -= offset;
            }

            // 如果缓冲区快满了，扩大缓冲区
            if (bytesInBuffer > buffer.Length * 0.8)
            {
                var newBuffer = new byte[buffer.Length * 2];
                Buffer.BlockCopy(buffer, 0, newBuffer, 0, bytesInBuffer);
                buffer = newBuffer;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"接收数据时出错: {ex.Message}");
            await TryReconnectAsync();
        }
    }
}
```

## 3. 使用示例

### 客户端使用
```csharp
public class Program
{
    public static async Task Main()
    {
        using var client = new LongLiveSocketClient("localhost", 8080);
        
        // 订阅事件
        client.MessageReceived += message => 
            Console.WriteLine($"收到: {message}");
            
        client.Connected += () => 
            Console.WriteLine("连接成功");
            
        client.Disconnected += () => 
            Console.WriteLine("连接断开");
            
        client.ErrorOccurred += ex => 
            Console.WriteLine($"错误: {ex.Message}");

        // 连接服务器
        await client.ConnectAsync();

        // 发送消息
        await client.SendAsync("Hello Server!");

        // 保持程序运行
        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
    }
}
```

### 服务器使用
```csharp
public class Program
{
    public static async Task Main()
    {
        using var server = new LongLiveSocketServer(8080);
        
        // 启动服务器
        var serverTask = server.StartAsync();

        Console.WriteLine("服务器运行中...");
        Console.WriteLine("按任意键停止服务器...");
        Console.ReadKey();
    }
}
```

## 4. WebSocket 长连接（现代替代方案）

### 使用 ASP.NET Core WebSocket
```csharp
// Startup.cs 或 Program.cs
public class WebSocketHandler
{
    private static readonly ConcurrentDictionary<string, WebSocket> _sockets = new();

    public static async Task HandleWebSocket(HttpContext context, WebSocket webSocket)
    {
        var socketId = Guid.NewGuid().ToString();
        _sockets.TryAdd(socketId, webSocket);

        try
        {
            var buffer = new byte[1024 * 4];
            
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"收到: {message}");

                    // 广播消息
                    await BroadcastAsync(message, socketId);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, 
                        "Closed by client", CancellationToken.None);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WebSocket错误: {ex.Message}");
        }
        finally
        {
            _sockets.TryRemove(socketId, out _);
            webSocket.Dispose();
        }
    }

    private static async Task BroadcastAsync(string message, string excludeSocketId)
    {
        var tasks = _sockets
            .Where(s => s.Key != excludeSocketId)
            .Select(s => SendAsync(s.Value, message));
            
        await Task.WhenAll(tasks);
    }

    private static async Task SendAsync(WebSocket socket, string message)
    {
        if (socket.State == WebSocketState.Open)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            await socket.SendAsync(new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}

// 在Configure方法中
app.UseWebSockets();
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            await WebSocketHandler.HandleWebSocket(context, webSocket);
        }
        else
        {
            context.Response.StatusCode = 400;
        }
    }
    else
    {
        await next();
    }
});
```

## 5. 最佳实践和注意事项

### 连接保持策略
1. **心跳机制**：定期发送心跳包检测连接状态
2. **TCP KeepAlive**：使用操作系统级别的KeepAlive
3. **应用层心跳**：实现自定义心跳协议处理网络中间件超时

### 错误处理和重连
```csharp
public class RobustConnectionManager
{
    private readonly Func<Task> _connectAction;
    private readonly TimeSpan _initialRetryDelay = TimeSpan.FromSeconds(1);
    private readonly TimeSpan _maxRetryDelay = TimeSpan.FromMinutes(5);
    private int _retryCount = 0;

    public RobustConnectionManager(Func<Task> connectAction)
    {
        _connectAction = connectAction;
    }

    public async Task MaintainConnectionAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _connectAction();
                _retryCount = 0; // 重置重试计数
                
                // 连接成功，等待直到连接断开
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine($"连接失败: {ex.Message}");
                await WaitForRetryAsync(cancellationToken);
            }
        }
    }

    private async Task WaitForRetryAsync(CancellationToken cancellationToken)
    {
        // 指数退避策略
        var delay = TimeSpan.FromTicks(_initialRetryDelay.Ticks * (long)Math.Pow(2, _retryCount));
        delay = TimeSpan.FromTicks(Math.Min(delay.Ticks, _maxRetryDelay.Ticks));
        
        _retryCount++;
        
        Console.WriteLine($"等待 {delay.TotalSeconds} 秒后重试...");
        await Task.Delay(delay, cancellationToken);
    }
}
```

### 性能优化
1. **缓冲区管理**：重用缓冲区避免频繁分配
2. **异步操作**：使用真正的异步Socket操作
3. **连接池**：对于客户端考虑使用连接池
4. **消息批处理**：对于高频消息考虑批处理

## 总结

实现Socket长连接的关键要素：

1. **连接保持**：使用心跳和TCP KeepAlive
2. **错误恢复**：实现自动重连机制
3. **资源管理**：正确释放Socket资源
4. **状态监控**：监控连接健康状态
5. **协议设计**：设计可靠的消息协议

对于现代应用，建议考虑使用WebSocket作为长连接的替代方案，它提供了更好的浏览器兼容性和更简单的API。对于传统的Socket编程，上述模式可以提供稳定可靠的长连接实现。


# CancellationTokenSource 详解

`CancellationTokenSource` 是 C# 中用于取消异步操作的核心类，它提供了取消令牌的创建和管理机制。

## 1. 基本概念和用途

### 什么是 CancellationTokenSource？
```csharp
using System;
using System.Threading;
using System.Threading.Tasks;

public class CancellationTokenSourceBasic
{
    public static void DemonstrateBasics()
    {
        // 创建 CancellationTokenSource
        var cts = new CancellationTokenSource();
        
        // 获取 CancellationToken
        CancellationToken token = cts.Token;
        
        Console.WriteLine($"Token 初始状态: {token.IsCancellationRequested}"); // False
        
        // 触发取消
        cts.Cancel();
        
        Console.WriteLine($"取消后 Token 状态: {token.IsCancellationRequested}"); // True
    }
}
```

## 2. 核心方法和属性

### 主要成员
```csharp
public class CancellationTokenSourceMembers
{
    public static void DemonstrateMembers()
    {
        var cts = new CancellationTokenSource();
        
        // 属性
        Console.WriteLine($"Token: {cts.Token}");
        Console.WriteLine($"IsCancellationRequested: {cts.IsCancellationRequested}");
        
        // 方法
        cts.Cancel();                           // 立即取消
        cts.CancelAfter(TimeSpan.FromSeconds(5)); // 5秒后自动取消
        cts.Dispose();                          // 释放资源
        
        // 使用示例
        var cts2 = new CancellationTokenSource();
        Console.WriteLine($"创建后状态: {cts2.IsCancellationRequested}");
        
        cts2.CancelAfter(1000); // 1秒后取消
        Console.WriteLine($"CancelAfter 后状态: {cts2.IsCancellationRequested}"); // False
        
        Thread.Sleep(1500);
        Console.WriteLine($"1.5秒后状态: {cts2.IsCancellationRequested}"); // True
    }
}
```

## 3. 基本使用模式

### 模式1：简单取消
```csharp
public class SimpleCancellationExample
{
    public static async Task DemonstrateSimpleCancellation()
    {
        var cts = new CancellationTokenSource();
        
        // 启动一个可取消的任务
        var task = LongRunningOperationAsync(cts.Token);
        
        // 3秒后取消
        cts.CancelAfter(TimeSpan.FromSeconds(3));
        
        try
        {
            await task;
            Console.WriteLine("操作完成");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("操作被取消");
        }
        finally
        {
            cts.Dispose();
        }
    }
    
    private static async Task LongRunningOperationAsync(CancellationToken cancellationToken)
    {
        for (int i = 0; i < 10; i++)
        {
            // 检查取消请求
            cancellationToken.ThrowIfCancellationRequested();
            
            Console.WriteLine($"工作进度: {i + 1}/10");
            await Task.Delay(1000, cancellationToken);
        }
    }
}
```

### 模式2：轮询检查取消
```csharp
public class PollingCancellationExample
{
    public static async Task DemonstratePolling()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // 5秒超时
        
        try
        {
            await ProcessDataWithPollingAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("处理因超时被取消");
        }
    }
    
    private static async Task ProcessDataWithPollingAsync(CancellationToken cancellationToken)
    {
        var data = Enumerable.Range(1, 100).ToList();
        
        foreach (var item in data)
        {
            // 方式1：抛出异常
            cancellationToken.ThrowIfCancellationRequested();
            
            // 方式2：检查状态并优雅退出
            if (cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("检测到取消请求，正在优雅退出...");
                return;
            }
            
            // 模拟工作
            await Task.Delay(100);
            Console.WriteLine($"处理项目: {item}");
        }
    }
}
```

## 4. 实际应用场景

### 场景1：Web API 请求取消
```csharp
public class ApiService
{
    private readonly HttpClient _httpClient;
    
    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<string> GetDataAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine("API 请求被用户取消");
            throw;
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("API 请求超时");
            throw;
        }
    }
}

// 在 ASP.NET Core 控制器中使用
[ApiController]
[Route("api/[controller]")]
public class DataController : ControllerBase
{
    private readonly ApiService _apiService;
    
    public DataController(ApiService apiService)
    {
        _apiService = apiService;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetData(CancellationToken cancellationToken)
    {
        try
        {
            // ASP.NET Core 会自动注入取消令牌（用户关闭浏览器等）
            var data = await _apiService.GetDataAsync("https://api.example.com/data", cancellationToken);
            return Ok(data);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499, "客户端关闭请求"); // 499: Client Closed Request
        }
    }
}
```

### 场景2：UI 应用程序中的取消
```csharp
public class MainWindow : Window
{
    private CancellationTokenSource _cancellationTokenSource;
    private readonly Button _startButton;
    private readonly Button _cancelButton;
    private readonly ProgressBar _progressBar;
    
    public MainWindow()
    {
        _startButton = new Button { Content = "开始" };
        _cancelButton = new Button { Content = "取消", IsEnabled = false };
        _progressBar = new ProgressBar();
        
        _startButton.Click += StartButton_Click;
        _cancelButton.Click += CancelButton_Click;
    }
    
    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        _startButton.IsEnabled = false;
        _cancelButton.IsEnabled = true;
        _progressBar.Value = 0;
        
        _cancellationTokenSource = new CancellationTokenSource();
        
        try
        {
            await ProcessFilesAsync(_cancellationTokenSource.Token);
            MessageBox.Show("处理完成!");
        }
        catch (OperationCanceledException)
        {
            MessageBox.Show("操作被用户取消");
        }
        finally
        {
            _startButton.IsEnabled = true;
            _cancelButton.IsEnabled = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }
    
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _cancellationTokenSource?.Cancel();
    }
    
    private async Task ProcessFilesAsync(CancellationToken cancellationToken)
    {
        var files = Directory.GetFiles(@"C:\Data", "*.txt");
        
        for (int i = 0; i < files.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // 处理文件
            await ProcessFileAsync(files[i], cancellationToken);
            
            // 更新进度
            _progressBar.Value = (i + 1) * 100 / files.Length;
            
            // 模拟长时间操作
            await Task.Delay(1000, cancellationToken);
        }
    }
    
    private async Task ProcessFileAsync(string filePath, CancellationToken cancellationToken)
    {
        // 文件处理逻辑
        await Task.Delay(500, cancellationToken);
    }
}
```

### 场景3：后台服务处理
```csharp
public class BackgroundWorkerService : IHostedService, IDisposable
{
    private readonly ILogger<BackgroundWorkerService> _logger;
    private Timer _timer;
    private CancellationTokenSource _stoppingCts;
    
    public BackgroundWorkerService(ILogger<BackgroundWorkerService> logger)
    {
        _logger = logger;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("后台服务启动");
        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        // 每30秒执行一次
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        
        return Task.CompletedTask;
    }
    
    private async void DoWork(object state)
    {
        try
        {
            using var workCts = CancellationTokenSource.CreateLinkedTokenSource(_stoppingCts.Token);
            workCts.CancelAfter(TimeSpan.FromSeconds(25)); // 工作最多运行25秒
            
            await PerformScheduledWorkAsync(workCts.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("后台工作被取消");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "后台工作发生错误");
        }
    }
    
    private async Task PerformScheduledWorkAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始执行计划工作");
        
        // 模拟工作
        for (int i = 0; i < 10; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            _logger.LogInformation("工作进度: {Progress}/10", i + 1);
            await Task.Delay(2000, cancellationToken);
        }
        
        _logger.LogInformation("计划工作完成");
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("后台服务停止");
        _timer?.Change(Timeout.Infinite, 0);
        _stoppingCts?.Cancel();
        
        return Task.CompletedTask;
    }
    
    public void Dispose()
    {
        _timer?.Dispose();
        _stoppingCts?.Dispose();
    }
}
```

## 5. 高级用法

### 链接多个 CancellationToken
```csharp
public class LinkedTokenExample
{
    public static async Task DemonstrateLinkedTokens()
    {
        // 创建多个取消源
        var userCancellation = new CancellationTokenSource();
        var timeoutCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var externalCancellation = new CancellationTokenSource();
        
        // 链接令牌
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            userCancellation.Token,
            timeoutCancellation.Token,
            externalCancellation.Token
        );
        
        var task = PerformCriticalWorkAsync(linkedCts.Token);
        
        // 模拟用户3秒后取消
        _ = Task.Run(async () =>
        {
            await Task.Delay(3000);
            userCancellation.Cancel();
            Console.WriteLine("用户请求取消");
        });
        
        try
        {
            await task;
            Console.WriteLine("工作完成");
        }
        catch (OperationCanceledException ex)
        {
            Console.WriteLine($"操作被取消: {ex.Message}");
            
            // 检查是哪个令牌触发的取消
            if (userCancellation.Token.IsCancellationRequested)
                Console.WriteLine("原因: 用户取消");
            if (timeoutCancellation.Token.IsCancellationRequested)
                Console.WriteLine("原因: 超时");
            if (externalCancellation.Token.IsCancellationRequested)
                Console.WriteLine("原因: 外部取消");
        }
        finally
        {
            userCancellation.Dispose();
            timeoutCancellation.Dispose();
            externalCancellation.Dispose();
        }
    }
    
    private static async Task PerformCriticalWorkAsync(CancellationToken cancellationToken)
    {
        for (int i = 0; i < 20; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            Console.WriteLine($"关键工作进度: {i + 1}/20");
            await Task.Delay(1000, cancellationToken);
        }
    }
}
```

### 自定义取消回调
```csharp
public class CancellationCallbackExample
{
    public static void DemonstrateCallbacks()
    {
        var cts = new CancellationTokenSource();
        
        // 注册取消回调
        cts.Token.Register(() =>
        {
            Console.WriteLine("取消回调执行: 正在清理资源...");
            CleanupResources();
        });
        
        // 注册多个回调
        cts.Token.Register(() => Console.WriteLine("另一个取消回调"));
        
        Console.WriteLine("启动工作...");
        
        // 在另一个线程中取消
        _ = Task.Run(() =>
        {
            Thread.Sleep(2000);
            Console.WriteLine("触发取消...");
            cts.Cancel();
        });
        
        // 模拟工作
        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                Console.WriteLine("工作中...");
                Thread.Sleep(500);
            }
        }
        finally
        {
            cts.Dispose();
        }
    }
    
    private static void CleanupResources()
    {
        Console.WriteLine("资源清理完成");
    }
}
```

### 超时和重试模式
```csharp
public class RetryWithTimeoutPattern
{
    public static async Task<T> ExecuteWithRetryAndTimeoutAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        int maxRetries = 3,
        TimeSpan timeout = default)
    {
        if (timeout == default)
            timeout = TimeSpan.FromSeconds(30);
        
        for (int retry = 0; retry < maxRetries; retry++)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token);
            
            try
            {
                Console.WriteLine($"尝试 {retry + 1}/{maxRetries}");
                return await operation(linkedCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
            {
                Console.WriteLine($"尝试 {retry + 1} 超时");
                if (retry == maxRetries - 1) throw new TimeoutException("操作超时");
            }
            catch (Exception ex) when (retry < maxRetries - 1)
            {
                Console.WriteLine($"尝试 {retry + 1} 失败: {ex.Message}");
                
                // 指数退避
                var delay = TimeSpan.FromSeconds(Math.Pow(2, retry));
                await Task.Delay(delay);
            }
        }
        
        throw new InvalidOperationException("所有重试都失败了");
    }
    
    public static async Task DemonstrateRetryPattern()
    {
        try
        {
            var result = await ExecuteWithRetryAndTimeoutAsync(
                async token =>
                {
                    // 模拟不可靠的操作
                    await Task.Delay(1000, token);
                    
                    if (Random.Shared.Next(3) == 0) // 33% 失败率
                        throw new Exception("模拟失败");
                    
                    return "操作成功";
                },
                maxRetries: 3,
                timeout: TimeSpan.FromSeconds(5)
            );
            
            Console.WriteLine($"最终结果: {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"最终失败: {ex.Message}");
        }
    }
}
```

## 6. 最佳实践和陷阱

### 正确使用模式
```csharp
public class BestPractices
{
    // ✅ 正确：使用 using 语句确保资源释放
    public static async Task ProperResourceManagementAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        
        try
        {
            await LongRunningOperationAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // 正常处理取消
        }
        // 不需要手动调用 Dispose，using 会自动处理
    }
    
    // ✅ 正确：传递 CancellationToken 给支持的方法
    public static async Task ProperTokenPassingAsync(CancellationToken cancellationToken = default)
    {
        // 传递给 Task.Delay
        await Task.Delay(1000, cancellationToken);
        
        // 传递给 HttpClient
        using var httpClient = new HttpClient();
        await httpClient.GetAsync("https://example.com", cancellationToken);
        
        // 传递给 Entity Framework
        // await dbContext.Users.ToListAsync(cancellationToken);
    }
    
    // ❌ 错误：忽略 CancellationToken
    public static async Task BadPracticeIgnoreTokenAsync(CancellationToken cancellationToken)
    {
        // 错误：没有检查取消令牌
        await Task.Delay(5000); // 应该使用 await Task.Delay(5000, cancellationToken);
        
        // 错误：没有传递令牌
        using var httpClient = new HttpClient();
        await httpClient.GetAsync("https://example.com"); // 应该传递 cancellationToken
    }
    
    // ❌ 错误：捕获异常后继续执行
    public static async Task BadPracticeSwallowCancellationAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                await DoWorkAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // 错误：没有重新抛出或退出循环
            Console.WriteLine("取消被忽略，继续执行"); // 这很危险！
        }
    }
    
    // ✅ 正确：适当处理取消
    public static async Task ProperCancellationHandlingAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await DoWorkAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // 记录日志或执行清理，但不忽略取消
            Console.WriteLine("操作被取消，正在退出");
            throw; // 重新抛出或优雅退出
        }
    }
    
    private static async Task DoWorkAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(1000, cancellationToken);
    }
    
    private static async Task<string> LongRunningOperationAsync(CancellationToken cancellationToken)
    {
        for (int i = 0; i < 10; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(1000, cancellationToken);
        }
        return "完成";
    }
}
```

### 性能考虑
```csharp
public class PerformanceConsiderations
{
    // ✅ 对于高频操作，考虑重用 CancellationTokenSource
    private static readonly CancellationTokenSource _longLivedCts = new CancellationTokenSource();
    
    public static async Task UseLongLivedTokenAsync()
    {
        // 对于应用程序生命周期的取消令牌
        await ProcessAsync(_longLivedCts.Token);
    }
    
    // ✅ 对于短期操作，使用 using 模式
    public static async Task UseShortLivedTokenAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await ProcessAsync(cts.Token);
    }
    
    // ❌ 避免频繁创建和销毁
    public static async Task BadPerformancePatternAsync()
    {
        for (int i = 0; i < 1000; i++)
        {
            using var cts = new CancellationTokenSource(); // 频繁创建/销毁
            await ProcessAsync(cts.Token);
        }
    }
    
    private static async Task ProcessAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
    }
}
```

## 7. 常见问题解答

### Q: 什么时候应该使用 CancellationTokenSource？
**A:** 当需要：
- 取消长时间运行的操作
- 实现操作超时
- 响应外部取消请求（如用户点击取消按钮）
- 协调多个相关操作的取消

### Q: CancellationToken 和 CancellationTokenSource 的区别？
**A:** 
- **CancellationTokenSource**: 产生取消信号的源
- **CancellationToken**: 传播取消信号的令牌，只能读取不能修改

### Q: 为什么需要链接令牌？
**A:** 链接令牌允许将多个取消条件组合在一起，当任何一个源取消时，整个操作都会取消。

## 总结

**CancellationTokenSource 核心要点：**

1. **创建取消信号**：通过 `new CancellationTokenSource()` 创建
2. **触发取消**：使用 `Cancel()` 或 `CancelAfter()`
3. **获取令牌**：通过 `Token` 属性获取 `CancellationToken`
4. **资源管理**：实现了 `IDisposable`，应该适当释放
5. **链接功能**：可以组合多个取消令牌

**最佳实践：**
- 总是使用 `using` 语句或适当调用 `Dispose()`
- 在异步方法中接受 `CancellationToken` 参数
- 定期检查 `IsCancellationRequested` 或调用 `ThrowIfCancellationRequested()`
- 将令牌传递给支持取消的框架方法

`CancellationTokenSource` 是现代 C# 异步编程的基石，正确使用可以大大提高应用程序的响应性和可靠性。