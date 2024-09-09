namespace DnsProxy.Options;

internal class MonitoringOptions
{
    public const string Key = "Monitoring";

    public string[] Interfaces { get; set; } = [];
}
