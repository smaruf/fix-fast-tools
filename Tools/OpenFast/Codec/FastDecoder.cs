/*

The contents of this file are subject to the Mozilla Public License
Version 1.1 (the "License"); you may not use this file except in
compliance with the License. You may obtain a copy of the License at
http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS"
basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
License for the specific language governing rights and limitations
under the License.

The Original Code is OpenFAST.

The Initial Developer of the Original Code is The LaSalle Technology
Group, LLC.  Portions created by Shariq Muhammad
are Copyright (C) Shariq Muhammad. All Rights Reserved.

Contributor(s): Shariq Muhammad <shariq.muhammad@gmail.com>
                Yuri Astrakhan <FirstName><LastName>@gmail.com
*/
using System.Collections;
using System.Collections.Generic;
using System.IO;
using OpenFAST.Template;
using OpenFAST.Template.Types.Codec;

namespace OpenFAST.Codec
{
    public sealed class FastDecoder : ICoder, IEnumerable<Message>
    {
        private readonly Context _context;
        private readonly Stream _inStream;

        public FastDecoder(Context context, Stream inStream)
        {
            _inStream = inStream;
            _context = context;
        }

        #region ICoder Members

        public void Reset()
        {
            _context.Reset();
        }

        #endregion

        #region IEnumerable<Message> Members

        public IEnumerator<Message> GetEnumerator()
        {
            Reset();
            Message msg;
            while ((msg = ReadMessage()) != null)
                yield return msg;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        public Message? ReadMessage()
        {
            var byteVectorValue = (ByteVectorValue) TypeCodec.ByteVector.Decode(_inStream);
            
            
            if (byteVectorValue?.Value == null || byteVectorValue.Value.Length <= 0)
                return null; // Must have reached end of stream;

            //todo need further investigate
            // for TCP, when connection is closed, found all bytes are equal to 0b11111111 = 255
            if (byteVectorValue.Value.Length == 127 &&
                byteVectorValue.Value.Length == byteVectorValue.Value.Count(d => d == 255))
                return null;

            // log
            //Console.WriteLine($"received length: {byteVectorValue.Value.Length}");
            //Console.WriteLine($"received message - {byteVectorValue.Value.ToBinaryString(true)}");

            using var streamData = new MemoryStream(byteVectorValue.Value);
            
            var message = ReadMessage(streamData);

            if (message == null)
            {
                message = new Message(new MessageTemplate("Unknown", Array.Empty<Field>())
                {
                    Id = "-1"
                });
            }

            message.RawData = byteVectorValue.Value;

            return message;
        }

        private Message? ReadMessage(Stream streamData)
        {
            var bitVectorValue = (BitVectorValue)TypeCodec.BitVector.Decode(streamData);

            if (bitVectorValue?.Value == null)
                return null; // Must have reached end of stream;

            BitVector pmap = bitVectorValue.Value;
            var presenceMapReader = new BitVectorReader(pmap);

            // if template id is not present, use previous, else decode template id
            int templateId = (presenceMapReader.Read())
                ? ((IntegerValue)TypeCodec.Uint.Decode(streamData)).Value
                : _context.LastTemplateId;

            MessageTemplate template = _context.GetTemplate(templateId);

            if (template == null)
            {
                return null;
            }
            _context.NewMessage(template);

            _context.LastTemplateId = templateId;

            return template.Decode(streamData, templateId, presenceMapReader, _context);
        }
    }
}