namespace Emotiv.Cortex.Models
{
    public readonly struct CortexError
    {
        public CortexErrorCode Code { get; }
        public string Message { get; }
        public int? RawCortexCode { get; }

        public CortexError(
            CortexErrorCode code,
            string message,
            int? rawCortexCode = null)
        {
            Code = code;
            Message = message;
            RawCortexCode = rawCortexCode;
        }
    }
}
