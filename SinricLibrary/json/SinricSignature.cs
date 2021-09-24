using Newtonsoft.Json;

namespace SinricLibrary.json
{
    internal class SinricSignature
    {
        [JsonProperty("HMAC")]
        public string Hmac { get; set; }
    }
}