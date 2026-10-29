$ErrorActionPreference='Stop'
Push-Location $PSScriptRoot
try {
 & "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /platform:x64 /out:BLUB-Display.exe /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Core.dll Program.cs UsbDisplay.cs Afterburner.cs PanelRenderer.cs
 if($LASTEXITCODE -ne 0){throw 'Build fehlgeschlagen'}
} finally {Pop-Location}
