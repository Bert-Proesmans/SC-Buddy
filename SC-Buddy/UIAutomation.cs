using SC_Buddy.Helpers;
using SC_Buddy.Model;
using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Automation;

namespace SC_Buddy
{
    public record SuppaChatto(Rect BoundingBox, Simp DollaDollaBill);
    public record Simp(ISOCurrency? Currency, decimal? Amount, string? Name, string? Message);

    class UIAutomation : IDisposable
    {
        record AutomationDetails(
            CacheRequest CacheRequest,
            AutomationElement Target,
            AutomationElement? Root,
            SuppaChatto? Result);

        private readonly Subject<Point> _ingress;
        private bool disposedValue;

        public UIAutomation()
        {
            _ingress = new Subject<Point>();

            Egress = ((IObservable<Point>)_ingress)
                .ObserveOn(ThreadPoolScheduler.Instance)
                // .Trace(nameof(FormulateElement))
                .Select(FormulateElement)
                .Where(x => x != null)
                .Select(x => x!)
                .Select(StraightUpFindAmount)
                // ___
                .Select(x => x.Result ?? new SuppaChatto(default, new(default, default, default, default)))
                .Publish()
                .RefCount();
        }

        public IObserver<Point> Ingress => _ingress;

        public IObservable<SuppaChatto> Egress { get; }

        static AutomationDetails? FormulateElement(Point p)
        {
            // NOTE; Cache data to speed up stuffs
            var cacheRequest = new CacheRequest();
            cacheRequest.Add(AutomationElement.NameProperty);
            cacheRequest.Add(AutomationElement.BoundingRectangleProperty);
            cacheRequest.Add(AutomationElement.IsContentElementProperty);
            cacheRequest.Add(ValuePattern.Pattern);
            cacheRequest.Add(ValuePattern.ValueProperty);
            cacheRequest.Add(TableItemPattern.Pattern);

            // TODO; IsTextChildPatternAvailable
            // TODO; IsTableItemPatternAvailable

            // WARN; Disables on-demand property access through property Current
            cacheRequest.AutomationElementMode = AutomationElementMode.None;

            AutomationElement? cachedElement;
            try
            {
                using (cacheRequest.Activate())
                {
                    cachedElement = AutomationElement.FromPoint(new Point(p.X, p.Y));
                }
            }
            catch (Exception)
            {
                return null;
            }

            var result = new SuppaChatto(cachedElement.Cached.BoundingRectangle, new(default, default, default, default));
            return new AutomationDetails(cacheRequest, cachedElement, default, result);
        }

        AutomationDetails StraightUpFindAmount(AutomationDetails state)
        {
            if (state.Result?.DollaDollaBill?.Currency != default ||
                state.Result?.DollaDollaBill?.Amount != default) return state;

            // NOTE; Attempt 1, use name of element
            var (Currency, Amount) = Currencies.ExtractCurrency(state.Target.Cached.Name);

            // CASE; Name wasn't succesfully used, fallback on value pattern
            if (Currency == null || Amount == null)
            {
                bool hasValue = state.Target.TryGetCachedPattern(ValuePattern.Pattern, out object valueObj);
                // ERROR; It doesn't seem that we can get the actual selected text to further slice the amount of
                // possibilities to transmogrify.
                //bool hasSelection = state.Target.TryGetCachedPattern(SelectionItemPattern.Pattern, out object selectionObj);

                if (hasValue /*|| !hasSelection*/)
                {
                    var valuePattern = (ValuePattern)valueObj;
                    var value = valuePattern.Cached.Value;
                    (Currency, Amount) = Currencies.ExtractCurrency(value);
                }
            }

            // CASE; No currency or amount detected, no fallback left
            if (Currency == null || Amount == null)
            {
                return state;
            }

            // TODO; Introduce fricking lenses, djeezus
            var old = state.Result;
            var bb = old?.BoundingBox ?? default;
            var ddb = new Simp(Currency, Amount, old?.DollaDollaBill?.Message ?? default, old?.DollaDollaBill?.Name ?? default);
            var newResult = new SuppaChatto(bb, ddb);
            return new(state.CacheRequest, state.Target, state.Root, newResult);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _ingress.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
