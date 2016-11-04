﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using MediaBrowser.Model.Net;

namespace Emby.Common.Implementations.Net
{
    public class SocketFactory : ISocketFactory
    {
        // THIS IS A LINKED FILE - SHARED AMONGST MULTIPLE PLATFORMS	
        // Be careful to check any changes compile and work for all platform projects it is shared in.

        // Not entirely happy with this. Would have liked to have done something more generic/reusable,
        // but that wasn't really the point so kept to YAGNI principal for now, even if the 
        // interfaces are a bit ugly, specific and make assumptions.

        /// <summary>
        /// Used by RSSDP components to create implementations of the <see cref="IUdpSocket"/> interface, to perform platform agnostic socket communications.
        /// </summary>
        private IPAddress _LocalIP;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="localIP">A string containing the IP address of the local network adapter to bind sockets to. Null or empty string will use <see cref="IPAddress.Any"/>.</param>
        public SocketFactory(string localIP)
        {
            if (String.IsNullOrEmpty(localIP))
                _LocalIP = IPAddress.Any;
            else
                _LocalIP = IPAddress.Parse(localIP);
        }

        #region ISocketFactory Members

        /// <summary>
        /// Creates a new UDP socket and binds it to the specified local port.
        /// </summary>
        /// <param name="localPort">An integer specifying the local port to bind the socket to.</param>
        public IUdpSocket CreateUdpSocket(int localPort)
        {
            if (localPort < 0) throw new ArgumentException("localPort cannot be less than zero.", "localPort");

            var retVal = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                return new UdpSocket(retVal, localPort, _LocalIP);
            }
            catch
            {
                if (retVal != null)
                    retVal.Dispose();

                throw;
            }
        }

        /// <summary>
        /// Creates a new UDP socket that is a member of the SSDP multicast local admin group and binds it to the specified local port.
        /// </summary>
        /// <param name="localPort">An integer specifying the local port to bind the socket to.</param>
        /// <returns>An implementation of the <see cref="IUdpSocket"/> interface used by RSSDP components to perform socket operations.</returns>
        public IUdpSocket CreateSsdpUdpSocket(int localPort)
        {
            if (localPort < 0) throw new ArgumentException("localPort cannot be less than zero.", "localPort");

            var retVal = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                retVal.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 4);
                retVal.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(IPAddress.Parse("239.255.255.250"), _LocalIP));
                return new UdpSocket(retVal, localPort, _LocalIP);
            }
            catch
            {
                if (retVal != null)
                    retVal.Dispose();

                throw;
            }
        }

        /// <summary>
        /// Creates a new UDP socket that is a member of the specified multicast IP address, and binds it to the specified local port.
        /// </summary>
        /// <param name="ipAddress">The multicast IP address to make the socket a member of.</param>
        /// <param name="multicastTimeToLive">The multicast time to live value for the socket.</param>
        /// <param name="localPort">The number of the local port to bind to.</param>
        /// <returns></returns>
        public IUdpSocket CreateUdpMulticastSocket(string ipAddress, int multicastTimeToLive, int localPort)
        {
            if (ipAddress == null) throw new ArgumentNullException("ipAddress");
            if (ipAddress.Length == 0) throw new ArgumentException("ipAddress cannot be an empty string.", "ipAddress");
            if (multicastTimeToLive <= 0) throw new ArgumentException("multicastTimeToLive cannot be zero or less.", "multicastTimeToLive");
            if (localPort < 0) throw new ArgumentException("localPort cannot be less than zero.", "localPort");

            var retVal = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            try
            {
#if NETSTANDARD1_3
				// The ExclusiveAddressUse socket option is a Windows-specific option that, when set to "true," tells Windows not to allow another socket to use the same local address as this socket
				// See https://github.com/dotnet/corefx/pull/11509 for more details
				if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
				{
					retVal.ExclusiveAddressUse = false;
				}
#else
                retVal.ExclusiveAddressUse = false;
#endif
                retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                retVal.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, multicastTimeToLive);
                retVal.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(IPAddress.Parse(ipAddress), _LocalIP));
                retVal.MulticastLoopback = true;

                return new UdpSocket(retVal, localPort, _LocalIP);
            }
            catch
            {
                if (retVal != null)
                    retVal.Dispose();

                throw;
            }
        }

        #endregion
    }
}
