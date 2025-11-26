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
using System.Net;
using System.Net.Sockets;

namespace OpenFAST.Sessions.Multicast
{
    public sealed class MulticastInputStream : Stream
    {
        private const int BufferSize = 1*1024*1024;
        private readonly ByteBuffer _buffer;
        private readonly UdpClient _socket;

        public MulticastInputStream(UdpClient socket)
        {
            _socket = socket;
            _buffer = ByteBuffer.Allocate(BufferSize);
            _buffer.Flip();
        }

        public override Boolean CanRead
        {
            get { return _buffer.CanRead; }
        }

        public override Boolean CanSeek
        {
            get { return _buffer.CanSeek; }
        }

        public override Boolean CanWrite
        {
            get { return _buffer.CanWrite; }
        }

        public override Int64 Length
        {
            get { return _buffer.Length; }
        }

        public override Int64 Position
        {
            get { return _buffer.Position; }
            set { }
        }

        public override int ReadByte()
        {
            if (_socket?.Client == null)
                return - 1;

            if (!_buffer.HasRemaining())
            {
                _buffer.Flip();

                var remoteIpEndPoint = new IPEndPoint(IPAddress.Any, (_socket.Client.LocalEndPoint as IPEndPoint).Port);
                byte[] dataIn = _socket.Receive(ref remoteIpEndPoint);

                //log
                //Console.WriteLine($"packet length: {dataIn.Length}");

                _buffer.WriteBytes(dataIn);
            }

            return _buffer.Get();
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override Int64 Seek(Int64 offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(Int64 value)
        {
            throw new NotImplementedException();
        }

        public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count)
        {
            throw new NotImplementedException();
        }

        public override void Write(Byte[] buffer, Int32 offset, Int32 count)
        {
            throw new NotImplementedException();
        }
    }
}