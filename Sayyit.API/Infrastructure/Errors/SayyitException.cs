namespace Sayyit.API.Infrastructure.Errors;

public sealed class SayyitException : Exception
{
    /// <summary>
    /// Creates a user-safe exception with internal diagnostics and an HTTP error status code.
    /// </summary>
    /// <param name="FriendlyMessage">Message safe to return to API callers.</param>
    /// <param name="InternalMessage">Internal diagnostic message for logs and telemetry.</param>
    /// <param name="Status">HTTP error status code in the range 400-599.</param>
    /// <exception cref="ArgumentException">Thrown when a required message is missing.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when status is not an HTTP error status code.</exception>
    public SayyitException(string FriendlyMessage, string InternalMessage, int Status)
        : base(string.IsNullOrWhiteSpace(InternalMessage)
            ? throw new ArgumentException("Internal message is required.", nameof(InternalMessage))
            : InternalMessage)
    {
        if (string.IsNullOrWhiteSpace(FriendlyMessage))
        {
            throw new ArgumentException("Friendly message is required.", nameof(FriendlyMessage));
        }

        if (Status is < 400 or > 599)
        {
            throw new ArgumentOutOfRangeException(nameof(Status), Status, "Status must be an HTTP error status code in the range 400-599.");
        }

        this.FriendlyMessage = FriendlyMessage;
        this.InternalMessage = InternalMessage;
        this.StatusCode = Status;
    }

    public string FriendlyMessage { get; }

    public string InternalMessage { get; }

    public int StatusCode { get; }
}
