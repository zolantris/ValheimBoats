namespace ValheimVehicles.Prefabs;

/**
 * @todo register translatable pieceName and pieceDescription based on these names for easy lookups
 */
public static class PrefabNames
{
  public enum PrefabSizeVariant
  {
    Two,
    Four,
  }

  public const string MBRaft = "MBRaft";
  public static int m_raftHash = MBRaft.GetStableHashCode();
  public const string Tier1RaftMastName = "MBRaftMast";
  public const string Tier2RaftMastName = "MBKarveMast";
  public const string Tier3RaftMastName = "MBVikingShipMast";
  public const string Tier4RaftMastName = $"{ValheimVehiclesPrefix}_DrakkalMast";
  public const string Tier1CustomSailName = "MBSail";
  public const string BoardingRamp = "MBBoardingRamp";
  public const string BoardingRampWide = "MBBoardingRamp_Wide";
  public const string ValheimVehiclesShipName = "ValheimVehicles_Ship";
  public const string WaterVehicleFloatCollider = "VehicleShip_FloatCollider";
  public const string WaterVehicleBlockingCollider = "VehicleShip_BlockingCollider";
  public const string WaterVehicleOnboardCollider = "VehicleShip_OnboardTriggerCollider";

  public const string ValheimRaftMenuName = "Raft";

  // Containers that are nested within a VehiclePrefab top level
  // utilize the Get<Name> methods within the LoadValheimVehiclesAssets class to get these GameObjects
  public const string PiecesContainer =
    "piecesContainer";

  public const string GhostContainer =
    "ghostContainer";

  public const string VehicleContainer =
    "vehicleContainer";

  public const string VehiclePiecesContainer = $"{ValheimVehiclesPrefix}_{PiecesContainer}";

  private const string ValheimVehiclesPrefix = "ValheimVehicles";
  public const string WaterVehicleShip = $"{ValheimVehiclesPrefix}_WaterVehicleShip";

  // hull ribs
  public const string ShipHullRibWoodPrefabName = $"{ValheimVehiclesPrefix}_Ship_Hull_Rib_Wood";
  public const string ShipHullRibIronPrefabName = $"{ValheimVehiclesPrefix}_Ship_Hull_Rib_Iron";

  // slabs
  public const string ShipHullSlabWoodPrefabName = $"{ValheimVehiclesPrefix}_Ship_Hull_Slab_Wood";
  public const string ShipHullSlabIronPrefabName = $"{ValheimVehiclesPrefix}_Ship_Hull_Slab_Iron";

  // to only be used for matching with generic prefab names
  public const string HullSlab = $"{ValheimVehiclesPrefix}_Hull_Slab";

  public const string HullWall =
    $"{ValheimVehiclesPrefix}_Hull_Wall";

  private static string GetMaterialVariantName(string materialVariant)
  {
    return materialVariant == ShipHulls.HullMaterial.Iron ? "Iron" : "Wood";
  }

  private static string GetPrefabSizeVariantName(PrefabSizeVariant prefabSizeVariant)
  {
    return prefabSizeVariant == PrefabSizeVariant.Four ? "4x4" : "2x2";
  }


  public static string GetHullSlabVariants(string materialVariant,
    PrefabSizeVariant prefabSizeVariant)
  {
    var sizeVariant = GetPrefabSizeVariantName(prefabSizeVariant);
    var materialVariantName = GetMaterialVariantName(materialVariant);

    return $"{HullSlab}_{materialVariantName}_{sizeVariant}";
  }

  public static string GetHullWallVariants(string materialVariant,
    PrefabSizeVariant prefabSizeVariant)
  {
    var sizeVariant = GetPrefabSizeVariantName(prefabSizeVariant);
    var materialVariantName = GetMaterialVariantName(materialVariant);

    return $"{HullWall}_{materialVariantName}_{sizeVariant}";
  }

  public const string ShipHullPrefabName = "Ship_Hull";

  // hull
  public const string ShipHullCenterWoodPrefabName =
    $"{ValheimVehiclesPrefix}_{ShipHullPrefabName}_Wood";

  public const string ShipHullCenterIronPrefabName =
    $"{ValheimVehiclesPrefix}_{ShipHullPrefabName}_Iron";

  public const string ShipRudderBasic = $"{ValheimVehiclesPrefix}_ShipRudderBasic";

  public const string ShipRudderAdvancedWood = $"{ValheimVehiclesPrefix}_ShipRudderAdvanced_Wood";
  public const string ShipRudderAdvancedIron = $"{ValheimVehiclesPrefix}_ShipRudderAdvanced_Iron";

  public const string ShipRudderAdvancedDoubleWood =
    $"{ValheimVehiclesPrefix}_ShipRudderAdvanced_Tail_Wood";

  public const string ShipRudderAdvancedDoubleIron =
    $"{ValheimVehiclesPrefix}_ShipRudderAdvanced_Tail_Iron";

  public const string ShipSteeringWheel = $"{ValheimVehiclesPrefix}_ShipSteeringWheel";
  public const string ShipKeel = $"{ValheimVehiclesPrefix}_ShipKeel";
  public const string WaterVehiclePreviewHull = $"{ValheimVehiclesPrefix}_WaterVehiclePreviewHull";

  public const string VehicleSail = $"{ValheimVehiclesPrefix}_VehicleSail";
  public const string VehicleShipTransform = $"{ValheimVehiclesPrefix}_VehicleShipTransform";
  public const string VehicleShipEffects = $"{ValheimVehiclesPrefix}_VehicleShipEffects";
  public const string VehicleSailMast = $"{ValheimVehiclesPrefix}_VehicleSailMast";
  public const string VehicleSailCloth = $"{ValheimVehiclesPrefix}_SailCloth";
  public const string VehicleToggleSwitch = $"{ValheimVehiclesPrefix}_VehicleToggleSwitch";
  public const string VehicleShipMovementOrientation = "VehicleShip_MovementOrientation";
  public const string VehicleHudAnchorIndicator = $"{ValheimVehiclesPrefix}_HudAnchorIndicator";
}