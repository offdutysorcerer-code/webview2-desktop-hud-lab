using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace HudWebViewDemo;

public sealed class HudForm : Form
{
    private const int HotkeyId = 0xA15;
    private const int WmHotkey = 0x0312;
    private const int GwlExstyle = -20;
    private const int WsExTransparent = 0x20;
    private const int WsExNoActivate = 0x08000000;
    private static readonly IntPtr HwndTopmost = new(-1);
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowWindow = 0x0040;
    private const uint MouseeventfLeftdown = 0x0002;
    private const uint MouseeventfLeftup = 0x0004;

    private readonly WebView2 webView = new() { Dock = DockStyle.Fill };
    private readonly System.Windows.Forms.Timer cursorTimer = new() { Interval = 33 };
    private CursorOverlayForm? cursorOverlay;
    private bool clickThrough;
    private bool pageReady;
    private int topmostRefreshTick;

    public HudForm()
    {
        Text = "A15 WebView2 HUD";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ClientSize = new Size(380, 410);
        var area = Screen.PrimaryScreen!.WorkingArea;
        Location = new Point(area.Right - Width - 28, area.Top + 28);
        TopMost = true;
        ShowInTaskbar = false;
        BackColor = Color.FromArgb(5, 19, 30);
        Controls.Add(webView);
        Load += InitializeAsync;
        FormClosed += (_, _) =>
        {
            cursorTimer.Stop();
            cursorOverlay?.Close();
            UnregisterHotKey(Handle, HotkeyId);
        };
        cursorTimer.Tick += UpdateCursor;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotkey && m.WParam.ToInt32() == HotkeyId) ToggleClickThrough();
        base.WndProc(ref m);
    }

    private async void InitializeAsync(object? sender, EventArgs e)
    {
        RegisterHotKey(Handle, HotkeyId, 0, (uint)Keys.F8);
        cursorOverlay = new CursorOverlayForm();
        cursorOverlay.Show(this);

        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "A15-HudWebViewDemo",
            "WebView2");
        Directory.CreateDirectory(userDataFolder);
        TryDeleteLegacyWebViewData();
        var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
        await webView.EnsureCoreWebView2Async(environment);
        webView.DefaultBackgroundColor = Color.FromArgb(5, 19, 30);
        webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        webView.CoreWebView2.WebMessageReceived += OnWebMessage;
        webView.CoreWebView2.NavigationCompleted += (_, _) =>
        {
            pageReady = true;
            SendState();
            cursorTimer.Start();
        };
        webView.Source = new Uri(Path.Combine(AppContext.BaseDirectory, "wwwroot", "index.html"));
    }

    private void UpdateCursor(object? sender, EventArgs e)
    {
        if (++topmostRefreshTick >= 15)
        {
            topmostRefreshTick = 0;
            EnsureHudTopMost();
        }

        var screen = Cursor.Position;
        cursorOverlay?.SetCursorPosition(screen);
        if (pageReady)
        {
            webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new
            {
                type = "cursor",
                screenX = screen.X,
                screenY = screen.Y
            }));
        }
    }

    private void OnWebMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        using var doc = JsonDocument.Parse(e.WebMessageAsJson);
        var root = doc.RootElement;
        var action = root.TryGetProperty("action", out var value) ? value.GetString() : null;
        switch (action)
        {
            case "togglePassThrough": ToggleClickThrough(); break;
            case "move": MoveTo(root.GetProperty("target").GetString()); break;
            case "click":
                mouse_event(MouseeventfLeftdown, 0, 0, 0, UIntPtr.Zero);
                mouse_event(MouseeventfLeftup, 0, 0, 0, UIntPtr.Zero);
                Flash("已送出滑鼠左鍵點擊");
                break;
            case "close": Close(); break;
        }
    }

    private void MoveTo(string? target)
    {
        var area = SystemInformation.VirtualScreen;
        var point = target switch
        {
            "topLeft" => new Point(area.Left + 80, area.Top + 80),
            "center" => new Point(area.Left + area.Width / 2, area.Top + area.Height / 2),
            "bottomRight" => new Point(area.Right - 80, area.Bottom - 80),
            _ => Cursor.Position
        };
        SetCursorPos(point.X, point.Y);
        Flash($"游標移至 {point.X}, {point.Y}");
    }

    private void ToggleClickThrough()
    {
        clickThrough = !clickThrough;
        var exStyle = GetWindowLongPtr(Handle, GwlExstyle).ToInt64();
        exStyle = clickThrough
            ? exStyle | WsExTransparent | WsExNoActivate
            : exStyle & ~(WsExTransparent | WsExNoActivate);
        SetWindowLongPtr(Handle, GwlExstyle, new IntPtr(exStyle));
        SendState();
    }

    private void EnsureHudTopMost()
    {
        const uint flags = SwpNoMove | SwpNoSize | SwpNoActivate | SwpShowWindow;
        SetWindowPos(Handle, HwndTopmost, 0, 0, 0, 0, flags);
        if (cursorOverlay is not null && !cursorOverlay.IsDisposed)
            SetWindowPos(cursorOverlay.Handle, HwndTopmost, 0, 0, 0, 0, flags);
    }

    private void SendState()
    {
        if (pageReady)
            webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new { type = "state", clickThrough }));
    }

    private void Flash(string text)
    {
        if (pageReady)
            webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new { type = "flash", text }));
    }

    private static void TryDeleteLegacyWebViewData()
    {
        var legacyPath = Path.Combine(AppContext.BaseDirectory, "HudWebViewDemo.exe.WebView2");
        try
        {
            if (Directory.Exists(legacyPath))
                Directory.Delete(legacyPath, recursive: true);
        }
        catch
        {
            // 舊版仍在執行或快取被鎖定時，留待下次啟動再清理。
        }
    }

    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);
    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int index, IntPtr value);
}

internal sealed class CursorOverlayForm : Form
{
    private const int WsExTransparent = 0x20;
    private const int WsExToolWindow = 0x80;
    private const int WsExNoActivate = 0x08000000;
    private static readonly IntPtr HwndTopmost = new(-1);
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowWindow = 0x0040;
    private Point cursor;

    public CursorOverlayForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = SystemInformation.VirtualScreen;
        TopMost = true;
        ShowInTaskbar = false;
        BackColor = Color.Black;
        TransparencyKey = Color.Black;
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WsExTransparent | WsExToolWindow | WsExNoActivate;
            return cp;
        }
    }

    public void SetCursorPosition(Point screenPoint)
    {
        cursor = new Point(screenPoint.X - Bounds.Left, screenPoint.Y - Bounds.Top);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var outer = new Rectangle(cursor.X - 25, cursor.Y - 25, 50, 50);
        var inner = new Rectangle(cursor.X - 18, cursor.Y - 18, 36, 36);

        using var softGlow = new Pen(Color.FromArgb(65, 0, 190, 220), 5);
        using var cyan = new Pen(Color.FromArgb(245, 70, 238, 255), 2);
        using var ice = new Pen(Color.FromArgb(225, 205, 252, 255), 1);
        using var core = new SolidBrush(Color.FromArgb(250, 235, 255, 255));

        g.DrawEllipse(softGlow, inner);
        g.DrawEllipse(ice, inner);
        g.DrawArc(cyan, outer, 205, 65);
        g.DrawArc(cyan, outer, 295, 65);
        g.DrawArc(cyan, outer, 25, 65);
        g.DrawArc(cyan, outer, 115, 65);

        g.DrawLine(ice, cursor.X - 29, cursor.Y, cursor.X - 20, cursor.Y);
        g.DrawLine(ice, cursor.X + 20, cursor.Y, cursor.X + 29, cursor.Y);
        g.DrawLine(ice, cursor.X, cursor.Y - 29, cursor.X, cursor.Y - 20);
        g.DrawLine(ice, cursor.X, cursor.Y + 20, cursor.X, cursor.Y + 29);

        g.DrawLine(cyan, cursor.X - 10, cursor.Y, cursor.X - 4, cursor.Y);
        g.DrawLine(cyan, cursor.X + 4, cursor.Y, cursor.X + 10, cursor.Y);
        g.DrawLine(cyan, cursor.X, cursor.Y - 10, cursor.X, cursor.Y - 4);
        g.DrawLine(cyan, cursor.X, cursor.Y + 4, cursor.X, cursor.Y + 10);
        g.FillEllipse(core, cursor.X - 2, cursor.Y - 2, 4, 4);
    }
}


