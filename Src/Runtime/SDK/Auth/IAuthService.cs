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
        /// A tuple containing the <see cref="CortexErrorCode"/> and the resulting <see cref="UserDataInfo"/>.
        /// </returns>
        Task<(CortexErrorCode Code, UserDataInfo User)> InitAndAuthorizeAsync();

        /// <summary>
        /// Starts the interactive login flow and attempts authorization.
        /// </summary>
        /// <returns>
        /// A tuple containing the <see cref="CortexErrorCode"/> and the resulting <see cref="UserDataInfo"/>.
        /// </returns>
        Task<(CortexErrorCode Code, UserDataInfo User)> LoginAndAuthorizeAsync();

        /// <summary>
        /// Logs out the current user and clears local auth state.
        /// </summary>
        void Logout();
    }
}