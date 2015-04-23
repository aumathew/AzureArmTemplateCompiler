using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HelloRPs.TemplateObjectModel
{

    public class HardwareProfile
    {

        [JsonProperty("vmSize")]
        public string VmSize { get; set; }
    }

    public class WindowsConfiguration
    {

        [JsonProperty("provisionVMAgent")]
        public bool ProvisionVMAgent { get; set; }
    }

    public class OsProfile
    {

        [JsonProperty("computername")]
        public string Computername { get; set; }

        [JsonProperty("adminUsername")]
        public string AdminUsername { get; set; }

        [JsonProperty("adminPassword")]
        public string AdminPassword { get; set; }

        [JsonProperty("windowsConfiguration")]
        public WindowsConfiguration WindowsConfiguration { get; set; }


    }

    public class Image
    {

        [JsonProperty("uri")]
        public string Uri { get; set; }
    }

    public class Vhd
    {

        [JsonProperty("uri")]
        public string Uri { get; set; }
    }

    public class OsDisk
    {

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("osType")]
        public string OsType { get; set; }

        [JsonProperty("caching")]
        public string Caching { get; set; }

        [JsonProperty("image")]
        public Image Image { get; set; }

        [JsonProperty("vhd")]
        public Vhd Vhd { get; set; }
    }


    public class StorageProfile
    {
        [JsonProperty("osDisk")]
        public OsDisk OsDisk { get; set; }

        [JsonProperty("sourceImage")]
        public ResourceId SourceImage { get; set; }

        [JsonProperty("destinationVhdsContainer")]
        public string DestinationVhdsContainer { get; set; }
    }



    public class NetworkProfile
    {

        [JsonProperty("networkInterfaces")]
        public IList<ResourceId> NetworkInterfaces { get; set; }
    }

    public class VirtualMachineProperties
    {

        [JsonProperty("availabilitySet")]
        public ResourceId AvailabilitySetId { get; set; }

        [JsonProperty("hardwareProfile")]
        public HardwareProfile HardwareProfile { get; set; }

        [JsonProperty("osProfile")]
        public OsProfile OsProfile { get; set; }

        [JsonProperty("storageProfile")]
        public StorageProfile StorageProfile { get; set; }

        [JsonProperty("networkProfile")]
        public NetworkProfile NetworkProfile { get; set; }
    }

    public class VirtualMachineDefinition : Resource
    {

        [JsonProperty("type")]
        public override string Type { get { return "Microsoft.Compute/virtualMachines"; } }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("dependsOn")]
        public IList<string> DependsOn { get; set; }

        [JsonProperty("properties")]
        public VirtualMachineProperties Properties { get; set; }
    }
}
