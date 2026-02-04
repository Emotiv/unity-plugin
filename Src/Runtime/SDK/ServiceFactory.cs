namespace Emotiv.Cortex.Service
{
    public static class ServiceFactory
    {
        public static IAuthService GetAuthService()
        {
            return AuthService.Instance;
        }
    }
}
