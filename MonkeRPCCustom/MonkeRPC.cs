using BepInEx;
using VmodMonkeMapLoader.Behaviours;

namespace MonkeRPCMapLoader
{
    /* That's me! */
    [BepInPlugin("net.rusjj.gorillatag.monkerpc.maploader", "Monke RPC (MapLoader)", "1.1.0")]
    /* MonkeRPC: SetCustomMapForRPC */
    [BepInDependency("net.rusjj.gorillatag.monkerpc", "1.1.0")]
    /* MapLoader: Custom map detecting */
    [BepInDependency("vadix.gorillatag.maploader", "1.1.0")]

    public class MonkeRPCMapLoader : BaseUnityPlugin
    {
        public void Start()
        {
            VmodMonkeMapLoader.Events.OnMapEnter += OnCustomMapEntered;
        }
        private static void OnCustomMapEntered(bool isJoined)
        {
            if (isJoined)
            {
                MonkeRPC.MonkeRPC.SetCustomMapForRPC(VmodMonkeMapLoader.Events.MapName, VmodMonkeMapLoader.Events.MapFileName);
            }
            else
            {
                MonkeRPC.MonkeRPC.SetCustomMapForRPC(null, null);
            }
        }
    }
}