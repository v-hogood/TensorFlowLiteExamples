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
    [Register("InferenceViewController")]
    partial class InferenceViewController
    {
        // MARK: Storyboard Outlets

        [Outlet]
        [GeneratedCode("iOS Designer", "1.0")]
        public UITableView tableView { get; set; }

        [Outlet]
        [GeneratedCode("iOS Designer", "1.0")]
        UIStepper threadStepper { get; set; }

        [Outlet]
        [GeneratedCode("iOS Designer", "1.0")]
        UILabel threadValueLabel { get; set; }

        [Outlet]
        [GeneratedCode("iOS Designer", "1.0")]
        UIStepper thresholdStepper { get; set; }

        [Outlet]
        [GeneratedCode("iOS Designer", "1.0")]
        UILabel thresholdValueLabel { get; set; }

        [Outlet]
        [GeneratedCode("iOS Designer", "1.0")]
        UIStepper maxResultStepper { get; set; }

        [Outlet]
        [GeneratedCode("iOS Designer", "1.0")]
        UILabel maxResultLabel { get; set; }

        [Outlet]
        [GeneratedCode("iOS Designer", "1.0")]
        UITextField modelTextField { get; set; }

        void ReleaseDesignerOutlets()
        {
            tableView?.Dispose();
            tableView = null;

            threadStepper?.Dispose();
            threadStepper = null;

            threadValueLabel?.Dispose();
            threadValueLabel = null;

            thresholdStepper?.Dispose();
            thresholdStepper = null;

            thresholdValueLabel?.Dispose();
            thresholdValueLabel = null;

            maxResultStepper?.Dispose();
            maxResultStepper = null;

            maxResultLabel?.Dispose();
            maxResultLabel = null;

            modelTextField?.Dispose();
            modelTextField = null;
        }
    }

    [Register("InfoCell")]
    public class InfoCell : UITableViewCell
    {
        public InfoCell(IntPtr handle) : base(handle) { }

        [Outlet]
        [GeneratedCode("iOS Designer", "1.0")]
        public UILabel fieldNameLabel { get; set; }

        [Outlet]
        [GeneratedCode("iOS Designer", "1.0")]
        public UILabel infoLabel { get; set; }

        void ReleaseDesignerOutlets()
        {
            fieldNameLabel?.Dispose();
            fieldNameLabel = null;

            infoLabel?.Dispose();
            infoLabel = null;
        }
    }
}
