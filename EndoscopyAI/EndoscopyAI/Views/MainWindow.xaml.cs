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

        // 统计图表数据
        private readonly int[] patientCounts = { 36, 25, 19, 21, 32 };
        private readonly string[] days = { "周一", "周二", "周三", "周四", "周五" };

        public event PropertyChangedEventHandler? PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();

            DataSharingService.Instance.ImageChanged += OnImageChanged;

            // 初始显示统计视图
            StatisticsView.Visibility = Visibility.Visible;
            ThreeColumnView.Visibility = Visibility.Collapsed;

            // 设置统计数据
            PendingTasksText.Text = "114";
            CompletedTasksText.Text = "51";
            AbnormalCasesText.Text = "4";

            // 监听窗口加载完成事件，绘制柱状图
            Loaded += (s, e) => DrawBarChart();
            SizeChanged += (s, e) => DrawBarChart();
            BarChartCanvas.SizeChanged += (s, e) => DrawBarChart();
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
            if (StatisticsView.Visibility == Visibility.Visible)
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
            StatisticsView.Visibility = Visibility.Visible;
            ThreeColumnView.Visibility = Visibility.Collapsed;
            ToggleViewButton.Content = "进入工作区";
            DrawBarChart(); // 更新图表
        }

        // 显示三栏视图
        private void ShowThreeColumnView()
        {
            StatisticsView.Visibility = Visibility.Collapsed;
            ThreeColumnView.Visibility = Visibility.Visible;
            ToggleViewButton.Content = "查看统计";
        }

        // 开始按钮点击事件
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            // 切换到三栏视图
            ShowThreeColumnView();

            // 打开患者选择窗口
            var Window2Patients = new Window2Patients();
            Window2Patients.Show();
        }

        // 绘制柱状图
        private void DrawBarChart()
        {
            if (BarChartCanvas.ActualWidth <= 0 || BarChartCanvas.ActualHeight <= 0)
                return;

            BarChartCanvas.Children.Clear();

            double canvasWidth = BarChartCanvas.ActualWidth;
            double canvasHeight = BarChartCanvas.ActualHeight;

            // 计算柱状图尺寸参数
            double barWidth = canvasWidth * 0.1;  // 柱宽为画布宽度的10%
            double gap = canvasWidth * 0.05;      // 柱间距为画布宽度的5%
            double maxBarHeight = canvasHeight * 0.8; // 最大柱高为画布高度的80%

            // 找出最大值用于比例计算
            double maxCount = 1;
            foreach (var v in patientCounts)
                if (v > maxCount) maxCount = v;

            // 计算起始X位置
            double totalWidth = patientCounts.Length * barWidth + (patientCounts.Length - 1) * gap;
            double startX = (canvasWidth - totalWidth) / 2;

            for (int i = 0; i < patientCounts.Length; i++)
            {
                // 计算柱高
                double barHeight = (patientCounts[i] / maxCount) * maxBarHeight;

                // 创建柱状体
                var rect = new System.Windows.Shapes.Rectangle
                {
                    Width = barWidth,
                    Height = barHeight,
                    Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(25, 118, 210)), // #1976d2
                    RadiusX = 4,
                    RadiusY = 4,
                    VerticalAlignment = VerticalAlignment.Bottom
                };

                // 定位柱状体
                Canvas.SetLeft(rect, startX + i * (barWidth + gap));
                Canvas.SetBottom(rect, 30); // 底部留出空间放日期标签
                BarChartCanvas.Children.Add(rect);

                // 数值标签
                var valueText = new TextBlock
                {
                    Text = patientCounts[i].ToString(),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(25, 118, 210)),
                    TextAlignment = TextAlignment.Center,
                    Width = barWidth
                };
                Canvas.SetLeft(valueText, startX + i * (barWidth + gap));
                Canvas.SetBottom(valueText, barHeight + 35); // 柱体上方
                BarChartCanvas.Children.Add(valueText);

                // 日期标签
                var dayText = new TextBlock
                {
                    Text = days[i],
                    FontSize = 13,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    TextAlignment = TextAlignment.Center,
                    Width = barWidth
                };
                Canvas.SetLeft(dayText, startX + i * (barWidth + gap));
                Canvas.SetBottom(dayText, 10); // 画布底部
                BarChartCanvas.Children.Add(dayText);
            }

            // 添加Y轴刻度
            AddYAxisLabels(maxCount, maxBarHeight, canvasHeight);
        }

        private void AddYAxisLabels(double maxValue, double maxBarHeight, double canvasHeight)
        {
            // 添加5个刻度
            for (int i = 0; i <= 4; i++)
            {
                double value = maxValue * i / 4;
                double yPosition = canvasHeight - 30 - (maxBarHeight * i / 4);

                var line = new Line
                {
                    X1 = 10,
                    X2 = BarChartCanvas.ActualWidth - 10,
                    Y1 = yPosition,
                    Y2 = yPosition,
                    Stroke = System.Windows.Media.Brushes.LightGray,
                    StrokeThickness = 0.5,
                    StrokeDashArray = new DoubleCollection { 4, 2 }
                };
                BarChartCanvas.Children.Add(line);

                var label = new TextBlock
                {
                    Text = value.ToString("0"),
                    FontSize = 10,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    Margin = new Thickness(0, 0, 5, 0)
                };
                Canvas.SetRight(label, BarChartCanvas.ActualWidth - 5);
                Canvas.SetTop(label, yPosition - 10);
                BarChartCanvas.Children.Add(label);
            }
        }
    }
}
