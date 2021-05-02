using System;
using System.IO;
using System.Threading;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using Utilla;
using ComputerInterface.Interfaces;

namespace MonkeRPC
{
    /* That's me! */
    [BepInPlugin("net.rusjj.gorillatag.monkerpc", "Monke RPC", "1.0.0")]
    /* Utilla: OnRoomJoined, used for private lobby detecting */
    [BepInDependency("org.legoandmars.gorillatag.utilla", "1.3.0")]
    /* ComputerInterface: Used for room details */
    [BepInDependency("tonimacaroni.computerinterface", "1.4.1")]

    public class MonkeRPC : BaseUnityPlugin
    {
        public static bool bIsInPrivateLobby = false;
        public static MonkeRPC hMyInnerMonster = null;
        public static Discord.Discord discord = null;
        public static Discord.Activity activity = new Discord.Activity
        {
            State = "Not Joined",
            Details = "Monke Tag",
            Timestamps =
            {
                Start = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds(),
            },
            Assets =
            {
                LargeImage = "gorillatag_forest",
                LargeText = "This is Monke",
            },
            Instance = true,
        };
        private static ConfigEntry<bool> cfgEnabled;
        private static ConfigEntry<bool> showRoomCodeEnabled;
        private static ConfigEntry<bool> showPrivateRoomCodeEnabled;
        private static ConfigEntry<bool> smallIconShowsNickname;

        static void UpdateActivity()
        {
            if(cfgEnabled.Value == true)
            {
                var activityManager = discord.GetActivityManager();
                var lobbyManager = discord.GetLobbyManager();

                activityManager.UpdateActivity(activity, result =>
                {
                    //hMyInnerMonster.Logger.LogInfo("Update Activity " + result.ToString());
                });
            }
        }
        static void DiscordGo()
        {
            discord = new Discord.Discord(837692600189190174, (UInt64)Discord.CreateFlags.Default);
            discord.SetLogHook(Discord.LogLevel.Debug, (level, message) =>
            {
                hMyInnerMonster.Logger.LogDebug("DiscordLog[" + level.ToString() + "] " + message);
            });

            var applicationManager = discord.GetApplicationManager();
            var activityManager = discord.GetActivityManager();
            activityManager.RegisterSteam(1533390);
            var lobbyManager = discord.GetLobbyManager();
            var imageManager = discord.GetImageManager();
            var userManager = discord.GetUserManager();
            var relationshipManager = discord.GetRelationshipManager();
            var storageManager = discord.GetStorageManager();
            try
            {
                while (true)
                {
                    PreDiscordRPC();
                    discord.RunCallbacks();
                    lobbyManager.FlushNetwork();
                    Thread.Sleep(1000 / 60);
                }
            }
            finally
            {
                discord.Dispose();
            }
        }
        public static void PreDiscordRPC()
        {
            /* Current player state */
            if(ComputerInterface.BaseGameInterface.GetRoomCode() == null)
            {
                activity.State = "Not Joined";
                activity.Assets.LargeImage = "lobby";
                activity.Assets.LargeText = "In Lobby";
            }
            else
            {
                if (showRoomCodeEnabled.Value == true)
                {
                    if (bIsInPrivateLobby)
                    {
                        if(showPrivateRoomCodeEnabled.Value == true) activity.State = "Playing (Private Room " + ComputerInterface.BaseGameInterface.GetRoomCode() + ")";
                        else activity.State = "Playing (Private Room)";
                    }
                    else activity.State = "Playing (Room " + ComputerInterface.BaseGameInterface.GetRoomCode() + ")";
                }
                else activity.State = "Playing";

                switch(ComputerInterface.BaseGameInterface.GetGroupMode())
                {
                    default:
                        activity.Assets.LargeImage = "gorillatag_forest";
                        activity.Assets.LargeText = "Forest";
                        break;
                    case ComputerInterface.BaseGameInterface.EGroup.Cave:
                        activity.Assets.LargeImage = "gorillatag_cave";
                        activity.Assets.LargeText = "Cave";
                        break;
                    case ComputerInterface.BaseGameInterface.EGroup.Canyon:
                        activity.Assets.LargeImage = "gorillatag_desert";
                        activity.Assets.LargeText = "Canyon";
                        break;
                }
            }

            /* Current game mode */
            activity.Details = ComputerInterface.BaseGameInterface.GetQueueMode().ToString() + " Mode";

            /* Nickname */
            if (smallIconShowsNickname.Value == true)
            {
                activity.Assets.SmallImage = "gorillatag_forest";
                activity.Assets.SmallText = ComputerInterface.BaseGameInterface.GetName();
            }

            UpdateActivity();
        }
        public void Awake()
        {
            hMyInnerMonster = this;

            Utilla.Events.RoomJoined += RoomJoined;

            var hCfgFile = new ConfigFile(Path.Combine(Paths.ConfigPath, "MonkeRPC.cfg"), true);
            cfgEnabled = hCfgFile.Bind("CFG", "IsRPCEnabled", true, "Should Discord RPC be enabled?");
            showRoomCodeEnabled = hCfgFile.Bind("CFG", "IsRoomCodeVisible", true, "Can everyone see a code of the room?");
            showPrivateRoomCodeEnabled = hCfgFile.Bind("CFG", "IsPrivateRoomCodeVisible", true, "Can everyone see a code of the PRIVATE room?");
            smallIconShowsNickname = hCfgFile.Bind("CFG", "SmallIconShowsNickname", true, "Should a small icon in Discord show your nickname?");

            Thread thread1 = new Thread(DiscordGo);
            thread1.Start();
        }

        private void RoomJoined(object sender, Events.RoomJoinedArgs e)
        {
            if (e != null) bIsInPrivateLobby = e.isPrivate;
        }
    }
}