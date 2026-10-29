$ErrorActionPreference='Stop'
Push-Location $PSScriptRoot
try {
 & "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /platform:x64 /out:BLUB-Display.exe /win32icon:App.ico /resource:App.ico,AioDisplay.Icon /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Core.dll Program.cs UsbDisplay.cs Afterburner.cs PanelRenderer.cs FanData.cs Settings.cs UiTheme.cs
 if($LASTEXITCODE -ne 0){throw 'Build fehlgeschlagen'}
} finally {Pop-Location}
