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

    //知识图谱查找服务，定义了方向、节点、目标、关系、标签等等
    public class Neo4jService : IAsyncDisposable
    {
        private readonly IDriver _driver;

        public Neo4jService(string uri, string user, string password)
        {
            _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));//账号密码先用的我的，影响不大
        }

        public async Task<List<Neo4jResult>> QueryEntitiesByKeywordAsync(string keyword)
        {
            var session = _driver.AsyncSession();
            var results = new List<Neo4jResult>();
            //知识图谱查询代码，不能随意改动
            try
            {
                var cypher = @"
                    MATCH (n)-[r]-(m)
                    WHERE any(label IN labels(n) WHERE label IN $validLabels)
                      AND toLower(n.name) CONTAINS toLower($keyword)
                    RETURN DISTINCT 
                        labels(n) AS sourceLabels, 
                        n.name AS sourceName, 
                        type(r) AS relationType, 
                        startNode(r).name AS fromName,
                        endNode(r).name AS toName,
                        labels(m) AS targetLabels,
                        m.name AS targetName
                ";

                var result = await session.RunAsync(cypher, new
                {
                    keyword,//从后台获取的信息关键词，让这些关键词与下面的节点匹配
                    //节点包括：疾病、症状、治疗等
                    validLabels = new[]
                    {
                        "Disease", "Symptom", "Treatment", "Drug", "AdjuvantTherapy", "Operation",
                        "Diagnosis", "Pathogenesis", "Check", "Prognosis", "Cause", "PathologicalType",
                        "Complication", "Department", "Stage", "SymptomAndSign", "TreatmentPrograms", "DrugTherapy"
                    }
                });

                await result.ForEachAsync(record =>
                {
                    var fromName = record["fromName"].As<string>();
                    var toName = record["toName"].As<string>();
                    var sourceName = record["sourceName"].As<string>();

                    var direction = fromName == sourceName ? "-->" : "<--";//双向查询关系，查询到更多相关信息

                    results.Add(new Neo4jResult
                    {
                        Source = sourceName,
                        SourceLabels = string.Join(", ", record["sourceLabels"].As<List<string>>()),
                        Relation = record["relationType"].As<string>(),
                        Direction = direction,
                        Target = record["targetName"].As<string>(),
                        TargetLabels = string.Join(", ", record["targetLabels"].As<List<string>>())
                    });//构造输出结果
                });
            }
            finally
            {
                await session.CloseAsync();
            }

            return results;
        }

        public async ValueTask DisposeAsync()
        {
            await _driver.CloseAsync();//自动关闭，不要管，在线查询，删了可能出问题
        }
    }

    public class LargeModelService
    {
        private readonly HttpClient _httpClient;
        private readonly DeepSeekOptions _options;

        // 构造函数只需HttpClient参数，即输入deepseek的api网址
        public LargeModelService(HttpClient httpClient)
        {
            _httpClient = httpClient;

        }

        public async Task<string> GenerateDiagnosisSuggestionAsync(string prompt)
        {
            var requestBody = new
            {
                model = "deepseek-chat",  // 调用DeepSeek专用模型，可以改成deepseek-reasoner（深度思考），但是推理时间会略长
                messages = new[]
                {
                    new {
                        role = "system",
                        content = "作为消化内科专家，请根据以下内容，给出100字内的专业诊断报告。严格按照如下要求：给出两段内容，一段是“诊断结果”，包括“症状+疾病判断”；一段是“治疗方案”，包括“用药与后期检查方案”。"//背景exprience，可修改
                    },
                    new { role = "user", content = prompt }//设定使用角色
                },
                temperature = 0.7,    // 更保守的参数设置，输出温度，值越大，自由度越高
                max_tokens = 200      // 显式控制输出长度，这里先设置200个tokens，在prompt里面说明100字以内
            };//api里面先充了10块钱，不够用找我

            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    "v1/chat/completions",
                    requestBody
                );

                // 处理速率限制（建议后续改用Polly重试策略，更为稳健）
                if ((int)response.StatusCode == 429)
                {
                    await Task.Delay(2000);  // 简单延迟2秒重试，避免反复请求报错
                    return await GenerateDiagnosisSuggestionAsync(prompt);
                }

                response.EnsureSuccessStatusCode();

                var stream = await response.Content.ReadAsStreamAsync();
                var doc = await JsonDocument.ParseAsync(stream);

                // DeepSeek的响应结构
                return doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString()
                    ?? "生成诊断报告失败，请检查输入内容。";
            }
            catch (Exception ex)
            {
                // 捕获错误，建议记录日志
                return $"服务暂时不可用，错误信息：{ex.Message}";
            }
        }
    }
    public sealed class DeepseekDevice
    {
        private readonly static DeepseekDevice _instance = new DeepseekDevice();
        public static DeepseekDevice Instance
        {
            get { return _instance; }
        }

        private DeepseekDevice() { }
        public async Task<(string DiagnosisAdvice, string TreatmentPlan)> GetRecommendationAsync(string AIResult, string ChiefComplain, string MedicalHistory)
        {
            //Step 1:从后台获取分类结果，一定要精确输入
            if (string.IsNullOrWhiteSpace(AIResult))
            {
                MessageBox.Show("病人症状不能为空，请重新选择", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return ("", "");//TODO
            }

            var history = MedicalHistory;
            if (string.IsNullOrWhiteSpace(history))
            {
                history = "无既往病史";
            }

            //关键词精确匹配
            var mappedKeyword = KeywordMapper.MapToMedicalTerm(AIResult);

            if (mappedKeyword == null)
            {
                Console.WriteLine("输入的关键词无效或未定义对应医学术语。");
            }//稳健性，这里应该用不到

            //如果病人健康，可以直接输出这个结果并进行出现在“诊断建议“治疗方案的框里面”
            if (mappedKeyword == "normal")
            {
                var resultText = "恭喜！病人健康，无需其他诊断建议！";
                return (resultText, resultText);//直接返回建议即可
            }

            //Neo4j的账号密码，如果登陆不上就是我关机了，请立刻私信我
            var neo4j = new Neo4jService("bolt://10.208.123.179:7687", "neo4j", "Benzion030921");

            //Step 2:根据知识图谱，输出查找的症状结果
            try
            {
                var results = await neo4j.QueryEntitiesByKeywordAsync(mappedKeyword);

                //Step 3:对话操作，请求医生的进一步输入的主诉
                var symptomInput = ChiefComplain;

                if (!string.IsNullOrWhiteSpace(symptomInput))
                {
                    // 症状之间需要分割，任何分割都是可以的
                    var Symptoms = Regex
                        .Split(symptomInput, @"[,\，\.\。\s;；\n\r]+")
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s.Trim())
                        .ToList();

                    // 启动模糊查找
                    foreach (var symptom in Symptoms)
                    {
                        var symptomResults = await neo4j.QueryEntitiesByKeywordAsync(symptom);

                        foreach (var category in new[] { "Cause", "Drug", "Operation" })
                        {
                            var filtered = symptomResults.Where(r => r.TargetLabels.Contains(category)).ToList();
                        }
                    }
                }

                // 组装诊断建议文本，考虑到使用长度，优先症状重合者先输出，且先输出前6个高频内容
                var PromptInitial = new StringBuilder();

                var symptoms = results
                    .Where(r => r.TargetLabels.Contains("Symptom") || r.TargetLabels.Contains("SymptomAndSign"))
                    .GroupBy(r => r.Target)
                    .OrderByDescending(g => g.Count())
                    .Take(6)
                    .Select(g => g.Key);

                var causes = results
                    .Where(r => r.TargetLabels.Contains("Cause"))
                    .GroupBy(r => r.Target)
                    .OrderByDescending(g => g.Count())
                    .Take(6)
                    .Select(g => g.Key);

                var drugs = results
                    .Where(r => r.TargetLabels.Contains("Drug"))
                    .GroupBy(r => r.Target)
                    .OrderByDescending(g => g.Count())
                    .Take(6)
                    .Select(g => g.Key);

                var operations = results
                    .Where(r => r.TargetLabels.Contains("Operation"))
                .GroupBy(r => r.Target)
                    .OrderByDescending(g => g.Count())
                    .Take(6)
                    .Select(g => g.Key);

                // 读取配置部分修改（注意配置路径变更）
                var config = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json", optional: false)
                            .Build();

                //Step 4:调用DeepSeek大模型生成诊断建议
                // 显式指定命名空间
                var deepseekOptions = config.GetSection("DeepSeekOptions").Get<DeepSeekOptions>();

                if (string.IsNullOrEmpty(deepseekOptions?.ApiKey))
                {
                    MessageBox.Show("DeepSeek API Key 未配置", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return ("", "");//TODO
                }

                // API端点变更，从openAI变为deepseek
                var httpClient = new HttpClient
                {
                    BaseAddress = new Uri("https://api.deepseek.com/")//deepseek基础链接
                };
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", deepseekOptions.ApiKey);
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                // 现在可以单参数初始化
                var largeModelService = new LargeModelService(httpClient);

                // 合并既往病史到finalPrompt
                var finalPrompt = $"病人既往病史：{history}\n现在的症状是：{AIResult}\n根据知识图谱，得到的诊断建议：{PromptInitial.ToString()}";

                //Step 5:DeepSeek输出，这里是需要展示的内容

                var suggestion = await largeModelService.GenerateDiagnosisSuggestionAsync(finalPrompt);
                // 假设 suggestion 格式为：强制输出deepseek的输出
                // 诊断结果：xxx。治疗方案：yyy。严格限制deepseek的输出格式
                var diagnosis = "";
                var treatment = "";

                // 尝试用关键词分割
                var diagnosisIndex = suggestion.IndexOf("诊断结果");
                var treatmentIndex = suggestion.IndexOf("治疗方案");

                if (diagnosisIndex >= 0 && treatmentIndex > diagnosisIndex)
                {
                    diagnosis = suggestion.Substring(diagnosisIndex, treatmentIndex - diagnosisIndex).Trim();
                    treatment = suggestion.Substring(treatmentIndex).Trim();
                }
                else
                {
                    // 如果格式不标准，全部拿取建议即可，需要医生进行修改
                    diagnosis = suggestion;
                    treatment = suggestion;
                }

                // 分两段输出
                //Console.WriteLine("\n【诊断结果】\n" + diagnosis + "\n");
                //Console.WriteLine("【治疗方案】\n" + treatment + "\n");

                return (diagnosis, treatment);//返回结果

            }
            finally
            {
                await neo4j.DisposeAsync();
            }
        }
    }

}
