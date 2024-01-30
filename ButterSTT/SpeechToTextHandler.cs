using System.Text;
using AprilAsr;
using ButterSTT.Config;
using ButterSTT.MessageSystem;
using ButterSTT.TextProcessing;
using NAudio.Wave;

namespace ButterSTT
{
    public class SpeechToTextHandler : IDisposable
    {
        // Audio
        private readonly WaveInEvent _audioIn;
        private bool _restartRecordingNextStop = false;

        // Model
        private readonly AprilModel _model;
        public readonly FileInfo ModelFile;

        // Session
        private readonly AprilSession _session;

        // Output
        private readonly StringBuilder _consoleOutput = new();
        private readonly StringBuilder _aprilOutput = new();
        private readonly MessageQueue _messageQueue;

        public int WaveDeviceNumber { get; private set; } =
            STTConfig.Default.MicrophoneDeviceNumber;
        public bool MicrophoneRecording { get; private set; } = false;

        public SpeechToTextHandler(
            FileInfo modelFile,
            MessageQueue messageQueue,
            int deviceNumber = 0
        )
        {
            _messageQueue = messageQueue;

            // Load model
            _model = new AprilModel(modelFile.FullName);
            ModelFile = modelFile;

            Console.WriteLine(
                $"Model loaded from \"{modelFile.FullName}\":\n  > Name: {_model.Name}\n  > Description: {_model.Description}\n  > Language: {_model.Language}\n  > Sample Rate: {_model.SampleRate} Hz"
            );

            // Initialize session
            _session = new AprilSession(_model, OnAprilTokens, async: true);

            // Initialize microphone
            _audioIn = new WaveInEvent()
            {
                DeviceNumber = deviceNumber,
                WaveFormat = new(_model.SampleRate, 16, 1)
            };
            WaveDeviceNumber = deviceNumber;

            // Register microphone events
            _audioIn.DataAvailable += OnMicData;
            _audioIn.RecordingStopped += OnMicStop;
        }

        public void StartRecording()
        {
            _audioIn.StartRecording();
            MicrophoneRecording = true;
        }

        public void StopRecording()
        {
            // Tell the recording not to restart
            _restartRecordingNextStop = false;

            // This keeps recording for a little bit longer, it will call the event when it's done
            _audioIn.StopRecording();
        }

        public void SwapMicrophoneDevice(int deviceNumber)
        {
            // If it's already using this device, ignore it and continue
            if (_audioIn.DeviceNumber == deviceNumber)
                return;

            var wasRecording = MicrophoneRecording;

            // Make sure the recording is stopped
            StopRecording();

            // Swap devices
            _audioIn.DeviceNumber = deviceNumber;
            WaveDeviceNumber = deviceNumber;

            // If it's already stopped, restart it immediately
            // Otherwise, start it again when it's done stopping
            if (wasRecording && !MicrophoneRecording)
            {
                StartRecording();
            }
            else
            {
                _restartRecordingNextStop = true;
            }
        }

        private void OnMicData(object? sender, WaveInEventArgs args)
        {
            if (args.BytesRecorded <= 0)
                return;

            // Convert the bytes to shorts
            var shorts = new short[args.BytesRecorded / sizeof(short)];
            Buffer.BlockCopy(args.Buffer, 0, shorts, 0, args.BytesRecorded);
            _session.FeedPCM16(shorts, shorts.Length);
        }

        private void OnMicStop(object? sender, StoppedEventArgs args)
        {
            _session.Flush();
            MicrophoneRecording = false;

            if (_restartRecordingNextStop)
                StartRecording();
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

            foreach (AprilToken token in tokens)
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
            StopRecording();
            _audioIn.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
