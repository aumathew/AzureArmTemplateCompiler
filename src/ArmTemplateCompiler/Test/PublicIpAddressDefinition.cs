using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HelloRPs.TemplateObjectModel
{
    public class DnsSettings
    {
        [JsonProperty("domainNameLabel")]
        public string DomainNameLabel { get; set; }
    }

    public class Properties
    {

        [JsonProperty("publicIPAllocationMethod")]
        public string PublicIPAllocationMethod { get; set; }

        [JsonProperty("dnsSettings")]
        public DnsSettings DnsSettings { get; set; }
    }

    public class PublicIpAddressDefinition : Resource
    {
        [JsonProperty("type")]
        public override string Type { get { return "Microsoft.Network/publicIPAddresses"; } }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("properties")]
        public Properties Properties { get; set; }
    }
}
