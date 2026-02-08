using System.Text;
using Microsoft.Extensions.Logging;

namespace ItchProtocol.DSE;

/// <summary>
/// ITCH Protocol Consumer for DSE-BD (Dhaka Stock Exchange - Bangladesh)
/// Parses and processes NASDAQ ITCH 5.0 format market data messages
/// </summary>
public class ItchConsumer
{
    private readonly ILogger<ItchConsumer> _logger;
    private readonly Dictionary<string, StockDirectoryMessage> _stocks = new();
    private readonly Dictionary<ulong, AddOrderMessage> _orders = new();
    private long _messagesProcessed = 0;
    private long _messagesWithErrors = 0;

    public ItchConsumer(ILogger<ItchConsumer> logger)
    {
        _logger = logger;
    }

    public long MessagesProcessed => _messagesProcessed;
    public long MessagesWithErrors => _messagesWithErrors;
    public int StocksCount => _stocks.Count;
    public int ActiveOrdersCount => _orders.Count;

    /// <summary>
    /// Process a single ITCH message
    /// </summary>
    public void ProcessMessage(byte[] data, int offset = 0)
    {
        try
        {
            if (data.Length < offset + 1)
            {
                _logger.LogWarning("Message too short");
                _messagesWithErrors++;
                return;
            }

            var messageType = (ItchMessageType)data[offset];
            
            ItchMessage? message = messageType switch
            {
                ItchMessageType.SystemEvent => new SystemEventMessage(),
                ItchMessageType.StockDirectory => new StockDirectoryMessage(),
                ItchMessageType.AddOrder => new AddOrderMessage(),
                ItchMessageType.OrderExecuted => new OrderExecutedMessage(),
                ItchMessageType.Trade => new TradeMessage(),
                _ => null
            };

            if (message != null)
            {
                message.MessageType = messageType;
                
                // Parse common header
                if (data.Length >= offset + 11)
                {
                    message.StockLocate = BitConverter.ToUInt16(ItchBinaryUtil.ReadBigEndian(data, offset + 1, 2), 0);
                    message.TrackingNumber = BitConverter.ToUInt16(ItchBinaryUtil.ReadBigEndian(data, offset + 3, 2), 0);
                    message.Timestamp = BitConverter.ToUInt64(ItchBinaryUtil.ReadBigEndian(data, offset + 5, 6), 0);
                }

                // Parse message-specific data
                message.Parse(data, offset);
                
                ProcessParsedMessage(message);
                _messagesProcessed++;
            }
            else
            {
                _logger.LogDebug("Unsupported message type: {MessageType}", (char)messageType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
            _messagesWithErrors++;
        }
    }

    /// <summary>
    /// Process a stream of ITCH messages from a file or network stream
    /// </summary>
    public void ProcessStream(Stream stream)
    {
        _logger.LogInformation("Starting to process ITCH stream...");
        
        var buffer = new byte[65536]; // 64KB buffer
        int bytesRead;
        var incompleteMessage = new List<byte>();

        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            var offset = 0;
            
            // If we have incomplete message from previous read, prepend it
            if (incompleteMessage.Count > 0)
            {
                var combined = new byte[incompleteMessage.Count + bytesRead];
                incompleteMessage.CopyTo(combined);
                Array.Copy(buffer, 0, combined, incompleteMessage.Count, bytesRead);
                buffer = combined;
                bytesRead = combined.Length;
                incompleteMessage.Clear();
            }

            while (offset < bytesRead)
            {
                // Read message length (2 bytes, big-endian)
                if (offset + 2 > bytesRead)
                {
                    // Incomplete length header, save for next read
                    for (int i = offset; i < bytesRead; i++)
                        incompleteMessage.Add(buffer[i]);
                    break;
                }

                var messageLength = BitConverter.ToUInt16(ItchBinaryUtil.ReadBigEndian(buffer, offset, 2), 0);
                
                if (offset + 2 + messageLength > bytesRead)
                {
                    // Incomplete message, save for next read
                    for (int i = offset; i < bytesRead; i++)
                        incompleteMessage.Add(buffer[i]);
                    break;
                }

                // Process the complete message
                ProcessMessage(buffer, offset + 2);
                offset += 2 + messageLength;
            }
        }

        _logger.LogInformation("Finished processing ITCH stream");
        _logger.LogInformation("Total messages processed: {Count}", _messagesProcessed);
        _logger.LogInformation("Messages with errors: {Count}", _messagesWithErrors);
    }

    private void ProcessParsedMessage(ItchMessage message)
    {
        switch (message)
        {
            case SystemEventMessage systemEvent:
                ProcessSystemEvent(systemEvent);
                break;
                
            case StockDirectoryMessage stockDir:
                ProcessStockDirectory(stockDir);
                break;
                
            case AddOrderMessage addOrder:
                ProcessAddOrder(addOrder);
                break;
                
            case OrderExecutedMessage orderExec:
                ProcessOrderExecuted(orderExec);
                break;
                
            case TradeMessage trade:
                ProcessTrade(trade);
                break;
        }
    }

    private void ProcessSystemEvent(SystemEventMessage message)
    {
        _logger.LogInformation("System Event: {Event} at {Timestamp}", 
            message.GetEventDescription(), 
            FormatTimestamp(message.Timestamp));
    }

    private void ProcessStockDirectory(StockDirectoryMessage message)
    {
        _stocks[message.Stock] = message;
        _logger.LogInformation("Stock Directory: {Stock} - Category: {Category}, RoundLot: {RoundLot}", 
            message.Stock, 
            message.MarketCategory,
            message.RoundLotSize);
    }

    private void ProcessAddOrder(AddOrderMessage message)
    {
        _orders[message.OrderReferenceNumber] = message;
        _logger.LogInformation("Add Order: {Stock} {Side} {Shares}@{Price} (Ref: {Ref})", 
            message.Stock,
            message.BuySellIndicator == 'B' ? "BUY" : "SELL",
            message.Shares,
            message.GetPrice(),
            message.OrderReferenceNumber);
    }

    private void ProcessOrderExecuted(OrderExecutedMessage message)
    {
        if (_orders.TryGetValue(message.OrderReferenceNumber, out var order))
        {
            _logger.LogInformation("Order Executed: Ref {Ref}, Shares: {Shares}, Match: {Match}", 
                message.OrderReferenceNumber,
                message.ExecutedShares,
                message.MatchNumber);
            
            // Validate executed shares don't exceed order shares
            if (message.ExecutedShares > order.Shares)
            {
                _logger.LogWarning("Protocol violation: ExecutedShares ({Executed}) exceeds order Shares ({OrderShares}) for Ref {Ref}", 
                    message.ExecutedShares, order.Shares, message.OrderReferenceNumber);
                _messagesWithErrors++;
                return;
            }
            
            // Update or remove order
            order.Shares -= message.ExecutedShares;
            if (order.Shares == 0)
            {
                _orders.Remove(message.OrderReferenceNumber);
            }
        }
    }

    private void ProcessTrade(TradeMessage message)
    {
        _logger.LogInformation("Trade: {Stock} {Side} {Shares}@{Price} (Match: {Match})", 
            message.Stock,
            message.BuySellIndicator == 'B' ? "BUY" : "SELL",
            message.Shares,
            message.GetPrice(),
            message.MatchNumber);
    }

    public void PrintStatistics()
    {
        Console.WriteLine("\n=== ITCH Consumer Statistics ===");
        Console.WriteLine($"Messages Processed: {_messagesProcessed}");
        Console.WriteLine($"Messages with Errors: {_messagesWithErrors}");
        Console.WriteLine($"Stocks in Directory: {_stocks.Count}");
        Console.WriteLine($"Active Orders: {_orders.Count}");
        
        if (_stocks.Count > 0)
        {
            Console.WriteLine("\nStocks:");
            foreach (var stock in _stocks.Values.Take(10))
            {
                Console.WriteLine($"  {stock.Stock,-12} Category: {stock.MarketCategory}  RoundLot: {stock.RoundLotSize}");
            }
            if (_stocks.Count > 10)
            {
                Console.WriteLine($"  ... and {_stocks.Count - 10} more");
            }
        }
    }

    private static string FormatTimestamp(ulong timestamp)
    {
        // ITCH timestamps are nanoseconds since midnight
        var totalSeconds = timestamp / 1_000_000_000.0;
        var hours = (int)(totalSeconds / 3600);
        var minutes = (int)((totalSeconds % 3600) / 60);
        var seconds = totalSeconds % 60;
        return $"{hours:D2}:{minutes:D2}:{seconds:F6}";
    }

    /// <summary>
    /// Generate sample ITCH messages for testing
    /// </summary>
    public static byte[] GenerateSampleMessage(ItchMessageType type)
    {
        var message = new List<byte>();
        
        switch (type)
        {
            case ItchMessageType.SystemEvent:
                message.Add((byte)'S');  // Message type
                message.AddRange(new byte[2]); // Stock locate
                message.AddRange(new byte[2]); // Tracking number
                message.AddRange(new byte[6]); // Timestamp
                message.Add((byte)'O'); // Event code: Start of Messages
                return message.ToArray();
                
            case ItchMessageType.StockDirectory:
                message.Add((byte)'R');  // Message type
                message.AddRange(new byte[2]); // Stock locate
                message.AddRange(new byte[2]); // Tracking number
                message.AddRange(new byte[6]); // Timestamp
                message.AddRange(Encoding.ASCII.GetBytes("ACI     ")); // Stock (8 bytes, padded)
                message.Add((byte)'Q'); // Market category
                message.Add((byte)'N'); // Financial status
                message.AddRange(BitConverter.GetBytes(100u).Reverse()); // Round lot size
                message.Add((byte)'Y'); // Round lots only
                message.Add((byte)'C'); // Issue classification
                message.AddRange(Encoding.ASCII.GetBytes("  ")); // Issue subtype (2 bytes)
                message.Add((byte)'P'); // Authenticity
                message.Add((byte)'N'); // Short sale threshold
                message.Add((byte)'N'); // IPO flag
                message.Add((byte)'1'); // LULD tier
                message.Add((byte)'N'); // ETP flag
                message.AddRange(BitConverter.GetBytes(1u).Reverse()); // ETP leverage
                message.Add((byte)'N'); // Inverse indicator
                return message.ToArray();
                
            case ItchMessageType.AddOrder:
                message.Add((byte)'A');  // Message type
                message.AddRange(new byte[2]); // Stock locate
                message.AddRange(new byte[2]); // Tracking number
                message.AddRange(new byte[6]); // Timestamp
                message.AddRange(BitConverter.GetBytes(12345UL).Reverse()); // Order ref
                message.Add((byte)'B'); // Buy/Sell (B = Buy)
                message.AddRange(BitConverter.GetBytes(100u).Reverse()); // Shares
                message.AddRange(Encoding.ASCII.GetBytes("ACI     ")); // Stock (8 bytes)
                message.AddRange(BitConverter.GetBytes(8765000u).Reverse()); // Price (876.5000)
                return message.ToArray();
        }
        
        return message.ToArray();
    }
}
