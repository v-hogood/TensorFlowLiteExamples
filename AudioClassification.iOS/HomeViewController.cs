using AVFoundation;
using CoreFoundation;
using Foundation;
using UIKit;
using TensorFlowLiteTaskAudio;
using static AudioClassification.InferenceView;

namespace AudioClassification
{
    // The sample app's home screen.
    public partial class HomeViewController : UIViewController,
        IUITableViewDataSource,
        AudioClassificationHelperDelegate,
        InferenceViewDelegate
    {
        public HomeViewController(System.IntPtr handle) : base(handle) { }

        // MARK: - Variables

        private TFLCategory[] inferenceResults;
        private ModelType modelType = new ModelType.Yamnet();
        private double overLap = 0.5;
        private int maxResults = 3;
        private float threshold = 0.0f;
        private int threadCount = 2;

        private AudioClassificationHelper audioClassificationHelper;

        // MARK: - View controller lifecycle methods
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            inferenceView.SetDefault(
                model: modelType, overlap: overLap, maxResult: maxResults, threshold: threshold,
                threads: threadCount);
            inferenceView.Delegate = this;
            StartAudioClassification();
        }

        // MARK: - Private Methods
        /// Request permission and start audio classification if granted.
        private void StartAudioClassification()
        {
            AVAudioSession.SharedInstance().RequestRecordPermission((granted) =>
            {
                if (granted)
                {
                    DispatchQueue.MainQueue.DispatchAsync(() =>
                    {
                        this.RestartClassifier();
                    });
                }
                else
                {
                    this.CheckPermissions();
                }
            });
        }

        /// Check permission and show error if user denied permission.
        private void CheckPermissions()
        {
            switch(AVAudioSession.SharedInstance().RecordPermission)
            {
                case AVAudioSessionRecordPermission.Granted:
                    StartAudioClassification();
                    break;
                case AVAudioSessionRecordPermission.Denied:
                    ShowPermissionsErrorAlert();
                    break;
                default:
                    throw new System.Exception("Microphone permission check returned unexpected result.");
            }
        }

        /// Start a new audio classification routine.
        private void RestartClassifier()
        {
            // Stop the existing classifier if one is running.
            audioClassificationHelper?.StopClassifier();

            // Create a new classifier instance.
            audioClassificationHelper = new AudioClassificationHelper(
                modelType: modelType,
                threadCount: threadCount,
                scoreThreshold: threshold,
                maxResults: maxResults);

            // Start the new classification routine.
            if (audioClassificationHelper != null)
                audioClassificationHelper.Delegate = this;
            audioClassificationHelper?.StartClassifier(overlap: overLap);
        }

        private void ShowPermissionsErrorAlert()
        {
            var alertController = UIAlertController.Create(
                title: "Microphone Permissions Denied",
                message:
                    "Microphone permissions have been denied for this app. You can change this by going to Settings",
                    preferredStyle: UIAlertControllerStyle.Alert
                );

            var cancelAction = UIAlertAction.Create(title: "Cancel", style: UIAlertActionStyle.Cancel, handler: null);
            var settingsAction = UIAlertAction.Create(title: "Settings", style: UIAlertActionStyle.Default, ((_) =>
                {
                    UIApplication.SharedApplication.OpenUrl(
                        new NSUrl(UIApplication.OpenSettingsUrlString));
                }));
            alertController.AddAction(cancelAction);
            alertController.AddAction(settingsAction);

            PresentViewController(alertController, animated: true, completionHandler: (() => { }));
        }

        public class ModelType : NSObject
        {
            public sealed class Yamnet : ModelType { public string modelType = "YAMNet"; }
            public sealed class SpeechCommandModel : ModelType { public string modelType = "Speech Command"; }

            public string FileName
            {
                get
                {
                    if (this is Yamnet)
                        return "yamnet";
                    if (this is SpeechCommandModel)
                        return "speech_commands";
                    return null;
                }
            }
        }

        public UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var cell = tableView.DequeueReusableCell(reuseIdentifier: "ResultCell", indexPath) as ResultTableViewCell;
            if (cell == null) throw new System.Exception();
            cell.SetData(inferenceResults[indexPath.Row]);
            return cell;
        }

        public System.nint RowsInSection(UITableView tableView, System.nint section)
        {
            return inferenceResults.Length;
        }

        void AudioClassificationHelperDelegate.AudioClassificationHelper(AudioClassificationHelper helper, Result result)
        {
            inferenceResults = result.Categories;
            tableView.ReloadData();
            inferenceView.SetInferenceTime(result.InferenceTime);
        }

        void AudioClassificationHelperDelegate.AudioClassificationHelper(AudioClassificationHelper helper, NSError error)
        {
            var errorMessage =
                "An error occured while running audio classification: " + error.LocalizedDescription;
            var alert = UIAlertController.Create(
                title: "Error", message: errorMessage, preferredStyle: UIAlertControllerStyle.Alert);
            alert.AddAction(UIAlertAction.Create(title: "OK", style: UIAlertActionStyle.Default, handler: ((_) => { })));
            PresentViewController(alert, animated: true, completionHandler: (() => { }));
        }

        void InferenceViewDelegate.View(InferenceView view, InferenceView.Action action)
        {
            if (action is Action.ChangeModel)
                this.modelType = (action as Action.ChangeModel).modelType;
            else if (action is Action.ChangeOverlap)
                this.overLap = (action as Action.ChangeOverlap).overlap;
            else if (action is Action.ChangeMaxResults)
                this.maxResults = (action as Action.ChangeMaxResults).maxResults;
            else if (action is Action.ChangeScoreThreshold)
                this.threshold = (action as Action.ChangeScoreThreshold).threshold;
            else if (action is Action.ChangeThreadCount)
                this.threadCount = (action as Action.ChangeThreadCount).threadCount;

            // Restart the audio classifier as the config as changed.
            RestartClassifier();
        }
    }
}
