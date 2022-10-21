using CoreFoundation;
using Foundation;
using System;
using System.Collections.Generic;
using System.Text;
using TensorFlowLiteTaskText;
using UIKit;

namespace TextClassification
{
    public partial class ViewController : UIViewController,
        IUITableViewDataSource,
        IUITextFieldDelegate
    {
        private TFLNLClassifier classifier;

        void LoadModel()
        {
            var modelPath = NSBundle.MainBundle.PathForResource(
                "text_classification", "tflite");
            if (modelPath == null) return;
            var options = new TFLNLClassifierOptions();
            this.classifier = TFLNLClassifier.NlClassifierWithModelPath(modelPath, options);
        }

        private List<ClassificationResult> results = new List<ClassificationResult>();

        public ViewController(IntPtr handle) : base(handle) { }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad();

            NavigationController.NavigationBar.BarTintColor =
                new UIColor(red: 1, green: 0x6F / 0xFF, blue: 0, alpha: 1);
            NavigationController.NavigationBar.TitleTextAttributes =
                new UIStringAttributes() { ForegroundColor = UIColor.White };
            NavigationController.NavigationBar.Translucent = false;

            tableView.RegisterClassForCellReuse(typeof(UITableViewCell), "UITableViewCell");
            tableView.DataSource = this;
            textField.Delegate = this;

            // Initialize a TextClassification instanc
            LoadModel();
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            NSNotificationCenter.DefaultCenter.AddObserver(
                aName: UIKeyboard.WillShowNotification,
                notify: ((NSNotification notification) =>
                { KeyboardWillShow(notification); }),
                fromObject: null);
            NSNotificationCenter.DefaultCenter.AddObserver(
                aName: UIKeyboard.WillHideNotification,
                notify: ((NSNotification notification) =>
                { KeyboardWillHide(notification); }),
                fromObject: null);
        }

        public override void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);
            NSNotificationCenter.DefaultCenter.RemoveObserver(this);
        }

        // Action when user tap the "Classify" button.
        [Export("tapClassify:")]
        public void TapClassify(UIButton sender)
        {
            var text = textField.Text;
            if (text.Length == 0) { return; }
            Classify(text: text);
        }

        // Classify the text and display the result.
        private void Classify(string text)
        {
            // Run TF Lite inference in a background thread to avoid blocking app UI.
            DispatchQueue.DefaultGlobalQueue.DispatchAsync(() =>
            {
                var classifierResults = classifier.ClassifyWithText(text: text);
                var result = new ClassificationResult() { text = text, results = classifierResults };
                this.results.Add(result);

                DispatchQueue.MainQueue.DispatchAsync(() =>
                {
                    // Return to main thread to update the UI.
                    this.textField.Text = null;
                    this.tableView.ReloadData();
                });
            });
        }

        // UITableViewDataSource

        public System.nint RowsInSection(UITableView tableView, System.nint section)
        {
            return results.Count;
        }

        public UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var result = results[indexPath.Row];
            var cell = tableView.DequeueReusableCell(identifier: "UITableViewCell");
            var displayText = "Input: " + result.text + "\nOutput:\n";
            foreach (var key in result.results.Keys)
            {
                var value = result.results.ValueForKey(key);
                displayText += "\t" + key + ": " + value + "\n";
            }
            cell.TextLabel.Text = displayText.Trim();
            cell.TextLabel.Lines = 0;
            return cell;
        }

        // UITextFieldDelegate

        public bool ShouldReturn(UITextField textField)
        {
            var text = textField.Text.Trim();
            Classify(text: text);
            textField.ResignFirstResponder();
            return true;
        }

        void KeyboardWillShow(NSNotification notification)
        {
            var keyboardFrame =
                notification.UserInfo.ValueForKey(UIKeyboard.FrameEndUserInfoKey) as NSValue;
            var keyboardRectangle = keyboardFrame.CGRectValue;
            var keyboardHeight = keyboardRectangle.Height;
            textFieldBottomConstraint.Constant = -keyboardHeight;
            UIView.Animate(duration: 0.2, (() =>
            {
                this.View.LayoutIfNeeded();
            }));
        }

        void KeyboardWillHide(NSNotification notification)
        {
            textFieldBottomConstraint.Constant = 0;
            UIView.Animate(duration: 0.2, (() =>
            {
                this.View.LayoutIfNeeded();
            }));
        }
    }

    public struct ClassificationResult
    {
        public string text;
        public NSDictionary<NSString, NSNumber> results;
    }
}
