// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using System.CodeDom.Compiler;
using Foundation;
using UIKit;

namespace ImageClassification
{
    [Register ("ViewController")]
    partial class ViewController
    {
        // MARK: Storyboards Connections

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        PreviewView previewView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UILabel cameraUnavailableLabel { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIButton resumeButton { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIView bottomSheetView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        NSLayoutConstraint bottomSheetViewBottomSpace { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIImageView  bottomSheetStateImageView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        NSLayoutConstraint bottomViewHeightConstraint { get; set; }

        void ReleaseDesignerOutlets()
        {
            previewView?.Dispose();
            previewView = null;

            cameraUnavailableLabel?.Dispose();
            cameraUnavailableLabel = null;

            resumeButton?.Dispose();
            resumeButton = null;

            bottomSheetView?.Dispose();
            bottomSheetView = null;

            bottomSheetViewBottomSpace?.Dispose();
            bottomSheetViewBottomSpace = null;

            bottomSheetStateImageView?.Dispose();
            bottomSheetStateImageView = null;

            bottomViewHeightConstraint?.Dispose();
            bottomViewHeightConstraint = null;
        }
    }
}
