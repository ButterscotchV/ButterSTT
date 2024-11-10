namespace ButterSTT.STT
{
    public class SlidingAudioHandler : AudioHandler
    {
        private readonly short[] _buffer;

        public SlidingAudioHandler(int sampleRate = 16000, int deviceNumber = 0, int stepMs = 3000, int lengthMs = 10000, int keepMs = 200) : base(sampleRate, deviceNumber)
        {
            // Buffer sample count in ms
            _buffer = new short[(lengthMs * sampleRate) / 1000];
        }

        protected override void ProcessWaveData(short[] shorts)
        {
            SendWaveData(shorts);
        }
    }
}
