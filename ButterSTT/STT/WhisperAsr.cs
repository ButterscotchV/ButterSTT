using Whisper.net;

namespace ButterSTT.STT
{
    public class WhisperAsr : IDisposable
    {
        // Audio
        public readonly AudioHandler AudioHandler;

        // Model
        public readonly FileInfo ModelFile;
        private readonly WhisperProcessor _processor;

        public WhisperAsr(FileInfo modelFile, int deviceNumber = 0)
        {
            // Load model
            ModelFile = modelFile;
            using var whisperFactory = WhisperFactory.FromPath(modelFile.FullName);
            _processor = whisperFactory
                .CreateBuilder()
                .WithLanguage("auto")
                .WithSegmentEventHandler(OnSegmentEvent)
                .Build();

            Console.WriteLine($"Model loaded from \"{modelFile.FullName}\".");

            // Initialize microphone
            AudioHandler = new(16000, deviceNumber);
            AudioHandler.OnMicData += OnMicData;
        }

        private void OnMicData(object? sender, (short[] data, int length) data)
        {
            if (data.length <= 0)
                return;

            var floats = new float[data.length];
            for (var i = 0; i < data.length; i++)
                floats[i] = data.data[i] / 32768f;

            _processor.Process(floats);
        }

        private void OnSegmentEvent(SegmentData segment)
        {
            Console.WriteLine(segment.Text.Trim());
        }

        public void Dispose()
        {
            AudioHandler?.Dispose();
            _processor.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
