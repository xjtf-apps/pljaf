using System.Windows;

namespace voks.server.records
{
    public static class FrameworkElementExtensions
    {
        public static FrameworkElement? GetFirstParent(this FrameworkElement element, Predicate<FrameworkElement> predicate)
        {
            var parent = element.Parent;
            if (parent == null) return null;
            if (parent is FrameworkElement fParent)
            {
                if (predicate(fParent)) return fParent;
                else return fParent.GetFirstParent(predicate);
            }
            return null;
        }
    }
}
