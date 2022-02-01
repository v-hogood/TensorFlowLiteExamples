using Android.App;
using Org.Tensorflow.Lite.Support.Common;
using Org.Tensorflow.Lite.Support.Common.Ops;

namespace ImageClassification
{
    // This TensorFlowLite classifier works with the quantized EfficientNet model.
    public class ClassifierQuantizedEfficientNet : Classifier
    {
        //
        // The quantized model does not require normalization, thus set mean as 0.0f, and std as 1.0f to
        // bypass the normalization.
        //
        private const float ImageMean = 0.0f;
        private const float ImageStd = 1.0f;

        // Quantized EfficientNet requires additional dequantization to the output probability.
        private const float ProbabilityMean = 0.0f;
        private const float ProbabilityStd = 255.0f;

        //
        // Initializes a {@code ClassifierQuantizedEfficentNet}.
        //
        // @param activity
        // @param device a {@link Device} object to configure the hardware accelerator
        // @param numThreads the number of threads during the inference
        // @throws IOException if the model is not loaded correctly
        //
        public ClassifierQuantizedEfficientNet(Activity activity, Device device, int numThreads) :
            base(activity, device, numThreads)
        {
        }

        protected override string ModelPath { get; } = "efficientnet-lite0-int8.tflite";

        protected override string LabelPath { get; } = "labels_without_background.txt";

        protected override ITensorOperator PreprocessNormalizeOp { get; } = new NormalizeOp(ImageMean, ImageStd);

        protected override ITensorOperator PostprocessNormalizeOp { get; } = new NormalizeOp(ProbabilityMean, ProbabilityStd);
    }
}
