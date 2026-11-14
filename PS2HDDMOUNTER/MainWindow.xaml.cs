using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management; // NuGet pakket installeren indien nodig
using System.Windows;
using Microsoft.Win32;

namespace PS2HDDMOUNTER
{
    public partial class MainWindow : Window
    {
        // Pas dit pad aan naar de plek waar jouw pfsshell.exe staat
        private string pfsShellPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pfsshell.exe");

        public MainWindow()
        {
            InitializeComponent();
            LoadPhysicalDrives();
        }

        // 1. Haal automatisch alle fysieke schijven op uit Windows
        private void LoadPhysicalDrives()
        {
            List<DriveInfoItem> driveList = new List<DriveInfoItem>();
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
                foreach (ManagementObject t in searcher.Get())
                {
                    string caption = t["Caption"]?.ToString() ?? "Onbekende schijf";
                    string deviceId = t["DeviceID"]?.ToString() ?? ""; // Geeft bijv. \\.\PHYSICALDRIVE2

                    // Extraheer alleen het nummer aan het einde
                    string driveNumber = deviceId.Substring(deviceId.LastIndexOf("DRIVE") + 5);

                    driveList.Add(new DriveInfoItem
                    {
                        Caption = $"{caption} (Drive #{driveNumber})",
                        Number = driveNumber
                    });
                }
                cmbDrives.ItemsSource = driveList;
                if (driveList.Count > 0) cmbDrives.SelectedIndex = 0;
                Log("Schijvenlijst vernieuwd.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fout bij ophalen schijven. Start je de app wel als Administrator?\n" + ex.Message);
            }
        }

        // 2. Kernfunctie om scripts/commando's veilig naar pfsshell te sturen
        private string ExecutePfsCommand(string driveNumber, string scriptContent)
        {
            if (!File.Exists(pfsShellPath))
            {
                return $"FOUT: pfsshell.exe is niet gevonden in de map:\n{pfsShellPath}\nPlaats pfsshell.exe in dezelfde map als deze app.";
            }

            // PFS shell werkt het beste via een tijdelijk scriptbestand om 'hanging' te voorkomen
            string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pfs_script.txt");

            // Bouw de commando-reeks op
            string fullScript = $"device hdd{driveNumber}:\n" + scriptContent + "\nexit\n";
            File.WriteAllText(scriptPath, fullScript);

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = pfsShellPath,
                Arguments = scriptPath, // Geef het scriptbestand mee als argument
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Verb = "runas" // Forceer admin rechten binnen het proces
            };

            using (Process process = Process.Start(startInfo))
            {
                // Wacht maximaal 5 seconden (5000 ms) in plaats van oneindig
                if (process.WaitForExit(5000))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    // Ruim het tijdelijke bestand op
                    if (File.Exists(scriptPath)) File.Delete(scriptPath);

                    if (!string.IsNullOrEmpty(error)) return $"Foutmelding:\n{error}\n\nOutput:\n{output}";
                    return output;
                }
                else
                {
                    // PFS Shell hangt! Sluit het proces geforceerd af
                    process.Kill();
                    if (File.Exists(scriptPath)) File.Delete(scriptPath);
                    return "FOUT: pfsshell.exe reageerde niet binnen 5 seconden en is geforceerd afgesloten.\n" +
                           "Controleer of je Visual Studio als ADMINISTRATOR hebt opgestart en of het schijfnummer klopt!";
                }
            }
        }

        // Knoppen Events
        private void BtnRefreshDrives_Click(object sender, RoutedEventArgs e) => LoadPhysicalDrives();

        private void BtnListFiles_Click(object sender, RoutedEventArgs e)
        {
            var selectedDrive = cmbDrives.SelectedItem as DriveInfoItem;
            if (selectedDrive == null) { MessageBox.Show("Selecteer eerst een schijf."); return; }

            string partition = txtPartition.Text.Trim();
            if (string.IsNullOrEmpty(partition)) { MessageBox.Show("Vul een partitie in."); return; }

            txtStatus.Text = $"Status: Bestanden ophalen van {partition}...";

            // Commando's: mount de partitie en voer 'ls' uit
            string script = $"mount {partition}\nls\nunmount";
            string result = ExecutePfsCommand(selectedDrive.Number, script);

            Log(result);
            txtStatus.Text = "Status: Bestanden succesvol opgehaald.";
        }

        private void BtnUploadFile_Click(object sender, RoutedEventArgs e)
        {
            var selectedDrive = cmbDrives.SelectedItem as DriveInfoItem;
            if (selectedDrive == null) { MessageBox.Show("Selecteer eerst een schijf."); return; }

            string partition = txtPartition.Text.Trim();
            if (string.IsNullOrEmpty(partition)) { MessageBox.Show("Vul een partitie in."); return; }

            // Open Windows Verkenner om een bestand te kiezen (bijv een .VCD of .ico)
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                string geselecteerdBestand = openFileDialog.FileName;
                string bestandsNaam = Path.GetFileName(geselecteerdBestand);

                // PFS shell vereist vaak dat het bestand lokaal in zijn eigen werkmap staat om 'put' te gebruiken
                string doelPadInAppMap = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, bestandsNaam);

                try
                {
                    txtStatus.Text = $"Status: Bestand kopiëren naar werkmap...";
                    File.Copy(geselecteerdBestand, doelPadInAppMap, true);

                    txtStatus.Text = $"Status: {bestandsNaam} aan het uploaden naar {partition} via PFS shell...";

                    // Commando's: mount, ga naar de root, zet bestand over, unmount
                    string script = $"mount {partition}\nput {bestandsNaam}\nunmount";
                    string result = ExecutePfsCommand(selectedDrive.Number, script);

                    Log(result);

                    // Verwijder het bestand uit de tijdelijke app-map na upload
                    if (File.Exists(doelPadInAppMap)) File.Delete(doelPadInAppMap);

                    txtStatus.Text = "Status: Upload voltooid!";
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Fout tijdens uploaden: " + ex.Message);
                    txtStatus.Text = "Status: Upload mislukt.";
                }
            }
        }

        private void Log(string message)
        {
            txtLog.AppendText($"[{DateTime.Now.ToLongTimeString()}] \n{message}\n\n");
            txtLog.ScrollToEnd();
        }
    }

    // Helper klasse voor de ComboBox
    public class DriveInfoItem
    {
        public string Caption { get; set; }
        public string Number { get; set; }
    }
}
