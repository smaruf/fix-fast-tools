using System.Text;

namespace ChinPakTools.DSE
{
    public class FixMessageDecoder
    {
        private static readonly char SOH = '\x01'; // FIX field separator

        public static DecodedFixMessage DecodeMessage(string fixMessageString)
        {
            var decoded = new DecodedFixMessage
            {
                RawMessage = fixMessageString,
                DecodedFields = new List<FixField>()
            };

            try
            {
                // Parse FIX message by splitting on SOH (or |)
                var separator = fixMessageString.Contains(SOH) ? SOH : '|';
                var fields = fixMessageString.Split(separator, StringSplitOptions.RemoveEmptyEntries);

                string? msgType = null;

                foreach (var field in fields)
                {
                    var parts = field.Split('=', 2);
                    if (parts.Length != 2)
                        continue;

                    if (!int.TryParse(parts[0], out var tag))
                        continue;

                    var value = parts[1];
                    var fieldDef = GetFieldDefinition(tag);
                    var fieldName = fieldDef?.Name ?? $"Tag{tag}";
                    var displayValue = TranslateValue(tag, value, fieldDef);

                    decoded.DecodedFields.Add(new FixField
                    {
                        Tag = tag,
                        Name = fieldName,
                        Value = displayValue
                    });

                    // Capture message type
                    if (tag == 35)
                        msgType = value;
                }

                // Set message type name
                decoded.MessageType = msgType != null ? GetMessageTypeName(msgType) : "Unknown";
                decoded.Success = true;
            }
            catch (Exception ex)
            {
                decoded.Success = false;
                decoded.Error = ex.Message;
            }

            return decoded;
        }

        private static string TranslateValue(int tag, string value, FieldDefinition? fieldDef)
        {
            return tag switch
            {
                54 => value switch // Side
                {
                    "1" => "Buy",
                    "2" => "Sell",
                    "3" => "Buy minus",
                    "4" => "Sell plus",
                    "5" => "Sell short",
                    "6" => "Sell short exempt",
                    "7" => "Undisclosed",
                    "8" => "Cross",
                    "9" => "Cross short",
                    _ => value
                },
                40 => value switch // OrdType
                {
                    "1" => "Market",
                    "2" => "Limit",
                    "3" => "Stop",
                    "4" => "Stop limit",
                    "P" => "Pegged",
                    _ => value
                },
                59 => value switch // TimeInForce
                {
                    "0" => "Day",
                    "1" => "GTC (Good Till Cancel)",
                    "2" => "OPG (At the Opening)",
                    "3" => "IOC (Immediate or Cancel)",
                    "4" => "FOK (Fill or Kill)",
                    "6" => "GTD (Good Till Date)",
                    _ => value
                },
                150 => value switch // ExecType
                {
                    "0" => "New",
                    "1" => "Partial fill",
                    "2" => "Fill",
                    "3" => "Done for day",
                    "4" => "Canceled",
                    "5" => "Replace",
                    "6" => "Pending Cancel",
                    "7" => "Stopped",
                    "8" => "Rejected",
                    "9" => "Suspended",
                    "A" => "Pending New",
                    "B" => "Calculated",
                    "C" => "Expired",
                    "D" => "Restated",
                    "E" => "Pending Replace",
                    _ => value
                },
                39 => value switch // OrdStatus
                {
                    "0" => "New",
                    "1" => "Partially filled",
                    "2" => "Filled",
                    "3" => "Done for day",
                    "4" => "Canceled",
                    "5" => "Replaced",
                    "6" => "Pending Cancel",
                    "7" => "Stopped",
                    "8" => "Rejected",
                    "9" => "Suspended",
                    "A" => "Pending New",
                    "B" => "Calculated",
                    "C" => "Expired",
                    "E" => "Pending Replace",
                    _ => value
                },
                _ => value
            };
        }

        private static string GetMessageTypeName(string msgType)
        {
            return msgType switch
            {
                "0" => "Heartbeat",
                "1" => "Test Request",
                "2" => "Resend Request",
                "3" => "Reject",
                "4" => "Sequence Reset",
                "5" => "Logout",
                "8" => "Execution Report",
                "9" => "Order Cancel Reject",
                "A" => "Logon",
                "D" => "New Order Single",
                "F" => "Order Cancel/Replace Request",
                "G" => "Order Cancel Request",
                "H" => "Order Status Request",
                "j" => "Business Message Reject",
                _ => $"Unknown ({msgType})"
            };
        }

        private static FieldDefinition? GetFieldDefinition(int tag)
        {
            return FixDictionaryViewer.LookupField(tag);
        }
    }

    public class DecodedFixMessage
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public required string RawMessage { get; set; }
        public string MessageType { get; set; } = "Unknown";
        public required List<FixField> DecodedFields { get; set; }

        public void PrintToConsole()
        {
            if (!Success)
            {
                Console.WriteLine($"[ERROR] Failed to decode message: {Error}");
                return;
            }

            Console.WriteLine($"\n{new string('=', 60)}");
            Console.WriteLine($"Message Type: {MessageType}");
            Console.WriteLine($"{new string('=', 60)}");
            Console.WriteLine($"{"Tag",-6} | {"Field Name",-25} | Value");
            Console.WriteLine(new string('-', 60));
            
            foreach (var field in DecodedFields)
            {
                var value = field.Value.Length > 25 ? field.Value.Substring(0, 22) + "..." : field.Value;
                Console.WriteLine($"{field.Tag,-6} | {field.Name,-25} | {value}");
            }
            
            Console.WriteLine(new string('=', 60));
        }
    }

    public class FixField
    {
        public required int Tag { get; set; }
        public required string Name { get; set; }
        public required string Value { get; set; }
    }
}
