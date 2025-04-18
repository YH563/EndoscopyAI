using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
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
                if (property.Name == "ConfidenceLevel" || property.Name == "DiagnosisResult")
                    continue;
                if (value.ToString() == "Unkown" || value.ToString() == "0")
                {
                    return false;
                }
            }
            return true;
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
                MessageBox.Show("请完整输入病人信息！", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


    }
}
