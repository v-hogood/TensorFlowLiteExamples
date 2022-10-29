using Foundation;
using UIKit;
using static AudioClassification.HomeViewController;

namespace AudioClassification
{
    public interface InferenceViewDelegate
    {
        // This method is called when the user changes the value to update model used for inference.
        void View(InferenceView view, InferenceView.Action action);
    }

    // View to allows users changing the inference configs.
    public partial class InferenceView : UIView
    {
        public InferenceView(System.IntPtr handle) : base(handle) { }

        public class Action : NSObject
        {
            public sealed class ChangeModel : Action { public ModelType modelType; };
            public sealed class ChangeOverlap : Action { public double overlap; };
            public sealed class ChangeMaxResults : Action { public int maxResults; };
            public sealed class ChangeScoreThreshold : Action { public float threshold;  };
            public sealed class ChangeThreadCount : Action { public int threadCount; };
        }

        public InferenceViewDelegate Delegate;

        ///Set the default settings.
        public void SetDefault(ModelType model, double overlap, int maxResult, float threshold, int threads)
        {
            modelSegmentedControl.SelectedSegment = model is ModelType.Yamnet ? 0 : 1;
            overlapLabel.Text = (int)(overlap * 100) + "%";
            overlapStepper.Value = overlap;
            maxResultsLabel.Text = maxResult + "";
            maxResultsStepper.Value = (double)maxResult;
            thresholdLabel.Text = string.Format("%.1f", threshold);
            thresholdStepper.Value = (double)threshold;
            threadsLabel.Text = threads + "";
            threadsStepper.Value = (double)threads;
        }

        public void SetInferenceTime(double inferenceTime)
        {
            inferenceTimeLabel.Text = (int)(inferenceTime * 1000) + "ms";
        }

        [Export("modelSegmentedValueChanged:")]
        public void modelSegmentedValueChanged(UISegmentedControl sender)
        {
            ModelType modelSelect;
            if (sender.SelectedSegment == 0)
                modelSelect = new ModelType.Yamnet();
            else
                modelSelect = new ModelType.SpeechCommandModel();
            Delegate?.View(this, action: new Action.ChangeModel { modelType = modelSelect });
        }

        [Export("overlapStepperValueChanged:")]
        public void overlapStepperValueChanged(UIStepper sender)
        {
            overlapLabel.Text = System.String.Format("%.0f", sender.Value * 100) + "%";
            Delegate?.View(this, action: new Action.ChangeOverlap { overlap = sender.Value });
        }

        [Export("maxResultsStepperValueChanged:")]
        public void maxResultsStepperValueChanged(UIStepper sender)
        {
            maxResultsLabel.Text = sender.Value + "";
            Delegate?.View(this, action: new Action.ChangeMaxResults { maxResults = (int)sender.Value });
        }

        [Export("thresholdStepperValueChanged:")]
        public void thresholdStepperValueChanged(UIStepper sender)
        {
            thresholdLabel.Text = System.String.Format("%.1f", sender.Value);
            Delegate?.View(this, action: new Action.ChangeScoreThreshold { threshold = (float)sender.Value });
        }

        [Export("threadsStepperValueChanged:")]
        public void threadsStepperValueChanged(UIStepper sender)
        {
            threadsLabel.Text = (int)sender.Value + "";
            Delegate?.View(this, action: new Action.ChangeThreadCount { threadCount = (int)sender.Value });
        }

        [Export("showHiddenButtonTouchUpInside:")]
        public void showHidenButtonTouchUpInside(UIButton sender)
        {
            sender.Selected = !sender.Selected;
            showHiddenButtonLayoutConstraint.Constant = sender.Selected ? 300 : 40;
            UIView.Animate(
                duration: 0.3,
                animation: (() =>
                    this.Superview?.LayoutIfNeeded()
                ), completion: (() => { }));
        }
    }
}
