using System;
using BepInEx; // requires BepInEx.dll and BepInEx.Harmony.dll
using UnboundLib; // requires UnboundLib.dll
using UnityEngine;
using HarmonyLib; // requires 0Harmony.dll
using System.Collections;
using System.Reflection;
// requires Assembly-CSharp.dll
// requires MMHOOK-Assembly-CSharp.dll

namespace PCE
{
    [BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin("pykess.rounds.plugins.continue", "Continue", "0.0.0.0")]
    [BepInProcess("Rounds.exe")]
    public class PCE : BaseUnityPlugin
    {
        private void Awake()
        {
            new Harmony("pykess.rounds.plugins.continue").PatchAll();
        }
        private void Start()
        {

        }
        private const string ModId = "pykess.rounds.plugins.continue";

        private const string ModName = "Continue";
    }


    [Serializable]
    [HarmonyPatch(typeof(GM_ArmsRace), "GameOver")]
    class GM_ArmsRacePatchRoundTransition : MonoBehaviour
    {
        private static bool Prefix(GM_ArmsRace __instance, int winningTeamID)
        {
            Traverse.Create(__instance).Field("currentWinningTeamID").SetValue(winningTeamID);

            __instance.StartCoroutine(GameOverTransition(__instance, winningTeamID));
            
            return false;

        }
        private static IEnumerator GameOverTransition(GM_ArmsRace GMinstance, int winningTeamID)
        {
            UIHandler.instance.ShowRoundCounterSmall(GMinstance.p1Rounds, GMinstance.p2Rounds, GMinstance.p1Points, GMinstance.p2Points);
            UIHandler.instance.DisplayScreenText(PlayerManager.instance.GetColorFromTeam(winningTeamID).winText, "VICTORY!", 1f);
            yield return new WaitForSecondsRealtime(2f);
            // ask for continue
            typeof(GM_ArmsRace).InvokeMember("GameOverContinue", BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic, null, GMinstance, new object[] { winningTeamID });
            // then ask for rematch
            //typeof(GM_ArmsRace).InvokeMember("GameOverRematch", BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic, null, GMinstance, new object[] { winningTeamID });
            yield break;
        }

    }
}