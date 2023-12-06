using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stuxnet_HN.Static
{
    public class States
    {
        public enum IllustratorStates : int
        {
            None = 0,
            DrawTitle = 1,
            CTCDialogue = 2,
            AutoDialogue = 3
        }
    }
}
