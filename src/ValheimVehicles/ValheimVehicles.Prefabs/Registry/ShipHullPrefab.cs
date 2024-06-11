using System.Linq;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimRAFT;

namespace ValheimVehicles.Prefabs.Registry;

public class ShipHullPrefab : IRegisterPrefab
{
  public static readonly ShipHullPrefab Instance = new();

  public void Register(PrefabManager prefabManager, PieceManager pieceManager)
  {
    var sizeVariants = new[]
    {
      PrefabNames.PrefabSizeVariant.Two,
      PrefabNames.PrefabSizeVariant.Four
    };
    var hullMaterialTypes = new[] { ShipHulls.HullMaterial.Wood, ShipHulls.HullMaterial.Iron };

    foreach (var hullMaterialType in hullMaterialTypes)
    {
      foreach (var sizeVariant in sizeVariants)
      {
        var materialCount = sizeVariant == PrefabNames.PrefabSizeVariant.Two ? 2 : 4;
        RegisterHull(
          PrefabNames.GetHullSlabVariants(hullMaterialType, sizeVariant),
          hullMaterialType,
          materialCount,
          sizeVariant,
          prefabManager,
          pieceManager);

        RegisterHull(
          PrefabNames.GetHullWallVariants(hullMaterialType, sizeVariant),
          hullMaterialType,
          materialCount,
          sizeVariant,
          prefabManager,
          pieceManager);
      }
    }

    // hulls 4x8
    RegisterHull(PrefabNames.ShipHullCenterWoodPrefabName, ShipHulls.HullMaterial.Wood, 8,
      PrefabNames.PrefabSizeVariant.Four,
      prefabManager, pieceManager);
    RegisterHull(PrefabNames.ShipHullCenterIronPrefabName, ShipHulls.HullMaterial.Iron,
      8, PrefabNames.PrefabSizeVariant.Four, prefabManager, pieceManager);


    // hull-ribs
    RegisterHullRib(PrefabNames.ShipHullRibWoodPrefabName, ShipHulls.HullMaterial.Wood,
      prefabManager, pieceManager);
    RegisterHullRib(PrefabNames.ShipHullRibIronPrefabName, ShipHulls.HullMaterial.Iron,
      prefabManager, pieceManager);
  }

  public static RequirementConfig[] GetRequirements(string material, int materialCount)
  {
    RequirementConfig[] requirements = [];
    switch (material)
    {
      case ShipHulls.HullMaterial.Iron:
        return
        [
          new RequirementConfig
          {
            Amount = materialCount,
            Item = "Iron",
            Recover = true
          },
          new RequirementConfig
          {
            Amount = materialCount,
            Item = "Bronze",
            Recover = true
          },
          new RequirementConfig
          {
            Amount = 10 * materialCount,
            Item = "BronzeNails",
            Recover = true
          }
        ];
      case ShipHulls.HullMaterial.Wood:
        return
        [
          new RequirementConfig
          {
            Amount = 5 * materialCount,
            Item = "Wood",
            Recover = true
          }
        ];
    }

    return requirements;
  }

  /// <summary>
  /// Experimental not ready
  /// </summary>
  private static void RegisterHullRib(
    string prefabName,
    string hullMaterial,
    PrefabManager prefabManager,
    PieceManager pieceManager)
  {
    var prefab =
      prefabManager.CreateClonedPrefab(
        prefabName, GetShipHullRibAssetByMaterial(hullMaterial));

    var wnt = PrefabRegistryHelpers.SetWearNTear(prefab);
    PrefabRegistryHelpers.SetWearNTearSupport(wnt, WearNTear.MaterialType.Iron);

    wnt.m_supports = true;
    wnt.m_support = 2000f;
    wnt.m_noSupportWear = true;
    wnt.m_noRoofWear = true;
    wnt.m_hitEffect = LoadValheimAssets.woodFloorPieceWearNTear.m_hitEffect;
    wnt.m_switchEffect = LoadValheimAssets.woodFloorPieceWearNTear.m_switchEffect;
    wnt.m_hitNoise = LoadValheimAssets.woodFloorPieceWearNTear.m_hitNoise;
    wnt.m_burnable = hullMaterial != ShipHulls.HullMaterial.Iron;

    ShipHulls.SetMaterialHealthValues(hullMaterial, wnt, 9);
    PrefabRegistryHelpers.AddNewOldPiecesToWearNTear(prefab, wnt);

    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    prefab.layer = 0;
    prefab.gameObject.layer = 0;
    PrefabRegistryHelpers.AddPieceForPrefab(prefabName, prefab);

    PrefabRegistryHelpers.HoistSnapPointsToPrefab(prefab,
      prefab.transform.Find("new") ?? prefab.transform,
      ["bottom_connector", "top_connector"]);

    pieceManager.AddPiece(new CustomPiece(prefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Category = PrefabNames.ValheimRaftMenuName,
      Enabled = true,
      Requirements = GetRequirements(hullMaterial, 4)
    }));
  }

  private static GameObject GetShipHullAssetByMaterial(string prefabName, string hullMaterial,
    PrefabNames.PrefabSizeVariant sizeVariant)
  {
    if (prefabName.Contains(PrefabNames.HullWall))
    {
      if (sizeVariant == PrefabNames.PrefabSizeVariant.Four)
      {
        return hullMaterial.Equals(ShipHulls.HullMaterial.Iron)
          ? LoadValheimVehicleAssets.ShipHullWall4X4IronAsset
          : LoadValheimVehicleAssets.ShipHullWall4X4WoodAsset;
      }

      return hullMaterial.Equals(ShipHulls.HullMaterial.Iron)
        ? LoadValheimVehicleAssets.ShipHullWall2X2IronAsset
        : LoadValheimVehicleAssets.ShipHullWall2X2WoodAsset;
    }

    if (prefabName.Contains(PrefabNames.HullSlab))
    {
      if (sizeVariant == PrefabNames.PrefabSizeVariant.Four)
      {
        return hullMaterial.Equals(ShipHulls.HullMaterial.Iron)
          ? LoadValheimVehicleAssets.ShipHullSlab4X4IronAsset
          : LoadValheimVehicleAssets.ShipHullSlab4X4WoodAsset;
      }

      return hullMaterial.Equals(ShipHulls.HullMaterial.Iron)
        ? LoadValheimVehicleAssets.ShipHullSlab2X2IronAsset
        : LoadValheimVehicleAssets.ShipHullSlab2X2WoodAsset;
    }

    return hullMaterial.Equals(ShipHulls.HullMaterial.Iron)
      ? LoadValheimVehicleAssets.ShipHullIronAsset
      : LoadValheimVehicleAssets.ShipHullWoodAsset;
  }

  private static GameObject GetShipHullRibAssetByMaterial(string hullMaterial)
  {
    return hullMaterial.Equals(ShipHulls.HullMaterial.Iron)
      ? LoadValheimVehicleAssets.ShipHullRibIronAsset
      : LoadValheimVehicleAssets.ShipHullRibWoodAsset;
  }

  private static void RegisterHull(
    string prefabName,
    string hullMaterial,
    int materialCount,
    PrefabNames.PrefabSizeVariant prefabSizeVariant,
    PrefabManager prefabManager,
    PieceManager pieceManager)
  {
    var prefab =
      prefabManager.CreateClonedPrefab(
        prefabName, GetShipHullAssetByMaterial(prefabName, hullMaterial, prefabSizeVariant));

    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    prefab.layer = 0;
    prefab.gameObject.layer = 0;
    var piece = PrefabRegistryHelpers.AddPieceForPrefab(prefabName, prefab);

    prefab.gameObject.transform.position = Vector3.zero;
    prefab.gameObject.transform.localPosition = Vector3.zero;
    piece.m_waterPiece = false;
    piece.m_noClipping = false;
    piece.m_noInWater = false;

    var wnt = PrefabRegistryHelpers.SetWearNTear(prefab);
    wnt.m_supports = true;
    wnt.m_noSupportWear = true;
    wnt.m_noRoofWear = true;
    wnt.m_hitEffect = LoadValheimAssets.woodFloorPieceWearNTear.m_hitEffect;
    wnt.m_switchEffect = LoadValheimAssets.woodFloorPieceWearNTear.m_switchEffect;
    wnt.m_hitNoise = LoadValheimAssets.woodFloorPieceWearNTear.m_hitNoise;
    wnt.m_burnable = hullMaterial != ShipHulls.HullMaterial.Iron;

    ShipHulls.SetMaterialHealthValues(hullMaterial, wnt, materialCount);
    PrefabRegistryHelpers.AddNewOldPiecesToWearNTear(prefab, wnt);
    // this will be used to hide water on the boat
    var hoistParents = new[] { "new" };

    if (prefabName.Contains(PrefabNames.ShipHullPrefabName))
    {
      hoistParents.AddItem("hull_slab_new_shared");
    }

    PrefabRegistryHelpers.HoistSnapPointsToPrefab(prefab,
      prefab.transform.Find("new") ?? prefab.transform, hoistParents
    );

    // ReSharper disable once ReplaceWithSingleAssignment.True
    var isEnabled = true;

    if (hullMaterial.Equals(ShipHulls.HullMaterial.Iron) &&
        !ValheimRaftPlugin.Instance.AllowExperimentalPrefabs.Value)
    {
      isEnabled = false;
    }

    pieceManager.AddPiece(new CustomPiece(prefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Category = PrefabNames.ValheimRaftMenuName,
      Enabled = isEnabled,
      Requirements = GetRequirements(hullMaterial, materialCount)
    }));
  }
}