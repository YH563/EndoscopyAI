using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace EndoscopyAI.Views.SubWindows
{
    public partial class InformationStatistics : Window
    {
        //
        // TODO：在此确定近五天的患者数量数据
        //
        private readonly int[] patientCounts = { 36, 25, 19, 21, 32 };
        //
        // TODO：在此确定近五天的日期
        //
        private readonly string[] days = { "周一", "周二", "周三", "周四", "周五" };

        public InformationStatistics()
        {
            InitializeComponent();

            // 感觉不好确定，这里就做一个假的吧(._.`)
            PendingTasksText.Text = "114";     // 待处理任务数
            CompletedTasksText.Text = "51";   // 已完成任务数
            AbnormalCasesText.Text = "4";    // 异常病例数

            // 监听窗口加载完成事件
            Loaded += (s, e) => DrawBarChart();

            // 监听窗口大小变化和柱状图画布大小变化
            SizeChanged += (s, e) => DrawBarChart();
            BarChartCanvas.SizeChanged += (s, e) => DrawBarChart();
        }

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
                var rect = new Rectangle
                {
                    Width = barWidth,
                    Height = barHeight,
                    Fill = new SolidColorBrush(Color.FromRgb(25, 118, 210)), // #1976d2
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
                    Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
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
                    Foreground = Brushes.Gray,
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
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 0.5,
                    StrokeDashArray = new DoubleCollection { 4, 2 }
                };
                BarChartCanvas.Children.Add(line);

                var label = new TextBlock
                {
                    Text = value.ToString("0"),
                    FontSize = 10,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(0, 0, 5, 0)
                };
                Canvas.SetRight(label, BarChartCanvas.ActualWidth - 5);
                Canvas.SetTop(label, yPosition - 10);
                BarChartCanvas.Children.Add(label);
            }
        }
    }
}