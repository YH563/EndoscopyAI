using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace EndoscopyAI.Tests
{
    /// <summary>
    /// TestWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TestWindow : Window
    {
        private PatientDataGeneration patientDataGeneration = new PatientDataGeneration();

        public TestWindow()
        {
            InitializeComponent();
        }

        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            patientDataGeneration.RandomGeneratePatientData();
            MessageBox.Show("患者信息生成成功！", "信息", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            patientDataGeneration.DeleteAllPatientInformation();
            MessageBox.Show("患者信息删除成功！", "信息", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
