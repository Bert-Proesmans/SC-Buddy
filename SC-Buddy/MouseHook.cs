using SC_Buddy.Helpers;
using System;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using Vanara.PInvoke;

namespace SC_Buddy
{
    /// <summary>
    /// Low Level mouse hooking.
    /// </summary>
    /// <remarks>
    /// So, it took me a while to properly figure this one out. Basically the following will happen in the Win-API;
    /// 1. Setup global low-level hook
    /// 1. We expect our code somehow runs on the hook event
    /// 1. The general expectation is that our container DLL gets injected into each process
    ///     1. BUT, we want a global LL_MOUSE hook
    ///     1. BUT, hooks are just a special kind of window message handlers
    ///     1. SO ->
    ///         1. Our hookFN should be executable from any process inside the active desktop
    ///         1. BUT, This hook specifically is *always* run in the context of the _thread_ that installed it
    ///         1. _thread_ here actually means _message pumping thread_ (GetMessage(..) family)
    ///         1. _message pumping thread_ actually means, typically, main thread of the process
    ///             1. !! It's not conventional to run multiple message pumps, in seperate threads, per process.
    ///                 ** According to old posts on MSDN
    ///             1. This IS doable though.. and it seems to work ¯\_(ツ)_/¯
    ///     1. ALSO ->
    ///         1. Global hooks are looked down upon because of their intrusiveness with other processess and
    ///         basically blocking their main threads.
    ///         Yeah, I wouldn't like my users having a bad experience, because of some bad hooking code, either.
    ///         1. The next best thing seems to be _Raw Input_ which promises asynchronous processing, 
    ///         but haven't looked into this yet
    /// ----
    /// 1. So the desktop session performs an execution switch by proxying into the message pumping thread of the
    /// process that installed the hook. This happens for every event from the mouse driver.
    /// 1. Which means the pumping thread gets these messages
    /// 1. Which means the hookFN is executed on top of the second dispatcher
    /// 1. This shouldn't negatively affect main thread performance, that handles timers and rendering
    ///     of UI elements.
    /// 1. It works :tm:.. but could be better.
    /// </remarks>
    class MouseHook : IDisposable
    {
        // NOTE; This funky setup is required to allow at most one mouse hook per
        // running process.
        private static readonly Lazy<Task<MouseHook>> _singleton = new(() => Task.Run(InitSingleton), false);

        private readonly PumpingThread _threadWrapper;
        private readonly Subject<Point> _stream;
        /* WARN; Object garbage collection prevention */
        private User32.SafeHHOOK? _hookHandle = null;
        private User32.HookProc? _hookDelegate = null;
        /**/
        private bool disposedValue;

        // NOTE; Cannot accept value for _hookHandle since the delegate requires an object
        // reference to MouseHook!
        private MouseHook(PumpingThread t, Subject<Point> s)
        {
            _threadWrapper = t;
            _stream = s;

            Raw = _stream.AsObservable();
            Stream = _stream
                // WARN; Perform instant backpressure on hot observable.
                // NOTE; The event is published to the threadpool for downstream processing.
                .Throttle(TimeSpan.FromMilliseconds(400), ThreadPoolScheduler.Instance)
                .Publish()
                .RefCount();
        }

        public static Task<MouseHook> Singleton => _singleton.Value;

        // NOTE; Safe access to the stream of mouse cursor positions
        public IObservable<Point> Stream { get; }

        // WARN; Unsafe access to the stream of mouse cursor positions
        public IObservable<Point> Raw { get; }

        // NOTE; Exceptions that terminated the wrapped thread
        public IObservable<Exception> Errors => _threadWrapper.Errors;

        private static async Task<MouseHook> InitSingleton()
        {
            var dataIngress = new Subject<Point>();
            // NOTE; Inlined thread creation as to not confuse myself if I should dispose seperately or not.
            var mouseHook = new MouseHook(await PumpingThread.Create(), dataIngress);

            try
            {
                await mouseHook.SetupHook();
                return mouseHook;
            }
            catch (Exception)
            {
                mouseHook.Dispose();
                throw;
            }
        }

        private async Task SetupHook()
        {
            _hookDelegate = HookCallback;
            _hookHandle = await SafeNative.HookLowLevelMouse(_hookDelegate, _threadWrapper.Dispatcher);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            // ERROR; We asynchronously set our own handle, and a race happens when
            // the callback is executed before setup completed!
            if (_hookHandle == null || nCode < 0) goto Skip;
            // NOTE; Skip all message that aren't about mouse movement.
            if ((IntPtr)User32.WindowMessage.WM_MOUSEFIRST != wParam) goto Skip;
            if (lParam == default) goto Skip;
            var hookData = Marshal.PtrToStructure<User32.MSLLHOOKSTRUCT>(lParam);
            
            var eventData = new Point(hookData.pt.X, hookData.pt.Y);
            _stream.OnNext(eventData);

        Skip:
            return User32.CallNextHookEx(default, nCode, wParam, lParam);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _hookHandle?.Dispose();
                    _threadWrapper.Dispose();
                    _stream.Dispose();
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
