# FIX Protocol and ITCH Consumer for Bangladesh Stock Exchanges

This directory contains FIX protocol client-server implementations and ITCH market data consumer for Bangladesh stock exchanges (DSE-BD and CSE-BD).

## Overview

### Projects

1. **FixProtocol.DSE** - FIX Protocol Client/Server for Dhaka Stock Exchange (DSE-BD)
2. **FixProtocol.CSE** - FIX Protocol Client/Server for Chittagong Stock Exchange (CSE-BD)
3. **ItchProtocol.DSE** - ITCH Protocol Consumer for DSE-BD market data

## FIX Protocol Implementation

The FIX (Financial Information eXchange) protocol implementation uses QuickFIX/n, the industry-standard .NET implementation of the FIX protocol.

### Features

- **Server Mode (Acceptor)**: Accepts connections from FIX clients, processes orders, and sends execution reports
- **Client Mode (Initiator)**: Connects to FIX servers and sends orders
- **Session Management**: Proper logon/logout handling with sequence number management
- **Message Logging**: Comprehensive logging of all FIX messages (admin and application)
- **Order Processing**: Support for:
  - New Order Single (D)
  - Order Cancel Request (G)
  - Order Cancel/Replace Request (F)
  - Order Status Request (H)
  - Execution Reports (8)

### DSE-BD FIX Protocol

#### Running as Server

```bash
cd FixProtocol.DSE
dotnet run

# Select option: 1 (Run as FIX Server)
```

The server will:
- Listen on port **5001**
- Create session logs in `./logs/server/`
- Create session data in `./data/server/`
- Accept connections from FIX clients

#### Running as Client

```bash
cd FixProtocol.DSE
dotnet run

# Select option: 2 (Run as FIX Client)
```

The client will:
- Connect to localhost:5001 (configurable)
- Provide interactive menu to send test orders or custom orders
- Log all messages in `./logs/client/`
- Store session data in `./data/client/`

### CSE-BD FIX Protocol

#### Running as Server

```bash
cd FixProtocol.CSE
dotnet run

# Select option: 1 (Run as FIX Server)
```

The server will:
- Listen on port **5002**
- Create session logs in `./logs/server/`
- Create session data in `./data/server/`
- Accept connections from FIX clients

#### Running as Client

```bash
cd FixProtocol.CSE
dotnet run

# Select option: 2 (Run as FIX Client)
```

The client will:
- Connect to localhost:5002 (configurable)
- Provide interactive menu to send test orders or custom orders

## ITCH Protocol Implementation

The ITCH (NASDAQ TotalView-ITCH 5.0) protocol consumer parses binary market data messages from DSE-BD.

### Features

- **Message Parsing**: Supports multiple ITCH 5.0 message types:
  - System Event (S)
  - Stock Directory (R)
  - Add Order (A)
  - Order Executed (E)
  - Trade (P)
  - And more...
- **Market Data Processing**: Real-time order book construction
- **Statistics**: Track message counts, stocks, and active orders
- **Stream Processing**: Handle continuous market data feeds

### Running ITCH Consumer

```bash
cd ItchProtocol.DSE
dotnet run
```

Options:
1. **Process sample ITCH messages** - Demo mode with generated messages
2. **Process ITCH file** - Parse ITCH messages from a binary file
3. **Listen for ITCH stream** - (Not implemented - placeholder for UDP/Multicast)

#### Example Output

```
=== System Event ===
System Event: Start of Messages at 00:00:00.000000

=== Stock Directory ===
Stock Directory: ACI - Category: Q, RoundLot: 100

=== Add Order ===
Add Order: ACI BUY 100@876.5000 (Ref: 12345)

=== ITCH Consumer Statistics ===
Messages Processed: 10
Messages with Errors: 0
Stocks in Directory: 1
Active Orders: 5
```

## Testing the Implementation

### Test FIX Client-Server Connection

1. **Terminal 1** - Start DSE Server:
   ```bash
   cd FixProtocol.DSE
   dotnet run
   # Select: 1 (Server)
   ```

2. **Terminal 2** - Start DSE Client:
   ```bash
   cd FixProtocol.DSE
   dotnet run
   # Select: 2 (Client)
   # Then: 1 (Send test order for ACI stock)
   ```

3. **Observe**: 
   - Server logs will show incoming order
   - Client logs will show execution report
   - Both will maintain session with heartbeats

### Test ITCH Consumer

```bash
cd ItchProtocol.DSE
dotnet run
# Select: 1 (Process sample messages)
```

## Session Management and Logging

### FIX Session Logs

All FIX messages are logged in two locations:

1. **File Logs**: `./logs/server/` or `./logs/client/`
   - Contains all FIX messages with timestamps
   - Useful for debugging and audit

2. **Session Store**: `./data/server/` or `./data/client/`
   - Stores sequence numbers
   - Enables session recovery after disconnection

### Console Logs

Both FIX and ITCH applications use Microsoft.Extensions.Logging with console output, showing:
- Information: Major events (logon, orders, trades)
- Debug: Detailed message content
- Errors: Any processing errors

## Configuration

### FIX Protocol Settings

Sessions are configured programmatically in `Program.cs`:

```csharp
// Server Configuration
dictionary.SetString("ConnectionType", "acceptor");
dictionary.SetString("SocketAcceptPort", "5001");  // DSE: 5001, CSE: 5002
dictionary.SetString("FileStorePath", "./data/server");
dictionary.SetString("FileLogPath", "./logs/server");

// Client Configuration
dictionary.SetString("ConnectionType", "initiator");
dictionary.SetString("SocketConnectHost", "localhost");
dictionary.SetString("SocketConnectPort", "5001");
dictionary.SetLong("HeartBtInt", 30);  // Heartbeat interval
```

### Customizing for Real Exchanges

To connect to actual DSE-BD or CSE-BD FIX servers:

1. Update `SocketConnectHost` with exchange IP
2. Update `SocketConnectPort` with exchange port
3. Update `SenderCompID` and `TargetCompID` with your credentials
4. Add authentication if required by the exchange
5. Set `UseDataDictionary` to true and provide FIX data dictionary

## Dependencies

### NuGet Packages

- **QuickFIXn.Core** (1.14.0) - Core FIX protocol engine
- **QuickFIXn.FIX4.4** (1.13.0) - FIX 4.4 message definitions
- **Microsoft.Extensions.Logging** (10.0.2) - Logging framework
- **Microsoft.Extensions.Logging.Console** (10.0.2) - Console logging

All packages are vulnerability-free and production-ready.

## Architecture

### FIX Protocol Stack

```
┌─────────────────────────────────┐
│  Application (Orders, Reports)  │
├─────────────────────────────────┤
│  FIX Application Layer          │  ← Our Implementation
│  (IApplication interface)       │
├─────────────────────────────────┤
│  QuickFIX/n Engine              │  ← QuickFIXn library
│  (Session, Transport, Codec)    │
├─────────────────────────────────┤
│  TCP/IP Network                 │
└─────────────────────────────────┘
```

### ITCH Protocol Stack

```
┌─────────────────────────────────┐
│  Market Data Consumer           │  ← Our Implementation
│  (Order Book, Statistics)       │
├─────────────────────────────────┤
│  ITCH Message Parser            │  ← Our Implementation
│  (Binary decode)                │
├─────────────────────────────────┤
│  Stream/File Input              │
│  (UDP Multicast or File)        │
└─────────────────────────────────┘
```

## Future Enhancements

1. **FIX Protocol**:
   - Add market data subscription (FIX 4.4 Market Data Request)
   - Implement position reports
   - Add support for FIX 5.0
   - SSL/TLS encryption for production

2. **ITCH Protocol**:
   - Implement MoldUDP64 or SoupBinTCP session layer
   - Add order book reconstruction
   - Real-time market data statistics
   - UDP multicast listener for live feeds

3. **General**:
   - Add unit tests
   - Configuration file support (appsettings.json)
   - Database storage for messages
   - Web dashboard for monitoring

## Support and Resources

- [QuickFIX/n Documentation](https://quickfixengine.org/n/documentation/)
- [FIX Protocol Specification](https://www.fixtrading.org/standards/)
- [NASDAQ ITCH 5.0 Specification](https://www.nasdaqtrader.com/content/technicalsupport/specifications/dataproducts/NQTVITCHspecification.pdf)
- [DSE Official Website](https://www.dsebd.org/)
- [CSE Official Website](https://www.cse.com.bd/)

## License

MIT License - See LICENSE file in the root directory
