$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$stage = Join-Path $root 'build\tools'
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Path $stage -Force | Out-Null
$packages = Join-Path $env:LOCALAPPDATA 'Microsoft\WinGet\Packages'
Copy-Item (Join-Path $env:LOCALAPPDATA 'YT-DLP-Downloader\yt-dlp.exe') $stage
foreach ($name in @('ffmpeg.exe', 'ffprobe.exe', 'deno.exe')) {
    $source = Get-ChildItem $packages -Recurse -Filter $name | Select-Object -First 1
    if (-not $source) { throw "Missing $name" }
    Copy-Item $source.FullName $stage
}
$license = Get-ChildItem (Join-Path $packages 'yt-dlp.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe') -Recurse -Filter LICENSE.txt | Select-Object -First 1
if (-not $license) { throw 'Missing FFmpeg license' }
Copy-Item $license.FullName (Join-Path $stage 'FFmpeg-LICENSE.txt')
Invoke-WebRequest 'https://raw.githubusercontent.com/denoland/deno/v2.9.8/LICENSE.md' -OutFile (Join-Path $stage 'Deno-LICENSE.txt')
Invoke-WebRequest 'https://raw.githubusercontent.com/yt-dlp/yt-dlp/2026.10.26/LICENSE' -OutFile (Join-Path $stage 'yt-dlp-LICENSE.txt')
Copy-Item (Join-Path $root 'THIRD-PARTY.txt') $stage
Copy-Item (Join-Path $root 'licenses\yt-dlp-THIRD_PARTY_LICENSES.txt') $stage
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = Join-Path $root 'build\tools.zip'
if (Test-Path $zip) { Remove-Item $zip }
[IO.Compression.ZipFile]::CreateFromDirectory($stage, $zip, [IO.Compression.CompressionLevel]::Optimal, $false)
$desktop = [Environment]::GetFolderPath('Desktop')
$icon = Join-Path $root 'YouTubeDownloader.ico'
$output = Join-Path $root 'build\YouTubeDownloader.exe'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler /nologo /target:winexe /platform:x64 /optimize+ "/out:$output" "/win32icon:$icon" "/resource:$zip,tools.zip" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll (Join-Path $root 'Program.cs')
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed' }
Get-Item $output | Select-Object FullName, Length
