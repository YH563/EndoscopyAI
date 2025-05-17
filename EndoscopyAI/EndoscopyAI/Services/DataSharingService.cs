using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace EndoscopyAI.Services
{
    // 单例用于在应用程序中共享数据
    public sealed class DataSharingService : INotifyPropertyChanged
    {
        // 单例实例
        private static readonly DataSharingService _instance = new DataSharingService();
        public static DataSharingService Instance => _instance;

        // 私有构造函数防止外部实例化
        private DataSharingService() { }

        // 不需要实时显示的数据
        public string? ImagePath { get; set; }

        // 诊断结果（支持外部赋值）
        private string _diagnosisResult = string.Empty;
        public string DiagnosisResult
        {
            get => _diagnosisResult;
            set
            {
                if (_diagnosisResult != value)
                {
                    _diagnosisResult = value;
                    OnPropertyChanged();
                }
            }
        }

        // 置信度相关
        private float _confidenceLevel;
        public float Confidence
        {
            get => _confidenceLevel;
            set
            {
                if (!_confidenceLevel.Equals(value))
                {
                    _confidenceLevel = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ConfidenceDisplay));
                }
            }
        }

        // 医生姓名
        private string _doctorName = string.Empty;
        public string DoctorName
        {
            get => _doctorName;
            set 
            { 
                if (_doctorName != value)
                {
                    _doctorName = value;
                    OnPropertyChanged();
                }
            }
        }

        // 格式化显示的置信度
        public string ConfidenceDisplay => $"{_confidenceLevel:P2}";

        // 实现属性变更通知
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            // 确保在UI线程更新
            Application.Current.Dispatcher.Invoke(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            });
        }
    }
}