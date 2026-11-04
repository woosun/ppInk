using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace gInk
{
    /// <summary>Lets the user choose the monitor used for inking, from the tray or floating icon.</summary>
    public class InkingScreenMenu
    {
        private const string MenuTitle = "필기 모니터";
        private const int IconMargin = 8;
        private readonly Root Root;
        private MenuItem TrayEntry;

        /// <param name="root">Application state holding the inking area.</param>
        public InkingScreenMenu(Root root)
        {
            Root = root;
        }

        /// <summary>Falls back to the primary monitor when the saved area is not exactly one monitor.</summary>
        public void EnsureValidArea()
        {
            if (Root.WindowRect.Width <= 0 || Root.WindowRect.Height <= 0)
                return;
            if (Screen.AllScreens.Any(s => s.Bounds == Root.WindowRect))
                return;
            Root.WindowRect = Screen.PrimaryScreen.Bounds;
            Root.gpButtonsLeft = 0; // makes the toolbar return to the corner of the new area
            Root.gpButtonsTop = 0;
        }

        /// <summary>Moves the saved icon position to the inking monitor when it is off every monitor.</summary>
        public void EnsureIconVisible()
        {
            var icon = new Rectangle(Root.FormLeft, Root.FormTop, Root.FormWidth, Root.FormWidth);
            if (Screen.AllScreens.Any(s => s.Bounds.Contains(icon)))
                return;
            Rectangle area = Screen.FromRectangle(Root.WindowRect).WorkingArea;
            Root.FormLeft = area.Right - Root.FormWidth - IconMargin;
            Root.FormTop = area.Bottom - Root.FormWidth - IconMargin;
        }

        /// <summary>Adds the monitor submenu to the tray menu; safe to call after the menu is rebuilt.</summary>
        public void AttachToTrayMenu(ContextMenu tray)
        {
            TrayEntry = new MenuItem(MenuTitle);
            TrayEntry.MenuItems.AddRange(BuildItems());
            tray.MenuItems.Add(2, TrayEntry);
            tray.Popup -= RefreshTrayEntry;
            tray.Popup += RefreshTrayEntry;
        }

        /// <summary>Opens the monitor menu over a control.</summary>
        public void ShowAt(Control owner, Point position)
        {
            new ContextMenu(BuildItems()).Show(owner, position);
        }

        private void RefreshTrayEntry(object sender, EventArgs e)
        {
            TrayEntry.MenuItems.Clear();
            TrayEntry.MenuItems.AddRange(BuildItems());
        }

        private MenuItem[] BuildItems()
        {
            bool inking = Root.FormCollection?.Visible ?? false;
            return Screen.AllScreens.Select(s => new MenuItem(Describe(s), (o, e) => Select(s))
            {
                Checked = Root.WindowRect == s.Bounds,
                Enabled = !inking,
            }).ToArray();
        }

        private void Select(Screen screen)
        {
            Root.WindowRect = screen.Bounds;
            Root.gpButtonsLeft = 0; // makes the toolbar return to the corner of the new area
            Root.gpButtonsTop = 0;
            MoveIconTo(screen);
            Root.SaveOptions(Program.RunningFolder + "config.ini");
        }

        /// <summary>Moves the floating icon to the same corner offset on another monitor.</summary>
        private void MoveIconTo(Screen screen)
        {
            CallForm icon = Root.callForm;
            if (icon == null || !icon.Visible)
                return;
            Rectangle from = Screen.FromControl(icon).WorkingArea;
            Rectangle to = screen.WorkingArea;
            int left = to.Right - (from.Right - icon.Left);
            int top = to.Bottom - (from.Bottom - icon.Top);
            icon.Left = Math.Max(to.Left, Math.Min(left, to.Right - icon.Width));
            icon.Top = Math.Max(to.Top, Math.Min(top, to.Bottom - icon.Height));
            Root.FormLeft = icon.Left;
            Root.FormTop = icon.Top;
        }

        private static string Describe(Screen screen)
        {
            Rectangle b = screen.Bounds;
            string size = b.Width + "x" + b.Height;
            return screen.Primary ? "메인 (" + size + ")" : Position(b) + " (" + size + ")";
        }

        private static string Position(Rectangle b)
        {
            Rectangle p = Screen.PrimaryScreen.Bounds;
            int dx = (b.Left + b.Width / 2) - (p.Left + p.Width / 2);
            int dy = (b.Top + b.Height / 2) - (p.Top + p.Height / 2);
            string column = Math.Abs(dx) < p.Width / 4 ? "" : (dx < 0 ? "왼쪽" : "오른쪽");
            string row = Math.Abs(dy) < p.Height / 4 ? "" : (dy < 0 ? "위" : "아래");
            return (column + " " + row).Trim();
        }
    }
}
