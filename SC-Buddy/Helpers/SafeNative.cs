using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using Vanara.PInvoke;

namespace SC_Buddy.Helpers
{
    public static class SafeNative
    {
        /// <summary>
        /// Places a low-level mouse hook on the desktop session.
        /// </summary>
        /// <remarks>
        /// The thread that registers this hook must pump window messages!
        /// 
        /// SafeHHOOK will automatically call UnhookWindowsHookEx on dispose!
        /// This is possible because UnhookWindowsHook can be called from any thread.
        /// </remarks>
        /// <param name="hookFunc">The function to invoke</param>
        /// <param name="runner">The (message pumping) thread controller to register the hook on</param>
        /// <returns>Task that completes with a safe handle for the allocated resources.</returns>
        /// <seealso cref="MouseHook"/>
        public static Task<User32.SafeHHOOK> HookLowLevelMouse(User32.HookProc hookFunc, Dispatcher runner)
        {
            if (hookFunc is null) throw new ArgumentNullException(nameof(hookFunc));
            if (runner is null) throw new ArgumentNullException(nameof(runner));

            User32.SafeHHOOK Unsafe()
            {
                var hookResult = User32.SetWindowsHookEx(User32.HookType.WH_MOUSE_LL, hookFunc, default, 0);
                Win32Error.ThrowLastErrorIfInvalid(hookResult, "Failed to set low-level mouse hook");
                return hookResult;
            }

            return runner.InvokeAsync(Unsafe).Task;
        }
    }
}
