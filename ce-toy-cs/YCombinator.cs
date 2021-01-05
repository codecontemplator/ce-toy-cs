using System;
using System.Linq.Expressions;

namespace ce_toy_cs
{
    static class YCombinator<T, TResult>
    {
        // RecursiveFunc is not needed to call Fix() and so can be private.
        private delegate Func<T, TResult> RecursiveFunc(RecursiveFunc r);

        public static Expression<Func<Func<Func<T, TResult>, Func<T, TResult>>, Func<T, TResult>>> Fix { get; } =
            f => ((RecursiveFunc)(g => f(x => g(g)(x))))(g => f(x => g(g)(x)));
    }
}
