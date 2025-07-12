using EndoscopyAI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using EndoscopyAI.Tests;

namespace EndoscopyAI.ViewModels.SubViewModels
{
    interface ILoginViewModel
    {
        // 检查登录帐号密码是否正确
        bool CheckLogin(string userId, string password);
    }

    class LoginViewModel : ILoginViewModel
    {
        // 检查登录帐号密码是否正确
        public bool CheckLogin(string userId, string password)
        {
            if (string.IsNullOrEmpty(userId))
            {
                MessageBox.Show("请输入工号！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("请输入密码！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // 打开管理员界面
            if (userId == "admin" && password == "admin")
            {
                var testWindow = new TestWindow();
                testWindow.Show();
                return true;
            }

            if ((userId == "user" && password == "123456") || GlobalDbService.DoctorDbService.CheckPassword(userId, password))
                return true;
            else
            {
                MessageBox.Show("工号或密码错误！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
}
