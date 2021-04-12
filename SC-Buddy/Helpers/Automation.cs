using System;
using System.Windows.Automation;

namespace SC_Buddy.Helpers
{
    static class Automation
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
    }
}
