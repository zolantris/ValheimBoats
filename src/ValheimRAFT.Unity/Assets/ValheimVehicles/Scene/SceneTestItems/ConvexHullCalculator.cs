using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ValheimVehicles.Scene
{
  public class ConvexHullCalculator
  {
    const int UNASSIGNED = -2;
    
    const int INSIDE = -1;
    
    const float EPSILON = 0.0001f;
    
    struct Face
    {
      public int Vertex0;
      public int Vertex1;
      public int Vertex2;

      public int Opposite0;
      public int Opposite1;
      public int Opposite2;

      public Vector3 Normal;

      public Face(int v0, int v1, int v2, int o0, int o1, int o2,
        Vector3 normal)
      {
        Vertex0 = v0;
        Vertex1 = v1;
        Vertex2 = v2;
        Opposite0 = o0;
        Opposite1 = o1;
        Opposite2 = o2;
        Normal = normal;
      }

      public bool Equals(Face other)
      {
        return (this.Vertex0 == other.Vertex0)
               && (this.Vertex1 == other.Vertex1)
               && (this.Vertex2 == other.Vertex2)
               && (this.Opposite0 == other.Opposite0)
               && (this.Opposite1 == other.Opposite1)
               && (this.Opposite2 == other.Opposite2)
               && (this.Normal == other.Normal);
      }
    }
    
    struct PointFace
    {
      public int Point;
      public int Face;
      public float Distance;

      public PointFace(int p, int f, float d)
      {
        Point = p;
        Face = f;
        Distance = d;
      }
    }
    
    struct HorizonEdge
    {
      public int Face;
      public int Edge0;
      public int Edge1;
    }
    
    Dictionary<int, Face> faces;

    List<PointFace> openSet;


    HashSet<int> litFaces;

    List<HorizonEdge> horizon;
    
    Dictionary<int, int> hullVerts;


    int openSetTail = -1;
    
    int faceCount = 0;
    
    public void GenerateHull(
      List<Vector3> points,
      bool splitVerts,
      ref List<Vector3> verts,
      ref List<int> tris,
      ref List<Vector3> normals)
    {
      if (points.Count < 4)
      {
        throw new System.ArgumentException(
          "Need at least 4 points to generate a convex hull");
      }

      Initialize(points, splitVerts);

      GenerateInitialHull(points);

      while (openSetTail >= 0)
      {
        GrowHull(points);
      }

      ExportMesh(points, splitVerts, ref verts, ref tris, ref normals);
      VerifyMesh(points, ref verts, ref tris);
    }


    void Initialize(List<Vector3> points, bool splitVerts)
    {
      faceCount = 0;
      openSetTail = -1;

      if (faces == null)
      {
        faces = new Dictionary<int, Face>();
        litFaces = new HashSet<int>();
        horizon = new List<HorizonEdge>();
        openSet = new List<PointFace>(points.Count);
      }
      else
      {
        faces.Clear();
        litFaces.Clear();
        horizon.Clear();
        openSet.Clear();

        if (openSet.Capacity < points.Count)
        {
          openSet.Capacity = points.Count;
        }
      }

      if (!splitVerts)
      {
        if (hullVerts == null)
        {
          hullVerts = new Dictionary<int, int>();
        }
        else
        {
          hullVerts.Clear();
        }
      }
    }

    /// <summary>
    ///   Create initial seed hull.
    /// </summary>
    void GenerateInitialHull(List<Vector3> points)
    {
      int b0, b1, b2, b3;
      FindInitialHullIndices(points, out b0, out b1, out b2, out b3);

      var v0 = points[b0];
      var v1 = points[b1];
      var v2 = points[b2];
      var v3 = points[b3];

      var above = Dot(v3 - v1, Cross(v1 - v0, v2 - v0)) > 0.0f;

      faceCount = 0;
      if (above)
      {
        faces[faceCount++] = new Face(b0, b2, b1, 3, 1, 2,
          Normal(points[b0], points[b2], points[b1]));
        faces[faceCount++] = new Face(b0, b1, b3, 3, 2, 0,
          Normal(points[b0], points[b1], points[b3]));
        faces[faceCount++] = new Face(b0, b3, b2, 3, 0, 1,
          Normal(points[b0], points[b3], points[b2]));
        faces[faceCount++] = new Face(b1, b2, b3, 2, 1, 0,
          Normal(points[b1], points[b2], points[b3]));
      }
      else
      {
        faces[faceCount++] = new Face(b0, b1, b2, 3, 2, 1,
          Normal(points[b0], points[b1], points[b2]));
        faces[faceCount++] = new Face(b0, b3, b1, 3, 0, 2,
          Normal(points[b0], points[b3], points[b1]));
        faces[faceCount++] = new Face(b0, b2, b3, 3, 1, 0,
          Normal(points[b0], points[b2], points[b3]));
        faces[faceCount++] = new Face(b1, b3, b2, 2, 0, 1,
          Normal(points[b1], points[b3], points[b2]));
      }

      VerifyFaces(points);

      // Create the openSet. Add all points except the points of the seed
      // hull.
      for (int i = 0; i < points.Count; i++)
      {
        if (i == b0 || i == b1 || i == b2 || i == b3) continue;

        openSet.Add(new PointFace(i, UNASSIGNED, 0.0f));
      }

      // Add the seed hull verts to the tail of the list.
      openSet.Add(new PointFace(b0, INSIDE, float.NaN));
      openSet.Add(new PointFace(b1, INSIDE, float.NaN));
      openSet.Add(new PointFace(b2, INSIDE, float.NaN));
      openSet.Add(new PointFace(b3, INSIDE, float.NaN));
      
      openSetTail = openSet.Count - 5;

      Assert(openSet.Count == points.Count);
      
      for (int i = 0; i <= openSetTail; i++)
      {
        Assert(openSet[i].Face == UNASSIGNED);
        Assert(openSet[openSetTail].Face == UNASSIGNED);
        Assert(openSet[openSetTail + 1].Face == INSIDE);

        var assigned = false;
        var fp = openSet[i];

        Assert(faces.Count == 4);
        Assert(faces.Count == faceCount);
        for (int j = 0; j < 4; j++)
        {
          Assert(faces.ContainsKey(j));

          var face = faces[j];

          var dist =
            PointFaceDistance(points[fp.Point], points[face.Vertex0], face);

          if (dist > 0)
          {
            fp.Face = j;
            fp.Distance = dist;
            openSet[i] = fp;

            assigned = true;
            break;
          }
        }

        if (!assigned)
        {
          // Point is inside
          fp.Face = INSIDE;
          fp.Distance = float.NaN;

          // Point is inside seed hull: swap point with tail, and move
          // openSetTail back. We also have to decrement i, because
          // there's a new item at openSet[i], and we need to process
          // it next iteration
          openSet[i] = openSet[openSetTail];
          openSet[openSetTail] = fp;

          openSetTail -= 1;
          i -= 1;
        }
      }

      VerifyOpenSet(points);
    }
    
    void FindInitialHullIndices(List<Vector3> points, out int b0, out int b1,
      out int b2, out int b3)
    {
      var count = points.Count;

      for (int i0 = 0; i0 < count - 3; i0++)
      {
        for (int i1 = i0 + 1; i1 < count - 2; i1++)
        {
          var p0 = points[i0];
          var p1 = points[i1];

          if (AreCoincident(p0, p1)) continue;

          for (int i2 = i1 + 1; i2 < count - 1; i2++)
          {
            var p2 = points[i2];

            if (AreCollinear(p0, p1, p2)) continue;

            for (int i3 = i2 + 1; i3 < count - 0; i3++)
            {
              var p3 = points[i3];

              if (AreCoplanar(p0, p1, p2, p3)) continue;

              b0 = i0;
              b1 = i1;
              b2 = i2;
              b3 = i3;
              return;
            }
          }
        }
      }

      throw new System.ArgumentException(
        "Can't generate hull, points are coplanar");
    }
    
    void GrowHull(List<Vector3> points)
    {
      Assert(openSetTail >= 0);
      Assert(openSet[0].Face != INSIDE);

      // Find farthest point and first lit face.
      var farthestPoint = 0;
      var dist = openSet[0].Distance;

      for (int i = 1; i <= openSetTail; i++)
      {
        if (openSet[i].Distance > dist)
        {
          farthestPoint = i;
          dist = openSet[i].Distance;
        }
      }

      // Use lit face to find horizon and the rest of the lit
      // faces.
      FindHorizon(
        points,
        points[openSet[farthestPoint].Point],
        openSet[farthestPoint].Face,
        faces[openSet[farthestPoint].Face]);

      VerifyHorizon();

      // Construct new cone from horizon
      ConstructCone(points, openSet[farthestPoint].Point);

      VerifyFaces(points);

      // Reassign points
      ReassignPoints(points);
    }
    
    void FindHorizon(List<Vector3> points, Vector3 point, int fi, Face face)
    {
      // TODO should I use epsilon in the PointFaceDistance comparisons?

      litFaces.Clear();
      horizon.Clear();

      litFaces.Add(fi);

      Assert(PointFaceDistance(point, points[face.Vertex0], face) > 0.0f);

      {
        var oppositeFace = faces[face.Opposite0];

        var dist = PointFaceDistance(
          point,
          points[oppositeFace.Vertex0],
          oppositeFace);

        if (dist <= 0.0f)
        {
          horizon.Add(new HorizonEdge
          {
            Face = face.Opposite0,
            Edge0 = face.Vertex1,
            Edge1 = face.Vertex2,
          });
        }
        else
        {
          SearchHorizon(points, point, fi, face.Opposite0, oppositeFace);
        }
      }

      if (!litFaces.Contains(face.Opposite1))
      {
        var oppositeFace = faces[face.Opposite1];

        var dist = PointFaceDistance(
          point,
          points[oppositeFace.Vertex0],
          oppositeFace);

        if (dist <= 0.0f)
        {
          horizon.Add(new HorizonEdge
          {
            Face = face.Opposite1,
            Edge0 = face.Vertex2,
            Edge1 = face.Vertex0,
          });
        }
        else
        {
          SearchHorizon(points, point, fi, face.Opposite1, oppositeFace);
        }
      }

      if (!litFaces.Contains(face.Opposite2))
      {
        var oppositeFace = faces[face.Opposite2];

        var dist = PointFaceDistance(
          point,
          points[oppositeFace.Vertex0],
          oppositeFace);

        if (dist <= 0.0f)
        {
          horizon.Add(new HorizonEdge
          {
            Face = face.Opposite2,
            Edge0 = face.Vertex0,
            Edge1 = face.Vertex1,
          });
        }
        else
        {
          SearchHorizon(points, point, fi, face.Opposite2, oppositeFace);
        }
      }
    }

    /// <summary>
    ///   Recursively search to find the horizon or lit set.
    /// </summary>
    void SearchHorizon(List<Vector3> points, Vector3 point, int prevFaceIndex,
      int faceCount, Face face)
    {
      Assert(prevFaceIndex >= 0);
      Assert(litFaces.Contains(prevFaceIndex));
      Assert(!litFaces.Contains(faceCount));
      Assert(faces[faceCount].Equals(face));

      litFaces.Add(faceCount);
      
      int nextFaceIndex0;
      int nextFaceIndex1;
      int edge0;
      int edge1;
      int edge2;

      if (prevFaceIndex == face.Opposite0)
      {
        nextFaceIndex0 = face.Opposite1;
        nextFaceIndex1 = face.Opposite2;

        edge0 = face.Vertex2;
        edge1 = face.Vertex0;
        edge2 = face.Vertex1;
      }
      else if (prevFaceIndex == face.Opposite1)
      {
        nextFaceIndex0 = face.Opposite2;
        nextFaceIndex1 = face.Opposite0;

        edge0 = face.Vertex0;
        edge1 = face.Vertex1;
        edge2 = face.Vertex2;
      }
      else
      {
        Assert(prevFaceIndex == face.Opposite2);

        nextFaceIndex0 = face.Opposite0;
        nextFaceIndex1 = face.Opposite1;

        edge0 = face.Vertex1;
        edge1 = face.Vertex2;
        edge2 = face.Vertex0;
      }

      if (!litFaces.Contains(nextFaceIndex0))
      {
        var oppositeFace = faces[nextFaceIndex0];

        var dist = PointFaceDistance(
          point,
          points[oppositeFace.Vertex0],
          oppositeFace);

        if (dist <= 0.0f)
        {
          horizon.Add(new HorizonEdge
          {
            Face = nextFaceIndex0,
            Edge0 = edge0,
            Edge1 = edge1,
          });
        }
        else
        {
          SearchHorizon(points, point, faceCount, nextFaceIndex0, oppositeFace);
        }
      }

      if (!litFaces.Contains(nextFaceIndex1))
      {
        var oppositeFace = faces[nextFaceIndex1];

        var dist = PointFaceDistance(
          point,
          points[oppositeFace.Vertex0],
          oppositeFace);

        if (dist <= 0.0f)
        {
          horizon.Add(new HorizonEdge
          {
            Face = nextFaceIndex1,
            Edge0 = edge1,
            Edge1 = edge2,
          });
        }
        else
        {
          SearchHorizon(points, point, faceCount, nextFaceIndex1, oppositeFace);
        }
      }
    }
    
    void ConstructCone(List<Vector3> points, int farthestPoint)
    {
      foreach (var fi in litFaces)
      {
        Assert(faces.ContainsKey(fi));
        faces.Remove(fi);
      }

      var firstNewFace = faceCount;

      for (int i = 0; i < horizon.Count; i++)
      {
        var v0 = farthestPoint;
        var v1 = horizon[i].Edge0;
        var v2 = horizon[i].Edge1;
        
        var o0 = horizon[i].Face;
        var o1 = (i == horizon.Count - 1) ? firstNewFace : firstNewFace + i + 1;
        var o2 = (i == 0)
          ? (firstNewFace + horizon.Count - 1)
          : firstNewFace + i - 1;

        var fi = faceCount++;

        faces[fi] = new Face(
          v0, v1, v2,
          o0, o1, o2,
          Normal(points[v0], points[v1], points[v2]));

        var horizonFace = faces[horizon[i].Face];

        if (horizonFace.Vertex0 == v1)
        {
          Assert(v2 == horizonFace.Vertex2);
          horizonFace.Opposite1 = fi;
        }
        else if (horizonFace.Vertex1 == v1)
        {
          Assert(v2 == horizonFace.Vertex0);
          horizonFace.Opposite2 = fi;
        }
        else
        {
          Assert(v1 == horizonFace.Vertex2);
          Assert(v2 == horizonFace.Vertex1);
          horizonFace.Opposite0 = fi;
        }

        faces[horizon[i].Face] = horizonFace;
      }
    }
    
    void ReassignPoints(List<Vector3> points)
    {
      for (int i = 0; i <= openSetTail; i++)
      {
        var fp = openSet[i];

        if (litFaces.Contains(fp.Face))
        {
          var assigned = false;
          var point = points[fp.Point];

          foreach (var kvp in faces)
          {
            var fi = kvp.Key;
            var face = kvp.Value;

            var dist = PointFaceDistance(
              point,
              points[face.Vertex0],
              face);

            if (dist > EPSILON)
            {
              assigned = true;

              fp.Face = fi;
              fp.Distance = dist;

              openSet[i] = fp;
              break;
            }
          }

          if (!assigned)
          {

            fp.Face = INSIDE;
            fp.Distance = float.NaN;

            openSet[i] = openSet[openSetTail];
            openSet[openSetTail] = fp;

            i--;
            openSetTail--;
          }
        }
      }
    }
    
    void ExportMesh(
      List<Vector3> points,
      bool splitVerts,
      ref List<Vector3> verts,
      ref List<int> tris,
      ref List<Vector3> normals)
    {
      if (verts == null)
      {
        verts = new List<Vector3>();
      }
      else
      {
        verts.Clear();
      }

      if (tris == null)
      {
        tris = new List<int>();
      }
      else
      {
        tris.Clear();
      }

      if (normals == null)
      {
        normals = new List<Vector3>();
      }
      else
      {
        normals.Clear();
      }

      foreach (var face in faces.Values)
      {
        int vi0, vi1, vi2;

        if (splitVerts)
        {
          vi0 = verts.Count;
          verts.Add(points[face.Vertex0]);
          vi1 = verts.Count;
          verts.Add(points[face.Vertex1]);
          vi2 = verts.Count;
          verts.Add(points[face.Vertex2]);

          normals.Add(face.Normal);
          normals.Add(face.Normal);
          normals.Add(face.Normal);
        }
        else
        {
          if (!hullVerts.TryGetValue(face.Vertex0, out vi0))
          {
            vi0 = verts.Count;
            hullVerts[face.Vertex0] = vi0;
            verts.Add(points[face.Vertex0]);
          }

          if (!hullVerts.TryGetValue(face.Vertex1, out vi1))
          {
            vi1 = verts.Count;
            hullVerts[face.Vertex1] = vi1;
            verts.Add(points[face.Vertex1]);
          }

          if (!hullVerts.TryGetValue(face.Vertex2, out vi2))
          {
            vi2 = verts.Count;
            hullVerts[face.Vertex2] = vi2;
            verts.Add(points[face.Vertex2]);
          }
        }

        tris.Add(vi0);
        tris.Add(vi1);
        tris.Add(vi2);
      }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    float PointFaceDistance(Vector3 point, Vector3 pointOnFace, Face face)
    {
      return Dot(face.Normal, point - pointOnFace);
    }

    /// <summary>
    ///   Calculate normal for triangle
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Vector3 Normal(Vector3 v0, Vector3 v1, Vector3 v2)
    {
      return Cross(v1 - v0, v2 - v0).normalized;
    }

    /// <summary>
    ///   Dot product, for convenience.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static float Dot(Vector3 a, Vector3 b)
    {
      return a.x * b.x + a.y * b.y + a.z * b.z;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Vector3 Cross(Vector3 a, Vector3 b)
    {
      return new Vector3(
        a.y * b.z - a.z * b.y,
        a.z * b.x - a.x * b.z,
        a.x * b.y - a.y * b.x);
    }

    /// <summary>
    ///   Check if two points are coincident
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool AreCoincident(Vector3 a, Vector3 b)
    {
      return (a - b).magnitude <= EPSILON;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool AreCollinear(Vector3 a, Vector3 b, Vector3 c)
    {
      return Cross(c - a, c - b).magnitude <= EPSILON;
    }

    /// <summary>
    ///   Check if four points are coplanar
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool AreCoplanar(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
      var n1 = Cross(c - a, c - b);
      var n2 = Cross(d - a, d - b);

      var m1 = n1.magnitude;
      var m2 = n2.magnitude;

      return m1 <= EPSILON
             || m2 <= EPSILON
             || AreCollinear(Vector3.zero,
               (1.0f / m1) * n1,
               (1.0f / m2) * n2);
    }
    
    [Conditional("DEBUG_QUICKHULL")]
    void VerifyOpenSet(List<Vector3> points)
    {
      for (int i = 0; i < openSet.Count; i++)
      {
        if (i > openSetTail)
        {
          Assert(openSet[i].Face == INSIDE);
        }
        else
        {
          Assert(openSet[i].Face != INSIDE);
          Assert(openSet[i].Face != UNASSIGNED);

          Assert(PointFaceDistance(
            points[openSet[i].Point],
            points[faces[openSet[i].Face].Vertex0],
            faces[openSet[i].Face]) > 0.0f);
        }
      }
    }
    
    [Conditional("DEBUG_QUICKHULL")]
    void VerifyHorizon()
    {
      for (int i = 0; i < horizon.Count; i++)
      {
        var prev = i == 0 ? horizon.Count - 1 : i - 1;

        Assert(horizon[prev].Edge1 == horizon[i].Edge0);
        Assert(HasEdge(faces[horizon[i].Face], horizon[i].Edge1,
          horizon[i].Edge0));
      }
    }

    /// <summary>
    ///   Method used for debugging, verifies that the faces array is in a
    ///   sensible state. Conditionally compiled if DEBUG_QUICKHULL if
    ///   defined.
    /// </summary>
    [Conditional("DEBUG_QUICKHULL")]
    void VerifyFaces(List<Vector3> points)
    {
      foreach (var kvp in faces)
      {
        var fi = kvp.Key;
        var face = kvp.Value;

        Assert(faces.ContainsKey(face.Opposite0));
        Assert(faces.ContainsKey(face.Opposite1));
        Assert(faces.ContainsKey(face.Opposite2));

        Assert(face.Opposite0 != fi);
        Assert(face.Opposite1 != fi);
        Assert(face.Opposite2 != fi);

        Assert(face.Vertex0 != face.Vertex1);
        Assert(face.Vertex0 != face.Vertex2);
        Assert(face.Vertex1 != face.Vertex2);

        Assert(HasEdge(faces[face.Opposite0], face.Vertex2, face.Vertex1));
        Assert(HasEdge(faces[face.Opposite1], face.Vertex0, face.Vertex2));
        Assert(HasEdge(faces[face.Opposite2], face.Vertex1, face.Vertex0));

        Assert((face.Normal - Normal(
          points[face.Vertex0],
          points[face.Vertex1],
          points[face.Vertex2])).magnitude < EPSILON);
      }
    }
    
    [Conditional("DEBUG_QUICKHULL")]
    void VerifyMesh(List<Vector3> points, ref List<Vector3> verts,
      ref List<int> tris)
    {
      Assert(tris.Count % 3 == 0);

      for (int i = 0; i < points.Count; i++)
      {
        for (int j = 0; j < tris.Count; j += 3)
        {
          var t0 = verts[tris[j]];
          var t1 = verts[tris[j + 1]];
          var t2 = verts[tris[j + 2]];

          Assert(Dot(points[i] - t0, Vector3.Cross(t1 - t0, t2 - t0)) <=
                 EPSILON);
        }
      }
    }


    bool HasEdge(Face f, int e0, int e1)
    {
      return (f.Vertex0 == e0 && f.Vertex1 == e1)
             || (f.Vertex1 == e0 && f.Vertex2 == e1)
             || (f.Vertex2 == e0 && f.Vertex0 == e1);
    }

    [Conditional("DEBUG_QUICKHULL")]
    static void Assert(bool condition)
    {
      if (!condition)
      {
        throw new UnityEngine.Assertions.AssertionException("Assertion failed",
          "");
      }
    }
  }
}