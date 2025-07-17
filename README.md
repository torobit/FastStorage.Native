FastStorage.Native

High‑performance, cross‑platform native reader for the proprietary FastCache market‑data format.  The library is written in C# 11 / .NET 8, 
ahead‑of‑time (AOT) compiled into a small native shared library that can be loaded from Python, Rust, C++, or any other language that can call a C ABI.

Why? We had terabytes of historical Binance/Bybit order‑book snapshots and trade ticks compressed with K4os/LZ4‑Pickler.  
Reading them back from Python was painfully slow.  FastStorage.Native gives you zero‑allocation reads at ~540 k msg/s on Apple M‑series and lets you keep all the heavy lifting in one place.


Features

🏎 Zero‑GC hot path – direct pointer arithmetic in unsafe C#.

🗜 Compatible with legacy .lz4 archives – uses K4os.Compression.LZ4 Pickler.

🔌 Minimal C ABI – three functions: open_reader, read_message, close_reader.

📚 Typed structs for Depth, Tick, Candle, etc., identical layout in C# and ctypes.

🐍 Drop‑in Python demo – see examples/process_messages.py.

🖥 Runs everywhere – Windows, Linux, macOS; x64 & ARM (win‑x64, linux‑arm64, osx‑arm64, …).


MessageHeader

Offset Size Field           Type            Description
0      2    Kind            short           See enum `MessageKind`
2      2    Size            ushort          Total bytes of this message struct
4      8    Time            long            Unix epoch millis (UTC)

Benchmark

Measured on an Apple M2 Pro (10‑core), macOS 14.4, .NET 8.0 AOT build (osx‑arm64).  Input file: combined depth + trade stream for ETH‑USDT (≈640 MB, Feb  9 2025).

BENCHMARK COMPLETE
Processed 64,520,101 messages in 119.9755 seconds.
Messages per second: 537,777
--------------------------------------------------
Final State:
Bids: 4 498   Asks: 4 766   Best Bid: 2626.07    Best Ask: 2626.08   
Trades count: 3 403 498
Last trade: (638747423998706545, 2626.07, 0.006)

That’s ~538 kmsg/s end‑to‑end through Python ctypes (no Cython, no NumPy) while maintaining zero allocations on the C# side.

Build & publish

Requires .NET 8 SDK (download from https://dotnet.microsoft.com/download)

One‑liner for your current OS/CPU

# Example: macOS ARM64 (Apple M‑series)
dotnet publish -c Release -r osx-arm64 -p:PublishAot=true --self-contained true

The native artifact will be in:
bin/Release/net8.0/<RID>/publish/FastStorage.Native{.dll|.so|.dylib}

Build matrix for common RIDs

OS

Runtime Identifier

Command

Windows 10/11 x64

win-x64

dotnet publish -c Release -r win-x64  -p:PublishAot=true --self-contained true

Windows ARM64

win-arm64

…

Ubuntu 22.04 x64

linux-x64

…

Ubuntu 22.04 ARM

linux-arm64

…

macOS Intel

osx-x64

…

macOS Apple Silicon

osx-arm64

…


from faststorage_native import FastReader, DepthItem, TickItem
from pathlib import Path

file = Path("~/data/ETHUSDT_20250209.bin.lz4").expanduser()
with FastReader(file) as reader:
    for msg in reader:
        if isinstance(msg, DepthItem):
            ...  # update order book
        elif isinstance(msg, TickItem):
            ...  # update trades

A full benchmark script is reproduced under examples/process_messages.py.

C API reference

int  open_reader (const char* path_utf8, void** handle_out);
int  read_message(void* handle, void** msg_ptr_out);   // returns size, 0 = EOF, <0 = error
void close_reader(void* handle);



Contributing

Fork and create a feature branch.
Run dotnet format before committing.
Open a PR – GitHub Actions will build all targets.
Issues & feature requests welcome!
