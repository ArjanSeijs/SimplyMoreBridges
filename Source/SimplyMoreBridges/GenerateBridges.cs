﻿using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace SimplyMoreBridges;

public class GenerateBridges
{
    public static readonly Dictionary<string, DesignatorDropdownGroupDef> DropDownDict = new();
    public static readonly List<TerrainDef> BridgeDefList = new();

    public static void Prefix()
    {
        try
        {
            AddBridgeDefs();
            AddIdeologyDesignatorGroups();
        }
        catch (Exception e)
        {
            Log.Error("[Simply More Bridges Improved] Failed Generating");
            Log.Error(e.ToString());
            Log.Error(e.InnerException?.ToString());
        }

        try
        {
            if (LoadedModManager
                .GetMod<SimplyMoreBridgesMod>()
                .GetSettings<SimplyMoreBridgesSettings>().BackwardsCompatibility)
            {
                GenerateBridgesBackCompat.Prefix();
            }
        }
        catch (Exception e)
        {
            Log.Error("[Simply More Bridges Improved] Backwards Compat Failed Somehow");
            Log.Error(e.ToString());
            Log.Error(e.InnerException?.ToString());
        }
    }

    /// <summary>
    /// Get all terrains that should be turned into bridges.
    /// </summary>
    private static void AddBridgeDefs()
    {
        var terrainDefs = DefDatabase<TerrainDef>.AllDefs
            .Concat(TerrainDefGenerator_Carpet.ImpliedTerrainDefs())
            .Where(Include)
            .ToList();

        foreach (var td in terrainDefs)
        {
            if (IsWooden(td))
            {
                AddDef(td, BridgeType.Wooden);
            }

            AddDef(td, BridgeType.Heavy);
            AddDef(td, BridgeType.Deep);
        }
    }

    /// <summary>
    /// Adds designator groups to StyleCategoryDefs of generated StyleCategory Bridges
    /// </summary>
    private static void AddIdeologyDesignatorGroups()
    {
        var styleCategoryDefs = DefDatabase<StyleCategoryDef>.AllDefsListForReading;
        foreach (var styleCategoryDef in styleCategoryDefs)
        {
            if (styleCategoryDef.addDesignatorGroups == null) continue;
            foreach (var groupDef in styleCategoryDef.addDesignatorGroups.ToList())
            {
                AddGroupToStyleIfExists(BridgeType.Wooden, groupDef, styleCategoryDef);
                AddGroupToStyleIfExists(BridgeType.Heavy, groupDef, styleCategoryDef);
                AddGroupToStyleIfExists(BridgeType.Deep, groupDef, styleCategoryDef);
            }
        }
    }

    /// <summary>
    /// Adds designator group to StyleCategoryDef of generated StyleCategory Bridges
    /// </summary>
    /// <param name="bridgeType"></param>
    /// <param name="groupDef"></param>
    /// <param name="styleCategoryDef"></param>
    private static void AddGroupToStyleIfExists(BridgeType bridgeType, DesignatorDropdownGroupDef groupDef,
        StyleCategoryDef styleCategoryDef)
    {
        var groupDefDefName = bridgeType.DefName() + groupDef.defName;
        if (DropDownDict.ContainsKey(groupDefDefName))
        {
            styleCategoryDef.addDesignatorGroups.Add(DropDownDict[groupDefDefName]);
        }
    }

    /// <summary>
    /// Whether this terrainDef should be turned into a bridge.
    /// </summary>
    /// <param name="td"></param>
    /// <returns></returns>
    private static bool Include(TerrainDef td)
    {
        return td.IsFloor && !td.bridge && td.BuildableByPlayer;
    }

    /// <summary>
    /// Wheter this terrainDef should create a woorden bridge.
    /// </summary>
    /// <param name="td"></param>
    /// <returns></returns>
    private static bool IsWooden(TerrainDef td)
    {
        return td.costList is { Count: > 0 } &&
               td.costList[0].thingDef is { stuffProps.categories: { } } &&
               td.costList[0].thingDef.stuffProps.categories.Contains(StuffCategoryDefOf.Woody);
    }

    /// <summary>
    /// Generates a bridge of type bridgeType of this terrainDef and adds it to Def Database
    /// </summary>
    /// <param name="td"></param>
    /// <param name="bridgeType"></param>
    public static void AddDef(TerrainDef td, BridgeType bridgeType)
    {
        try
        {
            var bridgeDef = GenerateBridgeDef(td, bridgeType);
            BridgeDefList.Add(bridgeDef);
            DefGenerator.AddImpliedDef(bridgeDef);
        }
        catch (Exception e)
        {
            throw new Exception($"[Simply More Bridges Improved] {td.defName} ({bridgeType.DefName()})", e);
        }
    }

    /// <summary>
    /// Generates a bridge of type bridgeType of this terrainDef
    /// </summary>
    /// <param name="baseDef"></param>
    /// <param name="bridgeType"></param>
    /// <returns></returns>
    private static TerrainDef GenerateBridgeDef(TerrainDef baseDef, BridgeType bridgeType)
    {
        var bridgeDef = GetNewBridge(baseDef, bridgeType);

        CopyFields(baseDef, bridgeDef);
        SetBridgeStats(baseDef, bridgeDef, bridgeType);
        SetDropdownDef(baseDef, bridgeDef, bridgeType);
        SetCosts(baseDef, bridgeDef, bridgeType);

        if (baseDef.researchPrerequisites != null)
        {
            bridgeDef.researchPrerequisites.AddRange(baseDef.researchPrerequisites);
        }

        if (baseDef.statBases != null)
        {
            bridgeDef.statBases.AddRange(baseDef.statBases
                .Where(baseStat => !bridgeDef.StatBaseDefined(baseStat.stat)));
        }

        bridgeDef.texturePath = baseDef.texturePath;

        var hitPoints = baseDef.GetStatValueAbstract(StatDefOf.MaxHitPoints) + 50f;
        bridgeDef.statBases.Add(new StatModifier { stat = StatDefOf.MaxHitPoints, value = hitPoints });
        bridgeDef.tags = baseDef.tags?.ToList();
        return bridgeDef;
    }

    /// <summary>
    /// Create a costlist based on the baseDef for this bridgeType adjusted for the custom cost setting.
    /// </summary>
    /// <param name="baseDef"></param>
    /// <param name="bridgeDef"></param>
    /// <param name="bridgeType"></param>
    private static void SetCosts(TerrainDef baseDef, TerrainDef bridgeDef, BridgeType bridgeType)
    {
        bridgeDef.costList = baseDef.costList?.ToList() ?? new List<ThingDefCountClass>();
        if (bridgeType == BridgeType.Wooden)
        {
            bridgeDef.costList = bridgeDef.costList
                .Select(cost =>
                    new ThingDefCountClass(cost.thingDef, GetCustomCost(2 * cost.count) + cost.count))
                .ToList();
        }
        else
        {
            bridgeDef.costList.Add(new ThingDefCountClass(ThingDef.Named("Steel"), GetCustomCost(5)));
        }
    }

    /// <summary>
    /// Set stats based on bridgeType
    /// </summary>
    /// <param name="baseDef"></param>
    /// <param name="bridgeDef"></param>
    /// <param name="bridgeType"></param>
    private static void SetBridgeStats(TerrainDef baseDef, TerrainDef bridgeDef, BridgeType bridgeType)
    {
        var statValue = baseDef.GetStatValueAbstract(StatDefOf.WorkToBuild);
        switch (bridgeType)
        {
            case BridgeType.Wooden:
                bridgeDef.terrainAffordanceNeeded = TerrainAffordanceDefOf.Bridgeable;
                bridgeDef.statBases.Add(new StatModifier { stat = StatDefOf.WorkToBuild, value = 1000 });
                bridgeDef.statBases.Add(new StatModifier { stat = StatDefOf.Flammability, value = 0.8f });
                bridgeDef.researchPrerequisites = new List<ResearchProjectDef>();
                break;
            case BridgeType.Heavy:
                bridgeDef.terrainAffordanceNeeded = TerrainAffordanceDefOf.Bridgeable;
                bridgeDef.statBases.Add(new StatModifier { stat = StatDefOf.WorkToBuild, value = 1000 + statValue });
                bridgeDef.statBases.Add(new StatModifier { stat = StatDefOf.Flammability, value = 0 });
                bridgeDef.researchPrerequisites = new List<ResearchProjectDef>
                {
                    DefDatabase<ResearchProjectDef>.GetNamedSilentFail("HeavyBridges")
                };
                break;
            case BridgeType.Deep:
                bridgeDef.terrainAffordanceNeeded = TerrainAffordanceDefOf.BridgeableDeep;
                bridgeDef.statBases.Add(new StatModifier { stat = StatDefOf.WorkToBuild, value = 1500 + statValue });
                bridgeDef.statBases.Add(new StatModifier { stat = StatDefOf.Flammability, value = 0 });
                bridgeDef.researchPrerequisites = new List<ResearchProjectDef>
                {
                    DefDatabase<ResearchProjectDef>.GetNamedSilentFail("DeepWaterBridges")
                };
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(bridgeType), bridgeType, null);
        }
    }

    /// <summary>
    /// Base properties for the new Bridge.
    /// </summary>
    /// <param name="baseDef"></param>
    /// <param name="bridgeType"></param>
    /// <returns></returns>
    private static TerrainDef GetNewBridge(TerrainDef baseDef, BridgeType bridgeType)
    {
        var defName = bridgeType.DefName() + baseDef.defName;
        var label = bridgeType.Label() + baseDef.label;

        var bridgeDef = new TerrainDef
        {
            defName = defName,
            label = label,
            edgeType = TerrainDef.TerrainEdgeType.Hard,
            renderPrecedence = 400,
            layerable = true,
            affordances =
                new List<TerrainAffordanceDef>
                {
                    RimWorld.TerrainAffordanceDefOf.Light,
                    RimWorld.TerrainAffordanceDefOf.Medium,
                    RimWorld.TerrainAffordanceDefOf.Heavy
                },
            designationCategory = DesignationCategoryDefOf.Structure,
            fertility = 0,
            constructEffect = EffecterDefOf.ConstructMetal,
            destroyBuildingsOnDestroyed = true,
            destroyEffect =
                DefDatabase<EffecterDef>.GetNamedSilentFail("Bridge_Collapse"),
            destroyEffectWater =
                DefDatabase<EffecterDef>.GetNamedSilentFail("Bridge_CollapseWater"),
            description =
                "A flat surface of the chosen material on supportive beams which can be built over water. You can even build heavy structures on these bridges, but be careful, they are still fragile. If a bridge falls, buildings on top of it fall as well.",
            resourcesFractionWhenDeconstructed = 0,
            destroyOnBombDamageThreshold = 40,
            statBases = new List<StatModifier>(),
            bridge = true
        };
        return bridgeDef;
    }

    /// <summary>
    /// Copies fields from the baseDef to the bridgeDef
    /// </summary>
    /// <param name="baseDef"></param>
    /// <param name="bridgeDef"></param>
    private static void CopyFields(TerrainDef baseDef, TerrainDef bridgeDef)
    {
        bridgeDef.autoRebuildable = false;
        bridgeDef.bridge = true;
        bridgeDef.changeable = false;
        bridgeDef.natural = false;

        bridgeDef.artisticSkillPrerequisite = baseDef.artisticSkillPrerequisite;
        bridgeDef.avoidWander = baseDef.avoidWander;
        bridgeDef.buildingPrerequisites = baseDef.buildingPrerequisites?.ToList();
        bridgeDef.canGenerateDefaultDesignator = baseDef.canGenerateDefaultDesignator;
        bridgeDef.color = baseDef.color;
        bridgeDef.colorDef = baseDef.colorDef;
        bridgeDef.colorPerStuff = baseDef.colorPerStuff?.ToList();
        bridgeDef.constructionSkillPrerequisite = baseDef.constructionSkillPrerequisite;
        bridgeDef.dominantStyleCategory = baseDef.dominantStyleCategory;
        bridgeDef.extinguishesFire = baseDef.extinguishesFire;
        bridgeDef.extraDeteriorationFactor = baseDef.extraDeteriorationFactor;
        bridgeDef.extraDraftedPerceivedPathCost = baseDef.extraDraftedPerceivedPathCost;
        bridgeDef.extraNonDraftedPerceivedPathCost = baseDef.extraNonDraftedPerceivedPathCost;
        bridgeDef.fertility = baseDef.fertility;
        bridgeDef.filthAcceptanceMask = baseDef.filthAcceptanceMask;
        bridgeDef.generated = baseDef.generated;
        bridgeDef.generatedFilth = baseDef.generatedFilth;
        bridgeDef.holdSnow = baseDef.holdSnow;
        bridgeDef.ideoBuilding = baseDef.ideoBuilding;
        bridgeDef.ignoreConfigErrors = baseDef.ignoreConfigErrors;
        bridgeDef.ignoreIllegalLabelCharacterConfigError = baseDef.ignoreIllegalLabelCharacterConfigError;
        bridgeDef.index = baseDef.index;
        bridgeDef.installBlueprintDef = baseDef.installBlueprintDef;
        bridgeDef.isAltar = baseDef.isAltar;
        bridgeDef.isPaintable = baseDef.isPaintable;
        bridgeDef.maxTechLevelToBuild = baseDef.maxTechLevelToBuild;
        bridgeDef.minTechLevelToBuild = baseDef.minTechLevelToBuild;
        bridgeDef.modContentPack = baseDef.modContentPack;
        bridgeDef.modExtensions = baseDef.modExtensions?.ToList();
        bridgeDef.passability = baseDef.passability;
        bridgeDef.pathCost = baseDef.pathCost;
        bridgeDef.pathCostIgnoreRepeat = baseDef.pathCostIgnoreRepeat;
        bridgeDef.pollutedTexturePath = baseDef.pollutedTexturePath;
        bridgeDef.pollutionCloudColor = baseDef.pollutionCloudColor;
        bridgeDef.pollutionColor = baseDef.pollutionColor;
        bridgeDef.pollutionOverlayScale = baseDef.pollutionOverlayScale;
        bridgeDef.pollutionOverlayScrollSpeed = baseDef.pollutionOverlayScrollSpeed;
        bridgeDef.pollutionOverlayTexturePath = baseDef.pollutionOverlayTexturePath;
        bridgeDef.pollutionShaderType = baseDef.pollutionShaderType;
        bridgeDef.pollutionTintColor = baseDef.pollutionTintColor;
        bridgeDef.repairEffect = baseDef.repairEffect;
        bridgeDef.scatterType = baseDef.scatterType;
        bridgeDef.stuffCategories = baseDef.stuffCategories?.ToList();
        bridgeDef.stuffCategorySummary = baseDef.stuffCategorySummary;
        bridgeDef.takeFootprints = baseDef.takeFootprints;
        bridgeDef.takeSplashes = baseDef.takeSplashes;
        bridgeDef.tools = baseDef.tools;
        bridgeDef.traversedThought = baseDef.traversedThought;
        bridgeDef.uiIconAngle = baseDef.uiIconAngle;
        bridgeDef.uiIconColor = baseDef.uiIconColor;
        bridgeDef.uiIconForStackCount = baseDef.uiIconForStackCount;
        bridgeDef.uiIconOffset = baseDef.uiIconOffset;
        bridgeDef.uiIconPath = baseDef.uiIconPath;
        bridgeDef.uiIconPathsStuff = baseDef.uiIconPathsStuff?.ToList();
        bridgeDef.uiOrder = baseDef.uiOrder;
    }

    /// <summary>
    /// Create a dropdown menu if it does not exist for the bridge.
    /// Or get an already existing dropdown menu.
    /// </summary>
    /// <param name="baseDef"></param>
    /// <param name="bridgeDef"></param>
    /// <param name="bridgeType"></param>
    private static void SetDropdownDef(TerrainDef baseDef, TerrainDef bridgeDef, BridgeType bridgeType)
    {
        if (baseDef.designatorDropdown == null) return;
        var baseDropdown = baseDef.designatorDropdown;
        var bridgeDropdownDefName = bridgeType.DefName() + baseDropdown.defName;

        if (!DropDownDict.ContainsKey(bridgeDropdownDefName))
        {
            var newBridgeDropdown = new DesignatorDropdownGroupDef
            {
                defName = bridgeDropdownDefName,
                label = baseDropdown.label,
                iconSource = baseDropdown.iconSource,
                useGridMenu = baseDropdown.useGridMenu,
                includeEyeDropperTool = baseDropdown.includeEyeDropperTool,
                description = baseDropdown.description + $" ({bridgeType.Label()})"
            };
            DropDownDict.Add(bridgeDropdownDefName, newBridgeDropdown);
        }

        var bridgeDropdown = DropDownDict[bridgeDropdownDefName];
        bridgeDef.designatorDropdown = bridgeDropdown;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="originalCost"></param>
    /// <returns></returns>
    private static int GetCustomCost(int originalCost)
    {
        var recountedCost = originalCost * LoadedModManager.GetMod<SimplyMoreBridgesMod>()
            .GetSettings<SimplyMoreBridgesSettings>().CostPercent;
        return Convert.ToInt32(Math.Ceiling(recountedCost));
    }
}