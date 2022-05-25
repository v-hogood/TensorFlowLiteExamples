using System.Collections.Generic;
using System.IO;
using Android.Content;
using Android.Content.Res;
using Android.Util;
using Java.IO;
using Java.Lang;
using Java.Nio;
using Java.Nio.Channels;
using Xamarin.TensorFlow.Lite;
using Xamarin.TensorFlow.Lite.Support.Metadata;

namespace TextClassification
{
    // Interface to load TfLite model and provide predictions.
    public class TextClassificationClient
    {
        private const string Tag = "Interpreter";

        private const int SentenceLen = 256; // The maximum length of an input sentence.
        // Simple delimiter to split words.
        private char[] SimpleSpaceOrPunctuation = { ' ', ',', '.', '!', '?', '\t' };
        private const string ModePath = "text_classification.tflite";
        //
        // Reserved values in ImdbDataSet dic:
        // dic["<PAD>"] = 0      used for padding
        // dic["<START>"] = 1    mark for the start of a sentence
        // dic["<UNKNOWN>"] = 2  mark for unknown words (OOV)
        //
        private const string Start = "<START>";
        private const string Pad = "<PAD>";
        private const string Unknown = "<UNKNOWN>";

        private Context context;
        public Dictionary<string, int> Dic { get; } = new Dictionary<string, int>();
        public List<string> Labels { get; } = new List<string>();
        public Interpreter TfLite { get; private set; }

        public TextClassificationClient(Context context)
        {
            this.context = context;
        }

        // Load the TF Lite model and dictionary so that the client can start classifying text.
        public void Load()
        {
            LoadModel();
        }

        // Load TF Lite model.
        private void LoadModel()
        {
            try
            {
                // Load the TF Lite model
                ByteBuffer buffer = LoadModelFile(this.context.Assets, ModePath);
                TfLite = new Interpreter(buffer);
                Log.Verbose(Tag, "TFLite model loaded.");

                // Use metadata extractor to extract the dictionary and label files.
                MetadataExtractor metadataExtractor = new MetadataExtractor(buffer);

                // Extract and load the dictionary file.
                Stream dictionaryFile = metadataExtractor.GetAssociatedFile("vocab.txt");
                LoadDictionaryFile(dictionaryFile);
                Log.Verbose(Tag, "Dictionary loaded.");

                // Extract and load the label file.
                Stream labelFile = metadataExtractor.GetAssociatedFile("labels.txt");
                LoadLabelFile(labelFile);
                Log.Verbose(Tag, "Labels loaded.");
            }
            catch (Java.IO.IOException ex)
            {
                Log.Error(Tag, "Error loading TF Lite model.\n", ex);
            }
        }

        // Free up resources as the client is no longer needed.
        public void Unload()
        {
            TfLite.Close();
            Dic.Clear();
            Labels.Clear();
        }

        // Classify an input string and returns the classification results.
        public List<Result> Classify(string text)
        {
            // Pre-prosessing.
            int[][] input = TokenizeInputText(text);

            // Run inference.
            Log.Verbose(Tag, "Classifying text with TF Lite...");
            float[][] output = new float[1][] { new float[Labels.Count] };
            Object Output = Object.FromArray(output);
            TfLite.Run(Object.FromArray(input), Output);
            output = Output.ToArray<float[]>();

            // Find the best classifications.
            List<Result> results = new List<Result>(Labels.Count);
            for (int i = 0; i < Labels.Count; i++)
            {
                results.Add(new Result("" + i, Labels[i], output[0][i]));
            }
            results.Sort((x, y) => y.Confidence.CompareTo(x.Confidence));

            // Return the probability of each class.
            return results;
        }

        // Load TF Lite model from assets.
        private static MappedByteBuffer LoadModelFile(AssetManager assetManager, string modelPath)
        {
            AssetFileDescriptor fileDescriptor = assetManager.OpenFd(modelPath);
            FileInputStream inputStream = new FileInputStream(fileDescriptor.FileDescriptor);
            FileChannel fileChannel = inputStream.Channel;
            long startOffset = fileDescriptor.StartOffset;
            long declaredLength = fileDescriptor.DeclaredLength;
            return fileChannel.Map(FileChannel.MapMode.ReadOnly, startOffset, declaredLength);
        }

        // Load dictionary from model file.
        private void LoadLabelFile(Stream ins)
        {
            StreamReader reader = new StreamReader(new BufferedStream(ins));
            // Each line in the label file is a label.
            while (!reader.EndOfStream)
            {
                Labels.Add(reader.ReadLine());
            }
        }

        // Load labels from model file.
        private void LoadDictionaryFile(Stream ins)
        {
            StreamReader reader = new StreamReader(new BufferedStream(ins));
            // Each line in the dictionary has two columns.
            // First column is a word, and the second is the index of this word.
            while (!reader.EndOfStream)
            {
                List<string> line = new List<string>(reader.ReadLine().Split(" "));
                if (line.Count < 2)
                {
                    continue;
                }
                Dic.Add(line[0], Integer.ParseInt(line[1]));
            }
        }

        // Pre-prosessing: tokenize and map the input words into a float array.
        int[][] TokenizeInputText(string text)
        {
            int[] tmp = new int[SentenceLen];
            List<string> array = new List<string>(text.Split(SimpleSpaceOrPunctuation));

            int index = 0;
            // Prepend <START> if it is in vocabulary file.
            if (Dic.ContainsKey(Start))
            {
                tmp[index++] = Dic.GetValueOrDefault(Start);
            }

            foreach (string word in array)
            {
                if (index >= SentenceLen)
                {
                    break;
                }
                tmp[index++] = Dic.ContainsKey(word) ? Dic.GetValueOrDefault(word) : Dic.GetValueOrDefault(Unknown);
            }
            // Padding and wrapping.
            System.Array.Fill(tmp, index, SentenceLen - 1, Dic.GetValueOrDefault(Pad));
            int[][] ans = { tmp };
            return ans;
        }
    }
}
