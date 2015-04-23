using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HelloRPs.TemplateObjectModel
{
    public class AddressSpace
    {

        [JsonProperty("addressPrefixes")]
        public IList<string> AddressPrefixes { get; set; }
    }

    public class SubnetProperties
    {

        [JsonProperty("addressPrefix")]
        public string AddressPrefix { get; set; }
    }

    public class Subnet
    {

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("properties")]
        public SubnetProperties Properties { get; set; }
    }

    public class VirtualNetworkProperties
    {

        [JsonProperty("addressSpace")]
        public AddressSpace AddressSpace { get; set; }

        [JsonProperty("subnets")]
        public IList<Subnet> Subnets { get; set; }
    }

    public class VirtualNetworkDefinition : Resource
    {
        [JsonProperty("type")]
        public override string Type { get { return "Microsoft.Network/virtualNetworks"; } }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("properties")]
        public VirtualNetworkProperties Properties { get; set; }
    }
}
