using System;
using System.Runtime.InteropServices;


[StructLayout(LayoutKind.Explicit, Size = 12)]
public struct MessageHeader
{
    [FieldOffset(0)]
    public MessageKind Kind;
    [FieldOffset(2)]
    public ushort Size;
    [FieldOffset(4)]
    public long Time;
}

public enum MessageKind : short
{
    Depth,
    Tick,
    Symbol,
    Candle,
    CandleEnd
}

[StructLayout(LayoutKind.Explicit, Size = 12 + 8 * 5 + 4)]
public struct CandleItem
{
    [FieldOffset(0)]
    public MessageHeader Header; 
    [FieldOffset(12)]
    public long Open;
    [FieldOffset(20)]
    public long High;
    [FieldOffset(28)]
    public long Low;
    [FieldOffset(36)]
    public long Close;
    [FieldOffset(44)]
    public long Volume;
    [FieldOffset(52)]
    public int PeriodSeconds;
}

[StructLayout(LayoutKind.Explicit, Size = 37)]
public struct TickItem
{
    [FieldOffset(0)]
    public MessageHeader Header; 
    [FieldOffset(12)]
    public long Id;
    [FieldOffset(20)]
    public long Price;
    [FieldOffset(28)]
    public long Volume;
    [FieldOffset(36)]
    public OrderSide Type;
}

[StructLayout(LayoutKind.Explicit, Size = 29)]
public struct DepthItem
{
    [FieldOffset(0)]
    public MessageHeader Header; 
    [FieldOffset(12)]
    public long Price;
    [FieldOffset(20)]
    public long Volume;
    [FieldOffset(28)]
    public MarketFlag Flags;
    public override string ToString()
    {
        return $"{Flags} {Volume}@{Price}";
    }
}

public enum OrderSide : byte
{
    Unknown = 0,
    Buy = 1,
    Sell = 2
}

[Flags]
public enum MarketFlag : byte
{
    Buy = 1,             
    Sell = 2,
    Clear = 4,            // begin of snapshot
    EndOfTransaction = 8  // end of transaction
}