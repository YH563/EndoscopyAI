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
}
