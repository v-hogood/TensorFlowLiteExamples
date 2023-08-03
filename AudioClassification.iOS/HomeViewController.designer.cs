// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using System.CodeDom.Compiler;

namespace AudioClassification
{
    [Register("HomeViewController")]
    partial class HomeViewController
    {
        [Outlet]
        [GeneratedCode("iOS Designer", "1.0")]
        UITableView tableView { get; set; }

        [Outlet]
        [GeneratedCode("iOS Designer", "1.0")]
        InferenceView inferenceView { get; set; }

        void ReleaseDesignerOutlets()
        {
            tableView?.Dispose();
            tableView = null;

            inferenceView?.Dispose();
            inferenceView = null;
        }
    }
}