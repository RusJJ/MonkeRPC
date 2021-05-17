using System;
using System.IO;
using System.Threading;
using BepInEx;
using BepInEx.Configuration;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BananaHook;
using BananaHook.Utils;

namespace MonkeRPC
{
    /* That's me! */
    [BepInPlugin("net.rusjj.gorillatag.monkerpc", "Monke RPC", "1.2.0")]
    /* BananaHook: Our little API */
    [BepInDependency("net.rusjj.gtlib.bananahook", "1.0.0")]

    public class MonkeRPC : BaseUnityPlugin
    {
        /* My DISCORD variables! */
        private static Discord.Discord m_hDiscord = null;
        private static Discord.ActivityManager m_hActivityManager = null;
        private static Discord.Activity m_hActivity = new Discord.Activity { Instance = true };

        /* My CONFIG variables! */
        private static ConfigEntry<bool> m_hCfgIsRPCEnabled;
        private static ConfigEntry<bool> m_hCfgUpdateTimeOnRoomJoin;
        private static ConfigEntry<bool> m_hCfgShowSmallImage;

        private static ConfigEntry<string> m_cfgLargeImageText;
        private static ConfigEntry<string> m_cfgSmallImageText;
        private static ConfigEntry<string> m_cfgState_Lobby;
        private static ConfigEntry<string> m_cfgState_PrivateRoom;
        private static ConfigEntry<string> m_cfgState_PublicRoom;
        private static ConfigEntry<string> m_cfgDetails_Lobby;
        private static ConfigEntry<string> m_cfgDetails_PrivateRoom;
        private static ConfigEntry<string> m_cfgDetails_PublicRoom;

        /* My OWN variables! */
        private static Dictionary<string, string> m_hKVDictionary = new Dictionary<string, string>()
        {
            { "nickname", "" },
            { "mapname", "" },
            { "mode", "Default" },
            { "code", "" },
            { "players", "-1" },
            { "maxplayers", "-1" },
            { "roomprivacy", "Private" },
        };
        static readonly Regex m_hRegex = new Regex(@"\{(\w+)\}", RegexOptions.Compiled);

        private static int m_nUpdateRate = 10;
        private static bool m_bIsOnCustomMap = false;
        private static string m_sRoomCode = null;
        private static string m_sCustomMapName = null;
        private static string m_sCustomMapFile = null;
        private static bool m_bIsInPrivateLobby = false;

        /* My functions! */
        private static void UpdateActivity()
        {
            if (m_hCfgIsRPCEnabled.Value == true)
            {
                m_hActivity.Assets.LargeText = RegexReplace(m_cfgLargeImageText.Value);
                m_hActivity.Assets.SmallText = RegexReplace(m_cfgSmallImageText.Value);

                if (m_sRoomCode == null) // Lobby
                {
                    m_hActivity.Details = RegexReplace(m_cfgDetails_Lobby.Value);
                    m_hActivity.State = RegexReplace(m_cfgState_Lobby.Value);
                }
                else // Playing
                {
                    if (m_bIsInPrivateLobby) // Ugh... Private lobby.
                    {
                        m_hActivity.Details = RegexReplace(m_cfgDetails_PrivateRoom.Value);
                        m_hActivity.State = RegexReplace(m_cfgState_PrivateRoom.Value);
                    }
                    else // Public lobby! F-U-N !!!
                    {
                        m_hActivity.Details = RegexReplace(m_cfgDetails_PublicRoom.Value);
                        m_hActivity.State = RegexReplace(m_cfgState_PublicRoom.Value);
                    }
                }

                m_hActivityManager.UpdateActivity(m_hActivity, result =>
                {
                    //m_hMe.Logger.LogInfo("Update Activity " + result.ToString());
                });
            }
        }
        private static void DiscordGo()
        {
            /* Give some time for game to initialize */
            Thread.Sleep(3000);
            
            m_hDiscord = new Discord.Discord(837692600189190174, (UInt64)Discord.CreateFlags.Default);
            m_hActivityManager = m_hDiscord.GetActivityManager();
            m_hActivityManager.RegisterSteam(1533390);

            m_hKVDictionary["code"] = "";
            m_hActivity.Assets.LargeImage = "lobby";
            m_hKVDictionary["mapname"] = "Lobby";
            m_hActivity.Timestamps.Start = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();
            UpdateActivity();

            if (m_hCfgShowSmallImage.Value) m_hActivity.Assets.SmallImage = "gorillatag_forest";
            try
            {
                while (true)
                {
                    if (m_nUpdateRate < 1 || m_nUpdateRate > 1000) m_nUpdateRate = 10;
                    Thread.Sleep(1000 / m_nUpdateRate);
                    m_hDiscord.RunCallbacks();
                }
            }
            finally
            {
                m_hDiscord.Dispose();
            }
        }
        void Awake()
        {
            Events.OnRoomJoined += RoomJoined;
            Events.OnRoomDisconnected += RoomDisconnected;
            Events.OnLocalNicknameChange += OnMyNicknameChange;
            Events.OnPlayerConnected += OnPlayerCountChange;
            Events.OnPlayerDisconnectedPost += OnPlayerCountChange;

            var hCfgFile = new ConfigFile(Path.Combine(Paths.ConfigPath, "MonkeRPC.cfg"), true);
            m_hCfgIsRPCEnabled = hCfgFile.Bind("CFG", "IsRPCEnabled", true, "Should Discord RPC be enabled?");
            m_hCfgUpdateTimeOnRoomJoin = hCfgFile.Bind("CFG", "UpdateTimeOnRoomJoin", false, "Update time when joining a room?");
            m_hCfgShowSmallImage = hCfgFile.Bind("CFG", "ShowSmallImage", true, "Should Discord show a little image in a bottom-right corner?");

            m_cfgLargeImageText = hCfgFile.Bind("CustomRPC", "LargeImageText", "{mapname}", "A text when you're hovering a mouse over large image");
            m_cfgSmallImageText = hCfgFile.Bind("CustomRPC", "SmallImageText", "{nickname}", "A text when you're hovering a mouse over small image");

            m_cfgState_Lobby = hCfgFile.Bind("CustomRPC", "State_Lobby", "Not joined", "A state in RPC when you're not joined to any room");
            m_cfgState_PrivateRoom = hCfgFile.Bind("CustomRPC", "State_PrivateRoom", "{mode} ({players}/{maxplayers})", "A state in RPC when you're in a private room");
            m_cfgState_PublicRoom = hCfgFile.Bind("CustomRPC", "State_PublicRoom", "{mode} ({players}/{maxplayers})", "A state in RPC when you're in a public room");

            m_cfgDetails_Lobby = hCfgFile.Bind("CustomRPC", "Details_Lobby", "Explores a dead tree", "A details in RPC when you're not joined to any room");
            m_cfgDetails_PrivateRoom = hCfgFile.Bind("CustomRPC", "Details_PrivateRoom", "Playing (Private Room)", "A details in RPC when you're in a private room");
            m_cfgDetails_PublicRoom = hCfgFile.Bind("CustomRPC", "Details_PublicRoom", "Playing (Room {code})", "A details in RPC when you're in a public room");

            Thread hDiscordThread = new Thread(DiscordGo);
            hDiscordThread.Start();
        }
        public static void SetCustomMapForRPC(string mapname = null, string mapfile = null)
        {
            if (mapname != null && mapfile != null)
            {
                m_bIsOnCustomMap = true;
                m_sCustomMapName = mapname;
                // Discord assets cant have anything except letters, digits and underscore (_)
                m_sCustomMapFile = mapfile.ToLower().Replace('\'', '_').Replace('.', '_').Replace('(', '_').Replace(')', '_');
            }
            else
            {
                m_bIsOnCustomMap = false;
            }
            UpdateActivity();
        }
        private void RoomJoined(object sender, RoomJoinedArgs e)
        {
            if (e != null)
            {
                m_bIsInPrivateLobby = e.isPrivate;
                m_hKVDictionary["code"] = m_sRoomCode = e.roomCode;
                m_hKVDictionary["roomprivacy"] = m_bIsInPrivateLobby ? "Private" : "Public";

                string sMyTemp = UnityEngine.PlayerPrefs.GetString("currentQueue", "DEFAULT");
                m_hKVDictionary["mode"] = sMyTemp.Substring(0, 1) + sMyTemp.Substring(1).ToLower();
                if (m_bIsOnCustomMap)
                {
                    if (m_sRoomCode != null)
                    {
                        m_hKVDictionary["mapname"] = m_sCustomMapName;
                        m_hActivity.Assets.LargeImage = m_sCustomMapFile;
                    }
                }
                else
                {
                    switch(Room.m_eTriggeredMap)
                    {
                        case eJoinedMap.Cave:
                            m_hKVDictionary["mapname"] = "Cave";
                            m_hActivity.Assets.LargeImage = "gorillatag_cave";
                            break;

                        case eJoinedMap.Canyon:
                            m_hKVDictionary["mapname"] = "Canyon";
                            m_hActivity.Assets.LargeImage = "gorillatag_desert"; // Yeah, that's a desert, sorry ;D
                            break;

                        default:
                            m_hKVDictionary["mapname"] = "Forest";
                            m_hActivity.Assets.LargeImage = "gorillatag_forest";
                            break;
                    }
                }
                m_hKVDictionary["players"] = Room.GetPlayers().ToString();
                m_hKVDictionary["maxplayers"] = Room.GetMaxPlayers().ToString();
            }
            if (m_hCfgUpdateTimeOnRoomJoin.Value == true)
            {
                m_hActivity.Timestamps.Start = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();
            }
            UpdateActivity();
        }
        private void RoomDisconnected(object sender, EventArgs e)
        {
            m_sRoomCode = null;
            m_hKVDictionary["code"] = "";
            m_hActivity.Assets.LargeImage = "lobby";
            m_hKVDictionary["mapname"] = "Lobby";
            UpdateActivity();
        }
        private void OnMyNicknameChange(object sender, PlayerNicknameArgs e)
        {
            m_hKVDictionary["nickname"] = e.newNickName;
            UpdateActivity();
        }
        private void OnPlayerCountChange(object sender, EventArgs e)
        {
            m_hKVDictionary["players"] = Room.GetPlayers().ToString();
            UpdateActivity();
        }
        public static string RegexReplace(string sText)
        {
            return m_hRegex.Replace(sText, match => {
                return m_hKVDictionary.ContainsKey(match.Groups[1].Value) ? m_hKVDictionary[match.Groups[1].Value] : match.Value;
            });
        }
        public static void AddKV(string sKey, string sValue)
        {
            m_hKVDictionary[sKey] = sValue;
        }
        public static bool IsOnACustomMap()
        {
            return m_bIsOnCustomMap;
        }
    }
}