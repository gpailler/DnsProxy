using System.ComponentModel.DataAnnotations;
using System.Net;

namespace DnsProxy.Options;

internal abstract class EndPointOptions
{
    [Required]
    [RegularExpression("^((25[0-5]|(2[0-4]|1\\d|[1-9]|)\\d)\\.?\\b){4}$")]
    public string Address { get; set; } = null!;

    [Required]
    [Range(1, 65535)]
    public ushort Port { get; set; }

    [Required]
    [Range(1, 30_000)]
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
