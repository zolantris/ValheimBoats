using HarmonyLib;

namespace ValheimRAFT.Patches;

/// <summary>
/// Compatibility Patches for the mod PlantEasy
/// </summary>
public class Advize_PlantEasily_Patch
{
  private enum Status
  {
    Healthy,
    LackResources,
    NotCultivated,
    WrongBiome,
    NoSpace,
    NoSun,
    Invalid,
  }

  [HarmonyPatch(typeof(Advize_PlantEasily.PlantEasily), "CheckPlacementStatus")]
  [HarmonyPrefix]
  private static bool CheckPlacementStatus(Advize_PlantEasily.PlantEasily __instance,
    object __result)
  {
    __result = Status.Healthy;
    return false;
  }
}