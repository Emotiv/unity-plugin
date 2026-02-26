using System.Threading.Tasks;
using Emotiv.Cortex.Models;
using EmotivUnityPlugin; // TODO (Tung Nguyen): remove this line when move all old code to new SDK

namespace Emotiv.Cortex.Service
{
    public interface IAuthService
    {

        /// <summary>
        /// Initializes the SDK and attempts authorization using an already logged-in user.
        /// </summary>
        /// <returns>
        /// A <see cref="CortexResult{T}"/> containing the resulting <see cref="UserDataInfo"/> or error.
        /// </returns>
        Task<CortexResult<UserDataInfo>> GetApiInfoAsync();

        /// <summary>
        /// Initializes the SDK and completes authorization using an already logged-in user.
        /// </summary>
        /// <returns>
        /// A <see cref="CortexResult{T}"/> containing the resulting <see cref="UserDataInfo"/> or error.
        /// </returns>
        Task<CortexResult<UserDataInfo>> InitAsync();

        /// <summary>
        /// Starts the interactive login flow and attempts authorization.
        /// </summary>
        /// <returns>
        /// A <see cref="CortexResult{T}"/> containing the resulting <see cref="UserDataInfo"/> or error.
        /// </returns>
        Task<CortexResult<UserDataInfo>> LoginAsync();

        /// <summary>
        /// Logs out the current user and clears local auth state.
        /// </summary>
        void Logout();
    }
}