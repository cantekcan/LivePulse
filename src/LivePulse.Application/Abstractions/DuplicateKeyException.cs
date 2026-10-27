namespace LivePulse.Application.Abstractions;

public sealed class DuplicateKeyException(Exception innerException)
    : Exception("A row with the same unique key already exists.", innerException);
