// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net.Sockets;
using System.Net;

namespace HttpPlugin;

/// <summary>
/// Extension methods on <see cref="System.Net.IPAddress"/>.
/// </summary>
public static class IPAddressExtensions
{
    /// <summary>
    /// Returns true if the IP address is in a private range.<br/>
    /// IPv4: Loopback, link local ("169.254.x.x"), class A ("10.x.x.x"), class B ("172.16.x.x" to "172.31.x.x") and class C ("192.168.x.x").<br/>
    /// IPv6: Loopback, link local, site local, unique local and private IPv4 mapped to IPv6.<br/>
    /// </summary>
    /// <param name="ip">The IP address.</param>
    /// <returns>True if the IP address was in a private range.</returns>
    /// <example><code>bool isPrivate = IPAddress.Parse("127.0.0.1").IsPrivate();</code></example>
    public static bool IsPrivate(this IPAddress ip)
    {
        // Map back to IPv4 if mapped to IPv6, for example "::ffff:1.2.3.4" to "1.2.3.4".
        if (ip.IsIPv4MappedToIPv6)
        {
            ip = ip.MapToIPv4();
        }

        // Checks loopback ranges for both IPv4 and IPv6.
        if (IPAddress.IsLoopback(ip))
        {
            return true;
        }

        // IPv4
        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            return IsPrivateIPv4(ip.GetAddressBytes());
        }

        // IPv6
        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return ip.IsIPv6LinkLocal ||
                   ip.IsIPv6UniqueLocal ||
                   ip.IsIPv6SiteLocal;
        }

        throw new NotSupportedException(
                $"IP address family {ip.AddressFamily} is not supported, expected only IPv4 (InterNetwork) or IPv6 (InterNetworkV6).");
    }

    private static bool IsPrivateIPv4(byte[] ipv4Bytes)
    {
        // Link local (no IP assigned by DHCP): 169.254.0.0 to 169.254.255.255 (169.254.0.0/16)
        bool IsLinkLocal() => ipv4Bytes[0] == 169 && ipv4Bytes[1] == 254;

        // Class A private range: 10.0.0.0 – 10.255.255.255 (10.0.0.0/8)
        bool IsClassA() => ipv4Bytes[0] == 10;

        // Class B private range: 172.16.0.0 – 172.31.255.255 (172.16.0.0/12)
        bool IsClassB() => ipv4Bytes[0] == 172 && ipv4Bytes[1] >= 16 && ipv4Bytes[1] <= 31;

        // Class C private range: 192.168.0.0 – 192.168.255.255 (192.168.0.0/16)
        bool IsClassC() => ipv4Bytes[0] == 192 && ipv4Bytes[1] == 168;

        return IsLinkLocal() || IsClassA() || IsClassC() || IsClassB();
    }
}
