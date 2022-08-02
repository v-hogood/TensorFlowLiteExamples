using Android.Content;
using Android.Media;
using Android.OS;
using Android.Util;
using Java.Lang;
using Java.Util.Concurrent;
using Xamarin.TensorFlow.Lite.Support.Audio;
using Xamarin.TensorFlow.Lite.Task.Audio.Classifier;
using Xamarin.TensorFlow.Lite.Task.Base;

namespace AudioClassification
{
    public class AudioClassificationHelper
    {
        private Context context;
        private IAudioClassificationListener listener;

        public string CurrentModel = YamnetModel;
        public float ClassificationThreshold = DisplayThreshold;
        public float Overlap = DefaultOverlapValue;
        public int NumOfResults = DefaultNumOfResults;
        public int CurrentDelegate = 0;
        public int NumThreads = 2;

        private AudioClassifier classifier;
        private TensorAudio tensorAudio;
        private AudioRecord recorder;
        private ScheduledThreadPoolExecutor executor;

        private Runnable classifyRunnable;

        public AudioClassificationHelper(Context context, IAudioClassificationListener listener)
        {
            this.context = context;
            this.listener = listener;

            classifyRunnable = new Runnable(() =>
            {
                ClassifyAudio();
            });

            InitClassifier();
        }

        public void InitClassifier()
        {
            // Set general detection options, e.g. number of used threads
            var baseOptionsBuilder = BaseOptions.InvokeBuilder();
            baseOptionsBuilder.SetNumThreads(NumThreads);

            // Use the specified hardware for running the model. Default to CPU.
            // Possible to also use a GPU delegate, but this requires that the classifier be created
            // on the same thread that is using the classifier, which is outside of the scope of this
            // sample's design.
            switch(CurrentDelegate)
            {
                case DelegateCpu:
                default:
                    // Default
                    break;
                case DelegateNnapi:
                    baseOptionsBuilder.UseNnapi();
                    break;
            }

            // Configures a set of parameters for the classifier and what results will be returned.
            var options = AudioClassifier.AudioClassifierOptions.InvokeBuilder()
                .SetScoreThreshold(ClassificationThreshold)
                .SetMaxResults(NumOfResults)
                .SetBaseOptions(baseOptionsBuilder.Build())
                .Build();

            try
            {
                // Create the classifier and required supporting objects
                classifier = AudioClassifier.CreateFromFileAndOptions(context, CurrentModel, options);
                tensorAudio = classifier.CreateInputTensorAudio();
                recorder = classifier.CreateAudioRecord();
                StartAudioClassification();
            }
            catch (IllegalStateException e)
            {
                listener.OnError(
                    "Audio Classifier failed to initialize. See error logs for details"
                );

                Log.Error("AudioClassification", "TFLite failed to load with error: " + e.Message);
            }
        }

        public void StartAudioClassification()
        {
            if (recorder.RecordingState == RecordState.Recording)
            {
                return;
            }

            recorder.StartRecording();
            executor = new ScheduledThreadPoolExecutor(1);

            // Each model will expect a specific audio recording length. This formula calculates that
            // length using the input buffer size and tensor format sample rate.
            // For example, YAMNET expects 0.975 second length recordings.
            // This needs to be in milliseconds to avoid the required Long value dropping decimals.
            var lengthInMilliSeconds = ((classifier.RequiredInputBufferSize * 1.0f) /
                    classifier.RequiredTensorAudioFormat.SampleRate) * 1000;

            var interval = (long) (lengthInMilliSeconds * (1 - Overlap));

            executor.ScheduleAtFixedRate(
                classifyRunnable,
                0,
                interval,
                TimeUnit.Milliseconds);
        }

        private void ClassifyAudio()
        {
            tensorAudio.Load(recorder);
            var inferenceTime = SystemClock.UptimeMillis();
            var output = classifier.Classify(tensorAudio);
            inferenceTime = SystemClock.UptimeMillis() - inferenceTime;
            listener.OnResult(output[0].Categories, inferenceTime);
        }

        public void StopAudioClassification()
        {
            recorder.Stop();
            executor.ShutdownNow();
        }

        const int DelegateCpu = 0;
        const int DelegateNnapi = 1;
        const float DisplayThreshold = 0.3f;
        const int DefaultNumOfResults = 2;
        const float DefaultOverlapValue = 0.5f;
        public const string YamnetModel = "yamnet.tflite";
        public const string SpeechCommandModel = "speech.tflite";
    }
}
