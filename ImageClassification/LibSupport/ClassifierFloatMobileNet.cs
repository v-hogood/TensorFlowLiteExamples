using Android.App;
using Org.Tensorflow.Lite.Support.Common;
using Org.Tensorflow.Lite.Support.Common.Ops;

namespace ImageClassification
{
    // This TensorFlowLite classifier works with the float MobileNet model.
    public class ClassifierFloatMobileNet : Classifier
    {
        // Float MobileNet requires additional normalization of the used input.
        private const float ImageMean = 127.5f;
        private const float ImageStd = 127.5f;

        //
        // Float model does not need dequantization in the post-processing. Setting mean and std as 0.0f
        // and 1.0f, repectively, to bypass the normalization.
        //
        private const float ProbabilityMean = 0.0f;

        private const float ProbabilityStd = 1.0f;

        //
        // Initializes a {@code ClassifierFloatMobileNet}.
        //
        // @param activity
        //
        public ClassifierFloatMobileNet(Activity activity, Device device, int numThreads) :
            base(activity, device, numThreads)
        {
        }

        protected override string GetModelPath()
        {
            // you can download this file from
            // see build.gradle for where to obtain this file. It should be auto
            // downloaded into assets.
            return "mobilenet_v1_1.0_224.tflite";
        }

        protected override string GetLabelPath()
        {
            return "labels.txt";
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
