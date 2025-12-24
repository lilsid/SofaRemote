# Contributing to Sofa Remote

Thank you for your interest in contributing to Sofa Remote! 🎉

## 🚀 Getting Started

1. **Fork** the repository
2. **Clone** your fork locally
3. **Create** a new branch for your feature/fix
4. **Make** your changes
5. **Test** thoroughly
6. **Submit** a pull request

## 🏗️ Development Setup

### Prerequisites
- .NET 8 SDK ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- Windows 10 SDK (10.0.19041.0 or higher)
- Any code editor (VS Code, Visual Studio, Rider)

### Build & Run
```powershell
# Clone the repo
git clone https://github.com/yourusername/SofaRemote.git
cd SofaRemote

# Build
dotnet build

# Run
dotnet run

# Run in release mode
dotnet run -c Release
```

### Testing
Test on multiple scenarios:
- ✅ Windows 10 and Windows 11
- ✅ Different browsers (Edge, Chrome, Firefox, Brave)
- ✅ Multiple devices (iPhone, Android, desktop)
- ✅ Different network configurations (Wi-Fi, Ethernet)
- ✅ Fullscreen video playback (YouTube, Netflix, etc.)

## 📐 Architecture Principles

### Single-File Design
Sofa Remote maintains a **single-file architecture** (`Program.cs`). This is intentional:
- ✅ Easy deployment (one exe + icons folder)
- ✅ Simple distribution
- ✅ No complex project structure
- ✅ Self-contained and portable

When contributing, keep all C# code in `Program.cs` unless there's a compelling reason to split.

### Embedded HTML/CSS/JS
The web interface is embedded as a C# string constant:
- **IndexHtml** - Main PWA interface
- **Manifest** - PWA manifest (generated)
- **Service Worker** - Offline caching (generated)

This keeps everything self-contained without external files.

## 🎨 Code Style

### C# Guidelines
- Use **meaningful variable names**
- Keep **methods focused** and single-purpose
- Add **error handling** with try-catch where appropriate
- Use **async/await** for I/O operations
- Follow **Microsoft C# conventions**

### JavaScript Guidelines
- Use **modern ES6+ syntax** where supported
- Keep code **vanilla** (no framework dependencies)
- Maintain **small bundle size**
- Optimize for **mobile performance**

### CSS Guidelines
- Use **CSS custom properties** for theming
- Keep styles **inline** in the HTML string
- Optimize for **touch interactions**
- Support **dark mode**

## 🔧 Common Changes

### Adding a New Control

1. **Add button to HTML**:
```csharp
<button id='newcontrol' class='tile'>
  <div class='icon'>🎵</div>
  <div class='label'>New Control</div>
</button>
```

2. **Add JavaScript handler**:
```javascript
document.getElementById('newcontrol').onclick=()=>doPost('/newcontrol');
```

3. **Add endpoint in HandleRequestAsync**:
```csharp
case "/newcontrol": SendNewControl(); res.StatusCode = 200; break;
```

4. **Implement the method**:
```csharp
private static void SendNewControl()
{
    keybd_event(VK_YOUR_KEY, 0, 0, UIntPtr.Zero);
}
```

### Modifying UI Styles
Edit the CSS section in the `IndexHtml` string constant around line 50-95.

### Changing Network Port
Update `8080` references throughout `Program.cs`. Search for `:8080` to find all occurrences.

## 🐛 Bug Fixes

### Reporting Bugs
When reporting bugs, include:
- **OS version** (Windows 10/11 build number)
- **Browser** and version (on phone/desktop)
- **Steps to reproduce**
- **Expected vs actual behavior**
- **Log file** content (`%LOCALAPPDATA%\SofaRemote\sofa_remote.log`)

### Fixing Bugs
1. **Reproduce** the issue locally
2. **Check logs** for error messages
3. **Add logging** if needed for debugging
4. **Test fix** on affected scenarios
5. **Document** the fix in PR description

## 📱 PWA Development

### Testing PWA Features
- **iOS Safari**: Test Add to Home Screen, standalone mode
- **Android Chrome**: Test install prompt, native install
- **Service Worker**: Test offline functionality
- **Manifest**: Verify icon sizes and theme colors

### PWA Checklist
- ✅ Manifest includes all required fields
- ✅ Icons are properly sized (192x192, 512x512)
- ✅ Service worker caches essential assets
- ✅ Offline mode works for cached pages
- ✅ Wake lock prevents screen sleep
- ✅ Install popup shows on first visit

## 🔐 Security Considerations

### Network Security
- **Local network only** - No internet exposure
- **No authentication** - Designed for trusted networks
- **Port binding** - Careful with wildcard bindings

### Code Security
- **Input validation** - Validate all HTTP inputs
- **Error handling** - Don't expose sensitive info in errors
- **Privilege escalation** - Only request admin when needed

## 📝 Commit Guidelines

### Commit Messages
Use clear, descriptive commit messages:

```
✅ Good:
- "Add volume control debouncing to prevent rapid duplicates"
- "Fix fullscreen toggle on multi-monitor setups"
- "Update PWA manifest icons to 512x512"

❌ Bad:
- "fix bug"
- "update stuff"
- "changes"
```

### Commit Structure
- One logical change per commit
- Keep commits focused and atomic
- Reference issue numbers: `Fix #123: Volume control issue`

## 🧪 Pull Request Process

1. **Update documentation** if adding features
2. **Test thoroughly** on multiple devices
3. **Keep changes focused** - one feature/fix per PR
4. **Describe changes** clearly in PR description
5. **Respond to feedback** promptly

### PR Checklist
- [ ] Code builds without errors
- [ ] Tested on Windows 10/11
- [ ] Tested on iOS/Android browsers
- [ ] No new warnings or errors in logs
- [ ] Documentation updated (if needed)
- [ ] Icons still load properly
- [ ] PWA installation still works

## 📚 Resources

### Documentation
- [.NET 8 Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [HttpListener Class](https://learn.microsoft.com/en-us/dotnet/api/system.net.httplistener)
- [PWA Documentation](https://web.dev/progressive-web-apps/)
- [Service Workers](https://developer.mozilla.org/en-US/docs/Web/API/Service_Worker_API)

### Tools
- [QRCoder Library](https://github.com/codebude/QRCoder)
- [Windows Forms](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/)
- [user32.dll Reference](https://learn.microsoft.com/en-us/windows/win32/api/winuser/)

## 💬 Questions?

- **GitHub Issues** - For bug reports and feature requests
- **GitHub Discussions** - For questions and general discussion
- **Pull Requests** - For code contributions

## 🎯 Areas for Contribution

Looking for ideas? Here are some areas that need work:

### High Priority
- [ ] Add configuration UI for custom keybindings
- [ ] Support HTTPS for secure connections
- [ ] Add media position slider
- [ ] Display now playing information

### Medium Priority
- [ ] Multi-language support
- [ ] Custom themes/color schemes
- [ ] Keyboard shortcut customization
- [ ] Multiple device profiles

### Low Priority
- [ ] Statistics/usage tracking
- [ ] Plugin system
- [ ] Remote desktop support
- [ ] Cross-platform support (macOS/Linux)

## 📄 License

By contributing, you agree that your contributions will be licensed under the MIT License.

---

**Thank you for contributing to Sofa Remote!** 🛋️✨
