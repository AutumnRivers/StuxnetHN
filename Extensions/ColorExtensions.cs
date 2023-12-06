using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;

namespace Stuxnet_HN.Extensions
{
    public static class ColorExtensions
    {
        public static Color FromString(this Color _, string colorString)
        {
            string[] stringArr = colorString.Split(',');

            Vector3 colorVec = new Vector3(
                int.Parse(stringArr[0]),
                int.Parse(stringArr[1]),
                int.Parse(stringArr[2])
                );

            return new Color(colorVec);
        }
    }
}
