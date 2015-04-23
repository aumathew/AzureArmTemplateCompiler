using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HelloRPs.TemplateObjectModel
{
    public class ResourceConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
           serializer.Serialize(writer, value);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var obj = serializer.Deserialize<JObject>(reader);
            var rop = obj["type"] as JValue;
            if (rop != null && rop.Value != null)
            {
                switch (rop.Value.ToString().ToUpperInvariant())
                {
                    case "MICROSOFT.COMPUTE/AVAILABILITYSETS":
                        return JsonConvert.DeserializeObject<AvailabilitySetDefinition>(JsonConvert.SerializeObject(obj));
                    case "MICROSOFT.NETWORK/PUBLICIPADDRESSES":
                        return JsonConvert.DeserializeObject<PublicIpAddressDefinition>(JsonConvert.SerializeObject(obj));
                    case "MICROSOFT.NETWORK/NETWORKINTERFACES":
                        return JsonConvert.DeserializeObject<NetworkInterfaceDefinition>(JsonConvert.SerializeObject(obj));
                    case "MICROSOFT.NETWORK/VIRTUALNETWORKS":
                        return JsonConvert.DeserializeObject<VirtualNetworkDefinition>(JsonConvert.SerializeObject(obj));
                    case "MICROSOFT.NETWORK/LOADBALANCERS":
                        return JsonConvert.DeserializeObject<LoadbalancerDefinition>(JsonConvert.SerializeObject(obj));
                    case "MICROSOFT.COMPUTE/VIRTUALMACHINES":
                        return JsonConvert.DeserializeObject<VirtualMachineDefinition>(JsonConvert.SerializeObject(obj));
                }

            }
            return null;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType.IsAssignableFrom(typeof (Resource));
        }
    }
    //[JsonConverter(typeof(ResourceConverter))]
    public abstract class Resource
    {
        [JsonProperty("apiVersion")]
        public string ApiVersion { get; set; }

        public abstract string Type { get; }
    }

    public class AvailabilitySetDefinition : Resource
    {

        [JsonProperty("type")]
        public override string Type { get { return "Microsoft.Compute/availabilitySets"; }}

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }
    }
}
