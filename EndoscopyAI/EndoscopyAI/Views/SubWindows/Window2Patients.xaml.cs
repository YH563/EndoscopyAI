﻿using EndoscopyAI.ViewModels.SubViewModels;
using System;
using System.Collections.Generic;
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

namespace EndoscopyAI.Views.SubWindows
{
    /// <summary>
    /// Window2Patients.xaml 的交互逻辑
    /// </summary>
    public partial class Window2Patients : Window
    {
        public Window2Patients()
        {
            InitializeComponent();
        }

        private void AIOutputBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            // 将输入框内容赋值到输出框
            AIOutputBox.Text = PatientInputBox.Text;
        }
    }
}
