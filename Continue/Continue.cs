using System;
using BepInEx; // requires BepInEx.dll and BepInEx.Harmony.dll
using UnboundLib; // requires UnboundLib.dll
using UnityEngine;
using HarmonyLib; // requires 0Harmony.dll
using System.Collections;
using System.Reflection;
using Photon.Pun;
// requires Assembly-CSharp.dll
// requires MMHOOK-Assembly-CSharp.dll

namespace Continue
{
    [BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin("pykess.rounds.plugins.continue", "Continue", "0.1.0.0")]
    [BepInProcess("Rounds.exe")]
    public class Continue : BaseUnityPlugin
    {
        private void Awake()
        {
            new Harmony("pykess.rounds.plugins.continue").PatchAll();
            NetworkingManager.RegisterEvent("pykess.rounds.plugins.continue_Sync", delegate (object[] sync)
            {
                Continue.extraRounds = (string)sync[0];
            });
        }
        private void Start()
        {
            Unbound.RegisterGUI("Rounds After Continue", new Action(this.DrawGUI));
            Unbound.RegisterHandshake("pykess.rounds.plugins.continue", new Action(this.OnHandShakeCompleted));
        }
        private void DrawGUI()
        {
            string text = GUILayout.TextField(Continue.extraRounds, 2, Array.Empty<GUILayoutOption>());
            if (text != Continue.extraRounds && PhotonNetwork.IsMasterClient)
            {
                NetworkingManager.RaiseEvent("pykess.rounds.plugins.continue_Sync", new object[]
                {
                    text
                });
            }
            Continue.extraRounds = text;
        }
        private void OnHandShakeCompleted()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                NetworkingManager.RaiseEvent("pykess.rounds.plugins.continue_Sync", new object[]
                {
                    Continue.extraRounds
                });
            }
        }

        private const string ModId = "pykess.rounds.plugins.continue";

        private const string ModName = "Continue";

        public static string extraRounds = "2";

        private struct NetworkEventType
        {
            public const string SyncContinue = "pykess.rounds.plugins.continue_Sync";
        }
    }


    [Serializable]
    [HarmonyPatch(typeof(GM_ArmsRace), "GameOver")]
    class GM_ArmsRacePatchRoundTransition
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
            GameOverContinue(GMinstance, winningTeamID);

            yield break;
        }
        private static void GameOverContinue(GM_ArmsRace GMinstance, int winningTeamID)
        {
            UIHandler.instance.DisplayScreenTextLoop(PlayerManager.instance.GetColorFromTeam(winningTeamID).winText, "CONTINUE?");
            Player firstplayer = (Player)typeof(PlayerManager).InvokeMember("GetFirstPlayerInTeam", BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic, null, PlayerManager.instance, new object[] { winningTeamID });
            Action<PopUpHandler.YesNo> GetContinueYesNo = createGetContinueYesNoForInstance(GMinstance, winningTeamID);
            typeof(UIHandler).InvokeMember("DisplayYesNoLoop", BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic, null, UIHandler.instance, new object[] { firstplayer, new Action<PopUpHandler.YesNo>(GetContinueYesNo)});
            MapManager.instance.LoadNextLevel(false, false);
        }
        private static Action<PopUpHandler.YesNo> createGetContinueYesNoForInstance(GM_ArmsRace GMinstance, int winningTeamID)
        {
            Action<PopUpHandler.YesNo> ContinueYesNoFromInstance = delegate (PopUpHandler.YesNo yesNo)
            {
                if (yesNo == PopUpHandler.YesNo.Yes)
                {
                    //typeof(GM_ArmsRace).InvokeMember("DoContinue", BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic, null, GMinstance, new object[] { });
                    DoContinue(GMinstance);
                    return;
                }
                else
                {
                    // ask for rematch instead
                    Unbound.Instance.ExecuteAfterSeconds(1f, delegate
                    {
                        typeof(GM_ArmsRace).InvokeMember("GameOverRematch", BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic, null, GMinstance, new object[] { winningTeamID });
                    });
                    return;
                }
            };

            return ContinueYesNoFromInstance;
        }
        private static void DoContinue(GM_ArmsRace GMinstance)
        {
            UIHandler.instance.StopScreenTextLoop();
            if (int.TryParse(Continue.extraRounds, out GM_ArmsRacePatchRoundTransition.roundsToAdd) && GM_ArmsRacePatchRoundTransition.roundsToAdd > 0)
            {
                GMinstance.roundsToWinGame += GM_ArmsRacePatchRoundTransition.roundsToAdd;
            }
            else
            {
                GMinstance.roundsToWinGame += 2;
            }
            typeof(UIHandler).InvokeMember("SetNumberOfRounds", BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic, null, UIHandler.instance, new object[] { GMinstance.roundsToWinGame });
            int currentWinningTeamID = (int)Traverse.Create(GMinstance).Field("currentWinningTeamID").GetValue();
            typeof(GM_ArmsRace).InvokeMember("RoundOver", BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic, null, GMinstance, new object[] { currentWinningTeamID, PlayerManager.instance.GetOtherTeam(currentWinningTeamID) });

        }

        public static int roundsToAdd;

    }
}