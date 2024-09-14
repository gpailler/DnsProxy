using System.Net;

namespace DnsProxy.Options;

internal class EndPointOptions
{
    public string Address { get; set; } = null!;

    public ushort Port { get; set; }

    public int Timeout { get; set; } = 10000;

    public static implicit operator IPEndPoint(EndPointOptions endPoint)
    {
        return new IPEndPoint(IPAddress.Parse(endPoint.Address), endPoint.Port);
    }

    public override string ToString()
    {
        return ((IPEndPoint)this).ToString();
    }
}
