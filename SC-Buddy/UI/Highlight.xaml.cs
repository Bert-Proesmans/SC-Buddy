using System.Windows.Controls.Primitives;

namespace SC_Buddy.UI
{
    /// <summary>
    /// Interaction logic for Highlight.xaml
    /// </summary>
    public partial class Highlight : Popup
    {
        public Highlight()
        {
            InitializeComponent();

            MouseDown += (sender, e) =>
            {
                MoveThumb.RaiseEvent(e);
            };

            MoveThumb.DragDelta += (sender, e) =>
            {
                HorizontalOffset += e.HorizontalChange;
                VerticalOffset += e.VerticalChange;
            };
        }
    }
}
