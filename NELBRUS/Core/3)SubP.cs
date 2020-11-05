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
    //======-SCRIPT BEGINING-======
    
    /// <summary>Basic subprogram class.</summary>
    abstract class SubP
    {
        public string Name { get; protected set; }
        public MyVersion V { get; }
        public string Info { get; protected set; }

        public SubP(string name, MyVersion v = null, string info = "Description " + NA + ".")
        {
            Name = name;
            V = v;
            Info = info;
        }
        public SubP(string name, string info)
        {
            Name = name;
            V = null;
            Info = info;
        }
    }

    //======-SCRIPT ENDING-======
}