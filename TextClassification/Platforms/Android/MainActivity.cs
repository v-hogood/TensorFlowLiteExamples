using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using AndroidX.Core.Widget;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.BottomSheet;
using Google.Android.Material.Button;
using Google.Android.Material.TextField;
using TensorFlow.Lite.Support.Label;
using View = Android.Views.View;

namespace TextClassification
{
    [Activity(Name = "org.tensorflow.lite.examples.textclassification.MainActivity", Label = "@string/app_name", Theme = "@style/AppTheme.TextClassification", MainLauncher = true)]
    public class MainActivity : AppCompatActivity,
        View.IOnClickListener,
        TextClassificationHelper.TextResultsListener,
        AdapterView.IOnItemSelectedListener,
        RadioGroup.IOnCheckedChangeListener
    {
        private const string Tag = "TextClassification";

        TextClassificationHelper classifierHelper;
        private ResultsAdapter adapter = new ResultsAdapter();

        public void OnResult(IList<Category> results, long inferenceTime)
        {
            RunOnUiThread(() =>
            {
                FindViewById<TextView>(Resource.Id.inference_time_val).Text =
                    inferenceTime + " ms";

                adapter.ResultsList = results.OrderByDescending(it =>
                    it.Score).ToList();

                adapter.NotifyDataSetChanged();
            });
        }

        public void OnError(string error)
        {
            Toast.MakeText(this, error, ToastLength.Short).Show();
        }

        public void OnClick(View v)
        {
            string text = FindViewById<TextInputEditText>(Resource.Id.input_text).Text;
            if (text == null || text.Length == 0)
            {
                classifierHelper.Classify(Resources.GetString(Resource.String.default_edit_text));
            }
            else
            {
                classifierHelper.Classify(text);
            }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            // Create the classification helper that will do the heavy lifting
            classifierHelper = new TextClassificationHelper(
                this, this);

            // Classify the text in the TextEdit box (or the default if nothing is added)
            // on button click.
            FindViewById<MaterialButton>(Resource.Id.classify_btn).SetOnClickListener(this);

            FindViewById<RecyclerView>(Resource.Id.results).SetAdapter(adapter);

            InitBottomSheetControls();
        }

        public void OnItemSelected(AdapterView parent, View view, int position, long id)
        {
            classifierHelper.CurrentDelegate = position;
            classifierHelper.InitClassifier();
        }

        public void OnNothingSelected(AdapterView parent)
        {
            // no op
        }

        private void InitBottomSheetControls()
        {
            var behavior = BottomSheetBehavior.From(FindViewById<NestedScrollView>(Resource.Id.bottom_sheet_layout));
            behavior.State = BottomSheetBehavior.StateExpanded;
            // When clicked, change the underlying hardware used for inference. Current options are CPU
            // and NNAPI. GPU is another available option, but when using this option you will need
            // to initialize the classifier on the thread that does the classifying.
            var spinner = FindViewById<AppCompatSpinner>(Resource.Id.spinner_delegate);
            spinner.OnItemSelectedListener = this;
            spinner.SetSelection(
                0,
                false);

            // Allows the user to switch between the classification models that are available.
            FindViewById<RadioGroup>(Resource.Id.model_selector).SetOnCheckedChangeListener(this);
        }

        public void OnCheckedChanged(RadioGroup group, int checkedId)
        {
            switch(checkedId)
            {
                case Resource.Id.wordvec:
                    classifierHelper.CurrentModel = TextClassificationHelper.WordVec;
                    classifierHelper.InitClassifier();
                    break;
                case Resource.Id.mobilebert:
                    classifierHelper.CurrentModel = TextClassificationHelper.MobileBert;
                    classifierHelper.InitClassifier();
                    break;
            };
        }

        public override void OnBackPressed()
        {
            if (Build.VERSION.SdkInt == BuildVersionCodes.Q)
            {
                // Workaround for Android Q memory leak issue in IRequestFinishCallback$Stub.
                // (https://issuetracker.google.com/issues/139738913)
                FinishAfterTransition();
            }
            else
            {
#pragma warning disable CA1422
                base.OnBackPressed();
#pragma warning restore CA1422
            }
        }
    }
}
