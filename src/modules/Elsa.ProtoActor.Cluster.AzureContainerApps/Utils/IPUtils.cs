using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using JetBrains.Annotations;

namespace Proto.Cluster.AzureContainerApps.Utils;

/// <summary>
///    Utility methods for configuration.
/// </summary>
public static class IPUtils
{
    /// <summary>
    ///   Finds the smallest IP address on the machine.
    /// </summary>
    /// <param name="family">The address family to look for.</param>
    /// <param name="includeLoopback">Whether to include loopback addresses.</param>
    public static IPAddress FindSmallestIpAddress(AddressFamily family = AddressFamily.InterNetwork, bool includeLoopback = false)
    {
        var addressCandidatesQuery = NetworkInterface.GetAllNetworkInterfaces()
            .Where(nif => nif.OperationalStatus == OperationalStatus.Up)
            .SelectMany(nif => nif.GetIPProperties().UnicastAddresses.Select(a => a.Address))
            .Where(addr => addr.AddressFamily == family);

        if (!includeLoopback) 
            addressCandidatesQuery = addressCandidatesQuery.Where(addr => !IPAddress.IsLoopback(addr));

        var addressCandidates = addressCandidatesQuery.ToList();

        return PickSmallestIpAddress(addressCandidates);
    }

    private static IPAddress PickSmallestIpAddress(IEnumerable<IPAddress> candidates)
    {
        IPAddress result = null!;

        foreach (var addr in candidates)
            if (CompareIpAddresses(addr, result))
                result = addr;

        return result;

        static bool CompareIpAddresses(IPAddress lhs, [CanBeNull] IPAddress rhs)
        {
            if (rhs == null)
                return true;

            var lbytes = lhs.GetAddressBytes();
            var rbytes = rhs.GetAddressBytes();

            if (lbytes.Length != rbytes.Length) return lbytes.Length < rbytes.Length;

            for (var i = 0; i < lbytes.Length; i++)
                if (lbytes[i] != rbytes[i])
                    return lbytes[i] < rbytes[i];

            return false;
        }
    }
}