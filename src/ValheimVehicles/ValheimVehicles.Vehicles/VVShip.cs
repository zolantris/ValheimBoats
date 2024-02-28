using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Serialization;
using ValheimRAFT;
using ValheimVehicles.ValheimVehicles.Prefabs;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Vehicles;

/*
 * Mostly vanilla Valheim However this is safe from other mods overriding valheim ships directly
 *
 * 2215240816:53310 53409
 */
public class VVShip : ValheimBaseGameShip, IVehicleShip
{
  public GameObject RudderObject { get; set; }

  public IWaterVehicleController Controller => _controller;

  private WaterVehicleController _controller;
  private GameObject _waterVehicle;

  public VVShip Instance => this;

  public BoxCollider FloatCollider
  {
    get => m_floatcollider;
    set => m_floatcollider = value;
  }

  public Transform ControlGuiPosition
  {
    get => m_controlGuiPos;
    set => m_controlGuiPos = value;
  }

  // private static readonly List<VVShip> s_currentShips = new();

  private new void Awake()
  {
    DontDestroyOnLoad(this.gameObject);
    base.Awake();

    Logger.LogDebug($"called Awake in VVShip, m_body {m_body}");
    if (!m_nview)
    {
      m_nview = GetComponent<ZNetView>();
    }

    InitializeWaterVehicleController();
  }

  /*
   * Only initializes the controller if the prefab is enabled (when zdo is initialized this happens)
   */
  private void InitializeWaterVehicleController()
  {
    if (!(bool)m_nview || m_nview.GetZDO() == null || (bool)_waterVehicle) return;

    enabled = true;
    Logger.LogDebug("Made it to InitializeWaterVehicleController");

    var ladders = GetComponentsInChildren<Ladder>();
    for (var i = 0; i < ladders.Length; i++) ladders[i].m_useDistance = 10f;

    // _waterVehicle = new GameObject
    // {
    //   name = PrefabNames.ValheimVehiclesShipName,
    //   layer = 0
    // };

    _controller = gameObject.AddComponent<WaterVehicleController>();
    _controller.InitializeShipValues(Instance);

    // _controller.transform.SetParent(_waterVehicle.transform);
    // _controller.ActivatePendingPiecesCoroutine();

    // waterVehicle must exist as it's own top level object
    // _waterVehicle.transform.SetParent(null);
    // _waterVehicle.transform.position = transform.position;
    // _waterVehicle.transform.rotation = transform.rotation;
  }

  public void OnDestroy()
  {
    if (_controller != null)
    {
      Destroy(_controller);
    }
  }

  public override void OnEnable()
  {
    base.OnEnable();
    if (_controller == null)
    {
      InitializeWaterVehicleController();
    }
  }

  /**
   * TODO this could be set to false for the ship as an override to allow the ship to never remove itself
   */
  // public bool CanBeRemoved()
  // {
  //   return m_players.Count == 0;
  // }
  public void FixedUpdate()
  {
    if (!(bool)_controller || !(bool)m_body || !(bool)m_floatcollider)
    {
      return;
    }

    // FixedUpdate1();
    // m_body.WakeUp();
    // var worldCenterOfMass = m_body.worldCenterOfMass;
    // var currentDepth = Floating.GetWaterLevel(worldCenterOfMass, ref m_previousCenter);
    // m_body.useGravity = _controller.m_zsync.m_useGravity =
    //   _controller.m_targetHeight == 0f;
    //
    // if (m_body.position.y <= currentDepth)
    // {
    //   var vectorForceMult =
    //     Mathf.Clamp01(Mathf.Abs(m_body.position.y - currentDepth) / m_forceDistance);
    //   m_body.AddForceAtPosition(Vector3.up * vectorForceMult, worldCenterOfMass,
    //     ForceMode.VelocityChange);
    // }

    ValheimRaftCustomFixedUpdate();
  }

  private static Vector3 CalculateAnchorStopVelocity(Vector3 currentVelocity)
  {
    var zeroVelocity = Vector3.zero;
    return Vector3.SmoothDamp(currentVelocity * 0.5f, Vector3.zero, ref zeroVelocity, 5f);
  }

  /**
   * This is the original code for updating ship speed
   */
  public void ValheimRaftCustomFixedUpdate()
  {
    if (!(bool)_controller || !(bool)m_nview || m_nview.m_zdo == null) return;

    /*
     * creative mode should not allows movement and applying force on a object will cause errors when the object is kinematic
     */
    if (_controller.isCreative)
    {
      return;
    }

    // This could be the spot that causes the raft to fly at spawn
    _controller.m_targetHeight =
      m_nview.m_zdo.GetFloat("MBTargetHeight", _controller.m_targetHeight);
    _controller.VehicleFlags =
      (WaterVehicleFlags)m_nview.m_zdo.GetInt("MBFlags",
        (int)_controller.VehicleFlags);

    // This could be the spot that causes the raft to fly at spawn
    _controller.m_zsync.m_useGravity =
      _controller.m_targetHeight == 0f;

    var flag = HaveControllingPlayer();

    UpdateControls(Time.fixedDeltaTime);
    // UpdateSail(Time.fixedDeltaTime);
    UpdateRudder(Time.fixedDeltaTime, flag);
    if (m_players.Count == 0 ||
        _controller.VehicleFlags.HasFlag(WaterVehicleFlags
          .IsAnchored))
    {
      m_speed = Speed.Stop;
      m_rudderValue = 0f;
      if (!_controller.VehicleFlags.HasFlag(
            WaterVehicleFlags.IsAnchored))
      {
        _controller.VehicleFlags |=
          WaterVehicleFlags.IsAnchored;
        m_nview.m_zdo.Set("MBFlags", (int)_controller.VehicleFlags);
      }
    }

    if ((bool)m_nview && !m_nview.IsOwner()) return;

    // don't damage the ship lol
    // UpdateUpsideDmg(Time.fixedDeltaTime);

    if (!flag && (m_speed == Speed.Slow || m_speed == Speed.Back))
      m_speed = Speed.Stop;
    var worldCenterOfMass = m_body.worldCenterOfMass;
    var vector = m_floatcollider.transform.position +
                 m_floatcollider.transform.forward * m_floatcollider.size.z /
                 2f;
    var vector2 = m_floatcollider.transform.position -
                  m_floatcollider.transform.forward * m_floatcollider.size.z /
                  2f;
    var vector3 = m_floatcollider.transform.position -
                  m_floatcollider.transform.right * m_floatcollider.size.x /
                  2f;
    var vector4 = m_floatcollider.transform.position +
                  m_floatcollider.transform.right * m_floatcollider.size.x /
                  2f;
    var waterLevel = Floating.GetWaterLevel(worldCenterOfMass, ref m_previousCenter);
    var waterLevel2 = Floating.GetWaterLevel(vector3, ref m_previousLeft);
    var waterLevel3 = Floating.GetWaterLevel(vector4, ref m_previousRight);
    var waterLevel4 = Floating.GetWaterLevel(vector, ref m_previousForward);
    var waterLevel5 = Floating.GetWaterLevel(vector2, ref m_previousBack);
    var averageWaterHeight =
      (waterLevel + waterLevel2 + waterLevel3 + waterLevel4 + waterLevel5) / 5f;
    var currentDepth = worldCenterOfMass.y - averageWaterHeight - m_waterLevelOffset;
    if (!(currentDepth > m_disableLevel))
    {
      _controller.UpdateStats(false);
      m_body.WakeUp();
      UpdateWaterForce(currentDepth, Time.fixedDeltaTime);
      var vector5 = new Vector3(vector3.x, waterLevel2, vector3.z);
      var vector6 = new Vector3(vector4.x, waterLevel3, vector4.z);
      var vector7 = new Vector3(vector.x, waterLevel4, vector.z);
      var vector8 = new Vector3(vector2.x, waterLevel5, vector2.z);
      var fixedDeltaTime = Time.fixedDeltaTime;
      var num3 = fixedDeltaTime * 50f;
      var num4 = Mathf.Clamp01(Mathf.Abs(currentDepth) / m_forceDistance);
      var vector9 = Vector3.up * m_force * num4;
      m_body.AddForceAtPosition(vector9 * num3, worldCenterOfMass,
        ForceMode.VelocityChange);
      var num5 = Vector3.Dot(m_body.velocity, transform.forward);
      var num6 = Vector3.Dot(m_body.velocity, transform.right);
      var velocity = m_body.velocity;
      var value = velocity.y * velocity.y * Mathf.Sign(velocity.y) * m_damping * num4;
      var value2 = num5 * num5 * Mathf.Sign(num5) * m_dampingForward * num4;
      var value3 = num6 * num6 * Mathf.Sign(num6) * m_dampingSideway * num4;
      velocity.y -= Mathf.Clamp(value, -1f, 1f);
      velocity -= transform.forward * Mathf.Clamp(value2, -1f, 1f);
      velocity -= transform.right * Mathf.Clamp(value3, -1f, 1f);
      if (velocity.magnitude > m_body.velocity.magnitude)
        velocity = velocity.normalized * m_body.velocity.magnitude;
      if (m_players.Count == 0 ||
          _controller.VehicleFlags.HasFlag(WaterVehicleFlags.IsAnchored))
      {
        var anchoredVelocity = CalculateAnchorStopVelocity(velocity);
        velocity = anchoredVelocity;
      }

      m_body.velocity = velocity;
      m_body.angularVelocity -=
        m_body.angularVelocity * m_angularDamping * num4;
      var num7 = 0.15f;
      var num8 = 0.5f;
      var f = Mathf.Clamp((vector7.y - vector.y) * num7, 0f - num8, num8);
      var f2 = Mathf.Clamp((vector8.y - vector2.y) * num7, 0f - num8, num8);
      var f3 = Mathf.Clamp((vector5.y - vector3.y) * num7, 0f - num8, num8);
      var f4 = Mathf.Clamp((vector6.y - vector4.y) * num7, 0f - num8, num8);
      f = Mathf.Sign(f) * Mathf.Abs(Mathf.Pow(f, 2f));
      f2 = Mathf.Sign(f2) * Mathf.Abs(Mathf.Pow(f2, 2f));
      f3 = Mathf.Sign(f3) * Mathf.Abs(Mathf.Pow(f3, 2f));
      f4 = Mathf.Sign(f4) * Mathf.Abs(Mathf.Pow(f4, 2f));
      m_body.AddForceAtPosition(Vector3.up * f * num3, vector, ForceMode.VelocityChange);
      m_body.AddForceAtPosition(Vector3.up * f2 * num3, vector2,
        ForceMode.VelocityChange);
      m_body.AddForceAtPosition(Vector3.up * f3 * num3, vector3,
        ForceMode.VelocityChange);
      m_body.AddForceAtPosition(Vector3.up * f4 * num3, vector4,
        ForceMode.VelocityChange);
      ApplySailForce(this, num5);
      ApplyEdgeForce(Time.fixedDeltaTime);
      if (_controller.m_targetHeight > 0f)
      {
        var centerpos = m_floatcollider.transform.position;
        var centerforce = GetUpwardsForce(_controller.m_targetHeight,
          centerpos.y + m_body.velocity.y, _controller.m_liftForce);
        m_body.AddForceAtPosition(Vector3.up * centerforce, centerpos,
          ForceMode.VelocityChange);
      }
    }
    else if (_controller.m_targetHeight > 0f)
    {
      if (m_players.Count == 0 ||
          _controller.VehicleFlags.HasFlag(WaterVehicleFlags.IsAnchored))
      {
        var anchoredVelocity = CalculateAnchorStopVelocity(m_body.velocity);
        m_body.velocity = anchoredVelocity;
      }

      _controller.UpdateStats(true);
      var side1 = m_floatcollider.transform.position +
                  m_floatcollider.transform.forward * m_floatcollider.size.z /
                  2f;
      var side2 = m_floatcollider.transform.position -
                  m_floatcollider.transform.forward * m_floatcollider.size.z /
                  2f;
      var side3 = m_floatcollider.transform.position -
                  m_floatcollider.transform.right * m_floatcollider.size.x /
                  2f;
      var side4 = m_floatcollider.transform.position +
                  m_floatcollider.transform.right * m_floatcollider.size.x /
                  2f;
      var centerpos2 = m_floatcollider.transform.position;
      var corner1curforce = m_body.GetPointVelocity(side1);
      var corner2curforce = m_body.GetPointVelocity(side2);
      var corner3curforce = m_body.GetPointVelocity(side3);
      var corner4curforce = m_body.GetPointVelocity(side4);
      var side1force =
        GetUpwardsForce(_controller.m_targetHeight,
          side1.y + corner1curforce.y,
          _controller.m_balanceForce);
      var side2force =
        GetUpwardsForce(_controller.m_targetHeight,
          side2.y + corner2curforce.y,
          _controller.m_balanceForce);
      var side3force =
        GetUpwardsForce(_controller.m_targetHeight,
          side3.y + corner3curforce.y,
          _controller.m_balanceForce);
      var side4force =
        GetUpwardsForce(_controller.m_targetHeight,
          side4.y + corner4curforce.y,
          _controller.m_balanceForce);
      var centerforce2 = GetUpwardsForce(_controller.m_targetHeight,
        centerpos2.y + m_body.velocity.y, _controller.m_liftForce);

      /**
       * applies only center force to keep boat stable and not flip
       */
      m_body.AddForceAtPosition(Vector3.up * centerforce2, side1,
        ForceMode.VelocityChange);
      m_body.AddForceAtPosition(Vector3.up * centerforce2, side2,
        ForceMode.VelocityChange);
      m_body.AddForceAtPosition(Vector3.up * centerforce2, side3,
        ForceMode.VelocityChange);
      m_body.AddForceAtPosition(Vector3.up * centerforce2, side4,
        ForceMode.VelocityChange);
      m_body.AddForceAtPosition(Vector3.up * centerforce2, centerpos2,
        ForceMode.VelocityChange);


      // m_body.AddForceAtPosition(Vector3.up * side1force, side1,
      //   ForceMode.VelocityChange);
      // m_body.AddForceAtPosition(Vector3.up * side2force, side2,
      //   ForceMode.VelocityChange);
      // m_body.AddForceAtPosition(Vector3.up * side3force, side3,
      //   ForceMode.VelocityChange);
      // m_body.AddForceAtPosition(Vector3.up * side4force, side4,
      //   ForceMode.VelocityChange);
      // m_body.AddForceAtPosition(Vector3.up * centerforce2, centerpos2,
      //   ForceMode.VelocityChange);
      var dir = Vector3.Dot(m_body.velocity, transform.forward);
      ApplySailForce(this, dir);
    }
  }

  private static void ApplySailForce(VVShip instance, float num5)
  {
    var sailArea = 0f;

    if ((bool)instance._controller)
    {
      sailArea = instance._controller.GetSailingForce();
    }

    /*
     * Computed sailSpeed based on the rudder settings.
     */
    switch (instance.m_speed)
    {
      case Speed.Full:
        break;
      case Speed.Half:
        sailArea *= 0.5f;
        break;
      case Speed.Slow:
        sailArea = Math.Min(0.1f, sailArea * 0.1f);
        break;
      case Speed.Stop:
      case Speed.Back:
      default:
        sailArea = 0f;
        break;
    }

    if (instance._controller.VehicleFlags.HasFlag(WaterVehicleFlags.IsAnchored))
    {
      sailArea = 0f;
    }

    var sailForce = instance.GetSailForce(sailArea, Time.fixedDeltaTime);

    var position = instance.m_body.worldCenterOfMass;


    //  * Math.Max(0.5f, ValheimRaftPlugin.Instance.RaftSailForceMultiplier.Value)
    // set the speed, this may need to be converted to a vector for the multiplier
    instance.m_body.AddForceAtPosition(
      sailForce,
      position,
      ForceMode.VelocityChange);

    var stearoffset = instance.m_floatcollider.transform.position -
                      instance.m_floatcollider.transform.forward *
                      instance.m_floatcollider.size.z / 2f;
    var num7 = num5 * instance.m_stearVelForceFactor;
    instance.m_body.AddForceAtPosition(
      instance.transform.right * num7 * (0f - instance.m_rudderValue) * Time.fixedDeltaTime,
      stearoffset, ForceMode.VelocityChange);
    var stearforce = Vector3.zero;
    switch (instance.m_speed)
    {
      case Speed.Slow:
        stearforce += instance.transform.forward * instance.m_backwardForce *
                      (1f - Mathf.Abs(instance.m_rudderValue));
        break;
      case Speed.Back:
        stearforce += -instance.transform.forward * instance.m_backwardForce *
                      (1f - Mathf.Abs(instance.m_rudderValue));
        break;
    }

    if (instance.m_speed == Speed.Back || instance.m_speed == Speed.Slow)
    {
      float num6 = instance.m_speed != Speed.Back ? 1 : -1;
      stearforce += instance.transform.right * instance.m_stearForce *
                    (0f - instance.m_rudderValue) * num6;
    }

    instance.m_body.AddForceAtPosition(stearforce * Time.fixedDeltaTime, stearoffset,
      ForceMode.VelocityChange);
  }
}