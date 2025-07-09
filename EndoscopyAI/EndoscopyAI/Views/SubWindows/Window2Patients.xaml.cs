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
        public string Timestamp { get; set; }
        public string Message { get; set; }
        public bool IsPatientMessage { get; set; }
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
                    var window = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w is Window2Patients);
                    if (window != null)
                    {
                        var style = window.FindResource(isPatientMessage ? "PatientMessageStyle" : "AIMessageStyle");
                        if (style != null)
                            return style;
                    }
                    return Application.Current.TryFindResource(isPatientMessage ? "PatientMessageStyle" : "AIMessageStyle");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"查找样式失败: {ex.Message}");
                    return null;
                }
            }
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
                return isPatientMessage ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"))
                                       : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333"));
            }
            return new SolidColorBrush(Colors.Black); // 默认黑色
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public partial class Window2Patients : Window
    {
        private ObservableCollection<ChatMessage> chatHistory = new ObservableCollection<ChatMessage>();

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
                chatHistory.Add(new ChatMessage
                {
                    Timestamp = DateTime.Now.ToString("HH:mm"),
                    Message = patientMessage,
                    IsPatientMessage = true
                });

                string aiResponse = "这是AI的回复"; // 实际应用中替换为AI服务调用
                chatHistory.Add(new ChatMessage
                {
                    Timestamp = DateTime.Now.ToString("HH:mm"),
                    Message = aiResponse,
                    IsPatientMessage = false
                });

                PatientInputBox.Clear();
            }
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            SendMessage_Click(sender, e);
        }
    }
}