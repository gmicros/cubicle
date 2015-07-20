using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Hardcodet.Wpf.TaskbarNotification;

namespace cubicle
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static TaskbarIcon tbi;
        private List<Rect> grid = new List<Rect>();
        private IntPtr hWndSelected = new IntPtr();
        private InputData buffer;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
            
            var editor = new System.Windows.Controls.MenuItem();
            editor.Header = "Editor";
            editor.Click += new RoutedEventHandler(editor_Click);

            var cont = new System.Windows.Controls.ContextMenu();
            cont.Items.Add(editor);
            
            tbi = new TaskbarIcon();
            //tbi.Icon = global::ControlSystemDialpadDisplayConfig.Properties.Resources.icon;
            tbi.ContextMenu = cont;
            tbi.ToolTipText = "cubicle";

                        
        }

        void editor_Click(object sender, RoutedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
           
            
            grid.Add(new Rect(0, 0, 960, 540));
            grid.Add(new Rect(0, 540, 960, 540));
            grid.Add(new Rect(960, 0, 960, 1080));
        }

        #region rawInput

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {            
            // get raw input data 
            buffer = new InputData();
            var rawInputSize = Marshal.SizeOf(buffer);
            GetRawInputData(lparam, DataCommand.RID_INPUT, out buffer, ref rawInputSize, Marshal.SizeOf(typeof(Rawinputheader)));

            // get cursur point
            POINT pt;
            GetCursorPos(out pt);
            txtX.Content = pt.X.ToString();
            txtY.Content = pt.Y.ToString();
            
            // middle mouse button down 
            if (buffer.data.mouse.usButtonFlags == 0x0010)
            {              
                hWndSelected = WindowFromPoint(pt);
            }
            // middle mouse button up
            if (buffer.data.mouse.usButtonFlags == 0x0020)
            {
                var temp = grid.Find(x => x.Contains(pt.X, pt.Y));
                SetWindowPos(hWndSelected, IntPtr.Zero, (int)temp.X, (int)temp.Y, (int)temp.Width, (int)temp.Height, SetWindowPosFlags.FrameChanged | SetWindowPosFlags.AsynchronousWindowPosition);
                hWndSelected = IntPtr.Zero;
            }

            return IntPtr.Zero;
        }

        private bool registerMouse(IntPtr hWnd)
        {
            RAWINPUTDEVICE[] devices = new RAWINPUTDEVICE[1];

            devices[0].WindowHandle = hWnd;
            devices[0].UsagePage = HIDUsagePage.Generic;
            devices[0].Usage = HIDUsage.Mouse;
            devices[0].Flags = RawInputDeviceFlags.InputSink;

            return RegisterRawInputDevices(devices, 1, Marshal.SizeOf(typeof(RAWINPUTDEVICE)));
        }

        #region win32

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        static extern IntPtr WindowFromPoint(POINT Point);


        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetParent(IntPtr hWnd);


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);


        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public RECT(System.Drawing.Rectangle r) : this(r.Left, r.Top, r.Right, r.Bottom) { }

            public int X
            {
                get { return Left; }
                set { Right -= (Left - value); Left = value; }
            }

            public int Y
            {
                get { return Top; }
                set { Bottom -= (Top - value); Top = value; }
            }

            public int Height
            {
                get { return Bottom - Top; }
                set { Bottom = value + Top; }
            }

            public int Width
            {
                get { return Right - Left; }
                set { Right = value + Left; }
            }

            public System.Drawing.Point Location
            {
                get { return new System.Drawing.Point(Left, Top); }
                set { X = value.X; Y = value.Y; }
            }

            public System.Drawing.Size Size
            {
                get { return new System.Drawing.Size(Width, Height); }
                set { Width = value.Width; Height = value.Height; }
            }

            public static implicit operator System.Drawing.Rectangle(RECT r)
            {
                return new System.Drawing.Rectangle(r.Left, r.Top, r.Width, r.Height);
            }

            public static implicit operator RECT(System.Drawing.Rectangle r)
            {
                return new RECT(r);
            }

            public static bool operator ==(RECT r1, RECT r2)
            {
                return r1.Equals(r2);
            }

            public static bool operator !=(RECT r1, RECT r2)
            {
                return !r1.Equals(r2);
            }

            public bool Equals(RECT r)
            {
                return r.Left == Left && r.Top == Top && r.Right == Right && r.Bottom == Bottom;
            }

            public override bool Equals(object obj)
            {
                if (obj is RECT)
                    return Equals((RECT)obj);
                else if (obj is System.Drawing.Rectangle)
                    return Equals(new RECT((System.Drawing.Rectangle)obj));
                return false;
            }

            public override int GetHashCode()
            {
                return ((System.Drawing.Rectangle)this).GetHashCode();
            }

            public override string ToString()
            {
                return string.Format(System.Globalization.CultureInfo.CurrentCulture, "{{Left={0},Top={1},Right={2},Bottom={3}}}", Left, Top, Right, Bottom);
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

        [Flags()]
        private enum SetWindowPosFlags : uint
        {
            /// <summary>If the calling thread and the thread that owns the window are attached to different input queues, 
            /// the system posts the request to the thread that owns the window. This prevents the calling thread from 
            /// blocking its execution while other threads process the request.</summary>
            /// <remarks>SWP_ASYNCWINDOWPOS</remarks>
            AsynchronousWindowPosition = 0x4000,
            /// <summary>Prevents generation of the WM_SYNCPAINT message.</summary>
            /// <remarks>SWP_DEFERERASE</remarks>
            DeferErase = 0x2000,
            /// <summary>Draws a frame (defined in the window's class description) around the window.</summary>
            /// <remarks>SWP_DRAWFRAME</remarks>
            DrawFrame = 0x0020,
            /// <summary>Applies new frame styles set using the SetWindowLong function. Sends a WM_NCCALCSIZE message to
            /// the window, even if the window's size is not being changed. If this flag is not specified, WM_NCCALCSIZE
            /// is sent only when the window's size is being changed.</summary>
            /// <remarks>SWP_FRAMECHANGED</remarks>
            FrameChanged = 0x0020,
            /// <summary>Hides the window.</summary>
            /// <remarks>SWP_HIDEWINDOW</remarks>
            HideWindow = 0x0080,
            /// <summary>Does not activate the window. If this flag is not set, the window is activated and moved to the
            /// top of either the topmost or non-topmost group (depending on the setting of the hWndInsertAfter 
            /// parameter).</summary>
            /// <remarks>SWP_NOACTIVATE</remarks>
            DoNotActivate = 0x0010,
            /// <summary>Discards the entire contents of the client area. If this flag is not specified, the valid 
            /// contents of the client area are saved and copied back into the client area after the window is sized or 
            /// repositioned.</summary>
            /// <remarks>SWP_NOCOPYBITS</remarks>
            DoNotCopyBits = 0x0100,
            /// <summary>Retains the current position (ignores X and Y parameters).</summary>
            /// <remarks>SWP_NOMOVE</remarks>
            IgnoreMove = 0x0002,
            /// <summary>Does not change the owner window's position in the Z order.</summary>
            /// <remarks>SWP_NOOWNERZORDER</remarks>
            DoNotChangeOwnerZOrder = 0x0200,
            /// <summary>Does not redraw changes. If this flag is set, no repainting of any kind occurs. This applies to
            /// the client area, the nonclient area (including the title bar and scroll bars), and any part of the parent 
            /// window uncovered as a result of the window being moved. When this flag is set, the application must 
            /// explicitly invalidate or redraw any parts of the window and parent window that need redrawing.</summary>
            /// <remarks>SWP_NOREDRAW</remarks>
            DoNotRedraw = 0x0008,
            /// <summary>Same as the SWP_NOOWNERZORDER flag.</summary>
            /// <remarks>SWP_NOREPOSITION</remarks>
            DoNotReposition = 0x0200,
            /// <summary>Prevents the window from receiving the WM_WINDOWPOSCHANGING message.</summary>
            /// <remarks>SWP_NOSENDCHANGING</remarks>
            DoNotSendChangingEvent = 0x0400,
            /// <summary>Retains the current size (ignores the cx and cy parameters).</summary>
            /// <remarks>SWP_NOSIZE</remarks>
            IgnoreResize = 0x0001,
            /// <summary>Retains the current Z order (ignores the hWndInsertAfter parameter).</summary>
            /// <remarks>SWP_NOZORDER</remarks>
            IgnoreZOrder = 0x0004,
            /// <summary>Displays the window.</summary>
            /// <remarks>SWP_SHOWWINDOW</remarks>
            ShowWindow = 0x0040,
        }

        [DllImport("User32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterRawInputDevices([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]RAWINPUTDEVICE[] pRawInputDevices, int uiNumDevices, int cbSize);

        [DllImport("User32.dll", SetLastError = true)]
        private static extern int GetRawInputData(IntPtr hRawInput, DataCommand command, [Out] out InputData buffer, [In, Out] ref int size, int cbSizeHeader);

        //private static extern int GetRawInputData(IntPtr hRawInput, DataCommand command, [Out] out RawInput buffer, [In, Out] ref int size, int cbSizeHeader);

        [DllImport("user32.dll")]
        public static extern int GetRawInputData(IntPtr hRawInput, DataCommand uiCommand, [Out] out IntPtr pData,[In, Out] ref int pcbSize, int cbSizeHeader);


        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }

            public static implicit operator System.Drawing.Point(POINT p)
            {
                return new System.Drawing.Point(p.X, p.Y);
            }

            public static implicit operator POINT(System.Drawing.Point p)
            {
                return new POINT(p.X, p.Y);
            }
        }

        public enum DataCommand : uint
        {
            RID_HEADER = 0x10000005, // Get the header information from the RAWINPUT structure.
            RID_INPUT = 0x10000003   // Get the raw data from the RAWINPUT structure.
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct InputData
        {
            public Rawinputheader header;           // 64 bit header size: 24  32 bit the header size: 16
            public RawData data;                    // Creating the rest in a struct allows the header size to align correctly for 32/64 bit
        }

        /// <summary>
        /// Contains the raw input from a device. 
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct RawInput
        {
            /// <summary>
            /// Header for the data.
            /// </summary>
            public Rawinputheader Header;
            public Union Data;
            [StructLayout(LayoutKind.Explicit)]
            public struct Union
            {
                /// <summary>
                /// Mouse raw input data.
                /// </summary>
                [FieldOffset(0)]
                public Rawmouse Mouse;
                /// <summary>
                /// Keyboard raw input data.
                /// </summary>
                [FieldOffset(0)]
                public Rawkeyboard Keyboard;
                /// <summary>
                /// HID raw input data.
                /// </summary>
                [FieldOffset(0)]
                public Rawhid HID;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Rawinputheader
        {
            public uint dwType;                     // Type of raw input (RIM_TYPEHID 2, RIM_TYPEKEYBOARD 1, RIM_TYPEMOUSE 0)
            public uint dwSize;                     // Size in bytes of the entire input packet of data. This includes RAWINPUT plus possible extra input reports in the RAWHID variable length array. 
            public IntPtr hDevice;                  // A handle to the device generating the raw input data. 
            public IntPtr wParam;                   // RIM_INPUT 0 if input occurred while application was in the foreground else RIM_INPUTSINK 1 if it was not.
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct RawData
        {
            [FieldOffset(0)]
            public Rawmouse mouse;
            [FieldOffset(0)]
            public Rawkeyboard keyboard;
            [FieldOffset(0)]
            public Rawhid hid;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Rawhid
        {
            public uint dwSizHid;
            public uint dwCount;
            public byte bRawData;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct Rawmouse
        {
            [FieldOffset(0)]
            public ushort usFlags;
            [FieldOffset(4)]
            public uint ulButtons;
            [FieldOffset(4)]
            public ushort usButtonFlags;
            [FieldOffset(6)]
            public ushort usButtonData;
            [FieldOffset(8)]
            public uint ulRawButtons;
            [FieldOffset(12)]
            public int lLastX;
            [FieldOffset(16)]
            public int lLastY;
            [FieldOffset(20)]
            public uint ulExtraInformation;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Rawkeyboard
        {
            public ushort Makecode;                 // Scan code from the key depression
            public ushort Flags;                    // One or more of RI_KEY_MAKE, RI_KEY_BREAK, RI_KEY_E0, RI_KEY_E1
            private readonly ushort Reserved;       // Always 0    
            public ushort VKey;                     // Virtual Key Code
            public uint Message;                    // Corresponding Windows message for exmaple (WM_KEYDOWN, WM_SYASKEYDOWN etc)
            public uint ExtraInformation;           // The device-specific addition information for the event (seems to always be zero for keyboards)
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICE
        {
            public HIDUsagePage UsagePage;
            public HIDUsage Usage;
            public RawInputDeviceFlags Flags;
            public IntPtr WindowHandle;
        }

        private enum HIDUsagePage : ushort
        {
            /// <summary>Unknown usage page.</summary>
            Undefined = 0x00,
            /// <summary>Generic desktop controls.</summary>
            Generic = 0x01,
            /// <summary>Simulation controls.</summary>
            Simulation = 0x02,
            /// <summary>Virtual reality controls.</summary>
            VR = 0x03,
            /// <summary>Sports controls.</summary>
            Sport = 0x04,
            /// <summary>Games controls.</summary>
            Game = 0x05,
            /// <summary>Keyboard controls.</summary>
            Keyboard = 0x07,
            /// <summary>LED controls.</summary>
            LED = 0x08,
            /// <summary>Button.</summary>
            Button = 0x09,
            /// <summary>Ordinal.</summary>
            Ordinal = 0x0A,
            /// <summary>Telephony.</summary>
            Telephony = 0x0B,
            /// <summary>Consumer.</summary>
            Consumer = 0x0C,
            /// <summary>Digitizer.</summary>
            Digitizer = 0x0D,
            /// <summary>Physical interface device.</summary>
            PID = 0x0F,
            /// <summary>Unicode.</summary>
            Unicode = 0x10,
            /// <summary>Alphanumeric display.</summary>
            AlphaNumeric = 0x14,
            /// <summary>Medical instruments.</summary>
            Medical = 0x40,
            /// <summary>Monitor page 0.</summary>
            MonitorPage0 = 0x80,
            /// <summary>Monitor page 1.</summary>
            MonitorPage1 = 0x81,
            /// <summary>Monitor page 2.</summary>
            MonitorPage2 = 0x82,
            /// <summary>Monitor page 3.</summary>
            MonitorPage3 = 0x83,
            /// <summary>Power page 0.</summary>
            PowerPage0 = 0x84,
            /// <summary>Power page 1.</summary>
            PowerPage1 = 0x85,
            /// <summary>Power page 2.</summary>
            PowerPage2 = 0x86,
            /// <summary>Power page 3.</summary>
            PowerPage3 = 0x87,
            /// <summary>Bar code scanner.</summary>
            BarCode = 0x8C,
            /// <summary>Scale page.</summary>
            Scale = 0x8D,
            /// <summary>Magnetic strip reading devices.</summary>
            MSR = 0x8E
        }

        private enum HIDUsage : ushort
        {
            /// <summary></summary>
            Pointer = 0x01,
            /// <summary></summary>
            Mouse = 0x02,
            /// <summary></summary>
            Joystick = 0x04,
            /// <summary></summary>
            Gamepad = 0x05,
            /// <summary></summary>
            Keyboard = 0x06,
            /// <summary></summary>
            Keypad = 0x07,
            /// <summary></summary>
            SystemControl = 0x80,
            /// <summary></summary>
            X = 0x30,
            /// <summary></summary>
            Y = 0x31,
            /// <summary></summary>
            Z = 0x32,
            /// <summary></summary>
            RelativeX = 0x33,
            /// <summary></summary>    
            RelativeY = 0x34,
            /// <summary></summary>
            RelativeZ = 0x35,
            /// <summary></summary>
            Slider = 0x36,
            /// <summary></summary>
            Dial = 0x37,
            /// <summary></summary>
            Wheel = 0x38,
            /// <summary></summary>
            HatSwitch = 0x39,
            /// <summary></summary>
            CountedBuffer = 0x3A,
            /// <summary></summary>
            ByteCount = 0x3B,
            /// <summary></summary>
            MotionWakeup = 0x3C,
            /// <summary></summary>
            VX = 0x40,
            /// <summary></summary>
            VY = 0x41,
            /// <summary></summary>
            VZ = 0x42,
            /// <summary></summary>
            VBRX = 0x43,
            /// <summary></summary>
            VBRY = 0x44,
            /// <summary></summary>
            VBRZ = 0x45,
            /// <summary></summary>
            VNO = 0x46,
            /// <summary></summary>
            SystemControlPower = 0x81,
            /// <summary></summary>
            SystemControlSleep = 0x82,
            /// <summary></summary>
            SystemControlWake = 0x83,
            /// <summary></summary>
            SystemControlContextMenu = 0x84,
            /// <summary></summary>
            SystemControlMainMenu = 0x85,
            /// <summary></summary>
            SystemControlApplicationMenu = 0x86,
            /// <summary></summary>
            SystemControlHelpMenu = 0x87,
            /// <summary></summary>
            SystemControlMenuExit = 0x88,
            /// <summary></summary>
            SystemControlMenuSelect = 0x89,
            /// <summary></summary>
            SystemControlMenuRight = 0x8A,
            /// <summary></summary>
            SystemControlMenuLeft = 0x8B,
            /// <summary></summary>
            SystemControlMenuUp = 0x8C,
            /// <summary></summary>
            SystemControlMenuDown = 0x8D,
            /// <summary></summary>
            KeyboardNoEvent = 0x00,
            /// <summary></summary>
            KeyboardRollover = 0x01,
            /// <summary></summary>
            KeyboardPostFail = 0x02,
            /// <summary></summary>
            KeyboardUndefined = 0x03,
            /// <summary></summary>
            KeyboardaA = 0x04,
            /// <summary></summary>
            KeyboardzZ = 0x1D,
            /// <summary></summary>
            Keyboard1 = 0x1E,
            /// <summary></summary>
            Keyboard0 = 0x27,
            /// <summary></summary>
            KeyboardLeftControl = 0xE0,
            /// <summary></summary>
            KeyboardLeftShift = 0xE1,
            /// <summary></summary>
            KeyboardLeftALT = 0xE2,
            /// <summary></summary>
            KeyboardLeftGUI = 0xE3,
            /// <summary></summary>
            KeyboardRightControl = 0xE4,
            /// <summary></summary>
            KeyboardRightShift = 0xE5,
            /// <summary></summary>
            KeyboardRightALT = 0xE6,
            /// <summary></summary>
            KeyboardRightGUI = 0xE7,
            /// <summary></summary>
            KeyboardScrollLock = 0x47,
            /// <summary></summary>
            KeyboardNumLock = 0x53,
            /// <summary></summary>
            KeyboardCapsLock = 0x39,
            /// <summary></summary>
            KeyboardF1 = 0x3A,
            /// <summary></summary>
            KeyboardF12 = 0x45,
            /// <summary></summary>
            KeyboardReturn = 0x28,
            /// <summary></summary>
            KeyboardEscape = 0x29,
            /// <summary></summary>
            KeyboardDelete = 0x2A,
            /// <summary></summary>
            KeyboardPrintScreen = 0x46,
            /// <summary></summary>
            LEDNumLock = 0x01,
            /// <summary></summary>
            LEDCapsLock = 0x02,
            /// <summary></summary>
            LEDScrollLock = 0x03,
            /// <summary></summary>
            LEDCompose = 0x04,
            /// <summary></summary>
            LEDKana = 0x05,
            /// <summary></summary>
            LEDPower = 0x06,
            /// <summary></summary>
            LEDShift = 0x07,
            /// <summary></summary>
            LEDDoNotDisturb = 0x08,
            /// <summary></summary>
            LEDMute = 0x09,
            /// <summary></summary>
            LEDToneEnable = 0x0A,
            /// <summary></summary>
            LEDHighCutFilter = 0x0B,
            /// <summary></summary>
            LEDLowCutFilter = 0x0C,
            /// <summary></summary>
            LEDEqualizerEnable = 0x0D,
            /// <summary></summary>
            LEDSoundFieldOn = 0x0E,
            /// <summary></summary>
            LEDSurroundFieldOn = 0x0F,
            /// <summary></summary>
            LEDRepeat = 0x10,
            /// <summary></summary>
            LEDStereo = 0x11,
            /// <summary></summary>
            LEDSamplingRateDirect = 0x12,
            /// <summary></summary>
            LEDSpinning = 0x13,
            /// <summary></summary>
            LEDCAV = 0x14,
            /// <summary></summary>
            LEDCLV = 0x15,
            /// <summary></summary>
            LEDRecordingFormatDet = 0x16,
            /// <summary></summary>
            LEDOffHook = 0x17,
            /// <summary></summary>
            LEDRing = 0x18,
            /// <summary></summary>
            LEDMessageWaiting = 0x19,
            /// <summary></summary>
            LEDDataMode = 0x1A,
            /// <summary></summary>
            LEDBatteryOperation = 0x1B,
            /// <summary></summary>
            LEDBatteryOK = 0x1C,
            /// <summary></summary>
            LEDBatteryLow = 0x1D,
            /// <summary></summary>
            LEDSpeaker = 0x1E,
            /// <summary></summary>
            LEDHeadset = 0x1F,
            /// <summary></summary>
            LEDHold = 0x20,
            /// <summary></summary>
            LEDMicrophone = 0x21,
            /// <summary></summary>
            LEDCoverage = 0x22,
            /// <summary></summary>
            LEDNightMode = 0x23,
            /// <summary></summary>
            LEDSendCalls = 0x24,
            /// <summary></summary>
            LEDCallPickup = 0x25,
            /// <summary></summary>
            LEDConference = 0x26,
            /// <summary></summary>
            LEDStandBy = 0x27,
            /// <summary></summary>
            LEDCameraOn = 0x28,
            /// <summary></summary>
            LEDCameraOff = 0x29,
            /// <summary></summary>    
            LEDOnLine = 0x2A,
            /// <summary></summary>
            LEDOffLine = 0x2B,
            /// <summary></summary>
            LEDBusy = 0x2C,
            /// <summary></summary>
            LEDReady = 0x2D,
            /// <summary></summary>
            LEDPaperOut = 0x2E,
            /// <summary></summary>
            LEDPaperJam = 0x2F,
            /// <summary></summary>
            LEDRemote = 0x30,
            /// <summary></summary>
            LEDForward = 0x31,
            /// <summary></summary>
            LEDReverse = 0x32,
            /// <summary></summary>
            LEDStop = 0x33,
            /// <summary></summary>
            LEDRewind = 0x34,
            /// <summary></summary>
            LEDFastForward = 0x35,
            /// <summary></summary>
            LEDPlay = 0x36,
            /// <summary></summary>
            LEDPause = 0x37,
            /// <summary></summary>
            LEDRecord = 0x38,
            /// <summary></summary>
            LEDError = 0x39,
            /// <summary></summary>
            LEDSelectedIndicator = 0x3A,
            /// <summary></summary>
            LEDInUseIndicator = 0x3B,
            /// <summary></summary>
            LEDMultiModeIndicator = 0x3C,
            /// <summary></summary>
            LEDIndicatorOn = 0x3D,
            /// <summary></summary>
            LEDIndicatorFlash = 0x3E,
            /// <summary></summary>
            LEDIndicatorSlowBlink = 0x3F,
            /// <summary></summary>
            LEDIndicatorFastBlink = 0x40,
            /// <summary></summary>
            LEDIndicatorOff = 0x41,
            /// <summary></summary>
            LEDFlashOnTime = 0x42,
            /// <summary></summary>
            LEDSlowBlinkOnTime = 0x43,
            /// <summary></summary>
            LEDSlowBlinkOffTime = 0x44,
            /// <summary></summary>
            LEDFastBlinkOnTime = 0x45,
            /// <summary></summary>
            LEDFastBlinkOffTime = 0x46,
            /// <summary></summary>
            LEDIndicatorColor = 0x47,
            /// <summary></summary>
            LEDRed = 0x48,
            /// <summary></summary>
            LEDGreen = 0x49,
            /// <summary></summary>
            LEDAmber = 0x4A,
            /// <summary></summary>
            LEDGenericIndicator = 0x3B,
            /// <summary></summary>
            TelephonyPhone = 0x01,
            /// <summary></summary>
            TelephonyAnsweringMachine = 0x02,
            /// <summary></summary>
            TelephonyMessageControls = 0x03,
            /// <summary></summary>
            TelephonyHandset = 0x04,
            /// <summary></summary>
            TelephonyHeadset = 0x05,
            /// <summary></summary>
            TelephonyKeypad = 0x06,
            /// <summary></summary>
            TelephonyProgrammableButton = 0x07,
            /// <summary></summary>
            SimulationRudder = 0xBA,
            /// <summary></summary>
            SimulationThrottle = 0xBB
        }

        [Flags]
        private enum RawInputDeviceFlags
        {
            /// <summary>No flags.</summary>
            None = 0,
            /// <summary>If set, this removes the top level collection from the inclusion list. This tells the operating system to stop reading from a device which matches the top level collection.</summary>
            Remove = 0x00000001,
            /// <summary>If set, this specifies the top level collections to exclude when reading a complete usage page. This flag only affects a TLC whose usage page is already specified with PageOnly.</summary>
            Exclude = 0x00000010,
            /// <summary>If set, this specifies all devices whose top level collection is from the specified usUsagePage. Note that Usage must be zero. To exclude a particular top level collection, use Exclude.</summary>
            PageOnly = 0x00000020,
            /// <summary>If set, this prevents any devices specified by UsagePage or Usage from generating legacy messages. This is only for the mouse and keyboard.</summary>
            NoLegacy = 0x00000030,
            /// <summary>If set, this enables the caller to receive the input even when the caller is not in the foreground. Note that WindowHandle must be specified.</summary>
            InputSink = 0x00000100,
            /// <summary>If set, the mouse button click does not activate the other window.</summary>
            CaptureMouse = 0x00000200,
            /// <summary>If set, the application-defined keyboard device hotkeys are not handled. However, the system hotkeys; for example, ALT+TAB and CTRL+ALT+DEL, are still handled. By default, all keyboard hotkeys are handled. NoHotKeys can be specified even if NoLegacy is not specified and WindowHandle is NULL.</summary>
            NoHotKeys = 0x00000200,
            /// <summary>If set, application keys are handled.  NoLegacy must be specified.  Keyboard only.</summary>
            AppKeys = 0x00000400
        }

        #endregion
        #endregion

    }
}
