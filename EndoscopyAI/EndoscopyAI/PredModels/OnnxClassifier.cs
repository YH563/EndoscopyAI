using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace OnnxImageClassifierWPF
{
    // 枚举类型，存放疾病类别
    public enum DiseaseCategory
    {
        Barret,
        Cancer,
        Inflammation,
        Normal
    }

    public class OnnxClassifier
    {
        private readonly InferenceSession session;

        public OnnxClassifier(string modelPath)
        {
            session = new InferenceSession(modelPath);
        }

        private float[] Softmax(float[] values)
        {
            var expValues = values.Select(v => Math.Exp(v)).ToArray();
            var sumExp = expValues.Sum();
            return expValues.Select(v => (float)(v / sumExp)).ToArray();
        }

        private float[] Normalize(float[] values)
        {
            float min = values.Min();
            float[] adjustedValues = values.Select(v => v - min).ToArray();
            float sum = adjustedValues.Sum();

            if (sum == 0)
            {
                return values.Select(_ => 1f / values.Length).ToArray();
            }
            return adjustedValues.Select(v => v / sum).ToArray();
        }

        public (string , float ) Predict(string imagePath)
        {
            var inputTensor = PreprocessImage(imagePath);
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input", inputTensor)
            };

            using var results = session.Run(inputs);
            var output = results.First().AsEnumerable<float>().ToArray();

            // 计算置信度
            var probabilities = Softmax(output);
            int predictedClass = Array.IndexOf(probabilities, probabilities.Max());
            DiseaseCategory[] categories = (DiseaseCategory[])Enum.GetValues(typeof(DiseaseCategory));
            string diagnosisResult = categories[predictedClass].ToString();
            float confidence = probabilities[predictedClass];

            return (diagnosisResult, confidence);
        }

        private DenseTensor<float> PreprocessImage(string path)
        {
            Bitmap bmp = new Bitmap(path);
            Bitmap resized = new Bitmap(bmp, new Size(512, 512));

            var tensor = new DenseTensor<float>(new[] { 1, 3, 512, 512 });
            for (int y = 0; y < 512; y++)
            {
                for (int x = 0; x < 512; x++)
                {
                    Color color = resized.GetPixel(x, y);
                    tensor[0, 0, y, x] = color.R / 255.0f;
                    tensor[0, 1, y, x] = color.G / 255.0f;
                    tensor[0, 2, y, x] = color.B / 255.0f;
                }
            }
            return tensor;
        }
    }
}