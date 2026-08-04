using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FluentConfig.Protocol
{
    /// <summary>
    /// Wire envelope discriminator values for <see cref="WireMessage.Kind"/>.
    /// </summary>
    public static class WireKinds
    {
        public const string Request = "request";
        public const string Response = "response";
        public const string Event = "event";
    }

    /// <summary>
    /// Top-level message on the WebView2 postMessage channel (either direction).
    /// Requests and responses share a correlation <see cref="Id"/>; events are one-way pushes.
    /// </summary>
    public sealed class WireMessage
    {
        /// <summary>"request" | "response" | "event"</summary>
        [JsonProperty("kind")]
        public string Kind { get; set; }

        /// <summary>Correlation id for request/response pairs. Omitted on events.</summary>
        [JsonProperty("id")]
        public long? Id { get; set; }

        /// <summary>RPC method name (requests only).</summary>
        [JsonProperty("method")]
        public string Method { get; set; }

        /// <summary>RPC params object (requests only).</summary>
        [JsonProperty("params")]
        public JToken Params { get; set; }

        /// <summary>Successful RPC result (responses only; mutually exclusive with Error).</summary>
        [JsonProperty("result")]
        public JToken Result { get; set; }

        /// <summary>RPC failure (responses only; mutually exclusive with Result).</summary>
        [JsonProperty("error")]
        public RpcError Error { get; set; }

        /// <summary>Push event name (events only). See <see cref="PushEventNames"/>.</summary>
        [JsonProperty("event")]
        public string Event { get; set; }

        /// <summary>Push event payload (events only).</summary>
        [JsonProperty("payload")]
        public JToken Payload { get; set; }

        public static WireMessage Request(long id, string method, object paramsObj = null) => new WireMessage
        {
            Kind = WireKinds.Request,
            Id = id,
            Method = method,
            Params = paramsObj == null ? null : JToken.FromObject(paramsObj, ProtocolJson.CreateSerializer()),
        };

        public static WireMessage ResponseResult(long id, object result = null) => new WireMessage
        {
            Kind = WireKinds.Response,
            Id = id,
            Result = result == null ? JValue.CreateNull() : JToken.FromObject(result, ProtocolJson.CreateSerializer()),
        };

        public static WireMessage ResponseError(long id, string code, string message) => new WireMessage
        {
            Kind = WireKinds.Response,
            Id = id,
            Error = new RpcError { Code = code, Message = message },
        };

        public static WireMessage Push(string eventName, object payload = null) => new WireMessage
        {
            Kind = WireKinds.Event,
            Event = eventName,
            Payload = payload == null ? null : JToken.FromObject(payload, ProtocolJson.CreateSerializer()),
        };
    }

    public sealed class RpcError
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
