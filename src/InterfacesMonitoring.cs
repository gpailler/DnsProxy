using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Timers;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.NetworkManagement.IpHelper;
using DnsProxy.Options;
using Microsoft.Extensions.Options;
using Serilog;
using Timer = System.Timers.Timer;

namespace DnsProxy;

internal class InterfacesMonitoring
{
    private readonly MonitoringOptions _monitoringOptions;
    private readonly IPAddress _listeningAddress;
    private readonly ILogger _logger;
    private readonly Timer? _timer;
    private readonly Dictionary<Guid, IPAddress[]> _originalDnsServers = new();

    public InterfacesMonitoring(IOptions<MonitoringOptions> monitoringOptions, IOptions<ListenOptions> listenOptions, ILogger logger)
    {
        _monitoringOptions = monitoringOptions.Value;
        _listeningAddress = ((IPEndPoint)listenOptions.Value).Address;
        _logger = logger;

        if (!Helpers.IsElevatedAccount())
        {
            _logger.Warning("Interfaces monitoring is deactivated because it required elevated account.");
        }
        else if (_monitoringOptions.Interfaces?.Length > 0)
        {
            _timer = new Timer(_monitoringOptions.Interval);
            _timer.AutoReset = false;
            _timer.Elapsed += OnTimerElapsed;
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
            .Where(networkInterface => networkInterface.OperationalStatus == OperationalStatus.Up
                                       && _monitoringOptions.Interfaces!.Contains(networkInterface.Name))
            .Select(networkInterface => (Guid.Parse(networkInterface.Id), networkInterface.GetIPProperties().DnsAddresses))
            .Where(networkInterface => !networkInterface.DnsAddresses.Intersect([_listeningAddress]).Any());

        foreach (var (interfaceId, dnsServers) in matchingInterfaces)
        {
            if (dnsServers.Any())
            {
                _originalDnsServers[interfaceId] = dnsServers.ToArray();

                _logger.Information("Replacing DNS servers...");
                SetDnsServers(interfaceId, [_listeningAddress]);
            }
        }

        _timer?.Start();
    }

    private unsafe void SetDnsServers(Guid interfaceId, IPAddress[] dnsServers)
    {
        var settings = new DNS_INTERFACE_SETTINGS();
        settings.Flags = PInvoke.DNS_SETTING_NAMESERVER;
        settings.Version = PInvoke.DNS_INTERFACE_SETTINGS_VERSION1;
        var nameServers = Marshal.StringToHGlobalUni(string.Join(',', dnsServers.Select(x => x.ToString())));
        settings.NameServer = new PWSTR((char*)nameServers);

        try
        {
            var result = PInvoke.SetInterfaceDnsSettings(interfaceId, settings);
            if (result != WIN32_ERROR.NO_ERROR)
            {
                _logger.Error("Failed to set DNS settings for interface {InterfaceId}. Error: {Error}", interfaceId, result);
            }
        }
        finally
        {
            PInvoke.FreeInterfaceDnsSettings(ref settings);
        }
    }
}
