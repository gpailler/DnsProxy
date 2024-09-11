using System.Net;

namespace DnsProxy.Options;

internal class EndPointOptions
{
    public string? Address { get; set; }

    public ushort Port { get; set; }

    public int Timeout { get; set; } = 10000;

    public static implicit operator IPEndPoint(EndPointOptions endPoint)
    {
        if (endPoint.Address == null)
        {
            throw new ArgumentException($"{nameof(Address)} option is null.");
        }

        try
        {
            return new IPEndPoint(IPAddress.Parse(endPoint.Address), endPoint.Port);
        }
        catch (Exception ex)
        {
            throw new FormatException($"{nameof(Address)} option '{endPoint.Address}' is invalid.", ex);
        }
    }

    public override string ToString()
    {
        return ((IPEndPoint)this).ToString();
    }
}
