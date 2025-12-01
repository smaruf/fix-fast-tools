# fix-fast-tools

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)

A .NET 8.0 console application for analyzing and decoding FIX/FAST (FIX Adapted for STreaming) binary data and templates.

## Overview

This tool helps developers and analysts work with FIX/FAST protocol messages by:

- Decoding FAST-encoded binary messages
- Parsing FAST template XML files
- Analyzing message structure and content
- Supporting multiple input formats (Base64, Hex, binary files, JSON)

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later

## Installation

Clone the repository:

```bash
git clone https://github.com/smaruf/fix-fast-tools.git
cd fix-fast-tools
```

Build the project:

```bash
cd Tools
dotnet build
```

## Usage

### Run with default sample files

```bash
dotnet run
```

This will process the included sample files (`FastIncomingMessage251126-ACI.json` and `sample_messages.dat`).

### Command Line Options

#### Decode Base64 encoded message

```bash
dotnet run -- --base64 <base64string>
```

#### Decode Hex encoded message

```bash
dotnet run -- --hex <hexstring>
```

#### Decode binary file

```bash
dotnet run -- --file <path>
```

#### Process JSON file with FAST messages

```bash
dotnet run -- --json <path>
```

## Sample Files

The `Tools` directory includes several sample files:

- `FAST_TEMPLATE.xml` - FAST template definition file
- `FastIncomingMessage251126-ACI.json` - Sample FAST messages in JSON format
- `sample_messages.dat` - Sample binary message log
- `security_definition_2025-11-26.json` - Security definition data

## Related Resources

- [FIX Protocol](https://www.fixtrading.org/) - Official FIX Trading Community
- [FAST Protocol Specification](https://www.fixtrading.org/standards/fast/) - FIX Adapted for STreaming specification
- [OpenFAST](https://github.com/openfast/openfast) - Open source FAST protocol implementation

## Project Structure

```
fix-fast-tools/
├── Tools/
│   ├── Program.cs           # Main application entry point
│   ├── Tools.csproj         # Project file
│   ├── OpenFast/            # OpenFAST library sources (excluded from build)
│   └── [sample files]
├── LICENSE                  # MIT License
└── README.md               # This file
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request
