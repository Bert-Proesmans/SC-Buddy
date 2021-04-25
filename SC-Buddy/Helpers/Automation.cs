using System;
using System.Diagnostics;
using System.Windows.Automation;

namespace SC_Buddy.Helpers
{
    static class AutomationHelper
    {
        /// <summary>
        ///     Retrieves the top-level window that contains the specified
        ///     UI Automation element.
        /// </summary>
        /// <param name="element">The contained element.</param>
        /// <returns>The  top-level window element.</returns>
        public static AutomationElement? GetTopLevelWindow(AutomationElement element)
        {
            AutomationElement elementParent;
            AutomationElement? node;
            var walker = TreeWalker.ControlViewWalker;
            node = element;

            try
            {
                if (node == AutomationElement.RootElement)
                {
                    return node;
                }

                // NOTE; Walk up the tree to _the child_ of the root.
                // The root itself is the desktop object!
                while (true)
                {
                    elementParent = walker.GetParent(node);

                    if (elementParent == null)
                    {
                        return null;
                    }

                    if (elementParent == AutomationElement.RootElement)
                    {
                        break;
                    }

                    node = elementParent;
                }
            }
            catch (ElementNotAvailableException)
            {
                // EXPECTED; Element has dissapeared (suddenly), menu items are likely to do so
                node = null;
            }
            catch (ArgumentNullException)
            {
                node = null;
            }

            return node;
        }

        public static AutomationElement? GetFirstParentElement(AutomationElement element, Condition condition, CacheRequest? cacheRequest = default, int maxJumps = int.MaxValue)
        {
            if (maxJumps < 0) throw new ArgumentOutOfRangeException(nameof(maxJumps));

            try
            {
                // ERROR; For fucks sake why doesn't Firefox return the same info to me as the Inspect tool displays!?
                // Why do I get a different, custom ID, control type while the Inspect tool resolves just fine?
                var walker = TreeWalker.ControlViewWalker;
                var node = element;
                cacheRequest ??= new CacheRequest();

                for (int i = 0; i < maxJumps && (node != null); ++i)
                {
                    if (node == AutomationElement.RootElement)
                    {
                        // WARN; It's possible that RootElement matches or not with the provided condition!
                        return node.FindFirst(TreeScope.Element, condition);
                    }

                    var elementParent = walker.GetParent(node);
                    using (cacheRequest.Activate())
                    {
                        // NOTE; Verify that the current element matches the provided condition before returning.
                        var elementMatch = elementParent?.FindFirst(TreeScope.Element, condition);
                        if (elementMatch != null)
                        {
                            return elementMatch;
                        }
                    }

                    Debug.WriteLine($"Non matching parent element skipped: {elementParent?.Current.ControlType.LocalizedControlType} " +
                        $"({elementParent?.Current.ControlType.Id.ToString("X4")})");
                    node = elementParent;
                }
            }
            catch (ElementNotAvailableException)
            {
                // EXPECTED; Element has dissapeared (suddenly), menu items are likely to do so
                return null;
            }
            catch (ArgumentNullException)
            {
                return null;
            }

            return null;
        }
    }
}
