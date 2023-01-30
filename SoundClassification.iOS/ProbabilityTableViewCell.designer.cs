// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using System;
using System.CodeDom.Compiler;
using Foundation;
using UIKit;

namespace SoundClassification
{
    [Register ("ProbabilityTableViewCell")]
    public class ProbabilityTableViewCell : UITableViewCell
    {
        public ProbabilityTableViewCell(IntPtr handle) : base(handle) { }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        public UILabel label { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        public UIProgressView progressView { get; set; }

        void ReleaseDesignerOutlets()
        {
            label?.Dispose();
            label = null;

            progressView?.Dispose();
            progressView = null;
        }
    }
}
