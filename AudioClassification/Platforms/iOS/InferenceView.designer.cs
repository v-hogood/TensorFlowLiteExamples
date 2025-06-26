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
    [Register("InferenceView")]
    partial class InferenceView
    {
        [Outlet]
        [GeneratedCode("iOS Designer", "1.0")]
        UILabel inferenceTimeLabel { get; set; }

        [Outlet]
        [GeneratedCode("iOS Designer", "1.0")]
        UILabel overlapLabel { get; set; }

        [Outlet]
        [GeneratedCode("iOS Designer", "1.0")]
        UILabel maxResultsLabel { get; set; }

        [Outlet]
        [GeneratedCode("iOS Designer", "1.0")]
        UILabel thresholdLabel { get; set; }

        [Outlet]
        [GeneratedCode("iOS Designer", "1.0")]
        UILabel threadsLabel { get; set; }

        [Outlet]
        [GeneratedCode("iOS Designer", "1.0")]
        UIStepper overlapStepper { get; set; }

        [Outlet]
        [GeneratedCode("iOS Designer", "1.0")]
        UIStepper maxResultsStepper { get; set; }

        [Outlet]
        [GeneratedCode("iOS Designer", "1.0")]
        UIStepper thresholdStepper { get; set; }

        [Outlet]
        [GeneratedCode("iOS Designer", "1.0")]
        UIStepper threadsStepper { get; set; }

        [Outlet]
        [GeneratedCode("iOS Designer", "1.0")]
        UISegmentedControl modelSegmentedControl { get; set; }

        [Outlet]
        [GeneratedCode("iOS Designer", "1.0")]
        NSLayoutConstraint showHiddenButtonLayoutConstraint { get; set; }

        [Outlet]
        [GeneratedCode("iOS Designer", "1.0")]
        UIButton showHiddenButton { get; set; }

        void ReleaseDesignerOutlets()
        {
            inferenceTimeLabel?.Dispose();
            inferenceTimeLabel = null; 

            overlapLabel?.Dispose();
            overlapLabel = null; 

            maxResultsLabel?.Dispose();
            maxResultsLabel = null; 

            thresholdLabel?.Dispose();
            thresholdLabel = null; 

            threadsLabel?.Dispose();
            threadsLabel = null; 

            overlapStepper?.Dispose();
            overlapStepper = null; 

            maxResultsStepper?.Dispose();
            maxResultsStepper = null; 

            thresholdStepper?.Dispose();
            thresholdStepper = null; 

            threadsStepper?.Dispose();
            threadsStepper = null; 

            modelSegmentedControl?.Dispose();
            modelSegmentedControl = null; 

            showHiddenButtonLayoutConstraint?.Dispose();
            showHiddenButtonLayoutConstraint = null; 

            showHiddenButton?.Dispose();
            showHiddenButton = null; 
        }
    }
}
