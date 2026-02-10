# CommonGUI - Universal GUI for Stock Exchanges

A universal graphical terminal interface for all stock exchange tools and protocols.

## 🎯 Overview

CommonGUI provides a unified Terminal.Gui-based interface for working with multiple financial message protocols across different Bangladesh stock exchanges:

- **FIX Protocol** - DSE-BD, CSE-BD
- **FAST Protocol** - High-speed message encoding/decoding
- **ITCH Protocol** - Market data parsing

## ✨ Features

### Multi-Exchange Support
- Dhaka Stock Exchange (DSE-BD)
- Chittagong Stock Exchange (CSE-BD)
- Generic FIX/FAST/ITCH support

### Trading Capabilities
- **Session Login** - Connect to DSE or CSE via FIX protocol
- **Place Orders** - Submit Buy/Sell orders (Market/Limit)
- **Execution Reports** - View and track order executions
- **Market Data Feed** - Subscribe to market data (placeholder)
- **Session Logout** - Disconnect from exchange

### Protocol Tools
- **FIX Decoder** - Decode FIX messages with field translation
- **FAST Decoder** - Decode FAST messages (Base64/Hex)
- **ITCH Parser** - Parse ITCH market data messages
- **Log Analyzer** - Comprehensive session log analysis
- **Dictionary Viewer** - Browse FIX field and message definitions

### Server Management
- Start/stop FIX servers for different exchanges
- FAST server support
- ITCH server support
- Multi-server management

### User Interface
- Cross-platform terminal UI using Terminal.Gui
- Interactive menus and dialogs
- Real-time output display
- File browsing (open/save)
- Quick action buttons
- Keyboard and mouse support

## 🚀 Quick Start

### Prerequisites

- .NET 8.0 SDK or later
- Terminal.Gui (installed automatically via NuGet)
- Reference to ChinPakFIXFastTools project

### Building

```bash
cd CommonGUI
dotnet restore
dotnet build
```

### Running

```bash
dotnet run
```

## 📖 Usage Guide

### Main Interface

When you launch CommonGUI, you'll see:

1. **Menu Bar** (top)
   - File - Open logs, save output, quit
   - Tools - Access decoders, parsers, and analyzers
   - Trading - Session management and order placement
   - Server - Start/stop protocol servers
   - Help - About and documentation

2. **Workspace** (center)
   - Output display area
   - Input field for commands
   - Execute button

3. **Quick Actions** (bottom)
   - FIX Decode
   - FAST Decode
   - ITCH Parse
   - Log Analyze

4. **Status Bar** (bottom)
   - Current status and information

### Menu Options

**Currently Implemented:**

#### File Menu
- **Open Log** - Browse and open session log files ✓
- **Save Output** - Save the current output to a file ✓
- **Quit** - Exit the application ✓

#### Tools Menu
- **FIX Decoder** - Decode FIX messages ✓
- **Log Analyzer** - Analyze session logs ✓
- **Dictionary** - View FIX dictionary ✓

#### Trading Menu
- **Session Login** - Connect to DSE or CSE exchange ✓
- **Place Order** - Submit buy/sell orders (Market/Limit) ✓
- **View Execution Reports** - Display order execution history ✓
- **Market Data Feed** - Subscribe to market data (Placeholder)
- **Session Logout** - Disconnect from exchange ✓

**Planned Features:**

#### Tools Menu (Coming Soon)
- **FAST Decoder** - Decode FAST messages (Base64/Hex)
- **ITCH Parser** - Parse ITCH market data

#### Server Menu (Coming Soon)
- **FIX Server (DSE)** - Start FIX server for DSE-BD
- **FIX Server (CSE)** - Start FIX server for CSE-BD
- **FAST Server** - Start FAST protocol server
- **ITCH Server** - Start ITCH market data server
- **Stop All** - Stop all running servers

### Command Input

You can type commands directly in the input field:

```
decode 8=FIX.4.4|35=D|55=ACI|54=1|38=100
analyze ./logs/session.log
```

### Quick Actions

Use the quick action buttons for common tasks:

1. **FIX Decode** - Opens dialog to decode FIX messages ✓
2. **FAST Decode** - Opens dialog to decode FAST messages (Coming Soon)
3. **ITCH Parse** - Parse ITCH market data (Coming Soon)
4. **Log Analyze** - Opens dialog to analyze log files ✓

## 🔧 Components

### ProgramGUI.cs
Main GUI application with:
- Terminal.Gui interface
- Menu system
- Dialog boxes
- Command processing
- Integration with protocol tools

### TradingSession.cs
Trading session management with:
- FIX session connection/disconnection
- Order placement (Market/Limit)
- Execution report tracking
- Event-based UI updates
- Support for DSE and CSE exchanges

### Integration
Uses components from:
- **ChinPakFIXFastTools** - FIX message decoding and analysis
- **FixProtocol.DSE** - FIX server for DSE-BD
- **FixProtocol.CSE** - FIX server for CSE-BD
- **FastTools.Core** - FAST message processing
- **ItchProtocol.DSE** - ITCH market data parsing

## 📊 Supported Operations

### Currently Implemented

#### FIX Protocol
- ✓ Decode FIX messages
- ✓ Analyze session logs
- ✓ Browse FIX dictionary
- ✓ Connect to exchanges (DSE/CSE)
- ✓ Place orders (Buy/Sell, Market/Limit)
- ✓ View execution reports

### Planned Features

#### FIX Protocol (Coming Soon)
- Start/stop FIX servers (DSE, CSE)

#### FAST Protocol (Coming Soon)
- Decode FAST messages (Base64/Hex)
- Start/stop FAST servers

#### ITCH Protocol (Coming Soon)
- Parse ITCH market data
- Start/stop ITCH servers

## 🏗️ Architecture

```
CommonGUI/
├── ProgramGUI.cs             - Main GUI application
├── TradingSession.cs         - Trading session management
├── CommonGUI.csproj          - Project file
└── README.md                 - Documentation

Dependencies:
├── ChinPakFIXFastTools       - FIX decoding and analysis
├── FixProtocol.DSE           - DSE-BD FIX client/server
├── FixProtocol.CSE           - CSE-BD FIX client/server
├── FastTools.Core            - FAST processing
└── ItchProtocol.DSE          - ITCH parsing
```

## 🎨 Terminal.Gui Features

The interface uses Terminal.Gui providing:
- Cross-platform support (Windows, Linux, macOS)
- Keyboard navigation
- Mouse support
- Dialogs and menus
- Text editing
- File browsing
- Color schemes

## 🔐 Exchange Support (Planned)

The following exchange integrations are planned for future releases:

### DSE-BD (Dhaka Stock Exchange)
- FIX 4.4 protocol
- Default port: 5001
- Session: FIX.4.4-DSE-BD-CLIENT

### CSE-BD (Chittagong Stock Exchange)
- FIX 4.4 protocol
- Default port: 5002
- Session: FIX.4.4-CSE-BD-CLIENT

**Note:** Server functionality requires integration with FixProtocol.DSE/CSE modules.

## 📝 Example Workflows

### Workflow 1: Decode FIX Message
1. Click **FIX Decode** quick action button
2. Enter FIX message in dialog
3. Click **Decode** button
4. View decoded output in workspace

### Workflow 2: Analyze Session Log
1. Use menu: **File → Open Log**
2. Browse and select log file
3. View analysis results in workspace

### Workflow 3: Browse FIX Dictionary
1. Use menu: **Tools → Dictionary**
2. View message types and field definitions
3. Use input commands for specific lookups

### Workflow 4: Place Trading Order
1. Use menu: **Trading → Session Login**
2. Select exchange (DSE or CSE)
3. Browse and select FIX configuration file
4. Click **Connect** and wait for logon
5. Use menu: **Trading → Place Order**
6. Enter symbol, side (BUY/SELL), quantity
7. Select order type (Market or Limit)
8. If Limit order, enter price
9. Confirm and submit order
10. View execution reports via **Trading → View Execution Reports**

### Workflow 5: Monitor Execution Reports
1. After placing orders, use menu: **Trading → View Execution Reports**
2. View formatted table of all execution reports
3. See order status, fills, prices, and messages
4. Click **Refresh** to update display
5. Use **Trading → Session Logout** to disconnect when done

### Workflow 6: Start FIX Server (Planned Feature)
1. Use menu: **Server → FIX Server (DSE)**
2. Currently shows "Not Implemented" message
3. Requires integration with FixProtocol.DSE/CSE modules

## 🤝 Related Projects

- **ChinPakFIXFastTools** - CLI tools for FIX/FAST
- **FixProtocol.DSE** - FIX client/server for DSE-BD
- **FixProtocol.CSE** - FIX client/server for CSE-BD
- **FastTools.Core** - FAST message decoder
- **FastTools.CLI** - FAST CLI tool
- **FastTools.Web** - FAST web interface
- **ItchProtocol.DSE** - ITCH parser for DSE-BD

## 📄 License

MIT License - See LICENSE file for details

## 🙏 Acknowledgments

Built for the Bangladesh stock exchange community to provide:
- Unified interface for all exchanges
- Easy access to protocol tools
- Server management capabilities
- Educational and development support

---

**CommonGUI - Universal Terminal Interface for All Stock Exchanges**
