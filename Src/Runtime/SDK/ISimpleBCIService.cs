using System;
using System.Threading.Tasks;
using Emotiv.Cortex.Models;
using EmotivUnityPlugin;

namespace Emotiv.Cortex.Service
{
    /// <summary>
    /// Runs BCI workflows after HeadsetService opens a session. Uses the first connected
    /// supported headset and manages one internal profile bound to that headset type so
    /// users do not need to manage profile details.
    /// </summary>
    public interface ISimpleBCIService
    {
        /// <summary>
        /// Creates and loads a training profile for the first connected headset. The
        /// profile is bound to the headset type (for example, "MN8_profile", "INSIGHT_profile");
        /// if it already exists, it is loaded. Returns the profile on success.
        /// </summary>
        /// <returns>Success with profile info, or fail with error.</returns>
        Task<CortexResult<EmoProfile>> LoadProfileAsync();

        /// <summary>
        /// Starts training for a mental command action (for example, neutral, pull, push).
        /// If autoAccept is true, the result is accepted automatically on success. If false,
        /// call AcceptTrainingAsync or RejectTrainingAsync after a successful result.
        /// </summary>
        /// <param name="action">The mental command action to train.</param>
        /// <param name="autoAccept">Whether to auto-accept a successful training result.</param>
        /// <returns>Success when training completes, or fail with error.</returns>
        Task<CortexResult> StartTrainingAsync(string action, bool autoAccept = true);

        /// <summary>
        /// Accepts the current training result manually. Call only after a successful
        /// StartTrainingAsync when autoAccept is false.
        /// </summary>
        /// <returns>Success or fail result.</returns>
        Task<CortexResult> AcceptTrainingAsync();

        /// <summary>
        /// Rejects the current training result manually. Call only after a successful
        /// StartTrainingAsync when autoAccept is false.
        /// </summary>
        /// <returns>Success or fail result.</returns>
        Task<CortexResult> RejectTrainingAsync();
    }
}
