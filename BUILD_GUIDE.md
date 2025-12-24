# Sofa Remote - Build Guide

## Quick Build (Recommended)

Run the automated build script to create everything at once:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

This creates:
1. **Regular build** → `.\publish\regular\`
2. **Single-file build** → `.\publish\single-file\`
3. **Professional installer** → `.\publish\installers\SofaRemote-Setup-2025.12.24.1.exe`

## Prerequisites

### Required
- **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Windows 10 SDK** (10.0.19041.0) - Included with Visual Studio or [standalone](https://developer.microsoft.com/windows/downloads/windows-sdk/)

### Optional (for creating installers)
- **Inno Setup 6** - [Download](https://jrsoftware.org/isinfo.php)
  - Only needed if you want to create the installer
  - Not needed to build the app itself
  - Install and it will auto-add to PATH

## Available Build Types

### 1. Regular Build (with DLLs)
**Location**: `.\publish\regular\`
- **Pros**:
  - Smaller executable size (~0.15 MB)
  - Faster startup time
  - Shared DLLs can be updated independently
- **Cons**:
  - Requires all DLL files to be distributed together (~241 files)
  - More files to manage
- **Use Case**: Development, or when you need smaller individual file sizes

### 2. Single-File Build
**Location**: `.\publish\single-file\`
- **Pros**:
  - One executable file (all-in-one, ~170 MB)
  - Easy to distribute
  - No dependency management
  - No .NET installation required
- **Cons**:
  - Larger file size
  - Slightly slower first-time startup (extraction)
- **Use Case**: Distribution, releases, easy deployment

### 3. Professional Installer (Recommended for Distribution)
**Location**: `.\publish\installers\`
- **File**: `SofaRemote-Setup-2025.12.24.1.exe`
- **What it does**:
  - Installs to `C:\Program Files\Sofa Remote\`
  - Adds to Start Menu
  - Creates Add/Remove Programs entry
  - Optional desktop shortcut
  - Optional auto-start with Windows
  - Prompts to configure firewall after installation
  - Clean uninstall support
- **Use Case**: Professional distribution, GitHub releases, end users

## Building

### Quick Build
Run the build script:
```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

This will create both versions in the `.\publish\` folder.

### Manual Build Commands

**Regular build**:
```powershell
dotnet publish -c Release -r win-x64 --self-contained true -o .\publish\regular
```

**Single-file build**:
```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\publish\single-file
```

## Distribution

### For GitHub Releases

Upload these files:

1. **SofaRemote-Setup-2025.12.24.1.exe** (~70-80 MB)
   - Professional installer
   - Recommended for most users
   - Includes everything needed

2. **SofaRemote-Portable.zip**
   - Create by zipping `.\publish\single-file\` folder
   - For users who prefer portable apps
   - No installation required

### What to Include in Each Distribution

**Installer** (created automatically by build.ps1):
- Single .exe file
- End users just download and run
- No other files needed

**Portable ZIP**:
```powershell
# Create portable distribution
Compress-Archive -Path .\publish\single-file\* -DestinationPath .\publish\SofaRemote-Portable.zip
```

Contains:
- SofaRemote.exe (from `.\publish\single-file\`)
- icons\ folder

**Regular Build** (optional, for advanced users):
```powershell
# Create regular build distribution
Compress-Archive -Path .\publish\regular\* -DestinationPath .\publish\SofaRemote-Regular.zip
```

Contains:
- SofaRemote.exe
- All .dll files
- icons\ folder

## End User Requirements

All builds (regular, single-file, and installer) are **self-contained**:

✅ **No .NET installation required** - Runtime included  
✅ **No dependencies** - Everything bundled  
✅ **Works offline** - No internet needed after download  
✅ **Portable** - Can run from any folder (except installer version)  

**System Requirements:**
- Windows 10/11 x64 (version 10.0.19041.0 or higher)
- ~200 MB disk space (installed)
- Administrator rights (one-time, for firewall configuration)

## Recommended for GitHub Releases

**Primary download** (for most users):
- `SofaRemote-Setup-2025.12.24.1.exe` - Professional installer

**Alternative download** (for advanced users):
- `SofaRemote-Portable.zip` - Portable version

Both are self-contained and don't require .NET to be installed on the user's system.
