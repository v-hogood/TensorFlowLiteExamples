using static AudioClassification.HomeViewController;

namespace AudioClassification
{
    public interface InferenceViewDelegate
    {
        // This method is called when the user changes the value to update model used for inference.
        void View(InferenceView view, InferenceView.Change action);
    }

    // View to allows users changing the inference configs.
    public partial class InferenceView : UIView
    {
        public InferenceView(IntPtr handle) : base(handle) { }

        public class Change : NSObject
        {
            public sealed class ChangeModel : Change { public ModelType modelType; };
            public sealed class ChangeOverlap : Change { public double overlap; };
            public sealed class ChangeMaxResults : Change { public int maxResults; };
            public sealed class ChangeScoreThreshold : Change { public float threshold;  };
            public sealed class ChangeThreadCount : Change { public int threadCount; };
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
            thresholdLabel.Text = string.Format("{0:0.0}", threshold);
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
            Delegate?.View(this, action: new Change.ChangeModel { modelType = modelSelect });
        }

        [Export("overlapStepperValueChanged:")]
        public void overlapStepperValueChanged(UIStepper sender)
        {
            overlapLabel.Text = String.Format("{0:0}", sender.Value * 100) + "%";
            Delegate?.View(this, action: new Change.ChangeOverlap { overlap = sender.Value });
        }

        [Export("maxResultsStepperValueChanged:")]
        public void maxResultsStepperValueChanged(UIStepper sender)
        {
            maxResultsLabel.Text = sender.Value + "";
            Delegate?.View(this, action: new Change.ChangeMaxResults { maxResults = (int)sender.Value });
        }

        [Export("thresholdStepperValueChanged:")]
        public void thresholdStepperValueChanged(UIStepper sender)
        {
            thresholdLabel.Text = String.Format("{0:0.0}", sender.Value);
            Delegate?.View(this, action: new Change.ChangeScoreThreshold { threshold = (float)sender.Value });
        }

        [Export("threadsStepperValueChanged:")]
        public void threadsStepperValueChanged(UIStepper sender)
        {
            threadsLabel.Text = (int)sender.Value + "";
            Delegate?.View(this, action: new Change.ChangeThreadCount { threadCount = (int)sender.Value });
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
