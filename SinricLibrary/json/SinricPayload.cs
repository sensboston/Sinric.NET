using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SinricLibrary.json
{
    internal class SinricPayload
    {
        public class Client
        {
            public const string Csharp = "csharp";
        }

        public class Messages
        {
            public const string Ok = "OK";
        }

        public class Result
        {
            public const bool Fail = false;
            public const bool Success = true;
        }

        public class MessageType
        {
            public const string Event = "event";
            public const string Response = "response";
            public const string Request = "request";
        }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("clientId")]
        public string ClientId { get; set; } = Client.Csharp;

        [JsonProperty("createdAt")]
        public uint CreatedAt { get; set; }

        [JsonIgnore]
        public DateTime CreatedAtUtc
        {
            get => Utility.UnixEpoch.AddSeconds(CreatedAt);
            set => CreatedAt = (uint)value.Subtract(Utility.UnixEpoch).TotalSeconds;
        }

        [JsonProperty("deviceAttributes")] 
        public List<object> DeviceAttributes { get; set; } = new List<object>();

        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }

        [JsonProperty("replyToken")]
        public string ReplyToken { get; set; }

        [JsonProperty("value")]
        public SinricValue Value { get; set; } = new SinricValue();

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Returns the requested value from the available list, if present on the message.
        /// </summary>
        /// <typeparam name="T">Expected data type</typeparam>
        /// <param name="key">The name of the value to retrieve</param>
        /// <returns>The value of type T if exists; otherwise null.</returns>
        public T GetValue<T>(string key) where T:class
        {
            Value.Fields.TryGetValue(key, out var value);

            return value?.Value<T>();
        }

        public SinricPayload SetValue(string key, object value)
        {
            if (value != null)
                Value.Fields[key] = JToken.FromObject(value);
            else
                Value.Fields[key] = null;

            return this;
        }

        public void SetState(string newState)
        {
            SetValue(SinricValue.State, newState);
        }

        [JsonProperty("cause")]
        public SinricCause Cause { get; set; } = new SinricCause();


        /// <summary>
        /// Returns the requested value from the available list, if present on the message.
        /// </summary>
        /// <typeparam name="T">Expected data type</typeparam>
        /// <param name="key">The name of the value to retrieve</param>
        /// <returns>The value of type T if exists; otherwise null.</returns>
        public T GetCause<T>(string key) where T : class
        {
            Cause.Fields.TryGetValue(key, out var cause);

            return cause?.Value<T>();
        }

        public SinricPayload SetCause(string key, object cause)
        {
            if (cause != null)
                Cause.Fields[key] = JToken.FromObject(cause);
            else
                Cause.Fields[key] = null;

            return this;
        }
    }
}