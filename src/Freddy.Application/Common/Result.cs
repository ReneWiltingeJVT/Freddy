#pragma warning disable CA1000 // Do not declare static members on generic types — Result<T> factory methods are idiomatic

namespace Freddy.Application.Common;

public sealed class Result<T>
{
    private Result(T? value, string? error, ResultType type)
    {
        Value = value;
        Error = error;
        Type = type;
    }

    public T? Value { get; }

    public string? Error { get; }

    public ResultType Type { get; }

    public bool IsSuccess => Type == ResultType.Success;

    public bool IsFailure => !IsSuccess;

    public static Result<T> Success(T value) =>
        new(value: value, error: null, type: ResultType.Success);

    public static Result<T> Failure(string error) =>
        new(value: default, error: error, type: ResultType.Error);

    public static Result<T> NotFound(string error = "Resource not found.") =>
        new(value: default, error: error, type: ResultType.NotFound);

    public static Result<T> ValidationError(string error) =>
        new(value: default, error: error, type: ResultType.ValidationError);
}
