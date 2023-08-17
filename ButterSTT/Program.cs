using AprilAsr;
using NAudio.Wave;

var modelPath = "C:\\Users\\Butterscotch\\Downloads\\april-english-dev-01110_en.april";

// Load the model and print metadata
var model = new AprilModel(modelPath);
Console.WriteLine("Name: " + model.Name);
Console.WriteLine("Description: " + model.Description);
Console.WriteLine("Language: " + model.Language);

var session = new AprilSession(model, (result, tokens) =>
{
    string s = "";
    if (result == AprilResultKind.PartialRecognition)
    {
        s = "- ";
    }
    else if (result == AprilResultKind.FinalRecognition)
    {
        s = "@ ";
    }
    else
    {
        s = " ";
    }

    foreach (AprilToken token in tokens)
    {
        s += token.Token;
    }

    Console.WriteLine(s);
}, async: true);

using var recorder = new WaveInEvent
{
    WaveFormat = new WaveFormat(16000, 16, 1)
};

recorder.DataAvailable += (sender, args) =>
{
    if (args.BytesRecorded <= 0) return;
    var shorts = new short[args.BytesRecorded / 2];
    Buffer.BlockCopy(args.Buffer, 0, shorts, 0, args.BytesRecorded);
    session.FeedPCM16(shorts, shorts.Length);
};
recorder.RecordingStopped += (sender, args) =>
{
    session.Flush();
};

recorder.StartRecording();
Console.ReadLine();
