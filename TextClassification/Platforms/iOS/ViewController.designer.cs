// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using System.CodeDom.Compiler;
using Foundation;
using UIKit;

namespace TextClassification
{
    [Register ("ViewController")]
    partial class ViewController
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UITableView tableView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UITextField textField { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        NSLayoutConstraint textFieldBottomConstraint { get; set; }

        void ReleaseDesignerOutlets()
        {
            if (tableView != null) {
                tableView.Dispose();
                tableView = null;
            }

            if (textField != null) {
                textField.Dispose();
                textField = null;
            }

            if (textFieldBottomConstraint != null) {
                textFieldBottomConstraint.Dispose();
                textFieldBottomConstraint = null;
            }
        }
    }
}