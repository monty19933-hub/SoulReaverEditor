using System;
using System.IO;
using System.Windows.Forms;

namespace SoulReaverEditor
{
    internal static class Program
    {
        private static readonly string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SoulReaverEditor.log");

        [STAThread]
        private static void Main(string[] args)
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.ThreadException += delegate(object sender, System.Threading.ThreadExceptionEventArgs e)
                {
                    ShowAndLog(e.Exception);
                };
                AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
                {
                    ShowAndLog(e.ExceptionObject as Exception);
                };

                string startupPath = null;
                if (args != null && args.Length > 0 && File.Exists(args[0]))
                {
                    startupPath = args[0];
                }

                Application.Run(new MainForm(startupPath));
            }
            catch (Exception ex)
            {
                ShowAndLog(ex);
            }
        }

        private static void ShowAndLog(Exception ex)
        {
            if (ex == null) return;
            try
            {
                File.AppendAllText(LogPath,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine +
                    ex + Environment.NewLine + Environment.NewLine);
            }
            catch
            {
            }

            try
            {
                MessageBox.Show(ex.Message + Environment.NewLine + Environment.NewLine + "Log: " + LogPath,
                    "Soul Reaver Editor error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch
            {
            }
        }
    }
}
