using System.Collections.Generic;
using Android.App;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;

namespace TextClassification
{
    // The main activity to provide interactions with users.
    [Activity(Name = "org.tensorflow.lite.examples.textclassification.MainActivity", Label = "@string/app_name", Theme = "@style/AppTheme.TextClassification", MainLauncher = true)]
    public class MainActivity : AppCompatActivity,
        ViewAnimator.IOnClickListener
    {
        private const string Tag = "TextClassificationDemo";

        private TextClassificationClient client;

        private TextView resultTextView;
        private EditText inputEditText;
        private Handler handler;
        private ScrollView scrollView;

        public void OnClick(View v)
        {
            Classify(inputEditText.Text.ToString());
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.tfe_tc_activity_main);
            Log.Verbose(Tag, "OnCreate");

            client = new TextClassificationClient(ApplicationContext);
            handler = new Handler(Looper.MainLooper);
            Button classifyButton = (Button)FindViewById(Resource.Id.button);
            classifyButton.SetOnClickListener(this);
            resultTextView = (TextView)FindViewById(Resource.Id.result_text_view);
            inputEditText = (EditText)FindViewById(Resource.Id.input_text);
            scrollView = (ScrollView)FindViewById(Resource.Id.scroll_view);
        }

        protected override void OnStart()
        {
            base.OnStart();
            Log.Verbose(Tag, "OnStart");
            handler.Post(() =>
            {
                client.Load();
            });
        }

        protected override void OnStop()
        {
            base.OnStop();
            Log.Verbose(Tag, "OnStop");
            handler.Post(() =>
            {
                client.Unload();
            });
        }

        // Send input text to TextClassificationClient and get the classify messages.
        private void Classify(string text)
        {
            handler.Post(() =>
            {
                // Run text classification with TF Lite.
                List<Result> results = client.Classify(text);

                // Show classification result on screen
                ShowResult(text, results);
            });
        }

        // Show classification result on the screen.
        private void ShowResult(string inputText, List<Result> results)
        {
            // Run on UI thread as we'll updating our app UI
            RunOnUiThread(() =>
            {
                string textToShow = "Input: " + inputText + "\nOutput:\n";
                for (int i = 0; i < results.Count; i++)
                {
                    Result result = results[i];
                    textToShow += string.Format("    {0}: {1}\n", result.Title, result.Confidence);
                }
                textToShow += "---------\n";

                // Append the result to the UI.
                resultTextView.Append(textToShow);

                // Clear the input text.
                inputEditText.EditableText.Clear();

                // Scroll to the bottom to show latest entry's classification result.
                scrollView.Post(() => scrollView.FullScroll(FocusSearchDirection.Down));
            });
        }
    }
}
