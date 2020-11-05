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

    /// <summary>Indicator automat.</summary>
    class Ind
    {
        /// <summary>Current variant.</summary>
        byte CV;
        /// <summary>Indicator variants.</summary>
        string[] Vrnts;
        /// <summary>Direction iterator.</summary>
        byte i;
        /// <summary>Method to update indicator.</summary>
        /// <param name="cv">Current variant.</param>
        /// <param name="v">Indicator variants.</param>
        /// <param name="i">Direction iterator.</param>
        public delegate void UpdateMethod(ref byte cv, string[] v, ref byte i);
        UpdateMethod _UpdateMethod;
        
        /// <param name="um">Method to update indicator.</param>
        /// <param name="v">Variants.</param>
        public Ind(UpdateMethod um, params string[] v)
        {
            CV = 0;
            _UpdateMethod = um;
            Vrnts = v;
            i = 1;
        }

        /// <summary>Update the indicator.</summary>
        public void Update()
        {
            _UpdateMethod(ref CV, Vrnts, ref i);
        }
        /// <summary>Update in direction and reverse.</summary>
        public static void UpdateTurn(ref byte cv, string[] v, ref byte i)
        {
            cv += i;
            if (cv >= v.Length - 1 || cv <= 0)
                i = (byte)-i;
        }
        /// <summary> Update in direction and repeat.</summary>
        public static void UpdateRepeat(ref byte cv, string[] v, ref byte i)
        {
            if (cv >= v.Length - 1)
                cv = 0;
            else
                cv += i;
        }

        /// <summary> Get current indicator.</summary>
        public string Get()
        {
            return Vrnts[CV];
        }
    }

    //======-SCRIPT ENDING-======
}