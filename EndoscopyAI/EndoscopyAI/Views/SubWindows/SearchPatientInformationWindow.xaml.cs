using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using EndoscopyAI.Services;
using EndoscopyAI.ViewModels.SubViewModels;

namespace EndoscopyAI.Views.SubWindows
{
    /// <summary>
    /// SearchPatientInformationWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SearchPatientInformationWindow : Window
    {
        List<Patient>? patients = new List<Patient>();  // 存储搜索结果
        private ObservableCollection<Patient>? _patients;  // 用于绑定到UI的只读集合
        IPatientInformation patientInformation = new PatientInformation();  // 病人信息接口实例

        public SearchPatientInformationWindow()
        {
            patients = patientInformation.GetAllPatientInformation();
            InitializeComponent();
            LoadPatientInformation();
        }

        // 加载患者信息
        private void LoadPatientInformation()
        {
            try
            {
                if (patients != null)
                {
                    _patients = new ObservableCollection<Patient>(patients);
                    PatientsItemsControl.ItemsSource = _patients;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载病人数据失败: {ex.Message}");
            }
        }

        // 搜索患者信息
        private bool SearchPatients()
        {
            if(searchTypeComboBox.SelectedIndex == 0 && patientInformation.PatientInformationFormatChecker(searchInputTextBox.Text, "Name"))
            {
                patients = patientInformation.GetAllPatientInformationByName(searchInputTextBox.Text);
                LoadPatientInformation();
                return true && patients != null;
            }
            if (searchTypeComboBox.SelectedIndex == 1 && patientInformation.PatientInformationFormatChecker(searchInputTextBox.Text, "NumberID"))
            {
                patients = patientInformation.GetAllPatientInformationByName(searchInputTextBox.Text);
                LoadPatientInformation();
                return true && patients != null;
            }
            return false;
        }

        // 确认按钮TODO
        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Patient patient)
            {
                try
                {
                    DataSharingService.Instance.Patient = patient;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"确认操作失败: {ex.Message}");
                }
            }
        }

        // 删除按钮
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Patient patient)
            {
                var result = MessageBox.Show($"确定要删除病人 {patient.Name} 吗?", "确认删除", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        if (_patients != null) _patients.Remove(patient);
                        patientInformation.DeletePatientInformation(patient.ID);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"删除失败: {ex.Message}");
                    }
                }
            }
        }

        // 添加患者信息
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddPatientInformationWindow();
            addWindow.Show();
        }

        // 搜索患者信息 TODO
        private void Search_Click(object sender, RoutedEventArgs e)
        {
            if(SearchPatients())
            {
                MessageBox.Show("搜索完成");
            }
            else
            {
                patients = patientInformation.GetAllPatientInformation();
                LoadPatientInformation();
            }
        }
    }
}
