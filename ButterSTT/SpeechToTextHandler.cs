using System.Buffers;
using System.Text;
using AprilAsr;
using NAudio.Wave;

namespace ButterSTT
{
    public class SpeechToTextHandler : IDisposable
    {
        // Audio
        private readonly WaveInEvent AudioIn;

        // Model
        private readonly AprilModel Model;
        public readonly string ModelPath;

        // Session
        private readonly AprilSession Session;

        // Output
        private readonly StringBuilder aprilOutput = new();

        public int WaveDeviceNumber { get; private set; } = 0;
        public bool MicrophoneRecording { get; private set; } = false;

        public SpeechToTextHandler(string modelPath, int deviceNumber = 0)
        {
            // Load model
            Model = new AprilModel(modelPath);
            ModelPath = modelPath;

            Console.WriteLine($"Model loaded from \"{modelPath}\":\n  > Name: {Model.Name}\n  > Description: {Model.Description}\n  > Language: {Model.Language}\n  > Sample Rate: {Model.SampleRate} Hz");

            // Initialize session
            Session = new AprilSession(Model, OnAprilTokens, async: true);

            // Initialize microphone
            AudioIn = new WaveInEvent()
            {
                DeviceNumber = deviceNumber,
                WaveFormat = new(Model.SampleRate, 16, 1)
            };
            AudioIn.DataAvailable += OnMicData;
            AudioIn.RecordingStopped += OnMicStop;
        }

        public void SwapMicrophoneDevice(int deviceNumber = 0)
        {
            var wasRecording = MicrophoneRecording;

            // Pause recording, then swap devices
            if (wasRecording) StopRecording();
            AudioIn.DeviceNumber = deviceNumber;
            if (wasRecording) StartRecording();
        }

        public void StartRecording()
        {
            AudioIn.StartRecording();
            MicrophoneRecording = true;
        }

        public void StopRecording()
        {
            AudioIn.StopRecording();
            MicrophoneRecording = false;
        }

        private void OnMicData(object? sender, WaveInEventArgs args)
        {
            if (args.BytesRecorded <= 0) return;

            // Convert the bytes to shorts
            var shorts = new short[args.BytesRecorded / 2];
            Buffer.BlockCopy(args.Buffer, 0, shorts, 0, args.BytesRecorded);
            Session.FeedPCM16(shorts, shorts.Length);
        }

        private void OnMicStop(object? sender, StoppedEventArgs args)
        {
            Session.Flush();
        }

        private void OnAprilTokens(AprilResultKind result, AprilToken[] tokens)
        {
            aprilOutput.Clear();

            switch (result)
            {
                case AprilResultKind.PartialRecognition:
                    aprilOutput.Append("- ");
                    break;
                case AprilResultKind.FinalRecognition:
                    aprilOutput.Append("@ ");
                    break;
                default:
                    aprilOutput.Append(' ');
                    break;
            }

            foreach (AprilToken token in tokens)
            {
                aprilOutput.Append(token.Token);
            }

            Console.WriteLine(aprilOutput.ToString().Trim());
        }

        public void Dispose()
        {
            StopRecording();
            AudioIn.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
