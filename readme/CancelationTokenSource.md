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

# C# Task ContinueWith 详解

`Task.ContinueWith` 是 C# 中用于创建**任务延续**的方法，它允许在一个任务完成后自动开始执行另一个任务。这是实现异步工作流的重要机制。

## 基本概念

### 1. 什么是 ContinueWith？

`ContinueWith` 创建一个在目标 `Task` 完成时异步执行的延续任务：

```csharp
Task originalTask = Task.Run(() => DoWork());
Task continuationTask = originalTask.ContinueWith(previousTask => 
{
    // 当 originalTask 完成后，这里会自动执行
    Console.WriteLine("前一个任务完成了！");
});
```

## 基本语法和用法

### 1. 基本形式

```csharp
// 无返回值的延续
Task ContinueWith(Action<Task> continuationAction)

// 有返回值的延续
Task<TResult> ContinueWith<TResult>(Func<Task, TResult> continuationFunction)

// 带任务选项的延续
Task ContinueWith(Action<Task> continuationAction, TaskContinuationOptions continuationOptions)
```

### 2. 简单示例

```csharp
// 示例1：基本延续
Task.Run(() => 
{
    Console.WriteLine("第一个任务");
    Thread.Sleep(1000);
})
.ContinueWith(previousTask => 
{
    Console.WriteLine("第二个任务（在第一个完成后执行）");
});

// 示例2：访问前一个任务的结果
Task<int> calculationTask = Task.Run(() => 42 * 2);
Task<string> resultTask = calculationTask.ContinueWith(prev => 
{
    int result = prev.Result; // 获取前一个任务的结果
    return $"结果是: {result}";
});

Console.WriteLine(await resultTask); // 输出: "结果是: 84"
```

## 在我们的 Socket 客户端中的应用

回顾我们之前代码中的使用：

```csharp
// 当任务完成时取消完成令牌
tcs.Task.ContinueWith(_ => completionCts.Cancel(), 
    TaskContinuationOptions.ExecuteSynchronously);
```

这个用法实现了：
- 当 `tcs.Task` 完成时（无论成功、失败还是取消）
- 自动调用 `completionCts.Cancel()`
- 使用 `ExecuteSynchronously` 选项提高性能

## 重要参数和选项

### 1. TaskContinuationOptions

这个枚举参数控制延续任务的执行行为：

```csharp
// 常用选项示例
task.ContinueWith(prev => 
{
    // 只在原任务成功完成时执行
}, TaskContinuationOptions.OnlyOnRanToCompletion);

task.ContinueWith(prev => 
{
    // 只在原任务失败时执行
}, TaskContinuationOptions.OnlyOnFaulted);

task.ContinueWith(prev => 
{
    // 只在原任务取消时执行
}, TaskContinuationOptions.OnlyOnCanceled);

task.ContinueWith(prev => 
{
    // 尝试同步执行（性能优化）
}, TaskContinuationOptions.ExecuteSynchronously);
```

### 2. 完整的选项列表

```csharp
[Flags]
public enum TaskContinuationOptions
{
    None = 0,
    PreferFairness = 1,
    LongRunning = 2,
    AttachedToParent = 4,
    DenyChildAttach = 8,
    HideScheduler = 16,
    LazyCancellation = 32,
    RunContinuationsAsynchronously = 64,
    NotOnRanToCompletion = 65536,
    NotOnFaulted = 131072,
    NotOnCanceled = 262144,
    OnlyOnRanToCompletion = NotOnFaulted | NotOnCanceled,        // 393216
    OnlyOnFaulted = NotOnRanToCompletion | NotOnCanceled,       // 327680
    OnlyOnCanceled = NotOnRanToCompletion | NotOnFaulted        // 196608
}
```

## 实际应用场景

### 场景1：错误处理链

```csharp
public async Task ProcessDataAsync()
{
    await Task.Run(() => RiskyOperation())
        .ContinueWith(prev =>
        {
            if (prev.IsFaulted)
            {
                Console.WriteLine($"操作失败: {prev.Exception?.Message}");
                // 执行错误恢复逻辑
                return RecoveryOperation();
            }
            return prev.Result;
        })
        .ContinueWith(prev =>
        {
            // 无论成功还是恢复成功，都执行清理
            Console.WriteLine("执行清理操作");
        });
}
```

### 场景2：条件性工作流

```csharp
public Task<string> DownloadAndProcessAsync(string url)
{
    return Task.Run(() => DownloadString(url))
        .ContinueWith(downloadTask =>
        {
            if (downloadTask.IsCompletedSuccessfully)
            {
                string data = downloadTask.Result;
                return ProcessData(data);
            }
            throw downloadTask.Exception!;
        }, TaskContinuationOptions.OnlyOnRanToCompletion)
        .ContinueWith(processTask =>
        {
            if (processTask.IsFaulted)
            {
                // 提供备用数据
                return "备用数据";
            }
            return processTask.Result;
        }, TaskContinuationOptions.OnlyOnFaulted);
}
```

### 场景3：资源清理

```csharp
public Task UseResourceAsync()
{
    var resource = new ExpensiveResource();
    
    return Task.Run(() => resource.DoWork())
        .ContinueWith(_ => 
        {
            // 确保资源被清理，无论任务成功还是失败
            resource.Dispose();
        }, TaskContinuationOptions.ExecuteSynchronously);
}
```

## 与 async/await 的对比

### 使用 ContinueWith
```csharp
Task<int> task = Task.Run(() => 42);
Task<string> result = task.ContinueWith(prev => 
{
    return $"结果是: {prev.Result}";
});
```

### 使用 async/await
```csharp
async Task<string> GetResultAsync()
{
    int value = await Task.Run(() => 42);
    return $"结果是: {value}";
}
```

**主要区别**：
- `async/await` 代码更清晰，更像同步代码
- `ContinueWith` 提供更细粒度的控制
- `ContinueWith` 可以指定特定的完成状态条件

## 最佳实践和注意事项

### 1. 异常处理

```csharp
// 不好的做法：可能吞掉异常
task.ContinueWith(prev => 
{
    Console.WriteLine(prev.Result); // 如果prev失败，这里会抛出异常
});

// 好的做法：明确处理异常
task.ContinueWith(prev =>
{
    if (prev.IsFaulted)
    {
        Console.WriteLine($"错误: {prev.Exception.Message}");
        return;
    }
    if (prev.IsCanceled)
    {
        Console.WriteLine("任务被取消");
        return;
    }
    Console.WriteLine(prev.Result);
});
```

### 2. 避免嵌套过深

```csharp
// 不好的做法：回调地狱
task.ContinueWith(t1 => 
{
    // ...
    return t2;
}).ContinueWith(t2 => 
{
    // ...
    return t3;
}).ContinueWith(t3 => 
{
    // 难以维护
});

// 好的做法：使用 async/await 或分解方法
public async Task CleanWorkflowAsync()
{
    var result1 = await Step1Async();
    var result2 = await Step2Async(result1);
    return await Step3Async(result2);
}
```

### 3. 注意任务调度器

```csharp
// 指定在UI线程上执行延续
task.ContinueWith(prev =>
{
    // 更新UI
    textBox.Text = prev.Result;
}, TaskScheduler.FromCurrentSynchronizationContext());
```

## 在我们 Socket 客户端中的具体作用

### 实现智能取消机制

```csharp
// 创建完成监控
using var completionCts = new CancellationTokenSource();

// 当任务完成时，自动取消监控令牌
tcs.Task.ContinueWith(_ => completionCts.Cancel(), 
    TaskContinuationOptions.ExecuteSynchronously);

// 使用链接令牌同时监控超时和完成
using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
    timeoutCts.Token, 
    completionCts.Token
);
```

**这个模式的优势**：
1. **自动清理**：任务完成时自动取消监控，避免资源泄漏
2. **性能优化**：使用 `ExecuteSynchronously` 减少上下文切换
3. **可靠性**：无论任务如何完成（成功、失败、取消），都会执行清理

## 总结

`Task.ContinueWith` 是一个强大的工具，主要用于：

1. **创建任务链**：定义任务完成后的下一步操作
2. **条件性执行**：基于前一个任务的状态决定是否执行
3. **错误处理**：为特定类型的完成状态提供处理逻辑
4. **资源管理**：确保资源被正确清理
5. **工作流控制**：构建复杂的异步操作序列

**关键要点**：
- 提供比 `async/await` 更细粒度的控制
- 适合需要基于任务状态进行条件处理的场景
- 需要注意异常处理和避免过深的嵌套
- 在现代 C# 中，许多场景可以用 `async/await` 替代，但某些高级场景仍需 `ContinueWith`

在我们的 Socket 客户端中，`ContinueWith` 优雅地实现了"任务完成时自动取消监控"的智能模式，展示了其在资源管理和条件执行方面的价值。

# 关于未使用返回值的 ContinueWith 执行问题

**答案是：会执行！** 这是一个很重要的理解点。

## 核心解释

### 1. Task 的"火并忘记"（Fire and Forget）特性

在 C# 中，当你调用 `Task.ContinueWith` 但不保存返回值时，这个延续任务**仍然会被创建和执行**。这被称为"火并忘记"模式。

```csharp
// 这行代码会执行，即使我们没有保存返回值
tcs.Task.ContinueWith(_ => completionCts.Cancel(), 
    TaskContinuationOptions.ExecuteSynchronously);

// 等同于：
Task continuationTask = tcs.Task.ContinueWith(_ => completionCts.Cancel(), 
    TaskContinuationOptions.ExecuteSynchronously);
// 只是我们没有使用 continuationTask 变量
```

### 2. 为什么会执行？

- **任务调度器负责执行**：当你调用 `ContinueWith` 时，任务会被注册到任务调度器中
- **自动生命周期管理**：.NET 的任务调度器会自动管理这些任务的执行，不需要我们手动"启动"
- **垃圾回收不影响执行**：即使没有引用，任务仍然会执行完成

## 实际验证

让我们通过一个简单的例子来验证：

```csharp
public class ContinueWithTest
{
    private static int executionCount = 0;
    
    public static void TestFireAndForget()
    {
        var tcs = new TaskCompletionSource<string>();
        
        // 不保存返回值的 ContinueWith
        tcs.Task.ContinueWith(prev => 
        {
            Interlocked.Increment(ref executionCount);
            Console.WriteLine($"延续任务执行了! 计数: {executionCount}");
        });
        
        // 启动原始任务
        Task.Run(() =>
        {
            Thread.Sleep(1000);
            tcs.SetResult("完成");
        });
        
        // 等待一会儿让延续任务有机会执行
        Thread.Sleep(2000);
        
        Console.WriteLine($"最终执行计数: {executionCount}"); // 输出: 最终执行计数: 1
    }
}
```

## 在我们代码中的具体分析

回顾我们的代码：

```csharp
// 这行代码一定会执行
tcs.Task.ContinueWith(_ => completionCts.Cancel(), 
    TaskContinuationOptions.ExecuteSynchronously);
```

**执行流程**：
1. 创建 `tcs.Task`（一个未完成的任务）
2. 调用 `ContinueWith` 注册延续
3. 延续任务被添加到任务调度器
4. 当 `tcs.Task` 完成时（无论成功、失败、取消）
5. 调度器自动执行延续任务 → 调用 `completionCts.Cancel()`

## 潜在问题和最佳实践

### 1. 异常处理问题

**问题代码**：
```csharp
// 如果延续任务中抛出异常，我们无法处理
tcs.Task.ContinueWith(_ => 
{
    throw new InvalidOperationException("意外错误!");
    completionCts.Cancel(); // 这行不会执行
});
```

**解决方案**：
```csharp
// 方案1：在延续内部处理异常
tcs.Task.ContinueWith(_ => 
{
    try
    {
        completionCts.Cancel();
    }
    catch (Exception ex)
    {
        // 记录日志或处理异常
        Logger.Error(ex, "取消操作失败");
    }
});

// 方案2：保存返回值以便观察异常
var continuation = tcs.Task.ContinueWith(_ => completionCts.Cancel());
// 可以在适当的地方检查 continuation.Exception
```

### 2. 资源泄漏风险

虽然任务会执行，但如果不保存返回值，我们无法知道它何时完成：

```csharp
// 我们不知道这个任务什么时候完成
tcs.Task.ContinueWith(_ => 
{
    using var expensiveResource = new ExpensiveResource();
    completionCts.Cancel();
    // expensiveResource 会在任务完成后被释放
    // 但我们无法控制释放时机
});
```

### 3. 在我们场景中的合理性

在我们的 Socket 客户端代码中，这种用法是合理的：

```csharp
tcs.Task.ContinueWith(_ => completionCts.Cancel(), 
    TaskContinuationOptions.ExecuteSynchronously);
```

**为什么合理**：
1. **操作简单**：只是调用 `Cancel()`，不会失败
2. **资源轻量**：没有需要手动管理的重资源
3. **可靠性高**：`CancellationTokenSource.Cancel()` 是线程安全的
4. **性能考虑**：使用 `ExecuteSynchronously` 避免额外调度

## 改进建议

虽然当前代码可以工作，但更健壮的做法是：

```csharp
// 方案1：添加错误处理
tcs.Task.ContinueWith(previousTask =>
{
    try
    {
        completionCts.Cancel();
    }
    catch (Exception ex)
    {
        // 在关键系统中记录错误
        LogError("Failed to cancel completion token", ex);
    }
}, TaskContinuationOptions.ExecuteSynchronously);

// 方案2：如果需要确保执行，可以保存引用（但不一定需要等待）
var cleanupTask = tcs.Task.ContinueWith(_ => 
{
    completionCts.Cancel();
}, TaskContinuationOptions.ExecuteSynchronously);

// 在 Dispose 中可以选择等待清理完成
public void Dispose()
{
    // ... 其他清理代码 ...
    cleanupTask?.Wait(1000); // 最多等待1秒
}
```

## 总结

**关键结论**：
1. ✅ **会执行**：不保存返回值的 `ContinueWith` 仍然会执行
2. ✅ **自动管理**：.NET 任务调度器负责执行生命周期
3. ⚠️ **异常风险**：延续中的异常可能被忽略
4. ⚠️ **控制力弱**：无法知道任务何时完成或是否成功

**在我们的代码中**：
- 当前用法是合理的，因为操作简单可靠
- 对于生产环境，建议添加基本的错误处理
- 这种模式适合轻量级的清理操作

这种"火并忘记"模式在简单的清理和通知场景中很有用，但在需要可靠性的关键操作中应该谨慎使用。