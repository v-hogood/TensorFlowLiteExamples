using System;
using System.Linq;
using CoreFoundation;
using Foundation;
using UIKit;

namespace SoundClassification
{
    partial class ViewController : UIViewController,
        IUITableViewDataSource, IUITableViewDelegate,
        IAudioInputManagerDelegate,
        ISoundClassifierDelegate
    {
        public ViewController(IntPtr handle) : base(handle) { }

        // MARK: - Variables
        private AudioInputManager audioInputManager;
        private SoundClassifier soundClassifier;
        private int bufferSize = 0;
        private float[] probabilities;

        // MARK: - View controller lifecycle methods

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            tableView.DataSource = this;
            tableView.BackgroundColor = UIColor.White;
            tableView.TableFooterView = new UIView();

            soundClassifier = new SoundClassifier(modelFileName: "sound_classification", Delegate: this);

            StartAudioRecognition();
        }

        // MARK: - Private Methods

        /// Initializes the AudioInputManager and starts recognizing on the output buffers.
        private void StartAudioRecognition()
        {
            audioInputManager = new AudioInputManager(sampleRate: soundClassifier.SampleRate);
            audioInputManager.Delegate = this;

            bufferSize = audioInputManager.bufferSize;

            audioInputManager.CheckPermissionsAndStartTappingMicrophone();
        }

        private void RunModel(short[] inputBuffer)
        {
            soundClassifier.Start(inputBuffer: inputBuffer);
        }

        void IAudioInputManagerDelegate.AudioInputManagerDidFailToAchievePermission(AudioInputManager audioInputManager)
        {
            var alertController = UIAlertController.Create(
                title: "Microphone Permissions Denied",
                message: "Microphone permissions have been denied for this app. You can change this by going to Settings",
                preferredStyle: UIAlertControllerStyle.Alert
            );

            var cancelAction = UIAlertAction.Create(title: "Cancel", style: UIAlertActionStyle.Cancel, handler: null);
            var settingsAction = UIAlertAction.Create(title: "Settings", style: UIAlertActionStyle.Default, handler: (_) =>
            {
                UIApplication.SharedApplication.OpenUrl(
                    url: new NSUrl(path: UIApplication.OpenSettingsUrlString, isDir: true),
                    options: new UIApplicationOpenUrlOptions(),
                    completion: null
                );
            });
            alertController.AddAction(cancelAction);
            alertController.AddAction(settingsAction);

            PresentViewController(alertController, animated: true, completionHandler: null);
        }

        void IAudioInputManagerDelegate.AudioInputManager(
            AudioInputManager audioInputManager,
            short[] channelData)
        {
            var sampleRate = soundClassifier.SampleRate;
            this.RunModel(inputBuffer: channelData[0..sampleRate]);
            this.RunModel(inputBuffer: channelData[sampleRate..bufferSize]);
        }

        void ISoundClassifierDelegate.SoundClassifier(
            SoundClassifier soundClassifier,
            float[] probabilities)
        {
            this.probabilities = probabilities;
            DispatchQueue.MainQueue.DispatchAsync(() =>
            {
                this.tableView.ReloadData();
            });
        }

        // MARK: - UITableViewDataSource
        public nint RowsInSection(UITableView tableView, nint section)
        {
            return probabilities == null ? 0 : probabilities.Length;
        }

        public UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var cell = tableView.DequeueReusableCell(
                reuseIdentifier: "probabilityCell",
                indexPath: indexPath
            ) as ProbabilityTableViewCell;
            if (cell == null) return new UITableViewCell();

            cell.label.Text = soundClassifier.LabelNames[indexPath.Row];
            UIView.Animate(duration: 0.4, () =>
                cell.progressView.SetProgress(this.probabilities[indexPath.Row], animated: true)
            );
            return cell;
        }
    }
}
