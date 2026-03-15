# Quick Start Guide - FAST Market Data Replay

## What Was Implemented

A console application that replays historical CSE FAST market data messages from MongoDB for testing or simulation purposes.

## Key Features

1. **User Input**:
   - Prompts for start date/time (UTC format)
   - Asks for duration in hours

2. **Message Retrieval**:
   - Queries `FastIncomingMessage` collection from MongoDB
   - Filters by date range: `StartDateTime` to `StartDateTime + DurationHours`
   - Sorts messages chronologically by `SendingDateTimeUtc`

3. **Message Processing**:
   - Converts each `FastIncomingMessage` to `FastDataPacket`
   - Deserializes JSON (`MsgText`) back to `IFastMessage` objects
   - Calls `FastDataMutationService.MutateMessageAsync()` for each message

4. **Progress Tracking**:
   - Shows total messages found
   - Updates progress every 100 messages
   - Reports final statistics (processed, errors)

## Files Created

1. **Program.cs** - Main replay logic
2. **appsettings.json** - MongoDB configuration
3. **README.md** - Detailed documentation
4. **EcoSoftBD.Oms.Fast.ReplyFASTMarketData.csproj** - Project file with dependencies

## Quick Setup

### 1. Update Configuration

Edit `appsettings.json`:

```json
{
  "MongoDb": {
    "ServerFast": "mongodb://your-server:27017",
    "FastDb": "YourDatabaseName"
  }
}
```

### 2. Build the Project

```bash
cd EcoSoftBD.Oms.Fast.ReplyFASTMarketData
dotnet build
```

### 3. Run the Application

```bash
dotnet run
```

### 4. Follow the Prompts

```
Enter start date/time (yyyy-MM-dd HH:mm:ss UTC): 2024-03-13 09:00:00
Enter duration in hours: 2
```

## Example Session

```
=== CSE FAST Market Data Replay Tool ===

Enter start date/time (yyyy-MM-dd HH:mm:ss UTC): 2024-03-13 09:00:00
Enter duration in hours: 2

Replay Configuration:
  Start DateTime (UTC): 2024-03-13 09:00:00
  End DateTime (UTC):   2024-03-13 11:00:00
  Duration: 2 hour(s)

Starting replay...

Found 15234 messages to replay

Processed 100 messages...
Processed 200 messages...
...
Processed 15200 messages...

Summary:
  Total messages: 15234
  Successfully processed: 15230
  Errors: 4

Replay completed successfully!

Press any key to exit...
```

## Supported Message Types

The replay tool handles all FAST message types:

- ✅ Heartbeat
- ✅ Logon/Logout
- ✅ News
- ✅ SecurityDefinition
- ✅ SecurityStatus
- ✅ MarketDataSnapshotFullRefresh (MDSnapshot)
- ✅ MarketDataIncrementalRefresh
- ✅ MarketDataRequestReject
- ✅ BusinessMessageReject
- ✅ ApplicationMessageRequestAck
- ✅ ApplicationMessageReport

## Architecture

```
┌─────────────────────────────────────┐
│   User Input (DateTime + Hours)    │
└────────────┬────────────────────────┘
             │
             ▼
┌─────────────────────────────────────┐
│  Query MongoDB                      │
│  (FastIncomingMessage collection)   │
│  Filter: SendingDateTimeUtc         │
│  Sort: Chronological order          │
└────────────┬────────────────────────┘
             │
             ▼
┌─────────────────────────────────────┐
│  For Each Message:                  │
│  1. Deserialize JSON → IFastMessage │
│  2. Create FastDataPacket           │
│  3. Call MutateMessageAsync()       │
└────────────┬────────────────────────┘
             │
             ▼
┌─────────────────────────────────────┐
│  FastDataMutationService            │
│  - Extracts message details         │
│  - Calls type-specific handlers:    │
│    * SecurityDefinition             │
│    * SecurityStatus                 │
│    * MarketData (Snapshot/Incr.)    │
│    * News                            │
│    * Orderbook updates              │
└─────────────────────────────────────┘
```

## Dependencies

The project references:
- `EcoSoftBD.Oms.Fast.Services` - Mutation service and handlers
- `EcoSoftBD.Oms.Fast.Db` - Repository implementations
- `EcoSoftBD.Oms.Fast.Message` - Message types
- `EcoSoftBD.Oms.Common` - Common utilities

NuGet packages:
- `Microsoft.Extensions.DependencyInjection`
- `Microsoft.Extensions.Configuration`
- `MongoDB.Driver`

## Troubleshooting

**Issue**: "Unable to access database from repository"
- **Fix**: Check MongoDB connection string in appsettings.json
- Verify MongoDB server is running
- Ensure database name is correct

**Issue**: "Could not deserialize message"
- **Fix**: Check that MsgText contains valid JSON
- Some messages may have been stored as RAW_HEX - these are skipped
- Verify message type names match the deserialization switch statement

**Issue**: Service registration errors
- **Fix**: Ensure all project references are restored: `dotnet restore`
- Verify EcoSoftBD.Oms.Fast.Db.IocConfig exists and is accessible

## Next Steps

### For Production Use:
1. Add error logging to file (e.g., using Serilog)
2. Implement retry logic for failed messages
3. Add filtering by message type or channel
4. Create command-line arguments instead of interactive prompts
5. Add resume capability for interrupted replays

### For Testing:
1. Start with a small time range (e.g., 15 minutes)
2. Verify message counts match expectations
3. Check that mutations are applied correctly
4. Monitor database changes during replay

## Contact & Support

For issues or questions, refer to the main project documentation or contact the development team.
