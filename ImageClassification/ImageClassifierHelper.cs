using System.Collections.Generic;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Views;
using Java.Lang;
using Xamarin.TensorFlow.Lite.GPU;
using Xamarin.TensorFlow.Lite.Support.Image;
using Xamarin.TensorFlow.Lite.Task.Base;
using Xamarin.TensorFlow.Lite.Task.Base.Vision;
using Xamarin.TensorFlow.Lite.Task.Vision.Classifier;

namespace ImageClassification
{
    public class ImageClassifierHelper
    {
        public float Threshold = 0.5f;
        public int NumThreads = 2;
        public int MaxResults = 3;
        public int CurrentDelegate = 0;
        public int CurrentModel = 0;

        Context context;
        IClassifierListener imageClassifierListener;
        ImageClassifier imageClassifier;

        public ImageClassifierHelper(Context context, IClassifierListener imageClassifierListener)
        {
            this.context = context;
            this.imageClassifierListener = imageClassifierListener;

            SetupImageClassifier();
        }

        public void ClearImageClassifier()
        {
            imageClassifier = null;
        }

        private void SetupImageClassifier()
        {
            var optionsBuilder = ImageClassifier.ImageClassifierOptions.InvokeBuilder()
                .SetScoreThreshold(Threshold)
                .SetMaxResults(MaxResults);

            var baseOptionsBuilder = BaseOptions.InvokeBuilder().SetNumThreads(NumThreads);

            switch(CurrentDelegate)
            {
                case DelegateCpu:
                    // Default
                    break;
                case DelegateGpu:
                    if (new CompatibilityList().IsDelegateSupportedOnThisDevice)
                    {
                        baseOptionsBuilder.UseGpu();
                    }
                    else
                    {
                        imageClassifierListener.OnError("GPU is not supported on this device");
                    }
                    break;
                case DelegateNnapi:
                    baseOptionsBuilder.UseNnapi();
                    break;
               }

            optionsBuilder.SetBaseOptions(baseOptionsBuilder.Build());

            var modelName =
                CurrentModel switch
                {
                    ModelMobilenetV1 => "mobilenetv1.tflite",
                    ModelEfficientnetV0 => "efficientnet-lite0.tflite",
                    ModelEfficientnetV1 => "efficientnet-lite1.tflite",
                    ModelEfficientnetV2 => "efficientnet-lite2.tflite",
                    _ => "mobilenetv1.tflite"
                };

            try
            {
                imageClassifier =
                    ImageClassifier.CreateFromFileAndOptions(context, modelName, optionsBuilder.Build());
            }
            catch (IllegalStateException e)
            {
                imageClassifierListener.OnError(
                    "Image classifier failed to initialize. See error logs for details"
                );
                Log.Error(Tag, "TFLite failed to load model with error: " + e.Message);
            }
        }

        public void Classify(Bitmap image, SurfaceOrientation rotation)
        {
            if (imageClassifier == null)
            {
                SetupImageClassifier();
            }

            // Inference time is the difference between the system time at the start and finish of the
            // process
            var inferenceTime = SystemClock.UptimeMillis();

            // Create preprocessor for the image.
            // See https://www.tensorflow.org/lite/inference_with_metadata/
            //            lite_support#imageprocessor_architecture
            var imageProcessor =
                new ImageProcessor.Builder()
                    .Build();

            // Preprocess the image and convert it into a TensorImage for classification.
            var tensorImage = imageProcessor.Process(TensorImage.FromBitmap(image));

            var imageProcessingOptions = ImageProcessingOptions.InvokeBuilder()
                .SetOrientation(GetOrientationFromRotation(rotation))
                .Build();

            var results = imageClassifier.Classify(tensorImage, imageProcessingOptions);
            inferenceTime = SystemClock.UptimeMillis() - inferenceTime;
            imageClassifierListener.OnResults(
                results,
                inferenceTime
            );
        }

        // Receive the device rotation (Surface.x values range from 0->3) and return EXIF orientation
        // http://jpegclub.org/exif_orientation.html
        private ImageProcessingOptions.Orientation GetOrientationFromRotation(SurfaceOrientation rotation)
        {
            return rotation switch
            {
                SurfaceOrientation.Rotation270 => ImageProcessingOptions.Orientation.BottomRight,
                SurfaceOrientation.Rotation180 => ImageProcessingOptions.Orientation.RightBottom,
                SurfaceOrientation.Rotation90 => ImageProcessingOptions.Orientation.TopLeft,
                _ => ImageProcessingOptions.Orientation.RightTop
            };
        }

        public interface IClassifierListener
        {
            void OnError(string error);
            void OnResults(
                IList<Classifications> results,
                long inferenceTime);
        }

        const int DelegateCpu = 0;
        const int DelegateGpu = 1;
        const int DelegateNnapi = 2;
        const int ModelMobilenetV1 = 0;
        const int ModelEfficientnetV0 = 1;
        const int ModelEfficientnetV1 = 2;
        const int ModelEfficientnetV2 = 3;

        private const string Tag = "ImageClassifierHelper";
    }
}
