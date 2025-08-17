using Hacknet;
using Hacknet.Gui;
using Microsoft.Xna.Framework;
using Pathfinder.Executable;
using Pathfinder.Meta.Load;
using Pathfinder.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stuxnet_HN.Executables
{
    [Executable("#REVOLUTION_EXE#")]
    public class RevolutionExe : GameExecutable
    {
        public const string PREFIX = "[RVLT] ";

        public static List<string> ExcludedNodeIDs = new();
        public bool KilledByAction = false;

        public RevolutionState State = RevolutionState.Launching;

        public RevolutionExe() : base()
        {
            baseRamCost = 400;
            ramCost = 400;
            IdentifierName = "REVOLUTION";
            name = "REVOLUTION!!REVOLUTION!!";
            needsProxyAccess = true;
        }

        public override void OnInitialize()
        {
            base.OnInitialize();
            Computer target = Programs.getComputer(os, targetIP);
            if (ExcludedNodeIDs.Contains(target.idName) || target.idName.StartsWith("exrvlt_"))
            {
                KilledByAction = true;
                os.terminal.writeLine(PREFIX + "THIS TARGET HAS NO SUCH WEAKNESS! RETREAT!");
                os.terminal.writeLine("Execution Failed");
                Killed();
                return;
            }
            else if (target.firewall != null && !target.firewall.solved)
            {
                KilledByAction = true;
                os.terminal.writeLine(PREFIX + "The Target Must Not Have an Active Firewall");
                os.terminal.writeLine("Execution Failed");
                Killed();
                return;
            }
            else if (target.portsNeededForCrack <= target.ports.Count)
            {
                KilledByAction = true;
                os.terminal.writeLine(PREFIX + "WE APPRECIATE THE ENERGY, BUT IT IS NOT REQUIRED HERE!");
                os.terminal.writeLine(PREFIX + "The target PC does not have Inviolability.");
                os.terminal.writeLine("Execution Failed");
                Killed();
                return;
            }
            else if (os.exes.Any(exe => exe is RevolutionExe))
            {
                KilledByAction = true;
                os.terminal.writeLine(PREFIX + "ONE AT A TIME! ONE AT A TIME!");
                os.terminal.writeLine("Execution Failed");
                Killed();
                return;
            }

            os.terminal.writeLine(PREFIX + "REVOLUTION 1.1-hnOS");
            os.terminal.writeLine(PREFIX + "hnOS fork maintained by anon");
        }

        public override void OnUpdate(float delta)
        {
            Computer target = ComputerLookup.FindByIp(targetIP);

            if (isExiting)
            {
                KilledByAction = true;
                Killed();
            }

            if (Lifetime > 4.3f && State == RevolutionState.Launching)
            {
                State = RevolutionState.Exploiting;
                os.terminal.writeLine(PREFIX + "SEARCHING FOR WEAKNESS...");
            }
            else if (Lifetime > 9.0 && State == RevolutionState.Exploiting)
            {
                State = RevolutionState.Success;
                os.terminal.writeLine(PREFIX + "WE'LL HOLD THE DOOR OPEN! GO GO GO!");
                target.portsNeededForCrack = target.ports.Count + 1;
            }
        }

        public override void OnCompleteKilled()
        {
            if (KilledByAction || State == RevolutionState.Killed) return;
            os = OS.currentInstance;

            if(State != RevolutionState.Success)
            {
                os.terminal.writeLine("REST, COMRADE! WE WILL FIGHT ANOTHER DAY");
                os.warningFlash();
                os.beepSound.Play();
                State = RevolutionState.Killed;
                return;
            }
            State = RevolutionState.Killed;

            os.thisComputer.disabled = true;
            os.thisComputer.bootTimer = Computer.BASE_BOOT_TIME;
            os.thisComputerCrashed();
        }

        public override void Draw(float t)
        {
            drawTarget("--REVOLUTION--");
            drawOutline();

            TextItem.doLabel(new Vector2(bounds.X + 10, bounds.Y), "Current state:\n" + getStateName(), Color.White);

            base.Draw(t);
        }

        private string getStateName()
        {
            return Enum.GetName(typeof(RevolutionState), State);
        }
    }

    public enum RevolutionState
    {
        Launching,
        Exploiting,
        Success,
        Killed
    }
}
