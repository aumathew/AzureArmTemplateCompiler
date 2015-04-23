using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HelloRPs.TemplateObjectModel
{

    public class FrontendIpConfigurationProperties
    {

        [JsonProperty("publicIPAddress")]
        public ResourceId PublicIPAddress { get; set; }
    }

    public class FrontendIPConfiguration
    {

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("properties")]
        public FrontendIpConfigurationProperties Properties { get; set; }
    }

    public class ResourceId
    {

        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class BackendAddressPoolProperties
    {

        [JsonProperty("backendIPConfigurations")]
        public IList<ResourceId> BackendIPConfigurationResourceIds { get; set; }
    }

    public class BackendAddressPool
    {

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("properties")]
        public BackendAddressPoolProperties Properties { get; set; }
    }


    public class NatRuleProperties
    {

        [JsonProperty("frontendIPConfigurations")]
        public IList<ResourceId> FrontendIPConfigurationResourceIds { get; set; }

        [JsonProperty("backendIPConfiguration")]
        public ResourceId BackendIPConfigurationId { get; set; }

        [JsonProperty("protocol")]
        public string Protocol { get; set; }

        [JsonProperty("frontendPort")]
        public int FrontendPort { get; set; }

        [JsonProperty("backendPort")]
        public int BackendPort { get; set; }

        [JsonProperty("enableFloatingIP")]
        public bool EnableFloatingIP { get; set; }
    }

    public class InboundNatRule
    {

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("properties")]
        public NatRuleProperties Properties { get; set; }
    }


    public class LoadbalancingRuleProperties
    {

        [JsonProperty("frontendIPConfigurations")]
        public IList<ResourceId> FrontendIPConfigurationIds { get; set; }

        [JsonProperty("backendAddressPool")]
        public ResourceId BackendAddressPoolId { get; set; }

        [JsonProperty("protocol")]
        public string Protocol { get; set; }

        [JsonProperty("frontendPort")]
        public string FrontendPort { get; set; }

        [JsonProperty("backendPort")]
        public string BackendPort { get; set; }

        [JsonProperty("enableFloatingIP")]
        public string EnableFloatingIP { get; set; }

        [JsonProperty("idleTimeoutInMinutes")]
        public string IdleTimeoutInMinutes { get; set; }

        [JsonProperty("probe")]
        public ResourceId ProbeId { get; set; }
    }

    public class LoadBalancingRule
    {

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("properties")]
        public LoadbalancingRuleProperties Properties { get; set; }
    }

    public class ProbeProperties
    {

        [JsonProperty("protocol")]
        public string Protocol { get; set; }

        [JsonProperty("port")]
        public string Port { get; set; }

        [JsonProperty("intervalInSeconds")]
        public string IntervalInSeconds { get; set; }

        [JsonProperty("numberOfProbes")]
        public string NumberOfProbes { get; set; }
    }

    public class ProbeDefinition
    {

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("properties")]
        public ProbeProperties Properties { get; set; }
    }

    public class LoadbalancerProperties
    {

        [JsonProperty("frontendIPConfigurations")]
        public IList<FrontendIPConfiguration> FrontendIPConfigurations { get; set; }

        [JsonProperty("backendAddressPools")]
        public IList<BackendAddressPool> BackendAddressPools { get; set; }

        [JsonProperty("inboundNatRules")]
        public IList<InboundNatRule> InboundNatRules { get; set; }

        [JsonProperty("loadBalancingRules")]
        public IList<LoadBalancingRule> LoadBalancingRules { get; set; }

        [JsonProperty("probes")]
        public IList<ProbeDefinition> Probes { get; set; }
    }

    public class LoadbalancerDefinition : Resource
    {

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public override string Type { get { return "Microsoft.Network/loadBalancers"; } }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("dependsOn")]
        public IList<string> DependsOn { get; set; }

        [JsonProperty("properties")]
        public LoadbalancerProperties Properties { get; set; }
    }
}
