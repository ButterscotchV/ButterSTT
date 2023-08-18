using System.Buffers;
using System.Text;
using AprilAsr;
using NAudio.Wave;

namespace ButterSTT
{
    public class SpeechToTextHandler : IDisposable
    {
        private WaveInEvent? AudioIn;
        private AprilModel? Model;
        private AprilSession? Session;

        public bool MicrophoneRecording { get; private set; } = false;
        public int WaveDeviceNumber => AudioIn?.DeviceNumber ?? 0;
        public string ModelPath { get; private set; } = "";

        public void ConnectMicrophone(int deviceNumber = 0)
        {
            InternalConnectMicrophone(deviceNumber, Model?.SampleRate ?? 16000, 16);
        }

        private void InternalConnectMicrophone(int deviceNumber, int sampleRate, int bits)
        {
            DisconnectMicrophone();

            var audioIn = new WaveInEvent()
            {
                DeviceNumber = deviceNumber,
                WaveFormat = new(sampleRate, bits, 1)
            };

            audioIn.DataAvailable += OnMicData;
            audioIn.RecordingStopped += OnMicStop;

            AudioIn = audioIn;
        }

        public void DisconnectMicrophone()
        {
            var audioIn = AudioIn;
            if (audioIn != null)
            {
                InternalStopRecording(audioIn);
                audioIn.Dispose();
            }

            AudioIn = null;
        }

        public void StartRecording()
        {
            var audioIn = AudioIn;
            if (audioIn != null)
            {
                audioIn.StartRecording();
                MicrophoneRecording = true;
            }
        }

        public void StopRecording()
        {
            var audioIn = AudioIn;
            if (audioIn != null) InternalStopRecording(audioIn);
        }

        private void InternalStopRecording(WaveInEvent audioIn)
        {
            audioIn.StopRecording();
            MicrophoneRecording = false;
        }

        public void LoadModel(string modelPath)
        {
            var model = new AprilModel(modelPath);

            Model = model;
            ModelPath = modelPath;

            Console.WriteLine($"Model loaded from \"{modelPath}\":\n  > Name: {model.Name}\n  > Description: {model.Description}\n  > Language: {model.Language}\n  > Sample Rate: {model.SampleRate} Hz");
        }

        public void StartSession()
        {
            var model = Model ?? throw new Exception("Unable to start a session without a model! Please load a model first.");

            // Handle audio in not matching the model wavelength automatically
            var audioIn = AudioIn;
            if (audioIn != null && model.SampleRate != audioIn.WaveFormat.SampleRate)
            {
                // Read the audio in settings
                var deviceNum = audioIn.DeviceNumber;
                var bitCount = audioIn.WaveFormat.BitsPerSample;
                var wasRecording = MicrophoneRecording;

                // Reconnect audio in using the same settings but with the sample rate modified
                InternalConnectMicrophone(deviceNum, model.SampleRate, bitCount);
                if (wasRecording) StartRecording();
            }

            Session = new AprilSession(model, OnAprilTokens, async: true);
        }

        private void OnMicData(object? sender, WaveInEventArgs args)
        {
            AprilSession? session = Session;
            if (args.BytesRecorded <= 0 || session == null) return;

            // Convert the bytes to shorts
            var shorts = new short[args.BytesRecorded / 2];
            Buffer.BlockCopy(args.Buffer, 0, shorts, 0, args.BytesRecorded);
            session.FeedPCM16(shorts, shorts.Length);
        }

        private void OnMicStop(object? sender, StoppedEventArgs args)
        {
            Session?.Flush();
        }

        private void OnAprilTokens(AprilResultKind result, AprilToken[] tokens)
        {
            var stringBuilder = new StringBuilder();

            switch (result)
            {
                case AprilResultKind.PartialRecognition:
                    stringBuilder.Append("- ");
                    break;
                case AprilResultKind.FinalRecognition:
                    stringBuilder.Append("@ ");
                    break;
                default:
                    stringBuilder.Append(' ');
                    break;
            }

            foreach (AprilToken token in tokens)
            {
                stringBuilder.Append(token.Token);
            }

            Console.WriteLine(stringBuilder.ToString().Trim());
        }

        public void Dispose()
        {
            DisconnectMicrophone();
            GC.SuppressFinalize(this);
        }
    }
}
