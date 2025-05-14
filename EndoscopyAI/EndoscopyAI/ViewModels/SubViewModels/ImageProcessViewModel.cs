using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace EndoscopyAI.ViewModels.SubViewModels
{
    interface IImageProcess
    {
        // 图像增强，目前只使用多通道直方图均衡化一种
        Mat HistogramEqualization(Mat input);

        // 图像降噪，各向异性扩散滤波
        Mat AnisotropicDiffusion(Mat input, int iterations = 10, float kappa = 50.0f);

        // 白平衡矫正
        Mat WhiteBalance(Mat input, Rect? roi = null);

        // 高光调整
        Mat AdjustHighlight(Mat input, double highlightFactor);

        // 图像锐化
        Mat SharpenImage(Mat input, double sharpenFactor);

        // Softmax函数
        float[] Softmax(float[] logits);

        // 创建透明叠加层
        Bitmap CreateTransparentOverlay(Tensor<float> output);
    }

    public class ImageProcess : IImageProcess
    {
        // 实现多通道直方图均衡化
        public Mat HistogramEqualization(Mat input)
        {
            if (input == null || input.Empty())
                throw new ArgumentNullException(nameof(input), "输入图像不能为空");

            // 检查图像是否为单通道或多通道
            if (input.Channels() == 1)
            {
                // 单通道图像，直接均衡化
                Mat output = new Mat();
                Cv2.EqualizeHist(input, output);
                return output;
            }
            else if (input.Channels() == 3)
            {
                // 多通道图像（如 RGB）
                Mat[] channels = Cv2.Split(input); // 分离通道
                Mat[] equalizedChannels = new Mat[3];

                // 对每个通道进行直方图均衡化
                for (int i = 0; i < channels.Length; i++)
                {
                    equalizedChannels[i] = new Mat();
                    Cv2.EqualizeHist(channels[i], equalizedChannels[i]);
                }

                // 合并均衡化后的通道
                Mat output = new Mat();
                Cv2.Merge(equalizedChannels, output);
                return output;
            }
            else
            {
                throw new NotSupportedException("仅支持单通道或三通道图像的直方图均衡化");
            }
        }

        // 图像降噪
        public Mat AnisotropicDiffusion(Mat input, int iterations = 10, float kappa = 50.0f)
        {
            if (input == null || input.Empty())
                throw new ArgumentNullException(nameof(input), "输入图像不能为空");

            // 如果是单通道图像，直接处理
            if (input.Channels() == 1)
            {
                return ApplyAnisotropicDiffusion(input, iterations, kappa);
            }
            // 如果是三通道图像，对每个通道分别处理
            else if (input.Channels() == 3)
            {
                Mat[] channels = Cv2.Split(input); // 分离通道
                Mat[] processedChannels = new Mat[3];

                for (int i = 0; i < channels.Length; i++)
                {
                    processedChannels[i] = ApplyAnisotropicDiffusion(channels[i], iterations, kappa);
                }

                Mat output = new Mat();
                Cv2.Merge(processedChannels, output); // 合并处理后的通道
                return output;
            }
            else
            {
                throw new NotSupportedException("仅支持单通道或三通道图像的各向异性扩散滤波");
            }
        }

        // 单通道图像的各向异性扩散滤波
        private Mat ApplyAnisotropicDiffusion(Mat input, int iterations, float kappa, float lambda = 0.1f)
        {
            if (input == null || input.Empty())
                throw new ArgumentNullException(nameof(input));

            Mat current = new Mat();
            input.ConvertTo(current, MatType.CV_32F);

            for (int iter = 0; iter < iterations; iter++)
            {
                // 创建梯度矩阵
                Mat north = Mat.Zeros(current.Size(), MatType.CV_32F);
                Mat south = Mat.Zeros(current.Size(), MatType.CV_32F);
                Mat east = Mat.Zeros(current.Size(), MatType.CV_32F);
                Mat west = Mat.Zeros(current.Size(), MatType.CV_32F);

                // 北方向梯度 (I(y-1,x) - I(y,x))
                current[new Rect(0, 0, current.Cols, current.Rows - 1)]
                    .CopyTo(north[new Rect(0, 1, current.Cols, current.Rows - 1)]);
                Cv2.Subtract(north, current, north);

                // 南方向梯度 (I(y+1,x) - I(y,x))
                current[new Rect(0, 1, current.Cols, current.Rows - 1)]
                    .CopyTo(south[new Rect(0, 0, current.Cols, current.Rows - 1)]);
                Cv2.Subtract(south, current, south);

                // 东方向梯度 (I(y,x+1) - I(y,x))
                current[new Rect(1, 0, current.Cols - 1, current.Rows)]
                    .CopyTo(east[new Rect(0, 0, current.Cols - 1, current.Rows)]);
                Cv2.Subtract(east, current, east);

                // 西方向梯度 (I(y,x-1) - I(y,x))
                current[new Rect(0, 0, current.Cols - 1, current.Rows)]
                    .CopyTo(west[new Rect(1, 0, current.Cols - 1, current.Rows)]);
                Cv2.Subtract(west, current, west);

                // 扩散系数（Perona-Malik函数 c(x) = exp(-|∇I|^2 / kappa^2)）
                Mat cN = new Mat(), cS = new Mat(), cE = new Mat(), cW = new Mat();
                Cv2.Exp(north.Mul(north) / (-kappa * kappa), cN);
                Cv2.Exp(south.Mul(south) / (-kappa * kappa), cS);
                Cv2.Exp(east.Mul(east) / (-kappa * kappa), cE);
                Cv2.Exp(west.Mul(west) / (-kappa * kappa), cW);

                // 计算扩散项
                Mat diffusion = new Mat(current.Size(), MatType.CV_32F, Scalar.All(0));
                Cv2.Add(diffusion, cN.Mul(north), diffusion);
                Cv2.Add(diffusion, cS.Mul(south), diffusion);
                Cv2.Add(diffusion, cE.Mul(east), diffusion);
                Cv2.Add(diffusion, cW.Mul(west), diffusion);

                // 更新图像 I = I + lambda * diffusion
                current += diffusion * lambda;
            }

            // 转回8位图像（如需）
            Mat result = new Mat();
            current.ConvertTo(result, input.Type());
            return result;
        }


        // 白平衡矫正
        public Mat WhiteBalance(Mat input, Rect? roi = null)
        {
            if (input == null || input.Empty())
                throw new ArgumentNullException(nameof(input), "输入图像不能为空");

            if (input.Channels() != 3)
                throw new NotSupportedException("白平衡矫正仅支持三通道图像");

            // 如果指定了 ROI（感兴趣区域），则只对该区域计算白平衡
            Mat regionOfInterest = input;
            if (roi.HasValue)
            {
                regionOfInterest = new Mat(input, roi.Value);
            }

            // 计算每个通道的平均值
            Scalar meanScalar = Cv2.Mean(regionOfInterest);
            double meanB = meanScalar.Val0; // 蓝色通道平均值
            double meanG = meanScalar.Val1; // 绿色通道平均值
            double meanR = meanScalar.Val2; // 红色通道平均值

            // 计算增益系数
            double meanGray = (meanB + meanG + meanR) / 3.0;
            double gainB = meanGray / meanB;
            double gainG = meanGray / meanG;
            double gainR = meanGray / meanR;

            // 分离通道
            Mat[] channels = Cv2.Split(input);

            // 调整每个通道的增益
            channels[0] *= gainB; // 蓝色通道
            channels[1] *= gainG; // 绿色通道
            channels[2] *= gainR; // 红色通道

            // 合并通道
            Mat output = new Mat();
            Cv2.Merge(channels, output);

            return output;
        }

        public Mat AdjustHighlight(Mat input, double highlightFactor)
        {
            if (input == null || input.Empty())
                throw new ArgumentNullException(nameof(input), "输入图像不能为空");

            if (input.Channels() != 3)
                throw new NotSupportedException("高光调整仅支持三通道图像");

            // 转换到 HSV 色空间以调整亮度
            Mat hsvImage = new Mat();
            Cv2.CvtColor(input, hsvImage, ColorConversionCodes.BGR2HSV);

            // 分离 HSV 通道
            Mat[] hsvChannels = Cv2.Split(hsvImage);

            // 获取亮度通道 (V 通道)
            Mat valueChannel = hsvChannels[2];

            // 调整高光：通过非线性变换控制亮度
            Mat adjustedValue = new Mat();
            valueChannel.ConvertTo(adjustedValue, MatType.CV_8UC1, 1.0, 0);

            // 应用高光调整公式：V' = V * (1 + factor * (V/255)^2)
            // 当 factor < 0 时抑制高光，当 factor > 0 时增强高光
            for (int i = 0; i < adjustedValue.Rows; i++)
            {
                for (int j = 0; j < adjustedValue.Cols; j++)
                {
                    byte pixelValue = adjustedValue.At<byte>(i, j);
                    float normalizedValue = pixelValue / 255.0f;
                    double adjustment = 1.0f + highlightFactor * normalizedValue * normalizedValue;
                    int newValue = (int)(pixelValue * adjustment);
                    newValue = Math.Min(255, Math.Max(0, newValue)); // 限制在 0-255 范围内
                    adjustedValue.Set(i, j, (byte)newValue);
                }
            }

            // 更新亮度通道
            hsvChannels[2] = adjustedValue;

            // 合并 HSV 通道
            Mat adjustedHsv = new Mat();
            Cv2.Merge(hsvChannels, adjustedHsv);

            // 转换回 BGR 色空间
            Mat output = new Mat();
            Cv2.CvtColor(adjustedHsv, output, ColorConversionCodes.HSV2BGR);

            return output;
        }

        public Mat SharpenImage(Mat input, double sharpenFactor)
        {
            if (input == null || input.Empty())
                throw new ArgumentNullException(nameof(input), "输入图像不能为空");

            if (input.Channels() != 3)
                throw new NotSupportedException("图像锐化仅支持三通道图像");

            // 转换为灰度图以进行边缘检测
            Mat gray = new Mat();
            Cv2.CvtColor(input, gray, ColorConversionCodes.BGR2GRAY);

            // 使用 Canny 边缘检测生成边缘掩码
            Mat edges = new Mat();
            Cv2.Canny(gray, edges, 80, 200); // 阈值可调，50 和 150 是常见值

            // 扩展边缘区域（可选，增加锐化区域的宽度）
            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(3, 3));
            Cv2.Dilate(edges, edges, kernel);

            // 转换为三通道掩码以匹配输入图像
            Mat edgeMask = new Mat();
            Cv2.CvtColor(edges, edgeMask, ColorConversionCodes.GRAY2BGR);

            // 应用拉普拉斯算子进行锐化
            Mat laplacian = new Mat();
            Cv2.Laplacian(gray, laplacian, MatType.CV_16S, ksize: 3);

            // 将拉普拉斯结果转换回 8 位
            Mat laplacian8u = new Mat();
            laplacian.ConvertTo(laplacian8u, MatType.CV_8U);

            // 转换为三通道
            Mat laplacianBgr = new Mat();
            Cv2.CvtColor(laplacian8u, laplacianBgr, ColorConversionCodes.GRAY2BGR);

            // 创建锐化图像：仅在边缘区域应用锐化
            Mat sharpened = new Mat();
            Cv2.AddWeighted(input, 1.0, laplacianBgr, sharpenFactor, 0.0, sharpened);

            // 使用边缘掩码混合原图和锐化图像
            Mat output = new Mat();
            Cv2.BitwiseAnd(sharpened, edgeMask, output); // 边缘区域取锐化结果
            Mat nonEdgeMask = new Mat();
            Cv2.BitwiseNot(edgeMask, nonEdgeMask); // 非边缘区域掩码
            Mat nonEdge = new Mat();
            Cv2.BitwiseAnd(input, nonEdgeMask, nonEdge); // 非边缘区域取原图
            Cv2.Add(output, nonEdge, output); // 合并边缘和非边缘区域

            // 确保输出像素值在 0-255 范围内
            Cv2.MinMaxLoc(output, out double minVal, out double maxVal);
            if (minVal < 0 || maxVal > 255)
            {
                output = output.Normalize(0, 255, NormTypes.MinMax, (int)MatType.CV_8UC3);
            }

            return output;
        }

        public float[] Softmax(float[] logits)
        {
            float[] probs = new float[logits.Length];
            float maxLogit = float.MinValue;
            for (int i = 0; i < logits.Length; i++)
            {
                if (logits[i] > maxLogit) maxLogit = logits[i];
            }

            float sum = 0;
            for (int i = 0; i < logits.Length; i++)
            {
                probs[i] = (float)Math.Exp(logits[i] - maxLogit); // 防止溢出
                sum += probs[i];
            }

            for (int i = 0; i < probs.Length; i++)
            {
                probs[i] /= sum;
            }

            return probs;
        }

        // 创建透明叠加层
        public Bitmap CreateTransparentOverlay(Tensor<float> output)
        {
            var overlay = new Bitmap(512, 512);
            var imageProcess = new ImageProcess();

            for (int y = 0; y < 512; y++)
            {
                for (int x = 0; x < 512; x++)
                {
                    // 获取所有类别的logits
                    float[] logits = new float[4];
                    for (int c = 0; c < 4; c++)
                    {
                        logits[c] = output[0, c, y, x];
                    }

                    // 应用softmax
                    float[] probs = imageProcess.Softmax(logits);

                    // 获取最大概率和对应类别
                    int predictedClass = 0;
                    float maxProb = probs[0];
                    for (int c = 1; c < 4; c++)
                    {
                        if (probs[c] > maxProb)
                        {
                            maxProb = probs[c];
                            predictedClass = c;
                        }
                    }

                    // 将最大概率映射为灰度值（0到255）
                    int grayValue = (int)(maxProb * 255);

                    // 颜色映射：灰度值决定RGB，透明度保持不变
                    Color color = predictedClass switch
                    {
                        1 => Color.FromArgb(128, grayValue, 0, 0), // 红色类别，灰度
                        2 => Color.FromArgb(128, 0, grayValue, 0), // 绿色类别，灰度
                        3 => Color.FromArgb(128, 0, 0, grayValue), // 蓝色类别，灰度
                        _ => Color.Transparent                             // 背景透明
                    };

                    overlay.SetPixel(x, y, color);
                }
            }

            return overlay;
        }
    }
}