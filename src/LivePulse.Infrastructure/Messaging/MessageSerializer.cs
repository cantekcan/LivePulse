using System.Text.Json;
using System.Text.Json.Serialization;

namespace LivePulse.Infrastructure.Messaging;

public static class MessageSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize<TMessage>(TMessage message) => JsonSerializer.Serialize(message, Options);

    public static TMessage? Deserialize<TMessage>(string payload) => JsonSerializer.Deserialize<TMessage>(payload, Options);
}
