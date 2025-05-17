using OpenCvSharp;
using System.IO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime.Tensors;
using EndoscopyAI.Services;
using System.Windows;

namespace EndoscopyAI.ViewModels.SubViewModels
{
    interface IImageProcess
    {
        //void UpdateImg(Patient updatedPatient);
    }

    public class ImageProcessViewModel:IImageProcess
    {
        private readonly IImageDisplay _imageDisplay;
        public string ImagePath { get; private set; }
        public Mat CurrentImage { get; private set; }
        public Mat OriginImage { get; private set; }

        public ImageProcessViewModel()
        {
            _imageDisplay = new ImageDisplay();
        }

        public void SyncImagePath()
        {
            ImagePath = DataSharingService.Instance.ImagePath;
            if (!string.IsNullOrEmpty(ImagePath) && File.Exists(ImagePath))
            {
                if (CurrentImage == null || OriginImage == null)
                {
                    try
                    {
                        CurrentImage = _imageDisplay.LoadImageFromFile(ImagePath);
                        OriginImage = CurrentImage.Clone();
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show($"加载图像时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}