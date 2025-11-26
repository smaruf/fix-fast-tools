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
using System;
using System.IO;

namespace OpenFAST
{
    public class ByteBuffer : MemoryStream
    {
        private readonly int size = 0;
        private long readPosition = 0;
        private long writePosition = 0;

        internal static ByteBuffer Allocate(int bufferSize)
        {
            var buff = new ByteBuffer(bufferSize);
            buff.SetLength(bufferSize);
            return buff;
        }

        private ByteBuffer(int bufferSize)
        {
            this.size = bufferSize;
        }

        public void Flip()  // reset
        {
            readPosition = 0;
            writePosition = 0;
        }

        public bool HasRemaining()
        {
            return readPosition < writePosition;
        }

        public int Get()
        {
            this.Position = readPosition;
            var data = this.ReadByte();
            this.readPosition = this.Position;

            return data;
        }

        public void WriteBytes(byte[] data)
        {
            if (writePosition + data.Length > size)
            {
                throw new IndexOutOfRangeException("writing data exceeds size.");
            }

            //log
            //Console.WriteLine($"writePosition: {writePosition}; data.Length: {data.Length}");

            this.Position = writePosition;
            this.Write(data);
            this.writePosition = this.Position;

            //Console.WriteLine($"after write -> writePosition: {writePosition}");
        }

        public byte[] Array()
        {
            throw new NotImplementedException();
        }
    }
}