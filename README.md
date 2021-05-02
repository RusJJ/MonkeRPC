# What is this mod?
A mod for PC version of Gorilla Tag that shows an info about your current gamestate in Discord.

## Manual Installation
Be sure that you are already installed BepInEx (using MonkeModManager for example). You need x64 version. [Take it there.](https://github.com/BepInEx/BepInEx/releases)

If your BepInEx copy installed then copy-paste MonkeRPC.dll to your Gorilla Tag/BepInEx/plugins folder somewhere (Gorilla Tag/BepInEx/MonkeRPC/MonkeRPC.dll for example).

Download Discord Game SDK: https://discord.com/developers/docs/game-sdk/sdk-starter-guide

Open discord_game_sdk.zip and follow that path: discord_game_sdk.zip\lib\x86_64. You need to copy discord_game_sdk.dll near the "Gorilla Tag.exe".

## Requirements
Utilla for private room detecting: https://github.com/legoandmars/Utilla/releases/latest

ComputerInterface for other things (room code, player name, current map): https://github.com/ToniMacaroni/ComputerInterface/releases

## Config

You can found a config at this path: `Gorilla Tag/BepInEx/config/MonkeRPC.cfg`

Just open it and you'll see everything you need.