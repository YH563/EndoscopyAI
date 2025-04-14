using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using OpenCvSharp;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using OpenCvSharp.WpfExtensions;

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

    public class ImageDisplay : IImageDisplay
    {
        // 实现 LoadImageFromFile 方法
        public Mat LoadImageFromFile(string filename)
        {
            try
            {
                return Cv2.ImRead(filename, ImreadModes.Color); // 使用 OpenCvSharp 加载图像
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading image: {ex.Message}");
                return null;
            }
        }

        // 实现 DisplayImage 方法
        public void DisplayImage(Image imageControl, Mat image, BitmapScalingMode scalingMode = BitmapScalingMode.HighQuality)
        {
            if (image == null || imageControl == null)
                throw new ArgumentNullException("Image or ImageControl cannot be null");

            BitmapSource bitmapSource = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(image);
            RenderOptions.SetBitmapScalingMode(imageControl, scalingMode);
            imageControl.Source = bitmapSource;
        }

        // 实现 ImageSave 方法
        public bool ImageSave(Mat image, string filePath, int? quality = null)
        {
            try
            {
                var parameters = new ImageEncodingParam(ImwriteFlags.JpegQuality, quality ?? 90);
                return Cv2.ImWrite(filePath, image, new[] { parameters });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving image: {ex.Message}");
                return false;
            }
        }
    }
}
