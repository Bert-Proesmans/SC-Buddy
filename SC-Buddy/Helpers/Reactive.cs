using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;

namespace SC_Buddy.Helpers
{
    static class Reactive
    {
        public static IObservable<Point> IgnoreDelta(this IObservable<Point> source, double xDiff, double yDiff)
        {
            Point? lastKnownValue = null;
            var absXDiff = Math.Abs(xDiff);
            var absDiffY = Math.Abs(yDiff);

            return Observable.Create<Point>(next =>
                source.Subscribe(newPoint =>
                {
                    var isBiggerThanDelta = false;
                    if (lastKnownValue.HasValue)
                    {
                        isBiggerThanDelta |= Math.Abs(lastKnownValue.Value.X - newPoint.X) > absXDiff;
                        isBiggerThanDelta |= Math.Abs(lastKnownValue.Value.Y - newPoint.Y) > absDiffY;
                    }

                    if (lastKnownValue.HasValue == false || isBiggerThanDelta)
                    {
                        lastKnownValue = newPoint;
                        next.OnNext(newPoint);
                    }
                },
                next.OnError,
                next.OnCompleted));
        }

        /// <summary>
        /// Find out what the hell is happening in reactive flows..
        /// </summary>
        /// <typeparam name="TSource">The item that's passed downstream reactive operators</typeparam>
        /// <param name="source">Upstream reactive pipeline</param>
        /// <param name="reference">Unique string to identify trace point</param>
        /// <returns>Wrapped reactive pipeline</returns>
        // SOURCE; https://stackoverflow.com/a/24349671/3620622
        public static IObservable<TSource> Trace<TSource>(this IObservable<TSource> source, string reference)
        {
            int id = 0;
            return Observable.Create<TSource>(observer =>
            {
                int registration = ++id;
                void trace(string step, object? payload) =>
                    Debug.WriteLine($"{reference}-{registration}: {step}({payload}), Thread: {Thread.CurrentThread.Name}({Thread.CurrentThread.ManagedThreadId})");
                trace("Subscribe", string.Empty);

                IDisposable disposable = source.Subscribe(
                    v => { trace("OnNext", v); observer.OnNext(v); },
                    e => { trace("OnError", string.Empty); observer.OnError(e); },
                    () => { trace("OnCompleted", string.Empty); observer.OnCompleted(); });
                return () => { trace("Dispose", string.Empty); disposable.Dispose(); };
            });
        }
    }
}
