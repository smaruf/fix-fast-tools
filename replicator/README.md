# CSE FAST Market Data Replay Tool

## Overview

This tool replays historical CSE FAST market data messages from the `fastIncomingMessageRepository` for testing or simulation purposes.

## Features

- Retrieves FAST messages from MongoDB within a specified time range
- Processes messages in chronological order
- Uses `FastDataMutationService.MutateMessageAsync()` for message processing
- Provides progress tracking and error reporting

## Configuration

### appsettings.json

Update the MongoDB connection settings in `appsettings.json`:

```json
{
  "MongoDb": {
    "ServerFast": "mongodb://localhost:27017",
    "FastDb": "FastDb"
  }
}
```

### Required Services

The tool requires the following services to be properly configured:

1. `IFastIncomingMessageRepository` - for retrieving stored messages
2. `FastDataMutationService` - for processing messages
3. Handler services:
   - `IFastMarketDataHandlerService`
   - `INewsDataMutationHandlerService`
   - `ISecurityDefinitionHandlerService`
   - `ISecurityStatusHandlerService`
4. `IOrderbookResolverService`
5. Various repository dependencies

## Usage

1. Run the application:
   ```
   dotnet run
   ```

2. Enter the start date/time in UTC format:
   ```
   yyyy-MM-dd HH:mm:ss
   Example: 2024-03-13 09:00:00
   ```

3. Enter the duration in hours:
   ```
   Example: 2
   ```

4. The tool will:
   - Query messages from the repository
   - Display the total number of messages found
   - Process each message through `FastDataMutationService`
   - Show progress every 100 messages
   - Display a summary at completion

## Implementation Notes

### Message Flow

1. **Query**: Messages are retrieved from `FastIncomingMessage` collection
   - Filtered by `SendingDateTimeUtc` field
   - Sorted chronologically
   
2. **Deserialization**: Each message is converted from JSON to `IFastMessage`
   - Message type is determined from `MsgName` field
   - Raw binary data is preserved if available

3. **Packet Creation**: `FastDataPacket` is constructed with:
   - `Channel` - from original message
   - `PacketNum` - from original message  
   - `FastMessage` - deserialized message object
   - `RawData` - original binary data

4. **Processing**: `FastDataMutationService.MutateMessageAsync()` handles:
   - Message detail extraction and storage
   - Type-specific mutations
   - Security definitions
   - Market data (snapshots and incremental updates)
   - News
   - Status updates

## Dependency Injection Setup

The application requires full DI configuration. If services are not available, you may need to:

1. Reference the complete IoC configuration from `EcoSoftBD.Oms.Fast.Client` or similar projects
2. Set up all handler services with their dependencies
3. Configure repository instances with proper MongoDB connections

## Alternative: Simplified Replay

For testing without full mutations, you can modify the code to:
- Skip `FastDataMutationService`
- Log messages to console or files
- Perform custom analytics

See `SimplifiedReplay.cs` for an example.

## Error Handling

- Messages that cannot be deserialized are logged but skipped
- Processing errors are caught and counted
- The tool continues processing remaining messages after errors

## Performance Considerations

- Large time ranges may retrieve many messages (monitor memory usage)
- Processing speed depends on mutation service performance
- Consider batching for very large datasets
- Progress is shown every 100 messages

## Troubleshooting

### "Unable to access database from repository"
- Ensure MongoDB connection string is correct
- Verify database and collection exist
- Check network connectivity

### "Could not deserialize message"
- Message JSON format may have changed
- Check `MsgText` field contains valid JSON
- Verify message type mapping is current

### Missing services errors
- Ensure all required services are registered in DI
- Verify project references are correct
- Check service implementations are available

## Future Enhancements

- Configuration file for MongoDB settings
- Batch processing for better performance
- Resume capability for interrupted replays
- Filtering by message type or channel
- Replay speed control (throttling)
- Dry-run mode without mutations
