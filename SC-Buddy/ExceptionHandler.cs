using SC_Buddy.Helpers;
using SC_Buddy.UI;
using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Windows;

namespace SC_Buddy
{
    class ExceptionHandler : ExceptionhandlerBase, IDisposable
    {
        private readonly CompositeDisposable _disposables;
        private readonly Subject<Exception> _ingress;
        private bool disposedValue;

        public ExceptionHandler(Application a) : base(a)
        {
            _disposables = new CompositeDisposable();
            _ingress = new Subject<Exception>();
            var registration = _ingress.Subscribe((e) => OnUnhandledException(e));
            _disposables.Add(registration);
            Ingress = _ingress.AsObserver();
        }

        public IObserver<Exception> Ingress { get; }

        protected override bool OnUnhandledException(Exception e)
        {
            _application.Dispatcher.InvokeAsync(() =>
            {
                new ExceptionWindow(_application, e).Show();
            });
            return true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _disposables.Dispose();
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
