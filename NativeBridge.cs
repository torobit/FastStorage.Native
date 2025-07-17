using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

/// <summary>
/// This class provides the C-style functions that can be called from native code (like Python).
/// It acts as a bridge between the .NET world and the outside world.
/// </summary>
public static unsafe class NativeBridge
{
    private static readonly ConcurrentDictionary<IntPtr, FastCacheReader> s_readers = new();
    private static long s_handleCounter = 0;

    [UnmanagedCallersOnly(EntryPoint = "open_reader")]
    public static int OpenReader(IntPtr filePathPtr, IntPtr* readerHandleOutPtr)
    {
        if (readerHandleOutPtr == null) return -1;
        *readerHandleOutPtr = IntPtr.Zero;

        try
        {
            string? filePath = Marshal.PtrToStringUTF8(filePathPtr);
            if (string.IsNullOrEmpty(filePath))
            {
                return -1; 
            }

            var reader = new FastCacheReader(filePath);
            
            var handle = (IntPtr)Interlocked.Increment(ref s_handleCounter);
            if (!s_readers.TryAdd(handle, reader))
            {
                reader.Dispose();
                return -1;
            }

            *readerHandleOutPtr = handle;
            
            return 0; // Success
        }
        catch (Exception)
        {
            return -1; // Failure
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "read_message")]
    public static int ReadMessage(IntPtr readerHandle, IntPtr* messagePtrOutPtr)
    {
        if (messagePtrOutPtr == null) return -1;
        *messagePtrOutPtr = IntPtr.Zero;

        if (!s_readers.TryGetValue(readerHandle, out var reader))
        {
            return -1; 
        }

        try
        {
            if (reader.Read())
            {
                *messagePtrOutPtr = reader.Current;
                var header = (MessageHeader*)(*messagePtrOutPtr);
                return header->Size;
            }
            else
            {
                return 0; 
            }
        }

        catch (InvalidDataException)
        {
            return -2; // Corrupted data block
        }
        catch (EndOfStreamException)
        {
            return -3; // Unexpected end of file
        }
        catch (Exception)
        {
            return -1; // Generic/unknown error
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "close_reader")]
    public static void CloseReader(IntPtr readerHandle)
    {
        if (s_readers.TryRemove(readerHandle, out var reader))
        {
            reader.Dispose();
        }
    }
}
