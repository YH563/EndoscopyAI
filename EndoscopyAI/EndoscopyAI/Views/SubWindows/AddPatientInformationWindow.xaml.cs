using EndoscopyAI.Services;
using EndoscopyAI.ViewModels.SubViewModels;
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

namespace EndoscopyAI.Views.SubWindows
{
    /// <summary>
    /// AddPatientInformationWindow.xaml 的交互逻辑
    /// </summary>
    public partial class AddPatientInformationWindow : Window
    {
        Patient? patient = new Patient();  // 病人信息类实例
        IPatientInformation patientInformation = new PatientInformation();   // 病人信息接口实例

        public AddPatientInformationWindow()
        {
            InitializeComponent();
        }

        // 确认按钮
        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if(patient != null)
            {
                patient.Name = PatientName.Text;
                patient.Age = int.Parse(string.IsNullOrEmpty(PatientAge.Text)? "0": PatientAge.Text);
                if (MaleRadio.IsChecked == true) patient.Gender = "男";
                else patient.Gender = "女";
                patient.Contact = PatientContact.Text;
                patient.NumberID = PatientNumberID.Text;

                // 添加病人信息
                if(patientInformation.AddPatientInformation(patient))
                {
                    this.Close();
                }
            }
        }

        // 取消按钮
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
