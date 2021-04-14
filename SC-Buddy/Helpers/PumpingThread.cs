using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace SC_Buddy.Helpers
{
    class PumpingThread : IDisposable
    {
        record ThreadState(TaskCompletionSource<Dispatcher> InitHandler, IObserver<Exception>? ErrorStream);

        private static int _count = 0;
        private bool disposedValue;
        private readonly Thread _thread;
        private readonly Subject<Exception> _exceptions;

        private PumpingThread(Thread t, Dispatcher d, Subject<Exception> e) =>
            (_thread, Dispatcher, _exceptions, Errors) = (t, d, e, e.AsObservable());

        public Dispatcher Dispatcher { get; }

        public IObservable<Exception> Errors { get; }

        public static async Task<PumpingThread> Create()
        {
            var entryCount = Interlocked.Increment(ref _count);
            var tsc = new TaskCompletionSource<Dispatcher>();
            var errorIngress = new Subject<Exception>();
            var thread = new Thread(ThreadEntry);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Name = $"WrappedThread_{entryCount}";
            thread.IsBackground = true;
            thread.Start(new ThreadState(tsc, errorIngress));

            // ERROR; We must wait for the thread to setup our dispatcher because
            // FromThread(..) doesn't create a new one!
            var dispatcher = await tsc.Task;
            if (dispatcher == null)
                throw new InvalidOperationException($"Dispatcher for wrapped thread is missing, cannot continue..");

            // TODO; Do we need to agree on contract that the dispatcher is guaranteed to be running at
            // the point of return?
            return new PumpingThread(thread, dispatcher, errorIngress);
        }

        /// <summary>
        /// Method that continuously pumps messages on the target thread.
        /// </summary>
        /// <param name="state"></param>
        static void ThreadEntry(object? state)
        {
            if (state is not ThreadState _thisIdentifierDoesntInferProperly_)
            {
                // NOTE; Attempt to bring down the entire application because this error is effectively non-recoverable.
                var exc = new InvalidOperationException($"Thread state is not of type {nameof(ThreadState)} as expected! This is a big developer OOPSIE!");
                var capturedException = ExceptionDispatchInfo.Capture(exc);
                Application.Current.Dispatcher.InvokeAsync(() => capturedException.Throw());
                // WARN; We MUST return since Application is terminated asynchronously!
                return;
            }

            // TODO; Remove weird compiler inference bug workaround
            (state as ThreadState)!.Deconstruct(out TaskCompletionSource<Dispatcher> tsc, out IObserver<Exception>? errorStream);

            try
            {
                // NOTE; This call effectively creates a new dispatcher attached to this thread.
                var threadDispatcher = Dispatcher.CurrentDispatcher;
                Debug.WriteLine($"Dispatcher for thread {Thread.CurrentThread.ManagedThreadId} is {threadDispatcher.Thread.ManagedThreadId}");

                var job = threadDispatcher.InvokeAsync(() => tsc.SetResult(threadDispatcher));
                job.Aborted += (_, _) => tsc.SetCanceled();

                // WARN; Exceptions thrown from Dispatcher jobs are _not_ thrown by Dispatcher.Run(..)!
                if (errorStream != null)
                {
                    threadDispatcher.UnhandledException += (_, e) => errorStream.OnNext(e.Exception);
                }

                Dispatcher.Run();
            }
            catch (Exception e)
            {
                // NOTE; Thread startup errors happen in this exception block.
                if (tsc.TrySetException(e) == false)
                {
                    // NOTE; Do nothing, task is already transitioned.
                }

                errorStream?.OnNext(e);
            }
            finally
            {
                errorStream?.OnCompleted();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (Dispatcher.HasShutdownStarted == false)
                    {
                        // WARN; Thread should cleanup automatically when dispatcher stops processing.
                        // But there is no guarantee, use a synchronous method for properly terminating
                        // the wrapped thread.
                        Dispatcher.BeginInvokeShutdown(DispatcherPriority.Send);
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            System.GC.SuppressFinalize(this);
        }
    }
}
