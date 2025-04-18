using EndoscopyAI.Services;
using EndoscopyAI.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EndoscopyAI.Views
{
    /// <summary>
    /// PatientInformationControl.xaml 的交互逻辑
    /// </summary>
    public partial class PatientInformationControl : UserControl
    {
        public PatientInformationControl()
        {
            InitializeComponent();
        }

        // 保存病人信息
        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            string name = stringName.Text;
            int age = string.IsNullOrEmpty(intAge.Text) ? 0 : int.Parse(intAge.Text);
            string gender = "Unkown";
            if (option1.IsChecked == true)
            {
                gender = option1.Content?.ToString() ?? "Unkown";
            }
            else if (option2.IsChecked == true)
            {
                gender = option2.Content?.ToString() ?? "Unkown";
            }
            string contact = stringContact.Text;
            string numberID = stringNumberID.Text;
            string diagnosisResult = stringDiagnosisResult.Text;
            double confidenctLevel = string.IsNullOrEmpty(doubleConfidenceLevel.Text) ? 0 : double.Parse(doubleConfidenceLevel.Text);

            Patient patient = new Patient
            {
                Name = name,
                Age = age,
                Gender = gender,
                Contact = contact,
                NumberID = numberID,
                DiagnosisResult = diagnosisResult,
                ConfidenceLevel = confidenctLevel
            };

            PatientInformation patientInformation = new PatientInformation();
            patientInformation.UpdatePatient(patient);
        }
    }
}
