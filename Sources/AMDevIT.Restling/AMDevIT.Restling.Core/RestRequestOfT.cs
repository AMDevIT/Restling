namespace AMDevIT.Restling.Core
{
    public class RestRequest<T>(string uri,
                                HttpMethod method,
                                T? requestData,
                                string? customMethod = null)
        : RestRequest(uri, 
               method, 
               customMethod)
    {
        #region Fields

        private T? requestData = requestData;

        #endregion

        #region Properties

        public T? RequestData
        {
            get => this.requestData;
            set => this.requestData = value;
        }

        #endregion
    }
}
