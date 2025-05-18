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
        // 病人信息实例
        Patient patient = new Patient();
        // 病人信息接口实例
        IPatientInformation patientInformation = new PatientInformation();  
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
        // 用于存储分割器
        private OnnxSegmenter segmenter = new OnnxSegmenter(
            System.IO.Path.Combine(
                 AppDomain.CurrentDomain.BaseDirectory,  // 指向 bin\Debug
                "PredModels",
                "SegmentModel.onnx"
            )
        );
        // 用于存储分割结果的叠加图像
        private BitmapSource segmentationOverlay;

        public event PropertyChangedEventHandler? PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();
            _imageDisplay = new ImageDisplay(); // 初始化 ImageDisplay 实例
            DataContext = this; // DataContext 设置为自身

            // 监听增强/分割等处理后的图像变化
            //DataSharingService.Instance.PropertyChanged += (sender, e) =>
            //{
            //    if (e.PropertyName == nameof(DataSharingService.Instance.ProcessedImage))
            //    {
            //        // 这里需要在UI线程更新Image控件
            //        Dispatcher.Invoke(() =>
            //        {
            //            var processed = DataSharingService.Instance.ProcessedImage;
            //            if (processed is BitmapSource bitmapSource)
            //            {
            //                ImageDisplay.Source = bitmapSource;
            //            }
            //        });
            //    }
            //};
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

            // 更新共享服务中的图像路径
            DataSharingService.Instance.ImagePath = imagePath;
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
            // 更新现有数据
            //imagePath = DataSharingService.Instance.ImagePath;
            //var bitmap_temp = DataSharingService.Instance.ProcessedImage;
            //_currentImage = ConvertBitmapSourceToMat(bitmap_temp as BitmapSource);

            //if (_currentImage == null)
            //{
            //    MessageBox.Show("没有图像可保存", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            //    return;
            //}

            //// 打开文件对话框
            //SaveFileDialog saveFileDialog = new SaveFileDialog
            //{
            //    Filter = "JPEG Image|*.jpg;*.jpeg|PNG Image|*.png|BMP Image|*.bmp"
            //};

            //if (saveFileDialog.ShowDialog() == true)
            //{
            //    try
            //    {
            //        // 保存当前图像
            //        bool success = _imageDisplay.ImageSave(_currentImage, saveFileDialog.FileName);
            //        DataSharingService.Instance.ImagePath = saveFileDialog.FileName;
            //        if (success)
            //        {
            //            MessageBox.Show("图像保存成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            //        }
            //        else
            //        {
            //            MessageBox.Show("图像保存失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        MessageBox.Show($"保存图像时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            //    }
            //}
        }

        private void ImgReset(object sender, RoutedEventArgs e)
        {
            //if (_originImage != null)
            //{
            //    _currentImage = _originImage.Clone(); // 克隆原始图像
            //    DataSharingService.Instance.ProcessedImage = _currentImage;
            //    _imageDisplay.DisplayImage(ImageDisplay, _currentImage);
            //}
            //else
            //{
            //    MessageBox.Show("没有原始图像可重置", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            //}
        }
    }
}