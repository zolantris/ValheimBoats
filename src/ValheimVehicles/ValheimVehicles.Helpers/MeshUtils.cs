﻿using UnityEngine;

namespace ValheimVehicles.Helpers;

public class MeshUtils
{
  public float SignedVolumeOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
  {
    var v321 = p3.x * p2.y * p1.z;
    var v231 = p2.x * p3.y * p1.z;
    var v312 = p3.x * p1.y * p2.z;
    var v132 = p1.x * p3.y * p2.z;
    var v213 = p2.x * p1.y * p3.z;
    var v123 = p1.x * p2.y * p3.z;

    return (1.0f / 6.0f) * (-v321 + v231 + v312 - v132 - v213 + v123);
  }

  public float VolumeOfMesh(Mesh mesh)
  {
    float volume = 0;

    var vertices = mesh.vertices;
    var triangles = mesh.triangles;

    for (var i = 0; i < triangles.Length; i += 3)
    {
      var p1 = vertices[triangles[i + 0]];
      var p2 = vertices[triangles[i + 1]];
      var p3 = vertices[triangles[i + 2]];
      volume += SignedVolumeOfTriangle(p1, p2, p3);
    }
    return Mathf.Abs(volume);
  }
}