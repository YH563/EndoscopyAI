﻿using Microsoft.Win32;
using OpenCvSharp;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using EndoscopyAI.ViewModels;
using OnnxImageClassifierWPF;
using System.ComponentModel;

namespace EndoscopyAI.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window, INotifyPropertyChanged
    {
        // 用于存储图像路径
        private string imagePath;
        // 用于加载和保存图像的实例
        private readonly IImageDisplay _imageDisplay;
        // 用于存储当前加载的图像
        private Mat _currentImage;
        // 用于存储原始图像
        private Mat _originImage;
        // 用于存储分类器
        private OnnxClassifier classifier = new OnnxClassifier(
            System.IO.Path.Combine(
                 AppDomain.CurrentDomain.BaseDirectory,  // 指向 bin\Debug
                "PredModels",
                "ClassifyModel.onnx"
            )
        );
        // 用于存储分类标签
        private string[] labels = { "Barrett", "Cancer", "Inflammation", "Normal" };
        // 用于存储滑动条的可见性状态
        private bool _isSliderVisible;
        public bool IsSliderVisible
        {
            get => _isSliderVisible;
            set
            {
                _isSliderVisible = value;
                OnPropertyChanged(nameof(IsSliderVisible));
            }
        }
        // 用于存储滑动条的值
        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();
            _imageDisplay = new ImageDisplay(); // 初始化 ImageDisplay 实例
            DataContext = this; // DataContext 设置为自身
            IsSliderVisible = false; // 默认隐藏滑动条
        }

        // 加载图像文件
        private void LoadFile(object sender, RoutedEventArgs e)
        {
            // 打开文件对话框
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    imagePath = openFileDialog.FileName;
                    _currentImage = _imageDisplay.LoadImageFromFile(openFileDialog.FileName);// 加载图像并存储到 _currentImage
                    _originImage = _currentImage.Clone(); // 克隆原始图像

                    // 在 Image 控件中显示图像
                    _imageDisplay.DisplayImage(ImageDisplay, _currentImage);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"加载图像时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // 保存图像文件
        private void SaveFile(object sender, RoutedEventArgs e)
        {
            if (_currentImage == null)
            {
                MessageBox.Show("没有图像可保存", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 打开文件对话框
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "JPEG Image|*.jpg;*.jpeg|PNG Image|*.png|BMP Image|*.bmp"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // 保存当前图像
                    bool success = _imageDisplay.ImageSave(_currentImage, saveFileDialog.FileName);
                    if (success)
                    {
                        MessageBox.Show("图像保存成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("图像保存失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存图像时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // 图像增强
        private void ImgEnhance(object sender, RoutedEventArgs e)
        {
            if (_currentImage == null)
            {
                MessageBox.Show("尚未导入图像！", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                // 图像增强处理
                var imageProcess = new ImageProcess();
                _currentImage = imageProcess.HistogramEqualization(_currentImage); // 更新 _currentImage

                // 显示增强后的图像
                _imageDisplay.DisplayImage(ImageDisplay, _currentImage);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"图像增强时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 图像去噪
        private void ImgDenoise(object sender, RoutedEventArgs e)
        {
            if (_currentImage == null)
            {
                MessageBox.Show("尚未导入图像！", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                // 图像去噪处理
                var imageProcess = new ImageProcess();
                _currentImage = imageProcess.AnisotropicDiffusion(_currentImage, iterations: 15, kappa: 30.0f); // 更新 _currentImage

                // 显示去噪后的图像
                _imageDisplay.DisplayImage(ImageDisplay, _currentImage);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"图像去噪时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 图像重置
        private void ImgReset(object sender, RoutedEventArgs e)
        {
            if (_originImage != null)
            {
                _currentImage = _originImage.Clone(); // 克隆原始图像
                _imageDisplay.DisplayImage(ImageDisplay, _currentImage);
            }
            else
            {
                MessageBox.Show("没有原始图像可重置", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // 图像分类
        private void ClassPredict(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                MessageBox.Show("请先上传一张图片。");
                return;
            }
            var (predictedClass, confidence) = classifier.Predict(imagePath);
            ClassifyResult.Text = $"预测分类：{labels[predictedClass]}";
            ClassifyConfidence.Text = $"分类置信度：{confidence:F2}%";
        }

        // 照度调节（调整滑动条状态）
        private void LightAugest(object sender, RoutedEventArgs e)
        {
            // 切换滑动条显示状态
            IsSliderVisible = !IsSliderVisible;
        }

        // 照度调节滑动条值变化事件
        private void BalanceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // 处理滑动条值变化，调整图像的参数
            double sliderValue = BalanceSlider.Value;
            // map
            double highlight_factor = sliderValue / 100.0 * 2 - 1; 
            // 进行处理
            if (_currentImage == null)
            {
                MessageBox.Show("尚未导入图像！", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                // 先恢复原图
                _currentImage = _originImage.Clone();

                // 图像增强处理
                var imageProcess = new ImageProcess();
                _currentImage = imageProcess.AdjustHighlight(_currentImage, highlight_factor); // 更新 _currentImage

                // 显示增强后的图像
                _imageDisplay.DisplayImage(ImageDisplay, _currentImage);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"图像照度调节时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        // 关闭窗口事件
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}