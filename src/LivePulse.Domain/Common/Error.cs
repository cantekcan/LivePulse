namespace LivePulse.Domain.Common;

public record Error(string Code, string Message, ErrorType Type = ErrorType.Validation);
