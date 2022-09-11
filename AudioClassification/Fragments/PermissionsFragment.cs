using System.Linq;
using Android;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using AndroidX.Core.Content;
using AndroidX.Fragment.App;
using AndroidX.Navigation;
using Java.Lang;

namespace AudioClassification
{
    //
    // The sole purpose of this fragment is to request permissions and, once granted, display the
    // audio fragment to the user.
    //
    [Android.App.Activity(Name = "org.tensorflow.lite.examples.audioclassification.fragments.PermissionsFragment")]
    class PermissionsFragment : Fragment,
        IActivityResultCallback
    {
        private static string[] PermissionsRequired = new string[] {
            Manifest.Permission.RecordAudio };

        private ActivityResultLauncher requestPermissionsLauncher;

        public void OnActivityResult(Object result)
        {
            var isGranted = result as Boolean;
            if (isGranted.BooleanValue())
            {
                Toast.MakeText(RequireContext(), "Permission request granted", ToastLength.Long).Show();
                NavigateToAudioFragment();
            }
            else
            {
                Toast.MakeText(RequireContext(), "Permission request denied", ToastLength.Long).Show();
            }
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            requestPermissionsLauncher =
                RegisterForActivityResult(new ActivityResultContracts.RequestPermission(), this);
        }

        public override void OnStart()
        {
            base.OnStart();

            if (ContextCompat.CheckSelfPermission(
                RequireContext(),
                Manifest.Permission.RecordAudio
            ) == Permission.Granted)
            {
                NavigateToAudioFragment();
            }
            else
            {
                requestPermissionsLauncher.Launch(
                    Manifest.Permission.RecordAudio);
            }
        }

        private void NavigateToAudioFragment()
        {
            Navigation.FindNavController(RequireActivity(), Resource.Id.fragment_container).Navigate(
                Resource.Id.action_permissions_to_audio);
        }

        // Convenience method used to check if all permissions required by this app are granted
        public static bool HasPermissions(Context context)
        {
            return PermissionsRequired.All(it => ContextCompat.CheckSelfPermission(context, it) == Permission.Granted);
        }
    }
}
