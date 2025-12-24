# Screenshot Guide

This guide helps you capture professional screenshots for the README.

## Required Screenshots

### 1. Banner (`banner.png`)
**Recommended size**: 1200x400px

A hero image that shows:
- App logo/icon
- App name "Sofa Remote"
- Tagline: "Control your PC from your phone"
- Optional: Phone showing the interface next to PC

**Tools**: Canva, Figma, or Photoshop

---

### 2. QR Window (`qr-window.png`)
**Recommended size**: 600x800px

Capture the QR code window:
1. Run SofaRemote.exe
2. Click the tray icon to show QR window
3. Use Windows Snipping Tool (Win + Shift + S)
4. Capture the entire QR window
5. Save as `qr-window.png`

**Tips**:
- Ensure the QR code is clearly visible
- Include the URL text below the QR code
- Clean background (default is fine)

---

### 3. Phone Interface (`phone-interface.png`)
**Recommended size**: 400x800px (portrait)

Capture the PWA interface on your phone:

**On iOS**:
1. Open the app on your iPhone
2. Take screenshot (Volume Up + Power button)
3. Transfer to PC via AirDrop or iCloud

**On Android**:
1. Open the app on your Android phone
2. Take screenshot (Volume Down + Power button)
3. Transfer to PC via Google Photos or USB

**Tips**:
- Show the full interface with all buttons
- Connection indicator should be green (connected)
- Clean screenshot (no notification bar if possible)
- Consider using phone mockup generators for a polished look:
  - https://mockuphone.com
  - https://smartmockups.com

---

### 4. Connection Status (`connection-status.png`)
**Recommended size**: 400x600px

Capture the connection indicator states:

**Option A**: Animated GIF showing:
1. Disconnected state (red pulsing)
2. Connecting
3. Connected (green glowing)

**Option B**: Side-by-side comparison:
- Connected state on left
- Disconnected state on right

**Tools for GIF**:
- ScreenToGif (Windows)
- LICEcap
- GIPHY Capture

---

## Optional Screenshots

### 5. Installation Process
Show the installer wizard steps

### 6. System Tray Icon
Zoomed view of the tray icon in Windows

### 7. Feature Showcase
Multiple phone screenshots showing:
- Fullscreen controls
- Volume controls
- Play/pause in action

---

## Screenshot Standards

### Quality
- **Resolution**: High DPI (at least 2x for retina displays)
- **Format**: PNG for UI screenshots, JPG for photos
- **Size**: Optimize with TinyPNG.com before committing

### Style
- **Consistent**: Use same phone for all mobile screenshots
- **Clean**: Remove personal information, clean backgrounds
- **Lighting**: Good contrast, readable text
- **Annotations**: Use arrows/highlights if needed to point out features

### Tools

**Screen Capture**:
- Windows: Snipping Tool (Win + Shift + S)
- ShareX (advanced)
- Greenshot

**Editing**:
- Paint.NET
- GIMP
- Photoshop

**Phone Mockups**:
- https://mockuphone.com
- https://shots.so
- https://deviceframes.com

---

## Quick Capture Workflow

1. **Run the app**: `SofaRemote.exe`
2. **Open on phone**: Scan QR code
3. **Capture QR window**: Snipping Tool
4. **Capture phone screen**: Take screenshot
5. **Edit if needed**: Crop, resize, annotate
6. **Optimize**: Use TinyPNG.com
7. **Save to**: `.github/screenshots/`
8. **Commit and push**

---

## File Naming

Use descriptive, lowercase names with hyphens:
- ✅ `qr-window.png`
- ✅ `phone-interface.png`
- ✅ `connection-status.png`
- ❌ `Screenshot1.png`
- ❌ `IMG_1234.png`

---

## Testing

After adding screenshots, preview the README:
1. Push to GitHub
2. View README on GitHub to ensure images load
3. Check sizing on different screen sizes
4. Verify alt text is descriptive

---

## Example README Structure

```markdown
## 📸 Screenshots

<div align="center">

### QR Code Window
<img src=".github/screenshots/qr-window.png" alt="QR Code Window" width="400"/>

*Scan the QR code with your phone to connect instantly*

### Phone Interface
<img src=".github/screenshots/phone-interface.png" alt="Phone Interface" width="300"/>

*Modern, touch-optimized controls with Material Design*

</div>
```

---

**Tip**: You can capture screenshots now while testing the installer!
