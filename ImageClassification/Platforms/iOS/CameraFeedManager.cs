using System.Diagnostics;
using AVFoundation;
using CoreFoundation;
using CoreMedia;
using CoreVideo;
using Foundation;

namespace ImageClassification
{
    // MARK: CameraFeedManagerDelegate Declaration
    public interface CameraFeedManagerDelegate
    {
        //
        // This method delivers the pixel buffer of the current frame seen by the device's camera.
        //
        public void DidOutput(CVPixelBuffer pixelBuffer);

        //
        // This method initimates that the camera permissions have been denied.
        //
        public void PresentCameraPermissionsDeniedAlert();

        //
        // This method initimates that there was an error in video configurtion.
        //
        public void PresentVideoConfigurationErrorAlert();

        //
        // This method initimates that a session runtime error occured.
        //
        public void SessionRunTimeErrorOccurred();

        //
        // This method initimates that the session was interrupted.
        //
        public void SessionWasInterrupted(bool canResumeManually);

        //
        // This method initimates that the session interruption has ended.
        //
        public void SessionInterruptionEnded();
    }

    //
    // This enum holds the state of the camera initialization.
    //
    enum CameraConfiguration
    {
          Success,
          Failed,
          PermissionDenied
    }

    //
    // This class manages all camera related functionality
    //
    class CameraFeedManager : NSObject,
        IAVCaptureVideoDataOutputSampleBufferDelegate
    {
        // MARK: Camera Related Instance Variables
        private AVCaptureSession session = new AVCaptureSession();
        private PreviewView previewView;
        private DispatchQueue sessionQueue = new DispatchQueue(label: "sessionQueue");
        private CameraConfiguration cameraConfiguration = CameraConfiguration.Failed;
        private AVCaptureVideoDataOutput videoDataOutput = new AVCaptureVideoDataOutput();
        private bool isSessionRunning = false;

        // MARK: CameraFeedManagerDelegate
        public CameraFeedManagerDelegate Delegate;

        // MARK: Initializer
        public CameraFeedManager(PreviewView previewView)
        {
            this.previewView = previewView;

            // Initializes the session
            session.SessionPreset = AVCaptureSession.PresetHigh;
            this.previewView.Session = session;
            this.previewView.PreviewLayer.VideoGravity = AVLayerVideoGravity.ResizeAspectFill;
            this.AttemptToConfigureSession();
        }

        // MARK: Session Start and End methods

        //
        // This method starts an AVCaptureSession based on whether the camera configuration was successful.
        //
        public void CheckCameraConfigurationAndStartSession()
        {
            sessionQueue.DispatchAsync(() =>
            {
                switch (this.cameraConfiguration)
                {
                    case CameraConfiguration.Success:
                        this.AddObservers();
                        this.StartSession();
                        break;
                    case CameraConfiguration.Failed:
                        DispatchQueue.MainQueue.DispatchAsync(() =>
                        {
                            this.Delegate?.PresentVideoConfigurationErrorAlert();
                        });
                        break;
                    case CameraConfiguration.PermissionDenied:
                        DispatchQueue.MainQueue.DispatchAsync(() =>
                        {
                            this.Delegate?.PresentCameraPermissionsDeniedAlert();
                        });
                        break;
                }
            });
        }

        //
        // This method stops a running an AVCaptureSession.
        //
        public void StopSession()
        {
            this.RemoveObservers();
            sessionQueue.DispatchAsync(() =>
            {
                if (this.session.Running)
                {
                    this.session.StopRunning();
                    this.isSessionRunning = this.session.Running;
                }
            });
        }

        //
        // This method resumes an interrupted AVCaptureSession.
        //
        void ResumeInterruptedSession(Action<bool> completion)
        {
            sessionQueue.DispatchAsync(() =>
            {
                this.StartSession();

                DispatchQueue.MainQueue.DispatchAsync(() =>
                {
                    completion(this.isSessionRunning);
                });
            });
        }

        //
        // This method starts the AVCaptureSession
        //
        private void StartSession()
        {
            this.session.StartRunning();
            this.isSessionRunning = this.session.Running;
        }

        // MARK: Session Configuration Methods.
        //
        // This method requests for camera permissions and handles the configuration of the session and stores the result of configuration.
        //
        private void AttemptToConfigureSession()
        {
            switch (AVCaptureDevice.GetAuthorizationStatus(AVAuthorizationMediaType.Video))
            {
                case AVAuthorizationStatus.Authorized:
                    this.cameraConfiguration = CameraConfiguration.Success;
                    break;
                case AVAuthorizationStatus.NotDetermined:
                    this.sessionQueue.Suspend();
                    this.RequestCameraAccess(completion: ((granted) =>
                    {
                        if (granted)
                            this.sessionQueue.Resume();
                    }));
                    break;
                case AVAuthorizationStatus.Denied:
                    this.cameraConfiguration = CameraConfiguration.PermissionDenied;
                    break;
                default:
                    break;
            }

            this.sessionQueue.DispatchAsync(() =>
            {
                this.ConfigureSession();
            });
        }

        //
        // This method requests for camera permissions.
        //
        private void RequestCameraAccess(Action<bool> completion)
        {
            AVCaptureDevice.RequestAccessForMediaType(mediaType: AVAuthorizationMediaType.Video,
                completion: ((granted) =>
            {
                if (!granted)
                {
                    this.cameraConfiguration = CameraConfiguration.PermissionDenied;
                }
                else
                {
                    this.cameraConfiguration = CameraConfiguration.Success;
                }
                completion(granted);
            }));
        }

        //
        // This method handles all the steps to configure an AVCaptureSession.
        //
        private void ConfigureSession()
        {
            if (cameraConfiguration != CameraConfiguration.Success)
                return;

            session.BeginConfiguration();

            // Tries to add an AVCaptureDeviceInput.
            if (!AddVideoDeviceInput())
            {
                this.session.CommitConfiguration();
                this.cameraConfiguration = CameraConfiguration.Failed;
                return;
            }

            // Tries to add an AVCaptureVideoDataOutput.
            if (!AddVideoDataOutput())
            {
                this.session.CommitConfiguration();
                this.cameraConfiguration = CameraConfiguration.Failed;
                return;
            }

            session.CommitConfiguration();
            this.cameraConfiguration = CameraConfiguration.Success;
        }

        //
        // This method tries to an AVCaptureDeviceInput to the current AVCaptureSession.
        // 
        private bool AddVideoDeviceInput()
        {
            //
            // Tries to get the default back camera.
            //
            var camera = AVCaptureDevice.GetDefaultDevice(AVCaptureDeviceType.BuiltInWideAngleCamera, AVMediaTypes.Video, AVCaptureDevicePosition.Back);
            if (camera == null)
                return false;

            NSError error;
            var videoDeviceInput = new AVCaptureDeviceInput(device: camera, error: out error);
            if (error != null)
                throw new Exception("Cannot create video device input");
            if (session.CanAddInput(videoDeviceInput))
            {
                session.AddInput(videoDeviceInput);
                return true;
            }
            else
            {
                return false;
            }
        }

        //
        // This method tries to an AVCaptureVideoDataOutput to the current AVCaptureSession.
        // 
        private bool AddVideoDataOutput()
        {
            var sampleBufferQueue = new DispatchQueue(label: "sampleBufferQueue");
            videoDataOutput.SetSampleBufferDelegate(this, sampleBufferCallbackQueue: sampleBufferQueue);
            videoDataOutput.AlwaysDiscardsLateVideoFrames = true;
            videoDataOutput.WeakVideoSettings = new CVPixelBufferAttributes { PixelFormatType = CVPixelFormatType.CV32BGRA }.Dictionary;

            if (session.CanAddOutput(videoDataOutput))
            {
                session.AddOutput(videoDataOutput);
                videoDataOutput.ConnectionFromMediaType(new NSString("vide")/*AVMediaType.Video*/).VideoOrientation = AVCaptureVideoOrientation.Portrait;
                return true;
            }
            return false;
        }

        // MARK: Notification Observer Handling
        private void AddObservers()
        {
            NSNotificationCenter.DefaultCenter.AddObserver(this, aSelector: new ObjCRuntime.Selector("sessionRuntimeErrorOccured:"), aName: AVCaptureSession.RuntimeErrorNotification, anObject: session);
            NSNotificationCenter.DefaultCenter.AddObserver(this, aSelector: new ObjCRuntime.Selector("sessionWasInterrupted(notification:)):"), aName: AVCaptureSession.WasInterruptedNotification, anObject: session);
            NSNotificationCenter.DefaultCenter.AddObserver(this, aSelector: new ObjCRuntime.Selector("sessionInterruptionEnded:"), aName: AVCaptureSession.InterruptionEndedNotification, anObject: session);
        }

        private void RemoveObservers()
        {
            NSNotificationCenter.DefaultCenter.RemoveObserver(this, aName: AVCaptureSession.RuntimeErrorNotification, anObject: session);
            NSNotificationCenter.DefaultCenter.RemoveObserver(this, aName: AVCaptureSession.WasInterruptedNotification, anObject: session);
            NSNotificationCenter.DefaultCenter.RemoveObserver(this, aName: AVCaptureSession.InterruptionEndedNotification, anObject: session);
        }

        // MARK: Notification Observers
        [Export("sessionWasInterrupted:")]
        void sessionWasInterrupted(NSNotification notification)
        {
            var userInfoValue = notification.UserInfo?.ValueForKey(AVCaptureSession.InterruptionReasonKey) as NSNumber;
            var reasonIntegerValue = userInfoValue.Int64Value;
            var reason = (AVCaptureSessionInterruptionReason)reasonIntegerValue;
            Debug.Print("Capture session was interrupted with reason " + reason.ToString());

            var canResumeManually = false;
            if (reason == AVCaptureSessionInterruptionReason.VideoDeviceInUseByAnotherClient)
                canResumeManually = true;
            else if (reason == AVCaptureSessionInterruptionReason.VideoDeviceNotAvailableWithMultipleForegroundApps)
                canResumeManually = false;

            this.Delegate?.SessionWasInterrupted(canResumeManually: canResumeManually);
        }

        [Export("sessionInterruptionEnded:")]
        void sessionInterruptionEnded(NSNotification notification)
        {
            this.Delegate?.SessionInterruptionEnded();
        }

        [Export("sessionRuntimeErrorOccurred:")]
        void sessionRuntimeErrorOccurred(NSNotification notification)
        {
            var value = notification.UserInfo?.ValueForKey(AVCaptureSession.ErrorKey) as NSNumber;
            if (value == null) return;
            var error = (AVError)value.Int64Value;

            Debug.Print("Capture session runtime error: " + error.ToString());

            if (error == AVError.MediaServicesWereReset)
            {
                sessionQueue.DispatchAsync(() =>
                {
                    if (this.isSessionRunning)
                    {
                        this.StartSession();
                    }
                    else
                    {
                        DispatchQueue.MainQueue.DispatchAsync(() =>
                        {
                            this.Delegate?.SessionRunTimeErrorOccurred();
                        });
                    }
                });
            }
            else
            {
                this.Delegate?.SessionRunTimeErrorOccurred();
            }
        }

        //
        // AVCaptureVideoDataOutputSampleBufferDelegate
        //
        //
        // This method delegates the CVPixelBuffer of the frame seen by the camera currently.
        //
        [Export("captureOutput:didOutputSampleBuffer:fromConnection:")]
        void DidOutputSampleBuffer(AVCaptureOutput output, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
        {
            // Converts the CMSampleBuffer to a CVPixelBuffer.
            var pixelBuffer = sampleBuffer.GetImageBuffer() as CVPixelBuffer;
            sampleBuffer.Dispose(); sampleBuffer = null;
            if (pixelBuffer == null) return;

            // Delegates the pixel buffer to the ViewController.
            Delegate?.DidOutput(pixelBuffer: pixelBuffer);
        }

        [Export("captureOutput:didDropSampleBuffer:fromConnection:")]
        public void DidDropSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
        {
            CMAttachmentMode mode = 0;
            var reason = CMAttachmentBearer.GetAttachment<NSString>(target: sampleBuffer, key: CMSampleBufferAttachmentKey.DroppedFrameReason, attachmentModeOut: out mode);
            Debug.Print("drop frame reason: " + reason);

            sampleBuffer.Dispose(); sampleBuffer = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
