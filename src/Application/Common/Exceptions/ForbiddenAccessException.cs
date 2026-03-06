namespace MyHomeSolution.Application.Common.Exceptions;

public sealed class ForbiddenAccessException()
    : Exception("You do not have permission to perform this action.");
