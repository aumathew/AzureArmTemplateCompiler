using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HelloRPs.TemplateObjectModel
{
    public class Parameter
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("defaultValue")]
        public JToken DefaultValue { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

    }

    public class ArmTemplate
    {
        [JsonProperty("$schema")]
        public string Schema { get; set; }

        [JsonProperty("contentVersion")]
        public string ContentVersion { get; set; }

        [JsonProperty("parameters")]
        public Dictionary<string, Parameter> Parameters { get; set; }

        [JsonProperty("variables")]
        public Dictionary<string,JToken> Variables { get; set; }

        [JsonProperty("resources")]
        public IList<Resource> Resources { get; set; }
    }
}
