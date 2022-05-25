using System.Collections.Generic;
using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Java.Lang;
using Java.Nio;
using Xamarin.TensorFlow.Lite.Support.Common;
using Xamarin.TensorFlow.Lite.Support.Image;
using Xamarin.TensorFlow.Lite.Support.Image.Ops;
using Xamarin.TensorFlow.Lite.Support.Label;
using Xamarin.TensorFlow.Lite.Support.TensorBuffer;
using Xamarin.TensorFlow.Lite;
using Xamarin.TensorFlow.Lite.Nnapi;
using Xamarin.TensorFlow.Lite.GPU;

namespace ImageClassification
{
    // A classifier specialized to label images using TensorFlow Lite.
    public abstract class Classifier : IComparer<float>
    {
        public const string Tag = "ClassifierWithSupport";

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

        // The loaded TensorFlow Lite model.

        // Image size along the x axis.
        public int ImageSizeX { get; private set; }

        // Image size along the y axis.
        public int ImageSizeY { get; private set; }

        // Optional GPU delegate for accleration.
        private GpuDelegate gpuDelegate = null;

        // Optional NNAPI delegate for accleration.
        private NnApiDelegate nnApiDelegate = null;

        // An instance of the driver class to run model inference with Tensorflow Lite.
        protected Interpreter tflite;

        // Options for configuring the Interpreter.
        private Interpreter.Options tfliteOptions = new Interpreter.Options();

        // Labels corresponding to the output of the vision model.
        private IList<string> labels;

        // Input image TensorBuffer.
        private TensorImage inputImageBuffer;

        // Output probability TensorBuffer.
        private TensorBuffer outputProbabilityBuffer;

        // Processer to apply post processing of the output probability.
        private TensorProcessor probabilityProcessor;

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
        public class Recognition
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
            public RectF Location { get; }

            public Recognition(string id, string title, float confidence, RectF location)
            {
                Id = id;
                Title = title;
                Confidence = new Float(confidence);
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
            MappedByteBuffer tfliteModel = FileUtil.LoadMappedFile(activity, ModelPath);
            switch (device)
            {
                case Device.NNAPI:
                    nnApiDelegate = new NnApiDelegate();
                    tfliteOptions.AddDelegate(nnApiDelegate);
                    break;
                case Device.GPU:
                    CompatibilityList compatList = new CompatibilityList();
                    if (compatList.IsDelegateSupportedOnThisDevice)
                    {
                        // if the device has a supported GPU, add the GPU delegate
                        GpuDelegate.Options delegateOptions = compatList.BestOptionsForThisDevice;
                        GpuDelegate gpuDelegate = new GpuDelegate(delegateOptions);
                        tfliteOptions.AddDelegate(gpuDelegate);
                        Log.Debug(Tag, "GPU supported. GPU delegate created and added to options");
                    }
                    else
                    {
                        tfliteOptions.SetUseXNNPACK(true);
                        Log.Debug(Tag, "GPU not supported. Default to CPU.");
                    }
                    break;
                case Device.CPU:
                    tfliteOptions.SetUseXNNPACK(true);
                    Log.Debug(Tag, "CPU execution");
                    break;
            }
            tfliteOptions.SetNumThreads(numThreads);
            tflite = new Interpreter(tfliteModel, tfliteOptions);

            // Loads labels out from the label file.
            labels = FileUtil.LoadLabels(activity, LabelPath);

            // Reads type and shape of input and output tensors, respectively.
            int imageTensorIndex = 0;
            int[] imageShape = tflite.GetInputTensor(imageTensorIndex).Shape(); // {1, height, width, 3}
            ImageSizeY = imageShape[1];
            ImageSizeX = imageShape[2];
            Xamarin.TensorFlow.Lite.DataType imageDataType = tflite.GetInputTensor(imageTensorIndex).DataType();
            int probabilityTensorIndex = 0;
            int[] probabilityShape =
                tflite.GetOutputTensor(probabilityTensorIndex).Shape(); // {1, NUM_CLASSES}
            Xamarin.TensorFlow.Lite.DataType probabilityDataType = tflite.GetOutputTensor(probabilityTensorIndex).DataType();

            // Creates the input tensor.
            inputImageBuffer = new TensorImage(imageDataType);

            // Creates the output tensor and its processor.
            outputProbabilityBuffer = TensorBuffer.CreateFixedSize(probabilityShape, probabilityDataType);

            // Creates the post processor for the output probability.
            probabilityProcessor = new TensorProcessor.Builder().Add(PostprocessNormalizeOp).Build();

            Log.Debug(Tag, "Created a Tensorflow Lite Image Classifier.");
        }

        // Runs inference and returns the classification results.
        public List<Recognition> RecognizeImage(Bitmap bitmap, int sensorOrientation)
        {
            // Logs this method so that it can be analyzed with systrace.
            Trace.BeginSection("RecognizeImage");

            Trace.BeginSection("LoadImage");
            long startTimeForLoadImage = SystemClock.UptimeMillis();
            inputImageBuffer = LoadImage(bitmap, sensorOrientation);
            long endTimeForLoadImage = SystemClock.UptimeMillis();
            Trace.EndSection();
            Log.Verbose(Tag, "Timecost to load the image: " + (endTimeForLoadImage - startTimeForLoadImage));

            // Runs the inference call.
            Trace.BeginSection("RunInference");
            long startTimeForReference = SystemClock.UptimeMillis();
            tflite.Run(inputImageBuffer.Buffer, outputProbabilityBuffer.Buffer.Rewind());
            long endTimeForReference = SystemClock.UptimeMillis();
            Trace.EndSection();
            Log.Verbose(Tag, "Timecost to run model inference: " + (endTimeForReference - startTimeForReference));

            // Gets the map of label and probability.
            IDictionary<string, Float> labeledProbability =
                new TensorLabel(labels, (TensorBuffer)probabilityProcessor.Process(outputProbabilityBuffer))
                    .MapWithFloatValue;
            Trace.EndSection();

            // Gets top-k results.
            return GetTopKProbability(labeledProbability);
        }

        // Closes the interpreter and model to release resources.
        public void Close()
        {
                tflite?.Close();
                tflite = null;

                gpuDelegate?.Close();
                gpuDelegate = null;

                nnApiDelegate?.Close();
                nnApiDelegate = null;
        }

        // Loads input image, and applies preprocessing.
        private TensorImage LoadImage(Bitmap bitmap, int sensorOrientation)
        {
            // Loads bitmap into a TensorImage.
            inputImageBuffer.Load(bitmap);

            // Creates processor for the TensorImage.
            int cropSize = Math.Min(bitmap.Width, bitmap.Height);
            int numRotation = sensorOrientation / 90;
            // TODO(b/143564309): Fuse ops inside ImageProcessor.
            ImageProcessor imageProcessor =
                new ImageProcessor.Builder()
                    .Add(new ResizeWithCropOrPadOp(cropSize, cropSize))
                    // TODO(b/169379396): investigate the impact of the resize algorithm on accuracy.
                    // To get the same inference results as lib_task_api, which is built on top of the Task
                    // Library, use ResizeMethod.BILINEAR.
                .Add(new ResizeOp(ImageSizeX, ImageSizeY, ResizeOp.ResizeMethod.NearestNeighbor))
                .Add(new Rot90Op(numRotation))
                .Add(PreprocessNormalizeOp)
                .Build();
            return imageProcessor.Process(inputImageBuffer);
        }

        public int Compare(float x, float y)
        {
            // Intentionally reversed to put high confidence at the head of the queue.
            return y == x ? 1 : y.CompareTo(x);
        }

        // Gets the top-k results.
        private List<Recognition> GetTopKProbability(IDictionary<string, Float> labelProb)
        {
            // Find the best classifications.
            SortedList<float, string> sl =
                new SortedList<float, string>(
                    MaxResults,
                    this);

            foreach (KeyValuePair<string, Float> entry in labelProb)
            {
                sl.Add(entry.Value.FloatValue(), entry.Key);
            }
             
            List<Recognition> recognitions = new List<Recognition>();
            int recognitionsSize = Math.Min(sl.Count, MaxResults);
            for (int i = 0; i < recognitionsSize; ++i)
            {
                recognitions.Add(new Recognition("" + sl.Values[i], sl.Values[i], sl.Keys[i], null));
            }
            return recognitions;
        }

        // Gets the name of the model file stored in Assets.
        protected abstract string ModelPath { get; }

        // Gets the name of the label file stored in Assets.
        protected abstract string LabelPath { get; }

        // Gets the TensorOperator to nomalize the input image in preprocessing.
        protected abstract ITensorOperator PreprocessNormalizeOp { get; }

        //
        // Gets the TensorOperator to dequantize the output probability in post processing.
        //
        // <p>For quantized model, we need de-quantize the prediction with NormalizeOp (as they are all
        // essentially linear transformation). For float model, de-quantize is not required. But to
        // uniform the API, de-quantize is added to float model too. Mean and std are set to 0.0f and
        // 1.0f, respectively.
        //
        protected abstract ITensorOperator PostprocessNormalizeOp { get; }
    }
}
