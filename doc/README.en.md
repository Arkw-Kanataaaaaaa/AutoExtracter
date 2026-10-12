# AutoExtracter

[日本語](../README.md)

## Overview

AutoExtracter is a Windows application that monitors a specified folder and automatically extracts compressed files placed in it.

It supports ZIP, 7z, and RAR archives and processes one archive at a time. Password-protected archives have lower priority, allowing regular archives to be processed first. Extracted files are saved in the completed folder, in a subfolder named after the archive without its extension.

## Installation

1. Download `AutoExtracter-Setup-x64.exe` from [GitHub Releases](https://github.com/Arkw-Kanataaaaaaa/AutoExtracter/releases) and run it.
2. Review the license and select the installation folder.
3. Select the watched folder, completed folder, desktop shortcut, and startup options.
4. Review your choices and install the application.
5. Choose whether to launch the application, then close the installer.

The default installation folder is `%LOCALAPPDATA%\AutoExtracter`.
If the watched and completed folders are left blank, the installer uses `AutoExtracter\Incoming` and `AutoExtracter\Completed` inside your Documents folder.

With the default settings, the installed application creates an `AutoExtracter` folder inside your Documents folder. Its subfolders include `Temporary` for extraction work, `Logs` for log files, `Errors` for failed archives, and `PasswordRequired` for archives requiring a password.

To uninstall the application, use Windows Settings under Installed apps, or run `Uninstall.exe` in the installation folder. Close AutoExtracter before uninstalling it.

The uninstaller lets you choose whether to delete application settings, logs, and the AutoExtracter folder inside Documents. This option is enabled by default. Other folders you selected, such as a custom completed folder, are not deleted.

## Requirements

- A 64-bit Windows environment.

The application and uninstaller include the .NET 8 runtime, so you do not need to install .NET separately.

To build from source, use Windows and the .NET 8 SDK. Run these commands from the repository root:

```powershell
dotnet build AutoExtracter.csproj -c Release
dotnet run --project AutoExtracter.csproj
```

The repository contains application source code and default settings. Installer-related files and build outputs are excluded. The installer is distributed through Releases.

## Usage

1. Launch AutoExtracter.
2. Place an archive in the watched folder.
3. Once copying is complete, the application moves the archive to its temporary folder and extracts it automatically.
4. Find the extracted files in the completed folder.

Left-click the system tray icon to toggle the notification panel. Right-click it to show the panel. Launching AutoExtracter while it is already running opens the existing instance's panel.

The panel displays the current archive name, size, progress, and queued archives. Click the watched folder name to open that folder, the gear icon to open settings, or the power icon to exit the application.

When the problem indicator appears, click it to open the relevant error or password folder and today's log. Logs are saved as `AutoExtracter-yyyyMMdd.log` and contain INFO, WARN, and ERROR entries with message identifiers.

## Settings

Processing settings include the watched folder, completed folder, archive retention, supported extensions, size limit, interactive password mode, and startup.

When archive retention is enabled, select a backup folder for the original archives. When it is disabled, the backup input and browse button remain visible but disabled, and original archives are deleted after successful extraction. Switching settings or sections does not resize the settings window.

The size limit applies to the compressed archive itself and is entered in GB. A red underline appears while editing a watched or completed folder path that does not exist.

When interactive password mode is enabled, the application displays a notification and a password input. Once password entry begins, subsequent extraction jobs wait until you enter a password or choose to move the archive aside. When the mode is disabled, password-protected archives are moved to the password folder. Passwords are not saved in settings or logs.

Applying processing settings prompts you to restart the application. Selecting OK saves the settings and restarts it. Selecting No returns to settings without saving. Startup changes take effect immediately.

Appearance settings include Light, Dark, and System themes, animations, and icons for idle, extraction, and password interaction states. Click an icon preview to choose a file. GIF files are copied into the application's settings storage when applied, so the original files can be deleted afterward. Appearance changes take effect immediately when applied. Disabling animations does not stop extraction or progress updates.

The Other section provides actions to create a desktop shortcut and open the installation folder or today's log. The default settings button restores input values to their defaults. Switching sections before applying discards unapplied changes and restores saved values.

## Libraries

See [NOTICE.md](NOTICE.md) for information about the libraries, font, and their licenses.

## License

AutoExtracter is licensed under the MIT License. See [LICENSE.txt](../LICENSE.txt) for the full text.

Bundled libraries and the font retain their respective licenses. See [NOTICE.md](NOTICE.md) for details.

## Bug Reports and Feedback

Please report bugs and feature requests through [GitHub Issues](https://github.com/Arkw-Kanataaaaaaa/AutoExtracter/issues).

Include the application version, Windows version, steps to reproduce the issue, time of occurrence, and relevant logs. Remove personal information and private folder names from logs as needed. Do not send archive passwords.

## Author

Arkw-Kanataaaaaaa
