// MessageModel.cs
using System;

namespace EndoscopyAI.Views.SubWindows
{
    public class MessageModel
    {
        public string Content { get; set; }
        public bool IsUserMessage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}