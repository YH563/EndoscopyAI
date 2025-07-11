using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EndoscopyAI.Views.SubWindows
{
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

        public Window2Patients()
        {
            InitializeComponent();
            ChatHistoryListBox.ItemsSource = chatHistory;   // 直接绑定集合
        }

        private void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            string patientMessage = PatientInputBox.Text?.Trim();
            if (string.IsNullOrEmpty(patientMessage)) return;

            DateTime now = DateTime.Now;
            bool showTime = (now - lastMessageTime).TotalMinutes >= 3 || lastMessageTime == DateTime.MinValue;

            // 患者消息
            chatHistory.Add(new ChatMessage
            {
                Timestamp = now.ToString("HH:mm"),
                Message = patientMessage,
                IsPatientMessage = true,
                SendTime = now,
                ShouldShowTimestamp = showTime
            });

            // AI 回复（此处仅示例，可替换为实际调用）
            chatHistory.Add(new ChatMessage
            {
                Timestamp = now.ToString("HH:mm"),
                Message = "这是 **AI** 的 _Markdown_ 回复示例",
                IsPatientMessage = false,
                SendTime = now,
                ShouldShowTimestamp = false   // 同一分钟内不再显示时间
            });

            lastMessageTime = now;
            PatientInputBox.Clear();
        }

        // 如果 XAML 仍然绑定了 Submit_Click，可保留此转发
        private void Submit_Click(object sender, RoutedEventArgs e) => SendMessage_Click(sender, e);
    }
}
