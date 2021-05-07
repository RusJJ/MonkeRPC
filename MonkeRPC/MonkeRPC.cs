using System;
using System.IO;
using System.Threading;
using BepInEx;
using BepInEx.Configuration;
using Utilla;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Photon.Pun;

namespace MonkeRPC
{
    /* That's me! */
    [BepInPlugin("net.rusjj.gorillatag.monkerpc", "Monke RPC", "1.1.0")]
    /* Utilla: OnRoomJoined, used for private lobby detecting */
    [BepInDependency("org.legoandmars.gorillatag.utilla", "1.3.4")]

    public class MonkeRPC : BaseUnityPlugin
    {
        /* My DISCORD variables! */
        private static Discord.Discord m_hDiscord = null;
        private static Discord.ActivityManager m_hActivityManager = null;
        private static Discord.Activity m_hActivity = new Discord.Activity
        {
            Timestamps = {
                Start = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds(),
            }, Instance = true
        };

        /* My CONFIG variables! */
        private static ConfigEntry<bool> m_hCfgIsRPCEnabled;
        private static ConfigEntry<bool> m_hCfgUpdateTimeOnRoomJoin;

        private static ConfigEntry<string> m_cfgLargeImageText;
        private static ConfigEntry<string> m_cfgSmallImageText;
        private static ConfigEntry<string> m_cfgState_Lobby;
        private static ConfigEntry<string> m_cfgState_PrivateRoom;
        private static ConfigEntry<string> m_cfgState_PublicRoom;
        private static ConfigEntry<string> m_cfgDetails_Lobby;
        private static ConfigEntry<string> m_cfgDetails_PrivateRoom;
        private static ConfigEntry<string> m_cfgDetails_PublicRoom;

        /* My OWN variables! */
        private static Dictionary<string, string> m_hKVDictionary = new Dictionary<string, string>() { };
        static readonly Regex m_hRegex = new Regex(@"\{(\w+)\}", RegexOptions.Compiled);

        private static int m_nUpdateRate = 10;
        private static int m_nCurrentPlayers = 0;
        private static int m_nMaxPlayers = 0;
        private static bool m_bIsOnCustomMap = false;
        private static string m_sRoomCode = "Just@A $Random&Code";
        private static string m_sNickname = "Just@A $Random&Name";
        private static string m_sCustomMapName = null;
        private static string m_sCustomMapFile = null;
        private static bool m_bIsInPrivateLobby = false;

        private static GorillaTagger m_hMeTagger = null;

        /* My functions! */
        private static void UpdateActivity()
        {
            if (m_hCfgIsRPCEnabled.Value == true)
            {
                m_hActivity.Assets.LargeText = RegexReplace(m_cfgLargeImageText.Value);
                m_hActivity.Assets.SmallText = RegexReplace(m_cfgSmallImageText.Value);

                if(m_sRoomCode == null) // Lobby
                {
                    m_hActivity.Details = RegexReplace(m_cfgDetails_Lobby.Value);
                    m_hActivity.State = RegexReplace(m_cfgState_Lobby.Value);
                }
                else // Playing
                {
                    if(m_bIsInPrivateLobby) // Ugh... Private lobby.
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
            /* Not enough time for game to initialize */
            Thread.Sleep(3000);
            
            m_hDiscord = new Discord.Discord(837692600189190174, (UInt64)Discord.CreateFlags.Default);
            m_hActivityManager = m_hDiscord.GetActivityManager();
            m_hActivityManager.RegisterSteam(1533390);
            try
            {
                while (true)
                {
                    if(m_nUpdateRate < 1) Thread.Sleep(1000 / 30);
                    else Thread.Sleep(1000 / m_nUpdateRate);

                    m_hDiscord.RunCallbacks();
                    OnDiscordRPC();
                }
            }
            finally
            {
                m_hDiscord.Dispose();
            }
        }
        private static void OnDiscordRPC()
        {
            if (m_hMeTagger == null)
            {
                m_hMeTagger = GorillaTagger.Instance;
                if (m_hMeTagger == null) return;
            }

            bool bIsUpdated = false;

            /* Room Code updated! */
            if (GetRoomCode() != m_sRoomCode)
            {
                bIsUpdated = true;
                m_sRoomCode = GetRoomCode();

                if (m_sRoomCode == null)
                {
                    m_hKVDictionary["code"] = "";
                    m_hActivity.Assets.LargeImage = "lobby";
                    m_hKVDictionary["mapname"] = "Lobby";
                }
                else
                {
                    m_hKVDictionary["code"] = m_sRoomCode;
                }
            }

            if(m_sRoomCode != null && m_nCurrentPlayers != PhotonNetwork.CurrentRoom.PlayerCount)
            {
                bIsUpdated = true;
                m_nCurrentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
                m_hKVDictionary["players"] = m_nCurrentPlayers.ToString();
            }

            if(PhotonNetwork.LocalPlayer.NickName != m_sNickname)
            {
                bIsUpdated = true;
                m_sNickname = PhotonNetwork.LocalPlayer.NickName;
                m_hKVDictionary["nickname"] = m_sNickname;
                m_hActivity.Assets.SmallImage = "gorillatag_forest";
            }

            if(bIsUpdated)
            {
                UpdateActivity();
            }
        }
        void Awake()
        {
            Utilla.Events.RoomJoined += RoomJoined;
            var hCfgFile = new ConfigFile(Path.Combine(Paths.ConfigPath, "MonkeRPC.cfg"), true);
            m_hCfgIsRPCEnabled = hCfgFile.Bind("CFG", "IsRPCEnabled", true, "Should Discord RPC be enabled?");
            m_hCfgUpdateTimeOnRoomJoin = hCfgFile.Bind("CFG", "UpdateTimeOnRoomJoin", false, "Update time when joining a room?");

            m_cfgLargeImageText = hCfgFile.Bind("CustomRPC", "LargeImageText", "{mapname}", "A text when you're hovering a mouse over large image");
            m_cfgSmallImageText = hCfgFile.Bind("CustomRPC", "SmallImageText", "{nickname}", "A text when you're hovering a mouse over small image");

            m_cfgState_Lobby = hCfgFile.Bind("CustomRPC", "State_Lobby", "Not joined", "A state in RPC when you're not joined to any room");
            m_cfgState_PrivateRoom = hCfgFile.Bind("CustomRPC", "State_PrivateRoom", "{mode} ({players}/{maxplayers})", "A state in RPC when you're in a private room");
            m_cfgState_PublicRoom = hCfgFile.Bind("CustomRPC", "State_PublicRoom", "{mode} ({players}/{maxplayers})", "A state in RPC when you're in a public room");

            m_cfgDetails_Lobby = hCfgFile.Bind("CustomRPC", "Details_Lobby", "Explores a dead tree", "A details in RPC when you're not joined to any room");
            m_cfgDetails_PrivateRoom = hCfgFile.Bind("CustomRPC", "Details_PrivateRoom", "Playing (Private {code})", "A details in RPC when you're in a private room");
            m_cfgDetails_PublicRoom = hCfgFile.Bind("CustomRPC", "Details_PublicRoom", "Playing (Room {code})", "A details in RPC when you're in a public room");

            Thread hDiscordThread = new Thread(DiscordGo);
            hDiscordThread.Start();
        }
        public static void SetCustomMapForRPC(string mapname = null, string mapfile = null)
        {
            if(mapname != null && mapfile != null)
            {
                m_bIsOnCustomMap = true;
                m_sCustomMapName = mapname;
                m_sCustomMapFile = mapfile.ToLower().Replace('\'', '_').Replace('.', '_').Replace('(', '_').Replace(')', '_');
            }
            else
            {
                m_bIsOnCustomMap = false;
            }
            UpdateActivity();
        }
        private void RoomJoined(object sender, Events.RoomJoinedArgs e)
        {
            if (e != null)
            {
                m_bIsInPrivateLobby = e.isPrivate;
                string sMyTemp = UnityEngine.PlayerPrefs.GetString("currentQueue", "DEFAULT");
                m_hKVDictionary["mode"] = sMyTemp.Substring(0, 1) + sMyTemp.Substring(1).ToLower();
                if (m_bIsOnCustomMap)
                {
                    if(m_sRoomCode != null)
                    {
                        m_hKVDictionary["mapname"] = m_sCustomMapName;
                        m_hActivity.Assets.LargeImage = m_sCustomMapFile;
                    }
                }
                else
                {
                    if (m_hMeTagger.transform.position.z < -90.0f)
                    {
                        m_hKVDictionary["mapname"] = "Canyon";
                        m_hActivity.Assets.LargeImage = "gorillatag_desert"; // Yeah, that's a desert, sorry ;D
                    }
                    else if (m_hMeTagger.transform.position.y < 9.0f && m_hMeTagger.transform.position.z < -79.0f)
                    {
                        m_hKVDictionary["mapname"] = "Cave";
                        m_hActivity.Assets.LargeImage = "gorillatag_cave";
                    }
                    else
                    {
                        m_hKVDictionary["mapname"] = "Forest";
                        m_hActivity.Assets.LargeImage = "gorillatag_forest";
                    }
                }
                
                m_nMaxPlayers = PhotonNetwork.CurrentRoom.MaxPlayers;
                m_hKVDictionary["maxplayers"] = m_nMaxPlayers.ToString();
            }

            if(m_hCfgUpdateTimeOnRoomJoin.Value == true)
            {
                m_hActivity.Timestamps.Start = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();
            }

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
        public static string GetRoomCode()
        {
            return PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.Name : null;
        }
    }
}