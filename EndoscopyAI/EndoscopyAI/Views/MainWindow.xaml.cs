using Microsoft.Win32;
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

namespace EndoscopyAI.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private readonly IImageDisplay _imageDisplay; // 用于加载和保存图像的实例
        private Mat _currentImage; // 用于存储当前加载的图像

        public MainWindow()
        {
            InitializeComponent();
            _imageDisplay = new ImageDisplay(); // 初始化 ImageDisplay 实例
        }

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
                    // 加载图像并存储到 _currentImage
                    _currentImage = _imageDisplay.LoadImageFromFile(openFileDialog.FileName);

                    // 在 Image 控件中显示图像
                    _imageDisplay.DisplayImage(ImageDisplay, _currentImage);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"加载图像时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

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

        private void WhiteBalance(object sender, RoutedEventArgs e)
        {
            if (_currentImage == null)
            {
                MessageBox.Show("尚未导入图像！", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                // 图像白平衡处理
                var imageProcess = new ImageProcess();
                _currentImage = imageProcess.WhiteBalance(_currentImage); // 更新 _currentImage

                // 显示白平衡调整后的图像
                _imageDisplay.DisplayImage(ImageDisplay, _currentImage);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"白平衡调整时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}