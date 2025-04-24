// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
using System;
using System.Threading.Tasks;

namespace R3SubscribeAnalyzer.Sample;

// If you don't see warnings, build the Analyzers Project.

public class Examples
{
    private readonly Button button = new();

    public void Main()
    {
        var observable = new Observable();
        observable.Subscribe(() => { }); // warning R3SUB001
        observable.Subscribe(() => { }).Anything(); // warning R3SUB001
        observable.Subscribe(() => { }).Dispose(); // warning R3SUB001
        observable.SubscribeAwait<int>((_) => ValueTask.CompletedTask); // warning R3SUB001
        observable.SubscribeAwait<int>((_) => ValueTask.CompletedTask).AddTo(this); // OK
        observable.Subscribe(() => { }).Anything().AddTo(this); // OK
        observable.Subscribe(() => { }).AddTo(this).Anything(); // OK
        observable.Subscribe(() => { }).AddTo(this).Anything().AddTo(this); // OK
        observable.Subscribe(() => { }).AddTo(this); // OK
        observable.Subscribe(() => { }).AddTo(this).AddTo(this); // OK
        var disposable = observable.Subscribe(() => { }); // OK
        using var disposable2 = observable.Subscribe(() => { }); // OK

        Observable.Interval(TimeSpan.FromSeconds(1))
            .Select((_, i) => i)
            .Where(x => x % 2 == 0)
            .Subscribe(() => { }); // warning R3SUB001
        Observable.Interval(TimeSpan.FromSeconds(1))
            .Select((_, i) => i)
            .Where(x => x % 2 == 0)
            .Subscribe(() => { }).AddTo(this); // OK

        Task.Run(async () => await button.OnClickAsObservable().ForEachAsync(_ => { })); // OK
    }
}

internal class Button
{
    public Observable OnClickAsObservable()
    {
        return new Observable();
    }
}

public class Observable : IDisposable
{
    public static Observable Interval(TimeSpan _)
    {
        return new Observable();
    }
    
    public Observable Select(Func<object, int, object> _)
    {
        return this;
    }
    
    public Observable Where(Func<int, bool> _)
    {
        return this;
    }
    
    public IDisposable Subscribe(Action _)
    {
        return this;
    }

    public IDisposable SubscribeAwait<T>(Func<T, ValueTask> _)
    {
        return this;
    }

    public void Dispose()
    {
    }

    public ValueTask ForEachAsync(Action<object> _)
    {
        return ValueTask.CompletedTask;
    }
}

public static class DisposableExtensions
{
    public static IDisposable AddTo(this IDisposable disposable, object _)
    {
        return disposable;
    }
    
    public static IDisposable Anything(this IDisposable disposable)
    {
        return disposable;
    }
}