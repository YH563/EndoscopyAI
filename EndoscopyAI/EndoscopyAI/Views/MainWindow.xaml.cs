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
using System.ComponentModel;
using System.Drawing;
using Microsoft.ML.OnnxRuntime.Tensors;
using EndoscopyAI.Services;
using EndoscopyAI.ViewModels.SubViewModels;

namespace EndoscopyAI.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window, INotifyPropertyChanged
    {
        // 用于加载和保存图像的实例
        private readonly IImageDisplay _imageDisplay = new ImageDisplay();

        public event PropertyChangedEventHandler? PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();

            //// 初始设置为最大化
            //this.WindowState = WindowState.Maximized;

            //// 限制调整模式为仅允许最小化
            //this.ResizeMode = ResizeMode.CanMinimize;

            DataSharingService.Instance.ImageChanged += OnImageChanged;

        }

        private void OnImageChanged(object? sender, EventArgs e)
        {
            // 处理图像变化事件
            if (DataSharingService.Instance.ProcessedImage != null)
            {
                // 显示处理后的图像
                _imageDisplay.DisplayImage(ImageDisplay, DataSharingService.Instance.ProcessedImage);
            }
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
                    DataSharingService.Instance.Patient.ImagePath = openFileDialog.FileName;
                    DataSharingService.Instance.ProcessedImage = _imageDisplay.LoadImageFromFile(DataSharingService.Instance.Patient.ImagePath);// 加载图像并存储到 _currentImage

                    // 在 Image 控件中显示图像
                    _imageDisplay.DisplayImage(ImageDisplay, DataSharingService.Instance.ProcessedImage);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"加载图像时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public Mat ConvertBitmapSourceToMat(BitmapSource bitmapSource)
        {
            if (bitmapSource == null)
                return null;
            return OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToMat(bitmapSource);
        }

        // 保存图像文件
        private void SaveFile(object sender, RoutedEventArgs e)
        {

            if (DataSharingService.Instance.ProcessedImage == null)
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
                    bool success = _imageDisplay.ImageSave(DataSharingService.Instance.ProcessedImage, saveFileDialog.FileName);
                    DataSharingService.Instance.Patient.ImagePath = saveFileDialog.FileName;
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

        private void ImgReset(object sender, RoutedEventArgs e)
        {
            if (DataSharingService.Instance.Patient.ImagePath != null)
            {
                // 重新导入原始图像
                DataSharingService.Instance.ProcessedImage = _imageDisplay.LoadImageFromFile(DataSharingService.Instance.Patient.ImagePath);
                _imageDisplay.DisplayImage(ImageDisplay, DataSharingService.Instance.ProcessedImage);
            }
            else
            {
                MessageBox.Show("没有原始图像可重置", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}