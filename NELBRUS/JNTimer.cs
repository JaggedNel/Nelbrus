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

    class JNTimer : SubP
    {
        public JNTimer() : base("Timer", "Shows the elapsed time on \"LCD timer\" when using the command \"ss\".") { }

        public override SdSubP Start(ushort id) { return new TP(id, this); }

        class TP : SdSubPCmd
        {
            IMyTextPanel LCD;
            uint start;
            bool s = false;
            CAct S = new CAct();

            public TP(ushort id, SubP p) : base(id, p)
            {
                if ((LCD = OS.P.GridTerminalSystem.GetBlockWithName("LCD Timer") as IMyTextPanel) == null)
                {
                    Terminate("\"LCD Timer\" not found.");
                    return;
                }
                SetCmd(new Dictionary<string, Cmd>
                {
                    { "ss", new Cmd(CmdSS, "Start/stop timer.") }
                });
            }

            void Show()
            {
                LCD.WriteText((OS.Tick - start).ToString());
            }

            #region Commands
            string CmdSS(List<string> a)
            {
                if (s)
                {
                    LCD.WriteText(NLB.F.TTT(OS.Tick - start));
                    RemAct(ref S);
                }
                else
                {
                    start = OS.Tick;
                    AddAct(ref S, Show, 10);
                }
                s = !s;
                return "";
            }
            #endregion Commands
        }
    }

    //======-SUBPROGRAM ENDING-======
}