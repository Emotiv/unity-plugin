using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Emotiv.Cortex.Models;
using EmotivUnityPlugin;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Emotiv.Cortex.Service
{
    public class SimpleBCIService : ISimpleBCIService
    {
        private const string DefaultDetection = "mentalCommand";
        private readonly CortexRuntimeContext _context;
        private readonly CortexClient _client;
        private readonly object _lock = new object();
        public event EventHandler<SysEventArgs> SysEventsReceived; 
        private readonly Dictionary<string, string> _loadedProfilesByHeadsetId = new Dictionary<string, string>(StringComparer.Ordinal);

        private string _currentHeadsetId;

        public SimpleBCIService(CortexRuntimeContext context, CortexClient client)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _client.StreamDataReceived += OnStreamDataReceived;
        }

        public async Task<CortexResult<EmoProfile>> LoadProfileAsync()
        {
            // get the first connected headset id from context if connected headset > 0, otherwise return error
            var connectedHeadsetIds = _context.ConnectedHeadsetIds;
            if (connectedHeadsetIds.Count == 0)
            {
                return CortexResult<EmoProfile>.Fail(CortexErrorMapper.FromErrorCode(CortexErrorCode.NoConnectedHeadset));
            }

            var headsetId = connectedHeadsetIds.First();
            // build default profile id based on headset id, e.g., "EPOC-123456" -> "EPOC_profile"
            var profileName = BuildDefaultProfileName(headsetId);

            var tcs = new TaskCompletionSource<CortexResult<EmoProfile>>();
            EventHandler<JArray> queryHandler = null;
            EventHandler<string> createHandler = null;
            EventHandler<string> loadHandler = null;

            void Cleanup()
            {
                _client.QueryProfileOK -= queryHandler;
                _client.CreateProfileOK -= createHandler;
                _client.LoadProfileOK -= loadHandler;
            }

            void CompleteSuccess(EmoProfile profile)
            {
                Cleanup();
                 tcs.TrySetResult(CortexResult<EmoProfile>.Success(profile));
            }

            void CompleteFailure(CortexError error)
            {
                Cleanup();
                tcs.TrySetResult(CortexResult<EmoProfile>.Fail(error));
            }

            loadHandler = (sender, loadedProfileName) =>
            {
                UnityEngine.Debug.Log($"Profile loaded: {loadedProfileName} for headset: {headsetId}");
                if (!string.Equals(loadedProfileName, profileName, StringComparison.Ordinal))
                {
                    return;
                }

                lock (_lock)
                {
                    _loadedProfilesByHeadsetId[headsetId] = loadedProfileName;
                    _currentHeadsetId = headsetId;
                }

                CompleteSuccess(new EmoProfile
                {
                    ProfileName = loadedProfileName,
                    SupportedDevices = Array.Empty<string>()
                });
            };

            createHandler = (sender, createdProfileName) =>
            {
                UnityEngine.Debug.Log($"Profile created: {createdProfileName} for headset: {headsetId}");
                if (!string.Equals(createdProfileName, profileName, StringComparison.Ordinal))
                {
                    return;
                }
                // load profile after creation
                LoadProfile(profileName, headsetId);
            };

            queryHandler = (sender, profiles) =>
            {
                UnityEngine.Debug.Log($"Profile query result for headset: {headsetId}, profiles: {profiles}");
                if (profiles == null)
                {
                    profiles = new JArray();
                }

                if (IsProfileExisted(profiles, profileName))
                {
                    LoadProfile(profileName, headsetId);
                    return;
                }
                // create new profile if the default profile does not exist
                CreateProfile(profileName, headsetId);
            };

            _client.QueryProfileOK += queryHandler;
            _client.CreateProfileOK += createHandler;
            _client.LoadProfileOK += loadHandler;

            // query profile
            _client.QueryProfile();

            return await tcs.Task;
        }

        public Task<CortexResult> StartTrainingAsync(string action, bool autoAccept = true)
        {
            if (string.IsNullOrWhiteSpace(action))
            {
                return Task.FromResult(CortexResult.Fail(
                    new CortexError(CortexErrorCode.TrainingFailed, "Training action is required")));
            }

            string headsetId;
            lock (_lock)
            {
                headsetId = _currentHeadsetId;
            }

            if (string.IsNullOrEmpty(headsetId))
            {
                return Task.FromResult(CortexResult.Fail(
                    CortexErrorMapper.FromErrorCode(CortexErrorCode.NoConnectedHeadset)));
            }

            var tcs = new TaskCompletionSource<CortexResult>();
            EventHandler<SysEventArgs> handler = null;

            void Cleanup()
            {
                SysEventsReceived -= handler;
            }

            handler = (sender, e) =>
            {
                if (e == null || !string.Equals(e.Detection, DefaultDetection, StringComparison.Ordinal))
                {
                    return;
                }

                switch (e.EventMessage)
                {
                    case "MC_Started":
                        UnityEngine.Debug.Log("Training started.");
                        return;
                    case "MC_Succeeded":
                        if (autoAccept)
                        {
                            AcceptTraining(headsetId, action);
                            return;
                        }
                        tcs.TrySetResult(CortexResult.Success());
                        return;
                    case "MC_Failed":
                        Cleanup();
                        tcs.TrySetResult(CortexResult.Fail(
                            new CortexError(CortexErrorCode.TrainingFailed, "Training failed")));
                        return;
                    case "MC_Completed":
                        Cleanup();
                        tcs.TrySetResult(CortexResult.Success());
                        return;
                    default:
                        UnityEngine.Debug.Log($"Unknown training event: {e.EventMessage}");
                        Cleanup();
                        tcs.TrySetResult(CortexResult.Fail(
                            CortexErrorMapper.FromErrorCode(CortexErrorCode.TrainingFailed)));
                        return;
                }
            };

            SysEventsReceived += handler;
            StartTraining(headsetId, action);

            return tcs.Task;
        }

        public Task<CortexResult> AcceptTrainingAsync()
        {
            string headsetId;
            lock (_lock)
            {
                headsetId = _currentHeadsetId;
            }

            if (string.IsNullOrEmpty(headsetId))
            {
                return Task.FromResult(CortexResult.Fail(
                    CortexErrorMapper.FromErrorCode(CortexErrorCode.NoConnectedHeadset)));
            }

            var tcs = new TaskCompletionSource<CortexResult>();
            EventHandler<SysEventArgs> handler = null;

            void Cleanup()
            {
                SysEventsReceived -= handler;
            }

            handler = (sender, e) =>
            {
                if (e == null || !string.Equals(e.Detection, DefaultDetection, StringComparison.Ordinal))
                {
                    return;
                }

                Cleanup();

                if (e.EventMessage == "MC_Completed")
                {
                    tcs.TrySetResult(CortexResult.Success());
                }
                else
                {
                    tcs.TrySetResult(CortexResult.Fail(
                        new CortexError(CortexErrorCode.TrainingFailed, "Training not completed")));
                }
            };

            SysEventsReceived += handler;
            AcceptTraining(headsetId, string.Empty);

            return tcs.Task;
        }

        public Task<CortexResult> RejectTrainingAsync()
        {
            string headsetId;
            lock (_lock)
            {
                headsetId = _currentHeadsetId;
            }

            if (string.IsNullOrEmpty(headsetId))
            {
                return Task.FromResult(CortexResult.Fail(
                    CortexErrorMapper.FromErrorCode(CortexErrorCode.NoConnectedHeadset)));
            }

            var tcs = new TaskCompletionSource<CortexResult>();
            EventHandler<SysEventArgs> handler = null;

            void Cleanup()
            {
                SysEventsReceived -= handler;
            }

            handler = (sender, e) =>
            {
                if (e == null || !string.Equals(e.Detection, DefaultDetection, StringComparison.Ordinal))
                {
                    return;
                }

                Cleanup();

                if (e.EventMessage == "MC_Rejected")
                {
                    tcs.TrySetResult(CortexResult.Success());
                }
                else
                {
                    tcs.TrySetResult(CortexResult.Fail(
                        new CortexError(CortexErrorCode.TrainingFailed, "Training not rejected")));
                }
            };

            SysEventsReceived += handler;
            RejectTraining(headsetId, string.Empty);

            return tcs.Task;
        }

        private string BuildDefaultProfileName(string headsetId)
        {
            // get headset type string from headsetId, e.g., "EPOC-123456" -> "EPOC"
            if (string.IsNullOrEmpty(headsetId))
            {
                throw new ArgumentNullException(nameof(headsetId));
            }

            var parts = headsetId.Split('-');
            if (parts.Length > 0)
            {
                // profile id format: "{headsetType}_profile", e.g., "EPOC-123456" -> "EPOC_profile"
                return $"{parts[0]}_profile";
            }

            // return empty profile id if headsetId format is unexpected
            return "";
        }

        private static bool IsProfileExisted(JArray profiles, string profileName)
        {
            foreach (JObject ele in profiles)
            {
                var name = (string)ele["name"];
                if (!string.Equals(name, profileName, StringComparison.Ordinal))
                {
                    continue;
                }
                return true;
            }

            return false;
        }

        private void CreateProfile(string profileName, string headsetId)
        {
            _client.SetupProfile(profileName, "create", headsetId);
        }

        private void LoadProfile(string profileName, string headsetId)
        {
            _client.SetupProfile(profileName, "load", headsetId);
        }

        private void UnLoadProfile(string profileName, string headsetId)
        {
            _client.SetupProfile(profileName, "unload", headsetId);
        }

        private void SaveProfile(string profileName, string headsetId)
        {
            _client.SetupProfile(profileName, "save", headsetId);
        }

        private void StartTraining(string headsetId, string action)
        {
            _client.Training(headsetId, "start", DefaultDetection, action);
        }

        private void AcceptTraining(string headsetId, string action)
        {
            _client.Training(headsetId, "accept", DefaultDetection, action);
        }

        private void RejectTraining(string headsetId, string action)
        {
            _client.Training(headsetId, "reject", DefaultDetection, action);
        }

        private void EraseTraining(string headsetId, string action)
        {
            _client.Training(headsetId, "erase", DefaultDetection, action);
        }

        private void OnStreamDataReceived(object sender, StreamDataEventArgs e)
        {
            if (e == null || string.IsNullOrEmpty(e.StreamName))
            {
                return;
            }

            if (e.StreamName == "sys" && e.Data != null)
            {
                double time         = Convert.ToDouble(e.Data[0]);
                string detection    = Convert.ToString(e.Data[1]);
                string eventMsg     = Convert.ToString(e.Data[2]);
                UnityEngine.Debug.Log($"Sys event received: time={time}, detection={detection}, eventMsg={eventMsg}");
                SysEventArgs sysEvent = new SysEventArgs(time, detection, eventMsg);
                
                SysEventsReceived(this, sysEvent);
            }

        }
    }
}
