using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;

namespace Stuxnet_HN.Extensions
{
    public static class VectorTwoExtensions
    {
        public static Vector2 FromString(this Vector2 _, string str)
        {
            string[] splitString = str.Split(',');

            float x = float.Parse(splitString[0]);
            float y = float.Parse(splitString[1]);

            return new Vector2(x, y);
        }

        public static Vector2 Approach(this Vector2 currentPos, Vector2 approachPos, float step)
        {
            Vector2 delta = approachPos - currentPos;
            float len2 = Vector2.Dot(delta, delta);

            if(len2 < step * step)
            {
                return approachPos;
            }

            Vector2 direction = delta / (float)Math.Sqrt(len2);

            return currentPos + step * direction;
        }
    }
}
