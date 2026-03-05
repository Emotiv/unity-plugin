using System;
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
        /// If streams is not null, subscribes to the specified data streams using DataSampleType (e.g., DataSampleType.DevInfo, DataSampleType.MentalCommand).
        /// The DataSampleType will be mapped to the corresponding stream name automatically.
        /// </summary>
        /// <param name="headsetId">The ID of the headset to connect.</param>
        /// <param name="mappings">Optional. Channel mappings for the EPOC FLEX headset only.</param>
        /// <param name="streams">Optional. List of data stream (sample) types to subscribe (e.g., DataSampleType.DevInfo, DataSampleType.MentalCommand).</param>
        /// <returns>Success or fail result. No session info returned.</returns>
        Task<CortexResult> ConnectHeadsetAsync(
            string headsetId,
            Dictionary<string, string> mappings = null,
            IReadOnlyList<DataSampleType> streams = null
        );

        /// <summary>
        /// Disconnects the headset with the specified headsetId.
        /// </summary>
        /// <param name="headsetId">The ID of the headset to disconnect.</param>
        /// <returns>Success or fail result.</returns>
        Task<CortexResult> DisconnectHeadsetAsync(string headsetId);

        /// <summary>
        /// Takes the latest sample of the specified subscribed data stream type.
        /// </summary>
        /// <param name="streamType">The type of the data stream (sample) (must be subscribed, e.g., DataSampleType.DevInfo, DataSampleType.MentalCommand).</param>
        /// <param name="sample">Output parameter for the latest sample.</param>
        /// <returns>True if a sample is available; otherwise, false.</returns>
        bool TakeLatestSample(DataSampleType streamType, out DataSample sample);
    }
}
