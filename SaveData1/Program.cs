using System;
using System.Threading;
using System.Windows.Forms;
using SaveData1.Helpers;

namespace SaveData1
{
    internal static class Program
    {
        /// <summary>Точка входа приложения.</summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += OnThreadException;
            AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
            TaskScheduler_UnobservedTaskException();

            AppLog.Info("Приложение запущено.");
            try
            {
                Application.Run(new LoginForm());
            }
            finally
            {
                AppLog.Info("Приложение завершено.");
            }
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            var ex = e.Exception;
            AppLog.Error("UI ThreadException", ex);
            try
            {
                MessageBox.Show(
                    "Произошла ошибка:\n" + ExceptionDisplay.MessageWithInners(ex) +
                    "\n\nПодробности записаны в журнал: " + (AppLog.LogDirectory ?? "(недоступно)"),
                    "Непредвиденная ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch
            {
                // не даём падать обработчику ошибок
            }
        }

        private static void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            AppLog.Error("AppDomain.UnhandledException (IsTerminating=" + e.IsTerminating + ")", ex);
            if (ex != null && !e.IsTerminating)
            {
                try
                {
                    MessageBox.Show(
                        "Фоновая ошибка:\n" + ExceptionDisplay.MessageWithInners(ex),
                        "Ошибка",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                catch
                {
                }
            }
        }

        private static void TaskScheduler_UnobservedTaskException()
        {
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                AppLog.Error("UnobservedTaskException", e.Exception);
                e.SetObserved();
            };
        }
    }
}
