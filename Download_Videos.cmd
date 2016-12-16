@echo off
cd /d "%~dp0"

%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe -ExecutionPolicy Unrestricted "(New-Object System.Net.WebClient).DownloadFile('https://yt-dl.org/downloads/2016.11.22/youtube-dl.exe', 'youtube-dl.exe')"

.\youtube-dl.exe "https://www.youtube.com/watch?v=8Y-aZ0rSzLo"
ren "Azure Media Services 101 - Get your video online now!-8Y-aZ0rSzLo.mp4" ams101.mp4

.\youtube-dl.exe "https://www.youtube.com/watch?v=U1GDGgGEtKY"
ren "Azure Media Services 102 - Dynamic Packaging and Mobile Devices-U1GDGgGEtKY.mp4" ams102.mp4