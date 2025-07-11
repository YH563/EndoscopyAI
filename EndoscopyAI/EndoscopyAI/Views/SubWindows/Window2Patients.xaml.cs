using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using EndoscopyAI.Services;

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
