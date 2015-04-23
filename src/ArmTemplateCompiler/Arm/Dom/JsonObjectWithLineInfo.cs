using Newtonsoft.Json;

namespace ArmEngine.Arm.Dom
{
    public abstract class JsonObjectWithLineInfo : IJsonLineInfo
    {
        public bool HasLineInfo()
        {
            return true;
        }

        [JsonIgnore]
        public int LineNumber { get; set; }

        [JsonIgnore]
        public int LinePosition { get; set; }
    }
}