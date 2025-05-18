using OpenCvSharp;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace EndoscopyAI.Services
{
    // 单例用于在应用程序中共享数据
    public sealed class DataSharingService
    {
        // 单例实例
        private static readonly Lazy<DataSharingService> _instance =
        new Lazy<DataSharingService>(() => new DataSharingService());

        public static DataSharingService Instance => _instance.Value;

        // 私有构造函数防止外部实例化
        private DataSharingService()
        {
            _patient = new Patient();
            PatientChanged = delegate { };
            ImageChanged = delegate { };
        }

        // 共享病人信息
        private Patient? _patient;
        public Patient? Patient
        {
            get => _patient;
            set
            {
                if (_patient != value)
                {
                    _patient = value;
                    PatientChanged.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private Mat? _originImage;
        public Mat? OriginImage
        {
            get => _originImage;
            set
            {
                if (_originImage != value)
                {
                    _originImage?.Dispose(); // 释放旧资源
                    _originImage = value?.Clone(); // 建议使用克隆避免外部修改影响
                }
            }
        }

        private Mat? _processedImage;
        public Mat? ProcessedImage
        {
            get => _processedImage;
            set
            {
                if (_processedImage != value)
                {
                    _processedImage?.Dispose(); // 释放旧资源
                    _processedImage = value?.Clone(); // 克隆新值
                    ImageChanged.Invoke(this, EventArgs.Empty);
                }
            }
        }

        // 委托事件
        public event EventHandler PatientChanged;
        public event EventHandler ImageChanged;
    }
}