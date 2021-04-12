using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace SC_Buddy.Helpers
{
    // REF; https://github.com/robibobi/dotnet-global-exceptionhandler/blob/6d99f37e7c7955a2a06884c0df5c70082a27942d/Tcoc.ExceptionHandler/ExceptionHandling/GlobalExceptionHandlerBase.cs
    abstract class ExceptionhandlerBase
    {
        class UnknownException : Exception
        {
            private readonly object? _exception;
            public UnknownException(string msg, object? exception) : base(msg) => _exception = exception;
        }

        protected readonly Application _application;

        public ExceptionhandlerBase(Application a)
        {
            AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            _application = a;
            a.DispatcherUnhandledException += OnDispatcherUnhandledException;
            a.Dispatcher.UnhandledExceptionFilter += OnDispatcherExceptionFilter;
        }

        protected virtual bool CatchDispatcherException(Exception e) => true;

        protected abstract bool OnUnhandledException(Exception e);

        private static Exception ExtractException(UnhandledExceptionEventArgs e) =>
            e.ExceptionObject as Exception ?? new UnknownException($"Unrecognized unhandled exception", e.ExceptionObject);

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            if (OnUnhandledException(e.Exception))
                e.SetObserved();
        }

        private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // var terminating = e.IsTerminating;
            OnUnhandledException(ExtractException(e));
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = OnUnhandledException(e.Exception);
        }

        private void OnDispatcherExceptionFilter(object sender, DispatcherUnhandledExceptionFilterEventArgs e)
        {
            e.RequestCatch = CatchDispatcherException(e.Exception);
        }
    }
}
