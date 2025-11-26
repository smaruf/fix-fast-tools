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
    public sealed class MulticastEndpoint : IEndpoint
    {
        private readonly string _group;
        private readonly int _port;

        public MulticastEndpoint(int port, string group)
        {
            _port = port;
            _group = group;
        }

        #region IEndpoint Members

        public ConnectionListener ConnectionListener
        {
            set { throw new NotSupportedException(); }
        }

        public void Accept()
        {
            throw new NotSupportedException();
        }

        public void Close()
        {
        }

        public IConnection Connect()
        {
            try
            {
                /*
                var socket = new UdpClient(_port);
                IPAddress groupAddress = Dns.GetHostEntry(_group).AddressList[0];
                socket.JoinMulticastGroup(groupAddress);
                
                return new MulticastConnection(socket, groupAddress);
                */

                IPAddress mcastIP = IPAddress.Parse(_group);
                var localPort = Convert.ToInt16(_port);

                var localIP = IPAddress.Any; //Parse("172.21.32.1");
                Console.WriteLine($"local ip = {localIP.MapToIPv4()}");
                Console.WriteLine($"mcast = {mcastIP.MapToIPv4()} : {_port}");

                var localEndPoint = new IPEndPoint(localIP, localPort);
                var socket = new UdpClient(localEndPoint);
                socket.JoinMulticastGroup(mcastIP);

                return new MulticastConnection(socket, mcastIP);
            }
            catch (IOException e)
            {
                throw new FastConnectionException(e);
            }
        }

        #endregion
    }
}