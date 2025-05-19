using EndoscopyAI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EndoscopyAI.ViewModels.SubViewModels
{
    interface IAIRecommendation
    {
        // 获得诊断建议和治疗方案
        Task<(string DiagnosisAdvice, string TreatmentPlan)> GetRecommendationAsync(
            string AIResult, string ChiefComplain, string MedicalHistory);
    }

    public class AIRecommendation : IAIRecommendation
    {
        // 异步实现
        public async Task<(string DiagnosisAdvice, string TreatmentPlan)> GetRecommendationAsync(
            string AIResult, string ChiefComplain, string MedicalHistory)
        {
            var (advice, plan) = await DeepseekDevice.Instance.GetRecommendationAsync(
                AIResult, ChiefComplain, MedicalHistory);
            return (advice, plan);
        }
    }

}
