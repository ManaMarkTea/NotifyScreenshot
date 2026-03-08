using Microsoft.Toolkit.Uwp.Notifications;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;

namespace NotifyScreenshot
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        //https://learn.microsoft.com/en-us/windows/apps/design/shell/tiles-and-notifications/send-local-toast?tabs=desktop

        protected override void OnStartup(StartupEventArgs args)
        {
            base.OnStartup(args);

            // Listen to notification activation
            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                // Obtain the arguments from the notification
                ToastArguments args = ToastArguments.Parse(toastArgs.Argument);

                // Obtain any user input (text boxes, menu selections) from the notification
                var userInput = toastArgs.UserInput;

                foreach(var arg in args)
                {
                    if (arg.Key == "imagePath")
                    {
                        Process.Start("explorer.exe", "/select,\"" + arg.Value + "\"");
                        break;
                    }
                }

                // Need to dispatch to UI thread if performing UI operations
                Application.Current.Dispatcher.Invoke(delegate
                {
                    Application.Current.Shutdown();
                });
            };

            if (ToastNotificationManagerCompat.WasCurrentProcessToastActivated())
            {
                return;
            }

            string imagePath = "";
            if (args.Args.Length > 0)
            {
                imagePath = args.Args[0];
            }


            var notification = new Microsoft.Toolkit.Uwp.Notifications.ToastContentBuilder()
           .AddArgument("action", "viewConversation")
           .AddArgument("conversationId", 9813)
           .AddArgument("imagePath", imagePath)
           .AddText("Screenshot")
           .AddText(imagePath)
            .AddInlineImage( new Uri("file:///" + imagePath));

            notification.Show();
            Application.Current.Shutdown();
        }

    }

}
