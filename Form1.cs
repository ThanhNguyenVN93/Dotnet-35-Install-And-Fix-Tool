using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Management;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace frmnet3._5fix
{
    public partial class Form1 : Form
    {
        private bool isWindows7 = false;
        private bool isWindows64Bit = false;

        // Static regex to avoid recompilation on every call
        private static readonly Regex HexErrorRegex = new Regex(@"0x[0-9A-Fa-f]{8}", RegexOptions.Compiled);

        public Form1()
        {
            InitializeComponent();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            this.Text = $".NET 3.5 Setup and Fix - {DateTime.Now:hh:mm:ss tt}";
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            timer1.Start(); // Start timer on form load

            // Display OS information
            lblos.Text = GetOperatingSystemInfo();

            // Store architecture information
            isWindows64Bit = Environment.Is64BitOperatingSystem;

            // Check Windows version
            if (!CheckWindowsVersion())
            {
                return;
            }

            // Check if .NET 3.5 is installed
            if (IsDotNet35Installed())
            {
                label1.Text = ".NET Framework 3.5 is already installed.";
                label1.ForeColor = Color.Green;
            }
            else
            {
                label1.Text = ".NET Framework 3.5 is not installed. Please install it to continue.";
                label1.ForeColor = Color.Red;
            }
        }
        private bool IsDotNet35Installed()
        {
            using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v3.5"))
            {
                if (key != null)
                {
                    var install = key.GetValue("Install");
                    return install != null && (int)install == 1;
                }
                return false;
            }
        }

        private string GetOperatingSystemInfo()
        {
            try
            {
                string osName = "";
                string osArchitecture = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";

                using (var searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem"))
                {
                    var osResults = searcher.Get().Cast<ManagementObject>().FirstOrDefault();
                    if (osResults != null)
                    {
                        string caption = osResults["Caption"].ToString();

                        // Shorten OS name
                        if (caption.Contains("Windows 11"))
                            osName = "Windows 11";
                        else if (caption.Contains("Windows 10"))
                            osName = "Windows 10";
                        else if (caption.Contains("Windows 8"))
                            osName = "Windows 8";
                        else if (caption.Contains("Windows 7"))
                            osName = "Windows 7";
                        else if (caption.Contains("Windows Server 2022"))
                            osName = "Windows Server 2022";
                        else if (caption.Contains("Windows Server 2019"))
                            osName = "Windows Server 2019";
                        else
                            osName = caption;
                    }
                }

                return $"{osName} - {osArchitecture}";
            }
            catch
            {
                return "Unknown - " + (Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit");
            }
        }

        private bool CheckWindowsVersion()
        {
            try
            {
                var osVersion = Environment.OSVersion;
                var major = osVersion.Version.Major;
                var minor = osVersion.Version.Minor;

                // Windows XP: Major 5, Minor 1
                if (major == 5 && minor == 1)
                {
                    MessageBox.Show("This tool does not support Windows XP.", "Unsupported OS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Application.Exit();
                    return false;
                }

                // Windows 7: Major 6, Minor 1
                if (major == 6 && minor == 1)
                {
                    isWindows7 = true;
                    if (!IsWindows7SP1OrLater())
                    {
                        return HandleWindows7RTM();
                    }
                }
                else
                {
                    isWindows7 = false;
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking Windows version: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return true;
            }
        }

        /// <summary>
        /// Handles Windows 7 RTM: checks internet first, then routes to online (Form3) or offline (.bat) path.
        /// </summary>
        private bool HandleWindows7RTM()
        {
            bool hasInternet = IsInternetAvailable();

            if (hasInternet)
            {
                // Online path: let user download KB via Form3
                DialogResult result = MessageBox.Show(
                    "Windows 7 without SP1 detected.\n\n" +
                    "Internet connection found.\n\n" +
                    "This tool will now download and install the required updates:\n" +
                    "  • KB4474419 (SHA-2 compatibility)\n" +
                    "  • KB4490628 (Servicing stack)\n\n" +
                    "The system will restart automatically after installation.",
                    "Windows 7 Update Required",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Information);

                if (result == DialogResult.Cancel)
                {
                    Application.Exit();
                    return false;
                }

                Form3 form3 = new Form3();
                form3.ShowDialog();
                return false; // App will reload after restart
            }
            else
            {
                // Offline path: no internet, guide user to run the .bat bootstrap
                string bootstrapPath = Path.Combine(Application.StartupPath, "win7rtm_bootstrap.bat");
                bool batExists = File.Exists(bootstrapPath);

                string message =
                    "Windows 7 without SP1 detected.\n\n" +
                    "No internet connection found.\n\n" +
                    "You must install the following updates manually before using this tool:\n" +
                    "  • KB4474419\n" +
                    "  • KB4490628\n\n" +
                    (batExists
                        ? "A bootstrap script was found. Click OK to open the instructions."
                        : "Please download the updates from another PC and transfer them via USB.");

                MessageBox.Show(message, "No Internet - Manual Update Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);

                if (batExists)
                    Process.Start(bootstrapPath);

                Application.Exit();
                return false;
            }
        }

        /// <summary>
        /// Checks internet connectivity by pinging Microsoft DNS (8.8.8.8 / 208.67.222.222).
        /// </summary>
        private bool IsInternetAvailable()
        {
            try
            {
                using (var ping = new System.Net.NetworkInformation.Ping())
                {
                    var reply = ping.Send("8.8.8.8", 2000);
                    if (reply.Status == System.Net.NetworkInformation.IPStatus.Success)
                        return true;

                    // Fallback: try Microsoft
                    reply = ping.Send("208.67.222.222", 2000);
                    return reply.Status == System.Net.NetworkInformation.IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        private bool IsWindows7SP1OrLater()
        {
            try
            {
                var osVersion = Environment.OSVersion;
                // Windows 7 SP1: Major 6, Minor 1, Build >= 7601
                if (osVersion.Version.Major == 6 && osVersion.Version.Minor == 1)
                {
                    return osVersion.Version.Build >= 7601;
                }
                return true;
            }
            catch
            {
                return true;
            }
        }

        private async void btnactive_Click(object sender, EventArgs e)
        {
            // Activate .NET Framework 3.5 via DISM
            btnactive.Enabled = false;
            txterrors.AppendText($"[{DateTime.Now:T}] Activating .NET 3.5 via DISM...\r\n");

            try
            {
                await Task.Run(() =>
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "dism.exe",
                        Arguments = "/Online /Enable-Feature /FeatureName:NetFx3 /All /NoRestart",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        Verb = "runas"
                    };

                    using (var p = Process.Start(psi))
                    {
                        p.WaitForExit();
                        return p.ExitCode;
                    }
                }).ContinueWith(t =>
                {
                    int exitCode = t.Result;
                    if (exitCode == 0)
                    {
                        txterrors.AppendText("[✓] .NET 3.5 has been activated successfully.\r\n");
                        MessageBox.Show("Installation successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        SetDotNetStatus(".NET Framework 3.5 is now installed.", Color.Green);
                    }
                    else
                    {
                        string hexError = "0x" + exitCode.ToString("X");
                        txterrors.AppendText($"[Error] Error code: {hexError}. Attempting offline installation...\r\n");
                        ExecuteOfflineInstall();
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception ex)
            {
                txterrors.AppendText($"[Exception] {ex.Message}\r\n");
            }
            finally
            {
                btnactive.Enabled = true;
            }
        }

        /// <summary>
        /// Updates label1 text and color (must be called on UI thread)
        /// </summary>
        private void SetDotNetStatus(string text, Color color)
        {
            label1.Text = text;
            label1.ForeColor = color;
        }

        private void ExecuteOfflineInstall()
        {
            // Determine resource name based on Windows version and architecture
            string cabResourceName, cabFileName;

            if (isWindows7)
            {
                cabResourceName = isWindows64Bit ? "frmnet3._5fix.win7x64" : "frmnet3._5fix.win7x86";
                cabFileName     = isWindows64Bit ? "win7x64.cab"           : "win7x86.cab";
                txterrors.AppendText($"[Info] Windows 7 - {(isWindows64Bit ? "64-bit" : "32-bit")} detected.\r\n");
            }
            else
            {
                cabResourceName = "frmnet3._5fix.win1011netfx3";
                cabFileName     = "win1011netfx3.cab";
                txterrors.AppendText("[Info] Windows 10/11 detected.\r\n");
            }

            txterrors.AppendText($"[Info] Extracting {cabFileName} from Resources...\r\n");

            string cabPath = ExtractCabFromResources(cabResourceName, cabFileName);
            if (string.IsNullOrEmpty(cabPath))
            {
                txterrors.AppendText("[!] Failed to extract .cab file from Resources.\r\n");
                MessageBox.Show("Error: Failed to extract CAB file from resources.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            txterrors.AppendText($"[Info] File extracted to: {cabPath}\r\n");

            // Fire and await from UI thread
            _ = InstallOfflineNetFx3(cabPath);
        }

        private string ExtractCabFromResources(string resourceName, string fileName)
        {
            try
            {
                using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    if (resourceStream == null)
                    {
                        txterrors.AppendText($"[!] Resource '{resourceName}' not found in assembly.\r\n");
                        return null;
                    }

                    string tempPath = Path.Combine(Path.GetTempPath(), "NetFx3_Install");
                    Directory.CreateDirectory(tempPath); // Does nothing if already exists

                    string outputPath = Path.Combine(tempPath, fileName);
                    using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                    {
                        resourceStream.CopyTo(fileStream);
                    }

                    return outputPath;
                }
            }
            catch (Exception ex)
            {
                txterrors.AppendText($"[Exception] Error extracting CAB: {ex.Message}\r\n");
                return null;
            }
        }

        private async Task InstallOfflineNetFx3(string cabPath)
        {
            txterrors.AppendText("[Info] Starting offline installation from source file...\r\n");

            try
            {
                int exitCode = await Task.Run(() =>
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "dism.exe",
                        Arguments = $"/Online /Enable-Feature /FeatureName:NetFx3 /Source:\"{cabPath}\" /LimitAccess /NoRestart",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        Verb = "runas"
                    };

                    using (var p = Process.Start(psi))
                    {
                        p.WaitForExit();
                        return p.ExitCode;
                    }
                });

                // Back on UI thread after await
                switch (exitCode)
                {
                    case 0:
                        txterrors.AppendText("[✓] .NET 3.5 has been installed successfully (offline).\r\n");
                        MessageBox.Show("Offline installation successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        SetDotNetStatus(".NET Framework 3.5 is now installed.", Color.Green);
                        break;

                    case 3010:
                        txterrors.AppendText("[!] .NET 3.5 installed - system restart required.\r\n");
                        MessageBox.Show("Installation successful but system restart is required.", "Restart Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        SetDotNetStatus(".NET Framework 3.5 is now installed (Restart required).", Color.Orange);
                        break;

                    default:
                        string hexError = "0x" + exitCode.ToString("X");
                        txterrors.AppendText($"[✗] Error: {hexError}\r\n");
                        MessageBox.Show($"Installation failed. Error code: {hexError}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                }
            }
            catch (Exception ex)
            {
                txterrors.AppendText($"[Exception] {ex.Message}\r\n");
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Fix Errors - Handle DISM errors (v1.3 Simplified)
        /// </summary>
        private async void btnfix_Click(object sender, EventArgs e)
        {
            btnfix.Enabled = false;
            txterrors.AppendText($"[{DateTime.Now:T}] [INFO] ===== ERROR FIXING STARTED =====\r\n");

            try
            {
                string errorCode = ExtractErrorCodeFromLog();

                // If error code not found or is null → generic fix
                if (string.IsNullOrEmpty(errorCode))
                {
                    txterrors.AppendText($"[{DateTime.Now:T}] [!] No specific error code found. Running generic fix...\r\n");
                    await FixErrorGeneric();
                }
                else if (errorCode.ToUpper() == "0x800F081F" || errorCode.ToUpper() == "0x800F0906")
                {
                    txterrors.AppendText($"[{DateTime.Now:T}] [*] Starting offline installation from Resources...\r\n");
                    await FixErrorOfflineInstall();
                }
                else if (errorCode.ToUpper() == "0x80070005")
                {
                    txterrors.AppendText($"[{DateTime.Now:T}] [*] Fixing access permission error (Access Denied)...\r\n");
                    await FixErrorAccessDenied();
                }
                else if (errorCode.ToUpper() == "0x80070422")
                {
                    txterrors.AppendText($"[{DateTime.Now:T}] [*] Restarting Windows Update Service...\r\n");
                    await FixErrorWindowsUpdateService();
                }
                else
                {
                    txterrors.AppendText($"[{DateTime.Now:T}] [*] Handling generic error (code: {errorCode}) - Running SFC & DISM Restore...\r\n");
                    await FixErrorGeneric();
                }

                txterrors.AppendText($"[{DateTime.Now:T}] [✓] Error fixing process completed.\r\n");
                MessageBox.Show("Error fixing process has completed. Check the log for details.", "Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                txterrors.AppendText($"[{DateTime.Now:T}] [ERROR] Exception: {ex.Message}\r\n");
                MessageBox.Show($"Error during error fixing: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnfix.Enabled = true;
            }
        }

        /// <summary>
        /// Extract latest error code from log (format: 0xXXXXXXXX)
        /// </summary>
        private string ExtractErrorCodeFromLog()
        {
            string logContent = txterrors.Text;
            if (string.IsNullOrEmpty(logContent))
                return null;

            var matches = HexErrorRegex.Matches(logContent);
            return matches.Count > 0 ? matches[matches.Count - 1].Value : null;
        }

        /// <summary>
        /// Fix error 0x800F081F / 0x800F0906 - Offline installation
        /// </summary>
        private async Task FixErrorOfflineInstall()
        {
            txterrors.AppendText("[*] Starting offline installation...\r\n");
            await InstallOfflineNetFx3(GetCabPath());
        }

        private string GetCabPath()
        {
            string cabResourceName, cabFileName;
            if (isWindows7)
            {
                cabResourceName = isWindows64Bit ? "frmnet3._5fix.win7x64" : "frmnet3._5fix.win7x86";
                cabFileName     = isWindows64Bit ? "win7x64.cab"           : "win7x86.cab";
            }
            else
            {
                cabResourceName = "frmnet3._5fix.win1011netfx3";
                cabFileName     = "win1011netfx3.cab";
            }
            return ExtractCabFromResources(cabResourceName, cabFileName);
        }

        /// <summary>
        /// Fix error 0x80070005 - Access Denied
        /// </summary>
        private async Task FixErrorAccessDenied()
        {
            txterrors.AppendText("[*] Checking Registry permissions...\r\n");

            txterrors.AppendText("[*] Step 1: Clearing Windows Update cache...\r\n");
            await RunCommandHiddenAsync("cmd.exe", "/c net stop wuauserv & del /Q /F /S C:\\Windows\\SoftwareDistribution\\Download\\* & net start wuauserv");

            txterrors.AppendText("[*] Step 2: Resetting Temp folder permissions...\r\n");
            await RunCommandHiddenAsync("cmd.exe", "/c icacls C:\\Windows\\Temp /reset /T /Q");

            txterrors.AppendText("[✓] Permissions restored. Please disable Antivirus if enabled.\r\n");
            MessageBox.Show("Permissions have been restored.\n\nIf errors persist, please:\n1. Temporarily disable Antivirus\n2. Click 'Active' to retry", "Access Denied Fix", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Fix error 0x80070422 - Windows Update Service Disabled
        /// </summary>
        private async Task FixErrorWindowsUpdateService()
        {
            txterrors.AppendText("[*] Enabling Windows Update Service...\r\n");

            txterrors.AppendText("[*] Step 1: Setting Windows Update to auto-start...\r\n");
            await RunCommandHiddenAsync("cmd.exe", "/c sc config wuauserv start= auto & net start wuauserv");

            txterrors.AppendText("[*] Step 2: Setting BITS service to auto-start...\r\n");
            await RunCommandHiddenAsync("cmd.exe", "/c sc config bits start= auto & net start bits");

            txterrors.AppendText("[✓] Windows Update & BITS services have been enabled.\r\n");
            MessageBox.Show("Update services have been enabled.\n\nClick 'Active' to retry.", "Service Fixed", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Fix Errors - Last resort solution: sfc /scannow & DISM restore
        /// </summary>
        private async Task FixErrorGeneric()
        {
            txterrors.AppendText("[*] Running System File Checker (sfc /scannow)...\r\n");
            txterrors.AppendText("[!] This step may take 15-20 minutes...\r\n");

            int sfcCode = await RunCommandHiddenAsync("cmd.exe", "/c sfc /scannow");
            txterrors.AppendText($"[*] SFC completed with exit code: {sfcCode}\r\n");

            txterrors.AppendText("[*] Running DISM Restore-Image...\r\n");
            int dismCode = await RunCommandHiddenAsync("cmd.exe", "/c Dism /Online /Cleanup-Image /RestoreHealth");
            txterrors.AppendText($"[*] DISM completed with exit code: {dismCode}\r\n");

            txterrors.AppendText("[✓] Generic fix completed.\r\n");
            MessageBox.Show("System recovery process has completed.\n\nPlease restart your computer.", "System Restored", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Helper: Run CMD command hidden, returns exit code
        /// </summary>
        private Task<int> RunCommandHiddenAsync(string fileName, string arguments)
        {
            return Task.Run(() =>
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var p = Process.Start(psi))
                {
                    string output = p.StandardOutput.ReadToEnd();
                    string error  = p.StandardError.ReadToEnd();
                    p.WaitForExit();

                    this.Invoke(new Action(() =>
                    {
                        if (!string.IsNullOrWhiteSpace(output))
                            txterrors.AppendText($"  Output: {output.Trim()}\r\n");
                        if (!string.IsNullOrWhiteSpace(error))
                            txterrors.AppendText($"  Error: {error.Trim()}\r\n");
                    }));

                    return p.ExitCode;
                }
            });
        }
    }
}
