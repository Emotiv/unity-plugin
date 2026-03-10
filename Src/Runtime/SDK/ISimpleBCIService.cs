using System;
using System.Threading.Tasks;
using Emotiv.Cortex.Models;
using EmotivUnityPlugin;

namespace Emotiv.Cortex.Service
{
    public interface ISimpleBCIService
    {
        Task<CortexResult<EmoProfile>> LoadProfileAsync();
        Task<CortexResult> StartTrainingAsync(string action, bool autoAccept = true);
        Task<CortexResult> AcceptTrainingAsync();
        Task<CortexResult> RejectTrainingAsync();
    }
}
