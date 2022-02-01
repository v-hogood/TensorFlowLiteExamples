using System.Collections.Generic;
using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Java.Lang;
using Java.Nio;
using Org.Tensorflow.Lite.Support.Common;
using Org.Tensorflow.Lite.Support.Image;
using Org.Tensorflow.Lite.Support.Label;
using Org.Tensorflow.Lite.Support.Metadata;
using Org.Tensorflow.Lite.Task.Core;
using Org.Tensorflow.Lite.Task.Core.Vision;
using Org.Tensorflow.Lite.Task.Vision.Classifier;
using static Org.Tensorflow.Lite.Task.Core.Vision.ImageProcessingOptions;
using static Org.Tensorflow.Lite.Task.Vision.Classifier.ImageClassifier;

namespace ImageClassification
{
    // A classifier specialized to label images using TensorFlow Lite.
    public abstract class Classifier : Object
    {
        public const string Tag = "ClassifierWithTaskApi";

        // The model type used for classification.
        public enum Model
        {
            Float_MobileNet,
            Quantized_MobileNet,
            Float_EfficientNet,
            Quantized_EfficientNet
        }

        // The runtime device type used for executing classification.
        public enum Device
        {
            CPU,
            NNAPI,
            GPU
        }

        // Number of results to show in the UI.
        private const int MaxResults = 3;

        // Image size along the x axis.
        public int ImageSizeX { get; private set; }

        // Image size along the y axis.
        public int ImageSizeY { get; private set; }

        // An instance of the driver class to run model inference with Tensorflow Lite.
        protected ImageClassifier imageClassifier;

        //
        // Creates a classifier with the provided configuration.
        //
        // @param activity The current Activity.
        // @param model The model to use for classification.
        // @param device The device to use for classification.
        // @param numThreads The number of threads to use for classification.
        // @return A classifier with the desired configuration.
        //
        public static Classifier Create(Activity activity, Model model, Device device, int numThreads)
        {
            return model switch
            {
                Model.Float_MobileNet => new ClassifierFloatMobileNet(activity, device, numThreads),
                Model.Quantized_MobileNet => new ClassifierQuantizedMobileNet(activity, device, numThreads),
                Model.Float_EfficientNet => new ClassifierFloatEfficientNet(activity, device, numThreads),
                Model.Quantized_EfficientNet => new ClassifierQuantizedEfficientNet(activity, device, numThreads),
                _ => throw new UnsupportedOperationException()
            };
        }

        // An immutable result returned by a Classifier describing what was recognized.
        public class Recognition : Object
        {
            //
            // A unique identifier for what has been recognized. Specific to the class, not the instance of
            // the object.
            //
            public string Id { get; }

            // Display name for the recognition.
            public string Title { get; }

            //
            // A sortable score for how good the recognition is relative to others. Higher should be better.
            //
            public Float Confidence { get; }

            // Optional location within the source image for the location of the recognized object.
            public RectF Location { get; set; }

            public Recognition(string id, string title, Float confidence, RectF location)
            {
                Id = id;
                Title = title;
                Confidence = confidence;
                Location = location;
            }

            public override string ToString()
            {
                string resultString = "";
                if (Id != null)
                {
                    resultString += "[" + Id + "] ";
                }
                if (Title != null)
                {
                    resultString += Title + " ";
                }
                if (Confidence != null)
                {
                    resultString += string.Format("{0:0.0} ", Confidence.FloatValue() * 100.0f);
                }
                if (Location != null)
                {
                    resultString += Location + " ";
                }
                return resultString.Trim();
            }
        }

        // Initializes a {@code Classifier}.
        protected Classifier(Activity activity, Device device, int numThreads)
        {
            BaseOptions.Builder baseOptionsBuilder = BaseOptions.InvokeBuilder();
            switch (device)
            {
                case Device.GPU:
                    baseOptionsBuilder.UseGpu();
                    break;
                case Device.NNAPI:
                    baseOptionsBuilder.UseNnapi();
                    break;
                default:
                    break;
            }

            // Create the ImageClassifier instance.
            ImageClassifierOptions options =
                ImageClassifierOptions.InvokeBuilder()
                    .SetBaseOptions(baseOptionsBuilder.SetNumThreads(numThreads).Build())
                    .SetMaxResults(MaxResults)
                    .Build();
            imageClassifier = ImageClassifier.CreateFromFileAndOptions(activity, GetModelPath(), options);
            Log.Debug(Tag, "Created a Tensorflow Lite Image Classifier.");

            // Get the input image size information of the underlying tflite model.
            MappedByteBuffer tfliteModel = FileUtil.LoadMappedFile(activity, GetModelPath());
            MetadataExtractor metadataExtractor = new MetadataExtractor(tfliteModel);
            // Image shape is in the format of {1, height, width, 3}.
            int[] imageShape = metadataExtractor.GetInputTensorShape(/*inputIndex=*/ 0);
            ImageSizeY = imageShape[1];
            ImageSizeX = imageShape[2];
        }

        // Runs inference and returns the classification results.
        public List<Recognition> RecognizeImage(Bitmap bitmap, int sensorOrientation)
        {
            // Logs this method so that it can be analyzed with systrace.
            Trace.BeginSection("RecognizeImage");

            TensorImage inputImage = TensorImage.FromBitmap(bitmap);
            int width = bitmap.Width;
            int height = bitmap.Height;
            int cropSize = Math.Min(width, height);
            // TODO(b/169379396): investigate the impact of the resize algorithm on accuracy.
            // Task Library resize the images using bilinear interpolation, which is slightly different from
            // the nearest neighbor sampling algorithm used in lib_support. See
            // https://github.com/tensorflow/examples/blob/0ef3d93e2af95d325c70ef3bcbbd6844d0631e07/lite/examples/image_classification/android/lib_support/src/main/java/org/tensorflow/lite/examples/classification/tflite/Classifier.java#L310.
            ImageProcessingOptions imageOptions =
                ImageProcessingOptions.InvokeBuilder()
                    .SetOrientation(getOrientation(sensorOrientation))
                    // Set the ROI to the center of the image.
                    .SetRoi(
                        new Rect(
                            /*left=*/ (width - cropSize) / 2,
                            /*top=*/ (height - cropSize) / 2,
                            /*right=*/ (width + cropSize) / 2,
                            /*bottom=*/ (height + cropSize) / 2))
                    .Build();

            // Runs the inference call.
            Trace.BeginSection("RunInference");
            long startTimeForReference = SystemClock.UptimeMillis();
            IList<Classifications> results = imageClassifier.Classify(inputImage, imageOptions);
            long endTimeForReference = SystemClock.UptimeMillis();
            Trace.EndSection();
            Log.Verbose(Tag, "Timecost to run model inference: " + (endTimeForReference - startTimeForReference));

            Trace.EndSection();

            // Gets top-k results.
            return GetRecognitions(results);
        }

        // Closes the interpreter and model to release resources.
        public void Close()
        {
            imageClassifier?.Close();
            imageClassifier = null;
        }

        //
        // Converts a list of {@link Classifications} objects into a list of {@link Recognition} objects
        // to match the interface of other inference method, such as using the <a
        // href="https://github.com/tensorflow/examples/tree/master/lite/examples/image_classification/android/lib_support">TFLite
        // Support Library.</a>.
        //
        private List<Recognition> GetRecognitions(IList<Classifications> classifications)
        {
            List<Recognition> recognitions = new List<Recognition>();
            // All the demo models are single head models. Get the first Classifications in the results.
            foreach (Category category in classifications[0].Categories)
            {
                recognitions.Add(
                    new Recognition(
                        "" + category.Label, category.Label, new Float(category.Score), null));
            }
            return recognitions;
        }

        // Convert the camera orientation in degree into {@link ImageProcessingOptions#Orientation}.
        private static Orientation getOrientation(int cameraOrientation)
        {
            return (cameraOrientation / 90) switch
            {
                3 => Orientation.BottomLeft,
                2 => Orientation.BottomRight,
                1 => Orientation.TopRight, 
                _ => Orientation.TopLeft
            };
        }

        // Gets the name of the model file stored in Assets.
        protected abstract string GetModelPath();
    }
}
