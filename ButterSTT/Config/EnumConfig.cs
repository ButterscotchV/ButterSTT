using System.Text.Json.Serialization;

namespace ButterSTT.Config
{
    public struct EnumConfig<T>
        where T : struct, Enum
    {
        [JsonPropertyName("options")]
        public readonly string[] Options { get; } = Enum.GetNames<T>();

        [JsonPropertyName("value")]
        public string Value { get; set; } = "";

        [JsonIgnore]
        public T EnumValue
        {
            readonly get => Enum.Parse<T>(Value, ignoreCase: true);
            set => Value = Enum.GetName(value)!;
        }

        public EnumConfig(T value)
        {
            EnumValue = value;
        }
    }
}
