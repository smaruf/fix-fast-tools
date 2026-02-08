using QuickFix;
using QuickFix.Transport;
using QuickFix.Fields;
using QuickFix.Store;
using QuickFix.Logger;
using Microsoft.Extensions.Logging;
using FixProtocol.CSE;

Console.WriteLine("==============================================");
Console.WriteLine("FIX Protocol Client/Server for CSE-BD");
Console.WriteLine("Chittagong Stock Exchange - Bangladesh");
Console.WriteLine("==============================================\n");

// Configure logging
using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .SetMinimumLevel(LogLevel.Information);
});

Console.WriteLine("Select mode:");
Console.WriteLine("1. Run as FIX Server (Acceptor)");
Console.WriteLine("2. Run as FIX Client (Initiator)");
Console.Write("\nEnter choice (1 or 2): ");

var choice = Console.ReadLine()?.Trim();

if (choice == "1")
{
    RunServer(loggerFactory);
}
else if (choice == "2")
{
    RunClient(loggerFactory);
}
else
{
    Console.WriteLine("Invalid choice. Exiting.");
}

static void RunServer(ILoggerFactory loggerFactory)
{
    var logger = loggerFactory.CreateLogger<Program>();
    logger.LogInformation("Starting FIX Server for CSE-BD...");

    // Create server configuration
    var settings = new SessionSettings();
    var sessionID = new SessionID("FIX.4.4", "CSE-BD", "CLIENT");
    var dictionary = new SettingsDictionary();
    
    dictionary.SetString("ConnectionType", "acceptor");
    dictionary.SetString("SocketAcceptPort", "5002");
    dictionary.SetString("StartTime", "00:00:00");
    dictionary.SetString("EndTime", "23:59:59");
    dictionary.SetBool("UseDataDictionary", false);
    dictionary.SetString("FileStorePath", "./data/server");
    dictionary.SetString("FileLogPath", "./logs/server");
    
    settings.Set(sessionID, dictionary);

    // Create directories if they don't exist
    Directory.CreateDirectory("./data/server");
    Directory.CreateDirectory("./logs/server");

    var application = new FixServer(loggerFactory.CreateLogger<FixServer>());
    var storeFactory = new FileStoreFactory(settings);
    var logFactory = new FileLogFactory(settings);
    var acceptor = new ThreadedSocketAcceptor(application, storeFactory, settings, logFactory);

    try
    {
        acceptor.Start();
        logger.LogInformation("FIX Server started successfully!");
        logger.LogInformation("Listening on port 5002");
        logger.LogInformation("Session: {SessionID}", sessionID);
        logger.LogInformation("\nPress Ctrl+C to stop the server...\n");

        // Keep the server running
        var cancellationTokenSource = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cancellationTokenSource.Cancel();
        };

        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            Thread.Sleep(1000);
        }

        logger.LogInformation("\nShutting down server...");
        acceptor.Stop();
        logger.LogInformation("Server stopped.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error running FIX server");
    }
}

static void RunClient(ILoggerFactory loggerFactory)
{
    var logger = loggerFactory.CreateLogger<Program>();
    logger.LogInformation("Starting FIX Client for CSE-BD...");

    // Create client configuration
    var settings = new SessionSettings();
    var sessionID = new SessionID("FIX.4.4", "CLIENT", "CSE-BD");
    var dictionary = new SettingsDictionary();
    
    dictionary.SetString("ConnectionType", "initiator");
    dictionary.SetString("SocketConnectHost", "localhost");
    dictionary.SetString("SocketConnectPort", "5002");
    dictionary.SetString("StartTime", "00:00:00");
    dictionary.SetString("EndTime", "23:59:59");
    dictionary.SetLong("HeartBtInt", 30);
    dictionary.SetBool("UseDataDictionary", false);
    dictionary.SetString("FileStorePath", "./data/client");
    dictionary.SetString("FileLogPath", "./logs/client");
    dictionary.SetLong("ReconnectInterval", 5);
    
    settings.Set(sessionID, dictionary);

    // Create directories if they don't exist
    Directory.CreateDirectory("./data/client");
    Directory.CreateDirectory("./logs/client");

    var application = new FixClient(loggerFactory.CreateLogger<FixClient>());
    var storeFactory = new FileStoreFactory(settings);
    var logFactory = new FileLogFactory(settings);
    var initiator = new SocketInitiator(application, storeFactory, settings, logFactory);

    try
    {
        initiator.Start();
        logger.LogInformation("FIX Client started successfully!");
        logger.LogInformation("Connecting to localhost:5002");
        logger.LogInformation("Session: {SessionID}", sessionID);
        logger.LogInformation("\nWaiting for connection...\n");

        // Wait for logon
        var timeout = DateTime.Now.AddSeconds(10);
        while (!application.IsLoggedOn && DateTime.Now < timeout)
        {
            Thread.Sleep(500);
        }

        if (application.IsLoggedOn)
        {
            logger.LogInformation("Successfully connected to CSE-BD FIX Server!\n");
            
            // Interactive menu for sending orders
            var running = true;
            while (running)
            {
                Console.WriteLine("\nOptions:");
                Console.WriteLine("1. Send test order (sample stock)");
                Console.WriteLine("2. Send custom order");
                Console.WriteLine("3. Exit");
                Console.Write("\nEnter choice: ");
                
                var option = Console.ReadLine()?.Trim();
                
                switch (option)
                {
                    case "1":
                        logger.LogInformation("Sending test order...");
                        application.SendNewOrder("TESTSTOCK", "BUY", 50, 100.00m);
                        break;
                        
                    case "2":
                        SendCustomOrder(application, logger);
                        break;
                        
                    case "3":
                        running = false;
                        break;
                        
                    default:
                        Console.WriteLine("Invalid choice.");
                        break;
                }
            }
        }
        else
        {
            logger.LogWarning("Failed to connect to server within timeout period");
        }

        logger.LogInformation("\nShutting down client...");
        initiator.Stop();
        logger.LogInformation("Client stopped.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error running FIX client");
    }
}

static void SendCustomOrder(FixClient client, ILogger logger)
{
    try
    {
        Console.Write("Enter symbol: ");
        var symbol = Console.ReadLine()?.Trim()?.ToUpper();
        
        Console.Write("Enter side (BUY/SELL): ");
        var side = Console.ReadLine()?.Trim()?.ToUpper();
        
        Console.Write("Enter quantity: ");
        var quantityStr = Console.ReadLine()?.Trim();
        
        Console.Write("Enter price (leave empty for market order): ");
        var priceStr = Console.ReadLine()?.Trim();
        
        if (string.IsNullOrEmpty(symbol) || string.IsNullOrEmpty(side) || string.IsNullOrEmpty(quantityStr))
        {
            logger.LogWarning("Invalid input. Order not sent.");
            return;
        }
        
        if (!decimal.TryParse(quantityStr, out var quantity))
        {
            logger.LogWarning("Invalid quantity. Order not sent.");
            return;
        }
        
        decimal? price = null;
        if (!string.IsNullOrEmpty(priceStr) && decimal.TryParse(priceStr, out var parsedPrice))
        {
            price = parsedPrice;
        }
        
        client.SendNewOrder(symbol, side, quantity, price);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error sending custom order");
    }
}
