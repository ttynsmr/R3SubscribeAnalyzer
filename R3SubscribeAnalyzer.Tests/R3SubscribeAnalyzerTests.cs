using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing.XUnit;
using Xunit;
using Verifier =
    Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<
        R3SubscribeAnalyzer.R3SubscribeAnalyzer>;

namespace R3SubscribeAnalyzer.Tests;

public class R3SubscribeAnalyzerTests
{
    [Fact]
    public async Task Subscribe_AlertDiagnostic()
    {
        const string text = @"
using System;
using System.Threading.Tasks;

public class Program
{
    public void Main()
    {
        var observable = new Observable();
        observable.Subscribe(() => { });
    }
}

public class Observable : IDisposable
{
    public IDisposable Subscribe(Action action)
    {
        return this;
    }
    public IDisposable SubscribeAwait<T>(Func<T, ValueTask> action)
    {
        return this;
    }
    public void Dispose() {}
}

public static class DisposableExtensions
{
    public static void AddTo(this IDisposable disposable, object target) {}
}
";

        await Verifier.VerifyAnalyzerAsync(text, Verifier.Diagnostic().WithLocation(10, 9));
    }

    [Fact]
    public async Task SubscribeAwait_AlertDiagnostic()
    {
        const string text = @"
using System;
using System.Threading.Tasks;

public class Program
{
    public void Main()
    {
        var observable = new Observable();
        observable.SubscribeAwait<int>((_) => { return new ValueTask(); });
    }
}

public class Observable : IDisposable
{
    public IDisposable Subscribe(Action action)
    {
        return this;
    }
    public IDisposable SubscribeAwait<T>(Func<T, ValueTask> action)
    {
        return this;
    }
    public void Dispose() {}
}

public static class DisposableExtensions
{
    public static void AddTo(this IDisposable disposable, object target) {}
}
";

        await Verifier.VerifyAnalyzerAsync(text, Verifier.Diagnostic().WithLocation(10, 9));
    }
    
    [Fact]
    public async Task SubscribeAndPass_AlertDiagnostic()
    {
        const string text = @"
using System;
using System.Threading.Tasks;

public class Program
{
    public void Main()
    {
        var observable = new Observable();
        observable.Subscribe(() => { }).Pass();
    }
}

public class Observable : IDisposable
{
    public IDisposable Subscribe(Action action)
    {
        return this;
    }
    public IDisposable SubscribeAwait<T>(Func<T, ValueTask> action)
    {
        return this;
    }
    public void Dispose() {}
}

public static class DisposableExtensions
{
    public static IDisposable AddTo(this IDisposable disposable, object target)
    {
        return disposable;
    }
    
    public static IDisposable Pass(this IDisposable disposable)
    {
        return disposable;
    }
}
";

        await Verifier.VerifyAnalyzerAsync(text, Verifier.Diagnostic().WithLocation(10, 9));
    }
}