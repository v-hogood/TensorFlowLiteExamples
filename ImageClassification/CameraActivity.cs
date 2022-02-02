using System;
using System.Collections.Generic;
using System.Linq;
using Android;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.View;
using AndroidX.Core.Content;
using AndroidX.Lifecycle;
using Google.Android.Material.BottomSheet;
using Java.Lang;
using Java.Util.Concurrent;
using static ImageClassification.Classifier;

namespace ImageClassification
{
    public abstract class CameraActivity : AppCompatActivity,
        ImageAnalysis.IAnalyzer,
        PixelCopy.IOnPixelCopyFinishedListener,
        ViewTreeObserver.IOnGlobalLayoutListener,
        View.IOnClickListener,
        AdapterView.IOnItemSelectedListener
    {
        private const string Tag = "CameraActivity";

        private const int PermissionsRequest = 1;
        private const string PermissionsCamera = Manifest.Permission.Camera;
        private SurfaceView surfaceView;
        private TextureView textureView;
        protected Bitmap rgbFrameBitmap = null;
        private Handler handler;
        private HandlerThread handlerThread;
        private bool isProcessingFrame = false;
        private Action postInferenceCallback;
        private LinearLayout bottomSheetLayout;
        private LinearLayout gestureLayout;
        private BottomSheetBehavior sheetBehavior;
        protected TextView recognitionTextView,
            recognition1TextView,
            recognition2TextView,
            recognitionValueTextView,
            recognition1ValueTextView,
            recognition2ValueTextView;
        protected TextView frameValueTextView,
            cropValueTextView,
            cameraResolutionTextView,
            rotationTextView,
            inferenceTimeTextView;
        protected ImageView bottomSheetArrowImageView;
        private ImageView plusImageView, minusImageView;
        private Spinner modelSpinner;
        private Spinner deviceSpinner;
        private TextView threadsTextView;

        private Model model = Model.Quantized_EfficientNet;
        private Device device = Device.CPU;
        private int numThreads = -1;

        private ProcessCameraProvider cameraProvider;
        private IExecutorService executor = Executors.NewSingleThreadExecutor();
        private BottomSheetCallback bottomSheetCallback;

        public void OnGlobalLayout()
        {
            gestureLayout.ViewTreeObserver.RemoveOnGlobalLayoutListener(this);
            // int width = bottomSheetLayout.MeasuredWidth;
            int height = gestureLayout.MeasuredHeight;
            sheetBehavior.PeekHeight = height;
        }

        private class BottomSheetCallback : BottomSheetBehavior.BottomSheetCallback
        {
            private CameraActivity parent;

            public BottomSheetCallback(CameraActivity activity)
            {
                parent = activity;
            }

            public override void OnStateChanged(View bottomSheet, int newState)
            {
                switch (newState)
                {
                    case BottomSheetBehavior.StateHidden:
                        break;
                    case BottomSheetBehavior.StateExpanded:
                        parent.bottomSheetArrowImageView.SetImageResource(Resource.Drawable.icn_chevron_down);
                        break;
                    case BottomSheetBehavior.StateCollapsed:
                        parent.bottomSheetArrowImageView.SetImageResource(Resource.Drawable.icn_chevron_up);
                        break;
                    case BottomSheetBehavior.StateDragging:
                        break;
                    case BottomSheetBehavior.StateSettling:
                        parent.bottomSheetArrowImageView.SetImageResource(Resource.Drawable.icn_chevron_up);
                        break;
                }
            }

            public override void OnSlide(View bottomSheet, float newState) { }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            Log.Debug(Tag, "OnCreate " + this);
            base.OnCreate(null);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            Window.AddFlags(WindowManagerFlags.KeepScreenOn);
            SetContentView(Resource.Layout.tfe_ic_activity_camera);

            threadsTextView = (TextView)FindViewById(Resource.Id.threads);
            plusImageView = (ImageView)FindViewById(Resource.Id.plus);
            minusImageView = (ImageView)FindViewById(Resource.Id.minus);
            modelSpinner = (Spinner)FindViewById(Resource.Id.model_spinner);
            deviceSpinner = (Spinner)FindViewById(Resource.Id.device_spinner);
            bottomSheetLayout = (LinearLayout)FindViewById(Resource.Id.bottom_sheet_layout);
            gestureLayout = (LinearLayout)FindViewById(Resource.Id.gesture_layout);
            sheetBehavior = BottomSheetBehavior.From(bottomSheetLayout);
            bottomSheetArrowImageView = (ImageView)FindViewById(Resource.Id.bottom_sheet_arrow);

            ViewTreeObserver vto = gestureLayout.ViewTreeObserver;
            vto.AddOnGlobalLayoutListener(this);
            sheetBehavior.Hideable = false;

            bottomSheetCallback = new BottomSheetCallback(this);
            sheetBehavior.AddBottomSheetCallback(bottomSheetCallback);

            recognitionTextView = (TextView)FindViewById(Resource.Id.detected_item);
            recognitionValueTextView = (TextView)FindViewById(Resource.Id.detected_item_value);
            recognition1TextView = (TextView)FindViewById(Resource.Id.detected_item1);
            recognition1ValueTextView = (TextView)FindViewById(Resource.Id.detected_item1_value);
            recognition2TextView = (TextView)FindViewById(Resource.Id.detected_item2);
            recognition2ValueTextView = (TextView)FindViewById(Resource.Id.detected_item2_value);

            frameValueTextView = (TextView)FindViewById(Resource.Id.frame_info);
            cropValueTextView = (TextView)FindViewById(Resource.Id.crop_info);
            cameraResolutionTextView = (TextView)FindViewById(Resource.Id.view_info);
            rotationTextView = (TextView)FindViewById(Resource.Id.rotation_info);
            inferenceTimeTextView = (TextView)FindViewById(Resource.Id.inference_info);

            modelSpinner.OnItemSelectedListener = this;
            deviceSpinner.OnItemSelectedListener = this;

            plusImageView.SetOnClickListener(this);
            minusImageView.SetOnClickListener(this);

            model = (Model)System.Enum.Parse(typeof(Model), modelSpinner.SelectedItem.ToString());
            device = (Device)System.Enum.Parse(typeof(Device), deviceSpinner.SelectedItem.ToString());
            numThreads = int.Parse(threadsTextView.Text.ToString().Trim());
        }

        public void OnPixelCopyFinished(int copyResult)
        {
            if (copyResult != (int)PixelCopyResult.Success)
            {
                Log.Error(Tag, "OnPixelCopyFinished() failed with error " + copyResult);
            }
        }

        // Callback for androidx.camera API
        public void Analyze(IImageProxy image)
        {
            image.Close();
            if (isProcessingFrame | rgbFrameBitmap == null)
            {
                Log.Warn(Tag, "Dropping frame!");
                return;
            }

            isProcessingFrame = true;

            // Copy out RGB bits to our shared buffer
            if (surfaceView != null && surfaceView.Holder.Surface != null && surfaceView.Holder.Surface.IsValid)
            {
                PixelCopy.Request(surfaceView, rgbFrameBitmap, this, surfaceView.Handler);
            }
            else if (textureView != null && textureView.IsAvailable)
            {
                textureView.GetBitmap(rgbFrameBitmap);
            }

            postInferenceCallback = () =>
            {
                isProcessingFrame = false;
            };

            ProcessImage();
        }

        protected override void OnStart()
        {
            Log.Debug(Tag, "OnStart " + this);
            base.OnStart();
        }

        protected override void OnResume()
        {
            Log.Debug(Tag, "OnResume " + this);
            base.OnResume();

            handlerThread = new HandlerThread("inference");
            handlerThread.Start();
            handler = new Handler(handlerThread.Looper);

            if (HasPermission())
            {
                BindCameraUseCases();
            }
            else
            {
                RequestPermission();
            }
        }

        protected override void OnPause()
        {
            Log.Debug(Tag, "OnPause " + this);

            handlerThread.QuitSafely();
            try
            {
                handlerThread.Join();
                handlerThread = null;
                handler = null;
            }
            catch (InterruptedException e)
            {
                Log.Error(Tag, e, "Exception!");
            }

            cameraProvider?.UnbindAll();
            rgbFrameBitmap = null;

            base.OnPause();
        }

        protected override void OnStop()
        {
            Log.Debug(Tag, "OnStop " + this);
            base.OnStop();
        }

        protected override void OnDestroy()
        {
            Log.Debug(Tag, "OnDestroy " + this);
            sheetBehavior.RemoveBottomSheetCallback(bottomSheetCallback);
            base.OnDestroy();
        }

        protected void RunInBackground(Action a)
        {
            handler?.Post(a);
        }

        public override void OnRequestPermissionsResult(
            int requestCode, string[] permissions, Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            if (requestCode == PermissionsRequest)
            {
                if (grantResults.All(x => x == Permission.Granted))
                {
                    BindCameraUseCases();
                }
                else
                {
                    RequestPermission();
                }
            }
        }

        private bool HasPermission()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                return CheckSelfPermission(PermissionsCamera) == Permission.Granted;
            }
            else
            {
                return true;
            }
        }

        private void RequestPermission()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                if (ShouldShowRequestPermissionRationale(PermissionsCamera))
                {
                    Toast.MakeText(
                        this,
                        "Camera permission is required for this demo",
                        ToastLength.Long)
                    .Show();
                }
                RequestPermissions(new string[] { PermissionsCamera }, PermissionsRequest);
            }
        }

        // Declare and bind preview and analysis use cases
        private void BindCameraUseCases()
        {
            PreviewView previewView = FindViewById(Resource.Id.preview) as PreviewView;
            previewView.Post(() =>
            {
                var cameraProviderFuture = ProcessCameraProvider.GetInstance(this);
                cameraProviderFuture.AddListener(new Runnable(() =>
                {
                    // Camera provider is now guaranteed to be available
                    cameraProvider = cameraProviderFuture.Get() as ProcessCameraProvider;

                    // Set up the view finder use case to display camera preview
                    var preview = new Preview.Builder()
                        .SetTargetResolution(DesiredPreviewSize)
                        .Build();

                    // Set up the image analysis use case which will process frames in real time
                    var imageAnalysis = new ImageAnalysis.Builder()
                        .SetTargetResolution(DesiredPreviewSize)
                        .SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)
                        .Build();

                    imageAnalysis.SetAnalyzer(executor, this);

                    // Create a new camera selector each time, enforcing lens facing
                    var cameraSelector = new CameraSelector.Builder()
                        .RequireLensFacing(CameraSelector.LensFacingBack)
                        .Build();

                    // Apply declared configs to CameraX using the same lifecycle owner
                    var camera = cameraProvider.BindToLifecycle(
                        this as ILifecycleOwner, cameraSelector, preview, imageAnalysis);

                    // Use the camera object to link our preview use case with the view
                    preview.SetSurfaceProvider(previewView.SurfaceProvider);

                    OnPreviewSizeChosen(preview.AttachedSurfaceResolution, 0);

                    previewView.Post(() =>
                    {
                        surfaceView = previewView.GetChildAt(0) as SurfaceView;
                        textureView = previewView.GetChildAt(0) as TextureView;
                    });

                }), ContextCompat.GetMainExecutor(this));
            });
        }

        protected void ReadyForNextImage()
        {
            postInferenceCallback?.Invoke();
        }

        protected int GetScreenOrientation()
        {
            return WindowManager.DefaultDisplay.Rotation switch
            {
                SurfaceOrientation.Rotation270 => 270,
                SurfaceOrientation.Rotation180 => 180,
                SurfaceOrientation.Rotation90 => 90,
                _ => 0
            };
        }

        // @UiThread
        protected void ShowResultsInBottomSheet(List<Recognition> results)
        {
            if (results != null && results.Count >= 3)
            {
                Recognition recognition = results[0];
                if (recognition != null)
                {
                    if (recognition.Title != null)
                        recognitionTextView.Text = recognition.Title;
                    if (recognition.Confidence != null)
                        recognitionValueTextView.Text =
                            string.Format("{0:0.00}", 100 * recognition.Confidence.FloatValue()) + "%";
                }

                Recognition recognition1 = results[1];
                if (recognition1 != null)
                {
                    if (recognition1.Title != null)
                        recognition1TextView.Text = recognition1.Title;
                    if (recognition1.Confidence != null)
                        recognition1ValueTextView.Text =
                            string.Format("{0:0.00}", 100 * recognition1.Confidence.FloatValue()) + "%";
                }

                Recognition recognition2 = results[2];
                if (recognition2 != null)
                {
                    if (recognition2.Title != null)
                        recognition2TextView.Text = recognition2.Title;
                    if (recognition2.Confidence != null)
                        recognition2ValueTextView.Text =
                            string.Format("{0:0.00}", 100 * recognition2.Confidence.FloatValue()) + "%";
                }
            }
        }

        protected void ShowFrameInfo(string frameInfo)
        {
            frameValueTextView.Text = frameInfo;
        }

        protected void ShowCropInfo(string cropInfo)
        {
            cropValueTextView.Text = cropInfo;
        }

        protected void ShowCameraResolution(string cameraInfo)
        {
            cameraResolutionTextView.Text = cameraInfo;
        }

        protected void ShowRotationInfo(string rotation)
        {
            rotationTextView.Text = rotation;
        }

        protected void ShowInference(string inferenceTime)
        {
            inferenceTimeTextView.Text = inferenceTime;
        }

        protected Model GetModel()
        {
            return model;
        }

        private void SetModel(Model model)
        {
            if (this.model != model)
            {
                Log.Debug(Tag, "Updating model: " + model);
                this.model = model;
                OnInferenceConfigurationChanged();
            }
        }

        protected Device GetDevice()
        {
            return device;
        }

        private void SetDevice(Device device)
        {
            if (this.device != device)
            {
                Log.Debug(Tag, "Updating device: " + device);
                this.device = device;
                bool threadsEnabled = device == Device.CPU;
                plusImageView.Enabled = threadsEnabled;
                minusImageView.Enabled = threadsEnabled;
                threadsTextView.Text = threadsEnabled ? numThreads.ToString() : "N/A";
                OnInferenceConfigurationChanged();
            }
        }

        protected int GetNumThreads()
        {
            return numThreads;
        }

        private void SetNumThreads(int numThreads)
        {
            if (this.numThreads != numThreads)
            {
                Log.Debug(Tag, "Updating numThreads: " + numThreads);
                this.numThreads = numThreads;
                OnInferenceConfigurationChanged();
            }
        }

        protected abstract void ProcessImage();

        protected abstract void OnPreviewSizeChosen(Size size, int rotation);

        protected abstract Size DesiredPreviewSize { get; }

        protected abstract void OnInferenceConfigurationChanged();

        public void OnClick(View v)
        {
            if (v.Id == Resource.Id.plus)
            {
                string threads = threadsTextView.Text.ToString().Trim();
                int numThreads = int.Parse(threads);
                if (numThreads >= 9) return;
                SetNumThreads(++numThreads);
                threadsTextView.Text = numThreads.ToString();
            }
            else if (v.Id == Resource.Id.minus)
            {
                string threads = threadsTextView.Text.ToString().Trim();
                int numThreads = int.Parse(threads);
                if (numThreads == 1) return;
                SetNumThreads(--numThreads);
                threadsTextView.Text = numThreads.ToString();
            }
        }

        public void OnItemSelected(AdapterView parent, View view, int pos, long id)
        {
            if (parent == modelSpinner)
            {
                SetModel((Model)System.Enum.Parse(typeof(Model), parent.GetItemAtPosition(pos).ToString()));
            }
            else if (parent == deviceSpinner)
            {
                SetDevice((Device)System.Enum.Parse(typeof(Device), parent.GetItemAtPosition(pos).ToString()));
            }
        }

        public void OnNothingSelected(AdapterView parent)
        {
            // Do nothing.
        }
    }
}
