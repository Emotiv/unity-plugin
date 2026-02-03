namespace Emotiv.Cortex.SDK
{
    public static class EmotivFactory
    {
        public static Auth.IAuthService GetAuthService()
        {
            return Auth.AuthService.Instance;
        }

        public static Auth.IAuthService getAuthService()
        {
            return Auth.AuthService.Instance;
        }
    }
}
