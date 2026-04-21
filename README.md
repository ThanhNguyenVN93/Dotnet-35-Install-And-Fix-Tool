# .NET Framework 3.5 Setup and Fix Tool

A lightweight Windows Forms utility to **check, install, and repair .NET Framework 3.5**
on Windows 7 SP1 through Windows 11, with automatic online/offline fallback and error auto-fixing.

---

## Features

| Feature | Description |
|---------|-------------|
| OS Detection | Identifies Windows version and CPU architecture (32/64-bit) |
| Status Check | Reads the Registry to verify .NET 3.5 installation state |
| Online Install | Activates .NET 3.5 via DISM — no separate download needed |
| Offline Fallback | Extracts the correct `.cab` from embedded resources when online install fails |
| Error Auto-Fix | Reads DISM error codes from the log and applies targeted fixes automatically |
| Win 7 RTM | Detects RTM builds, checks internet, routes to Form3 or bootstrap script |
| Admin Enforcement | Requests UAC elevation automatically on startup |

### Error Auto-Fix Reference

| Error Code | Cause | Action Taken |
|------------|-------|-------------|
| `0x800F081F` / `0x800F0906` | Missing source files | Offline install from embedded resources |
| `0x80070005` | Access denied | Reset permissions + clear Windows Update cache |
| `0x80070422` | WU service disabled | Enable `wuauserv` + `BITS` services |
| Other | System corruption | `sfc /scannow` + `DISM /RestoreHealth` |

---

## Requirements

| Item | Requirement |
|------|-------------|
| OS | Windows 7 SP1 (x86/x64) or later |
| Runtime | .NET Framework 4.7.2 |
| Privileges | Administrator |
| Internet | Optional — required for online install and KB download only |

---

## Embedded Resources

Three `.cab` files are compiled directly into the executable — no external files needed.

| Resource | Target OS |
|----------|-----------|
| `win7x86.cab` | Windows 7 32-bit |
| `win7x64.cab` | Windows 7 64-bit |
| `win1011netfx3.cab` | Windows 10 / 11 |

Extracted at runtime to: `%TEMP%\NetFx3_Install\`

---

## Usage

1. Run `frmnet3.5fix.exe` — UAC elevation is requested automatically.
2. The tool shows current OS info and .NET 3.5 status (Green = installed / Red = not installed).
3. Click **Active** to install:
   - Online DISM is attempted first.
   - On failure, offline install from embedded resources runs automatically.
4. If errors occur, click **Fix Errors**:
   - The tool reads the latest error code from the log.
   - Applies the appropriate fix automatically.
   - Click **Active** again to retry.

---

## Windows 7 RTM

Windows 7 RTM (Build 7600) requires **KB4474419** and **KB4490628** before .NET 3.5 can be installed.

### With Internet
The tool detects RTM automatically and opens **Form3** to download and install both KBs silently, then restarts the system.

### Without Internet
Use `win7rtm_bootstrap.bat`. Copy the following files into the same folder as the script and run it as Administrator:

```
KB4474419-x64.msu   (or KB4474419-x86.msu for 32-bit systems)
KB4490628.msu
```

The script installs both updates silently and schedules a system restart.
After reboot, run `frmnet3.5fix.exe` normally.

---

## Project Structure

```
frmnet3.5fix/
├── Program.cs              # Entry point, UAC check
├── Form1.cs                # Main form — install, fix, logging
├── Form3.cs                # KB download + silent install for Win 7 RTM
├── Properties/
│   └── Resources.resx      # Embedded .cab files
├── app.manifest            # requireAdministrator
├── win7rtm_bootstrap.bat   # Offline KB installer for Win 7 RTM (no internet)
├── LICENSE
└── README.md
```

---

## Exit Code Reference

| Code | Meaning |
|------|---------|
| `0` | Success |
| `3010` | Success — restart required |
| Other | Error — click Fix Errors to resolve |

---

## License

Released under the [MIT License](LICENSE).