using System.Collections.Generic;

namespace FishExclusions
{
    /// <summary> The mod config class. More info here: https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Config </summary>
    public class ModConfig
    {
        /// <summary>
        /// The items to exclude.
        /// </summary>
        public ItemsToExclude ItemsToExclude { get; set; } = new ItemsToExclude();
        
        /// <summary>
        /// The ID of the item to catch if all possible fish for this water body / season / weather is excluded.
        /// </summary>
        public int ItemToCatchIfAllFishIsExcluded { get; set; } = 168;
        
        /// <summary>
        /// The number of times to retry the 'fish choosing' algorithm before giving up and catching the item specified above.
        /// WARNING: Large numbers can cause a Stack Overflow exception. Use with caution.
        /// </summary>
        public int TimesToRetry { get; set; } = 20;
    }

    public class ItemsToExclude
    {
        /// <summary>
        /// Season- and location-independent exclusions.
        /// </summary>
        public int[] CommonExclusions { get; set; } = { };
        
        /// <summary>
        /// Season- and/or location-dependent exclusions.
        /// </summary>
        public List<ConditionalExclusion> ConditionalExclusions { get; set; } = new List<ConditionalExclusion>();
    }

    public abstract class ConditionalExclusion
    {
        public string Season { get; set; } = "";
        
        public string Location { get; set; } = "";
        
        public int[] FishToExclude { get; set; } = { };
    }
}
