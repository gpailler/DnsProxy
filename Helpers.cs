using System.Security.Principal;

namespace DnsProxy;

public static class Helpers
{
    public static bool IsElevatedAccount()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}
