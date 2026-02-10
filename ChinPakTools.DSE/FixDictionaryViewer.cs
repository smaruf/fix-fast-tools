using System.Text;

namespace ChinPakTools.DSE
{
    public class FixDictionaryViewer
    {
        private static Dictionary<int, FieldDefinition> _fieldDictionary = new();
        private static Dictionary<string, MessageDefinition> _messageDictionary = new();

        static FixDictionaryViewer()
        {
            InitializeFieldDictionary();
            InitializeMessageDictionary();
        }

        public static FieldDefinition? LookupField(int tag)
        {
            return _fieldDictionary.TryGetValue(tag, out var field) ? field : null;
        }

        public static MessageDefinition? LookupMessage(string msgType)
        {
            return _messageDictionary.TryGetValue(msgType, out var msg) ? msg : null;
        }

        public static void DisplayFieldInfo(int tag)
        {
            var field = LookupField(tag);
            if (field == null)
            {
                Console.WriteLine($"⚠️  Field tag {tag} not found in dictionary");
                return;
            }

            Console.WriteLine($"\n╔═══ FIX Field Information ═══╗");
            Console.WriteLine($"Tag:         {field.Tag}");
            Console.WriteLine($"Name:        {field.Name}");
            Console.WriteLine($"Type:        {field.Type}");
            Console.WriteLine($"Description: {field.Description}");
            Console.WriteLine($"╚═══════════════════════════════╝\n");
        }

        public static void DisplayMessageInfo(string msgType)
        {
            var message = LookupMessage(msgType);
            if (message == null)
            {
                Console.WriteLine($"⚠️  Message type '{msgType}' not found in dictionary");
                return;
            }

            Console.WriteLine($"\n╔═══ FIX Message Information ═══╗");
            Console.WriteLine($"Type:        {message.MsgType}");
            Console.WriteLine($"Name:        {message.Name}");
            Console.WriteLine($"Category:    {message.Category}");
            Console.WriteLine($"Description: {message.Description}");
            Console.WriteLine($"╚═══════════════════════════════════╝\n");
        }

        public static void SearchFields(string searchTerm)
        {
            var results = _fieldDictionary.Values
                .Where(f => f.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                           f.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f.Tag)
                .ToList();

            if (results.Count == 0)
            {
                Console.WriteLine($"No fields found matching '{searchTerm}'");
                return;
            }

            Console.WriteLine($"\n🔍 Found {results.Count} field(s) matching '{searchTerm}':\n");
            Console.WriteLine($"{"Tag",-6} | {"Name",-30} | {"Type",-10}");
            Console.WriteLine(new string('-', 60));
            
            foreach (var field in results.Take(20))
            {
                Console.WriteLine($"{field.Tag,-6} | {field.Name,-30} | {field.Type,-10}");
            }
            
            if (results.Count > 20)
                Console.WriteLine($"\n... and {results.Count - 20} more results");
        }

        public static void ListAllMessages()
        {
            Console.WriteLine("\n📋 FIX Message Types:\n");
            Console.WriteLine($"{"Type",-6} | {"Name",-35} | {"Category",-20}");
            Console.WriteLine(new string('-', 70));
            
            foreach (var msg in _messageDictionary.Values.OrderBy(m => m.MsgType))
            {
                Console.WriteLine($"{msg.MsgType,-6} | {msg.Name,-35} | {msg.Category,-20}");
            }
        }

        private static void InitializeFieldDictionary()
        {
            // Core session fields
            _fieldDictionary[8] = new FieldDefinition(8, "BeginString", "STRING", "FIX protocol version");
            _fieldDictionary[9] = new FieldDefinition(9, "BodyLength", "LENGTH", "Message body length");
            _fieldDictionary[10] = new FieldDefinition(10, "CheckSum", "STRING", "Message checksum");
            _fieldDictionary[35] = new FieldDefinition(35, "MsgType", "STRING", "Message type");
            _fieldDictionary[34] = new FieldDefinition(34, "MsgSeqNum", "SEQNUM", "Message sequence number");
            _fieldDictionary[49] = new FieldDefinition(49, "SenderCompID", "STRING", "Sender identifier");
            _fieldDictionary[56] = new FieldDefinition(56, "TargetCompID", "STRING", "Target identifier");
            _fieldDictionary[52] = new FieldDefinition(52, "SendingTime", "UTCTIMESTAMP", "Time of message transmission");
            
            // Heartbeat & session
            _fieldDictionary[108] = new FieldDefinition(108, "HeartBtInt", "INT", "Heartbeat interval");
            _fieldDictionary[112] = new FieldDefinition(112, "TestReqID", "STRING", "Test request identifier");
            _fieldDictionary[58] = new FieldDefinition(58, "Text", "STRING", "Free text");
            
            // Order fields
            _fieldDictionary[11] = new FieldDefinition(11, "ClOrdID", "STRING", "Client order ID");
            _fieldDictionary[37] = new FieldDefinition(37, "OrderID", "STRING", "Exchange order ID");
            _fieldDictionary[41] = new FieldDefinition(41, "OrigClOrdID", "STRING", "Original client order ID");
            _fieldDictionary[55] = new FieldDefinition(55, "Symbol", "STRING", "Security symbol");
            _fieldDictionary[54] = new FieldDefinition(54, "Side", "CHAR", "Order side (1=Buy, 2=Sell)");
            _fieldDictionary[38] = new FieldDefinition(38, "OrderQty", "QTY", "Order quantity");
            _fieldDictionary[40] = new FieldDefinition(40, "OrdType", "CHAR", "Order type (1=Market, 2=Limit)");
            _fieldDictionary[44] = new FieldDefinition(44, "Price", "PRICE", "Order price");
            _fieldDictionary[59] = new FieldDefinition(59, "TimeInForce", "CHAR", "Time in force (0=Day, 1=GTC)");
            
            // Execution fields
            _fieldDictionary[150] = new FieldDefinition(150, "ExecType", "CHAR", "Execution type");
            _fieldDictionary[39] = new FieldDefinition(39, "OrdStatus", "CHAR", "Order status");
            _fieldDictionary[151] = new FieldDefinition(151, "LeavesQty", "QTY", "Quantity remaining");
            _fieldDictionary[14] = new FieldDefinition(14, "CumQty", "QTY", "Cumulative quantity filled");
            _fieldDictionary[6] = new FieldDefinition(6, "AvgPx", "PRICE", "Average executed price");
            _fieldDictionary[32] = new FieldDefinition(32, "LastQty", "QTY", "Quantity of last fill");
            _fieldDictionary[31] = new FieldDefinition(31, "LastPx", "PRICE", "Price of last fill");
            _fieldDictionary[17] = new FieldDefinition(17, "ExecID", "STRING", "Execution ID");
            
            // Reject & error fields
            _fieldDictionary[45] = new FieldDefinition(45, "RefSeqNum", "SEQNUM", "Reference sequence number");
            _fieldDictionary[371] = new FieldDefinition(371, "RefTagID", "INT", "Reference tag ID");
            _fieldDictionary[372] = new FieldDefinition(372, "RefMsgType", "STRING", "Reference message type");
            _fieldDictionary[373] = new FieldDefinition(373, "SessionRejectReason", "INT", "Reject reason code");
            _fieldDictionary[102] = new FieldDefinition(102, "CxlRejReason", "INT", "Cancel reject reason");
            _fieldDictionary[103] = new FieldDefinition(103, "OrdRejReason", "INT", "Order reject reason");
            
            // Trading session
            _fieldDictionary[60] = new FieldDefinition(60, "TransactTime", "UTCTIMESTAMP", "Transaction time");
            _fieldDictionary[75] = new FieldDefinition(75, "TradeDate", "LOCALMKTDATE", "Trade date");
            _fieldDictionary[1] = new FieldDefinition(1, "Account", "STRING", "Account number");
            _fieldDictionary[21] = new FieldDefinition(21, "HandlInst", "CHAR", "Handling instructions");
            
            // Additional common fields
            _fieldDictionary[15] = new FieldDefinition(15, "Currency", "CURRENCY", "Currency code");
            _fieldDictionary[22] = new FieldDefinition(22, "SecurityIDSource", "STRING", "Security ID source");
            _fieldDictionary[48] = new FieldDefinition(48, "SecurityID", "STRING", "Security identifier");
            _fieldDictionary[100] = new FieldDefinition(100, "ExDestination", "EXCHANGE", "Execution destination");
            _fieldDictionary[109] = new FieldDefinition(109, "ClientID", "STRING", "Client identifier");
        }

        private static void InitializeMessageDictionary()
        {
            // Admin messages
            _messageDictionary["0"] = new MessageDefinition("0", "Heartbeat", "Admin", "Session heartbeat");
            _messageDictionary["1"] = new MessageDefinition("1", "TestRequest", "Admin", "Test request");
            _messageDictionary["2"] = new MessageDefinition("2", "ResendRequest", "Admin", "Request message resend");
            _messageDictionary["3"] = new MessageDefinition("3", "Reject", "Admin", "Message reject");
            _messageDictionary["4"] = new MessageDefinition("4", "SequenceReset", "Admin", "Sequence reset");
            _messageDictionary["5"] = new MessageDefinition("5", "Logout", "Admin", "Session logout");
            _messageDictionary["A"] = new MessageDefinition("A", "Logon", "Admin", "Session logon");
            
            // Application messages
            _messageDictionary["D"] = new MessageDefinition("D", "NewOrderSingle", "Order", "Submit new order");
            _messageDictionary["8"] = new MessageDefinition("8", "ExecutionReport", "Order", "Order execution report");
            _messageDictionary["9"] = new MessageDefinition("9", "OrderCancelReject", "Order", "Order cancel reject");
            _messageDictionary["F"] = new MessageDefinition("F", "OrderCancelReplaceRequest", "Order", "Cancel/replace order");
            _messageDictionary["G"] = new MessageDefinition("G", "OrderCancelRequest", "Order", "Cancel order");
            _messageDictionary["H"] = new MessageDefinition("H", "OrderStatusRequest", "Order", "Request order status");
            
            // Business messages
            _messageDictionary["j"] = new MessageDefinition("j", "BusinessMessageReject", "Business", "Business message reject");
        }
    }

    public class FieldDefinition
    {
        public int Tag { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }

        public FieldDefinition(int tag, string name, string type, string description)
        {
            Tag = tag;
            Name = name;
            Type = type;
            Description = description;
        }
    }

    public class MessageDefinition
    {
        public string MsgType { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }

        public MessageDefinition(string msgType, string name, string category, string description)
        {
            MsgType = msgType;
            Name = name;
            Category = category;
            Description = description;
        }
    }
}
