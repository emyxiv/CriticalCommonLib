﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CriticalCommonLib.Collections;
using CriticalCommonLib.Extensions;
using Dalamud.Data;
using Lumina;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using CriticalCommonLib.Sheets;
using Lumina.Data;

namespace CriticalCommonLib.Services
{
    public partial class ExcelCache : IDisposable
    {
        private readonly DataManager? _dataManager;
        private readonly GameData? _gameData;
        
        private Dictionary<uint, EventItem> _eventItemCache;
        private Dictionary<uint, ItemUICategory> _itemUiCategory;
        private Dictionary<uint, ItemSearchCategory> _itemSearchCategory;
        private Dictionary<uint, ItemSortCategory> _itemSortCategory;
        private Dictionary<uint, EquipSlotCategory> _equipSlotCategories;
        private Dictionary<uint, EquipRaceCategory> _equipRaceCategories;
        private Dictionary<uint, Recipe> _recipeCache;
        private Dictionary<uint, HashSet<uint>> _classJobCategoryLookup;
        private readonly HashSet<uint> _armoireItems;
        private bool _itemUiCategoriesFullyLoaded;
        private bool _itemUiSearchFullyLoaded;
        private bool _recipeLookUpCalculated;
        private bool _companyCraftSequenceCalculated;
        private bool _classJobCategoryLookupCalculated;
        private bool _craftLevesItemLookupCalculated;
        private bool _allItemsLoaded;
        private bool _armoireLoaded;
        private ENpcCollection _eNpcCollection;
        private ShopCollection _shopCollection;

        /// <summary>
        ///     Dictionary of each gc scrip shop and it's associated gc scrip shop items
        /// </summary>
        public Dictionary<uint, HashSet<uint>> GcScripShopItemToGcScripCategories { get; private set; }

        
        /// <summary>
        ///     Dictionary of each gc scrip shop category and it's associated grand company
        /// </summary>
        public Dictionary<uint, uint> GcScripShopCategoryGrandCompany { get; private set; }
        
        /// <summary>
        ///     Dictionary of each gc ID matched to a gc shop 
        /// </summary>
        public Dictionary<uint, uint> GcShopGrandCompany { get; private set; }
        
        /// <summary>
        ///     Dictionary of gc scrip shop items and their associated items
        /// </summary>
        public Dictionary<(uint, uint), uint> GcScripShopToItem { get; private set; }
        
        /// <summary>
        ///     Dictionary of item IDs and associated scrip shop items
        /// </summary>
        public Dictionary<uint, HashSet<(uint, uint)>> ItemGcScripShopLookup { get; private set; }
        
        /// <summary>
        ///     Dictionary of gil shop IDs and their associated item IDs
        /// </summary>
        public Dictionary<uint,HashSet<uint>> GilShopItemLookup { get; private set; }
        
        /// <summary>
        ///     Dictionary of item IDs and their associated gil shop IDs
        /// </summary>
        public Dictionary<uint,HashSet<uint>> ItemGilShopLookup { get; private set; }
        
        /// <summary>
        ///     Dictionary of gil shop IDs and their associated gil shop item ids
        /// </summary>
        public Dictionary<uint,HashSet<uint>> GilShopGilShopItemLookup { get; private set; }
        
        /// <summary>
        ///     Dictionary of item IDs and their associated special shop IDs where the item is the result
        /// </summary>
        public Dictionary<uint,HashSet<uint>> ItemSpecialShopResultLookup { get; private set; }
        
        /// <summary>
        ///     Dictionary of item IDs and their associated special shop IDs where the item is the cost
        /// </summary>
        public Dictionary<uint,HashSet<uint>> ItemSpecialShopCostLookup { get; private set; }
        
        /// <summary>
        ///     Dictionary of all reward items and their associated cost items(currencies)
        /// </summary>
        public Dictionary<uint,HashSet<uint>> SpecialShopItemRewardCostLookup { get; private set; }
        
        /// <summary>
        ///     Dictionary of all special shop cost items and their associated reward items(currencies)
        /// </summary>
        public Dictionary<uint,HashSet<uint>> SpecialShopItemCostRewardLookup { get; private set; }
        
        /// <summary>
        ///     Dictionary of item IDs and their associated gathering item IDs
        /// </summary>
        public Dictionary<uint, HashSet<uint>> ItemGatheringItem { get; private set; }
        
        /// <summary>
        ///     Dictionary of item IDs and it's associated gathering types 
        /// </summary>
        public Dictionary<uint, HashSet<uint>> ItemGatheringTypes { get; private set; }
        
        /// <summary>
        ///     Dictionary of each gathering item and it's associated points
        /// </summary>
        public Dictionary<uint, HashSet<uint>> GatheringItemToGatheringItemPoint { get; private set; }

        /// <summary>
        ///     Dictionary of each gathering item point and it's associated gathering point base
        /// </summary>
        public Dictionary<uint, HashSet<uint>> GatheringItemPointToGatheringPointBase  { get; private set; }

        /// <summary>
        ///     Dictionary of each gathering item point and it's associated gathering point base
        /// </summary>
        public Dictionary<uint, HashSet<uint>> GatheringPointBaseToGatheringPoint  { get; private set; }

        /// <summary>
        ///     Dictionary of each gathering item base to it's gathering type
        /// </summary>
        public Dictionary<uint, uint> GatheringPointBaseToGatheringType { get; private set; }

        /// <summary>
        ///     Dictionary of each item and it's associated aquarium fish(if applicable)
        /// </summary>
        public Dictionary<uint, uint> ItemToAquariumFish { get; private set; }

        /// <summary>
        ///     Dictionary of each npc and it's potential level(not including what's in the lgb files)
        /// </summary>
        public Dictionary<uint, uint> NpcToLevel { get; private set; }
        
        /// <summary>
        ///     Dictionary of each result item when handing in an inspection item for HWD and it's required items + amount
        /// </summary>
        public Dictionary<uint, (uint, uint)> HwdInspectionResults { get; private set; }
        
        /// <summary>
        ///     Dictionary of all the shops referenced in the topic select sheet and their associated actual shop ids
        /// </summary>
        public Dictionary<uint, HashSet<uint>> ShopToShopCollectionLookup { get; private set; }
        
        /// <summary>
        ///     Dictionary of each item and it's related fish parameter
        /// </summary>
        public Dictionary<uint, uint> FishParameters { get; private set; }
        
        /// <summary>
        ///     Dictionary of each tomestone ID and it's related item
        /// </summary>
        public Dictionary<uint, uint> TomestoneLookup { get; private set; }
        
        /// <summary>
        ///     Dictionary of each inclusion shop and it's categories
        /// </summary>
        public Dictionary<uint, HashSet<uint>> InclusionShopToCategoriesLookup { get; private set; }
        
        /// <summary>
        ///     Dictionary of each inclusion shop category and it's associated shop
        /// </summary>
        public Dictionary<uint, HashSet<uint>> InclusionShopCategoryToShopLookup { get; private set; }
        
        /// <summary>
        ///     Dictionary of each inclusion shop category and it's associated shop series
        /// </summary>
        public Dictionary<uint, HashSet<uint>> InclusionShopCategoryToShopSeriesLookup { get; private set; }
        
        /// <summary>
        ///     Caches all the items so we don't have to enumerate each frame
        /// </summary>
        public Dictionary<uint, ItemEx> AllItems
        {
            get
            {
                return _allItems ??= GetSheet<ItemEx>().ToCache();
            }
            private set => _allItems = value;
        }

        private Dictionary<uint, ItemEx>? _allItems;
        
        public HashSet<uint> GetCurrencies(uint minimumEntries = 0)
        {
            return ItemSpecialShopCostLookup.Where(c => minimumEntries == 0 || c.Value.Count >= minimumEntries).Select(c => c.Key).ToHashSet();
        }

        /// <summary>
        ///     Dictionary of items and their associated recipes
        /// </summary>
        public Dictionary<uint, HashSet<uint>> ItemRecipes
        {
            get => _itemRecipes ??= GetSheet<Recipe>().ToColumnLookup(c => c.ItemResult.Row, c => c.RowId);
            private set => _itemRecipes = value;
        }
        private Dictionary<uint, HashSet<uint>>? _itemRecipes;
        
        


        private readonly Dictionary<uint, Dictionary<uint, uint>>
            flattenedRecipes;

        private ConcurrentDictionary<uint, HashSet<uint>>? _itemToRetainerTaskNormalLookup;
        private Dictionary<uint,uint>? _retainerTaskToRetainerNormalLookup;

        //Key is the class job category and the hashset contains a list of class jobs
        public Dictionary<uint, HashSet<uint>> ClassJobCategoryLookup
        {
            get => _classJobCategoryLookup ?? new Dictionary<uint, HashSet<uint>>();
            set => _classJobCategoryLookup = value;
        }

        public Dictionary<uint, ItemUICategory> ItemUiCategory
        {
            get => _itemUiCategory ?? new Dictionary<uint, ItemUICategory>();
            set => _itemUiCategory = value;
        }

        public Dictionary<uint, ItemSearchCategory> SearchCategory
        {
            get => _itemSearchCategory ?? new Dictionary<uint, ItemSearchCategory>();
            set => _itemSearchCategory = value;
        }

        public Dictionary<uint, ItemSortCategory> SortCategory
        {
            get => _itemSortCategory ?? new Dictionary<uint, ItemSortCategory>();
            set => _itemSortCategory = value;
        }

        public Dictionary<uint, EquipSlotCategory> EquipSlotCategories
        {
            get => _equipSlotCategories ?? new Dictionary<uint, EquipSlotCategory>();
            set => _equipSlotCategories = value;
        }

        public Dictionary<uint, EventItem> EventItemCache
        {
            get => _eventItemCache ?? new Dictionary<uint, EventItem>();
            set => _eventItemCache = value;
        }

        public Dictionary<uint, Recipe> RecipeCache
        {
            get => _recipeCache ?? new Dictionary<uint, Recipe>();
            set => _recipeCache = value;
        }

        public Dictionary<uint, EquipRaceCategory> EquipRaceCategories
        {
            get => _equipRaceCategories ?? new Dictionary<uint, EquipRaceCategory>();
            set => _equipRaceCategories = value;
        }

        public HashSet<uint> GilShopBuyable { get; set; }

        //Lookup of each recipe available for each item
        public Dictionary<uint, HashSet<uint>> RecipeLookupTable { get; set; }

        //Dictionary of every item that an item can craft
        public Dictionary<uint, HashSet<uint>> CraftLookupTable { get; set; }

        public Dictionary<uint, string> AddonNames { get; set; }

        public Dictionary<uint, uint> CraftLevesItemLookup { get; set; }

        public Dictionary<uint, uint> CompanyCraftSequenceByItemIdLookup { get; set; }

        public ConcurrentDictionary<uint, HashSet<uint>> ItemToRetainerTaskNormalLookup
        {
            get
            {
                if (_itemToRetainerTaskNormalLookup == null)
                {
                    _itemToRetainerTaskNormalLookup =new  ConcurrentDictionary<uint, HashSet<uint>>(GetSheet<RetainerTaskNormal>()
                        .GroupBy(c => c.Item.Row)
                        .ToDictionary(c => c.Key, c => c.Select(t => t.RowId).ToHashSet()));
                }

                return _itemToRetainerTaskNormalLookup;
            }
        }
        
        public Dictionary<uint,uint> RetainerTaskToRetainerNormalLookup
        {
            get
            {
                if (_retainerTaskToRetainerNormalLookup == null)
                {
                    _retainerTaskToRetainerNormalLookup = GetSheet<RetainerTask>().ToSingleLookup(c => c.Task,c => c.RowId, true, true);
                }

                return _retainerTaskToRetainerNormalLookup;
            }
        }

        public ENpcCollection ENpcCollection => _eNpcCollection;
        public ShopCollection ShopCollection => _shopCollection;

        public string GetAddonName(uint addonId)
        {
            if (AddonNames.ContainsKey(addonId)) return AddonNames[addonId];

            var addonSheet = GetSheet<Addon>();
            var addonRow = addonSheet.GetRow(addonId);
            if (addonRow != null)
            {
                AddonNames.Add(addonId, addonRow.Text);
                return addonRow.Text;
            }

            return "";
        }

        public bool CanCraftItem(uint rowId)
        {
            if (!_recipeLookUpCalculated) CalculateRecipeLookup();

            return RecipeLookupTable.ContainsKey(rowId);
        }

        public bool IsCraftItem(uint rowId)
        {
            if (!_recipeLookUpCalculated) CalculateRecipeLookup();

            return CraftLookupTable.ContainsKey(rowId) && CraftLookupTable[rowId].Count != 0;
        }

        public bool IsArmoireItem(uint rowId)
        {
            if (!_armoireLoaded) CalculateArmoireItems();

            return _armoireItems.Contains(rowId);
        }

        private ExcelCache()
        {
            _eventItemCache = new Dictionary<uint, EventItem>();
            _itemUiCategory = new Dictionary<uint, ItemUICategory>();
            _itemSearchCategory = new Dictionary<uint, ItemSearchCategory>();
            _itemSortCategory = new Dictionary<uint, ItemSortCategory>();
            _equipSlotCategories = new Dictionary<uint, EquipSlotCategory>();
            _equipRaceCategories = new Dictionary<uint, EquipRaceCategory>();
            _recipeCache = new Dictionary<uint, Recipe>();
            _classJobCategoryLookup = new Dictionary<uint, HashSet<uint>>();
            _armoireItems = new HashSet<uint>();
            flattenedRecipes = new Dictionary<uint, Dictionary<uint, uint>>();
            GilShopBuyable = new HashSet<uint>();
            RecipeLookupTable = new Dictionary<uint, HashSet<uint>>();
            CraftLookupTable = new Dictionary<uint, HashSet<uint>>();
            AddonNames = new Dictionary<uint, string>();
            CraftLevesItemLookup = new Dictionary<uint, uint>();
            CompanyCraftSequenceByItemIdLookup = new Dictionary<uint, uint>();
            EventItemCache = new Dictionary<uint, EventItem>();
            EquipRaceCategories = new Dictionary<uint, EquipRaceCategory>();
            EquipSlotCategories = new Dictionary<uint, EquipSlotCategory>();
            SearchCategory = new Dictionary<uint, ItemSearchCategory>();
            SortCategory = new Dictionary<uint, ItemSortCategory>();
            ItemUiCategory = new Dictionary<uint, ItemUICategory>();
            GatheringItems = new Dictionary<uint, GatheringItem>();
            GatheringItemPoints = new Dictionary<uint, GatheringItemPoint>();
            GilShopBuyable = new HashSet<uint>();
            GatheringItemPointLinks = new Dictionary<uint, uint>();
            GatheringItemsLinks = new Dictionary<uint, uint>();
            GatheringPoints = new Dictionary<uint, GatheringPoint>();
            GatheringPointsTransients = new Dictionary<uint, GatheringPointTransient>();
            RecipeLookupTable = new Dictionary<uint, HashSet<uint>>();
            RecipeCache = new Dictionary<uint, Recipe>();
            ClassJobCategoryLookup = new Dictionary<uint, HashSet<uint>>();
            CraftLevesItemLookup = new Dictionary<uint, uint>();
            _itemUiCategoriesFullyLoaded = false;
            _gatheringItemLinksCalculated = false;
            _gatheringItemPointLinksCalculated = false;
            _classJobCategoryLookupCalculated = false;
            _itemUiSearchFullyLoaded = false;
            _recipeLookUpCalculated = false;
            _companyCraftSequenceCalculated = false;
            _craftLevesItemLookupCalculated = false;
            _armoireLoaded = false;
        }

        public ExcelCache(DataManager dataManager) : this()
        {
            _dataManager = dataManager;
            Service.ExcelCache = this;
            //RetainerTaskEx.Run(() => RetainerTaskEx.Run(CalculateLookups));
            CalculateLookups();
        }

        public ExcelCache(GameData gameData) : this()
        {
            _gameData = gameData;
            Service.ExcelCache = this;
            //Need to fix this, basically stop the entire loading of the plugin until it's done then fire an event
            CalculateLookups();
            //RetainerTaskEx.Run(() => RetainerTaskEx.Run(CalculateLookups));
        }
        
        public bool FinishedLoading { get; private set; }

        private void CalculateLookups()
        {
            GcScripShopCategoryGrandCompany = GetSheet<GCScripShopCategory>().ToSingleLookup(c => c.RowId, c => c.GrandCompany.Row);
            GcShopGrandCompany = GetSheet<GCShop>().ToSingleLookup(c => c.GrandCompany.Row, c => c.RowId);
            GcScripShopItemToGcScripCategories = GetSheet<GCScripShopItem>().ToColumnLookup(c => c.RowId, c => c.SubRowId);
            GcScripShopToItem = GetSheet<GCScripShopItem>().ToSingleTupleLookup(c => (c.RowId, c.SubRowId), c => c.Item.Row);
            ItemGcScripShopLookup = GetSheet<GCScripShopItem>().ToColumnLookupTuple(c => c.Item.Row, c => (c.RowId, c.SubRowId));
            GilShopItemLookup =
                GetSheet<GilShopItem>().ToColumnLookup(c => c.RowId, c => c.Item.Row);
            ItemGilShopLookup =
                GetSheet<GilShopItem>().ToColumnLookup(c => c.Item.Row, c => c.RowId);
            GilShopGilShopItemLookup =
                GetSheet<GilShopItem>().ToColumnLookup(c => c.RowId, c => c.SubRowId);
            ItemGatheringItem =
                GetSheet<GatheringItem>().ToColumnLookup(c => (uint)c.Item, c => c.RowId);
            ItemGatheringTypes =
                GetSheet<GatheringPointBase>().ToColumnLookup(c => c.Item, c => c.GatheringType.Row);
            GatheringItemToGatheringItemPoint =
                GetSheet<GatheringItemPoint>().ToColumnLookup(c => c.RowId, c => c.GatheringPoint.Row);
            GatheringItemPointToGatheringPointBase =
                GetSheet<GatheringPoint>().ToColumnLookup(c => c.RowId, c => c.GatheringPointBase.Row);
            GatheringPointBaseToGatheringType =
                GetSheet<GatheringPointBase>().ToSingleLookup(c => c.RowId, c => c.GatheringType.Row, true, false);
            GatheringPointBaseToGatheringPoint = GetSheet<GatheringPoint>().ToColumnLookup(c => c.GatheringPointBase.Row, c => c.RowId, true, true);
            NpcToLevel =
                GetSheet<Level>().ToSingleLookup(c => c.Object,c => c.RowId, true, false);
            ShopToShopCollectionLookup =
                GetSheet<TopicSelect>()
                    .ToDictionary(c => c.RowId, c => c.UnkData4.Distinct().Select(d => d.Shop).Where(d => d!= 0).ToHashSet());
            InclusionShopToCategoriesLookup =
                GetSheet<InclusionShop>().ToDictionary(c => c.RowId, c => c.Category.Select(c => c.Row).Where(c => c != 0).Distinct().ToHashSet());
            InclusionShopCategoryToShopLookup =
                GetSheet<InclusionShopSeries>().ToColumnLookup(c => c.RowId, c => c.SpecialShop.Row);
            InclusionShopCategoryToShopSeriesLookup =
                GetSheet<InclusionShopSeries>().ToColumnLookup(c => c.RowId, c => c.SpecialShop.Row);
            FishParameters = GetSheet<FishParameter>().ToSingleLookup(c => (uint)c.Item, c => c.RowId);
            TomestoneLookup = GetSheet<TomestonesItem>().ToSingleLookup(c => c.RowId, c => c.Item.Row);
            ItemToAquariumFish = GetSheet<AquariumFish>().ToSingleLookup(c => c.Item.Row, c => c.RowId);
            Dictionary<uint, (uint, uint)> inspectionResults = new Dictionary<uint, (uint, uint)>();
            foreach (var inspection in GetSheet<HWDGathererInspectionEx>())
            {
                inspectionResults = inspection.GenerateInspectionResults(inspectionResults);
            }

            HwdInspectionResults = inspectionResults;
            //Special case for special shops because square can't pick a lane
            var itemSpecialShopResults = new Dictionary<uint, HashSet<uint>>();
            var itemSpecialShopCosts = new Dictionary<uint, HashSet<uint>>();
            var specialShopItemRewardCostLookup = new Dictionary<uint, HashSet<uint>>();
            var specialShopItemCostRewardLookup = new Dictionary<uint, HashSet<uint>>();
            foreach (var sheet in GetSheet<SpecialShopEx>())
            {
                foreach (var listing in sheet.ShopListings.ToList())
                {
                    foreach (var item in listing.Rewards)
                    {
                        if (!itemSpecialShopResults.ContainsKey(item.ItemEx.Row))
                        {
                            itemSpecialShopResults.Add(item.ItemEx.Row, new HashSet<uint>());
                        }

                        if (!itemSpecialShopResults[item.ItemEx.Row].Contains(sheet.RowId))
                        {
                            itemSpecialShopResults[item.ItemEx.Row].Add(sheet.RowId);
                        }
                        
                        if (!specialShopItemRewardCostLookup.ContainsKey(item.ItemEx.Row))
                        {
                            specialShopItemRewardCostLookup.Add(item.ItemEx.Row, new HashSet<uint>());
                        }
                        if (item.ItemEx.Row == 31794)
                        {
                            var a = "";
                        }
                        foreach (var costItem in listing.Costs)
                        {
                            if (!specialShopItemRewardCostLookup[item.ItemEx.Row].Contains(costItem.ItemEx.Row))
                            {
                                specialShopItemRewardCostLookup[item.ItemEx.Row].Add(costItem.ItemEx.Row);
                            }
                        }
                        
                    }

                    foreach (var item in listing.Costs)
                    {
                        if (!itemSpecialShopCosts.ContainsKey(item.ItemEx.Row))
                        {
                            itemSpecialShopCosts.Add(item.ItemEx.Row, new HashSet<uint>());
                        }

                        if (!itemSpecialShopCosts[item.ItemEx.Row].Contains(sheet.RowId))
                        {
                            itemSpecialShopCosts[item.ItemEx.Row].Add(sheet.RowId);
                        }
                        if (!specialShopItemCostRewardLookup.ContainsKey(item.ItemEx.Row))
                        {
                            specialShopItemCostRewardLookup.Add(item.ItemEx.Row, new HashSet<uint>());
                        }
                        foreach (var rewardItem in listing.Rewards)
                        {
                            if (!specialShopItemCostRewardLookup[item.ItemEx.Row].Contains(rewardItem.ItemEx.Row))
                            {
                                specialShopItemCostRewardLookup[item.ItemEx.Row].Add(rewardItem.ItemEx.Row);
                            }
                        }
                    }
                }
            }

            ItemSpecialShopResultLookup = itemSpecialShopResults;
            ItemSpecialShopCostLookup = itemSpecialShopCosts;
            SpecialShopItemRewardCostLookup = specialShopItemRewardCostLookup;
            SpecialShopItemCostRewardLookup = specialShopItemCostRewardLookup;
                
            _eNpcCollection = new ENpcCollection();
            _shopCollection = new ShopCollection();
            FinishedLoading = true;
        }



        public void Destroy()
        {
            _itemUiCategoriesFullyLoaded = false;
            _gatheringItemLinksCalculated = false;
            _gatheringItemPointLinksCalculated = false;
            _itemUiSearchFullyLoaded = false;
            _recipeLookUpCalculated = false;
            _companyCraftSequenceCalculated = false;
            _classJobCategoryLookupCalculated = false;
            _craftLevesItemLookupCalculated = false;
            _vendorLocationsCalculated = false;
        }

        public bool IsItemCraftLeve(uint itemId)
        {
            CalculateCraftLevesItemLookup();
            if (CraftLevesItemLookup.ContainsKey(itemId)) return true;

            return false;
        }

        public CraftLeve? GetCraftLevel(uint itemId)
        {
            CalculateCraftLevesItemLookup();
            if (CraftLevesItemLookup.ContainsKey(itemId))
                return GetSheet<CraftLeve>().GetRow(CraftLevesItemLookup[itemId]);

            return null;
        }

        public void CalculateCraftLevesItemLookup()
        {
            if (!_craftLevesItemLookupCalculated)
            {
                _craftLevesItemLookupCalculated = true;
                foreach (var craftLeve in GetSheet<CraftLeve>())
                foreach (var item in craftLeve.UnkData3)
                    if (!CraftLevesItemLookup.ContainsKey((uint)item.Item))
                        CraftLevesItemLookup.Add((uint)item.Item, craftLeve.RowId);
            }
        }
        public ExcelSheet<T> GetSheet<T>() where T : ExcelRow
        {
            if (_dataManager != null)
                return _dataManager.Excel.GetSheet<T>()!;
            if (_gameData != null) return _gameData.GetExcelSheet<T>()!;

            throw new Exception("You must initialise the cache with a data manager instance or game data instance");
        }

        public T? GetFile<T>(string path) where T : FileResource
        {
            if (_dataManager != null)
                return _dataManager.GetFile<T>(path)!;
            if (_gameData != null) return _gameData.GetFile<T>(path)!;

            throw new Exception("You must initialise the cache with a data manager instance or game data instance");
        }

        public TripleTriadCard? GetTripleTriadCard(uint cardId)
        {
            return GetSheet<TripleTriadCard>().GetRow(cardId);
        }

        public EventItem? GetEventItem(uint itemId)
        {
            if (!EventItemCache.ContainsKey(itemId))
            {
                var item = GetSheet<EventItem>().GetRow(itemId);
                if (item == null) return null;

                EventItemCache[itemId] = item;
            }

            return EventItemCache[itemId];
        }

        public Recipe? GetRecipe(uint recipeId)
        {
            if (!RecipeCache.ContainsKey(recipeId))
            {
                var recipe = GetSheet<Recipe>().GetRow(recipeId);
                if (recipe == null) return null;

                RecipeCache[recipeId] = recipe;
            }

            return RecipeCache[recipeId];
        }

        private Dictionary<uint, uint> GetFlattenedItemRecipeLoop(Dictionary<uint, uint> itemIds, uint itemId,
            uint quantity)
        {
            var recipes = GetItemRecipes(itemId);
            foreach (var recipe in recipes)
            foreach (var ingredient in recipe.UnkData5)
                if (ingredient.ItemIngredient != 0)
                {
                    if (!itemIds.ContainsKey((uint)ingredient.ItemIngredient))
                        itemIds.Add((uint)ingredient.ItemIngredient, 0);

                    itemIds[(uint)ingredient.ItemIngredient] += ingredient.AmountIngredient * quantity;

                    if (CanCraftItem((uint)ingredient.ItemIngredient))
                        GetFlattenedItemRecipeLoop(itemIds, (uint)ingredient.ItemIngredient, quantity);
                }

            if (recipes.Count == 0)
            {
                if (!_companyCraftSequenceCalculated) CalculateCompanyCraftSequenceByItemId();

                if (CompanyCraftSequenceByItemIdLookup.ContainsKey(itemId))
                {
                    //Might need to split into parts at some point
                    var companyCraftSequence = GetSheet<CompanyCraftSequence>()
                        .GetRow(CompanyCraftSequenceByItemIdLookup[itemId]);
                    if (companyCraftSequence != null)
                        foreach (var lazyPart in companyCraftSequence.CompanyCraftPart)
                        {
                            var part = lazyPart.Value;
                            if (part != null)
                                foreach (var lazyProcess in part.CompanyCraftProcess)
                                {
                                    var process = lazyProcess.Value;
                                    if (process != null)
                                        foreach (var supplyItem in process.UnkData0)
                                        {
                                            var actualItem = GetSheet<CompanyCraftSupplyItem>()
                                                .GetRow(supplyItem.SupplyItem);
                                            if (actualItem != null)
                                                if (actualItem.Item.Row != 0)
                                                {
                                                    if (!itemIds.ContainsKey(actualItem.Item.Row))
                                                        itemIds.Add(actualItem.Item.Row, 0);

                                                    itemIds[actualItem.Item.Row] += (uint)supplyItem.SetQuantity *
                                                        supplyItem.SetsRequired * quantity;

                                                    GetFlattenedItemRecipeLoop(itemIds, actualItem.Item.Row, quantity);
                                                }
                                        }
                                }
                        }
                }
            }

            return itemIds;
        }

        public Dictionary<uint, uint> GetFlattenedItemRecipe(uint itemId, bool includeSelf = false,
            uint quantity = 1)
        {
            if (flattenedRecipes.ContainsKey(itemId))
            {
                if (includeSelf)
                {
                    var flattenedItemRecipe = flattenedRecipes[itemId].ToDictionary(c => c.Key, c => c.Value);
                    flattenedItemRecipe.Add(itemId, quantity);
                    return flattenedItemRecipe;
                }

                return flattenedRecipes[itemId];
            }

            var flattenedItemRecipeLoop = GetFlattenedItemRecipeLoop(new Dictionary<uint, uint>(), itemId, quantity);
            flattenedRecipes.Add(itemId, flattenedItemRecipeLoop);
            if (includeSelf)
            {
                var flattenedItemRecipe = flattenedRecipes[itemId].ToDictionary(c => c.Key, c => c.Value);
                flattenedItemRecipe.Add(itemId, 1);
            }

            return flattenedItemRecipeLoop;
        }

        public bool IsCompanyCraft(uint itemId)
        {
            if (itemId == 0) return false;

            if (!_companyCraftSequenceCalculated) CalculateCompanyCraftSequenceByItemId();

            return CompanyCraftSequenceByItemIdLookup.ContainsKey(itemId);
        }

        public CompanyCraftSequence? GetCompanyCraftSequenceByItemId(uint itemId)
        {
            if (itemId == 0) return null;

            if (!_companyCraftSequenceCalculated) CalculateCompanyCraftSequenceByItemId();

            if (CompanyCraftSequenceByItemIdLookup.ContainsKey(itemId))
                return GetSheet<CompanyCraftSequence>()
                    .GetRow(CompanyCraftSequenceByItemIdLookup[itemId]);

            return null;
        }

        public void CalculateCompanyCraftSequenceByItemId()
        {
            if (!_companyCraftSequenceCalculated)
            {
                _companyCraftSequenceCalculated = true;
                foreach (var companyCraftSequence in GetSheet<CompanyCraftSequence>())
                    if (!CompanyCraftSequenceByItemIdLookup.ContainsKey(companyCraftSequence.ResultItem.Row))
                        CompanyCraftSequenceByItemIdLookup.Add(companyCraftSequence.ResultItem.Row,
                            companyCraftSequence.RowId);
            }
        }

        public List<Recipe> GetItemRecipes(uint itemId)
        {
            if (itemId == 0) return new List<Recipe>();

            if (!_recipeLookUpCalculated) CalculateRecipeLookup();

            var recipes = new List<Recipe>();
            if (RecipeLookupTable.ContainsKey(itemId))
                foreach (var lookup in RecipeLookupTable[itemId])
                {
                    var recipe = GetRecipe(lookup);
                    if (recipe != null) recipes.Add(recipe);
                }

            return recipes;
        }

        public ItemUICategory? GetItemUICategory(uint itemId)
        {
            if (!ItemUiCategory.ContainsKey(itemId))
            {
                var item = GetSheet<ItemUICategory>().GetRow(itemId);
                if (item == null) return null;

                ItemUiCategory[itemId] = item;
            }

            return ItemUiCategory[itemId];
        }

        public Dictionary<uint, ItemUICategory> GetAllItemUICategories()
        {
            if (!_itemUiCategoriesFullyLoaded)
            {
                _itemUiCategory = GetSheet<ItemUICategory>().ToDictionary(c => c.RowId);
                _itemUiCategoriesFullyLoaded = true;
            }

            return _itemUiCategory;
        }

        public Dictionary<uint, ItemSearchCategory> GetAllItemSearchCategories()
        {
            if (!_itemUiSearchFullyLoaded)
            {
                _itemSearchCategory = GetSheet<ItemSearchCategory>().ToDictionary(c => c.RowId);
                _itemUiSearchFullyLoaded = true;
            }

            return _itemSearchCategory;
        }

        public ItemSearchCategory? GetItemSearchCategory(uint itemId)
        {
            if (!SearchCategory.ContainsKey(itemId))
            {
                var item = GetSheet<ItemSearchCategory>().GetRow(itemId);
                if (item == null) return null;

                SearchCategory[itemId] = item;
            }

            return SearchCategory[itemId];
        }

        public ItemSortCategory? GetItemSortCategory(uint itemId)
        {
            if (!SortCategory.ContainsKey(itemId))
            {
                var item = GetSheet<ItemSortCategory>().GetRow(itemId);
                if (item == null) return null;

                SortCategory[itemId] = item;
            }

            return SortCategory[itemId];
        }

        public EquipSlotCategory? GetEquipSlotCategory(uint itemId)
        {
            if (!EquipSlotCategories.ContainsKey(itemId))
            {
                var item = GetSheet<EquipSlotCategory>().GetRow(itemId);
                if (item == null) return null;

                EquipSlotCategories[itemId] = item;
            }

            return EquipSlotCategories[itemId];
        }

        public void CalculateRecipeLookup()
        {
            if (!_recipeLookUpCalculated)
            {
                _recipeLookUpCalculated = true;
                foreach (var recipe in GetSheet<Recipe>())
                {
                    if (recipe.ItemResult.Row != 0)
                    {
                        if (!RecipeLookupTable.ContainsKey(recipe.ItemResult.Row))
                            RecipeLookupTable.Add(recipe.ItemResult.Row, new HashSet<uint>());

                        RecipeLookupTable[recipe.ItemResult.Row].Add(recipe.RowId);
                        foreach (var item in recipe.UnkData5)
                        {
                            if (item.ItemIngredient != 0)
                            {
                                if (!CraftLookupTable.ContainsKey((uint)item.ItemIngredient))
                                    CraftLookupTable.Add((uint)item.ItemIngredient, new HashSet<uint>());

                                var hashSet = CraftLookupTable[(uint)item.ItemIngredient];
                                if (!hashSet.Contains(recipe.ItemResult.Row)) hashSet.Add(recipe.ItemResult.Row);
                            }
                        }
                    }
                }
            }
        }

        public void CalculateArmoireItems()
        {
            if (!_armoireLoaded)
            {
                _armoireLoaded = true;
                foreach (var armoireItem in GetSheet<Cabinet>())
                    if (!_armoireItems.Contains(armoireItem.Item.Row))
                        _armoireItems.Add(armoireItem.Item.Row);
            }
        }

        public bool IsItemEquippableBy(uint classJobCategory, uint classJobId)
        {
            CalculateClassJobCategoryLookup();
            if (!_classJobCategoryLookup.ContainsKey(classJobCategory)) return false;

            if (!_classJobCategoryLookup[classJobCategory].Contains(classJobId)) return false;

            return true;
        }

        public void CalculateClassJobCategoryLookup()
        {
            if (!_classJobCategoryLookupCalculated)
            {
                var classJobMap = new Dictionary<string, uint>();
                foreach (var classJob in GetSheet<ClassJob>())
                    if (!classJobMap.ContainsKey(classJob.Abbreviation))
                        classJobMap[classJob.Abbreviation] = classJob.RowId;

                _classJobCategoryLookupCalculated = true;
                var classJobCategoryMap = new Dictionary<uint, HashSet<uint>>();
                var propertyInfos = typeof(ClassJobCategory).GetProperties().Where(c => c.PropertyType == typeof(bool))
                    .ToList();

                foreach (var classJobCategory in GetSheet<ClassJobCategory>())
                {
                    //Dont hate me
                    var map = new HashSet<uint>();
                    foreach (var prop in propertyInfos)
                    {
                        var parsed = prop.GetValue(classJobCategory, null);
                        if (parsed is bool b && (bool?)b == true)
                            if (classJobMap.ContainsKey(prop.Name))
                            {
                                var classJobRowId = classJobMap[prop.Name];
                                if (!map.Contains(classJobRowId)) map.Add(classJobRowId);
                            }
                    }

                    classJobCategoryMap[classJobCategory.RowId] = map;
                }

                _classJobCategoryLookup = classJobCategoryMap;
            }
        }

        public bool IsItemAvailableAtTimedNode(uint itemId)
        {
            if (!_gatheringItemLinksCalculated) CalculateGatheringItemLinks();

            if (!_gatheringItemPointLinksCalculated) CalculateGatheringItemPointLinks();

            if (GatheringItemsLinks.ContainsKey(itemId))
            {
                var gatheringItemId = GatheringItemsLinks[itemId];
                if (GatheringItemPointLinks.ContainsKey(gatheringItemId))
                {
                    var gatheringPointId = GatheringItemPointLinks[gatheringItemId];
                    var gatheringPointTransient = GetGatheringPointTransient(gatheringPointId);
                    if (gatheringPointTransient != null)
                        return gatheringPointTransient.GatheringRarePopTimeTable.Row != 0;
                }
            }

            return false;
        }

        public void Dispose()
        {
        }
    }
}