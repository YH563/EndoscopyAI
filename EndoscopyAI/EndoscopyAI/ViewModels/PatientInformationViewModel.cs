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
using EndoscopyAI.ViewModels;

namespace EndoscopyAI.ViewModels
{
    interface IPatientInformation
    {
        // 检查输入病人信息是否有效
        bool PatientInformationChecker(Patient patient);

        // 更新病人信息
        void UpdatePatient(Patient updatedPatient);
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
        private bool PatientInformationFormatChecker(object value, string propertyName)
        {
            if (propertyName == "ConfidenceLevel" || propertyName == "DiagnosisResult" || 
                propertyName == "ImagePath")
                return true;
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
                if (!Regex.IsMatch(value.ToString(), phonePattern) && !Regex.IsMatch(value.ToString(), telPattern) 
                    || value.ToString() == "") return false;
                else return true;
            }
            // 检查身份证号/医保号格式
            if (propertyName == "NumberID")
            {
                var idCardPattern = @"^[1-9]\d{5}(18|19|20)\d{2}(0[1-9]|1[0-2])(0[1-9]|[1-2][0-9]|3[0-1])\d{3}[\dXx]$";
                var medicalPattern = @"^[A-Za-z0-9]{8,20}$";
                if (!Regex.IsMatch(value.ToString(), idCardPattern) && !Regex.IsMatch(value.ToString(), medicalPattern)
                    || value.ToString() == "") return false;
                else return true;
            }
            if (value.ToString() == "" || value.ToString() == "0") return false;
            else return true;
        }

        // 修改病人信息
        public void UpdatePatient(Patient updatedPatient)
        {
            if (PatientInformationChecker(updatedPatient))
            {
                GlobalDbService.DbService.UpdatePatient(updatedPatient);
                MessageBox.Show("病人信息更新成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("请输入正确完整的病人信息！", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
