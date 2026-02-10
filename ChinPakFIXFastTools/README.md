# ChinPak FIX/FAST Tools

A comprehensive toolkit for analyzing and processing FIX/FAST messages for the Dhaka Stock Exchange (DSE-BD) and other Bangladesh stock exchanges.

## 🎯 Overview

ChinPak FIX/FAST Tools provides a CLI interface for working with financial message protocols used in Bangladesh stock exchanges:

- **FIX Protocol** - Financial Information eXchange (FIX 4.4) for trading
- **FAST Protocol** - FIX Adapted for STreaming for high-speed data

## ✨ Features

### 1. FIX Message Decoder
- Parse and decode FIX messages
- Field-by-field breakdown with human-readable names
- Support for multiple field separators (SOH, pipe)
- Value translation for common fields (Side, OrdType, etc.)

### 2. Session Log Analyzer
- Comprehensive session log analysis
- Message statistics and breakdown
- Timeline visualization
- Error detection and reporting
- Message type distribution

### 3. FIX Dictionary Viewer
- Complete FIX 4.4 field dictionary
- Message type definitions
- Field lookup by tag
- Search functionality
- Type and description information

### 4. GUI Interface

For a graphical user interface, use the separate **CommonGUI** project:

```bash
cd ../CommonGUI
dotnet run
```

The CommonGUI provides a unified interface for all stock exchanges and protocols.

## 🚀 Quick Start

### Prerequisites

- .NET 8.0 SDK or later
- Terminal.Gui (for GUI version)

### Building

```bash
cd ChinPakFIXFastTools
dotnet restore
dotnet build
```

### Running

#### CLI Version (Interactive Menu)
```bash
dotnet run
```

#### GUI Version
For GUI interface, use the CommonGUI project:
```bash
cd ../CommonGUI
dotnet run
```

## 📖 Usage Guide

### 1. FIX Message Decoder

**CLI Mode:**
```
Select option: 1. Decode FIX Message
Enter FIX message: 8=FIX.4.4|9=154|35=D|49=CLIENT|56=DSE|...
```

**Example Output:**
```
Message Type: New Order Single
===============================================
Tag    | Field Name                | Value
-----------------------------------------------
8      | BeginString              | FIX.4.4
35     | MsgType                  | D
55     | Symbol                   | ACI
54     | Side                     | Buy
38     | OrderQty                 | 100
44     | Price                    | 876.50
===============================================
```

### 2. Session Log Analyzer

Analyzes FIX session log files and generates comprehensive statistics:

```
Select option: 2. Analyze Session Log
Enter log file path: ./logs/session_20240210.log
```

**Output includes:**
- Total messages processed
- Message direction (incoming/outgoing)
- Session events (logon, logout, heartbeats)
- Message type distribution
- Timeline (first/last message, duration)
- Error summary

### 3. FIX Dictionary Viewer

Interactive dictionary lookup:

```
Select option: 3. FIX Dictionary Viewer

Options:
  1. Lookup Field by Tag
  2. Lookup Message by Type
  3. Search Fields
  4. List All Messages
```

**Examples:**
- Lookup tag 55 → Symbol field details
- Lookup message type "D" → New Order Single
- Search "price" → All price-related fields

### 4. GUI Universal Runner

The GUI provides a comprehensive interface with:

**Menu Bar:**
- **File** - Open logs, save output, quit
- **Tools** - Access decoders, parsers, and analyzers
- **Server** - Start/stop protocol servers
- **Help** - About and documentation

**Quick Actions:**
- FIX Decode - Quick FIX message decoding
- FAST Decode - FAST message decoding
- ITCH Parse - ITCH message parsing
- Log Analyze - Session log analysis

**Command Input:**
Type commands directly:
```
decode 8=FIX.4.4|35=D|55=ACI|54=1|38=100
analyze ./logs/session.log
```

## 🔧 Components

### SessionLogAnalyzer.cs
Analyzes FIX session logs and generates statistics:
- Message counting and classification
- Timeline analysis
- Error detection
- Message type distribution

### FixMessageDecoder.cs
Decodes FIX messages with:
- Field parsing
- Value translation
- Message type identification
- Human-readable output

### FixDictionaryViewer.cs
FIX protocol dictionary with:
- Field definitions (60+ common fields)
- Message type definitions
- Search capabilities
- Type information

### Program.cs
CLI interface with:
- Interactive menu system
- Multiple tool access
- User-friendly prompts

### ProgramGUI.cs
Terminal GUI interface with:
- Visual menu system
- Dialog boxes
- Real-time output display
- Multi-protocol support

## 📊 Supported Message Types

### FIX Admin Messages
- **0** - Heartbeat
- **1** - Test Request
- **2** - Resend Request
- **3** - Reject
- **4** - Sequence Reset
- **5** - Logout
- **A** - Logon

### FIX Application Messages
- **D** - New Order Single
- **8** - Execution Report
- **9** - Order Cancel Reject
- **F** - Order Cancel/Replace Request
- **G** - Order Cancel Request
- **H** - Order Status Request
- **j** - Business Message Reject

## 🔌 Integration

### With Other DSE Tools

ChinPak Tools integrates with:
- **FixProtocol.DSE** - FIX client/server for DSE-BD (port 5001)
- **FixProtocol.CSE** - FIX client/server for CSE-BD (port 5002)
- **FastTools.Core** - FAST message decoder core
- **ItchProtocol.DSE** - ITCH market data parser

### Standalone Usage

Can be used independently for:
- Analyzing FIX log files
- Decoding FIX messages
- Learning FIX protocol
- Testing and development

## 📝 Example Scenarios

### Scenario 1: Debug Trading Session
```bash
# Analyze session log
dotnet run
# Select: 2. Analyze Session Log
# Enter: ./logs/server/FIX.4.4-DSE-BD-CLIENT.messages.current.log
```

### Scenario 2: Decode Specific Message
```bash
# Decode message from log
dotnet run
# Select: 1. Decode FIX Message
# Paste FIX message from log
```

### Scenario 3: Learn FIX Protocol
```bash
# Browse FIX dictionary
dotnet run
# Select: 3. FIX Dictionary Viewer
# Select: 4. List All Messages
```

### Scenario 4: GUI Interface
```bash
# Start GUI for all protocols
cd ../CommonGUI
dotnet run
# Use menu: Tools → FIX Decoder
# Use menu: Tools → Log Analyzer
# Use menu: Server → FIX Server (DSE)
```

## 🏗️ Architecture

```
ChinPakFIXFastTools/
├── FixMessageDecoder.cs      - FIX message parser
├── SessionLogAnalyzer.cs     - Log analysis engine
├── FixDictionaryViewer.cs    - FIX dictionary
├── Program.cs                - CLI interface
├── ChinPakFIXFastTools.csproj - Project file
└── README.md                 - Documentation

CommonGUI/                     - Separate GUI project
├── ProgramGUI.cs             - GUI interface for all exchanges
├── CommonGUI.csproj          - GUI project file
└── README.md                 - GUI documentation
```

## 🎨 GUI Interface

For a graphical terminal interface, see the **CommonGUI** project which provides:
- Cross-platform terminal UI
- Unified interface for all stock exchanges
- Support for FIX, FAST, and ITCH protocols
- Interactive menus and dialogs

## 🔐 Exchange Support

### DSE-BD (Dhaka Stock Exchange)
- FIX 4.4 protocol
- Port: 5001 (default)
- Session: FIX.4.4-DSE-BD-CLIENT

### CSE-BD (Chittagong Stock Exchange)
- FIX 4.4 protocol
- Port: 5002 (default)
- Session: FIX.4.4-CSE-BD-CLIENT

## 📚 Additional Resources

- [FIX Protocol Documentation](https://www.fixtrading.org/)
- [FAST Protocol Specification](https://www.fixtrading.org/standards/fast/)
- [NASDAQ ITCH Specification](https://www.nasdaqtrader.com/content/technicalsupport/specifications/dataproducts/NQTVITCHspecification.pdf)

## 🤝 Related Projects

- **CommonGUI** - Universal GUI for all stock exchanges
- **FixProtocol.DSE** - FIX client/server for DSE-BD
- **FixProtocol.CSE** - FIX client/server for CSE-BD
- **FastTools.Core** - FAST message decoder
- **FastTools.CLI** - FAST CLI tool
- **FastTools.Web** - FAST web interface
- **ItchProtocol.DSE** - ITCH parser for DSE-BD

## 📄 License

MIT License - See LICENSE file for details

## 🙏 Acknowledgments

Built for the Bangladesh stock exchange community to facilitate:
- Trading system development
- Protocol analysis and debugging
- Market data processing
- Educational purposes

---

**ChinPak FIX/FAST Tools - CLI Toolkit for Bangladesh Exchanges**
