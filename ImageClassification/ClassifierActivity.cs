using System.Collections.Generic;
using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Widget;
using Java.IO;
using Java.Lang;
using static ImageClassification.Classifier;

namespace ImageClassification
{
    [Activity(Name = "org.tensorflow.lite.examples.classification.ClassifierActivity", Label = "@string/app_name", Theme = "@style/AppTheme.ImageClassification", MainLauncher = true)]
    public class ClassifierActivity : CameraActivity
    {
        private const string Tag = "ClassifierActivity";
        protected override Size DesiredPreviewSize { get; } = new Size(480, 640);
        private int previewWidth = 0;
        private int previewHeight = 0;
        private long lastProcessingTimeMs;
        private Classifier classifier;
        // Input image size of the model along x axis.
        private int imageSizeX;
        // Input image size of the model along y axis.
        private int imageSizeY;

        protected override void OnPreviewSizeChosen(Size size)
        {
            RecreateClassifier(GetModel(), GetDevice(), GetNumThreads());
            if (classifier == null)
            {
                Log.Error(Tag, "No classifier on preview!");
                return;
            }

            previewWidth = size.Width;
            previewHeight = size.Height;

            Log.Info(Tag, "Initializing at size " + previewWidth + "x" + previewHeight);
            rgbFrameBitmap = Bitmap.CreateBitmap(previewWidth, previewHeight, Bitmap.Config.Argb8888);
        }

        protected override void ProcessImage(int rotation)
        {
            int cropSize = Math.Min(previewWidth, previewHeight);

            RunInBackground(() =>
            {
                if (classifier != null)
                {
                    long startTime = SystemClock.UptimeMillis();
                    List<Classifier.Recognition> results =
                        classifier.RecognizeImage(rgbFrameBitmap, rotation);
                    lastProcessingTimeMs = SystemClock.UptimeMillis() - startTime;
                    Log.Verbose(Tag, "Detect: " + results);

                    RunOnUiThread(() =>
                    {
                        ShowResultsInBottomSheet(results);
                        ShowFrameInfo(previewWidth + "x" + previewHeight);
                        ShowCropInfo(imageSizeX + "x" + imageSizeY);
                        ShowCameraResolution(cropSize + "x" + cropSize);
                        ShowRotationInfo(rotation.ToString());
                        ShowInference(lastProcessingTimeMs + "ms");
                    });
                }
                ReadyForNextImage();
            });
        }

        protected override void OnInferenceConfigurationChanged()
        {
            if (rgbFrameBitmap == null)
            {
                // Defer creation until we're getting camera frames.
                return;
            }
            Device device = GetDevice();
            Model model = GetModel();
            int numThreads = GetNumThreads();
            RunInBackground(() => RecreateClassifier(model, device, numThreads));
        }

        private void RecreateClassifier(Model model, Device device, int numThreads)
        {
            if (classifier != null)
            {
                Log.Debug(Tag, "Closing classifier.");
                classifier.Close();
                classifier = null;
            }
            if (device == Device.GPU
                && (model == Model.Quantized_MobileNet || model == Model.Quantized_EfficientNet))
            {
                Log.Debug(Tag, "Not creating classifier: GPU doesn't support quantized models.");
                RunOnUiThread(() =>
                    Toast.MakeText(this, Resource.String.tfe_ic_gpu_quant_error, ToastLength.Long).Show());
                return;
            }
            try
            {
                Log.Debug(Tag,
                    "Creating classifier (model={0}, device={1}, numThreads={2})", model, device, numThreads);
                classifier = Classifier.Create(this, model, device, numThreads);
            }
            catch (Exception e)
            {
                if (e is IOException || e is IllegalArgumentException)
                {
                    Log.Error(Tag, e, "Failed to create classifier.");
                    RunOnUiThread(() =>
                        Toast.MakeText(this, e.Message, ToastLength.Long).Show());
                }
                else throw;
                return;
            }

            // Updates the input image size.
            imageSizeX = classifier.ImageSizeX;
            imageSizeY = classifier.ImageSizeY;
        }
    }
}
