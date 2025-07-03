using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Threading.Tasks;
using Neo4j.Driver;
using EndoscopyAI.ViewModels.SubViewModels;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Windows;

namespace EndoscopyAI.Services
{
    public class DeepSeekOptions  // 专用配置类，为deepseek准备
    {
        public string ApiKey { get; set; } = string.Empty;
    }

    // DeepSeek API 响应模型，为后续解析API响应做准备，清除不必要内容获得纯文本输出
    public class DeepSeekApiResponse
    {
        public string Id { get; set; }
        public List<Choice> Choices { get; set; }
        public Usage Usage { get; set; }
    }

    public class Choice
    {
        public Message Message { get; set; }
    }

    public class Message
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }

    public class Usage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }


    public static class KeywordMapper
    {
        private static readonly Dictionary<string, string> keywordMap = new Dictionary<string, string>
        {
            { "barrett", "Barrett食管" },
            { "inflammation", "食管炎" },
            { "cancer", "食管癌" },
            { "normal", "normal" }
        };

        public static string MapToMedicalTerm(string input)
        {
            return keywordMap.TryGetValue(input, out var mapped) ? mapped : null;
        }
    }

    public class Neo4jResult
    {
        public string Source { get; set; }
        public string SourceLabels { get; set; }
        public string Relation { get; set; }
        public string Direction { get; set; }
        public string Target { get; set; }
        public string TargetLabels { get; set; }
    }

    // 本地部署知识图谱查找关系，从neo4j上面把相关的down下来作为csv文件
    // 定义图结构
    public class GraphNode
    {
        public string Name { get; set; }
        public string Label { get; set; }
        public List<GraphEdge> Edges { get; } = new List<GraphEdge>();
    }

    public class GraphEdge
    {
        public string Relation { get; set; }
        public GraphNode Target { get; set; }
    }

    public class LocalGraphService
    {
        private readonly Dictionary<string, GraphNode> _nodes = new();

        public void LoadFromCsv(string filePath, char delimiter = ',')  // 注意逗号分隔
        {
            using var reader = new StreamReader(filePath);
            string? header = reader.ReadLine(); // 跳过表头
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(delimiter);
                if (parts.Length < 5) continue;

                string sourceName = parts[0].Trim('\"', ' ');
                string sourceLabel = parts[1].Trim('\"', ' ');
                string relation = parts[2].Trim('\"', ' ');
                string targetName = parts[3].Trim('\"', ' ');
                string targetLabel = parts[4].Trim('\"', ' ');

                var source = GetOrCreateNode(sourceName, sourceLabel);
                var target = GetOrCreateNode(targetName, targetLabel);

                source.Edges.Add(new GraphEdge
                {
                    Relation = relation,
                    Target = target
                });
            }
        }

        private GraphNode GetOrCreateNode(string name, string label)
        {
            if (!_nodes.TryGetValue(name, out var node))
            {
                node = new GraphNode { Name = name, Label = label };
                _nodes[name] = node;
            }
            return node;
        }

        // 本地查询并转换为 Neo4jResult 格式
        public List<Neo4jResult> QueryEntitiesByKeyword(string keyword)
        {
            var results = new List<Neo4jResult>();

            if (!_nodes.TryGetValue(keyword, out var node)) return results;

            foreach (var edge in node.Edges)
            {
                results.Add(new Neo4jResult
                {
                    Source = node.Name,
                    SourceLabels = node.Label,
                    Relation = edge.Relation,
                    Direction = "-->",
                    Target = edge.Target.Name,
                    TargetLabels = edge.Target.Label
                });
            }

            return results;
        }

    }

    public class LargeModelService
    {
        private readonly HttpClient _httpClient;

        public LargeModelService(string apiKey)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.deepseek.com/")
            };
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        // 模型调用接口
        public async Task<string> GenerateDiagnosisSuggestionAsync(string prompt)
        {
            // 设置背景消息（系统消息）
            string backgroundMessage = "作为消化内科专家，请根据以下内容，给出200字内的专业诊断报告。严格按照如下要求：给出两段内容，一段是“诊断结果”，包括“症状+疾病判断”；一段是“治疗方案”，包括“用药与后期检查方案。";

            var requestContent = new
            {
                model = "deepseek-chat",
                messages = new[]
                {
            // 系统消息 - 设置背景和角色
            new {
                role = "system",
                content = backgroundMessage
            },
            
            // 用户消息
            new {
                role = "user",
                content = prompt
            }
        },
                max_tokens = 300,  // 增加了token限制以容纳更丰富的响应
                temperature = 0.7,
                stream = false
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync("v1/chat/completions", requestContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API 调用失败: {response.StatusCode}\n错误详情: {errorContent}");
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<DeepSeekApiResponse>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true  // 忽略大小写
                });

                Console.WriteLine("API 原始响应：\n" + jsonResponse);

                return apiResponse?.Choices?.FirstOrDefault()?.Message?.Content?.Trim() ?? string.Empty;
            }
            catch (Exception ex)
            {
                throw new Exception($"请求发生异常: {ex.Message}");
            }
        }

        // 接口方法：一次完成图谱加载、模型调用
        public async Task<(string DiagnosisAdvice, string TreatmentPlan)> GetRecommendationAsync(string AIResult, string ChiefComplain, string MedicalHistory)
        {
            if (string.IsNullOrWhiteSpace(AIResult))
            {
                MessageBox.Show("病人症状不能为空，请重新选择", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return ("", "");
            }

            var history = string.IsNullOrWhiteSpace(MedicalHistory) ? "无既往病史" : MedicalHistory;
            var mappedKeyword = KeywordMapper.MapToMedicalTerm(AIResult);

            if (mappedKeyword == null)
            {
                Console.WriteLine("输入的关键词无效或未定义对应医学术语。");
                return ("", "");
            }

            if (mappedKeyword == "normal")
            {
                var resultText = "恭喜！病人健康，无需其他诊断建议！";
                return (resultText, resultText);
            }

            // ✅ 本地图谱初始化（每次调用加载一次 CSV）
            var graph = new LocalGraphService();
            var projectRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\.."));
            var csvPath = Path.Combine(projectRoot, "Services", "esophagus_relations.csv");

            if (!File.Exists(csvPath))
            {
                throw new FileNotFoundException($"找不到知识图谱文件: {csvPath}");
            }

            graph.LoadFromCsv(csvPath);

            // ✅ 查询主关键词
            var results = graph.QueryEntitiesByKeyword(mappedKeyword);

            // ✅ 追加主诉症状模糊查询
            if (!string.IsNullOrWhiteSpace(ChiefComplain))
            {
                var Symptoms = Regex.Split(ChiefComplain, @"[,\，\.\。\s;；\n\r]+")
                                    .Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();

                foreach (var symptom in Symptoms)
                {
                    var symptomResults = graph.QueryEntitiesByKeyword(symptom);
                    results.AddRange(symptomResults);
                }
            }

            // ✅ 构造提示词
            var PromptInitial = new StringBuilder();

            void AppendItems(string title, IEnumerable<string> items)
            {
                if (items.Any())
                    PromptInitial.AppendLine($"{title}：" + string.Join("，", items) + "。");
            }

            AppendItems("症状", results.Where(r => r.TargetLabels.Contains("Symptom") || r.TargetLabels.Contains("SymptomAndSign"))
                                      .GroupBy(r => r.Target).OrderByDescending(g => g.Count()).Take(6).Select(g => g.Key));
            AppendItems("可能病因", results.Where(r => r.TargetLabels.Contains("Cause"))
                                          .GroupBy(r => r.Target).OrderByDescending(g => g.Count()).Take(6).Select(g => g.Key));
            AppendItems("推荐用药", results.Where(r => r.TargetLabels.Contains("Drug"))
                                          .GroupBy(r => r.Target).OrderByDescending(g => g.Count()).Take(6).Select(g => g.Key));
            AppendItems("推荐手术", results.Where(r => r.TargetLabels.Contains("Operation"))
                                          .GroupBy(r => r.Target).OrderByDescending(g => g.Count()).Take(6).Select(g => g.Key));

            var finalPrompt = $"病人既往病史：{history}\n现在的症状是：{AIResult}\n根据知识图谱，得到的诊断建议：\n{PromptInitial}";

            // ✅ 调用大模型
            var suggestion = await GenerateDiagnosisSuggestionAsync(finalPrompt);

            // ✅ 清理多余的空格和换行符
            suggestion = CleanResponseText(suggestion);

            // ✅ 增强提取逻辑
            var (diagnosis, treatment) = ExtractDiagnosisAndTreatment(suggestion);

            return (diagnosis, treatment);
        }

        // 清理一些不必要的文本输出
        private (string Diagnosis, string Treatment) ExtractDiagnosisAndTreatment(string suggestion)
        {
            // ✅ 更健壮的提取逻辑
            string diagnosis = "未找到诊断信息";
            string treatment = "未找到治疗方案信息";

            // 尝试多种可能的标题格式
            var diagnosisKeywords = new[] { "诊断结果", "诊断建议", "诊断分析" };
            var treatmentKeywords = new[] { "治疗方案", "治疗建议", "处理方案" };

            int diagnosisIndex = -1;
            int treatmentIndex = -1;
            string usedDiagnosisKeyword = string.Empty;
            string usedTreatmentKeyword = string.Empty;

            // 查找诊断标题
            foreach (var keyword in diagnosisKeywords)
            {
                int index = suggestion.IndexOf(keyword);
                if (index >= 0 && (diagnosisIndex == -1 || index < diagnosisIndex))
                {
                    diagnosisIndex = index;
                    usedDiagnosisKeyword = keyword;
                }
            }

            // 查找治疗标题
            foreach (var keyword in treatmentKeywords)
            {
                int index = suggestion.IndexOf(keyword);
                if (index >= 0 && (treatmentIndex == -1 || index < treatmentIndex))
                {
                    treatmentIndex = index;
                    usedTreatmentKeyword = keyword;
                }
            }

            // ✅ 确保索引在有效范围内
            if (diagnosisIndex >= suggestion.Length) diagnosisIndex = -1;
            if (treatmentIndex >= suggestion.Length) treatmentIndex = -1;

            // ✅ 安全的子字符串提取
            if (diagnosisIndex >= 0)
            {
                // 计算诊断部分的结束位置
                int diagnosisEnd = (treatmentIndex > diagnosisIndex && treatmentIndex < suggestion.Length)
                    ? treatmentIndex
                    : suggestion.Length;

                diagnosis = suggestion.Substring(diagnosisIndex, diagnosisEnd - diagnosisIndex).Trim();
            }

            if (treatmentIndex >= 0 && treatmentIndex < suggestion.Length)
            {
                treatment = suggestion.Substring(treatmentIndex).Trim();
            }

            // ✅ 处理只找到一种信息的情况
            if (diagnosisIndex >= 0 && treatmentIndex < 0)
            {
                // 尝试在诊断文本中分割
                var parts = diagnosis.Split(new[] { "治疗方案", "治疗建议" }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    diagnosis = parts[0].Trim();
                    treatment = parts[1].Trim();
                }
            }

            // ✅ 清理文本
            diagnosis = CleanSectionText(diagnosis, usedDiagnosisKeyword);
            treatment = CleanSectionText(treatment, usedTreatmentKeyword);

            return (diagnosis, treatment);
        }

        // ✅ 响应文本清理
        private string CleanResponseText(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            // 移除JSON转义字符
            input = input.Replace("\\n", "\n")
                         .Replace("\\\"", "\"")
                         .Replace("\\t", " ");

            // 合并多余换行
            input = Regex.Replace(input, @"\n{3,}", "\n\n");

            // 移除结尾的模型元数据
            input = Regex.Replace(input, @"(\n)?\[.*?\]$", "");
            input = Regex.Replace(input, @"(\n)?【.*?】$", "");

            return input.Trim();
        }

        //文本清理
        private string CleanSectionText(string input, string keyword)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            // 移除结尾的无关内容
            input = Regex.Replace(input, @"\s*/\w+$", ""); // 移除 /n /t 等
            input = Regex.Replace(input, @"\s*\d+$", "");  // 移除结尾数字

            var endPhrases = new[] {
        "以上建议仅供参考",
        "请咨询专业医生",
        "具体请以实际就诊为准",
        "祝您健康",
        "建议及时就医"
    };

            foreach (var phrase in endPhrases)
            {
                if (input.EndsWith(phrase))
                {
                    input = input.Substring(0, input.Length - phrase.Length).Trim();
                }
            }

            return input.Trim();
        }


        // 静态辅助方法：构造服务并调用
        public static async Task<(string, string)> RunAsync(string AIResult, string ChiefComplain, string MedicalHistory)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var options = config.GetSection("DeepSeekOptions").Get<DeepSeekOptions>();

            if (string.IsNullOrWhiteSpace(options?.ApiKey))
            {
                throw new InvalidOperationException("DeepSeek API Key 未配置");
            }

            var service = new LargeModelService(options.ApiKey);
            return await service.GetRecommendationAsync(AIResult, ChiefComplain, MedicalHistory);
        }
    }
}