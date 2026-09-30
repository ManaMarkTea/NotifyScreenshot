using Microsoft.Toolkit.Uwp.Notifications;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NotifyScreenshot;

internal static class Program
{
    private const int VirtualKeyAlt = 0x12;
    private const uint ClipboardFormatUnicodeText = 13;
    private const uint GlobalMemoryMoveable = 0x0002;
    private static readonly ManualResetEventSlim ToastActivationHandled = new(false);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr newOwner);

    [DllImport("user32.dll")]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll")]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint format, IntPtr data);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint flags, UIntPtr bytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr memory);

    [DllImport("kernel32.dll")]
    private static extern bool GlobalUnlock(IntPtr memory);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalFree(IntPtr memory);

    [STAThread]
    private static int Main(string[] args)
    {
        ToastNotificationManagerCompat.OnActivated += toastArgs =>
        {
            try
            {
                HandleToastActivation(toastArgs.Argument);
            }
            finally
            {
                ToastActivationHandled.Set();
            }
        };

        if (ToastNotificationManagerCompat.WasCurrentProcessToastActivated())
        {
            ToastActivationHandled.Wait(TimeSpan.FromSeconds(5));
            return 0;
        }

        if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
        {
            return 1;
        }

        ShowScreenshotNotification(Path.GetFullPath(args[0]));
        return 0;
    }

    private static void ShowScreenshotNotification(string imagePath)
    {
        new ToastContentBuilder()
            .AddArgument("imagePath", imagePath)
            .AddText("Screenshot")
            .AddText(imagePath)
            .AddInlineImage(new Uri(imagePath))
            .AddButton(new ToastButton()
                .SetContent("Edit")
                .AddArgument("action", "edit"))
            .AddButton(new ToastButton()
                .SetContent("Select")
                .AddArgument("action", "select"))
            .AddButton(new ToastButton()
                .SetContent("Copy path")
                .AddArgument("action", "copy"))
            .Show();
    }

    private static void HandleToastActivation(string argument)
    {
        ToastArguments args = ToastArguments.Parse(argument);
        string? imagePath = null;
        string? action = null;

        foreach (KeyValuePair<string, string> arg in args)
        {
            if (arg.Key == "imagePath")
            {
                imagePath = arg.Value;
            }
            else if (arg.Key == "action")
            {
                action = arg.Value;
            }
        }

        if (!string.IsNullOrWhiteSpace(imagePath))
        {
            OpenScreenshot(imagePath, action);
        }
    }

    private static void OpenScreenshot(string imagePath, string? action)
    {
        if (action == "copy")
        {
            CopyTextToClipboard(imagePath);
            return;
        }

        if (action == "edit" || (action is null && IsAltKeyDown()))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = imagePath,
                UseShellExecute = true
            });
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{imagePath}\"",
            UseShellExecute = true
        });
    }

    private static bool IsAltKeyDown() => (GetAsyncKeyState(VirtualKeyAlt) & 0x8000) != 0;

    private static void CopyTextToClipboard(string text)
    {
        // Another app may briefly hold the clipboard open, so retry a few times.
        for (int attempt = 0; !OpenClipboard(IntPtr.Zero); attempt++)
        {
            if (attempt >= 10)
            {
                return;
            }

            Thread.Sleep(50);
        }

        try
        {
            EmptyClipboard();

            int byteCount = (text.Length + 1) * sizeof(char);
            IntPtr memory = GlobalAlloc(GlobalMemoryMoveable, (UIntPtr)byteCount);
            if (memory == IntPtr.Zero)
            {
                return;
            }

            IntPtr target = GlobalLock(memory);
            if (target == IntPtr.Zero)
            {
                GlobalFree(memory);
                return;
            }

            Marshal.Copy(text.ToCharArray(), 0, target, text.Length);
            Marshal.WriteInt16(target, text.Length * sizeof(char), 0);
            GlobalUnlock(memory);

            // On success the system owns the memory, so it survives after this process exits.
            if (SetClipboardData(ClipboardFormatUnicodeText, memory) == IntPtr.Zero)
            {
                GlobalFree(memory);
            }
        }
        finally
        {
            CloseClipboard();
        }
    }
}
