using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Foundation;
using TensorFlowLite;

namespace SoundClassification
{
    public interface ISoundClassifierDelegate
    {
        void SoundClassifier(SoundClassifier soundClassifier,
            float[] didInterpreteProbabilities);
    }

    // Performs classification on sound.
    // The API supports models which accept sound input via `Int16` sound buffer and one classification output tensor.
    // The output of the recognition is emitted as delegation.
    public class SoundClassifier
    {
        // MARK: - Constants
        private string modelFileName;
        private string modelFileExtension;
        private string labelFilename;
        private string labelFileExtension;
        private int audioBufferInputTensorIndex = 0;

        // MARK: - Variables
        public ISoundClassifierDelegate Delegate;

        /// Sample rate for input sound buffer. Caution: generally this value is a bit less than 1 second's audio sample.
        public int SampleRate = 0;
        /// Lable names described in the lable file
        public string[] LabelNames;
        private TFLInterpreter interpreter;

        // MARK: - Public Methods

        public SoundClassifier(
            string modelFileName,
            string modelFileExtension = "tflite",
            string labelFilename = "labels",
            string labelFileExtension = "txt",
            ISoundClassifierDelegate Delegate = null)
        {
            this.modelFileName = modelFileName;
            this.modelFileExtension = modelFileExtension;
            this.labelFilename = labelFilename;
            this.labelFileExtension = labelFileExtension;
            this.Delegate = Delegate;

            SetupInterpreter();
        }

        // Invokes the `Interpreter` and processes and returns the inference results.
        public void Start(short[] inputBuffer)
        {
            TFLTensor outputTensor;
            NSData outputData;

            var audioBufferData = Int16ArrayToData(inputBuffer);

            NSError error;
            TFLTensor inputTensor = 
                interpreter.InputTensorAtIndex(index: (nuint)audioBufferInputTensorIndex, error: out error);
            if (inputTensor == null ||
                !inputTensor.CopyData(data: audioBufferData, error: out error) ||
                !interpreter.InvokeWithError(error: out error) ||
                (outputTensor = interpreter.OutputTensorAtIndex(0, error: out error)) == null ||
                (outputData = outputTensor.DataWithError(error: out error)) == null)
            {
                Debug.Print(">>> Failed to invoke the interpreter with error: " + error.LocalizedDescription);
                return;
            }

            // Gets the formatted and averaged results.
            var probabilities = DataToFloatArray(outputData);
            Delegate?.SoundClassifier(this, didInterpreteProbabilities: probabilities);
        }

        // MARK: - Private Methods

        private void SetupInterpreter()
        {
            var modelPath = NSBundle.MainBundle.PathForResource(
                name: modelFileName,
                ofType: modelFileExtension);
            if (modelPath == null) return;

            NSError error;
            interpreter = new TFLInterpreter(modelPath: modelPath, error: out error);

            NSNumber[] inputShape;
            if (interpreter != null &&
                interpreter.AllocateTensorsWithError(error: out error) &&
                (inputShape = (interpreter.InputTensorAtIndex(index: 0, error: out error)?.
                    ShapeWithError(error: out error))) != null &&
                interpreter.InvokeWithError(error: out error))
            {
                SampleRate = (int)inputShape[1];

                LabelNames = LoadLabels();
            }
            else
            {
                Debug.Print("Failed to create the interpreter with error: " + error.LocalizedDescription);
                return;
            }
        }

        private string[] LoadLabels()
        {
            var labelPath = NSBundle.MainBundle.PathForResource(
                name: labelFilename,
                ofType: labelFileExtension);
            if (labelPath == null) return null;

            var content = File.ReadAllText(path: labelPath, encoding: Encoding.UTF8);

            var labels = content.Split("\n")
                .Where(x => x.Length != 0).ToArray()
                .Select(x => x.Split(" ")[1]).ToArray();

            return labels;
        }

        // Creates a new buffer by copying the buffer pointer of the given `Int16` array.
        private NSData Int16ArrayToData(short[] buffer)
        {
            var floatData = buffer.Select(x => (float)x / (float)Int16.MaxValue).ToArray();
            var handle = GCHandle.Alloc(floatData, GCHandleType.Pinned);
            var data = NSData.FromBytes(handle.AddrOfPinnedObject(), (nuint)(floatData.Length * sizeof(float)));
            handle.Free();
            return data;
        }

        // Creates a new array from the bytes of the given unsafe data.
        // - Returns: `nil` if `unsafeData.count` is not a multiple of `MemoryLayout<Float>.stride`.
        private float[] DataToFloatArray(NSData data)
        {
            if (data.Count() % sizeof(float) != 0) return null;
            var floatArray = new float[data.Count() / sizeof(float)];
            Marshal.Copy(data.Bytes, floatArray, 0, floatArray.Length);
            return floatArray;
        }
    }
}
