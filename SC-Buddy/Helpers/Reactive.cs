using System;
using System.Reactive.Linq;
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
    }
}
