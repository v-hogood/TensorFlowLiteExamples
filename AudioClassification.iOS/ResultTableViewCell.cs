// TableViewCell to display the inference results. Each cell corresponds to a single category.
using TensorFlowLiteTaskAudio;
using UIKit;

namespace AudioClassification
{
    partial class ResultTableViewCell: UITableViewCell
    {
        public ResultTableViewCell(System.IntPtr handle) : base(handle) { }

        public void SetData(TFLCategory data)
        {
            nameLabel.Text = data.Label;
            if (!System.Double.IsNaN(data.Score))
            {
                // score view width is equal 1/4 screen with
                scoreWidthLayoutConstraint.Constant = UIScreen.MainScreen.Bounds.Width / 4 * new System.nfloat(data.Score);
            }
            else
            {
                scoreWidthLayoutConstraint.Constant = 0;
            }
        }
    }
}
