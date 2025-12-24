# Creating the Installer

## Step 1: Install Inno Setup

1. Download Inno Setup from: **https://jrsoftware.org/isinfo.php**
2. Run the installer (innosetup-X.X.X.exe)
3. Complete the installation (it automatically adds to PATH)

## Step 2: Build the Installer

Once Inno Setup is installed, run:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

This will:
1. Build the regular version (exe + DLLs)
2. Build the single-file version (all-in-one exe)
3. **Create the installer** using Inno Setup

The installer will be created at:
```
.\publish\installers\SofaRemote-Setup-2025.12.24.1.exe
```

## What the Installer Does

When users run the installer, it will:

✅ Install to `C:\Program Files\Sofa Remote\`  
✅ Add to Start Menu  
✅ Create uninstaller entry in Add/Remove Programs  
✅ **Optional**: Create desktop shortcut  
✅ **Optional**: Add to Windows Startup (auto-start)  
✅ Configure firewall rules (requires admin)  

## Manual Installer Creation

If you prefer to create the installer separately:

```powershell
iscc.exe SofaRemote.iss
```

## Customizing the Installer

Edit `SofaRemote.iss` to customize:
- App version
- Publisher name  
- GitHub URL
- Default installation options
- License file
- Icons

## Distribution

After the installer is created, distribute:
- **SofaRemote-Setup-2025.12.24.1.exe** (~70-80 MB)

Users can:
1. Download the installer
2. Run it (requires admin)
3. Choose options (desktop shortcut, auto-start)
4. Launch the app from Start Menu or Desktop

## Portable Version

If users prefer portable (no installation), you can also distribute:
- **SofaRemote-Portable.zip** containing the single-file exe + icons folder

They can run it from anywhere without installation.
