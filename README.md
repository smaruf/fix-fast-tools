# fix-fast-tools

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)

A comprehensive .NET 8.0 application suite for analyzing and decoding FIX/FAST (FIX Adapted for STreaming) binary data and templates. Now available with **CLI**, **Web UI**, and **API** interfaces with flexible deployment options!

## ✨ Features

- **Multiple Interfaces**:
  - 🖥️ **Enhanced CLI** - Interactive command-line tool with rich features
  - 🌐 **Web UI** - Beautiful web interface for decoding messages
  - 🔌 **REST API** - Programmatic access to decoding services
  - 📦 **Original Console Tool** - Classic command-line utility

- **Message Decoding**:
  - Decode FAST-encoded binary messages
  - Parse FAST template XML files
  - Analyze message structure and content
  - Support multiple input formats (Base64, Hex, binary files, JSON)

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

**API Endpoints:**
- `POST /api/FastMessage/decode/base64` - Decode Base64 message
- `POST /api/FastMessage/decode/hex` - Decode Hex message
- `POST /api/FastMessage/decode/file` - Decode binary file upload
- `POST /api/FastMessage/decode/json` - Decode JSON file with multiple messages
- `GET /api/FastMessage/health` - Health check

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
├── run.py                   # Python runner script
├── deploy.sh               # Deployment script
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
