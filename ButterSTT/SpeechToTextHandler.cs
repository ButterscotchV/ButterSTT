using System.Text;
using AprilAsr;
using ButterSTT.MessageSystem;
using ButterSTT.TextProcessing;
using CoreOSC;
using NAudio.Wave;

namespace ButterSTT
{
    public class SpeechToTextHandler : IDisposable
    {
        // Audio
        private readonly WaveInEvent AudioIn;
        private bool RestartRecordingNextStop = false;

        // Model
        private readonly AprilModel Model;
        public readonly string ModelPath;

        // Session
        private readonly AprilSession Session;

        // Output
        private readonly StringBuilder consoleOutput = new();
        private readonly StringBuilder aprilOutput = new();
        private readonly MessageQueue messageQueue = new();
        private readonly OSCHandler oscHandler = new();

        private DateTime lastMessage = DateTime.Now;

        public int WaveDeviceNumber { get; private set; } = 0;
        public bool MicrophoneRecording { get; private set; } = false;

        public SpeechToTextHandler(string modelPath, int deviceNumber = 0)
        {
            // Load model
            Model = new AprilModel(modelPath);
            ModelPath = modelPath;

            Console.WriteLine(
                $"Model loaded from \"{modelPath}\":\n  > Name: {Model.Name}\n  > Description: {Model.Description}\n  > Language: {Model.Language}\n  > Sample Rate: {Model.SampleRate} Hz"
            );

            // Initialize session
            Session = new AprilSession(Model, OnAprilTokens, async: true);

            // Initialize microphone
            AudioIn = new WaveInEvent()
            {
                DeviceNumber = deviceNumber,
                WaveFormat = new(Model.SampleRate, 16, 1)
            };
            WaveDeviceNumber = deviceNumber;

            // Register microphone events
            AudioIn.DataAvailable += OnMicData;
            AudioIn.RecordingStopped += OnMicStop;
        }

        public void StartRecording()
        {
            AudioIn.StartRecording();
            MicrophoneRecording = true;
        }

        public void StopRecording()
        {
            // Tell the recording not to restart
            RestartRecordingNextStop = false;

            // This keeps recording for a little bit longer, it will call the event when it's done
            AudioIn.StopRecording();
        }

        public void SwapMicrophoneDevice(int deviceNumber)
        {
            // If it's already using this device, ignore it and continue
            if (AudioIn.DeviceNumber == deviceNumber)
                return;

            var wasRecording = MicrophoneRecording;

            // Make sure the recording is stopped
            StopRecording();

            // Swap devices
            AudioIn.DeviceNumber = deviceNumber;
            WaveDeviceNumber = deviceNumber;

            // If it's already stopped, restart it immediately
            // Otherwise, start it again when it's done stopping
            if (wasRecording && !MicrophoneRecording)
            {
                StartRecording();
            }
            else
            {
                RestartRecordingNextStop = true;
            }
        }

        private void OnMicData(object? sender, WaveInEventArgs args)
        {
            if (args.BytesRecorded <= 0)
                return;

            // Convert the bytes to shorts
            var shorts = new short[args.BytesRecorded / sizeof(short)];
            Buffer.BlockCopy(args.Buffer, 0, shorts, 0, args.BytesRecorded);
            Session.FeedPCM16(shorts, shorts.Length);
        }

        private void OnMicStop(object? sender, StoppedEventArgs args)
        {
            Session.Flush();
            MicrophoneRecording = false;

            if (RestartRecordingNextStop)
                StartRecording();
        }

        private void OnAprilTokens(AprilResultKind result, AprilToken[] tokens)
        {
            consoleOutput.Clear();
            aprilOutput.Clear();

            switch (result)
            {
                case AprilResultKind.PartialRecognition:
                    consoleOutput.Append("- ");
                    break;
                case AprilResultKind.FinalRecognition:
                    consoleOutput.Append("@ ");
                    break;
                default:
                    consoleOutput.Append(' ');
                    break;
            }

            foreach (AprilToken token in tokens)
            {
                aprilOutput.Append(token.Token);
            }

            var aprilOutputString =
                tokens.Length > 0
                    ? EnglishCapitalization.Capitalize(aprilOutput.ToString().Trim())
                    : "";

            messageQueue.CurParagraph = EnglishTextParser.ParseParagraph(aprilOutputString);
            if (result == AprilResultKind.FinalRecognition)
                messageQueue.FinishCurrentParagraph();

            try
            {
                if (tokens.Length > 0 && !string.IsNullOrWhiteSpace(aprilOutputString))
                {
                    // Only print if at the end of a word or sentence, we don't want to print incomplete words
                    var lastToken = tokens.Last();
                    if (
                        (lastToken.WordBoundary || lastToken.SentenceEnd)
                        && (DateTime.Now - lastMessage).TotalSeconds > 1.3d
                    )
                    {
                        lastMessage = DateTime.Now;
                        if (result != AprilResultKind.FinalRecognition)
                        {
                            // Still typing the message... Show as typing!
                            oscHandler.OSCSender.Send(
                                new OscBundle(
                                    0,
                                    OSCHandler.MakeChatboxInput(aprilOutputString),
                                    OSCHandler.MakeChatboxTyping(true)
                                )
                            );
                        }
                        else
                        {
                            // Just send the message, no more typing
                            oscHandler.OSCSender.Send(
                                OSCHandler.MakeChatboxInput(aprilOutputString)
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            consoleOutput.Append(aprilOutputString);
            Console.WriteLine(consoleOutput);
        }

        public void Dispose()
        {
            StopRecording();
            AudioIn.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
