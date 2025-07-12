using EndoscopyAI.Services;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace EndoscopyAI.Views.SubWindows
{
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

    // 聊天消息数据模型
    public class ChatMessage
    {
        public string Timestamp { get; set; }      // “HH:mm”
        public string Message { get; set; }        // Markdown 文本
        public bool IsPatientMessage { get; set; } // true = 患者
        public DateTime SendTime { get; set; }
        public bool ShouldShowTimestamp { get; set; }
    }

    public partial class Window2Patients : Window
    {
        private readonly ObservableCollection<ChatMessage> chatHistory = new();
        private DateTime lastMessageTime = DateTime.MinValue;
        private readonly QwenChatService _chatService;//通义千问服务

        public Window2Patients(QwenChatService chatService)
        {
            InitializeComponent();
            _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
            ChatHistoryListBox.ItemsSource = chatHistory;
        }

        //private void SendMessage_Click(object sender, RoutedEventArgs e)
        //{
        //    string patientMessage = PatientInputBox.Text?.Trim();
        //    if (string.IsNullOrEmpty(patientMessage)) return;

        //    DateTime now = DateTime.Now;
        //    bool showTime = (now - lastMessageTime).TotalMinutes >= 3 || lastMessageTime == DateTime.MinValue;

        //    // 患者消息
        //    chatHistory.Add(new ChatMessage
        //    {
        //        Timestamp = now.ToString("HH:mm"),
        //        Message = patientMessage,
        //        IsPatientMessage = true,
        //        SendTime = now,
        //        ShouldShowTimestamp = showTime
        //    });

        //    // AI 回复内容
        //    chatHistory.Add(new ChatMessage
        //    {
        //        Timestamp = now.ToString("HH:mm"),
        //        Message = "这是 **AI** 的 _Markdown_ 回复示例",
        //        IsPatientMessage = false,
        //        SendTime = now,
        //        ShouldShowTimestamp = false   // 同一分钟内不再显示时间
        //    });

        //    lastMessageTime = now;
        //    PatientInputBox.Clear();
        //}

        // 如果 XAML 仍然绑定了 Submit_Click，可保留此转发
        private async void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            string patientMessage = PatientInputBox.Text?.Trim();
            if (string.IsNullOrEmpty(patientMessage)) return;

            DateTime now = DateTime.Now;
            bool showTime = (now - lastMessageTime).TotalMinutes >= 3 || lastMessageTime == DateTime.MinValue;

            // 添加患者消息
            chatHistory.Add(new ChatMessage
            {
                Timestamp = now.ToString("HH:mm"),
                Message = patientMessage,
                IsPatientMessage = true,
                SendTime = now,
                ShouldShowTimestamp = showTime
            });

            PatientInputBox.Clear();
            lastMessageTime = now;

            try
            {
                string aiReply = await _chatService.ChatAsync(patientMessage); // ✅ 调用真实模型
                chatHistory.Add(new ChatMessage
                {
                    Timestamp = DateTime.Now.ToString("HH:mm"),
                    Message = aiReply,
                    IsPatientMessage = false,
                    SendTime = DateTime.Now,
                    ShouldShowTimestamp = true
                });
            }
            catch (Exception ex)
            {
                chatHistory.Add(new ChatMessage
                {
                    Timestamp = DateTime.Now.ToString("HH:mm"),
                    Message = $"⚠️ AI出错：{ex.Message}",
                    IsPatientMessage = false,
                    SendTime = DateTime.Now,
                    ShouldShowTimestamp = true
                });
            }
        }
    }
}