# Sofa Remote Build Script
# Builds single-file executable

Write-Host "Building Sofa Remote..." -ForegroundColor Cyan
Write-Host ""

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
Remove-Item -Recurse -Force .\bin\Release -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force .\publish -ErrorAction SilentlyContinue

# Build single-file executable
Write-Host ""
Write-Host "Building single-file version..." -ForegroundColor Green
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\publish\single-file
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Single-file build completed: .\publish\single-file\SofaRemote.exe" -ForegroundColor Green
} else {
    Write-Host "✗ Single-file build failed" -ForegroundColor Red
    exit 1
}

# Copy icons to output folder
Write-Host ""
Write-Host "Copying icons..." -ForegroundColor Yellow
Copy-Item -Recurse -Force .\icons .\publish\single-file\icons

# Show file size
Write-Host ""
Write-Host "Build Summary:" -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────" -ForegroundColor DarkGray

$singleSize = (Get-Item .\publish\single-file\SofaRemote.exe).Length / 1MB

Write-Host "Single-file build:" -ForegroundColor White
Write-Host "  Location: .\publish\single-file\" -ForegroundColor Gray
Write-Host "  Size: $($singleSize.ToString('0.00')) MB (all-in-one)" -ForegroundColor Gray
Write-Host ""
Write-Host "✓ Build completed successfully!" -ForegroundColor Green

# Create installer if Inno Setup is installed
Write-Host ""
Write-Host "Creating Installer..." -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────" -ForegroundColor DarkGray

$innoSetup = Get-Command "iscc.exe" -ErrorAction SilentlyContinue
if ($innoSetup) {
    Write-Host "Running Inno Setup compiler..." -ForegroundColor Yellow
    & iscc.exe "SofaRemote.iss" /Q
    if ($LASTEXITCODE -eq 0) {
        $installerPath = Get-Item ".\publish\installers\SofaRemote-Setup-*.exe" -ErrorAction SilentlyContinue
        if ($installerPath) {
            $installerSize = [math]::Round($installerPath.Length / 1MB, 2)
            Write-Host "✓ Installer created: $($installerPath.Name)" -ForegroundColor Green
            Write-Host "  Size: $installerSize MB" -ForegroundColor Gray
            Write-Host "  Location: .\publish\installers\" -ForegroundColor Gray
        }
    } else {
        Write-Host "✗ Installer creation failed" -ForegroundColor Red
    }
} else {
    Write-Host "⚠ Inno Setup not found - installer not created" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "To create installers:" -ForegroundColor Gray
    Write-Host "  1. Download Inno Setup from: https://jrsoftware.org/isinfo.php" -ForegroundColor Gray
    Write-Host "  2. Install it (adds to PATH automatically)" -ForegroundColor Gray
    Write-Host "  3. Run this build script again" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Or compile manually: iscc.exe SofaRemote.iss" -ForegroundColor Gray
}
