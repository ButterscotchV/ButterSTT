using ButterSTT;
using ButterSTT.MessageSystem;

// Setup model dir
var modelsDir = Path.GetFullPath("Models");
Directory.CreateDirectory(modelsDir);

// Find the first model available
var modelPath =
    Directory
        .GetFiles(modelsDir)
        .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name) && name.EndsWith(".april"), null)
    ?? throw new FileNotFoundException(
        $"Could not find any available AprilAsr models (*.april) in \"{modelsDir}\"."
    );

var oscHandler = new OSCMessageHandler();
oscHandler.StartMessageLoop();

using var speechToTextHandler = new SpeechToTextHandler(modelPath, oscHandler.MessageQueue);
speechToTextHandler.StartRecording();

Console.ReadLine();
