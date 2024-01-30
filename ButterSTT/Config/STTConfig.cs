using System.Text.Json.Serialization;

namespace ButterSTT.Config
{
    public record STTConfig
    {
        public static readonly STTConfig Default = new();

        [JsonPropertyName("config_version")]
        public int ConfigVersion { get; set; } = 0;

        [JsonPropertyName("models_path")]
        public string ModelsPath { get; set; } = "Models";

        [JsonPropertyName("microphone_device_number")]
        public int MicrophoneDeviceNumber { get; set; } = 0;

        [JsonPropertyName("osc_address")]
        public string OSCAddress { get; set; } = "127.0.0.1";

        [JsonPropertyName("osc_port")]
        public int OSCPort { get; set; } = 9000;

        [JsonPropertyName("osc_chatbox_ratelimit_s")]
        public double OSCChatboxRateLimitS { get; set; } = 1.3;

        [JsonPropertyName("message_length")]
        public int MessageLength { get; set; } = 144;

        [JsonPropertyName("max_words_dequeued")]
        public int MaxWordsDequeued { get; set; } = 10;

        [JsonPropertyName("realtime_queue_padding")]
        public int RealtimeQueuePadding { get; set; } = 24;

        [JsonPropertyName("word_time_s")]
        public double WordTimeS { get; set; } = 5.0;

        [JsonPropertyName("hard_word_time_s")]
        public double HardWordTimeS { get; set; } = 16.0;

        // TimeSpan converters
        [JsonIgnore]
        public TimeSpan OSCChatboxRateLimit => TimeSpan.FromSeconds(OSCChatboxRateLimitS);

        [JsonIgnore]
        public TimeSpan WordTime => TimeSpan.FromSeconds(WordTimeS);

        [JsonIgnore]
        public TimeSpan HardWordTime => TimeSpan.FromSeconds(HardWordTimeS);
    }

    [JsonSerializable(typeof(STTConfig))]
    public partial class JsonContext : JsonSerializerContext { }
}
