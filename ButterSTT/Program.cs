using ButterSTT;
using ButterSTT.Config;
using ButterSTT.MessageSystem;

try
{
    // Load config
    JsonConfigHandler<STTConfig> configHandler =
        new("config.json", JsonConfigHandler<STTConfig>.Context.STTConfig);
    STTConfig config = configHandler.InitializeConfig(STTConfig.Default);

    // Fill in new values if the version changed
    if (config.ConfigVersion < STTConfig.Default.ConfigVersion)
    {
        var backupFile = $"{configHandler.ConfigFilePath}.old";
        configHandler.MakeBackup(backupFile);
        Console.WriteLine(
            $"Backed up the current config file to \"{backupFile}\", upgrading from v{config.ConfigVersion} to v{STTConfig.Default.ConfigVersion}..."
        );
        config.ConfigVersion = STTConfig.Default.ConfigVersion;
        configHandler.WriteConfig(config);
    }

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
        DequeueSystem = config.DequeueSystem.EnumValue,
        MaxWordsDequeued = config.MaxWordsDequeued < 0 ? int.MaxValue : config.MaxWordsDequeued,
        RealtimeQueuePadding = config.RealtimeQueuePadding,
        WordTime = config.WordTime,
        HardWordTime = config.HardWordTime,
        PageContext = config.PageContext,
    };
    using var oscHandler = new OSCMessageHandler(messageQueue, config.OSCEndpoint)
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
}
catch (Exception e)
{
    Console.Error.WriteLine(e);
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
}
