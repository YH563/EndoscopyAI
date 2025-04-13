using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using OpenCvSharp;
using System.Windows.Media.Imaging;
using System.Windows.Controls;

namespace EndoscopyAI.ViewModels
{
    interface IImageDisplay
    {
        // 通过路径读取文件
        Mat LoadImageFromFile(string filename);

        // 在WPF Image中显示图像
        void DisplayImage(Image imageControl, Mat image, BitmapScalingMode scalingMode = BitmapScalingMode.HighQuality);

        // 保存图像
        bool ImageSave(Mat image, string filePath, int? quality = null);
    }


}
