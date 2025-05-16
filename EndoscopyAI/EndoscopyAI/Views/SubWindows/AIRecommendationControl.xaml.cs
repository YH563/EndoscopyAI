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
using OnnxImageClassifierWPF;
using EndoscopyAI.ViewModels.SubViewModels;

namespace EndoscopyAI.Views.SubWindows
{
    /// <summary>
    /// AIRecommendationControl.xaml 的交互逻辑
    /// </summary>
    public partial class AIRecommendationControl : UserControl, INotifyPropertyChanged
    {
        // 用于存储图像路径 - 该变量可能需要通过服务或其他方式获取
        private string imagePath;

        // 用于加载和显示图像的实例
        private readonly IImageDisplay _imageDisplay;

        // 用于存储当前加载的图像
        private Mat _currentImage;

        // 用于存储原始图像
        private Mat _originImage;

        // 用于存储分类器
        private OnnxClassifier classifier = new OnnxClassifier(
            Path.Combine(
                 AppDomain.CurrentDomain.BaseDirectory,  // 指向 bin\Debug
                "PredModels",
                "ClassifyModel.onnx"
            )
        );

        // 用于存储分割器
        private OnnxSegmenter segmenter = new OnnxSegmenter(
            Path.Combine(
                 AppDomain.CurrentDomain.BaseDirectory,  // 指向 bin\Debug
                "PredModels",
                "SegmentModel.onnx"
            )
        );

        // 用于存储分割结果的叠加图像
        private BitmapSource segmentationOverlay;

        // 属性变更通知事件
        public event PropertyChangedEventHandler PropertyChanged;

        // 触发属性变更通知
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public AIRecommendationControl()
        {
            InitializeComponent();
            _imageDisplay = new ImageDisplay(); // 初始化 ImageDisplay 实例
            DataContext = this; // 设置数据上下文为自身

            // 订阅共享数据变化事件，以便更新当前图像
            DataSharingService.Instance.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(DataSharingService.Instance.ImagePath))
                {
                    // 当共享服务中的图像路径变化时，更新本地图像
                    imagePath = DataSharingService.Instance.ImagePath;
                    if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
                    {
                        try
                        {
                            _currentImage = _imageDisplay.LoadImageFromFile(imagePath);
                            _originImage = _currentImage.Clone();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"加载图像时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            };
        }

        private void SyncImagePath()
        {
            imagePath = DataSharingService.Instance.ImagePath;
            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                if (_currentImage == null || _originImage == null)
                {
                    try
                    {
                        _currentImage = _imageDisplay.LoadImageFromFile(imagePath);
                        _originImage = _currentImage.Clone();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"加载图像时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        // 图像增强
        private void ImgEnhance(object sender, RoutedEventArgs e)
        {
            SyncImagePath();
            if (_currentImage == null || _imageDisplay == null)
            {
                MessageBox.Show("尚未导入图像！", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 图像增强处理
                var imageProcess = new ImageProcess();
                _currentImage = imageProcess.HistogramEqualization(_currentImage); // 更新 _currentImage

                // 通知主窗口显示增强后的图像
                DataSharingService.Instance.ProcessedImage = _imageDisplay.ConvertMatToBitmapSource(_currentImage);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"图像增强时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 图像分类预测
        private void ClassPredict(object sender, RoutedEventArgs e)
        {
            SyncImagePath();
            if (string.IsNullOrEmpty(imagePath))
            {
                MessageBox.Show("请先上传一张图片。");
                return;
            }

            try
            {
                var (predictedClass, confidence) = classifier.Predict(imagePath);
                DataSharingService.Instance.DiagnosisResult = predictedClass;
                DataSharingService.Instance.Confidence = confidence;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"分类预测失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 图像分割预测
        private void SegmentPredict(object sender, RoutedEventArgs e)
        {
            SyncImagePath();
            if (string.IsNullOrEmpty(imagePath))
            {
                MessageBox.Show("请先上传一张图片。");
                return;
            }

            try
            {
                // 执行分割预测
                var result = segmenter.Predict(imagePath);

                var imageProcess = new ImageProcess();

                // 创建透明叠加层
                var overlay = imageProcess.CreateTransparentOverlay(result.OutputTensor);

                // 转换为BitmapSource
                segmentationOverlay = Imaging.CreateBitmapSourceFromHBitmap(
                    overlay.GetHbitmap(),
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromWidthAndHeight(512, 512));

                // 直接修改原始图像的Source，添加叠加效果
                var original = new Bitmap(imagePath);
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

                // 将合成后的图像发送到共享服务
                DataSharingService.Instance.ProcessedImage = combined;

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

                // 展示掩码图像
                DataSharingService.Instance.SegmentationImage = maskSource;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"分割预测失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
