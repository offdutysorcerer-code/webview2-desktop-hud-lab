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
    private PetOverlayForm? cursorOverlay;
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
        };
        cursorTimer.Tick += UpdateCursor;
    }

    private async void InitializeAsync(object? sender, EventArgs e)
    {
        cursorOverlay = new PetOverlayForm();
        cursorOverlay.Show(this);

        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "A15-HudWebViewDemo",
            "WebView2");
        Directory.CreateDirectory(userDataFolder);
        TryDeleteLegacyWebViewData();
        var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
        await cursorOverlay.InitializeAsync(environment);
        await webView.EnsureCoreWebView2Async(environment);
        webView.DefaultBackgroundColor = Color.FromArgb(5, 19, 30);
        webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "appassets.local",
            Path.Combine(AppContext.BaseDirectory, "wwwroot"),
            CoreWebView2HostResourceAccessKind.Allow);
        webView.CoreWebView2.WebMessageReceived += OnWebMessage;
        webView.CoreWebView2.NavigationCompleted += (_, _) =>
        {
            pageReady = true;
            cursorTimer.Start();
        };
        webView.Source = new Uri("https://appassets.local/index.html");
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
            case "setPet":
                cursorOverlay?.SetPetMode(root.GetProperty("mode").GetString() ?? "2d");
                break;
            case "close":
                Close();
                break;
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

    private void EnsureHudTopMost()
    {
        const uint flags = SwpNoMove | SwpNoSize | SwpNoActivate | SwpShowWindow;
        SetWindowPos(Handle, HwndTopmost, 0, 0, 0, 0, flags);
        if (cursorOverlay is not null && !cursorOverlay.IsDisposed)
            SetWindowPos(cursorOverlay.Handle, HwndTopmost, 0, 0, 0, 0, flags);
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

internal sealed class PetOverlayForm : Form
{
    private const int GwlExstyle = -20;
    private const int WsExTransparent = 0x20;
    private const int WsExToolWindow = 0x80;
    private const int WsExNoActivate = 0x08000000;

    private readonly WebView2 overlayView = new() { Dock = DockStyle.Fill };
    private bool pageReady;

    public PetOverlayForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = SystemInformation.VirtualScreen;
        TopMost = true;
        ShowInTaskbar = false;
        BackColor = Color.Black;
        TransparencyKey = Color.Black;
        Controls.Add(overlayView);
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

    public async Task InitializeAsync(CoreWebView2Environment environment)
    {
        await overlayView.EnsureCoreWebView2Async(environment);
        overlayView.DefaultBackgroundColor = Color.Transparent;
        overlayView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        overlayView.CoreWebView2.Settings.AreDevToolsEnabled = false;
        overlayView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "appassets.local",
            Path.Combine(AppContext.BaseDirectory, "wwwroot"),
            CoreWebView2HostResourceAccessKind.Allow);
        overlayView.CoreWebView2.NavigationCompleted += (_, _) => pageReady = true;

        var childStyle = GetWindowLongPtr(overlayView.Handle, GwlExstyle).ToInt64();
        SetWindowLongPtr(
            overlayView.Handle,
            GwlExstyle,
            new IntPtr(childStyle | WsExTransparent | WsExNoActivate));

        overlayView.Source = new Uri("https://appassets.local/pet-overlay.html");
    }

    public void SetCursorPosition(Point screenPoint)
    {
        if (!pageReady || overlayView.CoreWebView2 is null) return;

        overlayView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new
        {
            type = "cursor",
            x = screenPoint.X - Bounds.Left,
            y = screenPoint.Y - Bounds.Top
        }));
    }

    public void SetPetMode(string mode)
    {
        if (!pageReady || overlayView.CoreWebView2 is null) return;
        overlayView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new
        {
            type = "mode",
            mode = mode == "3d" ? "3d" : "2d"
        }));
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int index, IntPtr value);
}



