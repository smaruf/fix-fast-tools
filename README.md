# fix-fast-tools

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)

A comprehensive .NET 8.0 application suite for analyzing and decoding FIX/FAST (FIX Adapted for STreaming) binary data and templates. Now available with **CLI**, **Web UI**, **API** interfaces, plus **FIX Protocol client-server** and **ITCH market data consumer** for Bangladesh stock exchanges!

## ✨ Features

- **Multiple Interfaces**:
  - 🖥️ **Enhanced CLI** - Interactive command-line tool with rich features
  - 🌐 **Web UI** - Beautiful web interface for decoding messages
  - 🔌 **REST API** - Programmatic access to decoding services
  - 📦 **Original Console Tool** - Classic command-line utility
  - 🔧 **FIX Protocol Client/Server** - For DSE-BD and CSE-BD trading
  - 📊 **ITCH Market Data Consumer** - For DSE-BD market data analysis
  - 🎯 **ChinPakFIXFastTools** - Universal FIX/FAST/ITCH tools with CLI
  - 🖼️ **CommonGUI** - Universal graphical interface for all stock exchanges

- **Configuration & Management** (NEW):
  - 📋 **Exchange Configuration System** - JSON-based config for multiple exchanges
  - 🔄 **Config Import/Export** - Share configurations across environments
  - ✅ **Config Validation** - Automatic validation of exchange settings
  - 🎛️ **Web API Management** - Full CRUD operations via REST API
  - 📝 **Default Profiles** - Pre-configured DSE, CSE, and test exchanges

- **Load Testing & Analysis** (NEW):
  - ⚡ **Performance Testing Framework** - Measure throughput and latency
  - 📊 **Real-time Metrics** - Track messages/sec, response times, success rates
  - 📈 **Configurable Scenarios** - Default and high-throughput test profiles
  - 🎯 **Message Distribution** - Customizable order type distribution
  - 📉 **Ramp-up Support** - Gradual load increase for realistic testing

- **Demo & Learning** (NEW):
  - 🎓 **6 Interactive Scenarios** - From basic to advanced trading workflows
  - 📚 **Educational Content** - Step-by-step guided tutorials
  - 🔍 **Category Organization** - Basic, Intermediate, Advanced levels
  - 🎯 **Exchange-specific** - Scenarios for DSE, CSE, ITCH protocols
  - 📖 **Complete Documentation** - Comprehensive guides and examples

- **Message Decoding**:
  - Decode FAST-encoded binary messages
  - Parse FAST template XML files
  - Analyze message structure and content
  - Support multiple input formats (Base64, Hex, binary files, JSON)

- **FIX Protocol Support**:
  - FIX 4.4 client and server implementations
  - Session management with proper logon/logout
  - Order processing (New, Cancel, Replace, Status)
  - Comprehensive message logging for analysis
  - Separate implementations for DSE-BD (port 5001) and CSE-BD (port 5002)

- **ITCH Protocol Support**:
  - NASDAQ ITCH 5.0 message parsing
  - Market data consumption and analysis
  - Order book reconstruction
  - Real-time statistics tracking

- **ChinPakFIXFastTools**:
  - Universal CLI tools for FIX, FAST, and ITCH messages
  - FIX message decoder with human-readable output
  - Session log analyzer with comprehensive statistics
  - FIX field dictionary viewer and search
  - Integration with all DSE/CSE protocol tools

- **CommonGUI**:
  - Universal graphical interface for all stock exchanges
  - Terminal.Gui based visual interface
  - Supports DSE, CSE, and other exchange operations
  - Menu-driven workflow with real-time output
  - File browser and server management

- **Deployment Options**:
  - 🐍 Run with Python script (`run.py`)
  - 🐚 Deploy with shell script (`deploy.sh`)
  - 🐳 Docker support
  - ☁️ Cloud-ready with multiple runtime targets

## 🚀 Quick Start

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- Python 3.6+ (optional, for run.py script)

### Installation

Clone the repository:

```bash
git clone https://github.com/smaruf/fix-fast-tools.git
cd fix-fast-tools
```

## 📖 Usage

### Using Python Runner (Recommended)

The easiest way to run the tools:

```bash
# Show system info and available projects
python3 run.py --info

# Run interactive CLI
python3 run.py --cli

# Run web interface (default port 5000)
python3 run.py --web

# Run web on custom port
python3 run.py --web --port 8080

# Run original console tool
python3 run.py --tools
```

### Using Enhanced CLI

The enhanced CLI offers an interactive mode and improved command-line experience:

```bash
cd FastTools.CLI

# Interactive mode (default)
dotnet run

# Decode Base64 message
dotnet run -- --base64 <base64string>

# Decode Hex message
dotnet run -- --hex <hexstring>

# Decode binary file
dotnet run -- --file <path>

# Decode JSON file
dotnet run -- --json <path>

# Show help
dotnet run -- --help
```

### Using Web Interface

Start the web server:

```bash
cd FastTools.Web
dotnet run
```

Then open your browser to `http://localhost:5000` for the web UI.

**API Endpoints - Message Decoding:**
- `POST /api/FastMessage/decode/base64` - Decode Base64 message
- `POST /api/FastMessage/decode/hex` - Decode Hex message
- `POST /api/FastMessage/decode/file` - Decode binary file upload
- `POST /api/FastMessage/decode/json` - Decode JSON file with multiple messages
- `GET /api/FastMessage/health` - Health check

**API Endpoints - Exchange Configuration:**
- `GET /api/ExchangeConfig` - Get all exchange configs
- `GET /api/ExchangeConfig/{code}` - Get specific exchange
- `POST /api/ExchangeConfig` - Add new exchange
- `PUT /api/ExchangeConfig/{code}` - Update exchange
- `DELETE /api/ExchangeConfig/{code}` - Delete exchange
- `GET /api/ExchangeConfig/{code}/export` - Export config as JSON

**API Endpoints - Load Testing:**
- `POST /api/LoadTest/start` - Start a load test
- `GET /api/LoadTest/{testId}/status` - Get test status
- `GET /api/LoadTest/{testId}/results` - Get detailed metrics
- `GET /api/LoadTest/active` - List active tests

**API Endpoints - Demo Scenarios:**
- `GET /api/DemoScenario` - Get all demo scenarios
- `GET /api/DemoScenario/{id}` - Get specific scenario
- `POST /api/DemoScenario/{id}/execute` - Execute scenario
- `GET /api/DemoScenario/categories` - Get all categories
- `GET /api/DemoScenario/summary` - Get summary statistics

See [CONFIG_LOADTEST_DEMO_GUIDE.md](CONFIG_LOADTEST_DEMO_GUIDE.md) for detailed API documentation.

### Using Original Console Tool

The original command-line tool:

```bash
cd Tools

# Run with default sample files
dotnet run

# Decode Base64 encoded message
dotnet run -- --base64 <base64string>

# Decode Hex encoded message
dotnet run -- --hex <hexstring>

# Decode binary file
dotnet run -- --file <path>

# Process JSON file with FAST messages
dotnet run -- --json <path>
```

### Using FIX Protocol Client/Server (NEW)

#### For DSE-BD (Dhaka Stock Exchange):

```bash
cd FixProtocol.DSE
dotnet run

# Select: 1 for Server (listens on port 5001)
# Select: 2 for Client (connects to port 5001)
```

#### For CSE-BD (Chittagong Stock Exchange):

```bash
cd FixProtocol.CSE
dotnet run

# Select: 1 for Server (listens on port 5002)
# Select: 2 for Client (connects to port 5002)
```

**Features:**
- Session management with logon/logout
- Order processing (New, Cancel, Replace, Status)
- Execution reports
- Comprehensive logging in `./logs/` and session data in `./data/`

### Using ITCH Market Data Consumer (NEW)

```bash
cd ItchProtocol.DSE
dotnet run

# Select: 1 for sample messages (demo mode)
# Select: 2 to process ITCH file
```

**Features:**
- Parse NASDAQ ITCH 5.0 messages
- Track stocks, orders, and trades
- Real-time statistics

### Using ChinPakFIXFastTools (NEW)

```bash
cd ChinPakFIXFastTools
dotnet run

# Interactive CLI menu offers:
# 1. Decode FIX messages
# 2. Analyze session logs
# 3. View FIX field dictionary
# 4. Launch Common GUI
# 5. About
```

**Features:**
- FIX message decoder with human-readable field names
- Session log analyzer with comprehensive statistics
- Browse FIX data dictionary (60+ fields and message types)
- Search fields by keyword or lookup by tag
- Integrated with CommonGUI for visual interface

See [ChinPakFIXFastTools/README.md](ChinPakFIXFastTools/README.md) for detailed documentation.

### Using CommonGUI (NEW)

```bash
cd CommonGUI
dotnet run

# Or from ChinPakFIXFastTools CLI menu, select option 4
```

**Features:**
- Universal graphical interface for all stock exchanges (DSE, CSE, etc.)
- Terminal.Gui based visual interface
- Menu-driven workflow (File, Tools, Servers, Help)
- FIX/FAST/ITCH message decoding and operations
- Server management for DSE/CSE FIX, FAST, and ITCH
- Real-time output display and file operations
- Visual file browser

See [CommonGUI/README.md](CommonGUI/README.md) for detailed documentation.

See [FIX_ITCH_README.md](FIX_ITCH_README.md) for detailed documentation on FIX and ITCH implementations.

## 🔧 Configuration & Testing (NEW)

### Exchange Configuration Management

Manage multiple stock exchange configurations via JSON files or Web API:

```bash
# View current exchange configurations
curl http://localhost:5000/api/ExchangeConfig

# Get specific exchange (DSE, CSE, TEST)
curl http://localhost:5000/api/ExchangeConfig/DSE

# Export configuration
curl http://localhost:5000/api/ExchangeConfig/DSE/export > dse-backup.json

# Import configuration
curl -X POST http://localhost:5000/api/ExchangeConfig/import \
  -H "Content-Type: application/json" \
  -d @new-exchange.json
```

**Default Exchanges:**
- **DSE** - Dhaka Stock Exchange (FIX 4.4, port 5001)
- **CSE** - Chittagong Stock Exchange (FIX 4.4, port 5002)
- **DSE-ITCH** - DSE Market Data (ITCH 5.0, port 6001)
- **DSE-FAST** - DSE Market Data (FAST 1.1, port 6002)
- **TEST** - Sample Test Exchange (FIX 4.4, port 5999)

Configuration files are located in `configs/exchanges.json`.

### Load Testing & Performance Analysis

Run performance tests to measure throughput and latency:

```bash
# Start a default load test (600 messages, 10 msg/sec)
curl -X POST http://localhost:5000/api/LoadTest/start \
  -H "Content-Type: application/json" \
  -d @configs/loadtest-default.json

# Check test status
curl http://localhost:5000/api/LoadTest/{testId}/status

# Get detailed metrics
curl http://localhost:5000/api/LoadTest/{testId}/results
```

**Load Test Metrics:**
- Messages sent/received/failed
- Average/Min/Max latency (milliseconds)
- Throughput (messages per second)
- Success rate percentage
- Per-message latency tracking

**Pre-configured Test Scenarios:**
- `loadtest-default.json` - Standard test (600 msgs @ 10/sec)
- `loadtest-high-throughput.json` - Stress test (12000 msgs @ 100/sec)

### Demo Scenarios & Learning

6 interactive scenarios from basic to advanced:

```bash
# List all scenarios
curl http://localhost:5000/api/DemoScenario

# Get scenario categories (Basic, Intermediate, Advanced)
curl http://localhost:5000/api/DemoScenario/categories

# Execute a scenario
curl -X POST http://localhost:5000/api/DemoScenario/basic-order-001/execute
```

**Available Scenarios:**
1. **Basic Order Placement** - Simple buy/sell orders
2. **Market Data Consumption** - ITCH or FAST protocol usage
3. **FIX Session Management** - Logon/logout/heartbeats
4. **Order Cancel and Replace** - Modify existing orders
5. **Error Handling** - Recovery from failures
6. **Performance Testing** - Benchmark throughput

See [CONFIG_LOADTEST_DEMO_GUIDE.md](CONFIG_LOADTEST_DEMO_GUIDE.md) for complete documentation.

## 🚢 Deployment

### Using Deployment Script

```bash
# Build all projects
./deploy.sh build

# Publish for local deployment
./deploy.sh publish ./output

# Create standalone executables
./deploy.sh standalone ./bin linux-x64   # or win-x64, osx-x64

# Build Docker image
./deploy.sh docker fasttools latest

# Create docker-compose.yml
./deploy.sh compose

# Build, test, and publish everything
./deploy.sh all
```

### Docker Deployment

```bash
# Create Dockerfile and docker-compose.yml
./deploy.sh compose

# Build and run with Docker Compose
docker-compose up -d

# Or build manually
./deploy.sh docker
docker run -p 5000:8080 fasttools:latest
```

### Cloud Deployment

The application can be deployed to any platform that supports .NET 8.0:

- **Azure App Service**
- **AWS Elastic Beanstalk**
- **Google Cloud Run**
- **Heroku**
- **DigitalOcean App Platform**

Use the standalone publish option for platform-specific deployments:

```bash
./deploy.sh standalone ./publish <runtime-identifier>
```

## 📁 Project Structure

```
fix-fast-tools/
├── FastTools.Core/          # Core library with shared logic
│   ├── Models/              # Data models
│   └── Services/            # FAST message decoder service
├── FastTools.CLI/           # Enhanced CLI application
├── FastTools.Web/           # Web API and UI
│   ├── Controllers/         # API controllers
│   └── wwwroot/            # Web UI (HTML/CSS/JS)
├── Tools/                   # Original console application
│   ├── Program.cs           # Main application entry point
│   ├── FAST_TEMPLATE.xml    # FAST template definition
│   └── [sample files]       # Sample data files
├── FixProtocol.DSE/         # FIX Protocol for DSE-BD
│   ├── FixServer.cs         # FIX server implementation
│   ├── FixClient.cs         # FIX client implementation
│   └── Program.cs           # CLI interface
├── FixProtocol.CSE/         # FIX Protocol for CSE-BD
│   ├── FixServer.cs         # FIX server implementation
│   ├── FixClient.cs         # FIX client implementation
│   └── Program.cs           # CLI interface
├── ItchProtocol.DSE/        # ITCH Consumer for DSE-BD
│   ├── ItchMessages.cs      # ITCH message structures
│   ├── ItchConsumer.cs      # ITCH message parser
│   └── Program.cs           # CLI interface
├── ChinPakFIXFastTools/     # Universal FIX/FAST/ITCH Tools (NEW)
│   ├── FixMessageDecoder.cs # FIX message decoder
│   ├── SessionLogAnalyzer.cs # Session log analyzer
│   ├── FixDictionaryViewer.cs # FIX dictionary viewer
│   ├── Program.cs           # Interactive CLI
│   └── README.md            # Tool documentation
├── CommonGUI/               # Universal GUI for all stock exchanges (NEW)
│   ├── ProgramGUI.cs        # Terminal.Gui application
│   ├── CommonGUI.csproj     # Project file
│   └── README.md            # GUI documentation
├── run.py                   # Python runner script
├── deploy.sh               # Deployment script
├── FIX_ITCH_README.md      # FIX/ITCH documentation
├── LICENSE                  # MIT License
└── README.md               # This file
```

## 📋 Sample Files

The `Tools` directory includes several sample files:

- `FAST_TEMPLATE.xml` - FAST template definition file
- `FastIncomingMessage251126-ACI.json` - Sample FAST messages in JSON format
- `sample_messages.dat` - Sample binary message log
- `security_definition_2025-11-26.json` - Security definition data

## 🔧 Development

### Building from Source

```bash
# Build all projects
dotnet build

# Build specific project
dotnet build FastTools.Web/FastTools.Web.csproj

# Run tests (if available)
dotnet test
```

### Adding New Features

The modular architecture makes it easy to extend:

1. **Core Logic**: Add to `FastTools.Core/Services/`
2. **CLI Features**: Extend `FastTools.CLI/Program.cs`
3. **Web API**: Add controllers in `FastTools.Web/Controllers/`
4. **Web UI**: Modify `FastTools.Web/wwwroot/index.html`

## 🌐 Related Resources

- [FIX Protocol](https://www.fixtrading.org/) - Official FIX Trading Community
- [FAST Protocol Specification](https://www.fixtrading.org/standards/fast/) - FIX Adapted for STreaming specification
- [OpenFAST](https://github.com/openfast/openfast) - Open source FAST protocol implementation
- [QuickFIX/n](https://quickfixengine.org/n/) - FIX protocol engine for .NET
- [NASDAQ ITCH Specification](https://www.nasdaqtrader.com/content/technicalsupport/specifications/dataproducts/NQTVITCHspecification.pdf) - ITCH 5.0 protocol
- [DSE Official](https://www.dsebd.org/) - Dhaka Stock Exchange
- [CSE Official](https://www.cse.com.bd/) - Chittagong Stock Exchange

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📞 Support

For issues, questions, or contributions, please open an issue on GitHub.
