using System;
using System.Diagnostics.CodeAnalysis;

namespace SS14.Launcher.Utility;

/// <summary>
/// A zero-allocation struct representing either a successful outcome with a value
/// or a failure outcome with an error.
/// </summary>
public readonly struct ValueResult<T, TError>
{
    private readonly T? _value;
    private readonly TError? _error;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    private ValueResult(T value)
    {
        IsSuccess = true;
        _value = value;
        _error = default;
    }

    private ValueResult(TError error, bool _)
    {
        IsSuccess = false;
        _value = default;
        _error = error;
    }

    public static ValueResult<T, TError> Ok(T value) => new(value);
    public static ValueResult<T, TError> Fail(TError error) => new(error, false);

    public bool TryGetValue([NotNullWhen(true)] out T? value, [NotNullWhen(false)] out TError? error)
    {
        value = _value;
        error = _error;
        return IsSuccess;
    }

    public T Value => IsSuccess ? _value! : throw new InvalidOperationException($"Cannot access Value on a failed result: {_error}");
    public TError Error => !IsSuccess ? _error! : throw new InvalidOperationException("Cannot access Error on a successful result.");

    public static implicit operator ValueResult<T, TError>(T value) => Ok(value);
}

public readonly struct ValueResult<T>
{
    private readonly T? _value;
    private readonly string? _error;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    private ValueResult(T value)
    {
        IsSuccess = true;
        _value = value;
        _error = null;
    }

    private ValueResult(string error)
    {
        IsSuccess = false;
        _value = default;
        _error = error;
    }

    public static ValueResult<T> Ok(T value) => new(value);
    public static ValueResult<T> Fail(string error) => new(error);

    public bool TryGetValue([NotNullWhen(true)] out T? value, [NotNullWhen(false)] out string? error)
    {
        value = _value;
        error = _error;
        return IsSuccess;
    }

    public T Value => IsSuccess ? _value! : throw new InvalidOperationException($"Cannot access Value on failed result: {_error}");
    public string Error => _error ?? string.Empty;

    public static implicit operator ValueResult<T>(T value) => Ok(value);
}
