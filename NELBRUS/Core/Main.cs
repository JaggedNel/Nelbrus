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

public partial class Program : MyGridProgram {
    //======-SCRIPT BEGINING-======
    
    #region Core zone
    // Nelbrus OS v.0.5.0-[05.11.20]
    
    /// <summary>Operation System NELBRUS instance.</summary>
    readonly static NLB OS = new NLB(); // Initializing OS

    Program()
    {
        OS.Ready(this);
        OS.Go();
    }
    void Save()
    {

    }
    void Main(string arg, UpdateType uT)
    {
        OS.Main(arg, uT);
    }

    #region Global properties
    /// <summary>Not available message.</summary>
    public const string NA = "N/A";
    /// <summary>Help message.</summary>
    public const string mTUH = "Try use /help to fix your problem.";
    /// <summary>Argument exception message.</summary>
    public const string mAE = "Argument exception. " + mTUH;

    /// <summary>Action.</summary>
    delegate void Act();
    /// <summary>Request without arguments.</summary>
    delegate string Req();
    /// <summary>String request with string arguments used for commands.</summary>
    /// <returns>Answer of the executed command.</returns>
    delegate string ReqA(List<string> a);
    /// <summary>Integer request without arguments.</summary>
    delegate int ReqI();

    /// #INSERT Cmd
    /// #INSERT MyVersion
    /// #INSERT SubP
    /// #INSERT InitSubP
    /// #INSERT SdSubP
    /// #INSERT SdSubPCmd

    #endregion Global properties

    /// #INSERT NLB

    /// #INSERT Ind
    #endregion Core zone

    //======-SCRIPT ENDING-======
}
