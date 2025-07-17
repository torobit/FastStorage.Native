using System;
using System.IO;
using System.Runtime.InteropServices;
using K4os.Compression.LZ4;

public sealed unsafe class FastCacheReader : IDisposable
{
    // ── fields ──────────────────────────────────────────────────────────
    private readonly Stream? _stream;
    private readonly string _fileName;

    private readonly byte[] _srcBuf;      // holds decompressed data
    private readonly byte[] _cmpBuf;      // holds compressed frame
    private readonly GCHandle _srcHandle;
    private readonly IntPtr _srcAddr;

    private int _offset;                // cursor in _srcBuf
    private int _blockLen;              // bytes in current block
    private IntPtr _current;               // value returned by Current

    // ── ctor ────────────────────────────────────────────────────────────
    public FastCacheReader(string fileName, Stream? stream = null)
    {
        _fileName = fileName;
        _stream = stream ?? new FileStream(
                              fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        var reader = new BinaryReader(_stream);
        int bufLen = reader.ReadInt32();

        if (bufLen <= 0)
            throw new Exception($"Invalid buffer length ({bufLen}) in {fileName}");

        _srcBuf = new byte[bufLen];
        // The compressed buffer size is an estimate. It needs to be large enough for the worst case.
        _cmpBuf = new byte[LZ4Codec.MaximumOutputSize(bufLen) + 32]; // Ample buffer

        _srcHandle = GCHandle.Alloc(_srcBuf, GCHandleType.Pinned);
        _srcAddr = _srcHandle.AddrOfPinnedObject();

        _offset = 0;
        _blockLen = 0;
    }

    // ── public API ──────────────────────────────────────────────────────
    public bool Read()
    {
        if (_offset >= _blockLen)
            if (!LoadNextBlock()) return false;

        _current = _srcAddr + _offset;

        var hdr = (MessageHeader*)_current;
        if (hdr->Size == 0) return false;

        _offset += hdr->Size;

        if (_offset > _blockLen)
            throw new Exception(
                $"offset({_offset}) > blockLen({_blockLen}) : corrupt file {_fileName}");

        return true;
    }

    public IntPtr Current => _current;

    // ── helpers ─────────────────────────────────────────────────────────
    private bool LoadNextBlock()
    {
        if (_stream is null) return false;

        var lenBytes = new byte[4];
        if (_stream.Read(lenBytes, 0, 4) != 4) return false;

        int cmpLen = BitConverter.ToInt32(lenBytes, 0);
        if (cmpLen <= 0) return false;

        if (cmpLen > _cmpBuf.Length)
             throw new Exception($"Block length {cmpLen} exceeds buffer size in {_fileName}");

        if (_stream.Read(_cmpBuf, 0, cmpLen) != cmpLen)
            throw new EndOfStreamException($"Unexpected EOF in {_fileName}");
        
        var block = LZ4Pickler.Unpickle(_cmpBuf, 0, cmpLen);

        _blockLen = block.Length;
        if (_blockLen > _srcBuf.Length)
            throw new Exception($"Block bigger than buffer in {_fileName}");

        Buffer.BlockCopy(block, 0, _srcBuf, 0, _blockLen);

        _offset = 0;
        return true;
    }
    
    public void Dispose() => Dispose(true);

    private void Dispose(bool disposing)
    {
        if (disposing) _stream?.Dispose();
        if (_srcHandle.IsAllocated) _srcHandle.Free();
    }

    ~FastCacheReader() => Dispose(false);
}
