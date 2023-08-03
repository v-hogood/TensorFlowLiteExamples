using TensorFlowLiteTaskAudio;

namespace AudioClassification
{
    // TableViewCell to display the inference results. Each cell corresponds to a single category.
    partial class ResultTableViewCell: UITableViewCell
    {
        public ResultTableViewCell(System.IntPtr handle) : base(handle) { }

        public void SetData(TFLCategory data)
        {
            nameLabel.Text = data.Label;
            if (!Double.IsNaN(data.Score))
            {
                // score view width is equal 1/4 screen with
                scoreWidthLayoutConstraint.Constant = UIScreen.MainScreen.Bounds.Width / 4 * new nfloat(data.Score);
            }
            else
            {
                scoreWidthLayoutConstraint.Constant = 0;
            }
        }
    }
}
