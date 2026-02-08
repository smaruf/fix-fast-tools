using Microsoft.Extensions.Logging;
using ItchProtocol.DSE;

Console.WriteLine("==============================================");
Console.WriteLine("ITCH Protocol Consumer for DSE-BD");
Console.WriteLine("Dhaka Stock Exchange - Bangladesh");
Console.WriteLine("Market Data Feed Consumer");
Console.WriteLine("==============================================\n");

// Configure logging
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .SetMinimumLevel(LogLevel.Information);
});

var logger = loggerFactory.CreateLogger<Program>();
var consumer = new ItchConsumer(loggerFactory.CreateLogger<ItchConsumer>());

Console.WriteLine("Select mode:");
Console.WriteLine("1. Process sample ITCH messages (demo)");
Console.WriteLine("2. Process ITCH file");
Console.WriteLine("3. Listen for ITCH stream (UDP/Multicast) - Not implemented");
Console.Write("\nEnter choice (1, 2, or 3): ");

var choice = Console.ReadLine()?.Trim();

if (choice == "1")
{
    ProcessSampleMessages(consumer, logger);
}
else if (choice == "2")
{
    ProcessItchFile(consumer, logger);
}
else if (choice == "3")
{
    logger.LogWarning("UDP/Multicast streaming not implemented in this demo");
    logger.LogInformation("In production, this would connect to DSE-BD's ITCH feed");
    logger.LogInformation("Typically via MoldUDP64 or SoupBinTCP protocol");
}
else
{
    Console.WriteLine("Invalid choice. Exiting.");
}

static void ProcessSampleMessages(ItchConsumer consumer, ILogger logger)
{
    logger.LogInformation("Processing sample ITCH messages...\n");

    // Generate and process sample messages
    logger.LogInformation("=== Generating System Event ===");
    var systemEvent = ItchConsumer.GenerateSampleMessage(ItchMessageType.SystemEvent);
    consumer.ProcessMessage(systemEvent);

    Thread.Sleep(500);

    logger.LogInformation("\n=== Generating Stock Directory ===");
    var stockDir = ItchConsumer.GenerateSampleMessage(ItchMessageType.StockDirectory);
    consumer.ProcessMessage(stockDir);

    Thread.Sleep(500);

    logger.LogInformation("\n=== Generating Add Order ===");
    var addOrder = ItchConsumer.GenerateSampleMessage(ItchMessageType.AddOrder);
    consumer.ProcessMessage(addOrder);

    Thread.Sleep(500);

    // Generate multiple sample messages
    logger.LogInformation("\n=== Generating Multiple Orders ===");
    for (int i = 0; i < 5; i++)
    {
        var order = ItchConsumer.GenerateSampleMessage(ItchMessageType.AddOrder);
        consumer.ProcessMessage(order);
        Thread.Sleep(200);
    }

    logger.LogInformation("\n=== Processing Complete ===\n");
    consumer.PrintStatistics();
}

static void ProcessItchFile(ItchConsumer consumer, ILogger logger)
{
    Console.Write("\nEnter ITCH file path: ");
    var filePath = Console.ReadLine()?.Trim();

    if (string.IsNullOrEmpty(filePath))
    {
        logger.LogWarning("No file path provided");
        return;
    }

    if (!File.Exists(filePath))
    {
        logger.LogError("File not found: {FilePath}", filePath);
        return;
    }

    try
    {
        logger.LogInformation("Processing ITCH file: {FilePath}", filePath);
        
        using var fileStream = File.OpenRead(filePath);
        consumer.ProcessStream(fileStream);
        
        consumer.PrintStatistics();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error processing ITCH file");
    }
}

