using Android.App;
using Org.Tensorflow.Lite.Support.Common;
using Org.Tensorflow.Lite.Support.Common.Ops;

namespace ImageClassification
{
    // This TensorFlowLite classifier works with the float EfficientNet model.
    public class ClassifierFloatEfficientNet : Classifier
    {
        private const float ImageMean = 127.0f;
        private const float ImageStd = 128.0f;

        //
        // Float model does not need dequantization in the post-processing. Setting mean and std as 0.0f
        // and 1.0f, repectively, to bypass the normalization.
        //
        private const float ProbabilityMean = 0.0f;
        private const float ProbabilityStd = 1.0f;

        //
        // Initializes a {@code ClassifierFloatEfficientNet}.
        //
        // @param activity
        // @param device a {@link Device} object to configure the hardware accelerator
        // @param numThreads the number of threads during the inference
        // @throws IOException if the model is not loaded correctly
        //
        public ClassifierFloatEfficientNet(Activity activity, Device device, int numThreads) :
            base(activity, device, numThreads)
        {
        }

        protected override string ModelPath { get; } = "efficientnet-lite0-fp32.tflite";

        protected override string LabelPath { get; } = "labels_without_background.txt";

        protected override ITensorOperator PreprocessNormalizeOp { get; } = new NormalizeOp(ImageMean, ImageStd);

        protected override ITensorOperator PostprocessNormalizeOp { get; } = new NormalizeOp(ProbabilityMean, ProbabilityStd);
    }
}
