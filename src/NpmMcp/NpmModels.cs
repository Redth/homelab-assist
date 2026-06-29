using System.Text.Json.Serialization;

namespace NpmMcp;

// Auth
internal sealed record TokenRequest(
    [property: JsonPropertyName("identity")] string Identity,
    [property: JsonPropertyName("secret")] string Secret);

internal sealed record TokenResponse(
    [property: JsonPropertyName("token")] string? Token,
    [property: JsonPropertyName("expires")] string? Expires);

// Empty meta object for proxy-host create/update payloads. All fields optional so it
// serializes as {} unless something is set.
internal sealed record NpmMeta(
    [property: JsonPropertyName("letsencrypt_agree")] bool? LetsencryptAgree = null,
    [property: JsonPropertyName("dns_challenge")] bool? DnsChallenge = null);

// Payload we CONSTRUCT for create/update. Null fields are omitted (see NpmJsonContext
// options) so the same type works for full create and partial update.
internal sealed record ProxyHostPayload(
    [property: JsonPropertyName("domain_names")] string[]? DomainNames = null,
    [property: JsonPropertyName("forward_scheme")] string? ForwardScheme = null,
    [property: JsonPropertyName("forward_host")] string? ForwardHost = null,
    [property: JsonPropertyName("forward_port")] int? ForwardPort = null,
    [property: JsonPropertyName("certificate_id")] int? CertificateId = null,
    [property: JsonPropertyName("ssl_forced")] bool? SslForced = null,
    [property: JsonPropertyName("hsts_enabled")] bool? HstsEnabled = null,
    [property: JsonPropertyName("http2_support")] bool? Http2Support = null,
    [property: JsonPropertyName("block_exploits")] bool? BlockExploits = null,
    [property: JsonPropertyName("caching_enabled")] bool? CachingEnabled = null,
    [property: JsonPropertyName("allow_websocket_upgrade")] bool? AllowWebsocketUpgrade = null,
    [property: JsonPropertyName("access_list_id")] int? AccessListId = null,
    [property: JsonPropertyName("advanced_config")] string? AdvancedConfig = null,
    [property: JsonPropertyName("locations")] object[]? Locations = null,
    [property: JsonPropertyName("meta")] NpmMeta? Meta = null);

// Source-generated, reflection-free (de)serialization context for AOT. Only the types we
// build or parse with strong typing live here; GET responses are passed through as raw JSON.
[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
[JsonSerializable(typeof(TokenRequest))]
[JsonSerializable(typeof(TokenResponse))]
[JsonSerializable(typeof(ProxyHostPayload))]
[JsonSerializable(typeof(NpmMeta))]
[JsonSerializable(typeof(string))]
internal sealed partial class NpmJsonContext : JsonSerializerContext;
