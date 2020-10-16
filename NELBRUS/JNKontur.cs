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

    class JNKontur : SubP
    {
        public JNKontur() : base("Controller for KONTUR") { }

        public override SdSubP Start(ushort id) { return OS.CSP<TP>() ? null : new TP(id, this); }

        class TP : SdSubPCmd
        {
            bool loopturn;
            float NormWhAng, Tangle;

            float carspeed;
            float strengthSuspModifer, heightSuspModifer;
            bool switchingsusp;

            SuspensionModes SuspensionMode;
            DriveModes DriveMode;

            string FirstStr, SecondStr, LCDriveMode, LCDSuspMode;

            List<IMyMotorSuspension> Wheels;
            IMyMotorSuspension WheelLF, WheelRF, WheelLB, WheelRB;

            IMyRemoteControl RemoteDriver;
            IMyMotorStator RotorRull;
            IMyTextPanel DriverLCD;

            enum DriveModes : byte { full, front, rear }
            enum SuspensionModes : byte { sport, offroad }

            CAct MA = new CAct();

            public TP(ushort id, SubP p) : base(id, p)
            {
                loopturn = false;
                NormWhAng = 35; Tangle = 0;
                switchingsusp = false;
                SuspensionMode = SuspensionModes.sport; DriveMode = DriveModes.full;
                FirstStr = ""; SecondStr = "";
                Wheels = new List<IMyMotorSuspension>();

                if (
                    (DriverLCD = OS.GTS.GetBlockWithName("Screen Driver") as IMyTextPanel) == null ||
                    (WheelLF = OS.GTS.GetBlockWithName("Wheel Suspension 3x3 LF") as IMyMotorSuspension) == null ||
                    (WheelRF = OS.GTS.GetBlockWithName("Wheel Suspension 3x3 RF") as IMyMotorSuspension) == null ||
                    (WheelLB = OS.GTS.GetBlockWithName("Wheel Suspension 3x3 LB") as IMyMotorSuspension) == null ||
                    (WheelRB = OS.GTS.GetBlockWithName("Wheel Suspension 3x3 RB") as IMyMotorSuspension) == null ||
                    (RemoteDriver = OS.GTS.GetBlockWithName("Control car") as IMyRemoteControl) == null ||
                    (RotorRull = OS.GTS.GetBlockWithName("Rotor rull") as IMyMotorStator) == null
                )
                {
                    Terminate("Kontur blocks not found.");
                    return;
                }
                Wheels = new List<IMyMotorSuspension>() { WheelLF, WheelRF, WheelLB, WheelRB };

                string saved = DriverLCD.GetText();
                if (string.IsNullOrEmpty(saved))
                {
                    if (saved.Contains("front")) DriveMode = DriveModes.front;
                    else if (saved.Contains("rear")) DriveMode = DriveModes.rear;
                    if (saved.Contains("off-road")) SuspensionMode = SuspensionModes.offroad;
                    if (saved.Contains("looper")) loopturn = true;
                }

                ChangeFirst();

                AddAct(ref MA, Main, 1);

                SetCmd(new Dictionary<string, Cmd>
                {
                    { "sdm", new Cmd(CmdSDM, "Switch drive mode Full/Front/Rear.") },
                    { "ssm", new Cmd(CmdSSM, "Switch suspension mode Sport/Off road.") },
                    { "sl", new Cmd(CmdSL, "Switch loopturn mode.")}
                });
            }

            void Main()
            {
                carspeed = Convert.ToSingle(RemoteDriver.GetShipSpeed());
                if (RemoteDriver.IsUnderControl)
                {
                    RotateRull();
                    float whangle;
                    if (carspeed >= 18)
                        if (carspeed >= 40) whangle = 15;
                        else whangle = 20;
                    else whangle = NormWhAng;
                    if (Tangle != whangle)
                    {
                        Tangle = whangle;
                        foreach (IMyMotorSuspension ThisWheel in Wheels) ThisWheel.MaxSteerAngle = Tangle;
                    }

                }
                if (switchingsusp)
                {
                    switchingsusp = false;
                    foreach (IMyMotorSuspension Motor in Wheels)
                    {
                        Motor.Strength = Smoothing(Motor, "Strength", strengthSuspModifer, 0.15f);
                        Motor.Height = Smoothing(Motor, "Height", heightSuspModifer, 0.0042f);
                        if (Motor.GetValueFloat("Strength") != strengthSuspModifer || Motor.GetValueFloat("Height") != heightSuspModifer) switchingsusp = true;
                    }
                }
            }
            void ChangeFirst()
            {
                switch (DriveMode)
                {
                    case DriveModes.full:
                        LCDriveMode = "full";
                        break;
                    case DriveModes.front:
                        LCDriveMode = "front";
                        break;
                    case DriveModes.rear:
                        LCDriveMode = "rear";
                        break;
                }
                FirstStr = " –––Suspension manager–––\n Drive mode: " + LCDriveMode;
                if (loopturn) FirstStr += "–looper";
                FirstStr += "\n";
                switch (SuspensionMode)
                {
                    case SuspensionModes.sport:
                        LCDSuspMode = "sport";
                        break;
                    case SuspensionModes.offroad:
                        LCDSuspMode = "off-road";
                        break;
                }
                FirstStr += " Suspension mode: " + LCDSuspMode + "\n";

                DriverLCD.WriteText(FirstStr + SecondStr, false);
            }
            void RotateRull()
            {
                float mult = 1, destangl, temp;
                if (RemoteDriver.MoveIndicator.X == 0) destangl = 0;
                else
                {
                    if (RemoteDriver.MoveIndicator.X < 0) mult = -1;
                    temp = carspeed;
                    if (temp > 50) temp = 50;
                    destangl = Convert.ToSingle((120 - 4 * temp * temp / 100) * Math.PI / 180) * mult;
                }
                float turn = destangl - RotorRull.Angle;
                RotorRull.TargetVelocityRad = turn * 5;
            }
            float Smoothing(IMyMotorSuspension ThisMotor, string variable, float needed, float Step)
            {
                float TempFloat = ThisMotor.GetValueFloat(variable), mult;
                if (Math.Abs(TempFloat - needed) > Step)
                {
                    if (TempFloat < needed) mult = 1;
                    else mult = -1;
                    TempFloat = TempFloat + mult * Step;
                }
                else TempFloat = needed;
                return TempFloat;
            }

            #region Commands
            string CmdSDM(List<string> a)
            {
                if (DriveMode == DriveModes.rear) DriveMode = DriveModes.full;
                else DriveMode++;
                switch (DriveMode)
                {
                    case DriveModes.full:
                        WheelLB.Propulsion = true;
                        WheelRB.Propulsion = true;
                        WheelLF.Propulsion = true;
                        WheelRF.Propulsion = true;
                        break;
                    case DriveModes.front:
                        WheelLB.Propulsion = false;
                        WheelRB.Propulsion = false; // Turn off rear
                        WheelLF.Propulsion = true;
                        WheelRF.Propulsion = true; // Turn on front
                        break;
                    case DriveModes.rear:
                        WheelLB.Propulsion = true;
                        WheelRB.Propulsion = true; // Turn on rear
                        WheelLF.Propulsion = false;
                        WheelRF.Propulsion = false; // Turn off front
                        break;
                }
                ChangeFirst();
                return "";
            }
            string CmdSSM(List<string> a)
            {
                if (SuspensionMode == SuspensionModes.offroad) SuspensionMode = SuspensionModes.sport;
                else SuspensionMode++;
                switchingsusp = true;
                switch (SuspensionMode)
                {
                    case SuspensionModes.sport:
                        strengthSuspModifer = 25;
                        heightSuspModifer = -0.1080F;
                        break;
                    case SuspensionModes.offroad:
                        strengthSuspModifer = 16;
                        heightSuspModifer = -0.36F;
                        break;
                }
                ChangeFirst();
                return "";
            }
            string CmdSL(List<string> a)
            {
                float angle;
                if (loopturn)
                {// Turn off
                    angle = NormWhAng;
                    loopturn = false;
                }
                else
                {// Turn on
                    angle = 45;
                    loopturn = true;
                }
                WheelRF.InvertSteer = loopturn;
                WheelRF.InvertPropulsion = loopturn;
                WheelRB.Steering = loopturn;
                WheelLB.Steering = loopturn;
                WheelRB.InvertSteer = loopturn;
                WheelRB.InvertPropulsion = loopturn;
                foreach (IMyMotorSuspension ThisWheel in Wheels) ThisWheel.MaxSteerAngle = angle;
                ChangeFirst();
                return "";
            }
            #endregion Commands
        }
    }

    //======-SUBPROGRAM ENDING-======
}