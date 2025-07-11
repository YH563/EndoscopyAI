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
using EndoscopyAI.Views.SubWindows;

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

            DataSharingService.Instance.ImageChanged += OnImageChanged;

            // 初始显示统计视图
            StatisticsViewControl.Visibility = Visibility.Visible;
            ThreeColumnView.Visibility = Visibility.Collapsed;
            ToggleViewButton.Content = "进入工作区";

            // 订阅统计视图的开始按钮点击事件
            StatisticsViewControl.StartButtonClicked += StartButton_Clicked;
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
                    DataSharingService.Instance.ProcessedImage = _imageDisplay.LoadImageFromFile(DataSharingService.Instance.Patient.ImagePath);

                    // 在 Image 控件中显示图像
                    _imageDisplay.DisplayImage(ImageDisplay, DataSharingService.Instance.ProcessedImage);

                    // 自动切换到三栏视图
                    ShowThreeColumnView();
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
            // 确保先切换到三栏视图
            ShowThreeColumnView();

            // 保存文件逻辑
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp",
                Title = "保存图像"
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

        // 重置图像
        private void ImgReset(object sender, RoutedEventArgs e)
        {
            // 确保先切换到三栏视图
            ShowThreeColumnView();

            // 重置图像逻辑
            if (DataSharingService.Instance.Patient.ImagePath != null)
            {
                try
                {
                    DataSharingService.Instance.ProcessedImage = _imageDisplay.LoadImageFromFile(DataSharingService.Instance.Patient.ImagePath);
                    _imageDisplay.DisplayImage(ImageDisplay, DataSharingService.Instance.ProcessedImage);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"重置图像时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // 切换视图按钮事件处理
        private void ToggleView_Click(object sender, RoutedEventArgs e)
        {
            if (StatisticsViewControl.Visibility == Visibility.Visible)
            {
                ShowThreeColumnView();
            }
            else
            {
                ShowStatisticsView();
            }
        }

        // 显示统计视图
        private void ShowStatisticsView()
        {
            StatisticsViewControl.Visibility = Visibility.Visible;
            ThreeColumnView.Visibility = Visibility.Collapsed;
            ToggleViewButton.Content = "进入工作区";
        }

        // 显示三栏视图
        private void ShowThreeColumnView()
        {
            StatisticsViewControl.Visibility = Visibility.Collapsed;
            ThreeColumnView.Visibility = Visibility.Visible;
            ToggleViewButton.Content = "查看统计";
        }

        // 统计视图开始按钮点击事件处理
        private void StartButton_Clicked(object sender, EventArgs e)
        {
            // 切换到三栏视图
            ShowThreeColumnView();
            var qwen = new QwenChatService(); // 如果你使用的是单文件封装版
            var mainwindow = new Window2Patients(qwen);
            mainwindow.Show();
        }
    }
}
