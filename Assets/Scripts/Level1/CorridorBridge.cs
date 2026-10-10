using System.Collections.Generic;
using PerspectivePuzzle.Movement;
using UnityEngine;

namespace PerspectivePuzzle.Level1
{
    /// <summary>
    /// Seats the walk on the imported floors. Two ledges stay apart in the building
    /// and share one screen edge only at the corridor yaw.
    /// </summary>
    public static class CorridorBridge
    {
        public struct FloorRect
        {
            public float minX;
            public float maxX;
            public float minZ;
            public float maxZ;
            public float top;
        }

        public static readonly Vector3 PoleStand = new Vector3(-6.05f, 12.49f, -5.75f);
        /// <summary>
        /// Step from the landing lip onto the balcony. Screen length collapses only at the corridor yaw.
        /// </summary>
        public static readonly Vector3 StairLandingSeam = new Vector3(-0.45f, 0.05f, 0f);
        public static readonly Vector3 SpeakerRest = new Vector3(0f, 0.55f, 0f);
        public static readonly Vector3 ShelfS4 = new Vector3(-7.53f, 10.66f, -6.79f);
        public static readonly Vector3 ShelfLip = new Vector3(-8.00f, 10.66f, -6.80f);
        public static readonly Vector3 SlopeFoot = new Vector3(-8.00f, 11.18f, -6.80f);

        public static bool TryJoin(IList<FloorRect> floors, out Vector3 approachLip, out Vector3 otherLip)
        {
            approachLip = Vector3.zero;
            otherLip = Vector3.zero;
            float best = 0f;
            bool found = false;
            for (int i = 0; i < floors.Count; i++)
            {
                for (int j = i + 1; j < floors.Count; j++)
                {
                    FloorRect a = floors[i];
                    FloorRect b = floors[j];
                    if (Mathf.Abs(a.top - b.top) > 0.25f)
                        continue;

                    // A small overlap is a floor that was lengthened until it tucks under the other.
                    bool aLeft = a.maxX <= b.minX + 0.6f && a.minX < b.minX - 0.2f;
                    bool bLeft = b.maxX <= a.minX + 0.6f && b.minX < a.minX - 0.2f;
                    if (!aLeft && !bLeft)
                        continue;

                    FloorRect left = aLeft ? a : b;
                    FloorRect right = aLeft ? b : a;
                    float gap = right.minX - left.maxX;
                    if (gap < -0.6f || gap > 2.2f)
                        continue;

                    float z0 = Mathf.Max(left.minZ, right.minZ);
                    float z1 = Mathf.Min(left.maxZ, right.maxZ);
                    if (z1 - z0 < 1.2f)
                        continue;

                    Vector3 edgeOffset = new Vector3(gap, right.top - left.top, 0f);
                    float screen = PerspectiveGap.ScreenGap(StoryProgress.CorridorYaw, edgeOffset);
                    if (screen > 0.35f)
                        continue;

                    float score = (z1 - z0) / (screen + 0.02f);
                    if (score <= best)
                        continue;

                    best = score;
                    float z = (z0 + z1) * 0.5f;
                    float y = Mathf.Max(left.top, right.top) + 0.04f;
                    FloorRect approach = Area(left) >= Area(right) ? left : right;
                    FloorRect other = approach.minX == left.minX && approach.maxX == left.maxX ? right : left;
                    bool approachOnRight = (approach.minX + approach.maxX) > (other.minX + other.maxX);
                    approachLip = Lip(approach, z, y, towardPositiveX: approachOnRight);
                    otherLip = Lip(other, z, y, towardPositiveX: !approachOnRight);
                    found = true;
                }
            }

            return found;
        }

        public static void Align(
            PathNode start,
            PathNode gapFrom,
            PathNode gapTo,
            PathNode speaker,
            PathNode door,
            Transform roomFocus,
            RoomInteractable cabinet)
        {
            if (gapFrom == null || gapFrom.transform.parent == null)
                return;

            Transform nodes = gapFrom.transform.parent;
            Transform root = nodes;
            // Opening view is the saved isometric front. The seam opens at CorridorYaw.
            while (root.parent != null)
                root = root.parent;

            ClearBar(nodes, "BridgeNear");
            ClearBar(nodes, "BridgeFar");

            Transform house = root.Find("House");
            List<FloorRect> floors = house != null ? Collect(root, house) : new List<FloorRect>();
            SeatAll(nodes, root, floors);

            if (SeatGuide(root, nodes, start, gapFrom, gapTo, speaker, door, roomFocus, cabinet))
                return;

            if (!TryJoin(floors, out Vector3 approachLip, out Vector3 otherLip))
                return;

            Vector3 approachCenter = CenterOf(floors, approachLip);
            Vector3 otherCenter = CenterOf(floors, otherLip);
            Put(gapFrom, root, approachLip);
            Put(gapTo, root, otherLip);
            Put(FindNode(nodes, "A"), root, approachCenter);
            Put(FindNode(nodes, "A2"), root, Vector3.Lerp(approachCenter, approachLip, 0.34f));
            Put(FindNode(nodes, "Stair"), root, Vector3.Lerp(approachCenter, approachLip, 0.68f));
            Put(start, root, approachCenter);
            Put(speaker, root, Vector3.Lerp(otherLip, otherCenter, 0.45f));
            Put(door, root, otherCenter);
            Put(roomFocus, root, otherCenter + Vector3.up * 1.2f);
            Put(cabinet, root, otherCenter + (otherCenter - otherLip).normalized * 0.4f);
            Put(root.Find("Table"), root, otherCenter + new Vector3(0.8f, 0.4f, 0.6f));
            Put(root.Find("Shelf"), root, otherCenter + new Vector3(-0.8f, 0.8f, -0.4f));
        }

        /// <summary>
        /// Red round 1 climbs the real stair onto the landing.
        /// The landing ledge and the room ledge meet on screen only at the corridor yaw.
        /// </summary>
        static bool SeatGuide(
            Transform root,
            Transform nodes,
            PathNode start,
            PathNode gapFrom,
            PathNode gapTo,
            PathNode speaker,
            PathNode door,
            Transform roomFocus,
            RoomInteractable cabinet)
        {
            const float ground = 0.97f;
            const float upper = 8.11f;

            Put(FindNode(nodes, "A"), root, new Vector3(-4.2f, ground, 0.2f));
            Put(start, root, new Vector3(-4.2f, ground, 0.2f));
            PathNode a2 = FindNode(nodes, "A2");
            PathNode stair = FindNode(nodes, "Stair");
            Put(a2, root, new Vector3(0.25f, ground, -0.95f));
            // One node on each Plane.002 tread. A longer diagonal leaves the steps.
            string[] lowerNames =
            {
                "Tread1", "L2", "L3", "L4", "L5", "L6", "L7", "Tread2", "L8", "L9", "Tread3", "L11"
            };
            Vector3[] lowerPos =
            {
                new Vector3(0.75f, 1.36f, -0.10f),
                new Vector3(0.75f, 1.68f, 0.44f),
                new Vector3(0.75f, 2.02f, 0.99f),
                new Vector3(0.75f, 2.31f, 1.46f),
                new Vector3(0.75f, 2.60f, 1.95f),
                new Vector3(0.75f, 2.90f, 2.44f),
                new Vector3(0.75f, 3.19f, 2.92f),
                new Vector3(0.75f, 3.49f, 3.42f),
                new Vector3(0.75f, 3.80f, 3.93f),
                new Vector3(0.75f, 4.10f, 4.42f),
                new Vector3(0.75f, 4.39f, 4.90f),
                new Vector3(0.75f, 4.70f, 5.41f)
            };
            if (a2 != null && stair != null)
                a2.Unlink(stair);
            PathNode prev = a2;
            for (int i = 0; i < lowerNames.Length; i++)
            {
                PathNode step = Ensure(nodes, lowerNames[i], root, lowerPos[i]);
                if (prev != null)
                    prev.Link(step);
                prev = step;
            }
            Put(stair, root, StairSpan.NearLip);
            if (prev != null && stair != null)
                prev.Link(stair);
            HideDeck("LedgeNear");
            HideDeck("LedgeFar");
            BuildLip(root);
            PathNode landing = Ensure(nodes, "Landing", root, new Vector3(4.60f, 5.34f, 4.60f));
            PathNode spanFar = Ensure(nodes, "SpanFar", root, StairSpan.FarLip);
            PathNode nearMid = Ensure(nodes, "NearMid", root, new Vector3(3.70f, 5.34f, 5.50f));
            if (stair != null)
            {
                stair.Unlink(gapFrom);
                stair.Unlink(landing);
            }
            if (spanFar != null)
                spanFar.Link(landing);
            landing.Link(nearMid);
            if (nearMid != null)
                nearMid.Link(gapFrom);

            Put(gapFrom, root, new Vector3(3.20f, 5.34f, 6.20f));
            Put(gapTo, root, new Vector3(5.20f, 5.34f, 3.60f));
            if (gapFrom != null && gapTo != null)
                gapFrom.Link(gapTo);
            const float yRoom = 5.34f;
            Put(speaker, root, new Vector3(5.60f, yRoom, 2.20f));
            Put(door, root, new Vector3(4.40f, yRoom, 1.20f));
            Put(roomFocus, root, new Vector3(5.20f, yRoom + 1.2f, 2.80f));
            Put(cabinet, root, new Vector3(6.00f, yRoom + 0.86f, 1.80f));
            Put(root.Find("Table"), root, new Vector3(4.80f, yRoom + 0.41f, 4.00f));
            Put(root.Find("Shelf"), root, new Vector3(6.20f, yRoom + 0.86f, 3.60f));

            // Dog stays on Plane.002, the lower stair. It is a branch, not the way up.
            PathNode dog = FindNode(nodes, "Dog");
            Put(dog, root, new Vector3(0.90f, 3.49f, 3.42f));
            if (door != null && dog != null)
                door.Unlink(dog);
            PathNode safe = FindNode(nodes, "Safe");
            if (door != null && safe != null)
                door.Unlink(safe);
            Put(safe, root, new Vector3(1.50f, upper, 2.00f));

            // Plane.003, one node on each tread, from the foot beside the landing up to Cube.056.
            Ensure(nodes, "Up1", root, new Vector3(2.25f, 4.83f, 5.40f));
            Ensure(nodes, "Up2", root, new Vector3(2.25f, 5.13f, 4.90f));
            Ensure(nodes, "Up3", root, new Vector3(2.25f, 5.43f, 4.40f));
            Ensure(nodes, "Up4", root, new Vector3(2.25f, 5.73f, 3.90f));
            Ensure(nodes, "Up5", root, new Vector3(2.25f, 6.03f, 3.40f));
            Ensure(nodes, "Up6", root, new Vector3(2.25f, 6.33f, 2.90f));
            Ensure(nodes, "Up7", root, new Vector3(2.25f, 6.63f, 2.40f));
            Ensure(nodes, "Up8", root, new Vector3(2.25f, 6.93f, 1.90f));
            Ensure(nodes, "Up9", root, new Vector3(2.25f, 7.23f, 1.40f));
            Ensure(nodes, "Up10", root, new Vector3(2.25f, 7.53f, 0.48f));
            EnsureMark(nodes, "E", root, new Vector3(2.40f, upper, 0.35f));
            Put(FindNode(nodes, "F"), root, new Vector3(5.50f, upper, -2.00f));
            Put(FindNode(nodes, "I"), root, new Vector3(6.40f, upper, 2.80f));
            Put(FindNode(nodes, "L"), root, new Vector3(3.00f, upper, -5.20f));
            Put(FindNode(nodes, "G"), root, new Vector3(1.20f, upper, -2.80f));
            // Cube.056 ends at x=0 and Cube.040 starts at x=-1. One stride across that crack, then the west shelves.
            Ensure(nodes, "LipE", root, new Vector3(0.25f, upper, -3.40f));
            Ensure(nodes, "LipW", root, new Vector3(-1.15f, 8.09f, -3.40f));
            Ensure(nodes, "WestA", root, new Vector3(-4.20f, 8.09f, -4.80f));
            Ensure(nodes, "WestB", root, new Vector3(-7.15f, 8.09f, -6.20f));
            // Cube.021's first tread is 1.04 m above Cube.040. Cube.041's own lip steps 0.27 then 0.86.
            Ensure(nodes, "Wlip", root, new Vector3(-8.22f, 8.36f, -4.20f));
            Ensure(nodes, "Wmid", root, new Vector3(-8.22f, 9.22f, -4.90f));
            Ensure(nodes, "Whip", root, new Vector3(-8.22f, 9.22f, -6.40f));
            Ensure(nodes, "S1", root, new Vector3(-7.42f, 9.13f, -6.38f));
            Ensure(nodes, "S2", root, new Vector3(-7.42f, 9.55f, -6.38f));
            Ensure(nodes, "S3", root, new Vector3(-7.42f, 10.04f, -6.38f));
            Ensure(nodes, "S4", root, ShelfS4);
            Ensure(nodes, "ShelfW", root, ShelfLip);
            Vector3 slope = SlopeFoot;
            if (TryMeshHeight(root, "Plane", slope.x, slope.z, out float slopeY))
                slope.y = slopeY + 0.04f;
            Ensure(nodes, "SlopeS", root, slope);
            Ensure(nodes, "PlaneA", root, new Vector3(-7.50f, 11.36f, -6.20f));
            Ensure(nodes, "PlaneB", root, new Vector3(-7.05f, 11.52f, -3.20f));
            Ensure(nodes, "RoofA", root, new Vector3(-6.55f, 11.78f, -3.20f));
            Ensure(nodes, "RoofB", root, new Vector3(-5.80f, 12.28f, -4.20f));
            Put(FindNode(nodes, "M"), root, new Vector3(-5.60f, 12.49f, -5.30f));
            Put(FindNode(nodes, "Pole"), root, PoleStand);
            Put(root.Find("PoleAnchor"), root, PoleStand + SpeakerRest);
            return true;
        }

        /// <summary>
        /// Depth offset shared by the two corridor ledges. Screen gap is ~0 only at the corridor yaw.
        /// </summary>
        public static Vector3 SeamOffset()
        {
            PerspectiveGap.Split(StoryProgress.CorridorYaw, 2.6f, 1f, out Vector3 depthOff, out _);
            return depthOff;
        }

        /// <summary>
        /// Offset between the facing top edges of the ledge meshes that B and C stand on.
        /// </summary>
        public static bool TryLedgeEdges(Transform root, out Vector3 nearEdge, out Vector3 farEdge)
        {
            nearEdge = Vector3.zero;
            farEdge = Vector3.zero;
            if (root == null)
                return false;
            GameObject near = GameObject.Find("LedgeNear");
            GameObject far = GameObject.Find("LedgeFar");
            return EdgePoint(near, root, true, out nearEdge) && EdgePoint(far, root, false, out farEdge);
        }

        /// <summary>
        /// Highest up-facing point of a named mesh at this root-local xz.
        /// </summary>
        public static bool TryMeshHeight(Transform root, string meshName, float x, float z, out float y)
        {
            y = 0f;
            if (root == null || string.IsNullOrEmpty(meshName))
                return false;
            MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
            float best = float.MinValue;
            bool hit = false;
            for (int f = 0; f < filters.Length; f++)
            {
                if (filters[f].name != meshName || filters[f].sharedMesh == null)
                    continue;
                Mesh mesh = filters[f].sharedMesh;
                Vector3[] verts = mesh.vertices;
                int[] idx = mesh.triangles;
                for (int t = 0; t < idx.Length; t += 3)
                {
                    Vector3 a = root.InverseTransformPoint(filters[f].transform.TransformPoint(verts[idx[t]]));
                    Vector3 b = root.InverseTransformPoint(filters[f].transform.TransformPoint(verts[idx[t + 1]]));
                    Vector3 c = root.InverseTransformPoint(filters[f].transform.TransformPoint(verts[idx[t + 2]]));
                    Vector3 n = Vector3.Cross(b - a, c - a);
                    float mag = n.magnitude;
                    if (mag < 1e-6f || n.y / mag < 0.5f)
                        continue;
                    float minX = Mathf.Min(a.x, Mathf.Min(b.x, c.x));
                    float maxX = Mathf.Max(a.x, Mathf.Max(b.x, c.x));
                    float minZ = Mathf.Min(a.z, Mathf.Min(b.z, c.z));
                    float maxZ = Mathf.Max(a.z, Mathf.Max(b.z, c.z));
                    if (x < minX - 0.02f || x > maxX + 0.02f || z < minZ - 0.02f || z > maxZ + 0.02f)
                        continue;
                    Vector3 v0 = b - a;
                    Vector3 v1 = c - a;
                    Vector3 v2 = new Vector3(x - a.x, 0f, z - a.z);
                    float dot00 = v0.x * v0.x + v0.z * v0.z;
                    float dot01 = v0.x * v1.x + v0.z * v1.z;
                    float dot02 = v0.x * v2.x + v0.z * v2.z;
                    float dot11 = v1.x * v1.x + v1.z * v1.z;
                    float dot12 = v1.x * v2.x + v1.z * v2.z;
                    float den = dot00 * dot11 - dot01 * dot01;
                    if (Mathf.Abs(den) < 1e-8f)
                        continue;
                    float u = (dot11 * dot02 - dot01 * dot12) / den;
                    float v = (dot00 * dot12 - dot01 * dot02) / den;
                    if (u < -0.02f || v < -0.02f || u + v > 1.02f)
                        continue;
                    float hy = a.y + u * (b.y - a.y) + v * (c.y - a.y);
                    if (hy > best)
                    {
                        best = hy;
                        hit = true;
                    }
                }
            }

            if (!hit)
                return false;
            y = best;
            return true;
        }

        public static bool TryLedgeSurfaceOffset(out Vector3 offset)
        {
            offset = Vector3.zero;
            GameObject rootGo = GameObject.Find("BuildingRoot");
            if (rootGo == null || !TryLedgeEdges(rootGo.transform, out Vector3 nearEdge, out Vector3 farEdge))
                return false;
            offset = farEdge - nearEdge;
            return true;
        }

        public static bool TryLedgeWalkLine(out Vector3 nearBack, out Vector3 farEnd)
        {
            nearBack = Vector3.zero;
            farEnd = Vector3.zero;
            GameObject rootGo = GameObject.Find("BuildingRoot");
            if (rootGo == null)
                return false;
            Transform root = rootGo.transform;
            return EdgePoint(GameObject.Find("LedgeNear"), root, false, out nearBack)
                && EdgePoint(GameObject.Find("LedgeFar"), root, true, out farEnd);
        }

        static bool EdgePoint(GameObject go, Transform root, bool maxZ, out Vector3 point)
        {
            point = Vector3.zero;
            if (go == null)
                return false;
            MeshFilter filter = go.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            if (mesh == null)
                return false;

            Vector3[] verts = mesh.vertices;
            float top = float.MinValue;
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 local = root.InverseTransformPoint(go.transform.TransformPoint(verts[i]));
                if (local.y > top)
                    top = local.y;
            }

            float seamZ = maxZ ? float.MinValue : float.MaxValue;
            bool anyTop = false;
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 local = root.InverseTransformPoint(go.transform.TransformPoint(verts[i]));
                if (local.y < top - 0.02f)
                    continue;
                anyTop = true;
                if (maxZ ? local.z > seamZ : local.z < seamZ)
                    seamZ = local.z;
            }

            if (!anyTop)
                return false;

            Vector3 sum = Vector3.zero;
            int count = 0;
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 local = root.InverseTransformPoint(go.transform.TransformPoint(verts[i]));
                if (local.y < top - 0.02f || Mathf.Abs(local.z - seamZ) > 0.02f)
                    continue;
                sum += local;
                count++;
            }

            if (count == 0)
                return false;
            point = sum / count;
            return true;
        }

        static float Area(FloorRect floor)
        {
            return Mathf.Max(0f, floor.maxX - floor.minX) * Mathf.Max(0f, floor.maxZ - floor.minZ);
        }

        static Vector3 Lip(FloorRect floor, float z, float y, bool towardPositiveX)
        {
            float x = towardPositiveX ? floor.minX + 0.35f : floor.maxX - 0.35f;
            return new Vector3(x, y, z);
        }

        static Vector3 CenterOf(List<FloorRect> floors, Vector3 lip)
        {
            FloorRect best = floors[0];
            float bestDist = float.MaxValue;
            for (int i = 0; i < floors.Count; i++)
            {
                FloorRect floor = floors[i];
                if (Mathf.Abs(floor.top - lip.y) > 0.4f)
                    continue;
                if (lip.x < floor.minX - 0.2f || lip.x > floor.maxX + 0.2f || lip.z < floor.minZ - 0.2f || lip.z > floor.maxZ + 0.2f)
                    continue;
                float dx = lip.x - (floor.minX + floor.maxX) * 0.5f;
                float dz = lip.z - (floor.minZ + floor.maxZ) * 0.5f;
                float dist = dx * dx + dz * dz;
                if (dist >= bestDist)
                    continue;
                bestDist = dist;
                best = floor;
            }

            return new Vector3((best.minX + best.maxX) * 0.5f, lip.y, (best.minZ + best.maxZ) * 0.5f);
        }

        static void SeatAll(Transform nodes, Transform root, List<FloorRect> floors)
        {
            PathNode[] path = nodes.GetComponentsInChildren<PathNode>(true);
            for (int i = 0; i < path.Length; i++)
            {
                Vector3 local = root.InverseTransformPoint(path[i].transform.position);
                if (!FloorTop(floors, local.x, local.z, out float top))
                    continue;
                if (local.y > top - 0.15f && local.y < top + 1.6f)
                    continue;
                local.y = top + 0.04f;
                Put(path[i], root, local);
            }
        }

        static bool FloorTop(List<FloorRect> floors, float x, float z, out float top)
        {
            top = 0f;
            bool hit = false;
            for (int i = 0; i < floors.Count; i++)
            {
                FloorRect floor = floors[i];
                if (x < floor.minX || x > floor.maxX || z < floor.minZ || z > floor.maxZ)
                    continue;
                if (hit && floor.top < top)
                    continue;
                top = floor.top;
                hit = true;
            }

            return hit;
        }

        static List<FloorRect> Collect(Transform root, Transform house)
        {
            var floors = new List<FloorRect>();
            MeshFilter[] filters = house.GetComponentsInChildren<MeshFilter>();
            for (int i = 0; i < filters.Length; i++)
            {
                Mesh mesh = filters[i].sharedMesh;
                if (mesh == null)
                    continue;

                Bounds bounds = mesh.bounds;
                Vector3 ext = bounds.extents;
                Vector3 center = bounds.center;
                Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                for (int c = 0; c < 8; c++)
                {
                    Vector3 corner = center + new Vector3(
                        (c & 1) == 0 ? -ext.x : ext.x,
                        (c & 2) == 0 ? -ext.y : ext.y,
                        (c & 4) == 0 ? -ext.z : ext.z);
                    Vector3 local = root.InverseTransformPoint(filters[i].transform.TransformPoint(corner));
                    min = Vector3.Min(min, local);
                    max = Vector3.Max(max, local);
                }

                Vector3 size = max - min;
                if (size.y > 0.55f || size.x * size.z < 2f)
                    continue;

                floors.Add(new FloorRect
                {
                    minX = min.x,
                    maxX = max.x,
                    minZ = min.z,
                    maxZ = max.z,
                    top = max.y
                });
            }

            return floors;
        }

        static PathNode FindNode(Transform nodes, string name)
        {
            Transform child = nodes.Find(name);
            return child != null ? child.GetComponent<PathNode>() : null;
        }

        static PathNode Ensure(Transform nodes, string name, Transform root, Vector3 local)
        {
            PathNode node = FindNode(nodes, name);
            if (node == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(nodes, false);
                node = go.AddComponent<PathNode>();
            }

            Put(node, root, local);
            return node;
        }

        static PathNode EnsureMark(Transform nodes, string name, Transform root, Vector3 local)
        {
            PathNode node = FindNode(nodes, name);
            if (node == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = name;
                go.transform.SetParent(nodes, false);
                go.transform.localScale = new Vector3(0.7f, 0.04f, 0.7f);
                node = go.AddComponent<PathNode>();
                var way = go.AddComponent<Waypoint>();
                way.marker = go.GetComponent<Renderer>();
                way.click = go.GetComponent<Collider>();
                way.marker.material.color = new Color(0.93f, 0.72f, 0.28f);
                way.Show(false);
            }

            Put(node, root, local);
            return node;
        }

        static void Put(Component target, Transform root, Vector3 local)
        {
            if (target != null)
                target.transform.position = root.TransformPoint(local);
        }

        static void Put(Transform target, Transform root, Vector3 local)
        {
            if (target != null)
                target.position = root.TransformPoint(local);
        }

        static void ClearBar(Transform parent, string name)
        {
            Transform bar = parent.Find(name);
            if (bar != null)
                Object.Destroy(bar.gameObject);
        }

        static void BuildLip(Transform root)
        {
            // The extra tiled slab covered the white stair. It is not the corridor.
            Transform old = root.Find("CorridorLip");
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
        }

        static void HideDeck(string name)
        {
            GameObject deck = GameObject.Find(name);
            if (deck == null)
                return;
            Renderer[] views = deck.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < views.Length; i++)
                views[i].enabled = false;
            Collider[] blocks = deck.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < blocks.Length; i++)
                blocks[i].enabled = false;
        }
    }

    /// <summary>
    /// Kept so an old lip instance still compiles. The stair-covering slab is no longer built.
    /// </summary>
    public class CorridorStretch : MonoBehaviour
    {
        void LateUpdate()
        {
            ApplyNow();
        }

        public void ApplyNow()
        {
        }
    }
}
