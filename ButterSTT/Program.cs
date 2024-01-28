using ButterSTT;

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

using var speechToTextHandler = new SpeechToTextHandler(modelPath);
speechToTextHandler.StartRecording();

Console.ReadLine();
