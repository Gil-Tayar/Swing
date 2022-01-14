using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Swing
{
    public partial class MainForm : Form
    {
        // --- Consts ---
        const string APP_NAME = "Swing";

        const int WM_HOTKEY = 0x0312;
        const int MAXIMIZE_HOTKEY_ID = 000005;
        const int MINIMIZE_HOTKEY_ID = 000006;

        const int MOVE_LEFT_HOTKEY_ID = 000001;
        const int MOVE_RIGHT_HOTKEY_ID = 000002;
        const int MOVE_UP_HOTKEY_ID = 000003;
        const int MOVE_DOWN_HOTKEY_ID = 000004;

        const string REGISTRY_STARTUP = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        string REGISTRY_SWING = String.Format("SOFTWARE\\{0}", APP_NAME);
        const string REGISTRY_FIRST_RUN = "DidRunOnce";

        // how much to jump aside for searching new window
        const int WINDOW_BUFFER = 20;
        const int WINDOW_SPLIT_SIZE = 10;

        
        
        // --- Variables --- 
        IntPtr lastMaximizedHwnd = IntPtr.Zero;

        // 
        enum Direction
        {
            Left = 1,
            Right = 2,
            Up = 3,
            Down = 4
        }

        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Initialize();
        }

        public void Initialize()
        {
            // Alt = 1, Ctrl = 2, Shift = 4, Win = 8; also can sum them up, 3 = alt and ctrl
            WinApi.RegisterHotKey(this.Handle, MAXIMIZE_HOTKEY_ID, 3, (int)Keys.Enter);     // ctrl+alt + enter
            WinApi.RegisterHotKey(this.Handle, MINIMIZE_HOTKEY_ID, 5, (int)Keys.Enter);     // shift+alt + enter

            WinApi.RegisterHotKey(this.Handle, MOVE_LEFT_HOTKEY_ID, 3, (int)Keys.Left);     // ctrl+alt + left
            WinApi.RegisterHotKey(this.Handle, MOVE_RIGHT_HOTKEY_ID, 3, (int)Keys.Right);   // ctrl+alt + right
            WinApi.RegisterHotKey(this.Handle, MOVE_UP_HOTKEY_ID, 3, (int)Keys.Up);         // ctrl+alt + up
            WinApi.RegisterHotKey(this.Handle, MOVE_DOWN_HOTKEY_ID, 3, (int)Keys.Down);     // ctrl+alt + down

            SetStartup();
            TrayMenuContext();
            this.Visible = false;

            if (IsFirstRun()) {
                FirstTimeForm form = new FirstTimeForm();
                form.Show();
            }
        }

        private void TrayMenuContext()
        {
            this.notifyIcon.ContextMenuStrip = new ContextMenuStrip();
            this.notifyIcon.ContextMenuStrip.Items.Add("Exit", null, this.CloseApplication);
        }

        protected override void WndProc(ref Message m)
        {
            // catch the registerd HotKey and reactivates the window
            if (m.Msg == WM_HOTKEY)
            {
                int hotkeyId = m.WParam.ToInt32();

                switch (hotkeyId)
                {
                    case MAXIMIZE_HOTKEY_ID:
                        HandleMaximizeEvent();
                        break;

                    case MINIMIZE_HOTKEY_ID:
                        HandleMinimizeEvent();
                        break;

                    case MOVE_LEFT_HOTKEY_ID:
                    case MOVE_RIGHT_HOTKEY_ID:
                    case MOVE_UP_HOTKEY_ID:
                    case MOVE_DOWN_HOTKEY_ID:
                        HandleMoveToWindowEvent((Direction)hotkeyId);
                        break;
                }
            }

            base.WndProc(ref m);
        }

        private void SetStartup()
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey(REGISTRY_STARTUP, true);

            if (rk.GetValue(APP_NAME) == null)
                rk.SetValue(APP_NAME, Application.ExecutablePath);
        }

        private bool IsFirstRun()
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey(REGISTRY_SWING, true);
            
            if (rk == null)
            {
                // this is first run, create Key and return
                rk = Registry.CurrentUser.CreateSubKey(REGISTRY_SWING);
            }

            if (rk == null)
            {
                // unhandeled error
                return false;
            }

            object regValue = rk.GetValue(REGISTRY_FIRST_RUN);
            bool? didRunOnce = false;
            
            if (regValue != null)
            {
                try
                {
                    didRunOnce = Convert.ToBoolean(regValue);
                }
                catch
                {
                    // someone messed up the content of the registry value, continue
                }
            }

            if (didRunOnce == false)
            {
                // first run
                rk.SetValue(REGISTRY_FIRST_RUN, true);
                return true;
            }

            return false;
        }

        private void HandleMaximizeEvent()
        {
            // get foreground window
            IntPtr hwnd = WinApi.GetForegroundWindow();

            if (hwnd != lastMaximizedHwnd)
            {
                // make it maximize
                WinApi.ShowWindow(hwnd, WinApi.ShowWindowCommands.Maximize);
                lastMaximizedHwnd = hwnd;
            }
            else
            {
                // restore the window
                WinApi.ShowWindow(hwnd, WinApi.ShowWindowCommands.Normal);
                lastMaximizedHwnd = IntPtr.Zero;
            }
        }

        private void HandleMinimizeEvent()
        {
            // get foreground window
            IntPtr hwnd = WinApi.GetForegroundWindow();
            WinApi.ShowWindow(hwnd, WinApi.ShowWindowCommands.Minimize);
        }

        private void HandleMoveToWindowEvent(Direction direction)
        {
            IntPtr hwnd = WinApi.GetForegroundWindow();
            IntPtr nextHwnd = GetNeighborWindow(hwnd, direction);
            if (nextHwnd != IntPtr.Zero)
            {
                WinApi.SetForegroundWindow(nextHwnd);
            }
        }

        private IntPtr GetNeighborWindow(IntPtr hWnd, Direction direction)
        {
            // hWnd - the window we find neighbor of
            // direction - the direction of the neighbor

            // get the current window coordinates
            WinApi.RECT currentWindow;
            WinApi.GetWindowRect(hWnd, out currentWindow);

            int windowSectionSize, xStart, xEnd, yStart, yEnd;

            switch (direction)
            {
                case Direction.Left:
                    windowSectionSize = currentWindow.Left / WINDOW_SPLIT_SIZE;
                    xStart = currentWindow.Left - windowSectionSize;
                    xEnd = 0;
                    yStart = currentWindow.Top + WINDOW_BUFFER;
                    yEnd = currentWindow.Bottom;

                    return GetWindowInRang(xStart, xEnd, yStart, yEnd, windowSectionSize);

                case Direction.Right:
                    windowSectionSize = (Screen.PrimaryScreen.Bounds.Width - currentWindow.Right) / WINDOW_SPLIT_SIZE;
                    xStart = currentWindow.Right + windowSectionSize;
                    xEnd = Screen.PrimaryScreen.Bounds.Width;
                    yStart = currentWindow.Top + WINDOW_BUFFER;
                    yEnd = currentWindow.Bottom;

                    return GetWindowInRang(xStart, xEnd, yStart, yEnd, windowSectionSize);

                case Direction.Up:
                    windowSectionSize = (Screen.PrimaryScreen.Bounds.Width - currentWindow.Left) / WINDOW_SPLIT_SIZE;
                    xStart = currentWindow.Left + windowSectionSize;
                    xEnd = currentWindow.Right;
                    yStart = currentWindow.Top - WINDOW_BUFFER;
                    yEnd = 0;

                    return GetWindowInRang(xStart, xEnd, yStart, yEnd, windowSectionSize);

                case Direction.Down:
                    windowSectionSize = (Screen.PrimaryScreen.Bounds.Width - currentWindow.Left) / WINDOW_SPLIT_SIZE;
                    xStart = currentWindow.Left + windowSectionSize;
                    xEnd = currentWindow.Right;
                    yStart = currentWindow.Bottom + WINDOW_BUFFER;
                    yEnd = Screen.PrimaryScreen.Bounds.Height;

                    return GetWindowInRang(xStart, xEnd, yStart, yEnd, windowSectionSize);
            }

            return IntPtr.Zero;
        }

        private IntPtr GetWindowInRang(int xStart, int xEnd, int yStart, int yEnd, int windowSectionSize)
        {
            if (xStart < 0 || xStart > Screen.PrimaryScreen.Bounds.Width || yStart < 0 || yStart > Screen.PrimaryScreen.Bounds.Height)
                return IntPtr.Zero;

            int windowBuffer = WINDOW_BUFFER;
            if (yStart > yEnd)
            {
                yStart = -yStart;
            }
            if (xStart > xEnd)
            {
                xStart = -xStart;
            }

            for (int x = xStart; x < xEnd; x += windowSectionSize)
            {
                for (int y = yStart; y < yEnd; y += windowBuffer)
                {
                    IntPtr window = GetWindowByPoint(Math.Abs(x), Math.Abs(y));
                    if (window != IntPtr.Zero)
                        return window;
                }
            }
            return IntPtr.Zero;
        }

        private IntPtr GetWindowByPoint(int x, int y)
        {
            IntPtr hWnd = WinApi.GetWindowHandleFromPoint(x, y);

            if (hWnd == IntPtr.Zero)
                return IntPtr.Zero;

            string title = WinApi.GetWindowTitle(hWnd);
            if (title != String.Empty && !title.Equals("Program Manager")) // really weird bug
            {
                return hWnd;
            }
            return IntPtr.Zero;
        }

        private void CloseApplication(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
