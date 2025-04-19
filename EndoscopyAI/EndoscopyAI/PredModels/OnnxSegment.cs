using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OnnxImageClassifierWPF
{
    public class OnnxSegmenter
    {
        private readonly InferenceSession session;

        public OnnxSegmenter(string modelPath)
        {
            session = new InferenceSession(modelPath);
        }

        public SegmentationResult Predict(string imagePath)
        {
            var inputTensor = PreprocessImage(imagePath);
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input", inputTensor) 
            };

            // 运行推理
            using var results = session.Run(inputs);
            var output = results.First().AsTensor<float>();

            // 处理输出张量 (1×4×512×512)
            var segmentationMap = ProcessOutput(output);

            return new SegmentationResult
            {
                ClassMap = segmentationMap,
                OutputTensor = output
            };
        }

        private int[,] ProcessOutput(Tensor<float> output)
        {
            // 输出形状应为 [1, 4, 512, 512]
            if (output.Dimensions.Length != 4 ||
                output.Dimensions[0] != 1 ||
                output.Dimensions[1] != 4 ||
                output.Dimensions[2] != 512 ||
                output.Dimensions[3] != 512)
            {
                throw new ArgumentException("Unexpected output tensor shape");
            }

            var classMap = new int[512, 512];

            for (int y = 0; y < 512; y++)
            {
                for (int x = 0; x < 512; x++)
                {
                    // 获取当前像素的4个通道值
                    float[] pixelScores = new float[4];
                    for (int c = 0; c < 4; c++)
                    {
                        pixelScores[c] = output[0, c, y, x];
                    }

                    // 选择得分最高的类别
                    int predictedClass = 0;
                    float maxScore = pixelScores[0];
                    for (int c = 1; c < 4; c++)
                    {
                        if (pixelScores[c] > maxScore)
                        {
                            maxScore = pixelScores[c];
                            predictedClass = c;
                        }
                    }

                    classMap[y, x] = predictedClass;
                }
            }

            return classMap;
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

    public class SegmentationResult
    {
        /// <summary>
        /// 512x512的二维数组，每个元素表示该像素点的预测类别(0-3)
        /// </summary>
        public int[,] ClassMap { get; set; }

        /// <summary>
        /// 原始输出张量 (1×4×512×512)
        /// </summary>
        public Tensor<float> OutputTensor { get; set; }

        /// <summary>
        /// 将分类结果转换为彩色图像
        /// </summary>
        public Bitmap ToColorImage()
        {
            // 定义每个类别的颜色 (可以根据需要修改)
            Color[] classColors = new Color[]
            {
                Color.Red,     // 类别0
                Color.Green,   // 类别1
                Color.Blue,   // 类别2
                Color.Yellow  // 类别3
            };

            Bitmap result = new Bitmap(512, 512);
            for (int y = 0; y < 512; y++)
            {
                for (int x = 0; x < 512; x++)
                {
                    int classId = ClassMap[y, x];
                    result.SetPixel(x, y, classColors[classId]);
                }
            }

            return result;
        }
    }
}