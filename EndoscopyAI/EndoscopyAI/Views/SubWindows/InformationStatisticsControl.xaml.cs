using EndoscopyAI.ViewModels.SubViewModels;
using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace EndoscopyAI.Views.SubWindows
{
    public partial class InformationStatisticsControl : UserControl
    {
        IPatientInformation patientInformation = new PatientInformation();

        public SeriesCollection SeriesCollection { get; set; }
        public string[] Labels { get; set; }
        public Func<double, string> Formatter { get; set; }

        public InformationStatisticsControl()
        {
            InitializeComponent();

            // 感觉不好确定，这里就做一个假的吧(._.`)
            PendingTasksText.Text = "114";     // 待处理任务数
            CompletedTasksText.Text = "51";    // 已完成任务数
            AbnormalCasesText.Text = "4";      // 异常病例数
            FinalText.Text = "19";             // 最终诊断数

            // 初始化数据 - 这里使用示例数据，实际使用时可以从数据库获取
            var visitData = GetRecentVisitData();

            // 设置图表数据
            SeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "就诊人数",
                    Values = new ChartValues<int>(visitData.Select(x => x.Count).ToList()),
                    StrokeThickness = 2,
                    PointGeometrySize = 8,
                    Fill = System.Windows.Media.Brushes.Transparent
                }
            };

            // 设置X轴标签（日期）
            Labels = visitData.Select(x => x.Date.ToString("MM-dd")).ToArray();

            // 格式化X轴标签
            Formatter = value => value >= 0 && value < Labels.Length ? Labels[(int)value] : "";

            DataContext = this;
        }

        private List<(DateTime Date, int Count)> GetRecentVisitData()
        {
            var random = new Random();
            var today = DateTime.Today;
            List<int> patientCounts = patientInformation.GetPatientCountByDay(7);

            var data = new List<(DateTime, int)>();
            for (int i = 6; i >= 0; i--)
            {
                // 模拟数据：工作日人数较多，周末较少
                var date = today.AddDays(-i);
                var count = patientCounts[i];
                data.Add((date, count));
            }

            return data;
        }
    }
}
