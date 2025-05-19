using EndoscopyAI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EndoscopyAI.ViewModels.SubViewModels
{
    interface IAIRecommenation
    {
        // 获得诊断建议和治疗方案
        (string DiagnosisAdvice, string TreatmentPlan) GetRecommendation(string AIResult, string ChiefComplain, string MedicalHistory);
    }

    public class AIRecommenation : IAIRecommenation
    {
        // 获得诊断建议和治疗方案
        public (string DiagnosisAdvice, string TreatmentPlan) GetRecommendation(string AIResult, string ChiefComplain, string MedicalHistory)
        {
            var recommendationTask = DeepseekDevice.Instance.GetRecommendationAsync(AIResult, ChiefComplain, MedicalHistory);
            recommendationTask.Wait(); // 等待异步任务完成
            string advice = recommendationTask.Result.Item1; // 获取诊断建议
            string plan = recommendationTask.Result.Item2; // 获取治疗方案
            return (advice, plan);
        }
    }

}
