using System.Text;

namespace OpenFAST
{
    public static class OpenFastExtension
    {
        public static string ToBinaryString(this byte[] myByteArray, bool useSeparator = true)
        {
            StringBuilder sb = new StringBuilder();

            foreach (byte b in myByteArray)
            {
                sb.Append(Convert.ToString(b, 2).PadLeft(8, '0'));

                sb.Append(b >> 7 > 0 ? " | " : " ");
            }

            return sb.ToString();
        }

        public static string ToAsciiString(this byte[] myByteArray)
        {
            StringBuilder sb = new StringBuilder();

            foreach (byte b in myByteArray)
            {
                sb.Append(Convert.ToString((char)(b & 0b01111111)));

                sb.Append(b >> 7 > 0 ? " | " : "");
            }

            return sb.ToString();
        }
    }
}
