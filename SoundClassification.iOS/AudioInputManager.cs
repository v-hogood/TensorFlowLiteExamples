using System.Diagnostics;
using System.Runtime.InteropServices;
using AVFoundation;
using CoreFoundation;

namespace SoundClassification
{
    public interface IAudioInputManagerDelegate
    {
        void AudioInputManagerDidFailToAchievePermission(AudioInputManager audioInputManager);
        void AudioInputManager(AudioInputManager audioInputManager, short[] didCaptureChannelData);
    }

    public class AudioInputManager
    {
        // MARK: - Constants
        public int bufferSize;

        private int sampleRate;
        private DispatchQueue conversionQueue = new DispatchQueue(label: "conversionQueue");

        // MARK: - Variables
        public IAudioInputManagerDelegate Delegate;

        private AVAudioEngine audioEngine = new AVAudioEngine();

        // MARK: - Methods

        public AudioInputManager(int sampleRate)
        {
            this.sampleRate = sampleRate;
            this.bufferSize = sampleRate * 2;
        }

        public void CheckPermissionsAndStartTappingMicrophone()
        {
            switch(AVAudioSession.SharedInstance().RecordPermission)
            {
                case AVAudioSessionRecordPermission.Granted:
                    StartTappingMicrophone();
                    break;
                case AVAudioSessionRecordPermission.Denied:
                    Delegate?.AudioInputManagerDidFailToAchievePermission(this);
                    break;
                case AVAudioSessionRecordPermission.Undetermined:
                    RequestPermissions();
                    break;
                default:
                    throw new Exception();
            }
        }

        public void RequestPermissions()
        {
            AVAudioSession.SharedInstance().RequestRecordPermission((granted) =>
            {
                if (granted)
                {
                    this.StartTappingMicrophone();
                }
                else
                {
                    this.CheckPermissionsAndStartTappingMicrophone();
                }
            });
        }

        /// Starts tapping the microphone input and converts it into the format for which the model is trained and
        /// periodically returns it in the block
        public void StartTappingMicrophone()
        {
            var inputNode = audioEngine.InputNode;
            var inputFormat = inputNode.GetBusOutputFormat(bus: 0);
            var recordingFormat = new AVAudioFormat(
                format: AVAudioCommonFormat.PCMInt16,
                sampleRate: (double)sampleRate,
                channels: 1,
                interleaved: true
            );
            if (recordingFormat == null) return;
            var formatConverter = new AVAudioConverter(fromFormat: inputFormat, toFormat: recordingFormat);

            // installs a tap on the audio engine and specifying the buffer size and the input format.
            inputNode.InstallTapOnBus(bus: 0, bufferSize: (uint)bufferSize, format: inputFormat, ((buffer, _) =>
            {
                this.conversionQueue.DispatchAsync(() =>
                {
                    // An AVAudioConverter is used to convert the microphone input to the format required
                    // for the model.(pcm 16)
                    var pcmBuffer = new AVAudioPcmBuffer(
                        format: recordingFormat,
                        frameCapacity: (uint)(recordingFormat.SampleRate * 2.0));
                    if (pcmBuffer == null) return;

                    NSError error;
                    var inputBlock = new AVAudioConverterInputHandler((uint inNumberOfPackets, out AVAudioConverterInputStatus outStatus) =>
                    {
                        outStatus = AVAudioConverterInputStatus.HaveData;
                        return buffer;
                    });

                    formatConverter.ConvertToBuffer(outputBuffer: pcmBuffer, outError: out error, inputHandler: inputBlock);

                    if (error != null)
                    {
                        Debug.Print(error.LocalizedDescription);
                        return;
                    }
                    var channelData = pcmBuffer.Int16ChannelData;
                    unsafe
                    {
                        var channelDataValue = (IntPtr*)channelData.ToPointer();
                        var channelDataValueArray = new short[pcmBuffer.FrameLength];
                        Marshal.Copy(channelDataValue[0], channelDataValueArray, 0, (int)pcmBuffer.FrameLength);

                        // Converted pcm 16 values are delegated to the controller.
                        this.Delegate?.AudioInputManager(this, didCaptureChannelData: channelDataValueArray);
                    }
                });
            }));

            audioEngine.Prepare();

            NSError outError;
            if (!audioEngine.StartAndReturnError(outError: out outError))
            {
                Debug.Print(outError.LocalizedDescription);
            }
        }
    }
}
