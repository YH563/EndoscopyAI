using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EndoscopyAI.ViewModels
{
    interface IImageProcess
    {
        // 图像增强，目前只使用多通道直方图均衡化一种
        Mat HistogramEqualization(Mat input);

        // 图像降噪，各向异性扩散滤波
        Mat AnisotropicDiffusion(Mat input, int iterations = 10, float kappa = 50.0f);

        // 白平衡矫正
        Mat WhiteBalance(Mat input, Rect? roi = null);
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

        // 占位实现：图像降噪
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
        private Mat ApplyAnisotropicDiffusion(Mat input, int iterations, float kappa, float lambda = 0.25f)
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

    }
}