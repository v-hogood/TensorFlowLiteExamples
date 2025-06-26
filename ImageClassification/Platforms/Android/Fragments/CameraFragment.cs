using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.Widget;
using AndroidX.Camera.Core;
using AndroidX.Camera.Core.ResolutionSelector;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.View;
using AndroidX.Core.Content;
using AndroidX.Navigation;
using AndroidX.RecyclerView.Widget;
using Java.Lang;
using Java.Util.Concurrent;
using TensorFlow.Lite.Task.Vision.Classifier;
using Exception = Java.Lang.Exception;
using Fragment = AndroidX.Fragment.App.Fragment;
using Size = Android.Util.Size;
using View = Android.Views.View;

namespace ImageClassification
{
    [Android.App.Activity(Name = "org.tensorflow.lite.examples.imageclassification.fragments.CameraFragment")]
    public class CameraFragment : Fragment,
        ImageClassifierHelper.IClassifierListener,
        View.IOnClickListener,
        AdapterView.IOnItemSelectedListener,
        ImageAnalysis.IAnalyzer
    {
        private new const string Tag = "Image Classifier";

        private ImageClassifierHelper imageClassifierHelper;
        private Bitmap bitmapBuffer;
        private ClassificationResultsAdapter classificationResultsAdapter =
            new ClassificationResultsAdapter();

        private PreviewView viewFinder;
        private Preview preview;
        private ImageAnalysis imageAnalyzer;
        private ICamera camera;
        private ProcessCameraProvider cameraProvider;

        // Blocking camera operations are performed using this executor
        private IExecutorService cameraExecutor;

        public override void OnResume()
        {
            base.OnResume();

            if (!PermissionsFragment.HasPermissions(RequireContext()))
            {
                Navigation.FindNavController(RequireActivity(), Resource.Id.fragment_container)
                    .Navigate(Resource.Id.action_camera_to_permissions);
            }
        }

        public override void OnDestroyView()
        {
            base.OnDestroyView();

            // Shut down our background executor
            cameraExecutor.Shutdown();
        }

        public override View OnCreateView(
            LayoutInflater inflater,
            ViewGroup container,
            Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.fragment_camera, container, false);
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            imageClassifierHelper =
                new ImageClassifierHelper(RequireContext(), this);

            classificationResultsAdapter
                .UpdateAdapterSize(imageClassifierHelper.MaxResults);

            var recyclerViewResults =
                RequireActivity().FindViewById<RecyclerView>(Resource.Id.recyclerview_results);
            recyclerViewResults.SetLayoutManager(new LinearLayoutManager(RequireContext()));
            recyclerViewResults.SetAdapter(classificationResultsAdapter);

            cameraExecutor = Executors.NewSingleThreadExecutor();

            viewFinder =
                RequireActivity().FindViewById<PreviewView>(Resource.Id.view_finder);
            viewFinder.Post(() =>
            {
                // Set up the camera and its use cases
                SetUpCamera();
            });

            // Attach listeners to UI control widgets
            InitBottomSheetControls();
        }

        // Initialize CameraX, and prepare to bind the camera use cases
        private void SetUpCamera()
        {
            var cameraProviderFuture = ProcessCameraProvider.GetInstance(RequireContext());
            cameraProviderFuture.AddListener(new Runnable(() =>
            {
                // CameraProvider
                cameraProvider = cameraProviderFuture.Get() as ProcessCameraProvider;

                // Build and bind the camera use cases
                BindCameraUseCases();
            }),
            ContextCompat.GetMainExecutor(RequireContext()));
        }

        public void OnClick(View v)
        {
            if (v.Id == Resource.Id.threshold_minus)
            {
                if (imageClassifierHelper.Threshold >= 0.1)
                {
                    imageClassifierHelper.Threshold -= 0.1f;
                    UpdateControlsUi();
                }
            }
            else if (v.Id == Resource.Id.threshold_plus)
            {
                if (imageClassifierHelper.Threshold < 0.9)
                {
                    imageClassifierHelper.Threshold += 0.1f;
                    UpdateControlsUi();
                }
            }
            else if (v.Id == Resource.Id.max_results_minus)
            {
                if (imageClassifierHelper.MaxResults > 1)
                {
                    imageClassifierHelper.MaxResults--;
                    UpdateControlsUi();
                    classificationResultsAdapter.UpdateAdapterSize(imageClassifierHelper.MaxResults);
                }
            }
            else if (v.Id == Resource.Id.max_results_plus)
            {
                if (imageClassifierHelper.MaxResults < 3)
                {
                    imageClassifierHelper.MaxResults++;
                    UpdateControlsUi();
                    classificationResultsAdapter.UpdateAdapterSize(imageClassifierHelper.MaxResults);
                }
            }
            else if (v.Id == Resource.Id.threads_minus)
            {
                if (imageClassifierHelper.NumThreads > 1)
                {
                    imageClassifierHelper.NumThreads--;
                    UpdateControlsUi();
                }
            }
            else if (v.Id == Resource.Id.threads_plus)
            {
                if (imageClassifierHelper.NumThreads < 4)
                {
                    imageClassifierHelper.NumThreads++;
                    UpdateControlsUi();
                }
            }
        }

        private void InitBottomSheetControls()
        {
            // When clicked, lower classification score threshold floor
            RequireActivity().FindViewById<AppCompatImageButton>(Resource.Id.threshold_minus).SetOnClickListener(this);
            // When clicked, raise classification score threshold floor
            RequireActivity().FindViewById<AppCompatImageButton>(Resource.Id.threshold_plus).SetOnClickListener(this);

            // When clicked, reduce the number of objects that can be classified at a time
            RequireActivity().FindViewById<AppCompatImageButton>(Resource.Id.max_results_minus).SetOnClickListener(this);
            // When clicked, increase the number of objects that can be classified at a time
            RequireActivity().FindViewById<AppCompatImageButton>(Resource.Id.max_results_plus).SetOnClickListener(this);

            // When clicked, decrease the number of threads used for classification
            RequireActivity().FindViewById<AppCompatImageButton>(Resource.Id.threads_minus).SetOnClickListener(this);
            // When clicked, increase the number of threads used for classification
            RequireActivity().FindViewById<AppCompatImageButton>(Resource.Id.threads_plus).SetOnClickListener(this);

            // When clicked, change the underlying hardware used for inference. Current options are CPU
            // GPU, and NNAPI
            var spinnerDelegate =
                RequireActivity().FindViewById<AppCompatSpinner>(Resource.Id.spinner_delegate);
            spinnerDelegate.SetSelection(0, false);
            spinnerDelegate.OnItemSelectedListener = this;

            // When clicked, change the underlying model used for object classification
            var spinnerModel =
                RequireActivity().FindViewById<AppCompatSpinner>(Resource.Id.spinner_model);
            spinnerModel.SetSelection(0, false);
            spinnerModel.OnItemSelectedListener = this;
        }

        public void OnItemSelected(AdapterView parent, View view, int position, long id)
        {
            if (parent.Id == Resource.Id.spinner_delegate)
            {
                imageClassifierHelper.CurrentDelegate = position;
                UpdateControlsUi();
            }
            else if (parent.Id == Resource.Id.spinner_model)
            {
                imageClassifierHelper.CurrentModel = position;
                UpdateControlsUi();
            }
        }

        public void OnNothingSelected(AdapterView parent)
        {
            // no op;
        }

        // Update the values displayed in the bottom sheet. Reset classifier.
        private void UpdateControlsUi()
        {
            RequireActivity().FindViewById<TextView>(Resource.Id.max_results_value).Text =
                imageClassifierHelper.MaxResults.ToString();

            RequireActivity().FindViewById<TextView>(Resource.Id.threshold_value).Text =
                imageClassifierHelper.Threshold.ToString("0.00");

            RequireActivity().FindViewById<TextView>(Resource.Id.threads_value).Text =
                imageClassifierHelper.NumThreads.ToString();

            // Needs to be cleared instead of reinitialized because the GPU
            // delegate needs to be initialized on the thread using it when applicable
            imageClassifierHelper.ClearImageClassifier();
        }

        public override void OnConfigurationChanged(Android.Content.Res.Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);
            imageAnalyzer.TargetRotation = (int)viewFinder.Display.Rotation;
        }

        // Declare and bind preview, capture and analysis use cases
        private void BindCameraUseCases()
        {
            // CameraProvider
            if (cameraProvider == null)
                throw new IllegalStateException("Camera initialization failed.");

            // CameraSelector - makes assumption that we're only using the back camera
            var cameraSelector =
                new CameraSelector.Builder().RequireLensFacing(CameraSelector.LensFacingBack).Build();

            // ResolutionSelector
            var resolutionSelector = new ResolutionSelector.Builder().
                SetAspectRatioStrategy(AspectRatioStrategy.Ratio43FallbackAutoStrategy)
                .Build();

            // Preview. Only using the 4:3 ratio because this is the closest to our models
            preview =
                new Preview.Builder()
                    .SetResolutionSelector(resolutionSelector)
                    .SetTargetRotation((int)viewFinder.Display.Rotation)
                    .Build();

            // ImageAnalysis. Using RGBA 8888 to match how our models work
            imageAnalyzer =
                new ImageAnalysis.Builder()
                    .SetResolutionSelector(resolutionSelector)
                    .SetTargetRotation((int)viewFinder.Display.Rotation)
                    .SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)
                    .SetOutputImageFormat(ImageAnalysis.OutputImageFormatRgba8888)
                    .Build();

            // The analyzer can then be assigned to the instance
            imageAnalyzer.SetAnalyzer(cameraExecutor, this);

            // Must unbind the use-cases before rebinding them
            cameraProvider.UnbindAll();

            try
            {
                // A variable number of use-cases can be passed here -
                // camera provides access to CameraControl & CameraInfo
                camera = cameraProvider.BindToLifecycle(this, cameraSelector, preview, imageAnalyzer);

                // Attach the viewfinder's surface provider to preview use case
                preview.SetSurfaceProvider(cameraExecutor, viewFinder.SurfaceProvider);
            }
            catch (Exception exc)
            {
                Log.Error(Tag, "Use case binding failed", exc);
            }
        }

        Size ImageAnalysis.IAnalyzer.DefaultTargetResolution => null;

        public void Analyze(IImageProxy image)
        {
            if (bitmapBuffer == null)
            {
                // The image rotation and RGB image buffer are initialized only once
                // the analyzer has started running
                bitmapBuffer = Bitmap.CreateBitmap(
                    image.Width,
                    image.Height,
                    Bitmap.Config.Argb8888
                );
            }

            ClassifyImage(image);
        }

        private SurfaceOrientation GetScreenOrientation()
        {
            DisplayMetrics outMetrics = new DisplayMetrics();

            Display display;
#pragma warning disable CA1422
            if (Android.OS.Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
#pragma warning disable CA1416
                display = RequireActivity().Display;
#pragma warning restore CA1416
                display.GetRealMetrics(outMetrics);
            }
            else
            {
                display = RequireActivity().WindowManager.DefaultDisplay;
                display.GetMetrics(outMetrics);
            }
#pragma warning restore CA1422

            return display.Rotation;
        }

        private void ClassifyImage(IImageProxy image)
        {
            // Copy out RGB bits to the shared bitmap buffer
            bitmapBuffer.CopyPixelsFromBuffer(image.GetPlanes()[0].Buffer);
            image.Close();

            // Pass Bitmap and rotation to the image classifier helper for processing and classification
            imageClassifierHelper.Classify(bitmapBuffer, GetScreenOrientation());
        }

        public void OnError(string error)
        {
            RequireActivity().RunOnUiThread(() =>
            {
                Toast.MakeText(RequireContext(), error, ToastLength.Short).Show();
                classificationResultsAdapter.UpdateResults(null);
                classificationResultsAdapter.NotifyDataSetChanged();
            });
        }

        public void OnResults(IList<Classifications> results, long inferenceTime)
        {
            RequireActivity().RunOnUiThread(() =>
            {
                // Show result on bottom sheet
                classificationResultsAdapter.UpdateResults(results);
                classificationResultsAdapter.NotifyDataSetChanged();
                RequireActivity().FindViewById<TextView>(Resource.Id.inference_time_val).Text =
                    inferenceTime + " ms";
            });
        }
    }
}
