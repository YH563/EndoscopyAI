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

namespace EndoscopyAI.Services
{
    public class DeepSeekOptions  // 专用配置类，为deepseek准备
    {
        public string ApiKey { get; set; }
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
                        content = "作为消化科专家，请根据以下内容，给出100字内的专业诊断报告。严格按照如下要求：给出两段内容，一段是“诊断结果”，包括“症状+疾病判断”；一段是“治疗方案”，包括“用药与后期检查方案”。"//背景exprience，可修改
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
}
