using System.Text;
using System.Xml;
using FastTools.Core.Models;

namespace FastTools.Core.Services
{
    public class FastMessageDecoder
    {
        private Dictionary<int, string> _templateMap;

        public FastMessageDecoder()
        {
            _templateMap = new Dictionary<int, string>();
        }

        public void LoadTemplateMap(string xmlFilePath)
        {
            _templateMap = new Dictionary<int, string>();
            
            if (!File.Exists(xmlFilePath))
                return;

            try
            {
                var doc = new XmlDocument();
                doc.Load(xmlFilePath);
                var nodes = doc.GetElementsByTagName("template");
                
                foreach (XmlElement el in nodes)
                {
                    if (el.HasAttribute("id"))
                    {
                        if (int.TryParse(el.GetAttribute("id"), out int id))
                        {
                            var name = el.GetAttribute("name");
                            if (string.IsNullOrEmpty(name)) 
                                name = el.GetAttribute("dictionary");
                            _templateMap[id] = name ?? (el.LocalName ?? "template");
                        }
                    }
                }
            }
            catch { }
        }

        public DecodedMessage DecodeBinary(byte[] data, int? knownTemplateId = null)
        {
            var result = new DecodedMessage
            {
                RawBytes = data,
                HexRepresentation = BitConverter.ToString(data).Replace("-", " ")
            };

            if (data.Length == 0)
                return result;

            // Read first stop-bit encoded integer (usually template ID)
            int pos = 0;
            var (value, read) = ReadStopBitUInt(data, 0);
            
            if (read > 0)
            {
                result.TemplateId = value;
                
                if (_templateMap.TryGetValue(value, out var templateName))
                {
                    result.TemplateName = templateName;
                }
                
                pos += read;
            }
            else if (knownTemplateId.HasValue)
            {
                result.TemplateId = knownTemplateId.Value;
                if (_templateMap.TryGetValue(knownTemplateId.Value, out var templateName))
                {
                    result.TemplateName = templateName;
                }
            }

            // Extract strings
            var sb = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                byte b = data[i];
                if (b >= 32 && b <= 126)
                {
                    sb.Append((char)b);
                }
                else
                {
                    if (sb.Length >= 3)
                    {
                        result.DetectedStrings.Add(sb.ToString());
                    }
                    sb.Clear();
                }
            }
            if (sb.Length >= 3) 
                result.DetectedStrings.Add(sb.ToString());

            // Extract stop-bit integers
            int p = 0;
            for (int i = 0; i < 10 && p < data.Length; i++)
            {
                var (v, r) = ReadStopBitUInt(data, p);
                if (r == 0) break;
                result.StopBitIntegers.Add(v);
                p += r;
            }

            return result;
        }

        public List<DecodedMessage> DecodeJsonFile(string jsonFilePath)
        {
            var results = new List<DecodedMessage>();
            
            if (!File.Exists(jsonFilePath))
                return results;

            string jsonContent = File.ReadAllText(jsonFilePath);
            var messages = System.Text.Json.JsonSerializer.Deserialize<FastMessage[]>(jsonContent) ?? Array.Empty<FastMessage>();

            foreach (var msg in messages)
            {
                byte[] rawBytes = Convert.FromBase64String(msg.RawMsg?.Base64 ?? string.Empty);
                var decoded = DecodeBinary(rawBytes, msg.TemplateId);
                decoded.MsgType = msg.MsgType;
                decoded.MsgName = msg.MsgName;
                
                // Parse fields from MsgText if available
                if (!string.IsNullOrEmpty(msg.MsgText))
                {
                    try
                    {
                        var parsed = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(msg.MsgText);
                        ExtractFields(parsed, decoded.Fields);
                    }
                    catch { }
                }
                
                results.Add(decoded);
            }

            return results;
        }

        private void ExtractFields(System.Text.Json.JsonElement element, Dictionary<string, string> fields)
        {
            if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                foreach (var prop in element.EnumerateObject())
                {
                    var value = GetJsonValue(prop.Value);
                    if (!string.IsNullOrEmpty(value))
                    {
                        fields[prop.Name] = value;
                    }
                }
            }
        }

        private string GetJsonValue(System.Text.Json.JsonElement element)
        {
            if (element.ValueKind == System.Text.Json.JsonValueKind.Object && 
                element.TryGetProperty("Value", out var value))
            {
                return value.ToString();
            }
            return element.ToString();
        }

        private (int, int) ReadStopBitUInt(byte[] data, int offset)
        {
            long value = 0;
            int read = 0;
            
            for (int i = offset; i < data.Length; i++)
            {
                byte b = data[i];
                value = (value << 7) | ((long)(b & 0x7F));
                read++;
                
                if ((b & 0x80) != 0)
                {
                    return ((int)value, read);
                }
                
                if (read >= 5) break;
            }
            
            return (0, 0);
        }
    }
}
