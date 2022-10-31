using System;
using System.Linq;
using CoreGraphics;
using CoreImage;
using Foundation;
using Photos;
using UIKit;
using static ImageClassification.Constants;
using static ImageClassification.InferenceViewController;

namespace ImageClassification
{
    // MARK: InferenceViewControllerDelegate Method Declarations
    public interface InferenceViewControllerDelegate
    {
        //
        // This method is called when the user changes the value to update model used for inference.
        //
        public void ViewControllerAction(
            InferenceViewController viewController,
            InferenceViewController.Change action);
    }

    public static class Extensions
    {
        public static string DisplayString(this InferenceViewController.InferenceInfo inferenceInfo) =>
            inferenceInfo switch
            {
                InferenceViewController.InferenceInfo.Resolution => "Resolution",
                InferenceViewController.InferenceInfo.InferenceTime => "Inference Time",
                _ => ""
            };
    }

    public partial class InferenceViewController : UIViewController,
        IUITableViewDataSource, IUITableViewDelegate,
        IUIPickerViewDataSource, IUIPickerViewDelegate
    {
        public InferenceViewController(IntPtr handle) : base(handle) { }

        public class Change : NSObject
        {
            public sealed class ChangeThreadCount : Change { public int threadCount; };
            public sealed class ChangeScoreThreshold : Change { public float scoreThreshold; };
            public sealed class ChangeMaxResults : Change { public int maxResults; };
            public sealed class ChangeModel : Change { public ModelType model; };
        }

        // MARK: Sections and Information to display
        private enum InferenceSections
        {
            Results,
            InferenceInfo
        }

        public enum InferenceInfo
        {
            Resolution,
            InferenceTime
        }

        // MARK: Constants
        private float normalCellHeight = 27.0F;
        private float bottomSheetButtonDisplayHeight = 44.0F;
         private UIColor lightTextInfoColor = new UIColor(
            red: 117.0F / 255.0F, green: 117.0F / 255.0F, blue: 117.0F / 255.0F, alpha: 1.0F);
        private UIFont infoFont = UIFont.SystemFontOfSize(size: 14.0F, weight: UIFontWeight.Regular);
        private UIFont highlightedFont = UIFont.SystemFontOfSize(size: 14.0F, weight: UIFontWeight.Medium);

        // MARK: Instance Variables
        public ImageClassificationResult? InferenceResult;
        public CGSize Resolution = new CGSize(0, 0);
        public int MaxResults = DefaultConstants.MaxResults;
        public int CurrentThreadCount = DefaultConstants.ThreadCount;
        float scoreThreshold = DefaultConstants.ScoreThreshold;
        int modelSelectIndex = 0;
        private UIColor infoTextColor = UIColor.Black;

        private ModelType modelSelect =>
            modelSelectIndex < Enum.GetNames(typeof(ModelType)).Length ?
                (ModelType)modelSelectIndex :
                ModelType.EfficientnetLite0;

        // MARK: Delegate
        public InferenceViewControllerDelegate Delegate;

        // MARK: Computed properties
        public float CollapsedHeight =>
            normalCellHeight * (float)(MaxResults - 1) + bottomSheetButtonDisplayHeight;

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            // Set up stepper
            threadStepper.UserInteractionEnabled = true;
            threadStepper.Value = (double)CurrentThreadCount;

            // Set the info text color on iOS 11 and higher.
            if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
            {
                infoTextColor = UIColor.FromName(name: "darkOrLight");
            }
            SetupUI();
        }

        // MARK: private func
        UIButton doneButton;
        private void SetupUI()
        {
            threadStepper.Value = (double)CurrentThreadCount;
            threadValueLabel.Text = CurrentThreadCount.ToString();

            maxResultStepper.Value = (double)MaxResults;
            maxResultLabel.Text = MaxResults.ToString();

            thresholdStepper.Value = (double)scoreThreshold;
            thresholdValueLabel.Text = scoreThreshold.ToString();

            modelTextField.Text = modelSelect.Title();

            var picker = new UIPickerView();
            picker.DataSource = this;
            picker.Delegate = this;
            modelTextField.InputView = picker;

            doneButton = new UIButton(frame: new CGRect(x: 0, y: 0, width: 60, height: 44));
            doneButton.SetTitle("Done", forState: UIControlState.Normal) ;
            doneButton.SetTitleColor(UIColor.Blue, forState: UIControlState.Normal) ;
            doneButton.AddTarget(
                this, new ObjCRuntime.Selector("choseModelDoneButtonTouchUpInside:"), UIControlEvent.TouchUpInside);
            var inputAccessoryView = new UIView(
                frame: new CGRect(
                    x: 0,
                    y: 0,
                    width: UIScreen.MainScreen.Bounds.Size.Width,
                    height: 44));
            inputAccessoryView.BackgroundColor = UIColor.Gray;
            inputAccessoryView.AddSubview(doneButton);
            modelTextField.InputAccessoryView = inputAccessoryView;
        }

        // MARK: Buttion Actions
        //
        // Delegate the change of number of threads to View Controller and change the stepper display.
        //
        [Export("onClickThreadStepper:")]
        public void onClickThreadStepper(UIStepper sender)
        {
            CurrentThreadCount = (int)threadStepper.Value;
            threadValueLabel.Text = CurrentThreadCount.ToString();
            Delegate?.ViewControllerAction(this, action: new Change.ChangeThreadCount { threadCount = CurrentThreadCount });
        }

        [Export("thresholdStepperValueChanged:")]
        public void thresholdStepperValueChanged(UIStepper sender)
        {
            scoreThreshold = (float)sender.Value;
            Delegate?.ViewControllerAction(this, action: new Change.ChangeScoreThreshold { scoreThreshold = scoreThreshold });
            thresholdValueLabel.Text = scoreThreshold.ToString();
        }

        [Export("maxResultStepperValueChanged:")]
        public void maxResultStepperValueChanged(UIStepper sender)
        {
            MaxResults = (int)sender.Value;
            Delegate?.ViewControllerAction(this, action: new Change.ChangeMaxResults { maxResults = MaxResults });
            maxResultLabel.Text = MaxResults.ToString();
        }

        [Export("choseModelDoneButtonTouchUpInside:")]
        public void choseModelDoneButtonTouchUpInside(UIButton sender)
        {
            Delegate?.ViewControllerAction(this, action: new Change.ChangeModel { model = modelSelect });
            modelTextField.Text = modelSelect.Title();
            modelTextField.ResignFirstResponder();
        }

        // MARK: UITableView Data Source
        [Export("numberOfSectionsInTableView:")]
        public nint NumberOfSections(UITableView tableView)
        {
            return Enum.GetNames(typeof(InferenceSections)).Length;
        }

        public nint RowsInSection(UITableView tableView, nint section)
        {
            var inferenceSection = (InferenceSections)(int)section;
            if (!Enum.IsDefined(typeof(InferenceSections), inferenceSection))
                return 0;

            return inferenceSection switch
            {
                InferenceSections.Results => MaxResults,
                InferenceSections.InferenceInfo => Enum.GetNames(typeof(InferenceInfo)).Length,
                _ => 0
            };
        }

        [Export("tableView:heightForRowAtIndexPath:")]
        public nfloat GetHeightForRow(UITableView tableView, NSIndexPath indexPath)
        {
            return normalCellHeight;
        }

        public UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var cell = tableView.DequeueReusableCell(identifier: "INFO_CELL") as InfoCell;

            var inferenceSection = (InferenceSections)indexPath.Section;
            if (!Enum.IsDefined(typeof(InferenceSections), inferenceSection))
                return cell;

            var fieldName = "";
            var info = "";
            var font = infoFont;
            var color = infoTextColor;

            switch (inferenceSection)
            {
                case InferenceSections.Results:
                    var tuple1 = DisplayStringsForResults(row: indexPath.Row);
                    fieldName = tuple1.Item1;
                    info = tuple1.Item2;

                    if (indexPath.Row == 0)
                    {
                        font = highlightedFont;
                        color = infoTextColor;
                    }
                    else
                    {
                        font = infoFont;
                        color = lightTextInfoColor;
                    }
                    break;

                case InferenceSections.InferenceInfo:
                    var tuple2 = DisplayStringsForInferenceInfo(row: indexPath.Row);
                    fieldName = tuple2.Item1;
                    info = tuple2.Item2;
                    break;
            }

            cell.fieldNameLabel.Font = font;
            cell.fieldNameLabel.TextColor = color;
            cell.fieldNameLabel.Text = fieldName;
            cell.infoLabel.Text = info;
            return cell;
        }

        // MARK: Format Display of information in the bottom sheet
        //
        // This method formats the display of the inferences for the current frame.
        //
        Tuple<string, string> DisplayStringsForResults(int row)
        {
            var fieldName = "";
            var info = "";

            ImageClassificationResult? tempResult = InferenceResult;
            if (tempResult == null || tempResult?.classifications.Categories.Count() == 0)
            {
                if (row == 1)
                {
                    fieldName = "No Results";
                }
                return new Tuple<string, string>(fieldName, info);
            }

            if (row < tempResult?.classifications.Categories.Count())
            {
                var category = tempResult?.classifications.Categories[row];
                fieldName = category.Label ?? "";
                info = String.Format("{0:0.00}", category.Score * 100.0) + "%";
            }
            return new Tuple<string, string>(fieldName, info);
        }

        //
        // This method formats the display of additional information relating to the inferences.
        //
        Tuple<string, string> DisplayStringsForInferenceInfo(int row)
        {
            var fieldName = "";
            var info = "";

            var inferenceInfo = (InferenceInfo)row;
            if (!Enum.IsDefined(typeof(InferenceInfo), inferenceInfo))
                return new Tuple<string, string>(fieldName, info);

            fieldName = inferenceInfo.DisplayString();

            ImageClassificationResult? finalResults = InferenceResult;
            info = inferenceInfo switch
            {
                InferenceInfo.Resolution =>
                    (int)Resolution.Width + "x" + (int)Resolution.Height,
                InferenceInfo.InferenceTime =>
                    finalResults == null ? "0ms" :
                    String.Format("{0:0.00}ms", finalResults?.inferenceTime),
                _ => "0ms"
            };

            return new Tuple<string, string>(fieldName, info);
        }

        public nint GetComponentCount(UIPickerView pickerView)
        {
            return 1;
        }

        public nint GetRowsInComponent(UIPickerView pickerView, nint component)
        {
            return Enum.GetNames(typeof(ModelType)).Length;
        }

        [Export("pickerView:didSelectRow:inComponent:")]
        public void Selected(UIPickerView pickerView, nint row, nint component)
        {
            modelSelectIndex = (int)row;
        }

        [Export("pickerView:titleForRow:forComponent:")]
        public string GetTitle(UIPickerView pickerView, nint row, nint component)
        {
            if (row < Enum.GetNames(typeof(ModelType)).Length)
            {
                return ((ModelType)(int)row).Title();
            }
            else
            {
                return null;
            }
        }
    }
}
