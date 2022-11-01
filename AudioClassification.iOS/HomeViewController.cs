using System;
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
        IUITableViewDataSource, IUITableViewDelegate,
        AudioClassificationHelperDelegate,
        InferenceViewDelegate
    {
        public HomeViewController(IntPtr handle) : base(handle) { }

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
                    throw new Exception("Microphone permission check returned unexpected result.");
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
            if (cell == null) throw new Exception();
            cell.SetData(inferenceResults[indexPath.Row]);
            return cell;
        }

        public nint RowsInSection(UITableView tableView, nint section)
        {
            return inferenceResults == null ? 0 : inferenceResults.Length;
        }

        void AudioClassificationHelperDelegate.AudioClassificationHelper(AudioClassificationHelper helper, Result result)
        {
            inferenceResults = result.Categories;
            tableView.ReloadData();
            inferenceView.SetInferenceTime(result.InferenceTime);
        }

        void AudioClassificationHelperDelegate.AudioClassificationHelper(AudioClassificationHelper helper, NSError error)
        {
            DispatchQueue.MainQueue.DispatchAsync(() =>
            {
                var errorMessage =
                    "An error occured while running audio classification: " + error.LocalizedDescription;
                var alert = UIAlertController.Create(
                    title: "Error", message: errorMessage, preferredStyle: UIAlertControllerStyle.Alert);
                alert.AddAction(UIAlertAction.Create(title: "OK", style: UIAlertActionStyle.Default, handler: ((_) => { })));
                PresentViewController(alert, animated: true, completionHandler: (() => { }));
            });
        }

        void InferenceViewDelegate.View(InferenceView view, InferenceView.Change action)
        {
            if (action is Change.ChangeModel)
                this.modelType = (action as Change.ChangeModel).modelType;
            else if (action is Change.ChangeOverlap)
                this.overLap = (action as Change.ChangeOverlap).overlap;
            else if (action is Change.ChangeMaxResults)
                this.maxResults = (action as Change.ChangeMaxResults).maxResults;
            else if (action is Change.ChangeScoreThreshold)
                this.threshold = (action as Change.ChangeScoreThreshold).threshold;
            else if (action is Change.ChangeThreadCount)
                this.threadCount = (action as Change.ChangeThreadCount).threadCount;

            // Restart the audio classifier as the config as changed.
            RestartClassifier();
        }
    }
}
