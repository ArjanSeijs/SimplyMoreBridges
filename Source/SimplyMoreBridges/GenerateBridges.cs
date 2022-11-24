using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace SimplyMoreBridges;

public class GenerateBridges
{
    public static void Prefix()
    {
        try
        {
            var terrainDefs = DefDatabase<TerrainDef>.AllDefs
                .Concat(TerrainDefGenerator_Carpet.ImpliedTerrainDefs())
                .Where(td => td.IsFloor && !td.bridge && td.BuildableByPlayer)
                .ToList();

            var dropdownDict = new Dictionary<string, DesignatorDropdownGroupDef>();
            foreach (var td in terrainDefs)
            {
                AddDef(td, BridgeType.Heavy, dropdownDict);
                AddDef(td, BridgeType.Deep, dropdownDict);
            }
        }
        catch (Exception e)
        {
            Log.Error("[Simply More More Bridges] Failed Generating");
            Log.Error(e.ToString());
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="td"></param>
    /// <param name="bridgeType"></param>
    /// <param name="dropdownDict"></param>
    private static void AddDef(TerrainDef td, BridgeType bridgeType,
        Dictionary<string, DesignatorDropdownGroupDef> dropdownDict)
    {
        try
        {
            // DefGenerator.AddImpliedDef(GenerateBridgeDef(td, bridgeType, dropdownDict));
            DefGenerator.AddImpliedDef(GenerateBridgeDef(td, bridgeType, dropdownDict));
        }
        catch (Exception e)
        {
            Log.Error($"[Simply More More Bridges] {td.defName} ({bridgeType.DefName()})");
            Log.Error(e.Message);
            Log.Error(e.ToString());
            Log.Error(e.StackTrace);
            throw;
        }
    }

    private static TerrainDef GenerateBridgeDef(TerrainDef baseDef, BridgeType bridgeType,
        Dictionary<string, DesignatorDropdownGroupDef> dropdownDict)
    {
        var bridgeDef = GetNewBridge(baseDef, bridgeType);

        CopyFields(baseDef, bridgeDef);
        SetTerrainAffordance(bridgeType, baseDef, bridgeDef);
        SetDropdownDef(baseDef, bridgeDef,bridgeType, dropdownDict);
        SetCosts(baseDef, bridgeDef);
        const float hitPoints = 300f;

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
        bridgeDef.statBases.Add(new StatModifier {stat = StatDefOf.MaxHitPoints, value = hitPoints});
        bridgeDef.tags = baseDef.tags?.ToList();
        return bridgeDef;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="baseDef"></param>
    /// <param name="bridgeDef"></param>
    private static void SetCosts(TerrainDef baseDef, TerrainDef bridgeDef)
    {
        bridgeDef.costList = baseDef.costList?.ToList() ?? new List<ThingDefCountClass>();
        bridgeDef.costList.Add(new ThingDefCountClass(ThingDef.Named("Steel"), GetCustomCost(10)));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="bridgeType"></param>
    /// <param name="baseDef"></param>
    /// <param name="bridgeDef"></param>
    private static void SetTerrainAffordance(BridgeType bridgeType, TerrainDef baseDef, TerrainDef bridgeDef)
    {
        var statValue = 1000 + baseDef.GetStatValueAbstract(StatDefOf.WorkToBuild);
        switch (bridgeType)
        {
            // case BridgeType.Wooden
            case BridgeType.Heavy:
                bridgeDef.terrainAffordanceNeeded = TerrainAffordanceDefOf.Bridgeable;
                bridgeDef.statBases.Add(new StatModifier {stat = StatDefOf.WorkToBuild, value = statValue});
                bridgeDef.researchPrerequisites = new List<ResearchProjectDef>
                {
                    DefDatabase<ResearchProjectDef>.GetNamedSilentFail("HeavyBridges")
                };
                break;
            case BridgeType.Deep:
                bridgeDef.terrainAffordanceNeeded = TerrainAffordanceDefOf.BridgeableDeep;
                bridgeDef.statBases.Add(new StatModifier {stat = StatDefOf.WorkToBuild, value = statValue + 500});
                bridgeDef.researchPrerequisites = new List<ResearchProjectDef>
                {
                    DefDatabase<ResearchProjectDef>.GetNamedSilentFail("DeepWaterBridges")
                };
                break;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="baseDef"></param>
    /// <param name="bridgeType"></param>
    /// <returns></returns>
    private static TerrainDef GetNewBridge(TerrainDef baseDef, BridgeType bridgeType)
    {
        string defName = bridgeType.DefName() + baseDef.defName;
        string label = bridgeType.Label() + baseDef.label;

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
            statBases = new List<StatModifier>
            {
                new StatModifier {stat = StatDefOf.Flammability, value = 0}
            },
            bridge = true
        };
        return bridgeDef;
    }

    /// <summary>
    /// 
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
        bridgeDef.uiIcon = baseDef.uiIcon;
        bridgeDef.uiIconAngle = baseDef.uiIconAngle;
        bridgeDef.uiIconColor = baseDef.uiIconColor;
        bridgeDef.uiIconForStackCount = baseDef.uiIconForStackCount;
        bridgeDef.uiIconOffset = baseDef.uiIconOffset;
        bridgeDef.uiIconPath = baseDef.uiIconPath;
        bridgeDef.uiIconPathsStuff = baseDef.uiIconPathsStuff?.ToList(); 
        bridgeDef.uiOrder = baseDef.uiOrder;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="baseDef"></param>
    /// <param name="bridgeDef"></param>
    /// <param name="bridgeType"></param>
    /// <param name="dropdownDict"></param>
    private static void SetDropdownDef(TerrainDef baseDef, TerrainDef bridgeDef, BridgeType bridgeType,
        Dictionary<string, DesignatorDropdownGroupDef> dropdownDict)
    {
        if (baseDef.designatorDropdown == null) return;
        var baseDropdown = baseDef.designatorDropdown;
        var bridgeDropdownDefName = bridgeType.DefName() + baseDropdown.defName;

        if (!dropdownDict.ContainsKey(bridgeDropdownDefName))
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
            dropdownDict.Add(bridgeDropdownDefName, newBridgeDropdown);
        }

        var bridgeDropdown = dropdownDict[bridgeDropdownDefName];
        bridgeDef.designatorDropdown = bridgeDropdown;
    }
    private static int GetCustomCost(int originalCost)
    {
        var recountedCost = originalCost * LoadedModManager.GetMod<SimplyMoreBridgesMod>()
            .GetSettings<SimplyMoreBridgesSettings>().CostPercent;
        return Convert.ToInt32(Math.Ceiling(recountedCost));
    }
}