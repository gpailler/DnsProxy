using System.Collections.ObjectModel;

namespace DnsProxy.Options
{
    internal class CustomResolversOptions : Collection<CustomResolversOptions.Item>
    {
        public const string Key = "CustomResolvers";

        internal class Item : EndPointOptions
        {
            public string? Rule { get; set; }
        }
    }
}
