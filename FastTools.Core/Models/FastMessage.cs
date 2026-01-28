using System.Text.Json;

namespace FastTools.Core.Models
{
    public class FastMessage
    {
        public string Channel { get; set; }
        public int PacketNum { get; set; }
        public JsonElement SendingDateTimeUtc { get; set; }
        public int TemplateId { get; set; }
        public string MsgType { get; set; }
        public string MsgName { get; set; }
        public string ServerId { get; set; }
        public string MsgText { get; set; }
        public RawMsgData RawMsg { get; set; }
        public JsonElement CreatedDateTimeUtc { get; set; }
    }

    public class RawMsgData
    {
        public string Base64 { get; set; }
        public string SubType { get; set; }
    }
}
