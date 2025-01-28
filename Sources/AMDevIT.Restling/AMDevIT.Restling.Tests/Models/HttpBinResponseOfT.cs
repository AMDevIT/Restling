namespace AMDevIT.Restling.Tests.Models
{
    public class HttpBinResponse<T>
        : HttpBinResponse
    {
        #region Properties

        public T? Data { get; set; }
        public string? Json { get; set; }

        #endregion
    }
}
