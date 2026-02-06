using System.Collections.Generic;
using System.Threading.Tasks;
using Emotiv.Cortex.Models;
using EmotivUnityPlugin;

namespace Emotiv.Cortex.Service
{
    public interface IHeadsetService
    {
        /// <summary>
        /// Scans for available headsets and queries their status. Automatically retries the process when warning 142 is received (scanning finish warning).
        /// The API only returns success or fail (no data). To get the headset list, use GetHeadsets().
        /// </summary>
        /// <returns>Success or fail result. No headset data returned.</returns>
        Task<CortexResult> ScanHeadsetAsync();

        /// <summary>
        /// Retrieves the list of available headsets. This is a synchronous API.
        /// </summary>
        /// <returns>List of available headsets.</returns>
        List<Headset> GetHeadsets();

        /// <summary>
        /// Connects to the headset with the specified headsetId. If not already connected, creates a working session with the headset.
        /// If streams is not null, subscribes to the specified data streams (e.g., "eeg", "mot", "dev", ...).
        /// </summary>
        /// <param name="headsetId">The ID of the headset to connect.</param>
        /// <param name="mappings">Optional. Channel mappings for the EPOC FLEX headset only.</param>
        /// <param name="streams">Optional. List of data streams to subscribe (e.g., "eeg", "mot", "dev").</param>
        /// <returns>Session info for the connected headset.</returns>
        Task<CortexResult<SessionInfo>> ConnectHeadsetAsync(
            string headsetId,
            Dictionary<string, string> mappings = null,
            IReadOnlyList<string> streams = null
        );

        /// <summary>
        /// Disconnects the headset with the specified headsetId.
        /// </summary>
        /// <param name="headsetId">The ID of the headset to disconnect.</param>
        /// <returns>Success or fail result.</returns>
        Task<CortexResult> DisconnectHeadsetAsync(string headsetId);

        /// <summary>
        /// Takes the latest sample of the specified subscribed data stream.
        /// </summary>
        /// <param name="stream">The name of the data stream (must be subscribed).</param>
        /// <param name="sample">Output parameter for the latest sample.</param>
        /// <returns>True if a sample is available; otherwise, false.</returns>
        bool TakeLatestSample(string stream, out IDataSample sample);
    }
}
