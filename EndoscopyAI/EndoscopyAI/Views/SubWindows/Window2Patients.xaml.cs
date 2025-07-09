using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace EndoscopyAI.Views.SubWindows
{
    // 聊天消息数据模型
    public class ChatMessage
    {
        public string Timestamp { get; set; } // 显示用时间戳（HH:mm）
        public string Message { get; set; }
        public bool IsPatientMessage { get; set; }
        public DateTime SendTime { get; set; } // 实际发送时间
        public bool ShouldShowTimestamp { get; set; } // 是否显示时间戳
    }

    // 消息样式转换器
    public class MessageStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool isPatientMessage)
            {
                try
                {
                    string styleKey = isPatientMessage ? "PatientMessageStyle" : "AIMessageStyle";
                    var window = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w is Window2Patients);
                    if (window != null)
                    {
                        var style = window.FindResource(styleKey);
                        if (style != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"成功找到样式: {styleKey}, IsPatientMessage: {isPatientMessage}");
                            return style;
                        }
                    }
                    var appStyle = Application.Current.TryFindResource(styleKey);
                    if (appStyle != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"从应用程序资源找到样式: {styleKey}, IsPatientMessage: {isPatientMessage}");
                        return appStyle;
                    }
                    System.Diagnostics.Debug.WriteLine($"未找到样式: {styleKey}, IsPatientMessage: {isPatientMessage}");
                    return null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"查找样式失败: {ex.Message}, IsPatientMessage: {isPatientMessage}");
                    return null;
                }
            }
            System.Diagnostics.Debug.WriteLine($"MessageStyleConverter: 无效的输入值: {value}");
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // 消息前景色转换器
    public class MessageForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool isPatientMessage)
            {
                System.Diagnostics.Debug.WriteLine($"MessageForegroundConverter: IsPatientMessage: {isPatientMessage}");
                return isPatientMessage ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"))
                                       : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333"));
            }
            System.Diagnostics.Debug.WriteLine($"MessageForegroundConverter: 无效的输入值: {value}");
            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public partial class Window2Patients : Window
    {
        private ObservableCollection<ChatMessage> chatHistory = new ObservableCollection<ChatMessage>();
        private DateTime lastMessageTime = DateTime.MinValue; // 跟踪上一条消息时间

        public Window2Patients()
        {
            InitializeComponent();
            ChatHistoryListBox.ItemsSource = chatHistory;
        }

        private void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            string patientMessage = PatientInputBox.Text;
            if (!string.IsNullOrWhiteSpace(patientMessage))
            {
                DateTime currentTime = DateTime.Now;
                bool showTimestamp = (currentTime - lastMessageTime).TotalMinutes >= 3 || lastMessageTime == DateTime.MinValue;

                // 添加患者消息
                chatHistory.Add(new ChatMessage
                {
                    Timestamp = currentTime.ToString("HH:mm"),
                    Message = patientMessage,
                    IsPatientMessage = true,
                    SendTime = currentTime,
                    ShouldShowTimestamp = showTimestamp
                });
                System.Diagnostics.Debug.WriteLine($"添加患者消息: {patientMessage}, IsPatientMessage: true, ShouldShowTimestamp: {showTimestamp}");

                // 添加AI回复
                string aiResponse = "这是AI的回复"; // 实际应用中替换为AI服务调用
                showTimestamp = (currentTime - chatHistory[chatHistory.Count - 1].SendTime).TotalMinutes >= 3;
                chatHistory.Add(new ChatMessage
                {
                    Timestamp = currentTime.ToString("HH:mm"),
                    Message = aiResponse,
                    IsPatientMessage = false,
                    SendTime = currentTime,
                    ShouldShowTimestamp = showTimestamp
                });
                System.Diagnostics.Debug.WriteLine($"添加AI消息: {aiResponse}, IsPatientMessage: false, ShouldShowTimestamp: {showTimestamp}");

                lastMessageTime = currentTime;
                PatientInputBox.Clear();
            }
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            SendMessage_Click(sender, e);
        }
    }
}