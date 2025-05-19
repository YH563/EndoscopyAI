using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using EndoscopyAI.Services;

namespace EndoscopyAI.ViewModels.SubViewModels
{
    interface IPatientInformation
    {
        // 检查输入病人信息是否满足格式要求
        public bool PatientInformationFormatChecker(object value, string propertyName);

        // 添加病人信息
        bool AddPatientInformation(Patient patient);

        // 获取最近的病人信息
        Patient? GetLatestPatientInformation();

        // 获取所有病人信息
        List<Patient>? GetAllPatientInformation();

        // 根据名字获取所有对应的病人信息
        List<Patient>? GetAllPatientInformationByName(string name);

        // 根据身份证号/医保号获取病人信息
        List<Patient>? GetAllPatientInformationByNumberID(string numberID);

        // 更新病人信息
        void UpdatePatientInformation(Patient patient);

        // 删除病人信息
        void DeletePatientInformation(int ID);
    }

    public class PatientInformation : IPatientInformation
    {
        // 检查输入病人信息是否有效
        public bool PatientInformationChecker(Patient patient)
        {
            Type type = patient.GetType();
            PropertyInfo[] properties = type.GetProperties();
            foreach (PropertyInfo property in properties)
            {
                object value = property.GetValue(patient) ?? "";
                string name = property.Name;
                bool Format = PatientInformationFormatChecker(value, name);
                if (!Format) return false;
            }
            return true;
        }

        // 检查输入病人信息是否满足格式要求
        public bool PatientInformationFormatChecker(object value, string propertyName)
        {
            // 检查年龄格式
            if (propertyName == "Age")
            {
                if (!int.TryParse(value.ToString(), out int age) || age < 1 || age > 150 
                    || value.ToString() == "0") return false;
                else return true;
            }
            // 检查联系方式格式
            if (propertyName == "Contact")
            {
                var phonePattern = @"^1[3-9]\d{9}$";
                var telPattern = @"^\d{3,4}-\d{7,8}$";
                if (value.ToString() == "10086") return true;
                if (!Regex.IsMatch(value.ToString(), phonePattern) && !Regex.IsMatch(value.ToString(), telPattern) 
                    || value.ToString() == "") return false;
                else return true;
            }
            // 检查身份证号/医保号格式
            if (propertyName == "NumberID")
            {
                var idCardPattern = @"^[1-9]\d{5}(18|19|20)\d{2}(0[1-9]|1[0-2])(0[1-9]|[1-2][0-9]|3[0-1])\d{3}[\dXx]$";
                var medicalPattern = @"^[A-Za-z0-9]{8,20}$";
                if (value.ToString() == "10086" || value.ToString() == "10010" || value.ToString() == "10000") return true;
                if (string.IsNullOrEmpty(value.ToString())) return false;
                else if (!Regex.IsMatch(value.ToString(), idCardPattern) && !Regex.IsMatch(value.ToString(), medicalPattern))
                {
                    return false;
                }
            }
            // 检查姓名格式
            if (propertyName == "Name")
            {
                if (string.IsNullOrWhiteSpace(value.ToString())) return false;
                else return true;
            }

            return true;
        }

        // 添加病人信息
        public bool AddPatientInformation(Patient patient)
        {
            if(PatientInformationChecker(patient))
            {
                GlobalDbService.PatientDbService.AddPatient(patient);
                return true;
            }
            else
            {
                MessageBox.Show("输入信息有误，请重新输入", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        // 获取最近的病人信息
        public Patient? GetLatestPatientInformation()
        {
            return GlobalDbService.PatientDbService.GetLatestPatient();

        }

        // 获取所有病人信息
        public List<Patient>? GetAllPatientInformation()
        {
            List<Patient> patients = GlobalDbService.PatientDbService.GetPatients();
            if(patients.Count == 0) return null;
            else return patients;
        }
        // 根据名字获取所有对应的病人信息
        public List<Patient>? GetAllPatientInformationByName(string name)
        {
            return GlobalDbService.PatientDbService.GetPatientsByName(name);
        }

        // 根据身份证号/医保号获取病人信息
        public List<Patient>? GetAllPatientInformationByNumberID(string numberID)
        {
            return GlobalDbService.PatientDbService.GetPatientsByNumberID(numberID);
        }

        // 更新病人信息
        public void UpdatePatientInformation(Patient patient)
        {
            GlobalDbService.PatientDbService.UpdatePatient(patient);
            MessageBox.Show("患者信息保存成功！", "信息", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // 删除病人信息
        public void DeletePatientInformation(int ID)
        {
            GlobalDbService.PatientDbService.DeletePatient(ID);
        }
    }
}
