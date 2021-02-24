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
    //======-SCRIPT BEGINNING-======
    
    public class Info
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    /// <summary>Basic subprogram class.</summary>
    abstract class SubP
    {
        public Info I { get; private set; }
        public MyVersion V { get; }

        public SubP(string name, MyVersion v = null, string description = "Description " + NA + ".")
        {
            I.Name = name;
            V = v;
            I.Description = description;
        }
        public SubP(string name, string description)
        {
            I.Name = name;
            V = null;
            I.Description = description;
        }
    }

    //======-SCRIPT ENDING-======
}