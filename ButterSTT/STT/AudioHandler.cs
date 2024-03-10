using ButterSTT.Config;
using NAudio.Wave;

namespace ButterSTT.STT
{
    public class AudioHandler : IDisposable
    {
        // Audio
        private readonly WaveInEvent _audioIn;
        private bool _restartRecordingNextStop = false;

        public int WaveDeviceNumber { get; private set; } =
            STTConfig.Default.MicrophoneDeviceNumber;
        public bool IsMicrophoneRecording { get; private set; } = false;

        public event EventHandler? OnMicStart;
        public event EventHandler? OnMicStop;
        public event EventHandler<(short[] data, int length)>? OnMicData;

        public AudioHandler(int sampleRate = 16000, int deviceNumber = 0)
        {
            // Initialize microphone
            _audioIn = new WaveInEvent()
            {
                DeviceNumber = deviceNumber,
                WaveFormat = new(sampleRate, 16, 1)
            };
            WaveDeviceNumber = deviceNumber;

            // Register microphone events
            _audioIn.DataAvailable += OnWaveData;
            _audioIn.RecordingStopped += OnWaveStop;
        }

        public void StartRecording()
        {
            _audioIn.StartRecording();
            IsMicrophoneRecording = true;
            OnMicStart?.Invoke(this, EventArgs.Empty);
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

            var wasRecording = IsMicrophoneRecording;

            // Make sure the recording is stopped
            StopRecording();

            // Swap devices
            _audioIn.DeviceNumber = deviceNumber;
            WaveDeviceNumber = deviceNumber;

            // If it's already stopped, restart it immediately
            // Otherwise, start it again when it's done stopping
            if (wasRecording && !IsMicrophoneRecording)
            {
                StartRecording();
            }
            else
            {
                _restartRecordingNextStop = true;
            }
        }

        private void OnWaveData(object? sender, WaveInEventArgs args)
        {
            if (args.BytesRecorded <= 0)
                return;

            // Convert the bytes to shorts
            var shorts = new short[args.BytesRecorded / sizeof(short)];
            Buffer.BlockCopy(args.Buffer, 0, shorts, 0, args.BytesRecorded);

            OnMicData?.Invoke(this, (shorts, shorts.Length));
        }

        private void OnWaveStop(object? sender, StoppedEventArgs args)
        {
            IsMicrophoneRecording = false;

            if (_restartRecordingNextStop)
                StartRecording();
            else
                OnMicStop?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            StopRecording();
            _audioIn.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
