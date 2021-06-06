# DnsProxy

### Description
DnsProxy is a lightweight DNS forwarder designed to forward DNS queries to real, recursive DNS servers based on configurable rules.
It's a small .Net 5 console application that can be installed as a Windows service.

DnsProxy was designed to speed up DNS resolution when using a VPN in a professional environment.

For instance, when connecting a company VPN through GlobalProtect client, all the DNS queries are forwarded to the company name servers and it can introduce a performance hit depending of the latency of the VPN.
With DnsProxy, the DNS queries are forwarded to upstreams name servers based on regular expressions.

### Configuration example
```json
// appsettings.json file
{
    "$schema": "https://json.schemastore.org/appsettings.json",
    "Listen": {
        "Address": "127.0.0.1",
        "Port": 53
    },
    "DefaultResolver": {
        "Address": "192.168.10.254",
        "Port": 53
    },
    "CustomResolvers": [
        {
            "Rule": "^(jira\\.mycompany\\.com|.+\\.corp\\.mycompany\\.com)$",
            "Address": "172.16.10.1",
            "Port": 53
        },
        {
            "Rule": "^(.+\\.aws\\.mycompany\\.com)$",
            "Address": "10.100.10.100",
            "Port": 53
        }
    ]
}
```

### Installation / Configuration
- Download the latest [release](https://github.com/gpailler/DnsProxy/releases) and extract it in a convenient place.
- Rename `appsettings.example.json` to `appsettings.json` and edit the settings according to your needs.
- Change your preferred DNS server to `127.0.0.1` (or execute the command `netsh interface ipv4 set dnsservers name="[Network Interface Name]" source=static address=127.0.0.1`).
- Run `DnsProxy.exe`.

### Run as a Service
⚠️ DnsProxy runs using the less privileged NetworkService account. You need to edit the permissions of the DnsProxy folder and add the `NETWORK SERVICE` account with `Read & Execute` permissions.
- Install DnsProxy as a service by running `DnsProxy.exe install`.
- Start DnsProxy with `DnsProxy.exe start` (or through the Services MMC).
- When executed as a service, DnsProxy logs can be found in `C:\Windows\ServiceProfiles\NetworkService\AppData\Local\Temp\DnsProxy.log`.

