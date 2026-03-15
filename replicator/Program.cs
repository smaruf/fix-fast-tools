using System.Globalization;
using System.Reflection;
using EcoSoftBD.Lib.Cache;
using EcoSoftBD.Lib.Extensions;
using EcoSoftBD.Oms.Common;
using EcoSoftBD.Oms.Common.Interfaces;
using EcoSoftBD.Oms.Fast.Db;
using EcoSoftBD.Oms.Fast.Db.Entities;
using EcoSoftBD.Oms.Fast.Db.Interfaces;
using EcoSoftBD.Oms.Fast.Db.Repositories;
using EcoSoftBD.Oms.Fast.Message;
using EcoSoftBD.Oms.Fast.Message.Messages;
using EcoSoftBD.Oms.Fast.Services;
using EcoSoftBD.Oms.Fast.Services.Resolvers;
using EcoSoftBD.Oms.Fast.Services.Resolvers.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace EcoSoftBD.Oms.Fast.ReplyFASTMarketData
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== CSE FAST Market Data Replay Tool ===");
            Console.WriteLine();

            // Initialize cache configuration (required by some repositories)
            InitializeCacheConfiguration();

            // Configure services
            var services = ConfigureServices();
            var serviceProvider = services.BuildServiceProvider();

            var fastIncomingMessageRepository = serviceProvider.GetRequiredService<IFastIncomingMessageRepository>();
            
            // Check database status first
            await ShowDatabaseStatus(fastIncomingMessageRepository);
            Console.WriteLine();

            // Get user input
            DateTime startDateTime = GetStartDateTime();
            int durationHours = GetDurationHours();
            
            DateTime endDateTime = startDateTime.AddHours(durationHours);

            Console.WriteLine();
            Console.WriteLine("Replay Configuration:");
            Console.WriteLine($"  Start DateTime (UTC): {startDateTime:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"  End DateTime (UTC):   {endDateTime:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"  Duration: {durationHours} hour(s)");
            Console.WriteLine();

            // Get required services
            var fastDataMutationService = serviceProvider.GetRequiredService<FastDataMutationService>();
            var dateTimeProvider = serviceProvider.GetRequiredService<IDateTimeProvider>();

            Console.WriteLine("Starting replay...");
            Console.WriteLine();

            try
            {
                await ReplayMessagesAsync(
                    fastIncomingMessageRepository,
                    fastDataMutationService,
                    dateTimeProvider,
                    startDateTime,
                    endDateTime
                );

                Console.WriteLine();
                Console.WriteLine("Replay completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        static async Task ShowDatabaseStatus(IFastIncomingMessageRepository repository)
        {
            Console.WriteLine("Checking database...");
            
            try
            {
                var dbDynamic = (repository as FastIncomingMessageRepository)?.GetDb();
                if (dbDynamic == null)
                {
                    Console.WriteLine("WARNING: Unable to access database");
                    return;
                }

                var db = (FastDb)dbDynamic;
                var collection = db.GetCollection<FastIncomingMessage>();

                // Get total count
                var totalCount = await collection.CountDocumentsAsync(Builders<FastIncomingMessage>.Filter.Empty);
                Console.WriteLine($"Total messages in database: {totalCount:N0}");

                if (totalCount > 0)
                {
                    // Get earliest message
                    var earliest = await collection
                        .Find(Builders<FastIncomingMessage>.Filter.Empty)
                        .SortBy(x => x.SendingDateTimeUtc)
                        .Limit(1)
                        .FirstOrDefaultAsync();

                    // Get latest message
                    var latest = await collection
                        .Find(Builders<FastIncomingMessage>.Filter.Empty)
                        .SortByDescending(x => x.SendingDateTimeUtc)
                        .Limit(1)
                        .FirstOrDefaultAsync();

                    if (earliest != null && latest != null)
                    {
                        Console.WriteLine($"Date range: {earliest.SendingDateTimeUtc:yyyy-MM-dd HH:mm:ss} UTC to {latest.SendingDateTimeUtc:yyyy-MM-dd HH:mm:ss} UTC");
                        
                        // Get all unique message types from database
                        var uniqueMessageTypes = await collection
                            .DistinctAsync(new StringFieldDefinition<FastIncomingMessage, string>("MsgName"), 
                                           Builders<FastIncomingMessage>.Filter.Empty);
                        
                        var messageTypesList = await uniqueMessageTypes.ToListAsync();
                        
                        Console.WriteLine();
                        Console.WriteLine($"Unique message types in database ({messageTypesList.Count}):");
                        
                        foreach (var msgType in messageTypesList.OrderBy(x => x))
                        {
                            // Get count for each message type
                            var typeCount = await collection.CountDocumentsAsync(
                                Builders<FastIncomingMessage>.Filter.Eq(m => m.MsgName, msgType));
                            
                            Console.WriteLine($"  {msgType,-40} : {typeCount,8:N0} messages");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("⚠️  Database is empty! No messages to replay.");
                    Console.WriteLine("   Make sure the FAST client has been running and capturing messages.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR checking database: {ex.Message}");
            }
        }

        static DateTime GetStartDateTime()
        {
            while (true)
            {
                Console.Write("Enter start date (yyyy-MM-dd) or datetime (yyyy-MM-dd HH:mm:ss UTC or ISO 8601): ");
                string? input = Console.ReadLine();

                // Try parsing as just a date (yyyy-MM-dd)
                if (DateTime.TryParseExact(input, "yyyy-MM-dd", 
                    CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, 
                    out DateTime dateOnly))
                {
                    // Return start of day (00:00:00)
                    return DateTime.SpecifyKind(dateOnly.Date, DateTimeKind.Utc);
                }

                // Try parsing as ISO 8601 format (like 2026-03-12T03:43:15.61Z)
                if (DateTime.TryParse(input, CultureInfo.InvariantCulture, 
                    DateTimeStyles.RoundtripKind, out DateTime result))
                {
                    // Extract just the date part and return start of day
                    return DateTime.SpecifyKind(result.Date, DateTimeKind.Utc);
                }

                // Try parsing as simple format (yyyy-MM-dd HH:mm:ss)
                if (DateTime.TryParseExact(input, "yyyy-MM-dd HH:mm:ss", 
                    CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, 
                    out result))
                {
                    // Extract just the date part and return start of day
                    return DateTime.SpecifyKind(result.Date, DateTimeKind.Utc);
                }

                Console.WriteLine("Invalid format. Please use: yyyy-MM-dd, yyyy-MM-dd HH:mm:ss, or ISO 8601 (e.g., 2026-03-12T03:43:15Z)");
                Console.WriteLine("Note: Time portion is ignored - replay always starts from 00:00:00 UTC of the specified date.");
            }
        }

        static int GetDurationHours()
        {
            while (true)
            {
                Console.Write("Enter duration in hours (e.g., 24 for full day): ");
                string? input = Console.ReadLine();

                if (int.TryParse(input, out int hours) && hours > 0)
                {
                    return hours;
                }

                Console.WriteLine("Invalid input. Please enter a positive integer.");
            }
        }

        static async Task ReplayMessagesAsync(
            IFastIncomingMessageRepository fastIncomingMessageRepository,
            FastDataMutationService fastDataMutationService,
            IDateTimeProvider dateTimeProvider,
            DateTime startDateTime,
            DateTime endDateTime)
        {
            // Query messages from repository
            var messages = await GetMessagesInTimeRangeAsync(
                fastIncomingMessageRepository, 
                startDateTime, 
                endDateTime
            );

            Console.WriteLine($"Found {messages.Count} messages to replay");
            Console.WriteLine();

            int processedCount = 0;
            int errorCount = 0;
            
            // Track error types for summary
            var errorSummary = new Dictionary<string, int>();
            var errorExamples = new Dictionary<string, string>();

            foreach (var message in messages)
            {
                try
                {
                    // Convert FastIncomingMessage to FastDataPacket
                    var fastDataPacket = ConvertToFastDataPacket(message, out string? errorDetail);

                    if (fastDataPacket?.FastMessage != null)
                    {
                        // Process the message
                        await fastDataMutationService.MutateMessageAsync(fastDataPacket);
                        
                        processedCount++;

                        // Log MDIncrementalRefresh success with details
                        if (message.MsgName == "MDIncrementalRefresh")
                        {
                            var mdIncRefMsg = fastDataPacket.FastMessage as MDIncrementalRefreshFastMessage;
                            var entriesCount = mdIncRefMsg?.IncRefMDEntries?.Items?.Count ?? 0;
                            var isNull = mdIncRefMsg?.IncRefMDEntries == null ? "NULL" : 
                                        mdIncRefMsg?.IncRefMDEntries?.Items == null ? "Items=NULL" : 
                                        entriesCount == 0 ? "Items=EMPTY" : "OK";
                            
                            Console.WriteLine($"✓ MDIncrementalRefresh SUCCESS - {message.SendingDateTimeUtc:yyyy-MM-dd HH:mm:ss.fff} - Channel {message.Channel}, Packet {message.PacketNum}");
                            Console.WriteLine($"  IncRefMDEntries: {isNull}, EntryCount: {entriesCount}");
                            
                            // Show JSON for messages with 0 entries to debug
                            if (entriesCount == 0)
                            {
                                Console.WriteLine($"  JSON: {message.MsgText?.Substring(0, Math.Min(500, message.MsgText?.Length ?? 0))}...");
                            }
                        }

                        if (processedCount % 100 == 0)
                        {
                            Console.WriteLine($"Processed {processedCount} messages...");
                        }
                    }
                    else
                    {
                        errorCount++;
                        
                        // Log MDIncrementalRefresh failure
                        if (message.MsgName == "MDIncrementalRefresh")
                        {
                            Console.WriteLine($"✗ MDIncrementalRefresh FAILED - {message.SendingDateTimeUtc:yyyy-MM-dd HH:mm:ss.fff} - Channel {message.Channel}, Packet {message.PacketNum} - Reason: {errorDetail}");
                        }
                        
                        // Track error types for summary
                        var errorKey = $"{message.MsgName}: {errorDetail ?? "Unknown"}";
                        if (errorSummary.ContainsKey(errorKey))
                        {
                            errorSummary[errorKey]++;
                        }
                        else
                        {
                            errorSummary[errorKey] = 1;
                            // Store first example of each error type
                            errorExamples[errorKey] = $"{message.SendingDateTimeUtc:yyyy-MM-dd HH:mm:ss.fff} - Channel {message.Channel}";
                        }
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    
                    // Log MDIncrementalRefresh exception
                    if (message.MsgName == "MDIncrementalRefresh")
                    {
                        Console.WriteLine($"✗ MDIncrementalRefresh EXCEPTION - {message.SendingDateTimeUtc:yyyy-MM-dd HH:mm:ss.fff} - Channel {message.Channel}, Packet {message.PacketNum} - Error: {ex.Message}");
                    }
                    
                    var errorKey = $"Exception: {ex.Message}";
                    if (errorSummary.ContainsKey(errorKey))
                    {
                        errorSummary[errorKey]++;
                    }
                    else
                    {
                        errorSummary[errorKey] = 1;
                        errorExamples[errorKey] = $"{message.SendingDateTimeUtc:yyyy-MM-dd HH:mm:ss.fff} - {message.MsgName}";
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine("Summary:");
            Console.WriteLine($"  Total messages: {messages.Count}");
            Console.WriteLine($"  Successfully processed: {processedCount}");
            Console.WriteLine($"  Errors: {errorCount}");
            
            // Show error summary if there were errors
            if (errorCount > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Error Breakdown:");
                
                foreach (var error in errorSummary.OrderByDescending(x => x.Value))
                {
                    Console.WriteLine($"  {error.Value,6}x - {error.Key}");
                    if (errorExamples.ContainsKey(error.Key))
                    {
                        Console.WriteLine($"           Example: {errorExamples[error.Key]}");
                    }
                }
            }
        }

        static async Task<List<FastIncomingMessage>> GetMessagesInTimeRangeAsync(
            IFastIncomingMessageRepository repository,
            DateTime startDateTime,
            DateTime endDateTime)
        {
            // Access the MongoDB collection directly to build a custom query
            var dbDynamic = (repository as FastIncomingMessageRepository)?.GetDb();
            
            if (dbDynamic == null)
            {
                throw new InvalidOperationException("Unable to access database from repository");
            }

            var db = (FastDb)dbDynamic;
            var collection = db.GetCollection<FastIncomingMessage>();

            // Build filter for date range
            var filter = Builders<FastIncomingMessage>.Filter.Gte(x => x.SendingDateTimeUtc, startDateTime) &
                         Builders<FastIncomingMessage>.Filter.Lt(x => x.SendingDateTimeUtc, endDateTime);

            // Sort by SendingDateTimeUtc to ensure chronological order
            var messages = await collection
                .Find(filter)
                .SortBy(x => x.SendingDateTimeUtc)
                .ToListAsync();

            return messages;
        }

        static FastDataPacket? ConvertToFastDataPacket(FastIncomingMessage incomingMessage, out string? errorDetail)
        {
            errorDetail = null;
            
            try
            {
                // Deserialize the message from JSON
                IFastMessage? fastMessage = null;

                if (string.IsNullOrEmpty(incomingMessage.MsgText))
                {
                    errorDetail = "MsgText is empty";
                    return null;
                }
                
                if (incomingMessage.MsgText.StartsWith("RAW_HEX:"))
                {
                    errorDetail = "RAW_HEX message (binary data, not deserializable)";
                    return null;
                }
                
                if (incomingMessage.MsgText.StartsWith("RAW_EMPTY"))
                {
                    errorDetail = "RAW_EMPTY message (empty packet)";
                    return null;
                }

                fastMessage = DeserializeFastMessage(incomingMessage.MsgText, incomingMessage.MsgName, out string? deserializeError);
                
                if (fastMessage == null)
                {
                    errorDetail = $"Deserialization failed for '{incomingMessage.MsgName}': {deserializeError ?? "Unknown error"}"; 
                    return null;
                }

                // Create FastDataPacket
                var fastDataPacket = new FastDataPacket
                {
                    Channel = incomingMessage.Channel,
                    PacketNum = incomingMessage.PacketNum,
                    FastMessage = fastMessage,
                    RawData = incomingMessage.RawMsg ?? Array.Empty<byte>()
                };

                return fastDataPacket;
            }
            catch (Exception ex)
            {
                errorDetail = $"Exception: {ex.Message}";
                return null;
            }
        }

        static IFastMessage? DeserializeFastMessage(string msgText, string msgName, out string? errorDetail)
        {
            errorDetail = null;
            
            try
            {
                // Create JSON settings that allow private setters
                var jsonSettings = new JsonSerializerSettings
                {
                    ContractResolver = new PrivateSetterContractResolver(),
                    ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                    TypeNameHandling = TypeNameHandling.Auto,
                    MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead,
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects
                };
                
                // Deserialize based on message type
                IFastMessage? result = msgName switch
                {
                    "Heartbeat" => JsonConvert.DeserializeObject<HeartbeatFastMessage>(msgText, jsonSettings),
                    "Logon" => JsonConvert.DeserializeObject<LogonFastMessage>(msgText, jsonSettings),
                    "Logout" => JsonConvert.DeserializeObject<LogoutFastMessage>(msgText, jsonSettings),
                    "News" => JsonConvert.DeserializeObject<NewsFastMessage>(msgText, jsonSettings),
                    "SecurityDefinition" => JsonConvert.DeserializeObject<SecurityDefinitionFastMessage>(msgText, jsonSettings),
                    "SecurityStatus" => JsonConvert.DeserializeObject<SecurityStatusFastMessage>(msgText, jsonSettings),
                    
                    // Market data messages - support both short and long names
                    "MDSnapshot" or "MarketDataSnapshotFullRefresh" => JsonConvert.DeserializeObject<MDSnapshotFastMessage>(msgText, jsonSettings),
                    "MDIncrementalRefresh" => JsonConvert.DeserializeObject<MDIncrementalRefreshFastMessage>(msgText, jsonSettings),
                    "MarketDataRequestReject" => JsonConvert.DeserializeObject<MarketDataRequestRejectFastMessage>(msgText, jsonSettings),
                    
                    // Admin messages
                    "BusinessMessageReject" => JsonConvert.DeserializeObject<BusinessMessageRejectFastMessage>(msgText, jsonSettings),
                    "ApplicationMessageRequestAck" => JsonConvert.DeserializeObject<ApplicationMessageRequestAckFastMessage>(msgText, jsonSettings),
                    "ApplicationMessageReport" => JsonConvert.DeserializeObject<ApplicationMessageReportFastMessage>(msgText, jsonSettings),
                    _ => null
                };
                
                if (result == null && msgName != null)
                {
                    errorDetail = $"Unknown message type '{msgName}' or null deserialization result";
                }
                
                return result;
            }
            catch (Exception ex)
            {
                errorDetail = ex.Message;
                return null;
            }
        }

        // Custom contract resolver to handle private setters
        private class PrivateSetterContractResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var prop = base.CreateProperty(member, memberSerialization);

                if (!prop.Writable)
                {
                    var property = member as PropertyInfo;
                    if (property != null)
                    {
                        var hasPrivateSetter = property.GetSetMethod(true) != null;
                        prop.Writable = hasPrivateSetter;
                    }
                }

                return prop;
            }
        }

        static ServiceCollection ConfigureServices()
        {
            var services = new ServiceCollection();

            // Load configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            // Add logging support
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Register DateTimeConfig (required by DateTimeProvider)
            services.AddSingleton<IDateTimeConfig>(sp =>
                new DateTimeConfig
                {
                    ExchangeTimeZone = configuration["Exchange:TimeZone"] ?? "Bangladesh Standard Time"
                });

            // Register DateTimeProvider
            services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

            // Register MongoDB configuration
            services.AddSingleton<IMongoFastDbConfiguration>(sp => 
                new MongoFastDbConfiguration
                {
                    MongoDbServerFast = configuration["MongoDb:ServerFast"] ?? "mongodb://localhost:27017",
                    MongoDbFastDb = "OmsTradingApi_CSE_FAST_DB"
                });

            // Register all Fast.Db repositories using Fast.Db IocConfig
            IocConfig.Register(services);

            // Register Fast.Services using reflection (following the pattern from Fast.Client)
            RegisterFastServices(services);

            return services;
        }

        static void RegisterFastServices(IServiceCollection services)
        {
            // Register FieldValueResolverService with minimal settings
            services.AddSingleton<IFieldValueResolverService>(sp =>
            {
                var settings = new Dictionary<string, string>();
                return new FieldValueResolverService(settings);
            });

            // Automatically register all services and handlers from Fast.Services assembly
            // This includes all handler services and FastDataMutationService
            RegisterClassesFromAssembly(services, typeof(FastDataMutationService), "Service", true);
        }

        static void RegisterClassesFromAssembly(IServiceCollection services, Type assembly, string classSuffix,
            bool includeInterfaces = false)
        {
            Assembly.GetAssembly(assembly).GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith(classSuffix))
                .ForEach(t =>
                {
                    // Skip types that were explicitly registered (check both concrete type and interfaces)
                    if (services.Any(sd => sd.ImplementationType == t || sd.ServiceType == t))
                    {
                        return;
                    }

                    // Also skip if any interface of this type is already registered (e.g., via factory)
                    var typeInterfaces = t.GetInterfaces();
                    if (typeInterfaces.Any(i => services.Any(sd => sd.ServiceType == i)))
                    {
                        return;
                    }

                    services.AddSingleton(t);

                    if (!includeInterfaces)
                    {
                        return;
                    }

                    var interfaces = t.FindInterfaces((i, o) => i.Name.EndsWith(classSuffix), null);

                    if (interfaces.Length > 0)
                    {
                        interfaces.ForEach(i =>
                        {
                            if (!services.Any(sd => sd.ServiceType == i))
                            {
                                services.AddSingleton(i, t);
                            }
                        });
                    }
                });
        }

        // Helper class for MongoDB configuration
        private class MongoFastDbConfiguration : IMongoFastDbConfiguration
        {
            public string MongoDbServerFast { get; set; } = string.Empty;
            public string MongoDbFastDb { get; set; } = string.Empty;
            public string MongoDbFastLogDb { get; } = string.Empty;
            public int? MaxFastDbDataRetentionDays { get; } = null;
        }

        // Helper class for DateTime configuration
        private class DateTimeConfig : IDateTimeConfig
        {
            public string ExchangeTimeZone { get; set; } = "Bangladesh Standard Time";
        }

        static void InitializeCacheConfiguration()
        {
            CacheConfiguration.Initialize(new CacheConfiguration
            {
                AppCache = CacheTypeEnum.InMemory,
                CacheCircuitBreakerCount = 3,
                CacheCircuitBreakerDurationSeconds = 1,
                CacheNamespace = "FastReplay",
                CacheServerConnectionString = string.Empty,
            });
        }
    }

    // Extension method to access the underlying db (add to repository if needed)  
    public static class RepositoryExtensions
    {
        public static dynamic? GetDb(this FastIncomingMessageRepository repository)
        {
            // Use reflection to access the protected 'db' field from the base class
            var field = repository.GetType()
                .GetField("db", BindingFlags.NonPublic | BindingFlags.Instance);
            
            return field?.GetValue(repository);
        }
    }
}
