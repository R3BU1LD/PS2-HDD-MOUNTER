<img width="1408" height="768" alt="image_e316373" src="https://github.com/user-attachments/assets/b8ab82db-0938-4c88-9eab-eef4361667d7" />

# PS2 HDD MOUNTER // Safe USB PFS Partition & File Explorer

> ⚠️ **ADVANCED / POWER USER UTILITY — USE WITH CAUTION**  
> **PS2 HDD MOUNTER** provides direct low-level read/write access to PlayStation 2 APA partitions and PFS filesystems. While built-in safety locks protect your internal Windows drives and core FreeHDBoot system partitions (`__mbr`, `__boot`, `__sysconf`, etc.), modifying partitions or deleting files on a console hard drive requires basic knowledge of the PS2 filesystem structure. **Always back up important saves and configurations before making changes. Use at your own risk.**


A modern, sci-fi themed C# WPF utility for managing partitions and transferring files on **PlayStation 2 internal hard drives (APA / PFS)** over USB. Built to pair seamlessly with **PS2 HDD CREATOR**, it features a **dual-layer safety lock** that protects both your internal PC drives and critical FreeHDBoot system partitions.

---

## Key Features

* **Strict USB-Only Hardware Lock:** Queries Windows WMI exclusively for USB-connected storage (`InterfaceType='USB'`). Internal SATA and NVMe PC drives are completely hidden to prevent accidental data loss.
* **APA Partition Scanner (`SCAN PARTITIONS`):** Reads and displays the full PS2 APA partition table (`__mbr`, `__sysconf`, `+OPL`, `__.POPS`, etc.) with exact megabyte allocations.
* **1-Click Partition Creation (`CREATE PARTITION`):** Automatically creates (`mkpart`) and PFS-formats (`mkfs`) new 2048 MB partitions such as `+OPL` or `__.POPS` with a single click.
* **Protected Partition Deletion (`DELETE PARTITION`):** Removes unwanted data partitions (`rmpart`) while actively blocking the deletion of critical PS2/FreeHDBoot system partitions (`__mbr`, `__net`, `__system`, `__sysconf`, `__common`, `__boot`, `__empty`).
* **Full PFS File Management (`LS`, `PUT`, `GET`):**
  * Browse directories and files inside any mounted PS2 partition.
  * Upload PS1 `.VCD` images (for POPStarter), OPL themes, `.ELF` homebrew, or icon files directly onto your PS2 drive.
  * Export configuration files or saves from your PS2 partitions back to your PC.
* **Non-Blocking Async Pipe Engine:** Communicates directly with `pfsshell.exe` via asynchronous console pipes using Windows UNC paths (`\\.\PHYSICALDRIVE#`) so large 700 MB+ game files transfer smoothly without freezing the UI.

---

## Project Structure

```text
PS2HDDMOUNTER/
├── MainWindow.xaml         # Borderless 2026 Sci-Fi HUD & custom dropdown UI
├── MainWindow.xaml.cs      # USB-only WMI scanner, partition safety lock & async pfsshell engine
├── app.manifest            # Automatic Administrator elevation configuration
└── pfsshell.exe            # Underlying PS2 PFS filesystem utility (placed in app root)
```

---

## Prerequisites & Setup

1. **Operating System:** Windows 10 or Windows 11 (64-bit).
2. **Runtime:** [.NET 8.0 Desktop Runtime](https://microsoft.com).
3. **Administrator Privileges:** Automatically requested via `app.manifest` so `pfsshell.exe` can access raw USB physical disk sectors (`\\.\PHYSICALDRIVE#`).
4. **Included Binary:** Ensure `pfsshell.exe` is located in the same folder as `PS2HDDMOUNTER.exe`.

---

## License

Released under the **GNU General Public License v3.0 (GPL-3.0)** — powered by `pfsshell`.
