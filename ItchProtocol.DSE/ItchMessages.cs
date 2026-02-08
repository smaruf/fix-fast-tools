using System.Text;

namespace ItchProtocol.DSE;

/// <summary>
/// Utility class for ITCH protocol binary data operations
/// </summary>
public static class ItchBinaryUtil
{
    /// <summary>
    /// Read big-endian bytes and convert to little-endian for BitConverter
    /// </summary>
    public static byte[] ReadBigEndian(byte[] data, int offset, int length)
    {
        var result = new byte[8];
        for (int i = 0; i < length && i < 8; i++)
        {
            result[7 - i] = data[offset + length - 1 - i];
        }
        return result;
    }
}

/// <summary>
/// ITCH protocol message types based on NASDAQ ITCH 5.0 specification
/// Adapted for DSE-BD market data
/// </summary>
public enum ItchMessageType : byte
{
    SystemEvent = (byte)'S',
    StockDirectory = (byte)'R',
    StockTradingAction = (byte)'H',
    RegSHORestriction = (byte)'Y',
    MarketParticipantPosition = (byte)'L',
    AddOrder = (byte)'A',
    AddOrderMPID = (byte)'F',
    OrderExecuted = (byte)'E',
    OrderExecutedWithPrice = (byte)'C',
    OrderCancel = (byte)'X',
    OrderDelete = (byte)'D',
    OrderReplace = (byte)'U',
    Trade = (byte)'P',
    CrossTrade = (byte)'Q',
    BrokenTrade = (byte)'B',
    NOII = (byte)'I'
}

/// <summary>
/// Base class for all ITCH messages
/// </summary>
public abstract class ItchMessage
{
    public ItchMessageType MessageType { get; set; }
    public ushort StockLocate { get; set; }
    public ushort TrackingNumber { get; set; }
    public ulong Timestamp { get; set; }

    public abstract void Parse(byte[] data, int offset);
}

/// <summary>
/// System Event Message
/// </summary>
public class SystemEventMessage : ItchMessage
{
    public char EventCode { get; set; }

    public override void Parse(byte[] data, int offset)
    {
        EventCode = (char)data[offset + 11];
    }

    public string GetEventDescription()
    {
        return EventCode switch
        {
            'O' => "Start of Messages",
            'S' => "Start of System Hours",
            'Q' => "Start of Market Hours",
            'M' => "End of Market Hours",
            'E' => "End of System Hours",
            'C' => "End of Messages",
            _ => $"Unknown ({EventCode})"
        };
    }
}

/// <summary>
/// Stock Directory Message
/// </summary>
public class StockDirectoryMessage : ItchMessage
{
    public string Stock { get; set; } = string.Empty;
    public char MarketCategory { get; set; }
    public char FinancialStatusIndicator { get; set; }
    public uint RoundLotSize { get; set; }
    public char RoundLotsOnly { get; set; }
    public char IssueClassification { get; set; }
    public string IssueSubType { get; set; } = string.Empty;
    public char Authenticity { get; set; }
    public char ShortSaleThresholdIndicator { get; set; }
    public char IPOFlag { get; set; }
    public char LULDReferencePriceTier { get; set; }
    public char ETPFlag { get; set; }
    public uint ETPLeverageFactor { get; set; }
    public char InverseIndicator { get; set; }

    public override void Parse(byte[] data, int offset)
    {
        Stock = Encoding.ASCII.GetString(data, offset + 11, 8).Trim();
        MarketCategory = (char)data[offset + 19];
        FinancialStatusIndicator = (char)data[offset + 20];
        RoundLotSize = BitConverter.ToUInt32(ItchBinaryUtil.ReadBigEndian(data, offset + 21, 4), 0);
        RoundLotsOnly = (char)data[offset + 25];
        IssueClassification = (char)data[offset + 26];
        IssueSubType = Encoding.ASCII.GetString(data, offset + 27, 2);
        Authenticity = (char)data[offset + 29];
        ShortSaleThresholdIndicator = (char)data[offset + 30];
        IPOFlag = (char)data[offset + 31];
        LULDReferencePriceTier = (char)data[offset + 32];
        ETPFlag = (char)data[offset + 33];
        ETPLeverageFactor = BitConverter.ToUInt32(ItchBinaryUtil.ReadBigEndian(data, offset + 34, 4), 0);
        InverseIndicator = (char)data[offset + 38];
    }
}

/// <summary>
/// Add Order Message
/// </summary>
public class AddOrderMessage : ItchMessage
{
    public ulong OrderReferenceNumber { get; set; }
    public char BuySellIndicator { get; set; }
    public uint Shares { get; set; }
    public string Stock { get; set; } = string.Empty;
    public uint Price { get; set; }

    public override void Parse(byte[] data, int offset)
    {
        OrderReferenceNumber = BitConverter.ToUInt64(ItchBinaryUtil.ReadBigEndian(data, offset + 11, 8), 0);
        BuySellIndicator = (char)data[offset + 19];
        Shares = BitConverter.ToUInt32(ItchBinaryUtil.ReadBigEndian(data, offset + 20, 4), 0);
        Stock = Encoding.ASCII.GetString(data, offset + 24, 8).Trim();
        Price = BitConverter.ToUInt32(ItchBinaryUtil.ReadBigEndian(data, offset + 32, 4), 0);
    }

    public decimal GetPrice()
    {
        return Price / 10000m;
    }
}

/// <summary>
/// Order Executed Message
/// </summary>
public class OrderExecutedMessage : ItchMessage
{
    public ulong OrderReferenceNumber { get; set; }
    public uint ExecutedShares { get; set; }
    public ulong MatchNumber { get; set; }

    public override void Parse(byte[] data, int offset)
    {
        OrderReferenceNumber = BitConverter.ToUInt64(ItchBinaryUtil.ReadBigEndian(data, offset + 11, 8), 0);
        ExecutedShares = BitConverter.ToUInt32(ItchBinaryUtil.ReadBigEndian(data, offset + 19, 4), 0);
        MatchNumber = BitConverter.ToUInt64(ItchBinaryUtil.ReadBigEndian(data, offset + 23, 8), 0);
    }
}

/// <summary>
/// Trade Message (Non-Cross)
/// </summary>
public class TradeMessage : ItchMessage
{
    public ulong OrderReferenceNumber { get; set; }
    public char BuySellIndicator { get; set; }
    public uint Shares { get; set; }
    public string Stock { get; set; } = string.Empty;
    public uint Price { get; set; }
    public ulong MatchNumber { get; set; }

    public override void Parse(byte[] data, int offset)
    {
        OrderReferenceNumber = BitConverter.ToUInt64(ItchBinaryUtil.ReadBigEndian(data, offset + 11, 8), 0);
        BuySellIndicator = (char)data[offset + 19];
        Shares = BitConverter.ToUInt32(ItchBinaryUtil.ReadBigEndian(data, offset + 20, 4), 0);
        Stock = Encoding.ASCII.GetString(data, offset + 24, 8).Trim();
        Price = BitConverter.ToUInt32(ItchBinaryUtil.ReadBigEndian(data, offset + 32, 4), 0);
        MatchNumber = BitConverter.ToUInt64(ItchBinaryUtil.ReadBigEndian(data, offset + 36, 8), 0);
    }

    public decimal GetPrice()
    {
        return Price / 10000m;
    }
}
