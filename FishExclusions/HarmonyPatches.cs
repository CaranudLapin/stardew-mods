using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using Object = StardewValley.Object;
// ReSharper disable InconsistentNaming

namespace FishExclusions
{
    public class HarmonyPatches
    {
        /// <summary> Patch for the GameLocation.getFish method. </summary>
        public static void GetFish(GameLocation __instance, float millisecondsAfterNibble, int bait, int waterDepth, Farmer who,
            double baitPotency, Vector2 bobberTile, ref Object __result, string locationName = null)
        {
            if (!ModEntry.ExclusionsEnabled) return;
            
            var bannedIds = Utils.GetExcludedFish(ModEntry.Config, Game1.currentSeason, __instance.Name, Game1.IsRainingHere(__instance));
            
            // This method has a neat unused (yet?) parameter 'baitPotency'. Why not to use it to avoid recursion?
            if ((int) baitPotency == 909 || !bannedIds.Contains(__result.parentSheetIndex)) return;
            
            var numberOfAttempts = 0;
            
            // Retry x times before giving up.
            var maxAttempts = ModEntry.Config.TimesToRetry;

            var lastResult = __result;

            while (numberOfAttempts < maxAttempts && bannedIds.Contains(lastResult.parentSheetIndex))
            {
                lastResult = __instance.getFish(millisecondsAfterNibble, bait, waterDepth, who, 909, bobberTile,
                    locationName);
                
                numberOfAttempts++;
            }

            var itemToCatchIfNoVariantsLeft = ModEntry.Config.ItemToCatchIfAllFishIsExcluded == 0
                ? 168
                : ModEntry.Config.ItemToCatchIfAllFishIsExcluded;
            
            // Return Trash or the item specified in config in case all possible
            // fish for this water body / season / weather is excluded.
            if(bannedIds.Contains(lastResult.parentSheetIndex)) lastResult = new Object(itemToCatchIfNoVariantsLeft, 1);

            __result = lastResult;
        }
    }
}
