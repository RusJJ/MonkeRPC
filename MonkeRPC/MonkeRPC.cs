using System;
using System.IO;
using System.Threading;
using BepInEx;
using BepInEx.Configuration;
using Utilla;

namespace MonkeRPC
{
    /* That's me! */
    [BepInPlugin("net.rusjj.gorillatag.monkerpc", "Monke RPC", "1.0.1")]
    /* Utilla: OnRoomJoined, used for private lobby detecting */
    [BepInDependency("org.legoandmars.gorillatag.utilla", "1.3.0")]
    /* ComputerInterface: Used for room details */
    [BepInDependency("tonimacaroni.computerinterface", "1.4.1")]

    public class MonkeRPC : BaseUnityPlugin
    {
        private static ConfigEntry<bool> m_hCfgIsRPCEnabled;
        private static ConfigEntry<bool> m_hCfgShowRoomCodeEnabled;
        private static ConfigEntry<bool> m_hCfgShowPrivateRoomCodeEnabled;
        private static ConfigEntry<bool> m_hCfgSmallIconShowsNickname;

        private static string m_sCurrentLobbyMode = "Default Mode";

        private static ComputerInterface.BaseGameInterface.EGroup m_eJoinedMap = 0;
        private static bool m_bIsInPrivateLobby = false;
        private static MonkeRPC m_hMe = null;
        private static GorillaTagger m_hMeTagger = null;
        private static Discord.Discord m_hDiscord = null;
        private static Discord.Activity m_hActivity = new Discord.Activity
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

        static void UpdateActivity()
        {
            if(m_hCfgIsRPCEnabled.Value == true)
            {
                var activityManager = m_hDiscord.GetActivityManager();
                var lobbyManager = m_hDiscord.GetLobbyManager();

                activityManager.UpdateActivity(m_hActivity, result =>
                {
                    //m_hMe.Logger.LogInfo("Update Activity " + result.ToString());
                });
            }
        }
        static void DiscordGo()
        {
            /* Not enough time for game to initialize */
            Thread.Sleep(3000);

            m_hDiscord = new Discord.Discord(837692600189190174, (UInt64)Discord.CreateFlags.Default);
            /*hDiscord.SetLogHook(Discord.LogLevel.Debug, (level, message) =>
            {
                m_hMe.Logger.LogDebug("DiscordLog[" + level.ToString() + "] " + message);
            });*/

            //var applicationManager = m_hDiscord.GetApplicationManager();
            var activityManager = m_hDiscord.GetActivityManager();
            activityManager.RegisterSteam(1533390);
            /*var lobbyManager = m_hDiscord.GetLobbyManager();
            var imageManager = m_hDiscord.GetImageManager();
            var userManager = m_hDiscord.GetUserManager();
            var relationshipManager = m_hDiscord.GetRelationshipManager();
            var storageManager = m_hDiscord.GetStorageManager();*/
            try
            {
                while (true)
                {
                    Thread.Sleep(1000 / 60);
                    m_hDiscord.RunCallbacks();
                    //lobbyManager.FlushNetwork();
                    PreDiscordRPC();
                }
            }
            finally
            {
                m_hDiscord.Dispose();
            }
        }
        public static void PreDiscordRPC()
        {
            if(m_hMeTagger == null)
            {
                m_hMeTagger = GorillaTagger.Instance;
                if (m_hMeTagger == null) return;
            }

            /* Current player state */
            if(ComputerInterface.BaseGameInterface.GetRoomCode() == null)
            {
                m_hActivity.State = "Not Joined";
                m_hActivity.Assets.LargeImage = "lobby";
                m_hActivity.Assets.LargeText = "In Lobby";
                m_hActivity.Details = "Explores a dead tree";
            }
            else
            {
                if (m_hCfgShowRoomCodeEnabled.Value == true)
                {
                    if (m_bIsInPrivateLobby)
                    {
                        if(m_hCfgShowPrivateRoomCodeEnabled.Value == true) m_hActivity.State = "Playing (Private Room " + ComputerInterface.BaseGameInterface.GetRoomCode() + ")";
                        else m_hActivity.State = "Playing (Private Room)";
                    }
                    else m_hActivity.State = "Playing (Room " + ComputerInterface.BaseGameInterface.GetRoomCode() + ")";
                }
                else m_hActivity.State = "Playing";

                m_hActivity.Details = m_sCurrentLobbyMode;

                switch (m_eJoinedMap)
                {
                    default:
                        m_hActivity.Assets.LargeImage = "gorillatag_forest";
                        m_hActivity.Assets.LargeText = "Forest";
                        break;
                    case ComputerInterface.BaseGameInterface.EGroup.Cave:
                        m_hActivity.Assets.LargeImage = "gorillatag_cave";
                        m_hActivity.Assets.LargeText = "Cave";
                        break;
                    case ComputerInterface.BaseGameInterface.EGroup.Canyon:
                        m_hActivity.Assets.LargeImage = "gorillatag_desert";
                        m_hActivity.Assets.LargeText = "Canyon";
                        break;
                }
            }

            /* Nickname */
            if (m_hCfgSmallIconShowsNickname.Value == true)
            {
                m_hActivity.Assets.SmallImage = "gorillatag_forest";
                m_hActivity.Assets.SmallText = ComputerInterface.BaseGameInterface.GetName();
            }

            UpdateActivity();
        }
        public void Awake()
        {
            m_hMe = this;

            Utilla.Events.RoomJoined += RoomJoined;

            var hCfgFile = new ConfigFile(Path.Combine(Paths.ConfigPath, "MonkeRPC.cfg"), true);
            m_hCfgIsRPCEnabled = hCfgFile.Bind("CFG", "IsRPCEnabled", true, "Should Discord RPC be enabled?");
            m_hCfgShowRoomCodeEnabled = hCfgFile.Bind("CFG", "IsRoomCodeVisible", true, "Can everyone see a code of the room?");
            m_hCfgShowPrivateRoomCodeEnabled = hCfgFile.Bind("CFG", "IsPrivateRoomCodeVisible", true, "Can everyone see a code of the PRIVATE room?");
            m_hCfgSmallIconShowsNickname = hCfgFile.Bind("CFG", "SmallIconShowsNickname", true, "Should a small icon in Discord show your nickname?");

            Thread hDiscordThread = new Thread(DiscordGo);
            hDiscordThread.Start();
        }

        private void RoomJoined(object sender, Events.RoomJoinedArgs e)
        {
            if (e != null)
            {
                m_bIsInPrivateLobby = e.isPrivate;

                m_sCurrentLobbyMode = ComputerInterface.BaseGameInterface.GetQueueMode().ToString() + " Mode";

                if (m_hMeTagger.transform.position.z < -90.0f)
                {
                    m_eJoinedMap = ComputerInterface.BaseGameInterface.EGroup.Canyon;
                }
                else if (m_hMeTagger.transform.position.y < 9.0f)
                {
                    m_eJoinedMap = ComputerInterface.BaseGameInterface.EGroup.Cave;
                }
                else
                {
                    m_eJoinedMap = ComputerInterface.BaseGameInterface.EGroup.Forest;
                }
            }
        }
    }
}