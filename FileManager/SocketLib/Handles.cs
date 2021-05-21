using System;

namespace FileManager.SocketLib
{
    
    /// <summary>
    /// Socket 异步操作回调函数句柄
    /// </summary>
    public delegate void SocketAsyncCallback();
    public delegate void SocketAsyncExceptionCallback(Exception ex);
    public delegate void SocketOutputCallback(string s);
}