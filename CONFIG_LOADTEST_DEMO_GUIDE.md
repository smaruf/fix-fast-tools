# Configuration, Load Testing, and Demo Scenarios

This document describes the new configuration system, load testing framework, and demo scenarios for FIX/FAST/ITCH stock exchange tools.

## Overview

The toolkit now provides:
- **Configuration System**: JSON-based configuration for multiple stock exchanges
- **Load Testing**: Performance testing framework with metrics
- **Demo Scenarios**: Pre-built educational scenarios for learning

## Configuration System

### Exchange Configuration

Exchange configurations are stored in `configs/exchanges.json` and can be managed via:
- Web API endpoints
- CLI commands
- Direct file editing

#### Configuration Structure

```json
{
  "Name": "Dhaka Stock Exchange",
  "Code": "DSE",
  "Country": "Bangladesh",
  "Description": "DSE-BD - Dhaka Stock Exchange, Bangladesh",
  "IsEnabled": true,
  "Protocol": {
    "Type": "FIX",
    "Version": "4.4",
    "Connection": {
      "Host": "localhost",
      "Port": 5001,
      "UseSsl": false,
      "TimeoutSeconds": 30,
      "HeartbeatIntervalSeconds": 30
    },
    "Session": {
      "SenderCompId": "CLIENT1",
      "TargetCompId": "DSE-BD",
      "FileStorePath": "./data/dse",
      "FileLogPath": "./logs/dse",
      "UseDataDictionary": false
    }
  }
}
```

### Web API Endpoints

#### Exchange Configuration

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/ExchangeConfig` | Get all exchange configs |
| GET | `/api/ExchangeConfig/{code}` | Get specific exchange config |
| GET | `/api/ExchangeConfig/protocol/{type}` | Get exchanges by protocol type |
| POST | `/api/ExchangeConfig` | Add new exchange config |
| PUT | `/api/ExchangeConfig/{code}` | Update exchange config |
| DELETE | `/api/ExchangeConfig/{code}` | Delete exchange config |
| POST | `/api/ExchangeConfig/{code}/validate` | Validate exchange config |
| POST | `/api/ExchangeConfig/import` | Import config from JSON |
| GET | `/api/ExchangeConfig/{code}/export` | Export config as JSON |

#### Load Testing

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/LoadTest/start` | Start a load test |
| GET | `/api/LoadTest/{testId}/status` | Get test status |
| GET | `/api/LoadTest/{testId}/results` | Get test results |
| GET | `/api/LoadTest/active` | Get all active tests |
| POST | `/api/LoadTest/configs/default` | Get default load test config |
| POST | `/api/LoadTest/configs/high-throughput` | Get high-throughput config |
| DELETE | `/api/LoadTest/{testId}` | Clean up completed test |

#### Demo Scenarios

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/DemoScenario` | Get all demo scenarios |
| GET | `/api/DemoScenario/{id}` | Get specific scenario |
| GET | `/api/DemoScenario/category/{category}` | Get scenarios by category |
| GET | `/api/DemoScenario/categories` | Get all categories |
| POST | `/api/DemoScenario/{id}/execute` | Execute a scenario |
| POST | `/api/DemoScenario/import` | Import scenario from JSON |
| GET | `/api/DemoScenario/{id}/export` | Export scenario as JSON |
| GET | `/api/DemoScenario/summary` | Get scenarios summary |

## Load Testing

### Quick Start

```bash
# Using Web API
curl -X POST http://localhost:5000/api/LoadTest/start \
  -H "Content-Type: application/json" \
  -d @configs/loadtest-default.json

# Check status
curl http://localhost:5000/api/LoadTest/{testId}/status

# Get results
curl http://localhost:5000/api/LoadTest/{testId}/results
```

### Load Test Configuration

```json
{
  "Name": "Default Load Test",
  "Description": "Standard load test with moderate throughput",
  "ExchangeCode": "DSE",
  "Scenario": {
    "DurationSeconds": 60,
    "MessagesPerSecond": 10,
    "TotalMessages": 600,
    "RampUp": true,
    "RampUpSeconds": 10,
    "Distribution": {
      "NewOrderPercent": 60,
      "CancelPercent": 20,
      "ReplacePercent": 15,
      "StatusPercent": 5
    }
  }
}
```

### Performance Metrics

The load testing framework collects:
- **Throughput**: Messages per second
- **Latency**: Min, max, average response times
- **Success Rate**: Messages sent vs received
- **Failure Rate**: Failed message count

Example metrics output:
```json
{
  "MessagesSent": 600,
  "MessagesReceived": 570,
  "MessagesFailed": 30,
  "AverageLatencyMs": 15.5,
  "MinLatencyMs": 5.2,
  "MaxLatencyMs": 45.8,
  "ThroughputMps": 9.5,
  "StartTime": "2026-02-11T10:00:00Z",
  "EndTime": "2026-02-11T10:01:00Z"
}
```

## Demo Scenarios

### Available Scenarios

1. **Basic Order Placement** (`basic-order-001`)
   - Category: Basic
   - Learn how to place simple buy/sell orders

2. **Market Data Consumption** (`market-data-001`)
   - Category: Basic
   - Process real-time market data via ITCH or FAST protocol

3. **FIX Session Management** (`session-mgmt-001`)
   - Category: Intermediate
   - Understand FIX session lifecycle

4. **Order Cancel and Replace** (`cancel-replace-001`)
   - Category: Intermediate
   - Modify existing orders

5. **Error Handling and Recovery** (`error-handling-001`)
   - Category: Advanced
   - Handle errors and failures

6. **Performance Testing** (`perf-test-001`)
   - Category: Advanced
   - Run performance benchmarks

### Using Demo Scenarios

```bash
# Get all scenarios
curl http://localhost:5000/api/DemoScenario

# Get specific scenario
curl http://localhost:5000/api/DemoScenario/basic-order-001

# Execute a scenario
curl -X POST http://localhost:5000/api/DemoScenario/basic-order-001/execute

# Get scenarios by category
curl http://localhost:5000/api/DemoScenario/category/Basic
```

## Examples

### Adding a New Exchange

```bash
curl -X POST http://localhost:5000/api/ExchangeConfig \
  -H "Content-Type: application/json" \
  -d '{
    "Name": "New Stock Exchange",
    "Code": "NSE",
    "Country": "Country",
    "Description": "Description",
    "IsEnabled": true,
    "Protocol": {
      "Type": "FIX",
      "Version": "4.4",
      "Connection": {
        "Host": "localhost",
        "Port": 5003,
        "UseSsl": false,
        "TimeoutSeconds": 30,
        "HeartbeatIntervalSeconds": 30
      },
      "Session": {
        "SenderCompId": "CLIENT1",
        "TargetCompId": "NSE",
        "FileStorePath": "./data/nse",
        "FileLogPath": "./logs/nse",
        "UseDataDictionary": false
      }
    }
  }'
```

### Running a High-Throughput Test

```bash
# Get high-throughput config template
curl -X POST http://localhost:5000/api/LoadTest/configs/high-throughput \
  -H "Content-Type: application/json" \
  -d '"DSE"' > high-throughput.json

# Start the test
curl -X POST http://localhost:5000/api/LoadTest/start \
  -H "Content-Type: application/json" \
  -d @high-throughput.json
```

### Exporting Configuration

```bash
# Export DSE configuration
curl http://localhost:5000/api/ExchangeConfig/DSE/export > dse-config.json

# Import to another instance
curl -X POST http://localhost:5000/api/ExchangeConfig/import \
  -H "Content-Type: application/json" \
  -d @dse-config.json
```

## Configuration Files

Default configuration files are provided in the `configs/` directory:

- `exchanges.json` - Exchange configurations (DSE, CSE, DSE-ITCH, TEST)
- `loadtest-default.json` - Default load test configuration
- `loadtest-high-throughput.json` - High-throughput test configuration

## Architecture

### Configuration Layer
```
┌─────────────────────────────────────┐
│  Web API / CLI / GUI                │
├─────────────────────────────────────┤
│  ExchangeConfigManager              │
│  - Load/Save configs                │
│  - Validate configs                 │
│  - Import/Export                    │
├─────────────────────────────────────┤
│  JSON Configuration Files           │
│  - exchanges.json                   │
│  - loadtest configs                 │
└─────────────────────────────────────┘
```

### Load Testing Layer
```
┌─────────────────────────────────────┐
│  Load Test Controller               │
├─────────────────────────────────────┤
│  LoadTestingService                 │
│  - Message generation               │
│  - Metrics collection               │
│  - Progress reporting               │
├─────────────────────────────────────┤
│  Exchange Protocol Clients          │
│  (FIX, ITCH, FAST)                  │
└─────────────────────────────────────┘
```

## Best Practices

1. **Configuration Management**
   - Always validate configs before saving
   - Use meaningful exchange codes
   - Back up configs before modifications
   - Use version control for config files

2. **Load Testing**
   - Start with default configs for baseline
   - Use ramp-up for realistic testing
   - Monitor system resources during tests
   - Clean up completed tests

3. **Demo Scenarios**
   - Follow scenarios in order (Basic → Intermediate → Advanced)
   - Read step descriptions carefully
   - Review expected results
   - Customize scenarios for your needs

## Troubleshooting

### Configuration Issues

**Problem**: "Exchange code already exists"
- Solution: Use a unique exchange code or update the existing one

**Problem**: "Validation failed"
- Solution: Check all required fields are present and valid

### Load Testing Issues

**Problem**: High failure rate
- Solution: Check network connectivity and server capacity

**Problem**: Low throughput
- Solution: Increase MessagesPerSecond or check system resources

### Demo Scenario Issues

**Problem**: Scenario execution fails
- Solution: Ensure exchange is configured and accessible

**Problem**: Steps timeout
- Solution: Increase DelayMs in scenario configuration

## Support

For issues or questions:
1. Check this documentation
2. Review API endpoint documentation
3. Check logs in `./logs/` directory
4. Open an issue on GitHub

## License

MIT License - See LICENSE file for details
