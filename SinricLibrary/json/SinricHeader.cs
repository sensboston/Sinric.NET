using Newtonsoft.Json;

namespace SinricLibrary.json
{
    internal class SinricHeader
    {
        [JsonProperty("payloadVersion")]
        public int PayloadVersion { get; set; } = 2;

        [JsonProperty("signatureVersion")]
        public int SignatureVersion { get; set; } = 1;
    }
}