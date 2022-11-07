using System;
using CoreFoundation;
using Foundation;
using TensorFlowLiteTaskAudio;
using static AudioClassification.HomeViewController;

namespace AudioClassification
{
    // Delegate to return the classification results.
    public interface AudioClassificationHelperDelegate
    {
        void AudioClassificationHelper(AudioClassificationHelper helper, Result result);
        void AudioClassificationHelper(AudioClassificationHelper helper, NSError error);
    }

    // Stores results for a particular audio snipprt that was successfully classified.
    public struct Result
    {
        public double InferenceTime;
        public TFLCategory[] Categories;
    }

    // This class handles all data preprocessing and makes calls to run inference on an audio snippet
    // by invoking the Task Library's `AudioClassifier`.
    public class AudioClassificationHelper
    {
        private NSString errorDomain = new NSString("org.tensorflow.lite.examples");

        // MARK: Public properties

        public AudioClassificationHelperDelegate Delegate;

        // MARK: Private properties

        // An `AudioClassifier` object for performing audio classification using a given model.
        private TFLAudioClassifier classifier;

        /// An object to continuously record audio using the device's microphone.
        private TFLAudioRecord audioRecord;

        // A tensor to store the input audio for the model.
        private TFLAudioTensor inputAudioTensor;

        // A timer to schedule classification routine to run periodically.
        private NSTimer timer;

        /// A queue to offload the classification routine to a background thread.
        private DispatchQueue processQueue = new DispatchQueue(label: "processQueue");

        // MARK: - Initialization

        // A failable initializer for `AudioClassificationHelper`.
        //
        // A new instance is created if the model is successfully loaded from the app's main bundle.
        public AudioClassificationHelper(ModelType modelType, int threadCount, float scoreThreshold, int maxResults)
        {
            var modelFilename = modelType.FileName;

            // Construct the path to the model file.
            var modelPath = NSBundle.MainBundle.PathForResource(
                name: modelFilename,
                ofType: "tflite"
            );
            if (modelPath == null)
            {
                throw new System.Exception("Failed to load the model file " + modelFilename + ".tflite.");
            }

            // Specify the options for the classifier.
            var classifierOptions = new TFLAudioClassifierOptions(modelPath: modelPath);
            classifierOptions.BaseOptions.ComputeSettings.CpuSettings.NumThreads = threadCount;
            classifierOptions.ClassificationOptions.MaxResults = maxResults;
            classifierOptions.ClassificationOptions.ScoreThreshold = scoreThreshold;

            // Create the classifier.
            NSError error;
            classifier = TFLAudioClassifier.AudioClassifierWithOptions(options: classifierOptions, out error);
            if (error != null)
                throw new Exception("Failed to create the classifier with error: " + error.LocalizedDescription);

            // Create an `AudioRecord` instance to record input audio that satisfies
            // the model's requirements.
            audioRecord = classifier.CreateAudioRecordWithError(out error);
            if (error != null)
                throw new Exception("Failed to create the classifier with error: " + error.LocalizedDescription);
            inputAudioTensor = classifier.CreateInputAudioTensor();
        }

        public void StopClassifier()
        {
            audioRecord.Stop();
            timer?.Invalidate();
            timer = null;
        }

        // Start the audio classification routine in the background.
        //
        ///Classification results are periodically returned to the delegate.
        // - Parameters overlap: Overlapping factor between consecutive audio snippet to be classified.
        //   Value must be >= 0 and < 1.
        public void StartClassifier(double overlap)
        {
            if (overlap < 0)
            {
                var err = new NSError(
                    domain: errorDomain,
                    code: 0,
                    userInfo: NSDictionary.FromObjectAndKey(new NSString("overlap must be equal or larger than 0."), NSError.LocalizedDescriptionKey));
                Delegate?.AudioClassificationHelper(this, error: err);
            }

            if (overlap >= 1)
            {
                var err = new NSError(
                    domain: errorDomain, code: 0,
                    userInfo: NSDictionary.FromObjectAndKey(new NSString("overlap must be smaller than 1."), NSError.LocalizedDescriptionKey));
                Delegate?.AudioClassificationHelper(this, error: err);
            }

            // Start recording audio.
            NSError error;
            if (!audioRecord.StartRecordingWithError(out error))
                Delegate?.AudioClassificationHelper(this, error: error);

            // Calculate interval between sampling based on overlap.
            var audioFormat = inputAudioTensor.AudioFormat;
            var lengthInMilliSeconds =
                (double)inputAudioTensor.BufferSize / (double)audioFormat.SampleRate;
            var interval = lengthInMilliSeconds * (double)(1 - overlap);
            timer?.Invalidate();

            // Schedule the classification routine to run every fixed interval.
            timer = NSTimer.CreateScheduledTimer(interval: interval, repeats: true, block: ((_) =>
            {
                this.processQueue.DispatchAsync(() =>
                    this.RunClassification());
            }));
        }

        // Run the classification routine with the latest audio stored in the `AudioRecord` instance's
        // buffer.
        private void RunClassification()
        {
            var startTime = new NSDate().SecondsSince1970;

            // Grab the latest audio chunk in the audio record and run classification.
            NSError error;
            if (!inputAudioTensor.LoadAudioRecord(audioRecord: audioRecord, out error))
            {
                if (error.Code == (int)TFLAudioRecordErrorCode.WaitingForNewMicInputError)
                    return;
                Delegate?.AudioClassificationHelper(this, error: error);
            }
            var results = classifier.ClassifyWithAudioTensor(audioTensor: inputAudioTensor, out error);
            if (error != null)
                Delegate?.AudioClassificationHelper(this, error: error);
            var inferenceTime = new NSDate().SecondsSince1970 - startTime;

            // Return the classification result to the delegate.
            DispatchQueue.MainQueue.DispatchAsync(() =>
            {
                var result = new Result
                {
                    InferenceTime = inferenceTime,
                    Categories = results.Classifications[0].Categories
                };
                // Send classification result to the delegate.
                this.Delegate?.AudioClassificationHelper(this, result: result);
            });
        }
    }
}
