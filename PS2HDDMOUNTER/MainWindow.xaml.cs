using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace PS2HDDMOUNTER
{
    public partial class MainWindow : Window
    {
        private readonly string pfsShellPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pfsshell.exe");

        public MainWindow()
        {
            InitializeComponent();
            LoadPhysicalDrives();
        }

        // 1. Alleen USB-schijven ophalen (100% bescherming van interne Windows SATA/NVMe schijven)
        private void LoadPhysicalDrives()
        {
            List<DriveInfoItem> driveList = new List<DriveInfoItem>();
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive WHERE InterfaceType='USB'");
                foreach (ManagementObject t in searcher.Get())
                {
                    string caption = t["Caption"]?.ToString() ?? "Onbekende USB-schijf";
                    string deviceId = t["DeviceID"]?.ToString() ?? "";
                    long sizeBytes = Convert.ToInt64(t["Size"] ?? 0);
                    double sizeGB = Math.Round(sizeBytes / (1024.0 * 1024.0 * 1024.0), 1);

                    string driveNumber = deviceId.Substring(deviceId.LastIndexOf("DRIVE") + 5);

                    driveList.Add(new DriveInfoItem
                    {
                        Caption = $"[USB #{driveNumber}] {caption} ({sizeGB} GB)",
                        Number = driveNumber
                    });
                }

                cmbDrives.ItemsSource = driveList;
                if (driveList.Count > 0)
                {
                    cmbDrives.SelectedIndex = 0;
                    Log($"✔ {driveList.Count} USB-schijf/schijven gedetecteerd. Interne schijven zijn afgeschermd.");
                    txtStatus.Text = "[READY] USB PS2 HDD gedetecteerd.";
                }
                else
                {
                    Log("⚠ Geen USB-schijf gevonden. Koppel je PS2 schijf via een USB Hub/Dock aan en klik op 'SCAN USB'.");
                    txtStatus.Text = "[WARNING] Geen USB-schijf gevonden.";
                }
            }
            catch (Exception ex)
            {
                Log("✖ Fout bij ophalen schijven (Start als Administrator): " + ex.Message);
            }
        }

        private string GetTargetPartition()
        {
            string custom = txtCustomPartition.Text.Trim();
            if (!string.IsNullOrEmpty(custom)) return custom;

            if (cmbPartition.SelectedItem is ComboBoxItem item && item.Content != null)
            {
                return item.Content.ToString() ?? "+OPL";
            }
            return "+OPL";
        }

        // 2. Asynchrone pfsshell uitvoer zonder bevriezend scherm
        private Task<string> ExecutePfsCommandAsync(string driveNumber, string scriptContent, int timeoutMs = 60000)
        {
            return Task.Run(() =>
            {
                if (!File.Exists(pfsShellPath))
                {
                    return $"FOUT: pfsshell.exe is niet gevonden in:\n{pfsShellPath}\nPlaats pfsshell.exe in de programmamap.";
                }

                string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"pfs_script_{Guid.NewGuid():N}.txt");
                string fullScript = $"device hdd{driveNumber}:\n{scriptContent}\nexit\n";
                File.WriteAllText(scriptPath, fullScript);

                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = pfsShellPath,
                        Arguments = $"\"{scriptPath}\"",
                        WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using Process? process = Process.Start(startInfo);
                    if (process == null) return "FOUT: Kon pfsshell.exe niet starten.";

                    if (process.WaitForExit(timeoutMs))
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                        if (!string.IsNullOrEmpty(error)) return $"Melding:\n{error}\n\nOutput:\n{output}";
                        return output;
                    }
                    else
                    {
                        process.Kill();
                        return $"FOUT: Time-out na {timeoutMs / 1000} seconden. Controleer of je de app als Administrator draait.";
                    }
                }
                catch (Exception ex)
                {
                    return $"Systeemfout: {ex.Message}";
                }
                finally
                {
                    if (File.Exists(scriptPath)) File.Delete(scriptPath);
                }
            });
        }

        private void SetBusyState(bool isBusy, string statusMessage)
        {
            BtnListPartitions.IsEnabled = !isBusy;
            BtnListFiles.IsEnabled = !isBusy;
            BtnUploadFile.IsEnabled = !isBusy;
            BtnDownloadFile.IsEnabled = !isBusy;
            cmbDrives.IsEnabled = !isBusy;

            txtStatus.Text = statusMessage;
            prgProgress.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnRefreshDrives_Click(object sender, RoutedEventArgs e) => LoadPhysicalDrives();

        // Scan alle aanwezige APA-partities op de PS2 schijf
        private async void BtnListPartitions_Click(object sender, RoutedEventArgs e)
        {
            if (cmbDrives.SelectedItem is not DriveInfoItem selectedDrive)
            {
                MessageBox.Show("Selecteer eerst een USB PS2 HDD.", "Geen schijf", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SetBusyState(true, "[BUSY] APA-partitietabel uitlezen...");
            string result = await ExecutePfsCommandAsync(selectedDrive.Number, "ls");
            Log($"=== PARTITIES OP USB #{selectedDrive.Number} ===\n{result}");
            SetBusyState(false, "[OK] Partitielijst opgehaald.");
        }

        // Toon bestanden binnen een specifieke partitie
        private async void BtnListFiles_Click(object sender, RoutedEventArgs e)
        {
            if (cmbDrives.SelectedItem is not DriveInfoItem selectedDrive)
            {
                MessageBox.Show("Selecteer eerst een USB PS2 HDD.", "Geen schijf", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string partition = GetTargetPartition();
            SetBusyState(true, $"[BUSY] Bestanden ophalen uit '{partition}'...");

            string script = $"mount {partition}\nls\nunmount";
            string result = await ExecutePfsCommandAsync(selectedDrive.Number, script);

            Log($"=== BESTANDEN IN {partition} ===\n{result}");
            SetBusyState(false, $"[OK] Inhoud van '{partition}' opgehaald.");
        }

        // Upload een bestand (bijv. .VCD PS1 game, .ELF, .ICO of config) naar de partitie
        private async void BtnUploadFile_Click(object sender, RoutedEventArgs e)
        {
            if (cmbDrives.SelectedItem is not DriveInfoItem selectedDrive)
            {
                MessageBox.Show("Selecteer eerst een USB PS2 HDD.", "Geen schijf", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string partition = GetTargetPartition();
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = $"Kies een bestand om te uploaden naar {partition}"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string sourceFile = openFileDialog.FileName;
                string fileName = Path.GetFileName(sourceFile);
                string tempTarget = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

                SetBusyState(true, $"[BUSY] '{fileName}' voorbereiden en uploaden naar '{partition}'...");

                try
                {
                    await Task.Run(() => File.Copy(sourceFile, tempTarget, true));

                    // Timeout van 5 minuten (300.000 ms) zodat ook grote 700MB .VCD PS1-games rustig overgezet worden
                    string script = $"mount {partition}\nput \"{fileName}\"\nunmount";
                    string result = await ExecutePfsCommandAsync(selectedDrive.Number, script, 300000);

                    Log($"=== UPLOAD NAAR {partition}: {fileName} ===\n{result}");
                    SetBusyState(false, $"[OK] '{fileName}' succesvol geüpload naar '{partition}'!");
                }
                catch (Exception ex)
                {
                    Log($"✖ Upload fout: {ex.Message}");
                    SetBusyState(false, "[ERROR] Upload mislukt.");
                }
                finally
                {
                    if (File.Exists(tempTarget)) File.Delete(tempTarget);
                }
            }
        }

        // Download / Exporteer een bestand vanaf de PS2 partitie naar je PC
        private async void BtnDownloadFile_Click(object sender, RoutedEventArgs e)
        {
            if (cmbDrives.SelectedItem is not DriveInfoItem selectedDrive)
            {
                MessageBox.Show("Selecteer eerst een USB PS2 HDD.", "Geen schijf", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string partition = GetTargetPartition();
            string remoteFileName = Microsoft.VisualBasic.Interaction.InputBox($"Welk bestand uit partitie '{partition}' wil je kopiëren naar je PC?\n(Tip: Klik eerst op 'LIST FILES' om de exacte bestandsnaam te zien)","Bestand exporteren van PS2 HDD", "");
            if (string.IsNullOrWhiteSpace(remoteFileName)) return;
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                FileName = remoteFileName,
                Title = "Opslaan op PC"
            };
            if (saveDialog.ShowDialog() == true)
            {
                SetBusyState(true, $"[BUSY] '{remoteFileName}' downloaden van '{partition}'...");
                string tempDownloaded = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, remoteFileName);
                try
                {
                    string script = $"mount {partition}\nget \"{remoteFileName}\"\nunmount";
                    string result = await ExecutePfsCommandAsync(selectedDrive.Number, script, 300000);
                    if (File.Exists(tempDownloaded))
                    {
                        File.Copy(tempDownloaded, saveDialog.FileName, true);
                        File.Delete(tempDownloaded);
                        Log($"=== EXPORT VAN {partition}: {remoteFileName} ===\nOpgeslagen op: {saveDialog.FileName}\n{result}");
                        SetBusyState(false, $"[OK] '{remoteFileName}' opgeslagen op je PC!");
                    }
                    else
                    {
                        Log($"✖ Kon bestand '{remoteFileName}' niet vinden in {partition}.\n{result}");
                        SetBusyState(false, "[ERROR] Bestand niet gevonden op PS2 HDD.");
                    }
                }
                catch (Exception ex)
                {
                    Log($"✖ Export fout: {ex.Message}");
                    SetBusyState(false, "[ERROR] Export mislukt.");
                }
            }
        }
        private void BtnClearLog_Click(object sender, RoutedEventArgs e) => txtLog.Clear();
        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;
        private void BtnExit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }
        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n\n");
                txtLog.ScrollToEnd();
            });
        }
    }
    public class DriveInfoItem
    {
        public string Caption { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
    }
}