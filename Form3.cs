using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Diagnostics;

namespace frmnet3._5fix
{
    public partial class Form3 : Form
    {
        private bool installSuccess = false;
        private BackgroundWorker bwDownloadAndInstall;

        public Form3()
        {
            InitializeComponent();
            InitializeBackgroundWorker();
        }

        private void InitializeBackgroundWorker()
        {
            bwDownloadAndInstall = new BackgroundWorker();
            bwDownloadAndInstall.DoWork += BwDownloadAndInstall_DoWork;
            bwDownloadAndInstall.RunWorkerCompleted += BwDownloadAndInstall_RunWorkerCompleted;
            bwDownloadAndInstall.ProgressChanged += BwDownloadAndInstall_ProgressChanged;
            bwDownloadAndInstall.WorkerSupportsCancellation = true;
            bwDownloadAndInstall.WorkerReportsProgress = true;
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            // Bắt đầu quá trình tải và cài đặt
            bwDownloadAndInstall.RunWorkerAsync();
        }

        private void BwDownloadAndInstall_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                string tempPath = Path.Combine(Path.GetTempPath(), "KB_Updates");
                if (!Directory.Exists(tempPath))
                    Directory.CreateDirectory(tempPath);

                // Tải KB4474419
                bwDownloadAndInstall.ReportProgress(0, "Downloading KB4474419...");
                string kb1File = DownloadKB(tempPath, "KB4474419");
                if (string.IsNullOrEmpty(kb1File))
                {
                    e.Result = false;
                    return;
                }

                // Tải KB4490628
                bwDownloadAndInstall.ReportProgress(50, "Downloading KB4490628...");
                string kb2File = DownloadKB(tempPath, "KB4490628");
                if (string.IsNullOrEmpty(kb2File))
                {
                    e.Result = false;
                    return;
                }

                // Cài đặt KB4474419
                bwDownloadAndInstall.ReportProgress(75, "Installing KB4474419...");
                if (!InstallKB(kb1File))
                {
                    e.Result = false;
                    return;
                }

                // Cài đặt KB4490628
                bwDownloadAndInstall.ReportProgress(90, "Installing KB4490628...");
                if (!InstallKB(kb2File))
                {
                    e.Result = false;
                    return;
                }

                bwDownloadAndInstall.ReportProgress(100, "Installation completed successfully!");
                e.Result = true;
            }
            catch (Exception ex)
            {
                bwDownloadAndInstall.ReportProgress(0, $"Error: {ex.Message}");
                e.Result = false;
            }
        }

        private void BwDownloadAndInstall_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = Math.Min(e.ProgressPercentage, 100);
            lblStatus.Text = e.UserState?.ToString() ?? "Processing...";
        }

        private void BwDownloadAndInstall_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            installSuccess = (bool)e.Result;

            if (installSuccess)
            {
                MessageBox.Show("Windows 7 updates installed successfully!\n\nThe system will restart in 15 seconds to complete the installation.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // Restart hệ thống để nhận diện gói mới được cài
                RestartSystem();
            }
            else
            {
                MessageBox.Show("Failed to install Windows 7 updates.\n\nThis tool does not support your current configuration.", "Installation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        private string DownloadKB(string tempPath, string kbNumber)
        {
            try
            {
                // Xây dựng URL tải KB từ Microsoft
                // Lưu ý: Đây là ví dụ, URL thực tế có thể thay đổi
                string filename = $"{kbNumber}.msu";
                string filePath = Path.Combine(tempPath, filename);

                // Kiểm tra xem file đã tồn tại chưa
                if (File.Exists(filePath))
                {
                    return filePath;
                }

                // Tải từ Microsoft Update Catalog hoặc direct link
                string url = GetKBDownloadUrl(kbNumber);
                if (string.IsNullOrEmpty(url))
                {
                    return null;
                }

                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(url, filePath);
                }

                return filePath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error downloading {kbNumber}: {ex.Message}", "Download Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        private string GetKBDownloadUrl(string kbNumber)
        {
            // Trả về URL tải KB từ Microsoft
            // Bạn có thể thay đổi URL này theo nhu cầu
            switch (kbNumber)
            {
                case "KB4474419":
                    // Ví dụ URL (cần cập nhật với URL thực tế)
                    return "https://download.microsoft.com/download/0/5/5/055E7A3A-DA5E-4B6F-9CB3-1C7BB9A6A5B5/Windows6.1-KB4474419-x64.msu";
                case "KB4490628":
                    // Ví dụ URL (cần cập nhật với URL thực tế)
                    return "https://download.microsoft.com/download/5/C/5/5C53EE55-7E97-45C3-B3D6-A58E8A9B8B6B/Windows6.1-KB4490628-x64.msu";
                default:
                    return null;
            }
        }

        private bool InstallKB(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return false;
                }

                // Sử dụng wusa.exe để cài đặt
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "wusa.exe",
                    Arguments = $"\"{filePath}\" /quiet /norestart",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi))
                {
                    process.WaitForExit(300000); // Chờ tối đa 5 phút
                    return process.ExitCode == 0 || process.ExitCode == 3010; // 3010 = restart required
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error installing KB: {ex.Message}", "Installation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void Form3_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (bwDownloadAndInstall.IsBusy)
            {
                bwDownloadAndInstall.CancelAsync();
                e.Cancel = true;
            }
        }

        private void RestartSystem()
        {
            try
            {
                // Restart hệ thống sau 15 giây với thông báo
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "shutdown.exe",
                    Arguments = "/r /t 15 /c \"System is restarting to complete Windows updates installation.\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process.Start(psi);
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initiating system restart: {ex.Message}\n\nPlease restart your computer manually.", "System Restart Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Application.Exit();
            }
        }
    }
}
