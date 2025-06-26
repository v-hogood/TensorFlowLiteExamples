// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using System.CodeDom.Compiler;
using Foundation;
using UIKit;

namespace AudioClassification
{
    [Register ("ResultTableViewCell")]
    partial class ResultTableViewCell
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UILabel nameLabel { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        NSLayoutConstraint scoreWidthLayoutConstraint { get; set; }

        void ReleaseDesignerOutlets()
        {
            nameLabel?.Dispose();
            nameLabel = null;

            scoreWidthLayoutConstraint?.Dispose();
            scoreWidthLayoutConstraint = null;
        }
    }
}
