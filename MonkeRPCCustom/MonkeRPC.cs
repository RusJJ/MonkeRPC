using System;
using BepInEx;

namespace MonkeRPCMapLoader
{
    /* That's me! */
    [BepInPlugin("net.rusjj.gorillatag.monkerpc.maploader", "Monke RPC (MapLoader)", "1.1.0")]
    /* MonkeRPC: SetCustomMapForRPC */
    [BepInDependency("net.rusjj.gorillatag.monkerpc", "1.1.0")]
    /* MapLoader: Custom map detecting */
    [BepInDependency("vadix.gorillatag.maploader", "1.0.0")]

    public class MonkeRPCMapLoader : BaseUnityPlugin
    {
        public void Start()
        {
            MonkeRPC.MonkeRPC.m_evPreDiscordRPC += OnPreDiscordRPC;
        }
        private static void OnPreDiscordRPC(object sender, EventArgs e)
        {
            MonkeRPC.MonkeRPC.SetCustomMapForRPC(VmodMonkeMapLoader.Behaviours.GetMapName(), VmodMonkeMapLoader.Behaviours.GetMapFileName());
        }
    }
}