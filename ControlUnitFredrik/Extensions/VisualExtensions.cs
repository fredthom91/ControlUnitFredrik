using System.Collections.Generic;
using System.Windows.Media;

namespace ControlUnitFredrik.Extensions;

public static class VisualExtensions
{
    public static IEnumerable<Visual> GetVisualAncestors(this Visual? visual)
    {
        while (visual != null)
        {
            yield return visual;
            visual = VisualTreeHelper.GetParent(visual) as Visual;
        }
    }
}