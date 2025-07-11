﻿using EndoscopyAI.Services;
using EndoscopyAI.ViewModels.SubViewModels;
using System.Windows;

namespace EndoscopyAI.Views.SubWindows
{
    /// <summary>
    /// LoginWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginWindow : Window
    {
        ILoginViewModel loginViewModel = new LoginViewModel();  // 登录接口实例

        public LoginWindow()
        {
            InitializeComponent();
        }

        // 登录按钮点击事件
        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string userId = UserIdTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (loginViewModel.CheckLogin(userId, password))
            {
                //// 登录成功后打开信息统计窗口
                //var InformationStatistics = new InformationStatistics();
                //InformationStatistics.Show();

                // 同时打开面向患者的窗口
                var qwen = new QwenChatService(); // 从配置加载
                var Window2Patients = new Window2Patients(qwen);
                Window2Patients.Show();
                // 登陆成功后打开主窗口
                var mainWindow = new MainWindow();
                mainWindow.Show();

                
                // 关闭登录窗口
                this.Close();
            }
        }


        // 注册按钮点击事件
        private void Register_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("注册功能暂未开放。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
