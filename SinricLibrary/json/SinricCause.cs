using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SinricLibrary.json
{
    internal class SinricCause
    {
        public const string PhysicalInteraction = "PHYSICAL_INTERACTION";
        public const string CauseType = "type";

        // misc fields
        [JsonExtensionData]
        public IDictionary<string, JToken> Fields { get; set; } = new Dictionary<string, JToken>();
    }
}