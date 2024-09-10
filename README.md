# DnsProxy

[![Build Status](https://github.com/gpailler/DnsProxy/actions/workflows/main.yml/badge.svg)](https://github.com/gpailler/DnsProxy/actions/workflows/main.yml)
[![GitHub Release (Latest SemVer)](https://img.shields.io/github/v/release/gpailler/DnsProxy)](https://github.com/gpailler/DnsProxy/releases)
[![License](https://img.shields.io/github/license/gpailler/DnsProxy)](https://github.com/gpailler/DnsProxy/blob/master/LICENSE)

### Description

DnsProxy is a lightweight DNS forwarder designed to forward DNS queries to real, recursive DNS servers based on configurable rules.
It is a small .NET 8 console application that can be installed as a Windows service.

DnsProxy is intended to improve DNS resolution speed when using a VPN in a professional environment.

For example, when connected to a company VPN via the GlobalProtect client, all DNS queries are routed through the company's name servers, which can slow down performance, particularly when there is high VPN latency. With DnsProxy, DNS queries are forwarded to upstream name servers based on configurable regular expression rules.

### Configuration Example

```json
// appsettings.json file
{
    "$schema": "https://json.schemastore.org/appsettings.json",
    "Listen": {
        "Address": "127.0.0.1",
        "Port": 53
    },
    "Monitoring": {
        "Interfaces": [ "VPN" ]
    },
    "DefaultResolver": {
        "Address": "192.168.10.254",
        "Port": 53
    },
    "CustomResolvers": [
        {
            "Rule": "^(jira\\.mycompany\\.com|.+\\.corp\\.mycompany\\.com)\\.?$",
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

### Installation

1. Download the latest [release](https://github.com/gpailler/DnsProxy/releases) and extract it to a convenient location.
2. Rename `appsettings.example.json` to `appsettings.json`, and update the configuration to suit your needs.
3. Run `DnsProxy.exe`.

### Usage

To use DnsProxy, set the DNS server of the appropriate network interface to `127.0.0.1`:
- By manually adjusting the network adapter settings in the Windows GUI.
- By running the following command:
  `netsh interface ipv4 set dnsservers name="VPN" source=static address=127.0.0.1`
- Alternatively, let DnsProxy handle this for you. It can monitor network interfaces and automatically update DNS settings as needed (requires admin privileges).

### Running as a Service

- To install DnsProxy as a Windows service, run:
  `DnsProxy.exe service install`
- To uninstall DnsProxy as a Windows service, run:
  `DnsProxy.exe service uninstall`
