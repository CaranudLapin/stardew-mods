﻿using System;
using System.Collections.Generic;
using System.Linq;
using Harmony;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using xTile.Dimensions;
using xTile.Layers;
using xTile.Tiles;

namespace AnimalsNeedWater
{
    /// <summary> The mod entry class loaded by SMAPI. </summary>
    public class ModEntry : Mod
    {
        #region Variables

        public static IMonitor ModMonitor;
        public static IModHelper ModHelper;
        public static ModEntry Instance;
        public ModConfig Config;
        public Profile CurrentTroughPlacementProfile;
        public List<AnimalLeftThirsty> AnimalsLeftThirstyYesterday;
        
        #endregion
        #region Public methods

        /// <summary> The mod entry point, called after the mod is first loaded. </summary>
        /// <param name="helper"> Provides simplified APIs for writing mods. </param>
        public override void Entry(IModHelper helper)
        {
            ModHelper = helper;
            ModMonitor = Monitor;
            Instance = this;

            AnimalsLeftThirstyYesterday = new List<AnimalLeftThirsty>();

            Config = Helper.ReadConfig<ModConfig>();
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.DayEnding += HandleDayUpdate;

            DetermineTroughPlacementProfile();
        }

        /// <summary> Get ANW's API </summary>
        /// <returns> API instance </returns>
        public override object GetApi()
        {
            return new API();
        }
        
        #endregion
        #region Private methods
        
        /// <summary> Look for known mods that modify coop/barn interiors and load corresponding profiles. </summary>
        private void DetermineTroughPlacementProfile()
        {
            if (Helper.ModRegistry.IsLoaded("AairTheGreat.MoreBarnCoopAnimals"))
            {
                CurrentTroughPlacementProfile = Profiles.MoreBarnAndCoopAnimalsByAair;
                Monitor.Log("Loading trough placement profile for More Barn and Coop Animals mod by AairTheGreat.", LogLevel.Debug);
            } 
            else if (Helper.ModRegistry.IsLoaded("Froststar11.CleanBarnsCoops"))
            {
                CurrentTroughPlacementProfile = Profiles.CleanerBarnsAndCoopsByFroststar11;
                Monitor.Log("Loading trough placement profile for Froststar11's Cleaner Barns & Coops mod.", LogLevel.Debug);
            }
            else if (Helper.ModRegistry.IsLoaded("DaisyNiko.CCBB"))
            {
                CurrentTroughPlacementProfile = Profiles.CuterCoopsAndBetterBarnsByDaisyNiko;
                Monitor.Log("Loading trough placement profile for Cuter Coops and Better Barns mod by DaisyNiko.", LogLevel.Debug);
            }
            else if (Helper.ModRegistry.IsLoaded("nykachu.coopbarnfacelift"))
            {
                CurrentTroughPlacementProfile = Profiles.CoopAndBarnFaceliftByNykachu;
                Monitor.Log("Loading trough placement profile for Coop and Barn Facelift mod by nykachu.", LogLevel.Debug);
            }
            else if (Helper.ModRegistry.IsLoaded("pepoluan.cleanblockbarncoop"))
            {
                CurrentTroughPlacementProfile = Profiles.CleanAndBlockForBarnsAndCoopsByPepoluan;
                Monitor.Log("Loading trough placement profile for Clean and Block for Barns and Coops mod by pepoluan.", LogLevel.Debug);
            }
            else
            {
                CurrentTroughPlacementProfile = Profiles.Default;
                Monitor.Log("No known mods that affect trough placement in Barns and Coops found loaded. Loading the default trough placement profile.");
            }
        }

        /// <summary> Empty water troughs in animal houses. </summary>
        private void EmptyWaterTroughs()
        {
            ModData.BarnsWithWateredTrough = new List<string>();
            ModData.CoopsWithWateredTrough = new List<string>();

            foreach (Building building in Game1.getFarm().buildings)
            {
                // If the building is a deluxe one and the corresponding config option is set to true, 
                // avoid emptying troughs and mark it as watered.
                if(building.nameOfIndoorsWithoutUnique.ToLower().Contains("3") && Config.WateringSystemInDeluxeBuildings)
                {
                    switch (building.nameOfIndoorsWithoutUnique.ToLower())
                    {
                        case "barn3":
                        {
                            if (!ModData.BarnsWithWateredTrough.Contains(building.nameOfIndoors.ToLower()))
                                ModData.BarnsWithWateredTrough.Add(building.nameOfIndoors.ToLower());
                            break;
                        }
                        case "coop3":
                        {
                            if (!ModData.CoopsWithWateredTrough.Contains(building.nameOfIndoors.ToLower()))
                                ModData.CoopsWithWateredTrough.Add(building.nameOfIndoors.ToLower());
                            break;
                        }
                    }

                    continue;
                }
                
                EmptyWaterTroughsInBuilding(building);
            }
        }
        
        /// <summary>
        /// Empty water troughs in the specified animal house.
        /// </summary>
        /// <param name="building"></param>
        private void EmptyWaterTroughsInBuilding(Building building)
        {
            int animalCount = 0;
            GameLocation gameLocation = building.indoors.Value;

            foreach (FarmAnimal animal in Game1.getFarm().getAllFarmAnimals())
            {
                if (animal.home.nameOfIndoors.ToLower().Equals(building.nameOfIndoors.ToLower())) animalCount++;
            }

            if (building.nameOfIndoorsWithoutUnique.ToLower().Contains("coop") && animalCount > 0)
            {
                switch (building.nameOfIndoorsWithoutUnique.ToLower())
                {
                    case "coop":
                    {
                        ChangeCoopTexture(building, true);

                        foreach (TroughTile tile in CurrentTroughPlacementProfile.coopTroughTiles)
                        {
                            gameLocation.removeTile(tile.TileX, tile.TileY, tile.Layer);
                        }

                        Layer buildingsLayer = gameLocation.map.GetLayer("Buildings");
                        Layer frontLayer = gameLocation.map.GetLayer("Front");
                        TileSheet tilesheet = gameLocation.map.GetTileSheet("z_waterTroughTilesheet");

                        foreach (TroughTile tile in CurrentTroughPlacementProfile.coopTroughTiles)
                        {
                            if (tile.Layer.Equals("Buildings"))
                                buildingsLayer.Tiles[tile.TileX, tile.TileY] = new StaticTile(buildingsLayer, tilesheet, BlendMode.Alpha, tileIndex: tile.EmptyTroughTilesheetIndex);
                            else if (tile.Layer.Equals("Front"))
                                frontLayer.Tiles[tile.TileX, tile.TileY] = new StaticTile(frontLayer, tilesheet, BlendMode.Alpha, tileIndex: tile.EmptyTroughTilesheetIndex);
                        }

                        break;
                    }
                    case "coop2":
                    {
                        ChangeBigCoopTexture(building, true);

                        foreach (TroughTile tile in CurrentTroughPlacementProfile.coop2TroughTiles)
                        {
                            gameLocation.removeTile(tile.TileX, tile.TileY, tile.Layer);
                        }

                        Layer buildingsLayer = gameLocation.map.GetLayer("Buildings");
                        Layer frontLayer = gameLocation.map.GetLayer("Front");
                        TileSheet tilesheet = gameLocation.map.GetTileSheet("z_waterTroughTilesheet");

                        foreach (TroughTile tile in CurrentTroughPlacementProfile.coop2TroughTiles)
                        {
                            if (tile.Layer.Equals("Buildings"))
                                buildingsLayer.Tiles[tile.TileX, tile.TileY] = new StaticTile(buildingsLayer, tilesheet, BlendMode.Alpha, tileIndex: tile.EmptyTroughTilesheetIndex);
                            else if (tile.Layer.Equals("Front"))
                                frontLayer.Tiles[tile.TileX, tile.TileY] = new StaticTile(frontLayer, tilesheet, BlendMode.Alpha, tileIndex: tile.EmptyTroughTilesheetIndex);
                        }

                        break;
                    }
                    case "coop3":
                    {
                        foreach (TroughTile tile in CurrentTroughPlacementProfile.coop3TroughTiles)
                        {
                            gameLocation.removeTile(tile.TileX, tile.TileY, tile.Layer);
                        }

                        Layer buildingsLayer = gameLocation.map.GetLayer("Buildings");
                        Layer frontLayer = gameLocation.map.GetLayer("Front");
                        TileSheet tilesheet = gameLocation.map.GetTileSheet("z_waterTroughTilesheet");

                        foreach (TroughTile tile in CurrentTroughPlacementProfile.coop3TroughTiles)
                        {
                            if (tile.Layer.Equals("Buildings"))
                                buildingsLayer.Tiles[tile.TileX, tile.TileY] = new StaticTile(buildingsLayer, tilesheet, BlendMode.Alpha, tileIndex: tile.EmptyTroughTilesheetIndex);
                            else if (tile.Layer.Equals("Front"))
                                frontLayer.Tiles[tile.TileX, tile.TileY] = new StaticTile(frontLayer, tilesheet, BlendMode.Alpha, tileIndex: tile.EmptyTroughTilesheetIndex);
                        }

                        break;
                    }
                }
            }
            else if (building.nameOfIndoorsWithoutUnique.ToLower().Contains("barn") && animalCount > 0)
            {
                switch (building.nameOfIndoorsWithoutUnique.ToLower())
                {
                    case "barn":
                    {
                        foreach (TroughTile tile in CurrentTroughPlacementProfile.barnTroughTiles)
                        {
                            gameLocation.removeTile(tile.TileX, tile.TileY, tile.Layer);
                        }

                        Layer buildingsLayer = gameLocation.map.GetLayer("Buildings");
                        Layer frontLayer = gameLocation.map.GetLayer("Front");
                        TileSheet tilesheet = gameLocation.map.GetTileSheet("z_waterTroughTilesheet");

                        foreach (TroughTile tile in CurrentTroughPlacementProfile.barnTroughTiles)
                        {
                            if (tile.Layer.Equals("Buildings"))
                                buildingsLayer.Tiles[tile.TileX, tile.TileY] = new StaticTile(buildingsLayer, tilesheet, BlendMode.Alpha, tileIndex: tile.EmptyTroughTilesheetIndex);
                            else if (tile.Layer.Equals("Front"))
                                frontLayer.Tiles[tile.TileX, tile.TileY] = new StaticTile(frontLayer, tilesheet, BlendMode.Alpha, tileIndex: tile.EmptyTroughTilesheetIndex);
                        }

                        break;
                    }
                    case "barn2":
                    {
                        foreach (TroughTile tile in CurrentTroughPlacementProfile.barn2TroughTiles)
                        {
                            gameLocation.removeTile(tile.TileX, tile.TileY, tile.Layer);
                        }

                        Layer buildingsLayer = gameLocation.map.GetLayer("Buildings");
                        Layer frontLayer = gameLocation.map.GetLayer("Front");
                        TileSheet tilesheet = gameLocation.map.GetTileSheet("z_waterTroughTilesheet");

                        foreach (TroughTile tile in CurrentTroughPlacementProfile.barn2TroughTiles)
                        {
                            if (tile.Layer.Equals("Buildings"))
                                buildingsLayer.Tiles[tile.TileX, tile.TileY] = new StaticTile(buildingsLayer, tilesheet, BlendMode.Alpha, tileIndex: tile.EmptyTroughTilesheetIndex);
                            else if (tile.Layer.Equals("Front"))
                                frontLayer.Tiles[tile.TileX, tile.TileY] = new StaticTile(frontLayer, tilesheet, BlendMode.Alpha, tileIndex: tile.EmptyTroughTilesheetIndex);
                        }

                        break;
                    }
                    case "barn3":
                    {
                        foreach (TroughTile tile in CurrentTroughPlacementProfile.barn3TroughTiles)
                        {
                            gameLocation.removeTile(tile.TileX, tile.TileY, tile.Layer);
                        }

                        Layer buildingsLayer = gameLocation.map.GetLayer("Buildings");
                        Layer frontLayer = gameLocation.map.GetLayer("Front");
                        TileSheet tilesheet = gameLocation.map.GetTileSheet("z_waterTroughTilesheet");

                        foreach (TroughTile tile in CurrentTroughPlacementProfile.barn3TroughTiles)
                        {
                            if (tile.Layer.Equals("Buildings"))
                                buildingsLayer.Tiles[tile.TileX, tile.TileY] = new StaticTile(buildingsLayer, tilesheet, BlendMode.Alpha, tileIndex: tile.EmptyTroughTilesheetIndex);
                            else if (tile.Layer.Equals("Front"))
                                frontLayer.Tiles[tile.TileX, tile.TileY] = new StaticTile(frontLayer, tilesheet, BlendMode.Alpha, tileIndex: tile.EmptyTroughTilesheetIndex);
                        }

                        break;
                    }
                }
            }
            else if (animalCount == 0)
            {
                if (building.nameOfIndoorsWithoutUnique.ToLower().Contains("coop"))
                {
                    ModData.CoopsWithWateredTrough.Add(building.nameOfIndoors.ToLower());
                }
                else if (building.nameOfIndoorsWithoutUnique.ToLower().Contains("barn"))
                {
                    ModData.BarnsWithWateredTrough.Add(building.nameOfIndoors.ToLower());
                }
            }
        }

        /// <summary> Looks for animals left thirsty, notifies player of them and loads new tilesheets if needed. </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void HandleDayUpdate(object sender, DayEndingEventArgs e)
        {
            List<AnimalLeftThirsty> animalsLeftThirsty = new List<AnimalLeftThirsty>();
            
            // Look for all animals inside buildings and check whether their troughs are watered.
            foreach (Building building in Game1.getFarm().buildings)
            {
                if (building.nameOfIndoors.ToLower().Contains("coop"))
                {
                    foreach (var animal in ((AnimalHouse) building.indoors.Value).animals.Values
                        .Where(animal =>
                            ModData.CoopsWithWateredTrough.Contains(animal.home.nameOfIndoors.ToLower()) == false &&
                            ModData.FullAnimals.Contains(animal.displayName) == false).Where(animal =>
                            animalsLeftThirsty.All(item => item.DisplayName != animal.displayName)))
                    {
                        animal.friendshipTowardFarmer.Value -= Math.Abs(Config.NegativeFriendshipPointsForNotWateredTrough);
                        animalsLeftThirsty.Add(new AnimalLeftThirsty(animal.displayName, (animal.isMale() ? "male" : "female")));
                    }
                } 
                else if (building.nameOfIndoors.ToLower().Contains("barn"))
                {
                    foreach (var animal in ((AnimalHouse) building.indoors.Value).animals.Values
                        .Where(animal =>
                            ModData.BarnsWithWateredTrough.Contains(animal.home.nameOfIndoors.ToLower()) == false &&
                            ModData.FullAnimals.Contains(animal.displayName) == false).Where(animal =>
                            animalsLeftThirsty.All(item => item.DisplayName != animal.displayName)))
                    {
                        animal.friendshipTowardFarmer.Value -= Math.Abs(Config.NegativeFriendshipPointsForNotWateredTrough);
                        animalsLeftThirsty.Add(new AnimalLeftThirsty(animal.displayName, (animal.isMale() ? "male" : "female")));
                    }
                }
            }

            // Check for animals outside their buildings as well.
            foreach (FarmAnimal animal in Game1.getFarm().animals.Values)
            {
                if (animal.home.nameOfIndoorsWithoutUnique.ToLower().Contains("coop"))
                {
                    if ((ModData.CoopsWithWateredTrough.Contains(animal.home.nameOfIndoors.ToLower()) ||
                         ModData.FullAnimals.Contains((animal).displayName)) &&
                        animal.home.animalDoorOpen.Value) continue;
                    
                    if (animalsLeftThirsty.Any(item => item.DisplayName == animal.displayName)) continue;
                        
                    animal.friendshipTowardFarmer.Value -= Math.Abs(Config.NegativeFriendshipPointsForNotWateredTrough);
                    animalsLeftThirsty.Add(new AnimalLeftThirsty(animal.displayName, (animal.isMale() ? "male" : "female")));
                } 
                else if(animal.home.nameOfIndoorsWithoutUnique.ToLower().Contains("barn"))
                {
                    if ((ModData.BarnsWithWateredTrough.Contains(animal.home.nameOfIndoors.ToLower()) != false ||
                         ModData.FullAnimals.Contains(animal.displayName) != false) &&
                        animal.home.animalDoorOpen.Value != false) continue;
                    if (animalsLeftThirsty.Any(item => item.DisplayName == animal.displayName)) continue;
                    
                    animal.friendshipTowardFarmer.Value -= Math.Abs(Config.NegativeFriendshipPointsForNotWateredTrough);
                    animalsLeftThirsty.Add(new AnimalLeftThirsty(animal.displayName, (animal.isMale() ? "male" : "female")));
                }
            }
            
            // Notify player of animals left thirsty, if any.
            if (animalsLeftThirsty.Any())
            {
                switch (animalsLeftThirsty.Count())
                {
                    case 1 when animalsLeftThirsty[0].Gender == "male":
                        Game1.showGlobalMessage(ModHelper.Translation.Get("AnimalsLeftWithoutWaterYesterday.globalMessage.oneAnimal_Male", new { firstAnimalName = animalsLeftThirsty[0].DisplayName }));
                        break;
                    case 1 when animalsLeftThirsty[0].Gender == "female":
                        Game1.showGlobalMessage(ModHelper.Translation.Get("AnimalsLeftWithoutWaterYesterday.globalMessage.oneAnimal_Female", new { firstAnimalName = animalsLeftThirsty[0].DisplayName }));
                        break;
                    case 1:
                        Game1.showGlobalMessage(ModHelper.Translation.Get("AnimalsLeftWithoutWaterYesterday.globalMessage.oneAnimal_UnknownGender", new { firstAnimalName = animalsLeftThirsty[0].DisplayName }));
                        break;
                    case 2:
                        Game1.showGlobalMessage(ModHelper.Translation.Get("AnimalsLeftWithoutWaterYesterday.globalMessage.twoAnimals", new { firstAnimalName = animalsLeftThirsty[0].DisplayName, secondAnimalName = animalsLeftThirsty[1].DisplayName }));
                        break;
                    case 3:
                        Game1.showGlobalMessage(ModHelper.Translation.Get("AnimalsLeftWithoutWaterYesterday.globalMessage.threeAnimals", new { firstAnimalName = animalsLeftThirsty[0].DisplayName, secondAnimalName = animalsLeftThirsty[1].DisplayName, thirdAnimalName = animalsLeftThirsty[2].DisplayName }));
                        break;
                    default:
                        Game1.showGlobalMessage(ModHelper.Translation.Get("AnimalsLeftWithoutWaterYesterday.globalMessage.multipleAnimals", new { firstAnimalName = animalsLeftThirsty[0].DisplayName, secondAnimalName = animalsLeftThirsty[1].DisplayName, thirdAnimalName = animalsLeftThirsty[2].DisplayName, totalAmountExcludingFirstThree = animalsLeftThirsty.Count() - 3 }));
                        break;
                }
            }

            AnimalsLeftThirstyYesterday = animalsLeftThirsty;

            ModData.FullAnimals = new List<string>();

            List<object> nextDayAndSeasonList = GetNextDayAndSeason(Game1.dayOfMonth, Game1.currentSeason);
            
            // Check if tomorrow is a festival day. If not, empty the troughs.
            if (!Utility.isFestivalDay((int)nextDayAndSeasonList[0], (string)nextDayAndSeasonList[1]))
            {
                EmptyWaterTroughs();
            }
            else
            {
                foreach (Building building in Game1.getFarm().buildings)
                {
                    if (building.nameOfIndoorsWithoutUnique.ToLower().Contains("coop"))
                    {
                        ModData.CoopsWithWateredTrough.Add(building.nameOfIndoors.ToLower());
                    }
                    else if (building.nameOfIndoorsWithoutUnique.ToLower().Contains("barn"))
                    {
                        ModData.BarnsWithWateredTrough.Add(building.nameOfIndoors.ToLower());
                    }
                }
            }
            
            LoadNewTileSheets();
            PlaceWateringSystems();
        }

        /// <summary> Raised after the game is launched, right before the first update tick. This happens once per game session (unrelated to loading saves). All mods are loaded and initialised at this point, so this is a good time to set up mod integrations. </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var harmony = HarmonyInstance.Create("GZhynko.AnimalsNeedWater");

            harmony.Patch(
                AccessTools.Method(typeof(AnimalHouse), nameof(AnimalHouse.performToolAction)),
                new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.AnimalHouseToolAction))
            );

            harmony.Patch(
                AccessTools.Method(typeof(FarmAnimal), nameof(FarmAnimal.dayUpdate)),
                new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.AnimalDayUpdate))
            );

            harmony.Patch(
                AccessTools.Method(typeof(FarmAnimal), "behaviors", new[] {
                    typeof(GameTime),
                    typeof(GameLocation)
                }),
                new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.AnimalBehaviors))
            );

            harmony.Patch(
                AccessTools.Method(typeof(Game1), nameof(Game1.warpFarmer), new[] {
                    typeof(string),
                    typeof(int),
                    typeof(int),
                    typeof(int),
                    typeof(bool)
                }),
                new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.WarpFarmer))
            );
        }

        /// <summary> Raised after the save is loaded. </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            LoadNewTileSheets();
            PlaceWateringSystems();

            HandleDayStart();
        }

        private void HandleDayStart()
        {
            if (!Utility.isFestivalDay(Game1.dayOfMonth, Game1.currentSeason))
            {
                EmptyWaterTroughs();
            } 
            else
            {
                foreach (Building building in Game1.getFarm().buildings)
                {
                    if (building.nameOfIndoorsWithoutUnique.ToLower().Contains("coop"))
                    {
                        ModData.CoopsWithWateredTrough.Add(building.nameOfIndoors.ToLower());

                        if (building.nameOfIndoorsWithoutUnique.ToLower().Equals("coop"))
                        {
                            ChangeCoopTexture(building, false);
                        }
                        else if (building.nameOfIndoorsWithoutUnique.ToLower().Equals("coop2"))
                        {
                            ChangeBigCoopTexture(building, false);
                        }
                    }
                    else if (building.nameOfIndoorsWithoutUnique.ToLower().Contains("barn"))
                    {
                        ModData.BarnsWithWateredTrough.Add(building.nameOfIndoors.ToLower());
                    }
                }
            }
        }

        #endregion
        #region Utils
        
        public void ChangeBigCoopTexture(Building building, bool empty)
        {
            if (!Config.ReplaceCoopTextureIfTroughIsEmpty) return;

            building.texture = empty ? 
                new Lazy<Texture2D>(() => Helper.Content.Load<Texture2D>("assets/Coop2_emptyWaterTrough.png"))
                : new Lazy<Texture2D>(() => Helper.Content.Load<Texture2D>("assets/Coop2_fullWaterTrough.png"));
        }
        
        public void ChangeCoopTexture(Building building, bool empty)
        {
            if (!Config.ReplaceCoopTextureIfTroughIsEmpty) return;

            building.texture = empty ? 
                new Lazy<Texture2D>(() => Helper.Content.Load<Texture2D>("assets/Coop_emptyWaterTrough.png"))
                : new Lazy<Texture2D>(() => Helper.Content.Load<Texture2D>("assets/Coop_fullWaterTrough.png"));
        }

        private void LoadNewTileSheets()
        {
            foreach (Building building in Game1.getFarm().buildings)
            {
                if (building.nameOfIndoorsWithoutUnique.ToLower().Contains("coop"))
                {
                    var coopMap = building.indoors.Value.map;

                    var tileSheet = new TileSheet(
                        "z_waterTroughTilesheet",
                        coopMap,
                        Instance.Helper.Content.GetActualAssetKey("assets/waterTroughTilesheet.xnb"),
                        new Size(160, 16),
                        new Size(16, 16)
                    );

                    coopMap.AddTileSheet(tileSheet);
                    coopMap.LoadTileSheets(Game1.mapDisplayDevice);

                    if (building.nameOfIndoorsWithoutUnique.ToLower() != "coop3" ||
                        !Config.WateringSystemInDeluxeBuildings) continue;
                    
                    var coop3Map = building.indoors.Value.map;

                    var tileSheet3 = new TileSheet(
                        "z_wateringSystemTilesheet",
                        coop3Map,
                        Instance.Helper.Content.GetActualAssetKey("assets/wateringSystemTilesheet.xnb"),
                        new Size(48, 16),
                        new Size(16, 16)
                    );

                    coop3Map.AddTileSheet(tileSheet3);
                    coop3Map.LoadTileSheets(Game1.mapDisplayDevice);
                }
                else if (building.nameOfIndoorsWithoutUnique.ToLower().Contains("barn"))
                {
                    var barnMap = building.indoors.Value.map;

                    var tileSheet = new TileSheet(
                        "z_waterTroughTilesheet",
                        barnMap,
                        Instance.Helper.Content.GetActualAssetKey("assets/waterTroughTilesheet.xnb"),
                        new Size(160, 16),
                        new Size(16, 16)
                    );

                    barnMap.AddTileSheet(tileSheet);
                    barnMap.LoadTileSheets(Game1.mapDisplayDevice);

                    if (building.nameOfIndoorsWithoutUnique.ToLower() != "barn3" ||
                        !Config.WateringSystemInDeluxeBuildings) continue;
                    
                    var barn3Map = building.indoors.Value.map;

                    var tileSheet3 = new TileSheet(
                        "z_wateringSystemTilesheet",
                        barn3Map,
                        Instance.Helper.Content.GetActualAssetKey("assets/wateringSystemTilesheet.xnb"),
                        new Size(48, 16),
                        new Size(16, 16)
                    );

                    barn3Map.AddTileSheet(tileSheet3);
                    barn3Map.LoadTileSheets(Game1.mapDisplayDevice);
                }
            }
        }

        private void PlaceWateringSystems()
        {
            foreach (Building building in Game1.getFarm().buildings)
            {
                switch (building.nameOfIndoorsWithoutUnique.ToLower())
                {
                    case "coop3" when Config.WateringSystemInDeluxeBuildings:
                    {
                        var gameLocation = building.indoors.Value;

                        foreach (SimplifiedTile tile in CurrentTroughPlacementProfile.coop3WateringSystem.TilesToRemove)
                        {
                            gameLocation.removeTile(tile.TileX, tile.TileY, tile.Layer);
                        }

                        Layer buildingsLayer = gameLocation.map.GetLayer("Buildings");
                        Layer frontLayer = gameLocation.map.GetLayer("Front");
                        TileSheet tilesheet = gameLocation.map.GetTileSheet("z_wateringSystemTilesheet");

                        if (CurrentTroughPlacementProfile.coop3WateringSystem.Layer.Equals("Buildings"))
                            buildingsLayer.Tiles[CurrentTroughPlacementProfile.coop3WateringSystem.TileX, CurrentTroughPlacementProfile.coop3WateringSystem.TileY] = new StaticTile(buildingsLayer, tilesheet, BlendMode.Alpha, tileIndex: CurrentTroughPlacementProfile.coop3WateringSystem.SystemTilesheetIndex);
                        else if (CurrentTroughPlacementProfile.coop3WateringSystem.Layer.Equals("Front"))
                            frontLayer.Tiles[CurrentTroughPlacementProfile.coop3WateringSystem.TileX, CurrentTroughPlacementProfile.coop3WateringSystem.TileY] = new StaticTile(frontLayer, tilesheet, BlendMode.Alpha, tileIndex: CurrentTroughPlacementProfile.coop3WateringSystem.SystemTilesheetIndex);
                        break;
                    }
                    case "barn3" when Config.WateringSystemInDeluxeBuildings:
                    {
                        var gameLocation = building.indoors.Value;

                        foreach (SimplifiedTile tile in CurrentTroughPlacementProfile.barn3WateringSystem.TilesToRemove)
                        {
                            gameLocation.removeTile(tile.TileX, tile.TileY, tile.Layer);
                        }

                        Layer buildingsLayer = gameLocation.map.GetLayer("Buildings");
                        Layer frontLayer = gameLocation.map.GetLayer("Front");
                        TileSheet tilesheet = gameLocation.map.GetTileSheet("z_wateringSystemTilesheet");

                        if (CurrentTroughPlacementProfile.barn3WateringSystem.Layer.Equals("Buildings"))
                            buildingsLayer.Tiles[CurrentTroughPlacementProfile.barn3WateringSystem.TileX, CurrentTroughPlacementProfile.barn3WateringSystem.TileY] = new StaticTile(buildingsLayer, tilesheet, BlendMode.Alpha, tileIndex: CurrentTroughPlacementProfile.barn3WateringSystem.SystemTilesheetIndex);
                        else if (CurrentTroughPlacementProfile.barn3WateringSystem.Layer.Equals("Front"))
                            frontLayer.Tiles[CurrentTroughPlacementProfile.barn3WateringSystem.TileX, CurrentTroughPlacementProfile.barn3WateringSystem.TileY] = new StaticTile(frontLayer, tilesheet, BlendMode.Alpha, tileIndex: CurrentTroughPlacementProfile.barn3WateringSystem.SystemTilesheetIndex);
                        break;
                    }
                }
            }
        }

        private string NextSeason(string season)
        {
            var newSeason = "";
            
            switch (season)
            {
                case "spring":
                    newSeason = "summer";
                    break;
                case "summer":
                    newSeason = "fall";
                    break;
                case "fall":
                    newSeason = "winter";
                    break;
                case "winter":
                    newSeason = "spring";
                    break;
            }

            return newSeason;
        }
        
        private List<object> GetNextDayAndSeason(int currDay, string currSeason)
        {
            if (currDay + 1 <= 28)
            {
                List<object> returnList = new List<object>
                {
                    currDay + 1,
                    currSeason
                };
                return returnList;
            }
            else
            {
                List<object> returnList = new List<object>
                {
                    1,
                    NextSeason(currSeason)
                };
                return returnList;
            }
        }

        public class AnimalLeftThirsty
        {
            public AnimalLeftThirsty(string displayName, string gender)
            {
                DisplayName = displayName;
                Gender = gender;
            }

            public string DisplayName { get; }
            public string Gender { get; }
        }
        
        #endregion
    }
}
