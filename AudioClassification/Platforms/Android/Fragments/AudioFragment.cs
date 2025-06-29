using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.Widget;
using AndroidX.Navigation;
using AndroidX.RecyclerView.Widget;
using TensorFlow.Lite.Support.Label;
using Fragment = AndroidX.Fragment.App.Fragment;
using View = Android.Views.View;

namespace AudioClassification
{
    public interface IAudioClassificationListener
    {
        void OnError(string error);
        void OnResult(IList<Category> results, long interfaceTime);
    }

    [Android.App.Activity(Name = "org.tensorflow.lite.examples.audioclassification.fragments.AudioFragment")]
    public class AudioFragment : Fragment,
        IAudioClassificationListener,
        RadioGroup.IOnCheckedChangeListener,
        AdapterView.IOnItemSelectedListener,
        View.IOnClickListener
    {
        private ProbabilitiesAdapter adapter = new ProbabilitiesAdapter();

        private AudioClassificationHelper audioHelper;

        public void OnResult(IList<Category> results, long inferenceTime)
        {
            RequireActivity().RunOnUiThread(() =>
            {
                adapter.CategoryList = results;
                adapter.NotifyDataSetChanged();
                RequireActivity().FindViewById<TextView>(Resource.Id.inference_time_val).Text =
                    inferenceTime + " ms";
                });
        }

        public void OnError(string error)
        {
            RequireActivity().RunOnUiThread(() =>
            {
                Toast.MakeText(RequireContext(), error, ToastLength.Short).Show();
                adapter.CategoryList = new List<Category>();
                adapter.NotifyDataSetChanged();
            });
        }

        public void OnCheckedChanged(RadioGroup group, int checkedId)
        {
            switch(checkedId)
            {
                case Resource.Id.yamnet:
                    audioHelper.StopAudioClassification();
                    audioHelper.CurrentModel = AudioClassificationHelper.YamnetModel;
                    audioHelper.InitClassifier();
                    break;
                case Resource.Id.speech_command:
                    audioHelper.StopAudioClassification();
                    audioHelper.CurrentModel = AudioClassificationHelper.SpeechCommandModel;
                    audioHelper.InitClassifier();
                    break;
            }
        }

        public void OnItemSelected(AdapterView parent, View view, int position, long id)
        {
            if (parent.Id == Resource.Id.spinner_overlap)
            {
                audioHelper.StopAudioClassification();
                audioHelper.Overlap = 0.25f * position;
                audioHelper.StartAudioClassification();
            }
            else if (parent.Id == Resource.Id.spinner_delegate)
            {
                audioHelper.StopAudioClassification();
                audioHelper.CurrentDelegate = position;
                audioHelper.InitClassifier();
            }
        }

        public void OnNothingSelected(AdapterView parent)
        {
            // no op
        }

        public void OnClick(View v)
        {
            if (v.Id == Resource.Id.results_minus)
            {
                if (audioHelper.NumOfResults > 1)
                {
                    audioHelper.NumOfResults--;
                    audioHelper.StopAudioClassification();
                    audioHelper.InitClassifier();
                    RequireActivity().FindViewById<TextView>(Resource.Id.results_value).Text =
                        audioHelper.NumOfResults.ToString();
                }
            }
            else if (v.Id == Resource.Id.results_plus)
            {
                if (audioHelper.NumOfResults < 5)
                {
                    audioHelper.NumOfResults++;
                    audioHelper.StopAudioClassification();
                    audioHelper.InitClassifier();
                    RequireActivity().FindViewById<TextView>(Resource.Id.results_value).Text =
                        audioHelper.NumOfResults.ToString();
                }
            }
            else if (v.Id == Resource.Id.threshold_minus)
            {
                if (audioHelper.ClassificationThreshold >= 0.2)
                {
                    audioHelper.StopAudioClassification();
                    audioHelper.ClassificationThreshold -= 0.1f;
                    audioHelper.InitClassifier();
                    RequireActivity().FindViewById<TextView>(Resource.Id.threshold_value).Text =
                        audioHelper.ClassificationThreshold.ToString("0.00");
                }
            }
            else if (v.Id == Resource.Id.threshold_plus)
            {
                if (audioHelper.ClassificationThreshold <= 0.8)
                {
                    audioHelper.StopAudioClassification();
                    audioHelper.ClassificationThreshold += 0.1f;
                    audioHelper.InitClassifier();
                    RequireActivity().FindViewById<TextView>(Resource.Id.threshold_value).Text =
                        audioHelper.ClassificationThreshold.ToString("0.00");
                }
            }
            else if (v.Id == Resource.Id.threads_minus)
            {
                if (audioHelper.NumThreads > 1)
                {
                    audioHelper.StopAudioClassification();
                    audioHelper.NumThreads--;
                    RequireActivity().FindViewById<TextView>(Resource.Id.threads_value).Text = audioHelper
                        .NumThreads
                        .ToString();
                    audioHelper.InitClassifier();
                }
            }
            else if (v.Id == Resource.Id.threads_plus)
            {
                if (audioHelper.NumThreads < 4)
                {
                    audioHelper.StopAudioClassification();
                    audioHelper.NumThreads++;
                    RequireActivity().FindViewById<TextView>(Resource.Id.threads_value).Text = audioHelper
                        .NumThreads
                        .ToString();
                    audioHelper.InitClassifier();
                }
            }
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.fragment_audio, container, false);
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            RequireActivity().FindViewById<RecyclerView>(Resource.Id.recycler_view).SetAdapter(adapter);

            audioHelper = new AudioClassificationHelper(RequireContext(), this);

            // Allow the user to select between multiple supported audio models.
            // The original location and documentation for these models is listed in
            // the `download_model.gradle` file within this sample. You can also create your own
            // audio model by following the documentation here:
            // https://www.tensorflow.org/lite/models/modify/model_maker/speech_recognition
            RequireActivity().FindViewById<RadioGroup>(Resource.Id.model_selector).SetOnCheckedChangeListener(this);

            // Allow the user to change the amount of overlap used in classification. More overlap
            // can lead to more accurate resolves in classification.
            AppCompatSpinner spinnerOverlap = RequireActivity().FindViewById<AppCompatSpinner>(Resource.Id.spinner_overlap);
            spinnerOverlap.OnItemSelectedListener = this;

            // Allow the user to change the max number of results returned by the audio classifier.
            // Currently allows between 1 and 5 results, but can be edited here.
            RequireActivity().FindViewById<AppCompatImageButton>(Resource.Id.results_minus).SetOnClickListener(this);
            RequireActivity().FindViewById<AppCompatImageButton>(Resource.Id.results_plus).SetOnClickListener(this);

            // Allow the user to change the confidence threshold required for the classifier to return
            // a result. Increments in steps of 10%.
            RequireActivity().FindViewById<AppCompatImageButton>(Resource.Id.threshold_minus).SetOnClickListener(this);
            RequireActivity().FindViewById<AppCompatImageButton>(Resource.Id.threshold_plus).SetOnClickListener(this);

            // Allow the user to change the number of threads used for classification
            RequireActivity().FindViewById<AppCompatImageButton>(Resource.Id.threads_minus).SetOnClickListener(this);
            RequireActivity().FindViewById<AppCompatImageButton>(Resource.Id.threads_plus).SetOnClickListener(this);

            // When clicked, change the underlying hardware used for inference. Current options are CPU
            // and NNAPI. GPU is another available option, but when using this option you will need
            // to initialize the classifier on the thread that does the classifying. This requires a
            // different app structure than is used in this sample.
            RequireActivity().FindViewById<AppCompatSpinner>(Resource.Id.spinner_delegate).OnItemSelectedListener = this;

            spinnerOverlap.SetSelection(2, false);
            spinnerOverlap.SetSelection(0, false);
        }

        public override void OnResume()
        {
            base.OnResume();

            // Make sure that all permissions are still present, since the
            // user could have removed them while the app was in paused state.
            if (!PermissionsFragment.HasPermissions(RequireContext()))
            {
                Navigation.FindNavController(RequireActivity(), Resource.Id.fragment_container).Navigate(
                    Resource.Id.action_audio_to_permissions);
            }

            if (audioHelper != null)
            {
                audioHelper.StartAudioClassification();
            }
        }
    }
}
