using System;
using System.Linq;
using Android;
using Android.App;
using Android.Content.PM;
using Android.Media;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.Core.Content;
using AndroidX.Core.OS;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Slider;
using Google.Android.Material.SwitchMaterial;
using Org.Tensorflow.Lite.Support.Audio;
using Org.Tensorflow.Lite.Task.Audio.Classifier;

namespace SoundClassification
{
    [Activity(Name = "org.tensorflow.lite.examples.soundclassifier.MainActivity", Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity,
        CompoundButton.IOnCheckedChangeListener,
        ILabelFormatter,
        IBaseOnChangeListener
    {
        private ProbabilitiesAdapter probabilitiesAdapter = new ProbabilitiesAdapter();

        private AudioClassifier audioClassifier;
        private TensorAudio audioTensor;
        private AudioRecord audioRecord;
        private long classificationInterval = 500L; // how often should classification run in milli-secs
        private Handler handler; // background thread handler to run classification
        private bool isChecked;

        public void OnCheckedChanged(CompoundButton buttonView, bool isChecked)
        {
            if (this.isChecked = isChecked) StartAudioClassification(); else StopAudioClassification();
            KeepScreenOn(isChecked);
        }

        public string GetFormattedValue(float p0)
        {
            return string.Format("{0:0} ms", p0);
        }

        public void OnValueChange(Java.Lang.Object p0, float p1, bool p2)
        {
            classificationInterval = (long)p1;
            StopAudioClassification();
            StartAudioClassification();
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            RecyclerView recyclerView = FindViewById<RecyclerView>(Resource.Id.recycler_view);
            recyclerView.HasFixedSize = false;
            recyclerView.SetAdapter(probabilitiesAdapter);

            // Input switch to turn on/off classification
            SwitchMaterial inputSwitch = FindViewById<SwitchMaterial>(Resource.Id.input_switch);
            KeepScreenOn(isChecked = inputSwitch.Checked);
            inputSwitch.SetOnCheckedChangeListener(this);

            // Slider which control how often the classification task should run
            Slider classificationIntervalSlider = FindViewById<Slider>(Resource.Id.classification_interval_slider);
            classificationIntervalSlider.Value = classificationInterval;
            classificationIntervalSlider.SetLabelFormatter(this);
            var method = Java.Lang.Class.ForName("com.google.android.material.slider.BaseSlider").GetDeclaredMethods().FirstOrDefault(x => x.Name == "addOnChangeListener");
            method?.Invoke(classificationIntervalSlider, this); // this is implementing IBaseOnChangeListener

            // Create a handler to run classification in a background thread
            var handlerThread = new HandlerThread("backgroundThread");
            handlerThread.Start();
            handler = HandlerCompat.CreateAsync(handlerThread.Looper);

            // Initialize the audio classifier
            audioClassifier = AudioClassifier.CreateFromFile(this, ModelFile);
            audioTensor = audioClassifier.CreateInputTensorAudio();

            // Request microphone permission and start running classification
            RequestMicrophonePermission();
        }

        private void StartAudioClassification()
        {
            if (!isChecked) return;

            // Initialize the audio recorder
            audioRecord = audioClassifier.CreateAudioRecord();
            audioRecord.StartRecording();

            // Define the classification runnable
            Action run = null;
            run = (() =>
            {
                lock (handler)
                {
                    // If the audio recorder is not initialized and running, do nothing.
                    if (audioRecord == null) return;

                    var startTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

                    // Load the latest audio sample
                    audioTensor.Load(audioRecord);
                    var output = audioClassifier.Classify(audioTensor);

                    // Filter out results above a certain threshold, and sort them descendingly
                    var filteredModelOutput = output[0].Categories.Where(it =>
                        it.Score > MinimumDisplayThreshold
                    ).OrderBy(it =>
                        -it.Score
                    ).ToList();

                    var finishTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

                    Log.Debug(Tag, "Latency = {0}ms", finishTime - startTime);

                    // Updating the UI
                    RunOnUiThread(() =>
                    {
                        probabilitiesAdapter.CategoryList = filteredModelOutput;
                        probabilitiesAdapter.NotifyDataSetChanged();
                    });

                    // Rerun the classification after a certain interval
                    handler.PostDelayed(run, classificationInterval);
                }
            });

            // Start the classification process
            handler.Post(run);
        }

        private void StopAudioClassification()
        {
            lock(handler)
            {
                handler.RemoveCallbacksAndMessages(null);
                audioRecord?.Stop();
                audioRecord = null;
            }
        }

        public override void OnTopResumedActivityChanged(bool isTopResumedActivity)
        {
            // Handles "top" resumed event on multi-window environment
            if (isTopResumedActivity)
            {
                RequestMicrophonePermission();
            }
            else
            {
                StopAudioClassification();
            }
        }

        public override void OnRequestPermissionsResult(
            int requestCode,
            string[] permissions,
            Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (requestCode == RequestRecordAudio)
            {
                if (grantResults.Length > 0 && grantResults[0] == Permission.Granted)
                {
                    Log.Info(Tag, "Audio permission granted :)");
                    StartAudioClassification();
                }
                else
                {
                    Log.Error(Tag, "Audio permission not granted :(");
                }
            }
        }

        private void RequestMicrophonePermission()
        {
            if (ContextCompat.CheckSelfPermission(
                this,
                Manifest.Permission.RecordAudio
                ) == Permission.Granted)
            {
                StartAudioClassification();
            }
            else
            {
                RequestPermissions(new string[] { Manifest.Permission.RecordAudio }, RequestRecordAudio);
            }
        }

        private void KeepScreenOn(bool enable)
        {
            if (enable)
            {
                Window.AddFlags(WindowManagerFlags.KeepScreenOn);
            }
            else
            {
                Window.ClearFlags(WindowManagerFlags.KeepScreenOn);
            }
        }

        const int RequestRecordAudio = 1337;
        private const string Tag = "AudioDemo";
        private const string ModelFile = "yamnet.tflite";
        private const float MinimumDisplayThreshold = 0.3f;
    }
}
