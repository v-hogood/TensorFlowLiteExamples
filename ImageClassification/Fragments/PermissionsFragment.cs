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

namespace ImageClassification
{
    [Android.App.Activity(Name = "org.tensorflow.lite.examples.imageclassification.fragments.PermissionsFragment")]
    class PermissionsFragment : Fragment,
        IActivityResultCallback
    {
        private static string[] PermissionsRequired = new string[] {
            Manifest.Permission.Camera };

        private ActivityResultLauncher requestPermissionsLauncher;

        public void OnActivityResult(Object result)
        {
            var isGranted = result as Boolean;
            if (isGranted.BooleanValue())
            {
                Toast.MakeText(RequireContext(), "Permission request granted", ToastLength.Long).Show();
                NavigateToCamera();
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
                Manifest.Permission.Camera
            ) == Permission.Granted)
            {
                NavigateToCamera();
            }
            else
            {
                requestPermissionsLauncher.Launch(
                    Manifest.Permission.Camera);
            }
        }

        private void NavigateToCamera()
        {
            Navigation.FindNavController(RequireActivity(), Resource.Id.fragment_container).Navigate(
                Resource.Id.action_permissions_to_camera);
        }

        // Convenience method used to check if all permissions required by this app are granted
        public static bool HasPermissions(Context context)
        {
            return PermissionsRequired.All(it => ContextCompat.CheckSelfPermission(context, it) == Permission.Granted);
        }
    }
}
