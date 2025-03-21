# 🎮 CS2 Discord RPC

A sleek and lightweight Discord Rich Presence integration for Counter-Strike 2, enhancing your profile with real-time game status updates.

## ✨ Features
✅ Displays your current CS2 game status in Discord
✅ Shows map details and match score
✅ Indicates your team (CT/T) and status (alive/dead)
✅ Auto-detects when CS2 is running
✅ Customizable display options
✅ Runs in the system tray for minimal interference

## 📥 Installation
1. Download the latest release from the [Releases](https://github.com/ZoniBoy00/CS2-Discord-RPC/releases) page.
2. Run `CS2-Discord-RPC.exe`.
3. The application will automatically configure the necessary CS2 Game State Integration files.

## 🔧 Requirements
- Windows 10/11
- Discord desktop application
- Counter-Strike 2

## 🖼️ Screenshots
![Main Menu](https://github.com/ZoniBoy00/CS2-Discord-RPC/blob/main/screenshots/Menu.png)
![In Game](https://github.com/ZoniBoy00/CS2-Discord-RPC/blob/main/screenshots/InGame.png)

## 🚀 Usage
1. Ensure Discord is running.
2. Launch `CS2-Discord-RPC.exe`.
3. Start Counter-Strike 2.
4. Your Discord status will automatically update based on your in-game activity.
5. Access settings by double-clicking the system tray icon or right-clicking and selecting "Settings".

## ⚙️ Configuration
The application offers several customization options:
- **Show Map:** Display the current map name.
- **Show Game Mode:** Display the current game mode (Competitive, Casual, etc.).
- **Show Score:** Display the current match score.
- **Show Team:** Display your current team and status (alive/dead).

## 🛠️ Building from Source
### Prerequisites
- .NET 8.0 SDK or later
- Visual Studio 2022 (or any compatible editor)

### Build Steps
```sh
git clone https://github.com/ZoniBoy00/CS2-Discord-RPC.git
cd CS2-Discord-RPC
dotnet build
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:PublishReadyToRun=true \
  -p:IncludeNativeLibrariesForSelfExtract=true
```
- The compiled executable can be found in `bin\Release\net8.0-windows\win-x64\publish`.

## 📝 How It Works
CS2 Discord RPC utilizes Valve's Game State Integration (GSI) to receive real-time game updates and seamlessly updates your Discord status using the Discord Rich Presence API.

## 🤝 Contributing
Contributions are welcome! Feel free to submit issues or pull requests.

## 📜 License
This project is licensed under the MIT License. See the [LICENSE](https://raw.githubusercontent.com/ZoniBoy00/CS2-Discord-RPC/refs/tags/v1.0.0/LICENSE) file for details.

## 🙏 Acknowledgments
- [Discord Rich Presence Library](https://github.com/Lachee/discord-rpc-csharp)
- Valve for the Game State Integration API

