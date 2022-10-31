using System;
using CoreFoundation;
using CoreGraphics;
using CoreVideo;
using Foundation;
using UIKit;
using static ImageClassification.Constants;
using static ImageClassification.InferenceViewController;

namespace ImageClassification
{
    public partial class ViewController : UIViewController,
        InferenceViewControllerDelegate,
        CameraFeedManagerDelegate
    {
        public ViewController(IntPtr handle) : base(handle) { }

        // MARK: Constants
        private float collapseTransitionThreshold = -40.0F;
        private float expandTransitionThreshold = 40.0F;
        private float delayBetweenInferencesMs = 1000.0F;

        // MARK: Instance Variables
        private DispatchQueue inferenceQueue = new DispatchQueue(label: "org.tensorflow.lite.inferencequeue");
        private double previousInferenceTimeMs = NSDate.DistantPast.SecondsSince1970 * 1000;
        private bool isInferenceQueueBusy = false;
        private float initialBottomSpace = 0.0F;
        private int threadCount = DefaultConstants.ThreadCount;
        private int _maxResults = DefaultConstants.MaxResults;
        private int maxResults
        {
            get
            {
                return _maxResults;
            }
            set
            {
                _maxResults = maxResults;
                if (inferenceViewController == null) return;
                bottomViewHeightConstraint.Constant = inferenceViewController.CollapsedHeight + 290;
                View.LayoutSubviews();
            }
        }
        private float scoreThreshold = DefaultConstants.ScoreThreshold;
        private ModelType model = ModelType.EfficientnetLite0;

        // MARK: Controllers that manage functionality
        // Handles all the camera related functionality
        private CameraFeedManager cameraCapture;

        // Handles all data preprocessing and makes calls to run inference through the
        // `ImageClassificationHelper`.
        private ImageClassificationHelper imageClassificationHelper =
            new ImageClassificationHelper(
                modelFileInfo: DefaultConstants.Model.ModelFileInfo(),
                threadCount: DefaultConstants.ThreadCount,
                resultCount: DefaultConstants.MaxResults,
                scoreThreshold: DefaultConstants.ScoreThreshold);

        // Handles the presenting of results on the screen
        private InferenceViewController inferenceViewController;

        // MARK: View Handling Methods
        override public void ViewDidLoad()
        {
            base.ViewDidLoad();

            if (imageClassificationHelper == null)
            {
                throw new Exception("Model initialization failed.");
            }

            cameraCapture = new CameraFeedManager(previewView: previewView);
            cameraCapture.Delegate = this;
            AddPanGesture();

            if (inferenceViewController == null) { return; }
            bottomViewHeightConstraint.Constant = inferenceViewController.CollapsedHeight + 290;
            View.LayoutSubviews();
        }

        override public void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            DispatchQueue.MainQueue.DispatchAfter(when: new DispatchTime(DispatchTime.Now, 10000000), (() =>
            {
                this.ChangeBottomViewState();
            }));
            if (ObjCRuntime.Runtime.Arch != ObjCRuntime.Arch.SIMULATOR)
                cameraCapture.CheckCameraConfigurationAndStartSession();
        }

        override public void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);
            if (ObjCRuntime.Runtime.Arch != ObjCRuntime.Arch.SIMULATOR)
                cameraCapture.StopSession();
        }

        override public UIStatusBarStyle PreferredStatusBarStyle() =>
            UIStatusBarStyle.LightContent;

        void PresentUnableToResumeSessionAlert()
        {
            var alert = UIAlertController.Create(
                title: "Unable to Resume Session",
                message: "There was an error while attempting to resume session.",
                preferredStyle: UIAlertControllerStyle.Alert
            );
            alert.AddAction(UIAlertAction.Create(title: "OK", style: UIAlertActionStyle.Default, handler: ((_) => { })));

            this.PresentViewController(alert, animated: true, (() => { }));
        }

        // MARK: Storyboard Segue Handlers
        override public void PrepareForSegue(UIStoryboardSegue segue, NSObject sender)
        {
            base.PrepareForSegue(segue: segue, sender: sender);

            if (segue.Identifier == "EMBED")
            {
                inferenceViewController = segue.DestinationViewController as InferenceViewController;
                inferenceViewController.MaxResults = maxResults;
                inferenceViewController.CurrentThreadCount = threadCount;
                inferenceViewController.Delegate = this;
            }
        }

        // MARK: InferenceViewControllerDelegate Methods
        public void ViewControllerAction(
            InferenceViewController viewController,
            InferenceViewController.Change action)
        {
            var isModelNeedsRefresh = false;
            if (action is Change.ChangeThreadCount)
            {
                var threadCount = (action as Change.ChangeThreadCount).threadCount;
                if (this.threadCount != threadCount)
                    isModelNeedsRefresh = true;
                this.threadCount = threadCount;
            }
            else if (action is Change.ChangeScoreThreshold)
            {
                var scoreThreshold = (action as Change.ChangeScoreThreshold).scoreThreshold;
                if (this.scoreThreshold != scoreThreshold)
                    isModelNeedsRefresh = true;
                this.scoreThreshold = scoreThreshold;
            }
            else if (action is Change.ChangeMaxResults)
            {
                var maxResults = (action as Change.ChangeMaxResults).maxResults;
                if (this.maxResults != maxResults)
                    isModelNeedsRefresh = true;
                this.maxResults = maxResults;
            }
            else if (action is Change.ChangeModel)
            {
                var model = (action as Change.ChangeModel).model;
                if (this.model != model)
                    isModelNeedsRefresh = true;
                this.model = model;
            }
            if (isModelNeedsRefresh)
            {
                imageClassificationHelper = new ImageClassificationHelper(
                    modelFileInfo: model.ModelFileInfo(),
                    threadCount: threadCount,
                    resultCount: maxResults,
                    scoreThreshold: scoreThreshold);
            }
        }

        public void DidOutput(CVPixelBuffer pixelBuffer)
        {
            // Make sure the model will not run too often, making the results changing quickly and hard to
            // read.
            var currentTimeMs = new NSDate().SecondsSince1970 * 1000;
            if (currentTimeMs - previousInferenceTimeMs < delayBetweenInferencesMs) return;
            previousInferenceTimeMs = currentTimeMs;

            // Drop this frame if the model is still busy classifying a previous frame.
            if (isInferenceQueueBusy)
            {
                pixelBuffer.Dispose(); pixelBuffer = null;
                return;
            }

            inferenceQueue.DispatchAsync(() =>
            {
                this.isInferenceQueueBusy = true;

                // Pass the pixel buffer to TensorFlow Lite to perform inference.
                var result = this.imageClassificationHelper?.Classify(pixelBuffer: pixelBuffer);
                var resolution = new CGSize(
                    width: pixelBuffer.Width, height: pixelBuffer.Height);
                pixelBuffer.Dispose(); pixelBuffer = null;

                this.isInferenceQueueBusy = false;

                // Display results by handing off to the InferenceViewController.
                DispatchQueue.MainQueue.DispatchAsync(() =>
                {
                    this.inferenceViewController.InferenceResult = result;
                    this.inferenceViewController.Resolution = resolution;
                    this.inferenceViewController.tableView.ReloadData();
                });
            });
        }

        // MARK: Session Handling Alerts
        public void SessionWasInterrupted(bool canResumeManually)
        {
            // Updates the UI when session is interupted.
            if (canResumeManually)
                this.resumeButton.Hidden = false;
            else
                this.cameraUnavailableLabel.Hidden = false;
        }

        public void SessionInterruptionEnded()
        {
            // Updates UI once session interruption has ended.
            if (!this.cameraUnavailableLabel.Hidden)
                this.cameraUnavailableLabel.Hidden = true;

            if (!this.resumeButton.Hidden)
                this.resumeButton.Hidden = true;
        }

        public void SessionRunTimeErrorOccurred()
        {
            // Handles session run time error by updating the UI and providing a button if session can be
            // manually resumed.
            this.resumeButton.Hidden = false;
            previewView.ShouldUseClipboardImage = true;
        }

        public void PresentCameraPermissionsDeniedAlert()
        {
            var alertController = UIAlertController.Create(
                title: "Camera Permissions Denied",
                message:
                "Camera permissions have been denied for this app. You can change this by going to Settings",
                preferredStyle: UIAlertControllerStyle.Alert);

            var cancelAction = UIAlertAction.Create(title: "Cancel", style: UIAlertActionStyle.Cancel, handler: ((_) => { }));
            var settingsAction = UIAlertAction.Create(title: "Settings", style: UIAlertActionStyle.Default, handler: ((action) =>
            {
                UIApplication.SharedApplication.OpenUrl(
                    NSUrl.FromString(s: UIApplication.OpenSettingsUrlString), new NSDictionary(), completion: ((_) => { }));
            }));
            alertController.AddAction(cancelAction);
            alertController.AddAction(settingsAction);

            PresentViewController(alertController, animated: true, completionHandler: (() => { }));

            previewView.ShouldUseClipboardImage = true;
        }

        public void PresentVideoConfigurationErrorAlert()
        {
            var alert = UIAlertController.Create(
            title: "Camera Configuration Failed", message: "There was an error while configuring camera.",
            preferredStyle: UIAlertControllerStyle.Alert);
            alert.AddAction(UIAlertAction.Create(title: "OK", style: UIAlertActionStyle.Default, handler: ((_) => { })));

            this.PresentViewController(alert, animated: true, completionHandler: (() => { }));
            previewView.ShouldUseClipboardImage = true;
        }

        // MARK: Bottom Sheet Interaction Methods
        //
        // This method adds a pan gesture to make the bottom sheet interactive.
        //
        private void AddPanGesture()
        {
            var panGesture = new UIPanGestureRecognizer(
                target: this, action: new ObjCRuntime.Selector("didPan:"));
            bottomSheetView.AddGestureRecognizer(panGesture);
        }

        //
        // Change whether bottom sheet should be in expanded or collapsed state.
        //
        private void ChangeBottomViewState()
        {
            if (inferenceViewController == null) return;

            if (bottomSheetViewBottomSpace.Constant == inferenceViewController.CollapsedHeight
                - bottomSheetView.Bounds.Size.Height)
            {
                bottomSheetViewBottomSpace.Constant = 0.0F;
            }
            else
            {
                bottomSheetViewBottomSpace.Constant =
                    inferenceViewController.CollapsedHeight - bottomSheetView.Bounds.Size.Height;
            }
            SetImageBasedOnBottomViewState();
        }

        //
        //  Set image of the bottom sheet icon based on whether it is expanded or collapsed
        //
        private void SetImageBasedOnBottomViewState()
        {
            if (bottomSheetViewBottomSpace.Constant == 0.0)
            {
                bottomSheetStateImageView.Image = UIImage.FromBundle(name: "down_icon");
            }
            else
            {
                bottomSheetStateImageView.Image = UIImage.FromBundle(name: "up_icon");
            }
        }

        //
        // This method responds to the user panning on the bottom sheet.
        //
        [Export("didPan:")]
        void DidPan(UIPanGestureRecognizer panGesture)
        {
            // Opens or closes the bottom sheet based on the user's interaction with the bottom sheet.
            var translation = panGesture.TranslationInView(View);

            switch (panGesture.State)
            {
                case UIGestureRecognizerState.Began:
                    initialBottomSpace = (float)bottomSheetViewBottomSpace.Constant;
                    TranslateBottomSheet(verticalTranslation: (float)translation.Y);
                    break;
                case UIGestureRecognizerState.Changed:
                    TranslateBottomSheet(verticalTranslation: (float)translation.Y);
                    break;
                case UIGestureRecognizerState.Cancelled:
                    SetBottomSheetLayout(bottomSpace: initialBottomSpace);
                    break;
                case UIGestureRecognizerState.Ended:
                    TranslateBottomSheetAtEndOfPan(verticalTranslation: (float)translation.Y);
                    SetImageBasedOnBottomViewState();
                    initialBottomSpace = 0.0F;
                    break;
                default:
                    break;
            }
        }

        //
        // This method sets bottom sheet translation while pan gesture state is continuously changing.
        //
        private void TranslateBottomSheet(float verticalTranslation)
        {
            var bottomSpace = initialBottomSpace - verticalTranslation;
            if (bottomSpace > 0.0 ||
                bottomSpace < inferenceViewController.CollapsedHeight
                - bottomSheetView.Bounds.Size.Height)
            {
                return;
            }
            SetBottomSheetLayout(bottomSpace: bottomSpace);
        }

        //
        // This method changes bottom sheet state to either fully expanded or closed at the end of pan.
        //
        private void TranslateBottomSheetAtEndOfPan(float verticalTranslation)
        {
            // Changes bottom sheet state to either fully open or closed at the end of pan.
            var bottomSpace = BottomSpaceAtEndOfPan(verticalTranslation: verticalTranslation);
            SetBottomSheetLayout(bottomSpace: bottomSpace);
        }

        //
        // Return the final state of the bottom sheet view (whether fully collapsed or expanded) that is to
        // be retained.
        //
        private float BottomSpaceAtEndOfPan(float verticalTranslation)
        {
            // Calculates whether to fully expand or collapse bottom sheet when pan gesture ends.
            var bottomSpace = initialBottomSpace - verticalTranslation;

            var height = 0.0F;
            if (initialBottomSpace == 0.0)
            {
                height = (float)bottomSheetView.Bounds.Size.Height;
            }
            else
            {
                height = inferenceViewController.CollapsedHeight;
            }

            var currentHeight = bottomSheetView.Bounds.Size.Height + bottomSpace;

            if (currentHeight - height <= collapseTransitionThreshold)
            {
                bottomSpace = (float)(inferenceViewController.CollapsedHeight - bottomSheetView.Bounds.Size.Height);
            }
            else if (currentHeight - height >= expandTransitionThreshold)
            {
                bottomSpace = 0.0F;
            }
            else
            {
                bottomSpace = initialBottomSpace;
            }

            return bottomSpace;
        }

        //
        // This method layouts the change of the bottom space of bottom sheet with respect to the view
        // managed by this controller.
        //
        void SetBottomSheetLayout(float bottomSpace)
        {
            View.SetNeedsLayout();
            bottomSheetViewBottomSpace.Constant = bottomSpace;
            View.SetNeedsLayout();
        }
    }

    public static class Constants
    {
        // Define default constants
        public struct DefaultConstants
        {
            public const int ThreadCount = 4;
            public const int MaxResults = 3;
            public const float ScoreThreshold = 0.2F;
            public const ModelType Model = ModelType.EfficientnetLite0;
        }

        // TFLite model types
        public enum ModelType
        {
            EfficientnetLite0,
            EfficientnetLite1,
            EfficientnetLite2,
            EfficientnetLite3,
            EfficientnetLite4
        }

        public static FileInfo ModelFileInfo(this ModelType modelType)
        {
            return modelType switch
            {
                ModelType.EfficientnetLite0 => new FileInfo { name = "efficientnet_lite0", extension = "tflite" },
                ModelType.EfficientnetLite1 => new FileInfo { name = "efficientnet_lite1", extension = "tflite" },
                ModelType.EfficientnetLite2 => new FileInfo { name = "efficientnet_lite2", extension = "tflite" },
                ModelType.EfficientnetLite3 => new FileInfo { name = "efficientnet_lite3", extension = "tflite" },
                ModelType.EfficientnetLite4 => new FileInfo { name = "efficientnet_lite4", extension = "tflite" },
                _ => new FileInfo()
            };
        }

        public static string Title(this ModelType modelType)
        {
            return modelType switch
            {
                ModelType.EfficientnetLite0 => "EfficientNet-Lite0",
                ModelType.EfficientnetLite1 => "EfficientNet-Lite1",
                ModelType.EfficientnetLite2 => "EfficientNet-Lite2",
                ModelType.EfficientnetLite3 => "EfficientNet-Lite3",
                ModelType.EfficientnetLite4 => "EfficientNet-Lite4",
                _ => ""
            };
        }
    }
}
