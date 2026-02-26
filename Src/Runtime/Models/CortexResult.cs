namespace Emotiv.Cortex.Models
{
    public readonly struct CortexResult
    {
        public bool IsSuccess { get; }
        public CortexError Error { get; }

        private CortexResult(bool isSuccess, CortexError error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        public static CortexResult Success()
            => new CortexResult(true, default);

        public static CortexResult Fail(CortexError error)
            => new CortexResult(false, error);
    }

    public readonly struct CortexResult<T>
    {
        public bool IsSuccess { get; }
        public T Data { get; }
        public CortexError Error { get; }

        private CortexResult(bool isSuccess, T data, CortexError error)
        {
            IsSuccess = isSuccess;
            Data = data;
            Error = error;
        }

        public static CortexResult<T> Success(T data)
            => new CortexResult<T>(true, data, default);

        public static CortexResult<T> Fail(CortexError error)
            => new CortexResult<T>(false, default, error);
    }
}
