using System.Text;
using AprilAsr;
using ButterSTT.MessageSystem;
using ButterSTT.TextProcessing;

namespace ButterSTT.STT
{
    public class AprilAsr : IDisposable
    {
        // Audio
        public readonly AudioHandler AudioHandler;

        // Model
        public readonly FileInfo ModelFile;
        private readonly AprilModel _model;

        // Session
        private readonly AprilSession _session;

        // Output
        private readonly StringBuilder _consoleOutput = new();
        private readonly StringBuilder _aprilOutput = new();
        private readonly MessageQueue _messageQueue;

        public AprilAsr(FileInfo modelFile, MessageQueue messageQueue, int deviceNumber = 0)
        {
            _messageQueue = messageQueue;

            // Load model
            ModelFile = modelFile;
            _model = new AprilModel(modelFile.FullName);

            Console.WriteLine(
                $"Model loaded from \"{modelFile.FullName}\":\n  > Name: {_model.Name}\n  > Description: {_model.Description}\n  > Language: {_model.Language}\n  > Sample Rate: {_model.SampleRate} Hz"
            );

            // Initialize session
            _session = new AprilSession(_model, OnAprilTokens, async: true);

            // Initialize microphone
            AudioHandler = new(_model.SampleRate, deviceNumber);
            AudioHandler.OnMicData += OnMicData;
            AudioHandler.OnMicStop += OnMicStop;
        }

        private void OnMicData(object? sender, (short[] data, int length) data)
        {
            if (data.length <= 0)
                return;

            _session.FeedPCM16(data.data, data.length);
        }

        private void OnMicStop(object? sender, EventArgs e)
        {
            _session.Flush();
        }

        private void OnAprilTokens(AprilResultKind result, AprilToken[] tokens)
        {
            _consoleOutput.Clear();
            _aprilOutput.Clear();

            switch (result)
            {
                case AprilResultKind.PartialRecognition:
                    _consoleOutput.Append("- ");
                    break;
                case AprilResultKind.FinalRecognition:
                    _consoleOutput.Append("@ ");
                    break;
                default:
                    _consoleOutput.Append(' ');
                    break;
            }

            foreach (var token in tokens)
            {
                _aprilOutput.Append(token.Token);
            }

            var aprilOutputString =
                tokens.Length > 0
                    ? EnglishCapitalization.Capitalize(_aprilOutput.ToString().Trim())
                    : "";

            if (result == AprilResultKind.FinalRecognition)
            {
                _messageQueue.CurParagraph = EnglishTextParser.ParseParagraph(
                    aprilOutputString,
                    wordRegex: EnglishTextParser.WordKeepUrl()
                );
                _messageQueue.FinishCurrentParagraph();
            }
            else
            {
                _messageQueue.CurParagraph = EnglishTextParser.ParseParagraph(aprilOutputString);
            }

            _consoleOutput.Append(aprilOutputString);
            Console.WriteLine(_consoleOutput);
        }

        public void Dispose()
        {
            AudioHandler.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
