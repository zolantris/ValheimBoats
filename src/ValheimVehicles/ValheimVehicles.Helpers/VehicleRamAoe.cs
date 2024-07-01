using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using Microsoft.Win32;
using UnityEngine;
using ValheimRAFT;
using ValheimVehicles.Config;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Components;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Helpers;

public class VehicleRamAoe : Aoe
{
  // Typeof PrefabTiers
  public string materialTier;
  public static List<VehicleRamAoe> RamInstances = [];

  public static HitData.DamageTypes baseDamage;

  public float minimumVelocityToTriggerHit => RamConfig.minimumVelocityToTriggerHit.Value *
                                              (RamType == RamPrefabs.RamType.Blade ? 1 : 0.5f);

  public VehicleShip? vehicle;
  public RamPrefabs.RamType RamType;

  // damages
  public static int RamDamageToolTier = RamConfig.RamDamageToolTier.Value;
  public static float RamHitArea = RamConfig.HitRadius.Value;
  public static float PercentageDamageToSelf = RamConfig.PercentageDamageToSelf.Value;
  public static float RamBaseSlashDamage => RamConfig.RamBaseSlashDamage.Value;
  public static float RamBaseBluntDamage => RamConfig.RamBaseBluntDamage.Value;
  public static float RamBaseChopDamage => RamConfig.RamBaseChopDamage.Value;
  public static float RamBasePickAxeDamage => RamConfig.RamBasePickAxeDamage.Value;
  public static float RamBasePierceDamage => RamConfig.RamBasePierceDamage.Value;
  public static float RamBaseMaximumDamage => RamConfig.RamBaseMaximumDamage.Value;

  public static float RamHitInterval = RamConfig.RamHitInterval.Value;

  public static bool AllowContinuousDamage = RamConfig.AllowContinuousDamage.Value;

  // hit booleans
  public static bool RamsCanHitEnvironmentOrTerrain =
    RamConfig.CanHitEnvironmentOrTerrain.Value;

  public static bool RamsCanHitEnemies = RamConfig.CanHitEnemies.Value;
  public static bool CanDamageSelf = RamConfig.CanDamageSelf.Value;
  public static bool RamsCanHitCharacters = RamConfig.CanHitCharacters.Value;
  public static bool RamsCanHitFriendly = RamConfig.CanHitFriendly.Value;

  public float chopDamageRatio;
  public float pickaxeDamageRatio;
  public float slashDamageRatio;
  public float pierceDamageRatio;
  public float bluntDamageRatio;

  public static float DamageIncreasePercentagePerTier =>
    RamConfig.DamageIncreasePercentagePerTier.Value;

  public static float MaxVelocityMultiplier =>
    RamConfig.MaxVelocityMultiplier.Value;

  public static bool HasMaxDamageCap =>
    RamConfig.HasMaximumDamageCap.Value;

  public bool isReadyForCollisions = false;

  private Rigidbody rigidbody;

  public void InitializeFromConfig()
  {
    m_blockable = false;
    m_dodgeable = false;
    m_hitTerrain = RamsCanHitEnvironmentOrTerrain;
    m_hitProps = RamsCanHitEnvironmentOrTerrain;
    m_hitCharacters = RamsCanHitCharacters;
    m_hitFriendly = RamsCanHitFriendly;
    m_hitEnemy = RamsCanHitEnemies;
    m_hitParent = CanDamageSelf;

    // todo may need this to do damage to wearntear prefab of the ram
    // m_hitTerrain = true;
    // m_hitProps = true;
    // m_hitCharacters = true;
    // m_hitFriendly = true;
    // m_hitEnemy = true;
    // m_hitParent = true;

    m_hitInterval = Mathf.Clamp(RamHitInterval, 0.5f, 5f);

    // todo need to tweak this
    m_damageSelf = !CanDamageSelf ? 0 : 1;
    m_toolTier = RamDamageToolTier;
    m_attackForce = 5;
    m_attackForce = 50;

    m_radius = Mathf.Clamp(RamHitArea, 0.1f, 10f);
    m_radius *= RamType == RamPrefabs.RamType.Blade ? 1 : 0.5f;

    m_useTriggers = true;
    m_triggerEnterOnly = AllowContinuousDamage;
    m_triggerEnterOnly = true;
    m_useCollider = null;
    m_useAttackSettings = true;
    m_ttl = 0;
  }

  public float GetTotalDamage(float slashDamage, float bluntDamage, float chopDamage,
    float pickaxeDamage, float pierceDamage)
  {
    return slashDamage + bluntDamage + chopDamage + pickaxeDamage + pierceDamage;
  }

  public void InitAoe()
  {
    base.Awake();
  }

  public new void Awake()
  {
    if (!RamInstances.Contains(this))
    {
      RamInstances.Add(this);
    }

    InitializeFromConfig();
    SetBaseDamageFromConfig();

    InitAoe();

    rigidbody = GetComponent<Rigidbody>();
    // very important otherwise this rigidbody will interfere with physics of the Watervehicle controller due to nesting.
    // todo to move this rigidbody into a joint and make it a sibling of the WaterVehicle or PieceContainer (doing this would be a large refactor to structure, likely requiring a new prefab)
    if (rigidbody)
    {
      rigidbody.includeLayers = m_rayMask;
    }
  }

  private new void OnDisable()
  {
    if (RamInstances.Contains(this))
    {
      RamInstances.Remove(this);
    }

    base.OnDisable();
  }

  public void Start()
  {
    Invoke(nameof(UpdateReadyForCollisions), 1f);
  }

  public override void OnEnable()
  {
    if (!RamInstances.Contains(this))
    {
      RamInstances.Add(this);
    }

    Invoke(nameof(UpdateReadyForCollisions), 1f);
    base.OnEnable();
  }

  public new void CustomFixedUpdate(float fixedDeltaTime)
  {
    if ((UnityEngine.Object)m_nview != (UnityEngine.Object)null &&
        !m_nview.IsOwner())
      return;
    if (m_initRun && !m_useTriggers && !m_hitAfterTtl &&
        (double)m_activationTimer <= 0.0)
    {
      m_initRun = false;
      if ((double)m_hitInterval <= 0.0)
        Initiate();
    }

    if ((UnityEngine.Object)m_owner != (UnityEngine.Object)null &&
        m_attachToCaster)
    {
      transform.position =
        m_owner.transform.TransformPoint(m_offset);
      transform.rotation = m_owner.transform.rotation * m_localRot;
    }

    if ((double)m_activationTimer > 0.0)
      return;
    if ((double)m_hitInterval > 0.0 && !m_useTriggers)
    {
      m_hitTimer -= fixedDeltaTime;
      if ((double)m_hitTimer <= 0.0)
      {
        m_hitTimer = m_hitInterval;
        Initiate();
      }
    }

    if ((double)m_chainStartChance > 0.0 && (double)m_chainDelay >= 0.0)
    {
      m_chainDelay -= fixedDeltaTime;
      if ((double)m_chainDelay <= 0.0 &&
          (double)UnityEngine.Random.value < (double)m_chainStartChance)
      {
        Vector3 position1 = transform.position;
        FindHits();
        SortHits();
        int num1 = UnityEngine.Random.Range(m_chainMinTargets,
          m_chainMaxTargets + 1);
        foreach (Collider hit in Aoe.s_hitList)
        {
          if ((double)UnityEngine.Random.value < (double)m_chainChancePerTarget)
          {
            Vector3 position2 = hit.gameObject.transform.position;
            bool flag = false;
            for (int index = 0; index < Aoe.s_chainObjs.Count; ++index)
            {
              if ((bool)(UnityEngine.Object)Aoe.s_chainObjs[index])
              {
                if ((double)Vector3.Distance(Aoe.s_chainObjs[index].transform.position, position2) <
                    0.10000000149011612)
                {
                  flag = true;
                  break;
                }
              }
              else
                Aoe.s_chainObjs.RemoveAt(index);
            }

            if (!flag)
            {
              GameObject gameObject1 =
                UnityEngine.Object.Instantiate<GameObject>(m_chainObj, position2,
                  hit.gameObject.transform.rotation);
              Aoe.s_chainObjs.Add(gameObject1);
              IProjectile componentInChildren = gameObject1.GetComponentInChildren<IProjectile>();
              if (componentInChildren != null)
              {
                componentInChildren.Setup(m_owner, position1.DirTo(position2), -1f,
                  m_hitData, m_itemData, m_ammo);
                if (componentInChildren is Aoe aoe)
                  aoe.m_chainChance =
                    m_chainChance * m_chainStartChanceFalloff;
              }

              --num1;
              float num2 = Vector3.Distance(position2, transform.position);
              foreach (GameObject gameObject2 in m_chainEffects.Create(
                         position1 + Vector3.up,
                         Quaternion.LookRotation(position1.DirTo(position2 + Vector3.up))))
                gameObject2.transform.localScale = Vector3.one * num2;
            }
          }

          if (num1 <= 0)
            break;
        }
      }
    }

    if ((double)m_ttl <= 0.0)
      return;
    m_ttl -= fixedDeltaTime;
    if ((double)m_ttl > 0.0)
      return;
    if (m_hitAfterTtl)
      Initiate();
    if (!(bool)(UnityEngine.Object)ZNetScene.instance)
      return;
    ZNetScene.instance.Destroy(gameObject);
    return;
  }

  public void UpdateReadyForCollisions()
  {
    CancelInvoke(nameof(UpdateReadyForCollisions));
    if (!m_nview)
    {
      isReadyForCollisions = false;
      Invoke(nameof(UpdateReadyForCollisions), 1);
      return;
    }

    var isVehicleChild = m_nview.GetZDO().GetInt(VehicleZdoVars.MBParentIdHash);
    if (isVehicleChild == 0)
    {
      isReadyForCollisions = true;
      return;
    }

    // Must be within the BaseVehiclePieces after initialization otherwise this AOE could attempt to damage items within the raft ball
    var isChildOfBaseVehicle = transform.root.name.StartsWith(PrefabNames.WaterVehicleShip) ||
                               transform.root.name.StartsWith(PrefabNames.VehiclePiecesContainer) ||
                               transform.root.name.StartsWith(
                                 PrefabNames
                                   .VehicleMovingPiecesContainer);
    if (!isChildOfBaseVehicle)
    {
      isReadyForCollisions = false;
      return;
    }

    isReadyForCollisions = true;
  }

  public bool UpdateDamageFromVelocityCollider(Collider collider)
  {
    if (!collider) return false;
    // reset damage to base damage if one of these is not available, will still recalculate later
    // exit to apply damage that has no velocity
    if (!vehicle?.MovementController.m_body || !collider.attachedRigidbody)
    {
      m_damage = baseDamage;
      return true;
    }

    // Velocity will significantly increase if the object is moving towards the other object IE collision
    float relativeVelocity;
    if (!vehicle?.MovementController.m_body)
    {
      relativeVelocity = collider.attachedRigidbody.velocity.magnitude;
    }
    else
    {
      relativeVelocity =
        Vector3.Magnitude(collider?.attachedRigidbody?.velocity ??
                          Vector3.zero - vehicle?.MovementController.m_body?.velocity ??
                          Vector3.zero);
    }

    return UpdateDamageFromVelocity(relativeVelocity);
  }

  public bool UpdateDamageFromVelocity(float relativeVelocityMagnitude)
  {
    // exits if the velocity is not within expected damage ranges
    if (relativeVelocityMagnitude < minimumVelocityToTriggerHit) return false;

    var multiplier = Mathf.Min(relativeVelocityMagnitude * 0.5f, MaxVelocityMultiplier);

    if (materialTier == PrefabTiers.Tier3)
    {
      multiplier *= Mathf.Clamp(1 + DamageIncreasePercentagePerTier * 2, 1, 4);
    }

    if (Mathf.Approximately(multiplier, 0))
    {
      multiplier = 0;
    }

    var bluntDamage = baseDamage.m_blunt * multiplier;
    var pickaxeDamage = baseDamage.m_pickaxe * multiplier;
    float slashDamage = 0;
    float chopDamage = 0;
    float pierceDamage = 0;

    if (RamType == RamPrefabs.RamType.Stake)
    {
      pierceDamage = baseDamage.m_pierce * multiplier;
    }

    if (RamType == RamPrefabs.RamType.Blade)
    {
      slashDamage = baseDamage.m_slash * multiplier;
      chopDamage = baseDamage.m_chop * multiplier;
    }


    if (HasMaxDamageCap)
    {
      var nextTotalDamage =
        GetTotalDamage(slashDamage, bluntDamage, chopDamage, pickaxeDamage, pierceDamage);

      if (nextTotalDamage > RamBaseMaximumDamage)
      {
        if (nextTotalDamage <= 0) return false;
        if (chopDamageRatio == 0)
        {
          chopDamageRatio = chopDamage / nextTotalDamage;
        }

        if (pickaxeDamageRatio == 0)
        {
          pickaxeDamageRatio = pickaxeDamage / nextTotalDamage;
        }

        if (slashDamageRatio == 0)
        {
          slashDamageRatio = slashDamage / nextTotalDamage;
        }

        if (bluntDamageRatio == 0)
        {
          bluntDamageRatio = bluntDamage / nextTotalDamage;
        }

        slashDamage = baseDamage.m_slash * slashDamageRatio;
        bluntDamage = baseDamage.m_blunt * bluntDamageRatio;
        chopDamage = baseDamage.m_chop * chopDamageRatio;
        pickaxeDamage = baseDamage.m_pickaxe * pickaxeDamageRatio;
        pierceDamage = baseDamage.m_pierce * pierceDamageRatio;
      }
    }

    m_damage = new HitData.DamageTypes()
    {
      m_blunt = bluntDamage,
      m_pierce = pierceDamage,
      m_slash = slashDamage,
      m_chop = chopDamage,
      m_pickaxe = pickaxeDamage,
    };

    if (!CanDamageSelf)
    {
      return true;
    }

    m_damageSelf =
      GetTotalDamage(slashDamage, bluntDamage, chopDamage, pickaxeDamage, pierceDamage) *
      PercentageDamageToSelf;

    return true;
  }

  private void IgnoreCollider(Collider collider)
  {
    var childColliders = GetComponentsInChildren<Collider>();
    foreach (var childCollider in childColliders)
    {
      Physics.IgnoreCollision(childCollider, collider, true);
    }
  }

  /// <summary>
  /// Ignores anything within the current vehicle and other vehicle movement/float/onboard colliders 
  /// </summary>
  /// <param name="collider"></param>
  /// <returns></returns>
  private bool ShouldIgnore(Collider collider)
  {
    if (!collider) return false;
    if (PrefabNames.IsVehicleCollider(collider.name))
    {
      IgnoreCollider(collider);
      return true;
    }

    if (collider.transform.root != transform.root) return false;
    if (vehicle != null)
    {
      // allows for hitting other vehicles, excludes hitting current vehicle
      if (collider.transform.root != vehicle.transform.root)
      {
        return false;
      }
    }

    IgnoreCollider(collider);
    return true;
  }

  public new void OnCollisionEnter(Collision collision)
  {
    if (!isReadyForCollisions) return;
    if (ShouldIgnore(collision.collider)) return;
    if (!UpdateDamageFromVelocity(Vector3.Magnitude(collision.relativeVelocity))) return;
    base.OnCollisionEnter(collision);
  }

  public new void OnCollisionStay(Collision collision)
  {
    if (!isReadyForCollisions) return;
    if (ShouldIgnore(collision.collider)) return;
    if (!UpdateDamageFromVelocity(Vector3.Magnitude(collision.relativeVelocity))) return;
    base.OnCollisionStay(collision);
  }

  public new void OnTriggerEnter(Collider collider)
  {
    if (!isReadyForCollisions) return;
    if (ShouldIgnore(collider)) return;
    if (!UpdateDamageFromVelocityCollider(collider)) return;
    base.OnTriggerEnter(collider);
  }

  public new void OnTriggerStay(Collider collider)
  {
    if (!isReadyForCollisions) return;
    if (ShouldIgnore(collider)) return;
    if (!UpdateDamageFromVelocityCollider(collider)) return;
    base.OnTriggerStay(collider);
  }

  public void SetBaseDamage(HitData.DamageTypes data)
  {
    chopDamageRatio = 0;
    pickaxeDamageRatio = 0;
    slashDamageRatio = 0;
    pierceDamageRatio = 0;
    bluntDamageRatio = 0;
    baseDamage = data;
  }

  public void SetBaseDamageFromConfig()
  {
    Logger.LogDebug("Setting Damage config for Ram");
    SetBaseDamage(new HitData.DamageTypes()
    {
      m_slash = RamBaseSlashDamage,
      m_pierce = RamBasePierceDamage,
      m_blunt = RamBaseBluntDamage,
      m_chop = RamBaseChopDamage,
      m_pickaxe = RamBasePickAxeDamage,
    });
  }

  public static void OnBaseSettingsChange(object sender, EventArgs eventArgs)
  {
    foreach (var instance in RamInstances.ToList())
    {
      if (!instance)
      {
        RamInstances.Remove(instance);
        continue;
      }

      if (RamConfig.RamDamageEnabled.Value)
      {
        instance.InitializeFromConfig();
        instance.InitAoe();
        instance.gameObject.SetActive(true);
      }
      else
      {
        instance.gameObject.SetActive(false);
      }
    }
  }

  public static void OnSettingsChanged(object sender, EventArgs eventArgs)
  {
    foreach (var instance in RamInstances.ToArray())
    {
      if (!instance)
      {
        RamInstances.Remove(instance);
        continue;
      }

      instance.SetBaseDamageFromConfig();
      instance.InitAoe();
    }
  }
}