using OpenCvSharp;
using System.IO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime.Tensors;
using EndoscopyAI.Services;
using System.Windows;

namespace EndoscopyAI.ViewModels.SubViewModels
{
    interface IImageProcess
    {
        //图像增强
        Mat ImgEnhance(Mat input);

        // 图像分类
        (string, float) ImgClassify(string imagePath);

        // 图像分割
        SegmentationResult ImgSegment(string imagePath);
    }

    public class ImageProcessViewModel : IImageProcess
    {
        //图像增强
        public Mat ImgEnhance(Mat input)
        {
            return ImageProcess.Instance.HistogramEqualization(input);
        }

        // 图像分类
        public (string, float) ImgClassify(string imagePath)
        {
            return AIServiceImpl.Instance.ImgClassify(imagePath);
        }

        // 图像分割
        public SegmentationResult ImgSegment(string imagePath)
        {
            return AIServiceImpl.Instance.ImgSegment(imagePath);
        }
    }

}