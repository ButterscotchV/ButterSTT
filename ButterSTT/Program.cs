using ButterSTT;

var modelPath = "C:\\Users\\Butterscotch\\Downloads\\april-english-dev-01110_en.april";

using var speechToTextHandler = new SpeechToTextHandler();
speechToTextHandler.LoadModel(modelPath);
speechToTextHandler.StartSession();
speechToTextHandler.ConnectMicrophone();
speechToTextHandler.StartRecording();

Console.ReadLine();
