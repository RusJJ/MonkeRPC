using System;
using System.IO;
using System.Threading;
using BepInEx;
using BepInEx.Configuration;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BananaHook;
using BananaHook.Utils;
using Discord;

namespace MonkeRPC
{
    /* That's me! */
    [BepInPlugin("net.rusjj.gorillatag.monkerpc", "Monke RPC", "1.3.0")]
    /* BananaHook: Our little API */
    [BepInDependency("net.rusjj.gtlib.bananahook", "1.1.0")]

    public class MonkeRPC : BaseUnityPlugin
    {
        /* My DISCORD variables! */
        private static Discord.Discord m_hDiscord = null;
        private static ActivityManager m_hActivityManager = null;
        private static Activity m_hActivity = new Activity { Instance = true };

        /* My CONFIG variables! */
        private static ConfigEntry<bool>   m_hCfgIsRPCEnabled;
        private static ConfigEntry<bool>   m_hCfgUpdateTimeOnRoomJoin;
        private static ConfigEntry<bool>   m_hCfgShowSmallImage;
        private static ConfigEntry<bool>   m_hCfgSmallImageShowTagged;
        private static ConfigEntry<string> m_hCfgInfectedText;
        private static ConfigEntry<string> m_hCfgNotInfectedText;
        private static ConfigEntry<string> m_hCfgInfectedImage;
        private static ConfigEntry<string> m_hCfgNotInfectedImage;

        private static ConfigEntry<string> m_cfgLargeImageText;
        private static ConfigEntry<string> m_cfgSmallImageText;
        private static ConfigEntry<string> m_cfgState_Lobby;
        private static ConfigEntry<string> m_cfgState_PrivateRoom;
        private static ConfigEntry<string> m_cfgState_PublicRoom;
        private static ConfigEntry<string> m_cfgState_TagPrivateRoom;
        private static ConfigEntry<string> m_cfgState_TagPublicRoom;
        private static ConfigEntry<string> m_cfgDetails_Lobby;
        private static ConfigEntry<string> m_cfgDetails_PrivateRoom;
        private static ConfigEntry<string> m_cfgDetails_PublicRoom;
        private static ConfigEntry<string> m_cfgDetails_TagPrivateRoom;
        private static ConfigEntry<string> m_cfgDetails_TagPublicRoom;

        private static ConfigEntry<string> m_cfgTaggedOnJoinText;
        private static ConfigEntry<string> m_cfgTaggedOnRoundStartText;

        /* My OWN variables! */
        private static Dictionary<string, string> m_hKVDictionary = new Dictionary<string, string>()
        {
            { "nickname", "" },
            { "mapname", "" },
            { "mode", "Default" },
            { "code", "" },
            { "players", "-1" },
            { "maxplayers", "-1" },
            { "infected", "-1" },
            { "notinfected", "-1" },
            { "roomprivacy", "Private" },
            { "tagged", "Surviving" },
            { "taggedme", "Server" },
            { "taggedbyme", "0" },
            { "region", "unknown" },
        };
        static readonly Regex m_hRegex = new Regex(@"\{(\w+)\}", RegexOptions.Compiled);

        private static   bool m_bIsOnCustomMap = false;
        private static   bool m_bIsInfected = false;
        private static string m_sCustomMapName = null;
        private static string m_sCustomMapFile = null;
        private static   bool m_bIsInPrivateLobby = false;
        private static    int m_nPlayersTaggedByMe = 0;

        /* My functions! */
        private static void UpdateActivity()
        {
            if (m_hCfgIsRPCEnabled.Value)
            {
                m_hActivity.Assets.LargeText = RegexReplace(m_cfgLargeImageText.Value);
                m_hActivity.Assets.SmallText = RegexReplace(m_cfgSmallImageText.Value);
                if (Room.m_szRoomCode == null) // Lobby
                {
                    m_hActivity.Details = RegexReplace(m_cfgDetails_Lobby.Value);
                    m_hActivity.State = RegexReplace(m_cfgState_Lobby.Value);
                }
                else // Playing
                {
                    if (m_bIsInPrivateLobby) // Ugh... Private lobby.
                    {
                        m_hActivity.Details = RegexReplace(m_bIsInfected ? m_cfgDetails_TagPrivateRoom.Value : m_cfgDetails_PrivateRoom.Value);
                        m_hActivity.State = RegexReplace(m_bIsInfected ? m_cfgState_TagPrivateRoom.Value : m_cfgState_PrivateRoom.Value);
                    }
                    else // Public lobby! F-U-N !!!
                    {
                        m_hActivity.Details = RegexReplace(m_bIsInfected ? m_cfgDetails_TagPublicRoom.Value : m_cfgDetails_PublicRoom.Value);
                        m_hActivity.State = RegexReplace(m_bIsInfected ? m_cfgState_TagPublicRoom.Value : m_cfgState_PublicRoom.Value);
                    }
                    if(m_hCfgShowSmallImage.Value && m_hCfgSmallImageShowTagged.Value)
                    {
                        m_hActivity.Assets.SmallImage = m_bIsInfected ? m_hCfgInfectedImage.Value : m_hCfgNotInfectedImage.Value;
                    }
                }
                try
                {
                    /*
                        0x00007FFCFFDF4B89 (KERNELBASE) RaiseException
                        0x00007FFC8B83D9FD (discord_game_sdk) rust_eh_personality
                        0x00007FFC8B81D838 (discord_game_sdk) DiscordVersion
                        0x00007FFC8B818F38 (discord_game_sdk) DiscordVersion
                        0x00007FFC8B818E0A (discord_game_sdk) DiscordVersion
                        0x00007FFC8B8188C4 (discord_game_sdk) DiscordVersion
                        0x00007FFC8B8187A9 (discord_game_sdk) DiscordVersion
                        0x00007FFC8B82A139 (discord_game_sdk) rust_eh_personality
                        0x00007FFC8B82A23F (discord_game_sdk) rust_eh_personality
                        0x00007FFC8B64389F (discord_game_sdk) DiscordVersion
                        0x000001AE61A50482 (mscorlib) System.Object.wrapper_native_00007FFC8B643640()
                        0x000001AE61A54E1A (MonkeRPC) Discord.ActivityManager.UpdateActivity()
                        0x000001AE61A532BB (MonkeRPC) MonkeRPC.MonkeRPC.UpdateActivity()
                        0x000001AEB5B2B593 (MonkeRPC) MonkeRPC.MonkeRPC.OnRoundStart()
                        0x000001ADA2563468 (mscorlib) System.EventHandler`1.invoke_void_object_TEventArgs()
                        0x000001AEB5B2B20E (BananaHook) BananaHook.Utils.Room.Thread_CheckForGameToStart()
                        0x000001ADED39A286 (mscorlib) System.Threading.ThreadHelper.ThreadStart_Context()
                        0x000001ADED39979E (mscorlib) System.Threading.ExecutionContext.RunInternal()
                        0x000001ADED39949B (mscorlib) System.Threading.ExecutionContext.Run()
                        0x000001ADED3990AB (mscorlib) System.Threading.ExecutionContext.Run()
                        0x000001ADED398FB3 (mscorlib) System.Threading.ThreadHelper.ThreadStart()
                        0x000001ADA25FE7C4 (mscorlib) System.Object.runtime_invoke_void__this__()
                        0x00007FFC9057CBA0 (mono-2.0-bdwgc) mono_get_runtime_build_info
                        0x00007FFC90502112 (mono-2.0-bdwgc) mono_perfcounters_init
                        0x00007FFC9050B2E2 (mono-2.0-bdwgc) mono_runtime_invoke_array
                        0x00007FFC90525ABF (mono-2.0-bdwgc) mono_threads_set_shutting_down
                        0x00007FFC90525806 (mono-2.0-bdwgc) mono_threads_set_shutting_down
                        0x00007FFD02117034 (KERNEL32) BaseThreadInitThunk
                        0x00007FFD02582651 (ntdll) RtlUserThreadStart
                    */
                    m_hActivityManager.UpdateActivity(m_hActivity, result => {});
                } catch (Exception e) { UnityEngine.Debug.LogError("Discord::UpdateActivity throws an exception:\n" + e.Message); }
                
            }
        }
        private static void DiscordGo()
        {
            /* Give some time for game to initialize */
            Thread.Sleep(5000);
            
            m_hDiscord = new Discord.Discord(837692600189190174, (UInt64)CreateFlags.NoRequireDiscord);
            m_hActivityManager = m_hDiscord.GetActivityManager();
            m_hActivityManager.RegisterSteam(1533390);

            m_hKVDictionary["code"] = "";
            m_hActivity.Assets.LargeImage = "lobby";
            m_hKVDictionary["mapname"] = "Lobby";
            m_hActivity.Timestamps.Start = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();
            if (m_hCfgShowSmallImage.Value) m_hActivity.Assets.SmallImage = m_hCfgNotInfectedImage.Value;
            UpdateActivity();

            try
            {
                while (true)
                {
                    try
                    {
                        Thread.Sleep(500);
                        m_hDiscord.RunCallbacks();
                    }
                    catch(ResultException e)
                    {
                        UnityEngine.Debug.LogError("Discord throws a ResultException: " + e.Message);
                    }
                }
            }
            finally
            {
                m_hDiscord.Dispose();
            }
        }
        void Awake()
        {
            Events.OnRoomJoined +=              OnRoomJoined;
            Events.OnRoomDisconnected +=        OnRoomDisconnected;
            Events.OnLocalNicknameChange +=     OnMyNicknameChange;
            Events.OnPlayerConnected +=         OnPlayerCountChange;
            Events.OnPlayerDisconnectedPost +=  OnPlayerCountChange;
            Events.OnPlayerTagPlayer +=         OnTaggedSomeone;
            Events.OnRoundStart +=              OnRoundStart;

            var hCfgFile =                      new ConfigFile(Path.Combine(Paths.ConfigPath, "MonkeRPC.cfg"), true);
            m_hCfgIsRPCEnabled =                hCfgFile.Bind("CFG", "IsRPCEnabled", true, "Should Discord RPC be enabled?");
            m_hCfgUpdateTimeOnRoomJoin =        hCfgFile.Bind("CFG", "UpdateTimeOnRoomJoin", true, "Update time when joining a room?");
            m_hCfgShowSmallImage =              hCfgFile.Bind("CFG", "ShowSmallImage", true, "Should Discord show a little image in a bottom-right corner?");
            m_hCfgSmallImageShowTagged =        hCfgFile.Bind("CFG", "SmallImageShowTagged", true, "Should Discord show an other little image if you're tagged?");
            m_hCfgInfectedText =                hCfgFile.Bind("CFG", "InfectedText", "Hunting", "A text for {tagged} code when you're infected (tagged)");
            m_hCfgNotInfectedText =             hCfgFile.Bind("CFG", "NotInfectedText", "Surviving", "A text for {tagged} code when you're NOT infected (tagged)");
            
            m_hCfgInfectedImage =               hCfgFile.Bind("CustomRPC", "InfectedImage", "gorillatag_lavaskin", "A text for {tagged} code when you're infected (tagged)");
            m_hCfgNotInfectedImage =            hCfgFile.Bind("CustomRPC", "NotInfectedImage", "gorillatag_gorilla", "A text for {tagged} code when you're NOT infected (tagged)");

            m_cfgLargeImageText =               hCfgFile.Bind("CustomRPC", "LargeImageText", "{mapname}", "A text when you're hovering a mouse over large image");
            m_cfgSmallImageText =               hCfgFile.Bind("CustomRPC", "SmallImageText", "{nickname} | {tagged}", "A text when you're hovering a mouse over small image");

            m_cfgState_Lobby =                  hCfgFile.Bind("CustomRPC", "State_Lobby", "Not joined", "A state in RPC when you're not joined to any room");
            m_cfgState_PrivateRoom =            hCfgFile.Bind("CustomRPC", "State_PrivateRoom", "{mode} ({players}/{maxplayers})", "A state in RPC when you're in a private room");
            m_cfgState_PublicRoom =             hCfgFile.Bind("CustomRPC", "State_PublicRoom", "{mode} ({players}/{maxplayers})", "A state in RPC when you're in a public room");
            m_cfgState_TagPrivateRoom =         hCfgFile.Bind("CustomRPC", "State_TagPrivateRoom", "{mode} ({players}/{maxplayers}). I tagged {taggedbyme}!", "A state in RPC when you're tagged in a private room");
            m_cfgState_TagPublicRoom =          hCfgFile.Bind("CustomRPC", "State_TagPublicRoom", "{mode} ({players}/{maxplayers}). I tagged {taggedbyme}!", "A state in RPC when you're tagged in a public room");

            m_cfgDetails_Lobby =                hCfgFile.Bind("CustomRPC", "Details_Lobby", "Explores a dead tree", "A details in RPC when you're not joined to any room");
            m_cfgDetails_PrivateRoom =          hCfgFile.Bind("CustomRPC", "Details_PrivateRoom", "Playing (Room)", "A details in RPC when you're in a private room");
            m_cfgDetails_PublicRoom =           hCfgFile.Bind("CustomRPC", "Details_PublicRoom", "Playing ({code})", "A details in RPC when you're in a public room");
            m_cfgDetails_TagPrivateRoom =       hCfgFile.Bind("CustomRPC", "Details_TagPrivateRoom", "Tagged by {taggedme} (Room)", "A details in RPC when you're tagged in a private room");
            m_cfgDetails_TagPublicRoom =        hCfgFile.Bind("CustomRPC", "Details_TagPublicRoom", "Tagged by {taggedme} ({code})", "A details in RPC when you're tagged in a public room");

            m_cfgTaggedOnJoinText =             hCfgFile.Bind("CustomRPC", "TaggedOnJoinText", "server on join", "A details in RPC when you're tagged");
            m_cfgTaggedOnRoundStartText =       hCfgFile.Bind("CustomRPC", "TaggedOnRoundStartText", "server on start", "A details in RPC when you're tagged");

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
        private void OnRoomJoined(object sender, RoomJoinedArgs e)
        {
            m_bIsInPrivateLobby = e.isPrivate;
            m_hKVDictionary["code"] = e.roomCode;
            m_hKVDictionary["roomprivacy"] = m_bIsInPrivateLobby ? "Private" : "Public";
            m_hKVDictionary["mode"] = Room.m_eCurrentLobbyMode.ToString();
            m_hKVDictionary["region"] = Photon.Pun.PhotonNetwork.CloudRegion.ToUpper();
            if (m_bIsOnCustomMap)
            {
                if (e.roomCode != null)
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

                    case eJoinedMap.GorillaShop:
                        m_hKVDictionary["mapname"] = "Nice Gorilla Shop";
                        m_hActivity.Assets.LargeImage = "gorillatag_shop"; // Yeah, that's a desert, sorry ;D
                        break;

                    default:
                        m_hKVDictionary["mapname"] = "Forest";
                        m_hActivity.Assets.LargeImage = "gorillatag_forest";
                        break;
                }
            }
            m_hKVDictionary["players"] = Room.GetPlayers().ToString();
            m_hKVDictionary["maxplayers"] = Room.GetMaxPlayers().ToString();
            m_bIsInfected = !Room.IsTagging();
            m_hKVDictionary["tagged"] = m_bIsInfected ? m_hCfgInfectedText.Value : m_hCfgNotInfectedText.Value;
            m_hKVDictionary["taggedme"] = m_cfgTaggedOnJoinText.Value;
            if (m_hCfgUpdateTimeOnRoomJoin.Value == true)
            {
                m_hActivity.Timestamps.Start = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();
            }
            UpdateActivity();
        }
        private void OnRoomDisconnected(object sender, EventArgs e)
        {
            m_hKVDictionary["code"] = "";
            m_hActivity.Assets.LargeImage = "lobby";
            m_hKVDictionary["mapname"] = "Lobby";
            m_bIsInfected = false;
            m_hKVDictionary["tagged"] = m_hCfgNotInfectedText.Value;
            m_nPlayersTaggedByMe = 0;
            m_hKVDictionary["taggedbyme"] = "0";
            if (m_hCfgShowSmallImage.Value) m_hActivity.Assets.SmallImage = m_hCfgNotInfectedImage.Value;
            UpdateActivity();
        }
        private void OnMyNicknameChange(object sender, PlayerNicknameArgs e)
        {
            m_hKVDictionary["nickname"] = e.newNickName;
            UpdateActivity();
        }
        private void OnPlayerCountChange(object sender, EventArgs e)
        {
            int nTagged = Players.CountInfectedPlayers();
            m_hKVDictionary["infected"] = m_bIsInfected.ToString();
            m_hKVDictionary["notinfected"] = (Room.GetPlayers() - nTagged).ToString();
            m_hKVDictionary["players"] = Room.GetPlayers().ToString();
            UpdateActivity();
        }
        private void OnTaggedSomeone(object sender, PlayerTaggedPlayerArgs e)
        {
            if (e.victim == Photon.Pun.PhotonNetwork.LocalPlayer)
            {
                m_bIsInfected = true;
                m_hKVDictionary["taggedme"] = e.tagger.NickName;
            }
            else if (e.isTagging)
            {
                m_bIsInfected = false;
            }
            else if (e.tagger == Photon.Pun.PhotonNetwork.LocalPlayer)
            {
                ++m_nPlayersTaggedByMe;
                m_hKVDictionary["taggedbyme"] = m_nPlayersTaggedByMe.ToString();
            }
            m_hKVDictionary["tagged"] = m_hCfgInfectedText.Value;
            int nTagged = Players.CountInfectedPlayers();
            m_hKVDictionary["infected"] = nTagged.ToString();
            m_hKVDictionary["notinfected"] = (Room.GetPlayers() -  nTagged).ToString();
            UpdateActivity();
        }
        private void OnRoundStart(object sender, OnRoundStartArgs e)
        {
            m_bIsInfected = (e.player == Photon.Pun.PhotonNetwork.LocalPlayer);
            m_nPlayersTaggedByMe = 0;
            m_hKVDictionary["taggedbyme"] = "0";
            m_hKVDictionary["taggedme"] = "0";
            m_hKVDictionary["tagged"] = m_bIsInfected ? m_hCfgInfectedText.Value : m_hCfgNotInfectedText.Value;
            m_hKVDictionary["taggedme"] = m_cfgTaggedOnRoundStartText.Value;
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