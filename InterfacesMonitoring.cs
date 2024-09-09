using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Timers;
using DnsProxy.Options;
using Microsoft.Extensions.Options;
using Serilog;
using Timer = System.Timers.Timer;

namespace DnsProxy;

internal class InterfacesMonitoring
{
    private static readonly TimeSpan s_monitoringInterval = TimeSpan.FromSeconds(5);

    private readonly MonitoringOptions _monitoringOptions;
    private readonly IPAddress _listeningAddress;
    private readonly ILogger _logger;
    private readonly Timer? _timer;

    private readonly Dictionary<string, string[]> _originalDnsServers = new();

    public InterfacesMonitoring(IOptions<MonitoringOptions> monitoringOptions, IOptions<ListenOptions> listenOptions, ILogger logger)
    {
        _monitoringOptions = monitoringOptions.Value;
        ArgumentNullException.ThrowIfNull(listenOptions.Value.Address, nameof(listenOptions.Value.Address));
        _listeningAddress = IPAddress.Parse(listenOptions.Value.Address);
        _logger = logger;

        if (Helpers.IsElevatedAccount())
        {
            _timer = new Timer(s_monitoringInterval);
            _timer.AutoReset = false;
            _timer.Elapsed += OnTimerElapsed;
        }
        else
        {
            _logger.Warning("Interfaces monitoring is deactivated because it required elevated account.");
        }
    }

    public void Start()
    {
        _timer?.Start();
    }

    public void Stop()
    {
        _timer?.Dispose();

        foreach (var (interfaceId, dnsServers) in _originalDnsServers)
        {
            _logger.Information("Restoring original DNS servers...");
            SetDnsServers(interfaceId, dnsServers);
        }
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        var matchingInterfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(x => _monitoringOptions.Interfaces.Any(y => y == x.Name && x.OperationalStatus == OperationalStatus.Up))
            .Select(x => (Interface: x, Properties: x.GetIPProperties()))
            .Where(x => x.Properties.DnsAddresses.All(y => y.Equals(_listeningAddress) == false));

        foreach (var (networkInterface, properties) in matchingInterfaces)
        {
            _originalDnsServers[networkInterface.Id] = properties.DnsAddresses.Select(x => x.ToString()).ToArray();

            _logger.Information("Replacing DNS servers...");
            SetDnsServers(networkInterface.Id, [_listeningAddress.ToString()]);
        }

        _timer?.Start();
    }

    private void SetDnsServers(string interfaceId, string[] dnsServers)
    {
        var managementClass = new ManagementClass("Win32_NetworkAdapterConfiguration");
        var networkAdapters = managementClass.GetInstances().OfType<ManagementObject>();

        foreach (var adapter in networkAdapters)
        {
            if (adapter["SettingID"].ToString() == interfaceId)
            {
                var newDnsParameters = adapter.GetMethodParameters("SetDNSServerSearchOrder");
                newDnsParameters["DNSServerSearchOrder"] = dnsServers;
                var setDnsResult = adapter.InvokeMethod("SetDNSServerSearchOrder", newDnsParameters, null);

                uint returnValue = (uint)setDnsResult["ReturnValue"];
                if (returnValue == 0)
                {
                    _logger.Information("DNS servers for interface {Interface} updated successfully.", adapter["Caption"]);
                }
                else
                {
                    _logger.Warning("Failed to update DNS servers for inteface {Interface}. Error code: {Error}", adapter["Caption"], returnValue);
                }
            }
        }
    }
}
