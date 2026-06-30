using Microsoft.Toolkit.Uwp.Notifications;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NotifyScreenshot;

internal static class Program
{
    private const int VirtualKeyAlt = 0x12;
    private static readonly ManualResetEventSlim ToastActivationHandled = new(false);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int virtualKey);

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
            .Show();
    }

    private static void HandleToastActivation(string argument)
    {
        ToastArguments args = ToastArguments.Parse(argument);

        foreach (KeyValuePair<string, string> arg in args)
        {
            if (arg.Key != "imagePath")
            {
                continue;
            }

            OpenScreenshot(arg.Value);

            break;
        }
    }

    private static void OpenScreenshot(string imagePath)
    {
        if (IsAltKeyDown())
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
}
