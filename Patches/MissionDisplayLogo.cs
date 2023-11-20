using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using HarmonyLib;

using Hacknet;
using Hacknet.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Stuxnet_HN.Patches
{
    [HarmonyPatch]
    public class MissionDisplayLogo
    {
        static Texture2D clientLogo = null;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MissionHubServer),nameof(MissionHubServer.doContractPreviewScreen))]
        static bool Prefix(MissionHubServer __instance, Rectangle bounds, SpriteBatch sb)
        {
            string iDStringForContractFile = __instance.getIDStringForContractFile(__instance.listingsFolder.files[__instance.selectedElementIndex]);

            ActiveMission activeMission = __instance.listingMissions[iDStringForContractFile];

            string client = activeMission.client;

            GetClientLogo(client);

            if (clientLogo == null) { return true; }

            Rectangle clientLogoRect = new Rectangle()
            {
                X = bounds.Center.X - (bounds.Width / 3),
                Y = bounds.Center.Y - (bounds.Width / 3),
                Width = (int)(bounds.Width / 1.5f),
                Height = (int)(bounds.Width / 1.5f)
            };

            sb.Draw(clientLogo, clientLogoRect, Color.White * 0.25f);

            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MissionHubServer),nameof(MissionHubServer.doListingScreen))]
        static void Postfix()
        {
            if(clientLogo != null) { clientLogo = null; }
        }

        private static void GetClientLogo(string client)
        {
            if(clientLogo != null) { return; }

            string extensionFolder = ExtensionLoader.ActiveExtensionInfo.FolderPath;

            string targetFile = $"{extensionFolder}/Images/{client}.png";

            if (!File.Exists(targetFile)) { return; }

            FileStream clientLogoStream = File.OpenRead(targetFile);
            clientLogo = Texture2D.FromStream(GuiData.spriteBatch.GraphicsDevice, clientLogoStream);
            clientLogoStream.Dispose();
        }
    }
}
