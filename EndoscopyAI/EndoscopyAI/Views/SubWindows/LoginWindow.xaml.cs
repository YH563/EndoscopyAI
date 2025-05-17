using System.Windows;

namespace EndoscopyAI.Views.SubWindows
{
    /// <summary>
    /// LoginWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        // 登录按钮点击事件
        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string userId = UserIdTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(userId))
            {
                MessageBox.Show("请输入工号！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("请输入密码！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 先用来测试
            if (userId == "schbmeseu" && password == "1233211234567")
            {
                // 显示主窗口
                var mainWindow = new MainWindow();
                mainWindow.Show();

                // 关闭登录窗口
                this.Close();
            }
            else
            {
                MessageBox.Show("工号或密码错误！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // 注册按钮点击事件
        private void Register_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("注册功能暂未开放。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
