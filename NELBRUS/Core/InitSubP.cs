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

    /// <summary>
    /// Basic class for subprograms initilizer
    /// </summary>
    class InitSubP : SubP
    {
        public InitSubP(string name, MyVersion v = null, string info = "Description " + NA + ".") : base(name, v, info)
        {
            OS.ISP(this);
        }   
        public InitSubP(string name, string info) : this(name, null, info) { }

        /// <summary>Run new subprogram.</summary>
        /// <param name="id">Identificator of new subprogram.</param>
        /// <returns>Started subprogram.</returns>
        public virtual SdSubP Start() { return null; }
    }

    //======-SCRIPT ENDING-======
}