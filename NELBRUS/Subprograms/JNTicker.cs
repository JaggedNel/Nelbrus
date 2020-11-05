using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using VRageMath;
using VRage.Game;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using VRage.Collections;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Linq;
using VRage.Game.ModAPI.Ingame.Utilities;
using System.Text.RegularExpressions;

public partial class Program : MyGridProgram
{
    //======-SUBPROGRAM BEGINING-======

    JNTicker iJNTicker = new JNTicker();
    class JNTicker : InitSubP
    {
        public JNTicker() : base("Ticker", "First subprogram for NELBRUS system. This subprogram takes LCD with name \"Ticker\" and show current tick on it.") { } // Used for initialisation of subprogram

        public override SdSubP Start(ushort id) { return new TP(id, this); }

        class TP /* This Program */ : SdSubPCmd
        {
            IMyTextPanel LCD { get; }
            CAct Act = new CAct(); // Show current tick on text panel

            public TP(ushort id, SubP p) : base(id, p)
            {
                if ((LCD = OS.P.GridTerminalSystem.GetBlockWithName("LCD") as IMyTextPanel) == null)
                {
                    Terminate("\"LCD\" not found.");
                    return;
                }
                AddAct(ref Act, Show, 20);
                SetCmd(new Dictionary<string, Cmd>
                {
                    { "pause", new Cmd(CmdPause, "Pause show current tick.") },
                    { "play", new Cmd(CmdPlay, "Continue show current tick.") },
                });
            }

            void Show()
            {
                LCD.WriteText(OS.Tick.ToString());
                //Act = ChaAct(Act, 50, false); // Example
            }
            void MePause() { RemAct(ref Act); }
            void MePlay() { if (Act.ID == 0) AddAct(ref Act, Show, 20); }

            #region Commands
            string CmdPause(List<string> a) { MePause(); return null; }
            string CmdPlay(List<string> a) { MePlay(); return null; }
            #endregion Commands
        }
    }

    //======-SUBPROGRAM ENDING-======
}