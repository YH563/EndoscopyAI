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

namespace EndoscopyAI.Views.SubWindows
{
    /// <summary>
    /// PatientInformationControl.xaml 的交互逻辑
    /// </summary>
    public partial class PatientInformationControl : UserControl
    {
        Patient? patient = new Patient();  // 病人信息实例
        IPatientInformation patientInformation = new PatientInformation();  // 病人信息接口实例

        public PatientInformationControl()
        {
            InitializeComponent();
            patient = patientInformation.GetLatestPatientInformation();
            ShowPatientInformation();
            DataSharingService.Instance.PatientChanged += OnPatientDataChanged;
        }

        // 展示病人信息
        private void ShowPatientInformation()
        {
            if (patient != null)
            {
                PatientID.Text = patient.ID.ToString();
                PatientName.Text = patient.Name;
                PatientAge.Text = patient.Age.ToString();
                PatientGender.Text = patient.Gender;
                PatientNumberID.Text = patient.NumberID;
                PatientContact.Text = patient.Contact;
                PatientVisitTime.Text = patient.VisitTime;
                PatientMedicalHistory.Text = patient.MedicalHistory;
                PatientChiefComplaint.Text = patient.ChiefComplaint;
                PatientDiagnosisResult.Text = patient.DiagnosisResult;
                PatientTreatmentPlan.Text = patient.TreatmentPlan;
            }
        }

        // 确认按钮点击事件
        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (patient != null)
            {
                // 医生输入信息
                patient.MedicalHistory = PatientMedicalHistory.Text;
                patient.ChiefComplaint = PatientChiefComplaint.Text;
                patient.DiagnosisResult = PatientDiagnosisResult.Text;
                patient.TreatmentPlan = PatientTreatmentPlan.Text;

                patientInformation.UpdatePatientInformation(patient);
            }
            else
            {
                MessageBox.Show("请先选择病人信息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        // 查找按钮点击事件
        private void Search_Click(object sender, RoutedEventArgs e)
        {
            SearchPatientInformationWindow searchPatientInformationWindow = new SearchPatientInformationWindow();
            searchPatientInformationWindow.Show();
        }

        // 病人信息变更事件
        private void OnPatientDataChanged(object? sender, EventArgs e)
        {
            patient = DataSharingService.Instance.Patient;
            ShowPatientInformation();
        }
    }
}
