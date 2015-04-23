using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HelloRPs.TemplateObjectModel
{
    public class IpConfigurationProperties
    {

        [JsonProperty("privateIPAllocationMethod")]
        public string PrivateIPAllocationMethod { get; set; }

        [JsonProperty("publicIPAddress")]
        public ResourceId PublicIPAddressResourceId { get; set; }

        [JsonProperty("subnet")]
        public ResourceId SubnetResourceId { get; set; }
    }

    public class IpConfiguration
    {

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("properties")]
        public IpConfigurationProperties Properties { get; set; }
    }

    public class NetworkInterfaceProperties
    {

        [JsonProperty("ipConfigurations")]
        public IList<IpConfiguration> IpConfigurations { get; set; }
    }

    public class NetworkInterfaceDefinition : Resource
    {

        [JsonProperty("type")]
        public override string Type { get { return "Microsoft.Network/networkInterfaces"; } }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("dependsOn")]
        public IList<string> DependsOn { get; set; }

        [JsonProperty("properties")]
        public NetworkInterfaceProperties Properties { get; set; }
    }
}
