using MS.SS.Core.SharedKernel.Results;

namespace MS.SS.Core.Tests.SharedKernel;

public sealed class ResultTests
{
    [Fact]
    public void GenericResult_ImplicitFromValue_IsSuccess()
    {
        Result<string> result = "hello";

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void GenericResult_ImplicitFromException_IsFailure()
    {
        Result<string> result = new InvalidOperationException("boom");

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.IsType<InvalidOperationException>(result.Exception);
    }

    [Fact]
    public void GenericResult_ImplicitFromNullValue_IsNull()
    {
        string? value = null;
        Result<string> result = value;

        Assert.True(result.IsNull);
        Assert.False(result.IsSuccess);
        Assert.False(result.IsFailure);
    }

    [Fact]
    public void GenericResult_Match_Success_InvokesOnSuccess()
    {
        Result<int> result = 42;

        var matched = result.Match(v => $"ok:{v}", ex => $"fail:{ex.Message}");

        Assert.Equal("ok:42", matched);
    }

    [Fact]
    public void GenericResult_Match_Failure_InvokesOnFailure()
    {
        Result<int> result = new Exception("nope");

        var matched = result.Match(v => $"ok:{v}", ex => $"fail:{ex.Message}");

        Assert.Equal("fail:nope", matched);
    }

    [Fact]
    public void GenericResult_Match_Null_WithOnNull_InvokesOnNull()
    {
        Result<string> result = (string?)null;

        var matched = result.Match(v => "ok", ex => "fail", () => "null-case");

        Assert.Equal("null-case", matched);
    }

    [Fact]
    public void GenericResult_Match_Null_WithoutOnNull_Throws()
    {
        Result<string> result = (string?)null;

        Assert.Throws<InvalidOperationException>(() => result.Match(v => "ok", ex => "fail"));
    }

    [Fact]
    public void GenericResult_ImplicitToNonGenericResult_Success_IsSuccess()
    {
        Result<string> generic = "value";
        Result plain = generic;

        Assert.True(plain.IsSuccess);
    }

    [Fact]
    public void GenericResult_ImplicitToNonGenericResult_Failure_CarriesException()
    {
        Result<string> generic = new Exception("bad");
        Result plain = generic;

        Assert.True(plain.IsFailure);
        Assert.Equal("bad", plain.Exception.Message);
    }

    [Fact]
    public void GenericResult_ImplicitToNonGenericResult_Null_IsFailure()
    {
        Result<string> generic = (string?)null;
        Result plain = generic;

        Assert.True(plain.IsFailure);
        Assert.IsType<InvalidOperationException>(plain.Exception);
    }

    [Fact]
    public void NonGenericResult_Success_IsSuccess()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
    }

    [Fact]
    public void NonGenericResult_Failure_IsFailure()
    {
        var result = Result.Failure(new Exception("bad"));

        Assert.True(result.IsFailure);
        Assert.Equal("bad", result.Exception.Message);
    }

    [Fact]
    public void NonGenericResult_ImplicitFromException_IsFailure()
    {
        Result result = new InvalidOperationException("bad");

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void NonGenericResult_Match_Success_InvokesOnSuccess()
    {
        var result = Result.Success();

        var matched = result.Match(() => "ok", ex => "fail");

        Assert.Equal("ok", matched);
    }

    [Fact]
    public void NonGenericResult_Match_Failure_InvokesOnFailure()
    {
        var result = Result.Failure(new Exception("bad"));

        var matched = result.Match(() => "ok", ex => $"fail:{ex.Message}");

        Assert.Equal("fail:bad", matched);
    }
}
