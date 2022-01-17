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
        //
        public ClassifierQuantizedEfficientNet(Activity activity, Device device, int numThreads) :
            base(activity, device, numThreads)
        {
        }

        protected override string GetModelPath()
        {
            // you can download this file from
            // see build.gradle for where to obtain this file. It should be auto
            // downloaded into assets.
            return "efficientnet-lite0-int8.tflite";
        }

        protected override string GetLabelPath()
        {
            return "labels_without_background.txt";
        }

        protected override ITensorOperator GetPreprocessNormalizeOp()
        {
            return new NormalizeOp(ImageMean, ImageStd);
        }

        protected override ITensorOperator GetPostprocessNormalizeOp()
        {
            return new NormalizeOp(ProbabilityMean, ProbabilityStd);
        }
    }
}
