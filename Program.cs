using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace frmnet3._5fix
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Kiểm tra xem ứng dụng đã chạy với quyền Admin chưa
            if (!IsRunAsAdmin())
            {
                // Nếu chưa, yêu cầu restart với quyền Admin
                RestartAsAdmin();
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        /// <summary>
        /// Kiểm tra xem ứng dụng có chạy với quyền Admin không
        /// </summary>
        private static bool IsRunAsAdmin()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Khởi động lại ứng dụng với quyền Admin
        /// </summary>
        private static void RestartAsAdmin()
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = Application.ExecutablePath,
                    UseShellExecute = true,
                    Verb = "runas"
                };

                Process.Start(processInfo);
                Application.Exit();
            }
            catch (System.ComponentModel.Win32Exception)
            {
                MessageBox.Show("Ứng dụng yêu cầu quyền Administrator để chạy.", "Lỗi Quyền", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Application.Exit();
            }
        }
    }
}
