using System;
using System.Collections.Generic;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ZdoWatcher;

public class ZdoWatchManager
{
  public static Action<ZDO>? OnDeserialize = null;
  public static Action<ZDO>? OnLoad = null;
  public static Action<ZDO>? OnReset = null;

  public static readonly ZdoWatchManager Instance = new();
  private readonly Dictionary<int, ZDO> _zdoGuidLookup = new();

  public void Reset() => _zdoGuidLookup.Clear();

  /// <summary>
  /// PersistentIds many need to migrate to a safer structure such as using uuid.v4 or similar logic for larger longer lasting games
  /// </summary>
  /// <returns></returns>
  // public static int ParsePersistentIdString(string persistentString)
  // {
  //   // if (id == 0)
  //   // {
  //   //   // var outputString = zdo.GetString(PersistentUidHash, "");
  //   //   // if (outputString != "")
  //   //   // {
  //   //   //   id = ParsePersistentIdString(outputString);
  //   //   // }
  //   // }
  // }
  public static bool GetPersistentID(ZDO zdo, out int id)
  {
    id = zdo.GetInt(ZdoVarManager.PersistentUidHash, 0);
    return id != 0;
  }

  public static int ZdoIdToId(ZDOID zdoid) =>
    (int)zdoid.UserID + (int)zdoid.ID;

  public int GetOrCreatePersistentID(ZDO? zdo)
  {
    zdo ??= new ZDO();

    var id = zdo.GetInt(ZdoVarManager.PersistentUidHash, 0);
    if (id != 0) return id;
    id = ZdoIdToId(zdo.m_uid);

    // If the ZDO is not unique/exists in the dictionary, this number must be incremented to prevent a collision
    while (_zdoGuidLookup.ContainsKey(id))
      ++id;
    zdo.Set(ZdoVarManager.PersistentUidHash, id, false);

    _zdoGuidLookup[id] = zdo;

    return id;
  }

  public void HandleRegisterPersistentId(ZDO zdo)
  {
    if (!GetPersistentID(zdo, out var id))
    {
      return;
    }

    _zdoGuidLookup[id] = zdo;
  }

  private void HandleDeregisterPersistentId(ZDO zdo)
  {
    if (!GetPersistentID(zdo, out var id))
      return;

    _zdoGuidLookup.Remove(id);
  }

  public void Deserialize(ZDO zdo)
  {
    HandleRegisterPersistentId(zdo);

    if (OnDeserialize == null) return;
    try
    {
      OnDeserialize(zdo);
    }
    catch
    {
      Logger.LogError("OnDeserialize had an error");
    }
  }

  public void Load(ZDO zdo)
  {
    HandleRegisterPersistentId(zdo);
    if (OnLoad == null) return;
    try
    {
      OnLoad(zdo);
    }
    catch
    {
      Logger.LogError("OnLoad had an error");
    }
  }

  public void Reset(ZDO zdo)
  {
    HandleDeregisterPersistentId(zdo);
    if (OnReset == null) return;
    try
    {
      OnReset(zdo);
    }
    catch
    {
      Logger.LogError("OnReset had an error");
    }
  }

  /// <summary>
  /// Gets the ZDO from the persistent ZDOID int
  /// </summary>
  /// <param name="id"></param>
  /// <returns>ZDO|null</returns>
  public ZDO? GetZdo(int id)
  {
    return _zdoGuidLookup.TryGetValue(id, out var zdo) ? zdo : null;
  }

  public GameObject? GetGameObject(int id)
  {
    var instance = GetInstance(id);
    return instance
      ? instance?.gameObject
      : null;
  }

  public ZNetView? GetInstance(int id)
  {
    var zdo = GetZdo(id);
    if (zdo == null) return null;
    var output = ZNetScene.instance.FindInstance(zdo);
    return output;
  }
}