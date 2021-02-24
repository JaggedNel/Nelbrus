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

    /// <summary>Command.</summary>
    struct Cmd
    {
        /// <summary>Command request.</summary>
        public ReqA C { get; }
        /// <summary>Help info.</summary>
        public string H { get; set; }
        /// <summary>Details.</summary>
        public string D { get; set; }

        /// <param name="c">Command request.</param>
        /// <param name="h">Help info.</param>
        /// <param name="d">Details.</param>
        public Cmd(ReqA c, string h, string d) { C = c; H = h; D = d; }
        /// <param name="c">Command request.</param>
        /// <param name="h">Help info.</param>
        public Cmd(ReqA c, string h) : this(c, h, NA) { }
        /// <param name="c">Command request.</param>
        public Cmd(ReqA c) : this(c, NA) { }
    }

    //======-SCRIPT ENDING-======
}