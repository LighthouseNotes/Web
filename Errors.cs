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
    public class LighthouseNotesApiException(HttpRequestMessage request, HttpResponseMessage response) : Exception
    {
        public HttpRequestMessage Request { get; } = request;
        public HttpResponseMessage Response { get; } = response;
    }
}