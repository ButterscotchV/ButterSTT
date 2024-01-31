using System.Net;
using System.Text.Json.Serialization;
using ButterSTT.MessageSystem;

namespace ButterSTT.Config
{
    public record STTConfig
    {
        public static readonly STTConfig Default = new();

        [JsonPropertyName("config_version")]
        public int ConfigVersion { get; set; } = 1;

        [JsonPropertyName("models_path")]
        public string ModelsPath { get; set; } = "Models";

        [JsonPropertyName("microphone_device_number")]
        public int MicrophoneDeviceNumber { get; set; } = 0;

        [JsonPropertyName("osc_address")]
        public string OSCAddress { get; set; } = "127.0.0.1:9000";

        [JsonPropertyName("osc_chatbox_ratelimit_s")]
        public double OSCChatboxRateLimitS { get; set; } = 1.3;

        [JsonPropertyName("message_length")]
        public int MessageLength { get; set; } = 144;

        [JsonPropertyName("dequeue_system")]
        public EnumConfig<DequeueSystems> DequeueSystem { get; set; } =
            new(DequeueSystems.Pagination);

        [JsonPropertyName("max_words_dequeued")]
        public int MaxWordsDequeued { get; set; } = 10;

        [JsonPropertyName("realtime_queue_padding")]
        public int RealtimeQueuePadding { get; set; } = 24;

        [JsonPropertyName("word_time_s")]
        public double WordTimeS { get; set; } = 5.0;

        [JsonPropertyName("hard_word_time_s")]
        public double HardWordTimeS { get; set; } = 16.0;

        // Converter utilities
        private static TimeSpan Seconds(double s) =>
            s < 0d ? TimeSpan.MaxValue : TimeSpan.FromSeconds(s);

        [JsonIgnore]
        public IPEndPoint OSCEndpoint => IPEndPoint.Parse(OSCAddress);

        [JsonIgnore]
        public TimeSpan OSCChatboxRateLimit => Seconds(OSCChatboxRateLimitS);

        [JsonIgnore]
        public TimeSpan WordTime => Seconds(WordTimeS);

        [JsonIgnore]
        public TimeSpan HardWordTime => Seconds(HardWordTimeS);
    }

    [JsonSerializable(typeof(STTConfig))]
    public partial class JsonContext : JsonSerializerContext { }
}
