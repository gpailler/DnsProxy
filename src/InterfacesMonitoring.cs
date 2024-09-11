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

    private readonly Dictionary<string, IPAddress[]> _originalDnsServers = new();

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
            _timer = new Timer(s_monitoringInterval);
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

        foreach (var (interfaceName, dnsServers) in _originalDnsServers)
        {
            _logger.Information("Restoring original DNS servers...");
            SetDnsServers(interfaceName, dnsServers);
        }
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        var matchingInterfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(x => _monitoringOptions.Interfaces!.Any(y => y == x.Name && x.OperationalStatus == OperationalStatus.Up))
            .Select(x => (Interface: x, Properties: x.GetIPProperties()))
            .Where(x => x.Properties.DnsAddresses.All(a => !a.Equals(_listeningAddress)));

        foreach (var (networkInterface, properties) in matchingInterfaces)
        {
            _originalDnsServers[networkInterface.Name] = properties.DnsAddresses.ToArray();

            _logger.Information("Replacing DNS servers...");
            SetDnsServers(networkInterface.Name, [_listeningAddress]);
        }

        _timer?.Start();
    }

    private void SetDnsServers(string interfaceName, IPAddress[] dnsServers)
    {
        Helpers.Run("netsh.exe", $"interface ipv4 set dnsservers name=\"{interfaceName}\" static {dnsServers[0]} primary", _logger);
        if (dnsServers.Length > 1)
        {
            Helpers.Run("netsh.exe", $"interface ipv4 add dnsservers name=\"{interfaceName}\" {dnsServers[1]} index=2", _logger);
        }
    }
}
