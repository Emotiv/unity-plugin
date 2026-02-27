namespace Emotiv.Cortex.Models
{
    internal static class CortexErrorMapper
    {
        public static CortexError FromErrorCode(CortexErrorCode code)
        {
            return new CortexError(
                code,
                code.ToString(),
                null);
        }

        public static CortexError FromRawCortex(int rawCode, string message)
        {
            var mapped = MapToUnifiedError(rawCode);

            return new CortexError(
                mapped,
                message,
                rawCode);
        }

        private static CortexErrorCode MapToUnifiedError(int rawCode)
        {
            // Minimal mapping logic.
            // If no mapping rule exists, return CortexErrorCode.UnknownError.
            return CortexErrorCode.UnknownError;
        }
    }
}
