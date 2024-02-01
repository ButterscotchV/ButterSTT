using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace ButterSTT
{
    public static class AudioUtils
    {
        public static IEnumerable<(
            int index,
            WaveInCapabilities device,
            MMDevice? mmDevice
        )> EnumerateWaveInDevices()
        {
            using var enumerator = new MMDeviceEnumerator();
            for (var i = -1; i < WaveInEvent.DeviceCount; i++)
            {
                (int index, WaveInCapabilities device, MMDevice? mmDevice) device = (
                    i,
                    WaveInEvent.GetCapabilities(i),
                    null
                );

                if (i >= 0)
                {
                    var mmDevices = enumerator.EnumerateAudioEndPoints(
                        DataFlow.Capture,
                        DeviceState.Active
                    );

                    // Try the most likely index first, this makes it more likely to get the
                    // correct name if multiple are named only slightly differently
                    if (mmDevices.Count > i)
                    {
                        try
                        {
                            var likelyDevice = mmDevices[i];
                            if (likelyDevice.FriendlyName.StartsWith(device.device.ProductName))
                                device.mmDevice = likelyDevice;
                        }
                        catch { }
                    }

                    // If the device isn't in the same index, search all devices
                    if (device.mmDevice == null)
                    {
                        foreach (var mmDevice in mmDevices)
                        {
                            try
                            {
                                if (mmDevice.FriendlyName.StartsWith(device.device.ProductName))
                                {
                                    device.mmDevice = mmDevice;
                                    break;
                                }
                            }
                            catch { }
                        }
                    }
                }

                yield return device;
            }
        }
    }
}
