using System;
using AVFoundation;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace ImageClassification
{
    // Displays a preview of the image being processed. By default, this uses the device's camera frame,
    // but will use a still image copied from clipboard if `shouldUseClipboardImage` is set to true.
    [Register("PreviewView")]
    public class PreviewView : UIView
    {
        public PreviewView(IntPtr handle) : base(handle) { }

        private bool shouldUseClipboardImage = false;
        public bool ShouldUseClipboardImage
        {
            get { return shouldUseClipboardImage; }
            set
            {
                if (shouldUseClipboardImage = value)
                {
                    if (imageView.Superview == null)
                    {
                        AddSubview(imageView);
                        NSLayoutConstraint[] constraints = new NSLayoutConstraint[]
                        {
                            NSLayoutConstraint.Create(view1: imageView, attribute1: NSLayoutAttribute.Top,
                                                      relation: NSLayoutRelation.Equal,
                                                      view2: this, attribute2: NSLayoutAttribute.Top,
                                                      multiplier: 1, constant: 0),
                            NSLayoutConstraint.Create(view1: imageView, attribute1: NSLayoutAttribute.Leading,
                                                      relation: NSLayoutRelation.Equal,
                                                      view2: this, attribute2: NSLayoutAttribute.Leading,
                                                      multiplier: 1, constant: 0),
                            NSLayoutConstraint.Create(view1: imageView, attribute1: NSLayoutAttribute.Trailing,
                                                      relation: NSLayoutRelation.Equal,
                                                      view2: this, attribute2: NSLayoutAttribute.Trailing,
                                                      multiplier: 1, constant: 0),
                            NSLayoutConstraint.Create(view1: imageView, attribute1: NSLayoutAttribute.Bottom,
                                                      relation: NSLayoutRelation.Equal,
                                                      view2: this, attribute2: NSLayoutAttribute.Bottom,
                                                      multiplier: 1, constant: 0)
                        };
                        AddConstraints(constraints);
                    }
                    else
                    {
                        imageView.RemoveFromSuperview();
                    }
                }
            }
        }

        private UIImageView imageView = new UIImageView()
        {
            ContentMode = UIViewContentMode.ScaleAspectFill,
            TranslatesAutoresizingMaskIntoConstraints = false
        };

        UIImage Image => imageView.Image;

        public AVCaptureVideoPreviewLayer PreviewLayer
        {
            get
            {
                var previewLayer = Layer as AVCaptureVideoPreviewLayer;
                if (previewLayer == null)
                    throw new Exception("Layer expected is of type VideoPreviewLayer");
                return previewLayer;
            }
        }

        public AVCaptureSession Session
        {
            get { return PreviewLayer.Session; }
            set { PreviewLayer.Session = value; }
        }

        [Export("layerClass")]
        public static Class LayerClass => new Class(typeof(AVCaptureVideoPreviewLayer));
    }
}
