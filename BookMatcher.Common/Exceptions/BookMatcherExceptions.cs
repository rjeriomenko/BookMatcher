namespace BookMatcher.Common.Exceptions;

public class LlmServiceException : Exception
{
    public LlmServiceException(string message) : base(message) { }
    public LlmServiceException(string message, Exception innerException) : base(message, innerException) { }
}

public class OpenLibraryServiceException : Exception
{
    public OpenLibraryServiceException(string message) : base(message) { }
    public OpenLibraryServiceException(string message, Exception innerException) : base(message, innerException) { }
}
