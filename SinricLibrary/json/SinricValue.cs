using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SinricLibrary.json
{
    internal class SinricValue
    {
        public const string State = "state";
        public const string ThermostatMode = "thermostatMode";
        public const string Temperature = "temperature";

        // misc fields
        [JsonExtensionData]
        public IDictionary<string, JToken> Fields { get; set; } = new Dictionary<string, JToken>();
    }
}