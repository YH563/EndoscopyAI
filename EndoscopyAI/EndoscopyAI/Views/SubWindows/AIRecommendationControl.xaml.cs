using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using EndoscopyAI.Services;
using EndoscopyAI.ViewModels;
using OpenCvSharp;
using EndoscopyAI.ViewModels.SubViewModels;

namespace EndoscopyAI.Views.SubWindows
{
    /// <summary>
    /// AIRecommendationControl.xaml 的交互逻辑
    /// </summary>
    public partial class AIRecommendationControl : UserControl
    {

        // 用于加载和显示图像的实例
        private readonly IImageDisplay _imageDisplay;

        // 用于存储图像处理的 ViewModel
        private ImageProcessViewModel _imageProcessViewModel = new ImageProcessViewModel();

        IImageProcess imageProcess = new ImageProcessViewModel();  // 创建图像处理实例

        ImageProcess imgAlgorithm = new ImageProcess(); // 创建图像处理算法实例

        // 用于存储分割结果的叠加图像
        private BitmapSource segmentationOverlay;

        public AIRecommendationControl()
        {
            InitializeComponent();
            _imageDisplay = new ImageDisplay(); // 初始化 ImageDisplay 实例
            DataContext = this; // 设置数据上下文为自身
        }

        // 图像增强
        private void ImgEnhance(object sender, RoutedEventArgs e)
        {
            if (DataSharingService.Instance.ProcessedImage == null || _imageDisplay == null)
            {
                MessageBox.Show("尚未导入图像！", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                DataSharingService.Instance.ProcessedImage = _imageProcessViewModel.ImgEnhance(DataSharingService.Instance.ProcessedImage); // 更新 _currentImage
            }
            catch (Exception ex)
            {
                MessageBox.Show($"图像增强时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 图像分类预测
        private void ClassPredict(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(DataSharingService.Instance.Patient.ImagePath))
            {
                MessageBox.Show("请先上传一张图片。");
                return;
            }

            try
            {
                var (predictedClass, confidence) = imageProcess.ImgClassify(DataSharingService.Instance.Patient.ImagePath);
                DataSharingService.Instance.Patient.AIResult = predictedClass;
                DataSharingService.Instance.Patient.AIConfidenceLevel = confidence;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"分类预测失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 图像分割预测
        private void SegmentPredict(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(DataSharingService.Instance.Patient.ImagePath))
            {
                MessageBox.Show("请先上传一张图片。");
                return;
            }

            try
            {
                // 执行分割预测
                var result = _imageProcessViewModel.ImgSegment(DataSharingService.Instance.Patient.ImagePath);

                // 创建透明叠加层
                var overlay = imgAlgorithm.CreateTransparentOverlay(result.OutputTensor);

                // 转换为BitmapSource
                segmentationOverlay = Imaging.CreateBitmapSourceFromHBitmap(
                    overlay.GetHbitmap(),
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromWidthAndHeight(512, 512));

                // 直接修改原始图像的Source，添加叠加效果
                var original = new Bitmap(DataSharingService.Instance.Patient.ImagePath);
                var originalSource = Imaging.CreateBitmapSourceFromHBitmap(
                    original.GetHbitmap(),
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromWidthAndHeight(original.Width, original.Height));

                // 使用DrawingVisual组合图像
                var visual = new DrawingVisual();
                using (var context = visual.RenderOpen())
                {
                    context.DrawImage(originalSource, new System.Windows.Rect(0, 0, original.Width, original.Height));
                    context.DrawImage(segmentationOverlay, new System.Windows.Rect(0, 0, 512, 512));
                }

                var combined = new RenderTargetBitmap(
                    original.Width, original.Height, 96, 96, PixelFormats.Pbgra32);
                combined.Render(visual);

                var combined_mat = imgAlgorithm.ConvertRenderTargetBitmapToMat(combined);

                // 将合成后的图像发送到共享服务
                DataSharingService.Instance.ProcessedImage = combined_mat;

                // 创建黑白掩码图
                var maskBitmap = new Bitmap(512, 512);
                for (int y = 0; y < 512; y++)
                {
                    for (int x = 0; x < 512; x++)
                    {
                        var pixel = overlay.GetPixel(x, y);
                        // 如果原像素是透明的（正常区域），设置为黑色；否则设置为白色
                        maskBitmap.SetPixel(x, y, pixel.A == 0 ? System.Drawing.Color.Black : System.Drawing.Color.White);
                    }
                }

                // 转换为BitmapSource
                var maskSource = Imaging.CreateBitmapSourceFromHBitmap(
                    maskBitmap.GetHbitmap(),
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromWidthAndHeight(512, 512));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"分割预测失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
