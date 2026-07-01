using System.Text.Json.Serialization;

namespace DockhandMcp;

// Login payload for username/password (cookie) auth. provider defaults to "local".
internal sealed record LoginRequest(
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("password")] string Password,
    [property: JsonPropertyName("provider")] string Provider = "local");

// Payloads we CONSTRUCT for stack operations. Null fields are omitted (see context options).
internal sealed record StackCreate(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("compose")] string Compose,
    [property: JsonPropertyName("start")] bool? Start = null);

internal sealed record ComposeUpdate(
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("restart")] bool? Restart = null);

internal sealed record DeployOptions(
    [property: JsonPropertyName("pull")] bool? Pull = null,
    [property: JsonPropertyName("build")] bool? Build = null,
    [property: JsonPropertyName("forceRecreate")] bool? ForceRecreate = null);

internal sealed record DownOptions(
    [property: JsonPropertyName("removeVolumes")] bool? RemoveVolumes = null);

internal sealed record ImagePull(
    [property: JsonPropertyName("image")] string Image);

// Source-generated, reflection-free (de)serialization for AOT. Only the payloads we build are typed;
// GET/response bodies are passed through as raw JSON text.
[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(StackCreate))]
[JsonSerializable(typeof(ComposeUpdate))]
[JsonSerializable(typeof(DeployOptions))]
[JsonSerializable(typeof(DownOptions))]
[JsonSerializable(typeof(ImagePull))]
[JsonSerializable(typeof(string))]
internal sealed partial class DockhandJsonContext : JsonSerializerContext;
