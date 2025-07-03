using EndoscopyAI.Services;
using System.IO;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace EndoscopyAI.ViewModels.SubViewModels
{
    public interface IAIRecommendation
    {
        // 获得诊断建议和治疗方案
        Task<(string DiagnosisAdvice, string TreatmentPlan)> GetRecommendationAsync(
            string AIResult, string ChiefComplain, string MedicalHistory);
    }

    public class AIRecommendation : IAIRecommendation
    {
        public async Task<(string DiagnosisAdvice, string TreatmentPlan)> GetRecommendationAsync(
            string AIResult, string ChiefComplain, string MedicalHistory)
        {
            return await LargeModelService.RunAsync(AIResult, ChiefComplain, MedicalHistory);
        }
    }
}