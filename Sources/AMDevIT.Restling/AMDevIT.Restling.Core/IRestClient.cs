namespace AMDevIT.Restling.Core
{
    public interface IRestClient
    {
        #region Properties

        bool DisposeHttpClient
        {
            get;
            set;
        }

        bool Disposed
        {
            get;
        }

        #endregion

        #region Methods

        Task<RestRequestResult<T>> GetAsync<T>(string uri, CancellationToken cancellationToken = default);
           

        #endregion
    }
}
