
<img width="1408" height="768" alt="image_e316373" src="https://github.com/user-attachments/assets/b8ab82db-0938-4c88-9eab-eef4361667d7" />

> ⚠️ **ADVANCED / POWER USER UTILITY — USE WITH CAUTION**  
> **PS2 HDD MOUNTER** provides direct low-level read/write access to PlayStation 2 APA partitions and PFS filesystems. While built-in safety locks protect your internal Windows drives and core FreeHDBoot system partitions (`__mbr`, `__boot`, `__sysconf`, etc.), modifying partitions or deleting files on a console hard drive requires basic knowledge of the PS2 filesystem structure. **Always back up important saves and configurations before making changes. Use at your own risk.**

---
> ⚠️**Never delete or overwrite core bootloader files inside __boot or __system, as this will prevent FreeHDBoot from booting your console.**
---


<img width="1610" height="77" alt="PS2 HDD MOUNTER Safe USB PFS Partition and File Explorer" src="https://github.com/user-attachments/assets/d1abd3c2-51cf-429b-815e-f13a3eaae3d9" />


A modern, sci-fi themed C# WPF utility for managing partitions and transferring files on **PlayStation 2 internal hard drives (APA / PFS)** over USB. Built to pair seamlessly with **PS2 HDD CREATOR**, it features a **dual-layer safety lock** that protects both your internal PC drives and critical FreeHDBoot system partitions.

---

<img width="378" height="76" alt="Key Features" src="https://github.com/user-attachments/assets/ea91053e-aa79-4fd6-af7a-8658d76ccf45" />

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

<img width="512" height="76" alt="Project Structure2" src="https://github.com/user-attachments/assets/a8f738a7-617e-4d36-8a29-8a594acac9dd" />

```text
PS2HDDMOUNTER/
├── MainWindow.xaml         # Borderless 2026 Sci-Fi HUD & custom dropdown UI
├── MainWindow.xaml.cs      # USB-only WMI scanner, partition safety lock & async pfsshell engine
├── app.manifest            # Automatic Administrator elevation configuration
└── pfsshell.exe            # Underlying PS2 PFS filesystem utility (placed in app root)
```

---

<img width="600" height="81" alt="Prerequisites and Setup" src="https://github.com/user-attachments/assets/340f7c41-3534-4305-a6f2-7953b173476b" />

1. **Operating System:** Windows 10 or Windows 11 (64-bit).
2. **Runtime:** [.NET 8.0 Desktop Runtime](https://microsoft.com).
3. **Administrator Privileges:** Automatically requested via `app.manifest` so `pfsshell.exe` can access raw USB physical disk sectors (`\\.\PHYSICALDRIVE#`).
4. **Included Binary:** Ensure `pfsshell.exe` is located in the same folder as `PS2HDDMOUNTER.exe`.

---

<img width="231" height="76" alt="License" src="https://github.com/user-attachments/assets/ee2ce6ca-ad97-41ef-bd1a-9b9baf5f79b0" />

Released under the **GNU General Public License v3.0 (GPL-3.0)** — powered by `pfsshell`.
