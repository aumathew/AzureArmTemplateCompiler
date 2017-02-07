using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ArmEngine.Arm.Dom
{
    public class Parameter : JsonObjectWithLineInfo
    {
        [JsonProperty("type", Required = Required.Always)]
        public string Type { get; set; }

        [JsonProperty("defaultValue")]
        public JToken DefaultValue { get; set; }

        [JsonProperty("maxLength")]
        public JToken MaxLength { get; set; }
    }

    public class ArmTemplate : JsonObjectWithLineInfo
    {
        [JsonProperty("$schema", Required = Required.Always)]
        public string Schema { get; set; }

        [JsonProperty("contentVersion", Required = Required.Always)]
        public string ContentVersion { get; set; }

        [JsonProperty("parameters", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, Parameter> Parameters { get; set; }

        [JsonProperty("variables", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Variables { get; set; }

        [JsonProperty("scripts", NullValueHandling = NullValueHandling.Ignore)]
        public ArmTemplateScripts Scripts { get; set; }

        [JsonProperty("resources", Required = Required.Always)]
        public IList<JObject> Resources { get; set; }

        [JsonProperty("outputs", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, OutputItem> Outputs { get; set; }
    }

    public class ArmTemplateParameterList : JsonObjectWithLineInfo
    {
        [JsonProperty("$schema", Required = Required.Always)]
        public string Schema { get; set; }

        [JsonProperty("contentVersion", Required = Required.Always)]
        public string ContentVersion { get; set; }

        [JsonProperty("parameters", Required = Required.Always)]
        public Dictionary<string, ArmTemplateParameter> Parameters { get; set; }
    }

    public enum ArmTemplateScriptType
    {
        BeforeResourceEvaluation,
        AfterResourceEvaluation,
    }

    public class ArmTemplateScripts
    {
        [JsonProperty("beforeResourceEval")]
        public List<ArmTemplateScript> BeforeResourceEval { get; set; }

        [JsonProperty("afterResourceEval")]
        public List<ArmTemplateScript> AfterResourceEval { get; set; }

    }

    public class ArmTemplateScript : JsonObjectWithLineInfo
    {
        [JsonProperty("uri", Required = Required.Always)]
        public string ScriptUri { get; set; }

    }

    public class ArmTemplateParameter : JsonObjectWithLineInfo
    {
        [JsonProperty("value")]
        public JToken Value { get; set; }
    }

    public class ConverterWithLinfo : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var info = reader as IJsonLineInfo;
            if (typeof(JsonObjectWithLineInfo).IsAssignableFrom(objectType))
            {
                var p = Activator.CreateInstance(objectType) as JsonObjectWithLineInfo;
                int lineNumber = (reader as IJsonLineInfo).LineNumber;
                int col = (reader as IJsonLineInfo).LinePosition;
                p.LineNumber = lineNumber;
                p.LinePosition = col;
                serializer.Populate(reader, p);
                return p;
            }
            else if (typeof(JToken).IsAssignableFrom(objectType))
            {
                return JToken.Load(reader);
            }
            return null;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(JsonObjectWithLineInfo).IsAssignableFrom(objectType) || typeof(JToken).IsAssignableFrom(objectType);
        }
    }

    public class OutputItem : JsonObjectWithLineInfo
    {
        [JsonProperty(PropertyName = "value")]
        public JToken Value { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
    }
}
