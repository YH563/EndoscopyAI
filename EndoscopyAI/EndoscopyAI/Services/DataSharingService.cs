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

        // 共享原始图像信息
        public Mat? OriginImage { set; get; }

        // 共享处理后图像信息
        private Mat? _processedImage;
        public Mat? ProcessedImage
        {
            get => _processedImage;
            set
            {
                if (_processedImage != value)
                {
                    _processedImage = value;
                    ImageChanged.Invoke(this, EventArgs.Empty);
                }
            }
        }

        // 委托事件
        public event EventHandler PatientChanged;
        public event EventHandler ImageChanged;
    }
}