using SC_Buddy.UI;
using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace SC_Buddy
{
    public class Program : Application
    {
        private readonly CompositeDisposable _subscriptions = new();
        private ExceptionHandler? _exceptionHandler;
        private MouseHook? _hook;
        private UIAutomation? _automation;
        private FeedbackHandler? _feedbackHandler;

        private int raw_i = 0;
        private int sampled_i = 0;

        [STAThread]
        public static int Main() => new Program().Run();

        protected override async void OnStartup(StartupEventArgs e)
        {
            SetupStressTest();

            _exceptionHandler = new ExceptionHandler(this);
            _hook = await MouseHook.Singleton;
            _automation = new UIAutomation();
            _feedbackHandler = new FeedbackHandler(new Highlight(), new DebugMarker());

            _hook.Errors.Subscribe(_exceptionHandler.Ingress);

            var rawSubscription = _hook.Raw.Subscribe(x => Interlocked.Increment(ref raw_i));
            _subscriptions.Add(rawSubscription);

            var streamSubscription = _hook.Stream.Subscribe(x => Interlocked.Increment(ref sampled_i));
            _subscriptions.Add(streamSubscription);

            // NOTE; _hook.Stream is already pushing backpressured updates through the threadpool
            // so we don't have to change sheduler and stuff at this point.
            var mouseInput = _hook.Stream.Subscribe(_automation.Ingress);
            _subscriptions.Add(mouseInput);

            var superchats = _automation.Egress.Subscribe(_feedbackHandler.Ingress);
            _subscriptions.Add(superchats);

            var mouseInput2 = _hook.Raw.Subscribe(_feedbackHandler.IngressMouseMovements);
            _subscriptions.Add(mouseInput2);

            // DEBUG; Self-destruct strategy
            _ = Task.Run(async () =>
            {
                Debug.WriteLine("***** WARN; Program will shut down automatically in 10 minutes *****");
                await Task.Delay(TimeSpan.FromMinutes(10));
                Debug.WriteLine("***** SHUTTING DOWN *****");
                await Task.Delay(TimeSpan.FromSeconds(1));
                _ = Dispatcher.BeginInvoke((Action)Shutdown);
            });
        }

        [Conditional("DEBUG")]
        private static void SetupStressTest()
        {
            var timer = new DispatcherTimer();
            timer.Tick += (_, _) => GC.Collect();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Start();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Debug.WriteLine($"Exiting on thread {Thread.CurrentThread.Name} ({Thread.CurrentThread.ManagedThreadId})");
            _subscriptions?.Dispose();
            _hook?.Dispose();
            _automation?.Dispose();
            _feedbackHandler?.Dispose();
            Debug.WriteLine($"Raw count: {raw_i}");
            Debug.WriteLine($"Sampled count: {sampled_i}");
        }
    }
}
