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
using System.Windows.Threading;
using static MaterialDesignThemes.Wpf.Theme;

namespace EndoscopyAI.Views.SubWindows
{
    /// <summary>
    /// PatientInformationControl.xaml 的交互逻辑
    /// </summary>
    public partial class PatientInformationControl : UserControl
    {
        Patient? patient = new Patient();  // 病人信息实例
        IPatientInformation patientInformation = new PatientInformation();  // 病人信息接口实例
        private DispatcherTimer _inputTimer;

        public PatientInformationControl()
        {
            InitializeComponent();
            InitializeInputTimer();
            patient = patientInformation.GetLatestPatientInformation();
            ShowPatientInformationDefault();

            DataSharingService.Instance.PatientChanged += OnPatientDataChanged;
            DataSharingService.Instance.ImagePathChanged += OnImagePathChanged;
            DataSharingService.Instance.ClassificationResultChanged += OnClassificationResultChanged;
            DataSharingService.Instance.ConfidenceChanged += OnConfidenceChanged;
        }

        private void ShowPatientInformationDefault()
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
                PatientChiefComplaint.Text = "";
                PatientMedicalHistory.Text = "";
                PatientDiagnosisResult.Text = "";
                PatientTreatmentPlan.Text = "";
            }
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
                if(!string.IsNullOrEmpty(patient.AIResult))
                    PatientAIResult.Text = $"{patient.AIResult}\t 置信度为:{patient.AIConfidenceLevel * 100}%";
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
                DataSharingService.Instance.Patient = patient;
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
            if(patient != DataSharingService.Instance.Patient)
            {
                patient = DataSharingService.Instance.Patient;
                ShowPatientInformationDefault();
            }
        }

        // 图片路径变更事件
        private void OnImagePathChanged(object? sender, EventArgs e)
        {
            if(patient != null && DataSharingService.Instance.ImagePath != null && DataSharingService.Instance.Patient != null)
            {
                patient.ImagePath = DataSharingService.Instance.ImagePath;
            }
        }

        // 分类结果变更事件
        private void OnClassificationResultChanged(object? sender, EventArgs e)
        {
            if(patient!=null && DataSharingService.Instance.ClassificationResult != null && DataSharingService.Instance.Patient != null)
            {
                patient.AIResult = DataSharingService.Instance.ClassificationResult;
                patient.ChiefComplaint = PatientChiefComplaint.Text;
                ShowPatientInformation();
            }
        }

        // 分类置信度变更事件
        private void OnConfidenceChanged(object? sender, EventArgs e)
        {
            if (patient != null && DataSharingService.Instance.Confidence != 0.0 && DataSharingService.Instance.Patient != null)
            {
                patient.AIConfidenceLevel = DataSharingService.Instance.Confidence;
                patient.ChiefComplaint = PatientChiefComplaint.Text;
                ShowPatientInformation();
            }
        }

        // 初始化定时器（设置延迟时间，例如500毫秒）
        private void InitializeInputTimer()
        {
            _inputTimer = new DispatcherTimer();
            _inputTimer.Interval = TimeSpan.FromMilliseconds(500); // 设置等待时间
            _inputTimer.Tick += InputTimer_Tick;
        }

        // 主诉停止输入事件
        private void PCC_Changed(object sender, TextChangedEventArgs e)
        {
            _inputTimer.Stop();     // 停止之前的定时器
            _inputTimer.Start();    // 重新开始计时
        }

        // 既往病史停止输入事件
        private void PMH_Changed(object sender, TextChangedEventArgs e)
        {
            _inputTimer.Stop();     // 停止之前的定时器
            _inputTimer.Start();    // 重新开始计时
        }

        // 定时器结束时触发（用户停止输入）
        private void InputTimer_Tick(object sender, EventArgs e)
        {
            _inputTimer.Stop();
            if (patient != null)
            {
                patient.ChiefComplaint = PatientChiefComplaint.Text;
                patient.MedicalHistory = PatientMedicalHistory.Text;
                if (DataSharingService.Instance.Patient != null)
                {
                    DataSharingService.Instance.Patient.ChiefComplaint = patient.ChiefComplaint;
                    DataSharingService.Instance.Patient.MedicalHistory = patient.MedicalHistory;
                }
            }
        }
    }
}
