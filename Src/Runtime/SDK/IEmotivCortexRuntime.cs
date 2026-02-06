using System;

namespace Emotiv.Cortex.Service
{
    public interface IEmotivCortexRuntime : IDisposable
    {
        IAuthService Auth { get; }
        IHeadsetService Headset { get; }
    }
}