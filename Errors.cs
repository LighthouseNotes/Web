// ReSharper disable ClassNeverInstantiated.Global

namespace Web;

public class LighthouseNotesErrors
{
    [Serializable]
    public class ShouldNotBeNullException : Exception
    {
        public ShouldNotBeNullException()
        {
        }

        public ShouldNotBeNullException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    [Serializable]
    public class LighthouseNotesApiException : Exception
    {
        public LighthouseNotesApiException(HttpRequestMessage request, HttpResponseMessage response)
        {
            Request = request;
            Response = response;
        }

        public HttpRequestMessage Request { get; }
        public HttpResponseMessage Response { get; }
    }
}