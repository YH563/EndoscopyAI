using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace EndoscopyAI.Services
{
    // 请求结构
    public class ChatRequest
    {
        [JsonPropertyName("model")] public string Model { get; set; } = "qwen-plus";
        [JsonPropertyName("messages")] public List<QwenMessage> Messages { get; set; }
        [JsonPropertyName("stream")] public bool Stream { get; set; } = false;
        [JsonPropertyName("temperature")] public double Temperature { get; set; } = 0.8;
        [JsonPropertyName("top_p")] public double TopP { get; set; } = 0.9;
        [JsonPropertyName("max_tokens")] public int MaxTokens { get; set; } = 2048;
    }

    // 响应结构
    public class ChatResponse
    {
        [JsonPropertyName("choices")] public List<QwenChoice> Choices { get; set; }
    }

    public class QwenChoice
    {
        [JsonPropertyName("message")]
        public QwenMessage Message { get; set; }
    }

    public class QwenMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }
    }

    // 配置读取类
    public class QwenOptions
    {
        public string ApiKey { get; set; }
        public string Model { get; set; } = "qwen-plus";
        public double Temperature { get; set; } = 0.8;
        public double TopP { get; set; } = 0.9;
        public int MaxTokens { get; set; } = 2000;
    }

    // 核心服务类
    public class QwenChatService
    {
        private readonly QwenOptions _opts;
        private readonly List<QwenMessage> _history = new();
        private readonly HttpClient _httpClient = new();
        private const string ApiUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions";

        public QwenChatService()
        {
            _opts = LoadOptionsFromConfig();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _opts.ApiKey);

            // 加入 system 指令（一次性设置 AI 身份）
            _history.Add(new QwenMessage
            {
                Role = "system",
                Content = "你是一位专业的问诊助手，请使用简洁通俗易懂的语言回答用户问题，核心在于减少医患冲突，引导患者去理解医生。"
            });
        }

        public async Task<string> ChatAsync(string userInput)
        {
            _history.Add(new QwenMessage { Role = "user", Content = userInput });

            var request = new ChatRequest
            {
                Model = _opts.Model,
                Messages = _history,
                Temperature = _opts.Temperature,
                TopP = _opts.TopP,
                MaxTokens = _opts.MaxTokens,
                Stream = false
            };

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(ApiUrl, content);
            var responseJson = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseJson);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Qwen API 错误：{response.StatusCode}\n{responseJson}");
            }

            var parsed = JsonSerializer.Deserialize<ChatResponse>(responseJson);
            var reply = parsed?.Choices?[0]?.Message?.Content ?? "";

            _history.Add(new QwenMessage { Role = "assistant", Content = reply });
            return reply;
        }

        public void ClearHistory()
        {
            _history.Clear();
            _history.Add(new QwenMessage
            {
                Role = "system",
                Content = "你是一位专业的问诊助手，请使用简洁通俗易懂的语言回答用户问题，核心在于减少医患冲突，引导患者去理解医生。"
            });
        }

        private QwenOptions LoadOptionsFromConfig()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            return config.GetSection("Qwen").Get<QwenOptions>() ?? throw new Exception("无法加载 Qwen 配置！");
        }
    }
}
