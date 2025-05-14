using EndoscopyAI.Services;
using EndoscopyAI.ViewModels.SubViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EndoscopyAI.Views
{
    /// <summary>
    /// PatientInformationControl.xaml 的交互逻辑
    /// </summary>
    public partial class PatientInformationControl : UserControl
    {
        Patient patient = new Patient();  // 病人信息实例
        IPatientInformation patientInformation = new PatientInformation();  // 病人信息接口实例

        public PatientInformationControl()
        {
            InitializeComponent();
        }

        // 保存病人信息
        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            patient.Name = stringName.Text;
            patient.Age = string.IsNullOrEmpty(intAge.Text) ? 0 : int.Parse(intAge.Text);
            if (option1.IsChecked == true)
            {
                patient.Gender = option1.Content?.ToString() ?? "";
            }
            else if (option2.IsChecked == true)
            {
                patient.Gender = option2.Content?.ToString() ?? "";
            }
            patient.Contact = stringContact.Text;
            patient.NumberID = stringNumberID.Text;
            patient.DiagnosisResult = DataSharingService.Instance.DiagnosisResult;
            patient.ConfidenceLevel = DataSharingService.Instance.Confidence;
            patient.ImagePath = DataSharingService.Instance.ImagePath;
            //patient.HeatMapPath = DataSharingService.Instance.HeatMapPath;

            patientInformation.UpdatePatient(patient);
        }

    }
}
