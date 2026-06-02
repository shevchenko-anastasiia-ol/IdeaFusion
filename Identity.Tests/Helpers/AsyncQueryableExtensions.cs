using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace Identity.Tests.Helpers;

internal static class AsyncQueryableExtensions
{
    public static IQueryable<T> AsAsyncQueryable<T>(this IEnumerable<T> source)
        => new AsyncQueryable<T>(source);
}

internal class AsyncQueryable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public AsyncQueryable(IEnumerable<T> enumerable) : base(enumerable) { }
    public AsyncQueryable(Expression expression) : base(expression) { }

    IQueryProvider IQueryable.Provider => new AsyncQueryableProvider<T>(this);

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new AsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
}

internal class AsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public AsyncEnumerator(IEnumerator<T> inner) => _inner = inner;

    public T Current => _inner.Current;

    public ValueTask<bool> MoveNextAsync() => new(_inner.MoveNext());

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return default;
    }
}

internal class AsyncQueryableProvider<T> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    public AsyncQueryableProvider(IQueryProvider inner) => _inner = inner;

    public IQueryable CreateQuery(Expression expression) => new AsyncQueryable<T>(expression);

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        => new AsyncQueryable<TElement>(expression);

    public object? Execute(Expression expression) => _inner.Execute(expression);

    public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

    public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
        => new AsyncQueryable<TResult>(expression);

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
    {
        var result = Execute(expression);
        var resultType = typeof(TResult).GetGenericArguments()[0];
        return (TResult)typeof(Task)
            .GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType)
            .Invoke(null, [result])!;
    }
}
