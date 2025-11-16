# Socket 非托管资源释放完整指南

在 C# 中，Socket 是非托管资源，需要正确释放以避免内存泄漏和资源耗尽。以下是完整的释放方案：

## 1. 基本释放模式

### 使用 `using` 语句（最简单）
```csharp
using System;
using System.Net;
using System.Net.Sockets;

public class BasicSocketExample
{
    public void UseSocketWithUsing()
    {
        // 使用 using 语句自动释放
        using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
        {
            socket.Connect("www.example.com", 80);
            // 使用 socket...
            Console.WriteLine("Socket 已连接并正在使用");
        } // 这里会自动调用 socket.Dispose()
        
        Console.WriteLine("Socket 已自动释放");
    }
}
```

### 使用 `try-finally` 手动释放
```csharp
public void UseSocketWithTryFinally()
{
    Socket socket = null;
    try
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Connect("www.example.com", 80);
        // 使用 socket...
        Console.WriteLine("Socket 已连接并正在使用");
    }
    finally
    {
        socket?.Close(); // 或者 socket?.Dispose()
        Console.WriteLine("Socket 已在 finally 块中释放");
    }
}
```

## 2. 实现 `IDisposable` 模式

### 简单的 Socket 包装类
```csharp
using System;
using System.Net.Sockets;

public class SocketWrapper : IDisposable
{
    private Socket _socket;
    private bool _disposed = false;

    public SocketWrapper(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
    {
        _socket = new Socket(addressFamily, socketType, protocolType);
    }

    public void Connect(string host, int port)
    {
        ThrowIfDisposed();
        _socket.Connect(host, port);
    }

    public int Send(byte[] buffer)
    {
        ThrowIfDisposed();
        return _socket.Send(buffer);
    }

    public int Receive(byte[] buffer)
    {
        ThrowIfDisposed();
        return _socket.Receive(buffer);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SocketWrapper));
    }

    // 实现 IDisposable
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // 释放托管资源
            }

            // 释放非托管资源 (Socket)
            if (_socket != null)
            {
                try
                {
                    // 先优雅关闭连接
                    if (_socket.Connected)
                    {
                        _socket.Shutdown(SocketShutdown.Both);
                    }
                }
                catch (SocketException)
                {
                    // 忽略关闭时的异常
                }
                finally
                {
                    _socket.Close();
                    _socket.Dispose();
                    _socket = null;
                }
            }

            _disposed = true;
        }
    }

    // 析构函数（备用清理）
    ~SocketWrapper()
    {
        Dispose(false);
    }
}

// 使用示例
public class Program
{
    public static void Main()
    {
        using (var socketWrapper = new SocketWrapper(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
        {
            socketWrapper.Connect("www.example.com", 80);
            // 使用 socketWrapper...
            Console.WriteLine("使用 SocketWrapper...");
        } // 自动调用 Dispose()
    }
}
```

## 3. 异步 Socket 的释放

### 异步 Socket 包装类
```csharp
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class AsyncSocketWrapper : IAsyncDisposable, IDisposable
{
    private Socket _socket;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private bool _disposed = false;

    public AsyncSocketWrapper(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
    {
        _socket = new Socket(addressFamily, socketType, protocolType);
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public async Task ConnectAsync(string host, int port)
    {
        ThrowIfDisposed();
        await _socket.ConnectAsync(host, port, _cancellationTokenSource.Token);
    }

    public async Task<int> SendAsync(byte[] buffer)
    {
        ThrowIfDisposed();
        return await _socket.SendAsync(buffer, SocketFlags.None, _cancellationTokenSource.Token);
    }

    public async Task<int> ReceiveAsync(byte[] buffer)
    {
        ThrowIfDisposed();
        return await _socket.ReceiveAsync(buffer, SocketFlags.None, _cancellationTokenSource.Token);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AsyncSocketWrapper));
    }

    // 同步 Dispose
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    // 异步 Dispose
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            // 取消所有进行中的操作
            _cancellationTokenSource.Cancel();

            if (_socket != null)
            {
                try
                {
                    // 优雅关闭
                    if (_socket.Connected)
                    {
                        _socket.Shutdown(SocketShutdown.Both);
                        
                        // 等待数据发送完成或超时
                        await Task.WhenAny(
                            Task.Delay(TimeSpan.FromSeconds(5)),
                            WaitForSendCompletionAsync()
                        );
                    }
                }
                catch (Exception ex) when (ex is SocketException or ObjectDisposedException)
                {
                    // 忽略关闭过程中的异常
                }
                finally
                {
                    _socket.Close();
                    _socket.Dispose();
                    _socket = null;
                }
            }

            _cancellationTokenSource.Dispose();
            _disposed = true;
        }
        
        GC.SuppressFinalize(this);
    }

    private async Task WaitForSendCompletionAsync()
    {
        // 检查发送缓冲区是否为空
        while (_socket.Connected && _socket.Available > 0)
        {
            await Task.Delay(100);
        }
    }

    ~AsyncSocketWrapper()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}

// 使用示例
public class Program
{
    public static async Task Main()
    {
        await using (var asyncSocket = new AsyncSocketWrapper(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
        {
            await asyncSocket.ConnectAsync("www.example.com", 80);
            // 使用异步 Socket...
            Console.WriteLine("使用异步 SocketWrapper...");
        } // 自动调用 DisposeAsync()
    }
}
```

## 4. Socket 服务器资源管理

### Socket 服务器类
```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class SocketServer : IAsyncDisposable
{
    private Socket _listener;
    private readonly List<Socket> _connectedClients;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private bool _disposed = false;

    public SocketServer(IPAddress ipAddress, int port)
    {
        _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _listener.Bind(new IPEndPoint(ipAddress, port));
        _listener.Listen(100);
        
        _connectedClients = new List<Socket>();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public async Task StartAsync()
    {
        Console.WriteLine("服务器启动...");
        
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                var clientSocket = await _listener.AcceptAsync(_cancellationTokenSource.Token);
                _connectedClients.Add(clientSocket);
                
                // 处理客户端连接
                _ = Task.Run(() => HandleClientAsync(clientSocket, _cancellationTokenSource.Token));
            }
            catch (OperationCanceledException)
            {
                // 服务器停止时正常退出
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"接受连接时出错: {ex.Message}");
            }
        }
    }

    private async Task HandleClientAsync(Socket clientSocket, CancellationToken cancellationToken)
    {
        try
        {
            var buffer = new byte[1024];
            
            while (clientSocket.Connected && !cancellationToken.IsCancellationRequested)
            {
                var bytesReceived = await clientSocket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken);
                if (bytesReceived == 0)
                    break; // 客户端断开连接
                    
                // 处理接收到的数据
                var message = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                Console.WriteLine($"收到消息: {message}");
                
                // 回显消息
                await clientSocket.SendAsync(buffer, 0, bytesReceived, SocketFlags.None, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException)
        {
            // 正常处理连接断开或取消
        }
        finally
        {
            // 从连接列表中移除并关闭
            _connectedClients.Remove(clientSocket);
            SafeCloseSocket(clientSocket);
        }
    }

    private void SafeCloseSocket(Socket socket)
    {
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

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _cancellationTokenSource.Cancel();

            // 关闭所有客户端连接
            foreach (var client in _connectedClients.ToArray()) // 使用 ToArray 避免修改集合
            {
                SafeCloseSocket(client);
            }
            _connectedClients.Clear();

            // 关闭监听器
            if (_listener != null)
            {
                try
                {
                    if (_listener.IsBound)
                    {
                        _listener.Close();
                    }
                }
                finally
                {
                    _listener.Dispose();
                    _listener = null;
                }
            }

            _cancellationTokenSource.Dispose();
            _disposed = true;
        }
        
        GC.SuppressFinalize(this);
    }
}

// 使用示例
public class Program
{
    public static async Task Main()
    {
        var server = new SocketServer(IPAddress.Any, 8080);
        
        try
        {
            var serverTask = server.StartAsync();
            
            // 运行一段时间后停止
            await Task.Delay(TimeSpan.FromSeconds(30));
            Console.WriteLine("停止服务器...");
        }
        finally
        {
            await server.DisposeAsync();
        }
    }
}
```

## 5. 高级资源管理

### 使用 `SafeHandle` 包装 Socket
```csharp
using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

public class SafeSocketHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private Socket _socket;

    public SafeSocketHandle(Socket socket) : base(true)
    {
        _socket = socket;
        
        // 获取 Socket 的底层句柄
        var handle = _socket.Handle;
        SetHandle(handle);
    }

    protected override bool ReleaseHandle()
    {
        if (_socket != null)
        {
            try
            {
                if (_socket.Connected)
                {
                    _socket.Shutdown(SocketShutdown.Both);
                }
            }
            catch (SocketException)
            {
                // 忽略关闭异常
            }
            finally
            {
                _socket.Close();
                _socket.Dispose();
                _socket = null;
            }
        }
        
        return true;
    }
}

public class SafeSocket : IDisposable
{
    private readonly SafeSocketHandle _handle;
    private bool _disposed = false;

    public SafeSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
    {
        var socket = new Socket(addressFamily, socketType, protocolType);
        _handle = new SafeSocketHandle(socket);
    }

    public void Connect(string host, int port)
    {
        ThrowIfDisposed();
        
        // 通过句柄获取 Socket（实际使用时需要更复杂的处理）
        // 这里简化处理，实际应该保存 Socket 引用或重新创建
        using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
        {
            socket.Connect(host, port);
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SafeSocket));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _handle?.Dispose();
            _disposed = true;
        }
    }
}
```

## 6. 异常处理和最佳实践

### 完整的异常处理模式
```csharp
using System;
using System.Net.Sockets;

public class RobustSocketClient : IDisposable
{
    private Socket _socket;
    private bool _disposed = false;

    public void ConnectWithRetry(string host, int port, int maxRetries = 3)
    {
        ThrowIfDisposed();
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                
                // 设置超时
                _socket.SendTimeout = 5000;
                _socket.ReceiveTimeout = 5000;
                
                _socket.Connect(host, port);
                Console.WriteLine("连接成功");
                return;
            }
            catch (SocketException ex) when (attempt < maxRetries)
            {
                Console.WriteLine($"连接尝试 {attempt} 失败: {ex.Message}");
                CleanupSocket();
                
                // 等待后重试
                System.Threading.Thread.Sleep(1000 * attempt);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"连接失败: {ex.Message}");
                CleanupSocket();
                throw;
            }
        }
        
        throw new InvalidOperationException($"无法连接到 {host}:{port}，重试 {maxRetries} 次后失败");
    }

    public void SendData(byte[] data)
    {
        ThrowIfDisposed();
        
        try
        {
            int totalSent = 0;
            while (totalSent < data.Length)
            {
                int sent = _socket.Send(data, totalSent, data.Length - totalSent, SocketFlags.None);
                if (sent == 0)
                    throw new SocketException((int)SocketError.ConnectionReset);
                
                totalSent += sent;
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"发送数据时出错: {ex.Message}");
            CleanupSocket();
            throw;
        }
    }

    private void CleanupSocket()
    {
        if (_socket != null)
        {
            try
            {
                if (_socket.Connected)
                {
                    _socket.Shutdown(SocketShutdown.Both);
                }
            }
            catch (SocketException)
            {
                // 忽略关闭异常
            }
            finally
            {
                _socket.Close();
                _socket.Dispose();
                _socket = null;
            }
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RobustSocketClient));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            CleanupSocket();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    ~RobustSocketClient()
    {
        Dispose();
    }
}
```

## 7. 最佳实践总结

### 释放顺序和步骤
1. **优雅关闭**：先调用 `Shutdown(SocketShutdown.Both)`
2. **关闭连接**：调用 `Close()`
3. **释放资源**：调用 `Dispose()`

### 关键要点
- **总是使用 `using` 或 `try-finally`** 确保资源释放
- **处理异常**：在释放时忽略 `SocketException` 和 `ObjectDisposedException`
- **优雅关闭**：先 shutdown 再 close
- **异步支持**：实现 `IAsyncDisposable` 用于异步场景
- **取消支持**：使用 `CancellationToken` 取消长时间操作
- **连接池**：对于频繁创建的场景考虑连接池

### 常见错误避免
```csharp
// ❌ 错误：忘记释放
var socket = new Socket(...);
socket.Connect(...);
// 忘记调用 socket.Close() 或 socket.Dispose()

// ❌ 错误：重复释放
socket.Dispose();
socket.Dispose(); // 可能抛出异常

// ✅ 正确：使用 using 语句
using (var socket = new Socket(...))
{
    socket.Connect(...);
}

// ✅ 正确：空值检查和重复释放保护
if (socket != null)
{
    try
    {
        if (socket.Connected)
            socket.Shutdown(SocketShutdown.Both);
    }
    catch (SocketException)
    {
        // 忽略
    }
    finally
    {
        socket.Close();
        socket.Dispose();
        socket = null; // 防止重复使用
    }
}
```

通过遵循这些模式，你可以确保 Socket 非托管资源得到正确释放，避免内存泄漏和资源耗尽问题。