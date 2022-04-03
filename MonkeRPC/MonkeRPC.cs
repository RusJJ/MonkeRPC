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
    [BepInPlugin("net.rusjj.gorillatag.monkerpc", "Monke RPC", "1.4.1")]
    /* BananaHook: Our little API */
    [BepInDependency("net.rusjj.gtlib.bananahook", "1.3.0")]

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
        private static ConfigEntry<string> m_hCfgHuntedText;
        private static ConfigEntry<string> m_hCfgNotInfectedText;
        private static ConfigEntry<string> m_hCfgInfectedImage;
        private static ConfigEntry<string> m_hCfgHuntedImage;
        private static ConfigEntry<string> m_hCfgNotInfectedImage;

        private static ConfigEntry<string> m_cfgLargeImageText;
        private static ConfigEntry<string> m_cfgSmallImageText;
        private static ConfigEntry<string> m_cfgSmallImageHuntText;
        private static ConfigEntry<string> m_cfgState_Lobby;
        private static ConfigEntry<string> m_cfgState_PrivateRoom;
        private static ConfigEntry<string> m_cfgState_PublicRoom;
        private static ConfigEntry<string> m_cfgState_SlowingPrivateRoom;
        private static ConfigEntry<string> m_cfgState_SlowingPublicRoom;
        private static ConfigEntry<string> m_cfgState_TagPrivateRoom;
        private static ConfigEntry<string> m_cfgState_TagPublicRoom;
        private static ConfigEntry<string> m_cfgState_HuntPrivateRoom;
        private static ConfigEntry<string> m_cfgState_HuntPublicRoom;
        private static ConfigEntry<string> m_cfgDetails_Lobby;
        private static ConfigEntry<string> m_cfgDetails_PrivateRoom;
        private static ConfigEntry<string> m_cfgDetails_PublicRoom;
        private static ConfigEntry<string> m_cfgDetails_SlowingPrivateRoom;
        private static ConfigEntry<string> m_cfgDetails_SlowingPublicRoom;
        private static ConfigEntry<string> m_cfgDetails_TagPrivateRoom;
        private static ConfigEntry<string> m_cfgDetails_TagPublicRoom;
        private static ConfigEntry<string> m_cfgDetails_HuntPrivateRoom;
        private static ConfigEntry<string> m_cfgDetails_HuntPublicRoom;

        private static ConfigEntry<string> m_cfgTaggedOnJoinText;
        private static ConfigEntry<string> m_cfgTaggedOnRoundStartText;

        /* My OWN variables! */
        private static Dictionary<string, string> m_hKVDictionary = new Dictionary<string, string>()
        {
            { "nickname", "" },
            { "mapname", "" },
            { "queue", "Default" },
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
            { "myhunter", "unknown" },
            { "mytarget", "unknown" },
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
            if (m_hActivityManager != null && m_hCfgIsRPCEnabled.Value)
            {
                m_hActivity.Assets.LargeText = RegexReplace(m_cfgLargeImageText.Value);
                m_hActivity.Assets.SmallText = RegexReplace(Room.m_eCurrentGamemode == eRoomGamemode.Hunt ? m_cfgSmallImageHuntText.Value : m_cfgSmallImageText.Value);
                if (Room.m_szRoomCode == null) // Lobby
                {
                    m_hActivity.Details = RegexReplace(m_cfgDetails_Lobby.Value);
                    m_hActivity.State = RegexReplace(m_cfgState_Lobby.Value);
                }
                else // Playing
                {
                    if (m_bIsInPrivateLobby) // Ugh... Private lobby.
                    {
                        if(BananaHook.Utils.Room.m_eCurrentGamemode == eRoomGamemode.Hunt)
                        {
                            m_hActivity.Details = RegexReplace(!m_bIsInfected ? m_cfgDetails_HuntPrivateRoom.Value : m_cfgDetails_SlowingPrivateRoom.Value);
                            m_hActivity.State = RegexReplace(!m_bIsInfected ? m_cfgState_HuntPrivateRoom.Value : m_cfgState_SlowingPrivateRoom.Value);
                        }
                        else
                        {
                            m_hActivity.Details = RegexReplace(m_bIsInfected ? m_cfgDetails_TagPrivateRoom.Value : m_cfgDetails_PrivateRoom.Value);
                            m_hActivity.State = RegexReplace(m_bIsInfected ? m_cfgState_TagPrivateRoom.Value : m_cfgState_PrivateRoom.Value);
                        }
                    }
                    else // Public lobby! F-U-N !!!
                    {
                        if (BananaHook.Utils.Room.m_eCurrentGamemode == eRoomGamemode.Hunt)
                        {
                            m_hActivity.Details = RegexReplace(!m_bIsInfected ? m_cfgDetails_HuntPublicRoom.Value : m_cfgDetails_SlowingPublicRoom.Value);
                            m_hActivity.State = RegexReplace(!m_bIsInfected ? m_cfgState_HuntPublicRoom.Value : m_cfgState_SlowingPublicRoom.Value);
                        }
                        else
                        {
                            m_hActivity.Details = RegexReplace(m_bIsInfected ? m_cfgDetails_TagPublicRoom.Value : m_cfgDetails_PublicRoom.Value);
                            m_hActivity.State = RegexReplace(m_bIsInfected ? m_cfgState_TagPublicRoom.Value : m_cfgState_PublicRoom.Value);
                        }
                    }
                    if(m_hCfgShowSmallImage.Value && m_hCfgSmallImageShowTagged.Value)
                    {
                        m_hActivity.Assets.SmallImage = m_bIsInfected ? (BananaHook.Utils.Room.m_eCurrentGamemode == eRoomGamemode.Hunt ? m_hCfgHuntedImage.Value : m_hCfgInfectedImage.Value) : m_hCfgNotInfectedImage.Value;
                    }
                }
                try
                {
                    m_hActivityManager.UpdateActivity(m_hActivity, result => {});
                }
                catch (Exception e) { UnityEngine.Debug.LogError("Discord::UpdateActivity throws a " + e.GetType() + ":\n" + e.Message); }
                
            }
        }
        private static void DiscordGo()
        {
            /* Give some time for game to initialize */
            Thread.Sleep(5000);
            
            m_hDiscord = new Discord.Discord(837692600189190174, (UInt64)CreateFlags.NoRequireDiscord);
            m_hActivityManager = m_hDiscord.GetActivityManager();
            if (m_hActivityManager == null) return;
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
            Events.OnRoundEndPost +=            OnRoundEnd;

            var hCfgFile =                      new ConfigFile(Path.Combine(Paths.ConfigPath, "MonkeRPC.cfg"), true);
            m_hCfgIsRPCEnabled =                hCfgFile.Bind("CFG", "IsRPCEnabled", true, "Should Discord RPC be enabled?");
            m_hCfgUpdateTimeOnRoomJoin =        hCfgFile.Bind("CFG", "UpdateTimeOnRoomJoin", true, "Update time when joining a room?");
            m_hCfgShowSmallImage =              hCfgFile.Bind("CFG", "ShowSmallImage", true, "Should Discord show a little image in a bottom-right corner?");
            m_hCfgSmallImageShowTagged =        hCfgFile.Bind("CFG", "SmallImageShowTagged", true, "Should Discord show an other little image if you're tagged?");
            m_hCfgInfectedText =                hCfgFile.Bind("CFG", "InfectedText", "Hunting", "A text for {tagged} code when you're infected (tagged)");
            m_hCfgHuntedText =                  hCfgFile.Bind("CFG", "HuntedText", "Slowing down", "A text for {tagged} code when you're hunted");
            m_hCfgNotInfectedText =             hCfgFile.Bind("CFG", "NotInfectedText", "Surviving", "A text for {tagged} code when you're NOT infected (tagged)");
            
            m_hCfgInfectedImage =               hCfgFile.Bind("CustomRPC", "InfectedImage", "gorillatag_lavaskin", "A text for {tagged} code when you're infected (tagged)");
            m_hCfgHuntedImage =                 hCfgFile.Bind("CustomRPC", "HuntedImage", "gorillatag_waterskin", "A text for {tagged} code when you're hunted");
            m_hCfgNotInfectedImage =            hCfgFile.Bind("CustomRPC", "NotInfectedImage", "gorillatag_gorilla", "A text for {tagged} code when you're NOT infected (tagged)");

            m_cfgLargeImageText =               hCfgFile.Bind("CustomRPC", "LargeImageText", "{mapname}", "A text when you're hovering a mouse over large image");
            m_cfgSmallImageText =               hCfgFile.Bind("CustomRPC", "SmallImageText", "{nickname} | {tagged}", "A text when you're hovering a mouse over small image");
            m_cfgSmallImageHuntText =           hCfgFile.Bind("CustomRPC", "SmallImageHuntText", "{nickname} | {tagged} | Target: {mytarget}", "A text when you're hovering a mouse over small image");

            m_cfgState_Lobby =                  hCfgFile.Bind("CustomRPC", "State_Lobby", "Not joined", "A state in RPC when you're not joined to any room");
            m_cfgState_PrivateRoom =            hCfgFile.Bind("CustomRPC", "State_PrivateRoom", "{mode} ({players}/{maxplayers})", "A state in RPC when you're in a private room");
            m_cfgState_PublicRoom =             hCfgFile.Bind("CustomRPC", "State_PublicRoom", "{mode} ({players}/{maxplayers})", "A state in RPC when you're in a public room");
            m_cfgState_SlowingPrivateRoom =     hCfgFile.Bind("CustomRPC", "State_SlowingPrivateRoom", "{mode} ({players}/{maxplayers}). {taggedme} got me!", "A state in RPC when you're in a private room AND TAGGED (you're water monkey)");
            m_cfgState_SlowingPublicRoom =      hCfgFile.Bind("CustomRPC", "State_SlowingPublicRoom", "{mode} ({players}/{maxplayers}). {taggedme} got me!", "A state in RPC when you're in a public room AND TAGGED (you're water monkey)");
            m_cfgState_TagPrivateRoom =         hCfgFile.Bind("CustomRPC", "State_TagPrivateRoom", "{mode} ({players}/{maxplayers}). I tagged {taggedbyme}!", "A state in RPC when you're tagged in a private room");
            m_cfgState_TagPublicRoom =          hCfgFile.Bind("CustomRPC", "State_TagPublicRoom", "{mode} ({players}/{maxplayers}). I tagged {taggedbyme}!", "A state in RPC when you're tagged in a public room");
            m_cfgState_HuntPrivateRoom =        hCfgFile.Bind("CustomRPC", "State_HuntPrivateRoom", "{mode} ({players}/{maxplayers}). I hunted {taggedbyme}!", "A state in RPC when you're ALIVE in a private hunting room");
            m_cfgState_HuntPublicRoom =         hCfgFile.Bind("CustomRPC", "State_HuntPublicRoom", "{mode} ({players}/{maxplayers}). I hunted {taggedbyme}!", "A state in RPC when you're ALIVE in a public hunting room");

            m_cfgDetails_Lobby =                hCfgFile.Bind("CustomRPC", "Details_Lobby", "Explores a dead tree", "A details in RPC when you're not joined to any room");
            m_cfgDetails_PrivateRoom =          hCfgFile.Bind("CustomRPC", "Details_PrivateRoom", "Playing (Room)", "A details in RPC when you're in a private room");
            m_cfgDetails_PublicRoom =           hCfgFile.Bind("CustomRPC", "Details_PublicRoom", "Playing ({code})", "A details in RPC when you're in a public room");
            m_cfgDetails_SlowingPrivateRoom =   hCfgFile.Bind("CustomRPC", "Details_SlowingPrivateRoom", "Playing (Room)", "A details in RPC when you're in a private room AND TAGGED (you're water monkey)");
            m_cfgDetails_SlowingPublicRoom =    hCfgFile.Bind("CustomRPC", "Details_SlowingPublicRoom", "Playing ({code})", "A details in RPC when you're in a public room AND TAGGED (you're water monkey)");
            m_cfgDetails_TagPrivateRoom =       hCfgFile.Bind("CustomRPC", "Details_TagPrivateRoom", "Tagged by {taggedme} (Room)", "A details in RPC when you're tagged in a private room");
            m_cfgDetails_TagPublicRoom =        hCfgFile.Bind("CustomRPC", "Details_TagPublicRoom", "Tagged by {taggedme} ({code})", "A details in RPC when you're tagged in a public room");
            m_cfgDetails_HuntPrivateRoom =      hCfgFile.Bind("CustomRPC", "Details_HuntPrivateRoom", "Hunted by {taggedme} (Room)", "A details in RPC when you're hunted in a private room");
            m_cfgDetails_HuntPublicRoom =       hCfgFile.Bind("CustomRPC", "Details_HuntPublicRoom", "Hunted by {taggedme} ({code})", "A details in RPC when you're hunted in a public room");

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
            m_hKVDictionary["queue"] = Room.m_eCurrentLobbyMode.ToString();
            m_hKVDictionary["mode"] = Room.m_eCurrentGamemode.ToString();
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
                        m_hActivity.Assets.LargeImage = "gorillatag_shop";
                        break;

                    case eJoinedMap.Mountain:
                        m_hKVDictionary["mapname"] = "Mountain";
                        m_hActivity.Assets.LargeImage = "gorillatag_mountain";
                        break;

                    default:
                        m_hKVDictionary["mapname"] = "Forest";
                        m_hActivity.Assets.LargeImage = "gorillatag_forest";
                        break;
                }
            }
            m_hKVDictionary["players"] = Room.GetPlayers().ToString();
            m_hKVDictionary["maxplayers"] = Room.GetMaxPlayers().ToString();
            m_bIsInfected = Players.IsInfected(Photon.Pun.PhotonNetwork.LocalPlayer);
            m_hKVDictionary["tagged"] = m_bIsInfected ? (Room.m_eCurrentGamemode == eRoomGamemode.Hunt ? m_hCfgHuntedText.Value : m_hCfgInfectedText.Value) : m_hCfgNotInfectedText.Value;
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
            if(Room.m_eCurrentGamemode == eRoomGamemode.Hunt)
            {
                if (e.victim == Photon.Pun.PhotonNetwork.LocalPlayer)
                {
                    m_bIsInfected = true;
                    m_hKVDictionary["taggedme"] = e.tagger.NickName;
                }
                else if (e.tagger == Photon.Pun.PhotonNetwork.LocalPlayer)
                {
                    ++m_nPlayersTaggedByMe;
                    m_hKVDictionary["taggedbyme"] = m_nPlayersTaggedByMe.ToString();
                    m_hKVDictionary["mytarget"] = Players.GetTargetOf(e.victim).NickName; // For Hunt gamemode this is "before tagged" so we need to get who's target of our victim!
                }
            }
            else
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
            m_hKVDictionary["tagged"] = m_bIsInfected ? (BananaHook.Utils.Room.m_eCurrentGamemode == eRoomGamemode.Hunt ? m_hCfgHuntedText.Value : m_hCfgInfectedText.Value) : m_hCfgNotInfectedText.Value;
            m_hKVDictionary["taggedme"] = m_cfgTaggedOnRoundStartText.Value;
            UpdateActivity();
        }
        private void OnRoundEnd(object sender, EventArgs e)
        {
            m_nPlayersTaggedByMe = 0;
            m_hKVDictionary["taggedbyme"] = "0";
            m_hKVDictionary["taggedme"] = "0";
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