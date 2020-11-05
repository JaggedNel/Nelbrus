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
    sealed partial class NLB : SdSubPCmd
    {
        //======-SCRIPT BEGINING-======

        /// <summary> Basic echo controller class. </summary>
        public abstract class EchoController : SdSubP
        {
            /// <summary>Show duration of the custom information.</summary>
            public uint DT { get; set; }

            public EchoController(string n, MyVersion v = null, string i = NA) : base(1, n, v, i) { }

            /// <summary>Echo controller works with NELBRUS. Do not stop it.</summary>
            public override bool MayStop() { return false; }
            /// <summary>Refresh information at echo.</summary>
            public virtual void Refresh()
            {
                OS.P.Echo("OS NELBRUS is working. And used echo controller too but not configured.");
            }
            /// <summary>Show custom info at echo.</summary>
            public virtual void CShow(string s)
            {
                OS.P.Echo(s);
            }
            /// <summary>Remove custom info in echo.</summary>
            public virtual void CClr()
            {
                Refresh();
            }
        }

        //======-SCRIPT ENDING-======
    }


}