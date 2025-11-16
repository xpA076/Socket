# C# 中类似 StringBuilder 的字节数组拼接工具

在 C# 中，确实有几种类似 `StringBuilder` 的数据结构可以用来高效拼接 `byte[]` 数组。以下是主要的几种方法：

## 1. 使用 MemoryStream（最推荐）

```csharp
public class ByteArrayBuilder
{
    private readonly MemoryStream _memoryStream;
    
    public ByteArrayBuilder(int initialCapacity = 1024)
    {
        _memoryStream = new MemoryStream(initialCapacity);
    }
    
    public void Append(byte[] data)
    {
        _memoryStream.Write(data, 0, data.Length);
    }
    
    public void Append(byte[] data, int offset, int count)
    {
        _memoryStream.Write(data, offset, count);
    }
    
    public void Append(byte value)
    {
        _memoryStream.WriteByte(value);
    }
    
    public byte[] ToArray()
    {
        return _memoryStream.ToArray();
    }
    
    public long Length => _memoryStream.Length;
    
    public void Clear()
    {
        _memoryStream.SetLength(0);
    }
    
    public void Dispose()
    {
        _memoryStream?.Dispose();
    }
}

// 使用示例
using (var builder = new ByteArrayBuilder())
{
    builder.Append(new byte[] { 1, 2, 3 });
    builder.Append(new byte[] { 4, 5, 6 });
    builder.Append(new byte[] { 7, 8, 9 });
    
    byte[] result = builder.ToArray();
    // result = [1, 2, 3, 4, 5, 6, 7, 8, 9]
}
```

## 2. 使用 List<byte>

```csharp
public class ListByteBuilder
{
    private readonly List<byte> _list;
    
    public ListByteBuilder(int initialCapacity = 1024)
    {
        _list = new List<byte>(initialCapacity);
    }
    
    public void Append(byte[] data)
    {
        _list.AddRange(data);
    }
    
    public void Append(byte value)
    {
        _list.Add(value);
    }
    
    public void Append(IEnumerable<byte> data)
    {
        _list.AddRange(data);
    }
    
    public byte[] ToArray()
    {
        return _list.ToArray();
    }
    
    public int Length => _list.Count;
    
    public void Clear()
    {
        _list.Clear();
    }
    
    // 额外功能：直接访问内部列表
    public byte this[int index] => _list[index];
    
    public IEnumerator<byte> GetEnumerator() => _list.GetEnumerator();
}

// 使用示例
var builder = new ListByteBuilder();
builder.Append(new byte[] { 1, 2, 3 });
builder.Append(new byte[] { 4, 5, 6 });
byte[] result = builder.ToArray();
```

## 3. 使用 ArrayPool 的高性能版本

```csharp
using System.Buffers;

public class PooledByteBuilder : IDisposable
{
    private byte[] _buffer;
    private int _position;
    private readonly int _initialCapacity;
    
    public PooledByteBuilder(int initialCapacity = 4096)
    {
        _initialCapacity = initialCapacity;
        _buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
        _position = 0;
    }
    
    public void Append(byte[] data)
    {
        if (data == null) return;
        
        EnsureCapacity(_position + data.Length);
        Buffer.BlockCopy(data, 0, _buffer, _position, data.Length);
        _position += data.Length;
    }
    
    public void Append(ReadOnlySpan<byte> data)
    {
        EnsureCapacity(_position + data.Length);
        data.CopyTo(new Span<byte>(_buffer, _position, data.Length));
        _position += data.Length;
    }
    
    public void Append(byte value)
    {
        EnsureCapacity(_position + 1);
        _buffer[_position++] = value;
    }
    
    private void EnsureCapacity(int requiredCapacity)
    {
        if (requiredCapacity <= _buffer.Length) return;
        
        int newCapacity = Math.Max(_buffer.Length * 2, requiredCapacity);
        byte[] newBuffer = ArrayPool<byte>.Shared.Rent(newCapacity);
        
        Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _position);
        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = newBuffer;
    }
    
    public byte[] ToArray()
    {
        byte[] result = new byte[_position];
        Buffer.BlockCopy(_buffer, 0, result, 0, _position);
        return result;
    }
    
    public ReadOnlyMemory<byte> ToMemory()
    {
        return new Memory<byte>(_buffer, 0, _position);
    }
    
    public ReadOnlySpan<byte> ToSpan()
    {
        return new Span<byte>(_buffer, 0, _position);
    }
    
    public int Length => _position;
    
    public void Clear()
    {
        _position = 0;
    }
    
    public void Dispose()
    {
        if (_buffer != null)
        {
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = null;
        }
    }
}

// 使用示例
using (var builder = new PooledByteBuilder())
{
    builder.Append(new byte[] { 1, 2, 3 });
    builder.Append(new byte[] { 4, 5, 6 });
    
    byte[] result = builder.ToArray();
}
```

## 4. 使用 Span 和 Memory 的现代版本

```csharp
public ref struct SpanByteBuilder
{
    private Span<byte> _buffer;
    private int _position;
    
    public SpanByteBuilder(Span<byte> initialBuffer)
    {
        _buffer = initialBuffer;
        _position = 0;
    }
    
    public bool TryAppend(ReadOnlySpan<byte> data)
    {
        if (_position + data.Length > _buffer.Length)
            return false;
            
        data.CopyTo(_buffer.Slice(_position));
        _position += data.Length;
        return true;
    }
    
    public ReadOnlySpan<byte> ToSpan() => _buffer.Slice(0, _position);
    
    public int Length => _position;
    public int Capacity => _buffer.Length;
    public int Remaining => _buffer.Length - _position;
}

// 使用示例
byte[] buffer = new byte[1024];
var builder = new SpanByteBuilder(buffer);

builder.TryAppend(new byte[] { 1, 2, 3 });
builder.TryAppend(new byte[] { 4, 5, 6 });

ReadOnlySpan<byte> result = builder.ToSpan();
```

## 5. 扩展方法方式

```csharp
public static class ByteArrayExtensions
{
    public static byte[] Combine(params byte[][] arrays)
    {
        if (arrays == null || arrays.Length == 0)
            return Array.Empty<byte>();
            
        int totalLength = arrays.Sum(arr => arr?.Length ?? 0);
        byte[] result = new byte[totalLength];
        
        int position = 0;
        foreach (byte[] array in arrays)
        {
            if (array != null && array.Length > 0)
            {
                Buffer.BlockCopy(array, 0, result, position, array.Length);
                position += array.Length;
            }
        }
        
        return result;
    }
    
    public static byte[] Combine(this byte[] first, byte[] second)
    {
        if (first == null) return second ?? Array.Empty<byte>();
        if (second == null) return first;
        
        byte[] result = new byte[first.Length + second.Length];
        Buffer.BlockCopy(first, 0, result, 0, first.Length);
        Buffer.BlockCopy(second, 0, result, first.Length, second.Length);
        return result;
    }
}

// 使用示例
byte[] result1 = ByteArrayExtensions.Combine(
    new byte[] { 1, 2, 3 },
    new byte[] { 4, 5, 6 },
    new byte[] { 7, 8, 9 }
);

byte[] result2 = new byte[] { 1, 2, 3 }.Combine(new byte[] { 4, 5, 6 });
```

## 6. 完整的功能丰富的实现

```csharp
public class AdvancedByteBuilder : IDisposable
{
    private MemoryStream _stream;
    private readonly bool _usePooledArrays;
    
    public AdvancedByteBuilder(int initialCapacity = 4096, bool usePooledArrays = false)
    {
        _usePooledArrays = usePooledArrays;
        
        if (usePooledArrays && initialCapacity > 0)
        {
            // 使用数组池获取初始缓冲区
            byte[] initialBuffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
            _stream = new MemoryStream(initialBuffer, 0, initialBuffer.Length, true, true);
            _stream.SetLength(0); // 从位置0开始
        }
        else
        {
            _stream = new MemoryStream(initialCapacity);
        }
    }
    
    public void Append(byte[] data) => Append(data, 0, data?.Length ?? 0);
    
    public void Append(byte[] data, int offset, int count)
    {
        if (data == null || count == 0) return;
        _stream.Write(data, offset, count);
    }
    
    public void Append(ReadOnlySpan<byte> data)
    {
        _stream.Write(data);
    }
    
    public void Append(byte value)
    {
        _stream.WriteByte(value);
    }
    
    public void Append(string text, Encoding encoding = null)
    {
        encoding ??= Encoding.UTF8;
        byte[] bytes = encoding.GetBytes(text);
        Append(bytes);
    }
    
    public void Insert(int position, byte[] data)
    {
        if (data == null) return;
        
        long originalPosition = _stream.Position;
        _stream.Position = position;
        _stream.Write(data, 0, data.Length);
        
        // 如果插入位置在原来数据中间，需要移动后续数据
        if (position < originalPosition)
        {
            _stream.Position = originalPosition + data.Length;
        }
    }
    
    public byte[] ToArray()
    {
        return _stream.ToArray();
    }
    
    public byte[] ToArrayAndClear()
    {
        byte[] result = ToArray();
        Clear();
        return result;
    }
    
    public ReadOnlyMemory<byte> ToMemory()
    {
        return new ReadOnlyMemory<byte>(_stream.GetBuffer(), 0, (int)_stream.Length);
    }
    
    public void CopyTo(Stream stream)
    {
        _stream.WriteTo(stream);
    }
    
    public void CopyTo(byte[] buffer, int offset)
    {
        byte[] data = ToArray();
        Buffer.BlockCopy(data, 0, buffer, offset, data.Length);
    }
    
    public long Length => _stream.Length;
    public long Capacity => _stream.Capacity;
    
    public void Clear()
    {
        _stream.SetLength(0);
    }
    
    public void Dispose()
    {
        if (_stream != null)
        {
            if (_usePooledArrays)
            {
                byte[] buffer = _stream.GetBuffer();
                ArrayPool<byte>.Shared.Return(buffer);
            }
            _stream.Dispose();
            _stream = null;
        }
    }
}

// 使用示例
using (var builder = new AdvancedByteBuilder(usePooledArrays: true))
{
    builder.Append(new byte[] { 1, 2, 3 });
    builder.Append("Hello", Encoding.UTF8);
    builder.Append(new byte[] { 4, 5, 6 });
    
    byte[] result = builder.ToArray();
    
    // 或者直接使用 Memory 避免复制
    ReadOnlyMemory<byte> memory = builder.ToMemory();
}
```

## 性能比较和使用建议

| 方法 | 性能 | 内存效率 | 易用性 | 适用场景 |
|------|------|----------|--------|----------|
| **MemoryStream** | 高 | 高 | 高 | 通用场景，推荐使用 |
| **List\<byte>** | 中 | 中 | 高 | 简单场景 |
| **ArrayPool** | 最高 | 最高 | 中 | 高性能场景 |
| **Span** | 极高 | 极高 | 低 | 栈分配，小数据量 |
| **扩展方法** | 低 | 低 | 高 | 一次性合并 |

**推荐使用：**
1. **MemoryStream** - 大多数场景的最佳选择
2. **PooledByteBuilder** - 对性能要求极高的场景
3. **AdvancedByteBuilder** - 需要丰富功能时的选择

对于 64KB 左右的数据拼接，使用 `MemoryStream` 或 `PooledByteBuilder` 都是很好的选择，它们都能高效处理这种规模的数据。