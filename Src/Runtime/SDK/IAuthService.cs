using System.Threading.Tasks;
using Emotiv.Cortex.Models;
using EmotivUnityPlugin; // TODO (Tung Nguyen): remove this line when move all old code to new SDK

namespace Emotiv.Cortex.Service
{
    public interface IAuthService
    {

        /// <summary>
        /// Get some basic information about the user such as emotiv id, eula accepted or not
        /// </summary>
        /// <returns>
        /// A <see cref="CortexResult{T}"/> containing the resulting <see cref="UserDataInfo"/> or error.
        /// </returns>
        Task<CortexResult<UserDataInfo>> GetApiInfoAsync();

        /// <summary>
        /// To init authorization, the API should be called after GetApiInfoAsync. 
        /// If the user is already logged in, this will complete authorization and return user info. Otherwise, it will return an error indicating that no user is logged in.
        /// </summary>
        /// <returns>
        /// A <see cref="CortexResult{T}"/> containing the resulting <see cref="UserDataInfo"/> or error.
        /// </returns>
        Task<CortexResult<UserDataInfo>> InitAsync();

        /// <summary>
        /// Starts the interactive login flow and attempts authorization. The API should be called after GetApiInfoAsync and the result indicates that no user is logged in.
        /// The Login will open an embedded browser window for the user to login and authorize the application. 
        /// After the user completes the flow, the method will return the result of the authorization attempt.
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