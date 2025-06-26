using System.Diagnostics;
using CoreVideo;
using Foundation;
using TensorFlowLiteTaskVision;

namespace ImageClassification
{
    // A result from the `Classifications`.
    public struct ImageClassificationResult
    {
        public double inferenceTime;
        public TFLClassifications classifications;
    }

    // Information about a model file or labels file.
    public struct FileInfo
    {
        public string name;
        public string extension;
    }

    // This class handles all data preprocessing and makes calls to run inference on a given frame
    // by invoking the TFLite `ImageClassifier`. It then returns the top N results for a successful 
    // inference.
    public class ImageClassificationHelper
    {
        // MARK: - Model Parameters

        // TensorFlow Lite `Interpreter` object for performing inference on a given model.
        private TFLImageClassifier classifier;

        // MARK: - Initialization

        // A failable initializer for `ClassificationHelper`. A new instance is created if the model and
        // labels files are successfully loaded from the app's main bundle. Default `threadCount` is 1.
        public ImageClassificationHelper(FileInfo modelFileInfo, int threadCount, int resultCount, float scoreThreshold)
        {
            var modelFilename = modelFileInfo.name;
            // Construct the path to the model file.
            var modelPath = NSBundle.MainBundle.PathForResource(
                name: modelFilename,
                ofType: modelFileInfo.extension
            );
            if (modelPath == null)
            {
                throw new Exception("Failed to load the model file with name: " + modelFilename);
            }

            // Configures the initialization options.
            var options = new TFLImageClassifierOptions(modelPath: modelPath);
            options.BaseOptions.ComputeSettings.CpuSettings.NumThreads = threadCount;
            options.ClassificationOptions.MaxResults = resultCount;
            options.ClassificationOptions.ScoreThreshold = scoreThreshold;

            NSError error;
            classifier = TFLImageClassifier.ImageClassifierWithOptions(options: options,
                error: out error);
            if (error != null)
            {
                throw new Exception("Failed to create the interpreter with error: " + error.LocalizedDescription);
            }
        }

        // MARK: - Internal Methods

        // Performs image preprocessing, invokes the `ImageClassifier`, and processes the inference
        // results.
        public ImageClassificationResult? Classify(CVPixelBuffer pixelBuffer)
        {
            // Convert the `CVPixelBuffer` object to an `MLImage` object.
            var mlImage = new GMLImage(pixelBuffer: pixelBuffer);
            if (mlImage == null) return null;

            // Run inference using the `ImageClassifier{ object.
            var startDate = new NSDate();
            NSError error;
            var classificationResults = classifier.ClassifyWithGMLImage(image: mlImage,
                error: out error);
            var inferenceTime = new NSDate().GetSecondsSince(startDate) * 1000;
            if (error != null)
            {
                Debug.Print("Failed to invoke the interpreter with error: " + error.LocalizedDescription);
                return null;
            }

            // As all models used in this sample app are single-head models, gets the classification
            // result from the first (and only) classification head and return to the view controller to
            // display.
            var classifications = classificationResults.Classifications.First();
            if (classifications == null) return null;
            return new ImageClassificationResult
                { inferenceTime = inferenceTime, classifications = classifications };
        }
    }
}
