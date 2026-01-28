namespace FastTools.Core.Models
{
    public class DecodedMessage
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; }
        public string MsgType { get; set; }
        public string MsgName { get; set; }
        public byte[] RawBytes { get; set; }
        public Dictionary<string, string> Fields { get; set; }
        public List<string> DetectedStrings { get; set; }
        public List<int> StopBitIntegers { get; set; }
        public string HexRepresentation { get; set; }

        public DecodedMessage()
        {
            Fields = new Dictionary<string, string>();
            DetectedStrings = new List<string>();
            StopBitIntegers = new List<int>();
        }
    }
}
