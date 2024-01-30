using ButterSTT;
using ButterSTT.Config;
using ButterSTT.MessageSystem;

// Load config
JsonConfigHandler<STTConfig> configHandler =
    new("config.json", JsonConfigHandler<STTConfig>.Context.STTConfig);
STTConfig config = configHandler.InitializeConfig(STTConfig.Default);

// Setup model dir
var modelsDir = new DirectoryInfo(config.ModelsPath);
modelsDir.Create();

// Find the first model available
var modelFile =
    modelsDir
        .GetFiles()
        .FirstOrDefault(
            file =>
                file != null
                && file.Extension.Equals(".april", StringComparison.CurrentCultureIgnoreCase),
            null
        )
    ?? throw new FileNotFoundException(
        $"Could not find any available AprilAsr models (*.april) in \"{modelsDir}\"."
    );

var messageQueue = new MessageQueue()
{
    MessageLength = config.MessageLength,
    MaxWordsDequeued = config.MaxWordsDequeued,
    RealtimeQueuePadding = config.RealtimeQueuePadding,
    WordTime = config.WordTime,
    HardWordTime = config.HardWordTime,
};
var oscHandler = new OSCMessageHandler(
    messageQueue,
    oscAddress: config.OSCAddress,
    oscPort: config.OSCPort
)
{
    RateLimit = config.OSCChatboxRateLimit
};
oscHandler.StartMessageLoop();

using var speechToTextHandler = new SpeechToTextHandler(
    modelFile,
    oscHandler.MessageQueue,
    deviceNumber: config.MicrophoneDeviceNumber
);
speechToTextHandler.StartRecording();

Console.ReadLine();
