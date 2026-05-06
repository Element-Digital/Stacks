using System.Text.Json.Serialization;

namespace Stacks.State;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Dictionary<string, GameState>))]
internal sealed partial class StateJsonContext : JsonSerializerContext;
