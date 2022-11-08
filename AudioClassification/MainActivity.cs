using Android.App;
using Android.OS;
using AndroidX.AppCompat.App;

namespace AudioClassification
{
    [Activity(Name = "org.tensorflow.lite.examples.audioclassification.MainActivity", Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);
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
                base.OnBackPressed();
            }
        }
    }
}
