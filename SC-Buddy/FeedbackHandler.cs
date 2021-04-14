using SC_Buddy.Data;
using SC_Buddy.Helpers;
using SC_Buddy.Model;
using SC_Buddy.UI;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SC_Buddy
{
    class FeedbackHandler : IDisposable
    {
        enum CircuitBreaker { Open, Closed };
        private readonly Subject<SuppaChatto> _ingress;
        private readonly Subject<Point> _ingressMouseMovements;
        private readonly Dispatcher _uiThread;
        private readonly DispatcherTimer _hideTimer;
        private readonly Highlight _highlight;
        private readonly DebugMarker _debug;
        private readonly CompositeDisposable _subscriptions;

        private bool disposedValue;

        public FeedbackHandler(Highlight highlight, DebugMarker debug)
        {
            _ingress = new Subject<SuppaChatto>();
            _ingressMouseMovements = new Subject<Point>();
            _highlight = highlight;
            _debug = debug;
            _uiThread = highlight.Dispatcher;
            _hideTimer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, HideTimerTick, _uiThread);
            _hideTimer.Stop();

            _subscriptions = new CompositeDisposable();

            var sub1 = _ingress
                .Where(x => x.DollaDollaBill?.Currency != null && x.DollaDollaBill?.Amount != null)
                .ObserveOn(_uiThread)
                .Subscribe(x =>
            {
                _hideTimer.Stop();
                UpdateDebugWindow(x);
                UpdateHighlight(x);
            });
            _subscriptions.Add(sub1);

            var popupOpened = Observable.FromEventPattern<EventHandler, EventArgs>(
                h => _highlight.Opened += h,
                h => _highlight.Opened -= h)
                .Select(_ => Unit.Default);
            var popupClosed = Observable.FromEventPattern<EventHandler, EventArgs>(
                h => _highlight.Closed += h,
                h => _highlight.Closed += h)
                .Select(_ => Unit.Default);

            var whenMouseEntered = Observable.FromEventPattern<MouseEventHandler, MouseEventArgs>(
                h => _highlight.MouseEnter += h,
                h => _highlight.MouseEnter -= h)
                .Select(_ => CircuitBreaker.Closed);
            var whenMouseLeaves = Observable.FromEventPattern<MouseEventHandler, MouseEventArgs>(
                h => _highlight.MouseLeave += h,
                h => _highlight.MouseLeave -= h)
                .Select(_ => CircuitBreaker.Open);

            var circuit = whenMouseEntered
                .Merge(whenMouseLeaves)
                // NOTE; Circuit starts off in OPEN state
                .StartWith(CircuitBreaker.Open)
                // WARN; Keep circuit condition as state for new subscriptions!
                .Replay(1);

            var cancelDispatchTimer = whenMouseEntered
                .Subscribe(x => { Debug.WriteLine($"cancelDispatchTimer: {x}"); _hideTimer.Stop(); });
            _subscriptions.Add(cancelDispatchTimer);

            var startDispatcherTimer = popupOpened
                .Select(_ => _ingressMouseMovements)
                .Switch()
                // NOTE; Create dead zone to catch mouse drift
                .IgnoreDelta(5, 5)
                .CombineLatest(circuit)
                .Where(x => x.Second == CircuitBreaker.Open)
                .TakeUntil(popupClosed)
                .Repeat()
                .Subscribe(x => { Debug.WriteLine($"startDispatcherTimer: {x}"); _hideTimer.Start(); });
            _subscriptions.Add(startDispatcherTimer);

            _subscriptions.Add(circuit.Connect());
        }

        public IObserver<SuppaChatto> Ingress => _ingress;
        public IObserver<Point> IngressMouseMovements => _ingressMouseMovements;

        private void UpdateHighlight(SuppaChatto data)
        {
            if (_highlight.DataContext is not SuperChatVM vm)
                throw new InvalidOperationException($"Highlight VM is supposed to be {nameof(SuperChatVM)}, man!");

            vm.Name = data.DollaDollaBill?.Name ?? "UNKNOWN";

            var symbol = data.DollaDollaBill?.Currency?.Symbol.Value ?? "?";
            var amount = data.DollaDollaBill?.Amount?.ToString(CultureInfo.InvariantCulture) ?? "XXXX";
            vm.Amount = $"{symbol}{amount}";

            var scData = FetchSuperChatData(data.DollaDollaBill?.Currency, data.DollaDollaBill?.Amount);
            vm.HeaderBackground = new SolidColorBrush(scData.HeaderBackground);
            vm.TextBackground = scData.TextBackground.HasValue ? new SolidColorBrush(scData.TextBackground.Value) : null;


            _highlight.PlacementRectangle = data.BoundingBox;
            _highlight.IsOpen = true;
        }

        private static SuperChat FetchSuperChatData(ISOCurrency? currency, decimal? amount)
        {
            if (currency is null || amount is null)
                return SuperChatData.UNKNOWN;

            var superchatData = SuperChatData.OrderedSuperchats;
            if (currency != SuperChatData.KNOWN_CURRENCY)
                amount = Currencies.ConvertCurrency(SuperChatData.KNOWN_CURRENCY, currency, amount.Value)?.Value;

            foreach (var targetData in superchatData)
            {
                if (amount >= targetData.MinimumValuta.Value)
                    return targetData;
            }

            return SuperChatData.UNKNOWN;
        }

        [Conditional("DEBUG")]
        private void UpdateDebugWindow(SuppaChatto x)
        {
            _debug.Left = x.BoundingBox.Left;
            _debug.Top = x.BoundingBox.Top;
            _debug.Width = x.BoundingBox.Width;
            _debug.Height = x.BoundingBox.Height;
            _debug.Visibility = Visibility.Visible;
        }

        private void HideTimerTick(object? sender, EventArgs e)
        {
            if (_highlight.IsOpen != true) return;

            Debug.WriteLine("Closing HIGHLIGHT");
            _highlight.IsOpen = false;
            _highlight.HorizontalOffset = 0;
            _highlight.VerticalOffset = 0;

            if (_debug.IsVisible)
            {
                _debug.Hide();
            }

            _hideTimer.Stop();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _subscriptions.Dispose();
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
