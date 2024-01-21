﻿using BepInEx;
using BepInEx.Configuration;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using Jotunn;
using UnityEngine;
using UnityEngine.Serialization;
using ValheimRAFT.Patches;

namespace ValheimRAFT;

[BepInPlugin(BepInGuid, ModName, Version)]
[BepInDependency(Main.ModGuid)]
[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Patch)]
public class ValheimRaftPlugin : BaseUnityPlugin
{
  /*
   * @note keeping this as Sarcen for now since there are low divergences from the original codebase and patches already mapped to sarcen's mod
   */
  public const string Author = "Sarcen";
  public const string Version = "1.6.14";
  internal const string ModName = "ValheimRAFT";
  public const string BepInGuid = $"BepIn.{Author}.{ModName}";
  private const string HarmonyGuid = $"Harmony.{Author}.{ModName}";
  public const string ModDescription = "Valheim Mod for building on the sea";
  public const string CopyRight = "Copyright © 2023, GNU-v3 licensed";
  internal static readonly int CustomRaftLayer = 29;
  public static AssetBundle m_assetBundle;
  private bool m_customItemsAdded;
  public PrefabController prefabController;

  public static ValheimRaftPlugin Instance { get; private set; }

  public ConfigEntry<bool> MakeAllPiecesWaterProof { get; set; }

  public ConfigEntry<bool> AllowFlight { get; set; }

  public ConfigEntry<string> PluginFolderName { get; set; }
  public ConfigEntry<float> InitialRaftFloorHeight { get; set; }
  public ConfigEntry<bool> PatchPlanBuildPositionIssues { get; set; }
  public ConfigEntry<float> RaftHealth { get; set; }
  public ConfigEntry<float> ServerRaftUpdateZoneInterval { get; set; }
  public ConfigEntry<float> RaftSailForceMultiplier { get; set; }
  public ConfigEntry<bool> DisplacedRaftAutoFix { get; set; }
  public ConfigEntry<bool> AdminsCanOnlyBuildRaft { get; set; }


  // Propulsion Configs
  public ConfigEntry<bool> EnableCustomPropulsionConfig { get; set; }

  public ConfigEntry<float> MaxPropulsionSpeed { get; set; }
  public ConfigEntry<float> MaxSailSpeed { get; set; }
  public ConfigEntry<float> SpeedCapMultiplier { get; set; }

  // for those that want to cruise with rudder
  // public ConfigEntry<bool> AllowRudderSpeed { get; set; }
  // public ConfigEntry<float> RudderSpeed2 { get; set; }
  // public ConfigEntry<float> RudderSpeed3 { get; set; }
  public ConfigEntry<float> SailTier1Area { get; set; }
  public ConfigEntry<float> SailTier2Area { get; set; }
  public ConfigEntry<float> SailTier3Area { get; set; }
  public ConfigEntry<float> SailCustomAreaTier1Multiplier { get; set; }
  public ConfigEntry<float> BoatDragCoefficient { get; set; }
  public ConfigEntry<float> MastShearForceThreshold { get; set; }
  public ConfigEntry<bool> HasDebugSails { get; set; }
  public ConfigEntry<bool> HasDebugBase { get; set; }

  public ConfigEntry<bool> HasShipWeightCalculations { get; set; }
  public ConfigEntry<float> MassPercentageFactor { get; set; }
  public ConfigEntry<bool> ShowShipStats { get; set; }
  public ConfigEntry<bool> HasShipContainerWeightCalculations { get; set; }
  public ConfigEntry<float> RaftCreativeHeight { get; set; }
  public ConfigEntry<float> FloatingColliderVerticalSize { get; set; }
  public ConfigEntry<float> FloatingColliderVerticalCenterOffset { get; set; }
  public ConfigEntry<float> BlockingColliderVerticalSize { get; set; }
  public ConfigEntry<float> BlockingColliderVerticalCenterOffset { get; set; }
  public ConfigEntry<KeyboardShortcut> AnchorKeyboardShortcut { get; set; }

  /**
   * These folder names are matched for the CustomTexturesGroup
   */
  public string[] possibleModFolderNames =
    new[]
    {
      $"{Author}-{ModName}", $"zolantris-{ModName}", $"Zolantris-{ModName}", ModName
    };

  private ConfigDescription CreateConfigDescription(string description, bool isAdmin = false)
  {
    return new ConfigDescription(
      description,
      (AcceptableValueBase)null, new object[1]
      {
        (object)new ConfigurationManagerAttributes()
        {
          IsAdminOnly = true
        }
      });
  }

  private void CreateColliderConfig()
  {
    FloatingColliderVerticalCenterOffset = Config.Bind("Debug",
      "FloatingColliderVerticalCenterOffset",
      -0.65f,
      CreateConfigDescription(
        "Sets the raft vertical collision center location original value is -0.2f. Lower offsets can make the boat more jittery, positive offsets will cause the boat to go underwater in areas",
        false));
    FloatingColliderVerticalSize = Config.Bind("Debug", "FloatingColliderVerticalSize",
      1.3f,
      CreateConfigDescription(
        "Sets the raft floating collider size. Smaller sizes can make the boat more jittery",
        false));

    BlockingColliderVerticalSize = Config.Bind("Debug", "BlockingColliderVerticalSize",
      1.3f,
      CreateConfigDescription(
        "Sets sets the raft blocking collider size.", false));
    BlockingColliderVerticalCenterOffset = Config.Bind("Debug",
      "BlockingColliderVerticalCenterOffset",
      -0.65f,
      CreateConfigDescription(
        "Sets the raft BlockingColliderVerticalCenterOffset which blocks the player or objects passing through. This will trigger physics so if there is an interaction between the boat and player/it can cause the player to push the boat in the direction of interaction",
        false));
  }

  private void CreateCommandConfig()
  {
    RaftCreativeHeight = Config.Bind("Config", "RaftCreativeHeight",
      5f,
      CreateConfigDescription(
        "Sets the raftcreative command height, raftcreative is relative to the current height of the ship, negative numbers will sink your ship temporarily",
        false));
  }

  private void CreatePropulsionConfig()
  {
    ShowShipStats = Config.Bind("Debug", "ShowShipState", true);
    MaxPropulsionSpeed = Config.Bind("Propulsion", "MaxSailSpeed", 18f,
      CreateConfigDescription(
        "Sets the absolute max speed a ship can ever hit. Prevents or enables space launches",
        true));
    MaxSailSpeed = Config.Bind("Propulsion", "MaxSailSpeed", 10f,
      CreateConfigDescription(
        "Sets the absolute max speed a ship can ever hit with sails. Prevents or enables space launches, cannot exceed MaxPropulsionSpeed.",
        true));
    MassPercentageFactor = Config.Bind("Propulsion", "MassPercentage", 55f, CreateConfigDescription(
      "Sets the mass percentage of the ship that will slow down the sails",
      true));
    SpeedCapMultiplier = Config.Bind("Propulsion", "SpeedCapMultiplier", 1f,
      CreateConfigDescription(
        "Sets the speed at which it becomes significantly harder to gain speed per sail area",
        true));

    // RudderSpeed2 = Config.Bind("Propulsion", "RudderSpeed2", 5f,
    //   CreateConfigDescription(
    //     "Max speed at rudder speed 2.", true));
    // RudderSpeed3 = Config.Bind("Propulsion", "RudderSpeed3", 10f,
    //   CreateConfigDescription(
    //     "", true));
    // AllowRudderSpeed = Config.Bind("Propulsion", "AllowRudderSpeed", true,
    //   CreateConfigDescription(
    //     "", true));

    HasShipWeightCalculations = Config.Bind("Propulsion", "HasShipWeightCalculations", true,
      CreateConfigDescription(
        "enables ship weight calculations for sail-force (sailing speed) and future propulsion, makes larger ships require more sails and smaller ships require less"));

    HasShipContainerWeightCalculations = Config.Bind("Propulsion",
      "HasShipContainerWeightCalculations",
      true,
      CreateConfigDescription(
        "enables ship weight calculations for containers which affects sail-force (sailing speed) and future propulsion calculations. Makes ships with lots of containers require more sails"));

    HasDebugSails = Config.Bind("Debug", "HasDebugSails", false,
      CreateConfigDescription(
        "Outputs all custom sail information when saving and updating ZDOs for the sails. Debug only."));

    EnableCustomPropulsionConfig = Config.Bind("Propulsion",
      "EnableCustomPropulsionConfig", SailAreaForce.HasPropulsionConfigOverride,
      CreateConfigDescription("Enables all custom propulsion values", false));

    SailCustomAreaTier1Multiplier = Config.Bind("Propulsion",
      "SailCustomAreaTier1Multiplier", SailAreaForce.CustomTier1AreaForceMultiplier,
      CreateConfigDescription(
        "Manual sets the area multiplier the custom tier1 sail. Currently there is only 1 tier",
        true)
    );

    SailTier1Area = Config.Bind("Propulsion",
      "SailTier1Area", SailAreaForce.Tier1,
      CreateConfigDescription("Manual sets the area of the tier 1 sail.", true)
    );

    SailTier2Area = Config.Bind("Propulsion",
      "SailTier2Area", SailAreaForce.Tier2,
      CreateConfigDescription("Manual sets the area of the tier 2 sail.", true));

    SailTier3Area = Config.Bind("Propulsion",
      "SailTier3Area", SailAreaForce.Tier3,
      CreateConfigDescription("Manual sets the area of the tier 3 sail.", true));
  }

  private void CreateServerConfig()
  {
    AdminsCanOnlyBuildRaft = Config.Bind("Server config", "AdminsCanOnlyBuildRaft", false,
      new ConfigDescription(
        "ValheimRAFT hammer menu pieces are registered as disabled unless the user is an Admin, allowing only admins to create rafts. This will update automatically make sure to un-equip the hammer to see it apply (if your remove yourself as admin). Server / client does not need to restart",
        (AcceptableValueBase)null, new object[1]
        {
          (object)new ConfigurationManagerAttributes()
          {
            IsAdminOnly = true
          }
        }));

    ServerRaftUpdateZoneInterval = Config.Bind<float>("Server config",
      "ServerRaftUpdateZoneInterval",
      10f,
      new ConfigDescription(
        "Allows Server Admin control over the update tick for the RAFT location. Larger Rafts will take much longer and lag out players, but making this ticket longer will make the raft turn into a box from a long distance away.",
        (AcceptableValueBase)null, new object[1]
        {
          (object)new ConfigurationManagerAttributes()
          {
            IsAdminOnly = true
          }
        }));

    MakeAllPiecesWaterProof = Config.Bind<bool>("Server config",
      "MakeAllPiecesWaterProof", true, new ConfigDescription(
        "Makes it so all building pieces (walls, floors, etc) on the ship don't take rain damage.",
        (AcceptableValueBase)null, new object[1]
        {
          (object)new ConfigurationManagerAttributes()
          {
            IsAdminOnly = true
          }
        }));
    AllowFlight = Config.Bind<bool>("Server config", "AllowFlight", false,
      new ConfigDescription("Allow the raft to fly (jump\\crouch to go up and down)",
        (AcceptableValueBase)null, new object[1]
        {
          (object)new ConfigurationManagerAttributes()
          {
            IsAdminOnly = true
          }
        }));
  }

  private void CreateDebugConfig()
  {
    DisplacedRaftAutoFix = Config.Bind("Debug",
      "DisplacedRaftAutoFix", false,
      "Automatically fix a displaced glitched out raft if the player is standing on the raft. This will make the player fall into the water briefly but avoid having to run 'raftoffset 0 0 0'");
  }

  private void CreatePrefabConfig()
  {
    RaftHealth = Config.Bind<float>("Config", "raftHealth", 500f,
      "Set the raft health when used with wearNTear, lowest value is 100f");
  }

  private void CreateBaseConfig()
  {
    HasDebugBase = Config.Bind("Debug", "HasDebugBase", false,
      CreateConfigDescription(
        "Outputs more debug logs for the MoveableBaseRootComponent. Useful for troubleshooting errors, but may fill logs quicker"));
    PatchPlanBuildPositionIssues = Config.Bind<bool>("Patches",
      "fixPlanBuildPositionIssues", true, new ConfigDescription(
        "Fixes the PlanBuild mod position problems with ValheimRaft so it uses localPosition of items based on the parent raft. This MUST be enabled to support PlanBuild but can be disabled when the mod owner adds direct support for this part of ValheimRAFT.",
        (AcceptableValueBase)null, new object[1]
        {
          (object)new ConfigurationManagerAttributes()
          {
            IsAdminOnly = false
          }
        }));

    InitialRaftFloorHeight = Config.Bind<float>("Config",
      "Initial Floor Height", 0.6f, new ConfigDescription(
        "Allows users to set the raft floor spawn height. 0.45 was the original height in 1.4.9 but it looked a bit too low. Now people can customize it",
        (AcceptableValueBase)null, new object[1]
        {
          (object)new ConfigurationManagerAttributes()
          {
            IsAdminOnly = false
          }
        }));

    PluginFolderName = Config.Bind<string>("Config",
      "pluginFolderName", "", new ConfigDescription(
        "Users can leave this empty. If they do not, the mod will attempt to match the folder string. Allows users to set the folder search name if their" +
        $" manager renames the folder, r2modman has a fallback case added to search for {Author}-{ModName}" +
        "Default search values are an ordered list first one is always matching non-empty strings from this pluginFolderName." +
        $"Folder Matches are:  {Author}-{ModName}, zolantris-{ModName} Zolantris-{ModName}, and {ModName}",
        (AcceptableValueBase)null, new object[1]
        {
          (object)new ConfigurationManagerAttributes()
          {
            IsAdminOnly = false
          }
        }));
    PluginFolderName = Config.Bind<string>("Config",
      "pluginFolderName", "", new ConfigDescription(
        "Users can leave this empty. If they do not, the mod will attempt to match the folder string. Allows users to set the folder search name if their" +
        $" manager renames the folder, r2modman has a fallback case added to search for {Author}-{ModName}" +
        "Default search values are an ordered list first one is always matching non-empty strings from this pluginFolderName." +
        $"Folder Matches are:  {Author}-{ModName}, zolantris-{ModName} Zolantris-{ModName}, and {ModName}",
        (AcceptableValueBase)null, new object[1]
        {
          (object)new ConfigurationManagerAttributes()
          {
            IsAdminOnly = false
          }
        }));
  }

  private void CreateKeyboardSetup()
  {
    AnchorKeyboardShortcut =
      Config.Bind("Config", "AnchorKeyboardShortcut", new KeyboardShortcut(KeyCode.LeftShift),
        new ConfigDescription("Anchor keyboard hotkey. Only applies to keyboard"));
  }

  /*
   * aggregates all config creators.
   *
   * Future plans:
   * - Abstract specific config directly into related files and call init here to set those values in the associated classes.
   * - Most likely those items will need to be "static" values.
   * - Add a watcher so those items can take the new config and process it as things update.
   */

  private void CreateConfig()
  {
    CreateBaseConfig();
    CreatePrefabConfig();
    CreateDebugConfig();
    CreatePropulsionConfig();
    CreateServerConfig();
    CreateCommandConfig();
    CreateColliderConfig();
    CreateKeyboardSetup();
  }

  public void Awake()
  {
    Instance = this;
    CreateConfig();
    PatchController.Apply(HarmonyGuid);

    AddPhysicsSettings();

    CommandManager.Instance.AddConsoleCommand((ConsoleCommand)new CreativeModeConsoleCommand());
    CommandManager.Instance.AddConsoleCommand((ConsoleCommand)new MoveRaftConsoleCommand());
    CommandManager.Instance.AddConsoleCommand((ConsoleCommand)new HideRaftConsoleCommand());
    CommandManager.Instance.AddConsoleCommand((ConsoleCommand)new RecoverRaftConsoleCommand());

    /*
     * @todo add a way to skip LoadCustomTextures when on server. This check when used here crashes the Plugin.
     */
    PrefabManager.OnVanillaPrefabsAvailable += new Action(LoadCustomTextures);
    PrefabManager.OnVanillaPrefabsAvailable += new Action(AddCustomPieces);
  }

  private void AddPhysicsSettings()
  {
    var layer = LayerMask.NameToLayer("vehicle");

    for (var index = 0; index < 32; ++index)
      Physics.IgnoreLayerCollision(CustomRaftLayer, index,
        Physics.GetIgnoreLayerCollision(layer, index));

    Physics.IgnoreLayerCollision(CustomRaftLayer, LayerMask.NameToLayer("vehicle"),
      true);
    Physics.IgnoreLayerCollision(CustomRaftLayer, LayerMask.NameToLayer("piece"),
      true);
    Physics.IgnoreLayerCollision(CustomRaftLayer, LayerMask.NameToLayer("character"),
      true);
    Physics.IgnoreLayerCollision(CustomRaftLayer, LayerMask.NameToLayer("smoke"),
      true);
    Physics.IgnoreLayerCollision(CustomRaftLayer,
      LayerMask.NameToLayer("character_ghost"), true);
    Physics.IgnoreLayerCollision(CustomRaftLayer, LayerMask.NameToLayer("weapon"),
      true);
    Physics.IgnoreLayerCollision(CustomRaftLayer, LayerMask.NameToLayer("blocker"),
      true);
    Physics.IgnoreLayerCollision(CustomRaftLayer,
      LayerMask.NameToLayer("pathblocker"), true);
    Physics.IgnoreLayerCollision(CustomRaftLayer, LayerMask.NameToLayer("viewblock"),
      true);
    Physics.IgnoreLayerCollision(CustomRaftLayer,
      LayerMask.NameToLayer("character_net"), true);
    Physics.IgnoreLayerCollision(CustomRaftLayer,
      LayerMask.NameToLayer("character_noenv"), true);
    Physics.IgnoreLayerCollision(CustomRaftLayer,
      LayerMask.NameToLayer("Default_small"), false);
    Physics.IgnoreLayerCollision(CustomRaftLayer, LayerMask.NameToLayer("Default"),
      false);
  }

  private void LoadCustomTextures()
  {
    var sails = CustomTextureGroup.Load("Sails");
    for (var k = 0; k < sails.Textures.Count; k++)
    {
      var texture3 = sails.Textures[k];
      texture3.Texture.wrapMode = TextureWrapMode.Clamp;
      if ((bool)texture3.Normal) texture3.Normal.wrapMode = TextureWrapMode.Clamp;
    }

    var patterns = CustomTextureGroup.Load("Patterns");
    for (var j = 0; j < patterns.Textures.Count; j++)
    {
      var texture2 = patterns.Textures[j];
      texture2.Texture.filterMode = FilterMode.Point;
      texture2.Texture.wrapMode = TextureWrapMode.Repeat;
      if ((bool)texture2.Normal) texture2.Normal.wrapMode = TextureWrapMode.Repeat;
    }

    var logos = CustomTextureGroup.Load("Logos");
    for (var i = 0; i < logos.Textures.Count; i++)
    {
      var texture = logos.Textures[i];
      texture.Texture.wrapMode = TextureWrapMode.Clamp;
      if ((bool)texture.Normal) texture.Normal.wrapMode = TextureWrapMode.Clamp;
    }
  }

  internal void AddCustomPieces()
  {
    if (m_customItemsAdded) return;

    m_customItemsAdded = true;
    m_assetBundle =
      AssetUtils.LoadAssetBundleFromResources("valheimraft", Assembly.GetExecutingAssembly());

    // Registers all prefabs
    prefabController = gameObject.AddComponent<PrefabController>();
    prefabController.Init();
  }
}