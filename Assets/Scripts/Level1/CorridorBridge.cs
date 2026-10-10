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

        public static readonly Vector3 PoleStand = new Vector3(-7.25f, 12.28f, -5.75f);
        /// <summary>
        /// Step from the landing lip onto the balcony. Screen length collapses only at the corridor yaw.
        /// </summary>
        public static readonly Vector3 StairLandingSeam = new Vector3(-0.45f, 0.05f, 0f);
        public static readonly Vector3 SpeakerRest = new Vector3(0f, 0.55f, 0f);
        // Cube.021 shelf top 10.00, Cube.041 south wall top 10.62, Plane eave 11.185 at x=-8.5.
        public static readonly Vector3 ShelfS4 = new Vector3(-8.10f, 10.04f, -6.20f);
        public static readonly Vector3 ShelfLip = new Vector3(-8.50f, 10.66f, -6.62f);
        public static readonly Vector3 SlopeFoot = new Vector3(-8.50f, 11.22f, -6.62f);

        // CauThang1: center x 0.08, rises +z. Tread k top = 1.17 + 0.30k, centered z = -0.85 + 0.50k. Top tread 5.07.
        public static readonly string[] LowerTreads =
        {
            "Tread1", "L2", "L3", "L4", "L5", "L6", "L7", "Tread2", "L8", "L9", "Tread3", "L11", "L12"
        };
        public static readonly Vector3 StairTop = new Vector3(0.08f, 5.11f, 5.80f);

        // CauThang2: center x 1.6, rises -z from the Tang2 walkway. Tread j top = 5.35 + 0.30j, z = 5.62 - 0.50j.
        // Up10 stands on its top landing (8.05) beside Tang3.
        public static readonly string[] UpperTreads =
        {
            "Up1", "Up2", "Up3", "Up4", "Up5", "Up6", "Up7", "Up8", "Up9", "Up10"
        };

        public static Vector3 LowerTread(int k)
        {
            return new Vector3(0.08f, 1.21f + 0.30f * k, -0.85f + 0.50f * k);
        }

        public static Vector3 UpperTread(int j)
        {
            if (j >= 9)
                return new Vector3(1.60f, 8.09f, 0.75f);
            return new Vector3(1.60f, 5.39f + 0.30f * j, 5.62f - 0.50f * j);
        }

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
            // All heights below are surface + 0.04, measured on the GỜ ÊM GÊM triangles (BuildingRoot local).
            const float ground = 0.97f;   // Tang1 courtyard 0.93
            const float floor2 = 5.34f;   // Tang2 walkway and room 5.30
            const float floor3 = 8.09f;   // Tang3 8.05

            // Round 1, red: courtyard, up CauThang1 (rises +z, treads every 0.5 m, 0.30 m rise), onto the landing.
            Put(FindNode(nodes, "A"), root, new Vector3(-3.00f, ground, -1.00f));
            Put(start, root, new Vector3(-3.00f, ground, -1.00f));
            PathNode a2 = FindNode(nodes, "A2");
            PathNode stair = FindNode(nodes, "Stair");
            Put(a2, root, new Vector3(0.08f, ground, -1.55f));
            if (a2 != null && stair != null)
                a2.Unlink(stair);
            PathNode prev = a2;
            for (int i = 0; i < LowerTreads.Length; i++)
            {
                PathNode step = Ensure(nodes, LowerTreads[i], root, LowerTread(i));
                if (prev != null)
                    prev.Link(step);
                prev = step;
            }
            // Top tread of CauThang1 (5.07).
            Put(stair, root, StairTop);
            if (prev != null && stair != null)
                prev.Link(stair);
            HideDeck("LedgeNear");
            HideDeck("LedgeFar");
            BuildLip(root);
            ClearNode(nodes, "Landing");
            ClearNode(nodes, "SpanFar");
            ClearNode(nodes, "NearMid");

            // B: the landing on Tang2 at the head of CauThang1. Dead end until the corridor yaw.
            Put(gapFrom, root, new Vector3(0.10f, floor2, 6.50f));
            if (stair != null && gapFrom != null)
                stair.Link(gapFrom);
            // C: inside the tầng 2 room. The board links B-C only at the corridor yaw.
            Put(gapTo, root, new Vector3(3.40f, floor2, 6.30f));
            Put(speaker, root, new Vector3(5.00f, floor2, 2.70f));
            Put(door, root, new Vector3(2.60f, floor2, 6.55f));
            Put(roomFocus, root, new Vector3(4.20f, floor2 + 1.2f, 3.80f));
            // Click proxies sit on the real furniture: TuBuffet (cabinet), Cube.048 (dining table), the fan.
            Put(cabinet, root, new Vector3(5.70f, 5.95f, 2.72f));
            Put(root.Find("Table"), root, new Vector3(3.90f, 5.65f, 3.00f));
            Put(root.Find("Shelf"), root, new Vector3(4.90f, 5.70f, 4.65f));

            // Round 2, blue: out of the room along the walkway to the foot of CauThang2 (rises -z).
            Ensure(nodes, "Hall", root, new Vector3(1.60f, floor2, 6.45f));
            for (int i = 0; i < UpperTreads.Length; i++)
                Ensure(nodes, UpperTreads[i], root, UpperTread(i));
            // The dog walks CauThang2. Its trap node is the fourth tread.
            PathNode dog = FindNode(nodes, "Dog");
            Put(dog, root, UpperTread(3));
            if (door != null && dog != null)
                door.Unlink(dog);
            PathNode safe = FindNode(nodes, "Safe");
            if (door != null && safe != null)
                door.Unlink(safe);
            Put(safe, root, UpperTread(6));

            // Tang3 (8.05). Walls at x=4.0 and x=5.5 run from z=-1.25 to -6.5, so the east strip (x 5.75..7.25)
            // and the middle corridor (x 2.75..3.75) only meet along the open north band (z -1.0..0.5).
            EnsureMark(nodes, "E", root, new Vector3(3.10f, floor3, 1.00f));
            Ensure(nodes, "EF1", root, new Vector3(3.25f, floor3, -0.25f));
            Ensure(nodes, "EF2", root, new Vector3(6.50f, floor3, -0.25f));
            Put(FindNode(nodes, "F"), root, new Vector3(6.50f, floor3, -1.60f));
            Put(FindNode(nodes, "I"), root, new Vector3(6.50f, floor3, -5.70f));
            Put(FindNode(nodes, "L"), root, new Vector3(3.00f, floor3, -4.60f));
            Ensure(nodes, "LG1", root, new Vector3(0.00f, floor3, -3.30f));
            // Middle band corner (-0.6,-2.8) to west wing corner (-1.8,-2.7): one stride over the stairwell crack.
            Ensure(nodes, "LipE", root, new Vector3(-0.45f, floor3, -2.95f));
            Ensure(nodes, "LipW", root, new Vector3(-1.95f, floor3, -2.55f));
            Put(FindNode(nodes, "G"), root, new Vector3(-4.40f, floor3, -2.50f));
            // West wing to the west strip: a 1.5 m planter wall (x -7.67..-6.87, top 9.55) seals the strip.
            // Climb the Cube.024 bench (8.96), the planter soil (9.55), then the Cube.041 wall top (9.00).
            Ensure(nodes, "WFloor", root, new Vector3(-5.35f, floor3, -2.55f));
            Ensure(nodes, "WBench", root, new Vector3(-6.10f, 9.00f, -1.95f));
            Ensure(nodes, "WWall", root, new Vector3(-7.27f, 9.59f, -1.60f));
            Ensure(nodes, "C041a", root, new Vector3(-8.95f, 9.04f, -2.00f));
            Ensure(nodes, "C041b", root, new Vector3(-8.95f, 9.04f, -3.10f));
            // Cube.041 west shelves: 8.32 (z -4.2..-3.4), 9.18 (z -6.6..-4.6). Cube.021 top 10.00.
            Ensure(nodes, "Wmid", root, new Vector3(-8.90f, 8.36f, -3.85f));
            Ensure(nodes, "Whip", root, new Vector3(-8.90f, 9.24f, -4.90f));
            Ensure(nodes, "S1", root, new Vector3(-8.90f, 9.24f, -6.20f));
            Ensure(nodes, "S4", root, ShelfS4);
            Ensure(nodes, "ShelfW", root, ShelfLip);
            Ensure(nodes, "SlopeS", root, SlopeFoot);
            Ensure(nodes, "PlaneA", root, new Vector3(-8.10f, 11.37f, -5.60f));
            Put(FindNode(nodes, "M"), root, new Vector3(-7.30f, 12.28f, -4.70f));
            Put(FindNode(nodes, "Pole"), root, PoleStand);
            Put(root.Find("PoleAnchor"), root, PoleStand + SpeakerRest);
            ClearNode(nodes, "WestA");
            ClearNode(nodes, "WestB");
            ClearNode(nodes, "Wlip");
            ClearNode(nodes, "S2");
            ClearNode(nodes, "S3");
            ClearNode(nodes, "PlaneB");
            ClearNode(nodes, "RoofA");
            ClearNode(nodes, "RoofB");
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

        static void ClearNode(Transform nodes, string name)
        {
            Transform old = nodes.Find(name);
            if (old == null)
                return;
            PathNode node = old.GetComponent<PathNode>();
            if (node != null)
            {
                var neighbors = new List<PathNode>(node.Neighbors);
                for (int i = 0; i < neighbors.Count; i++)
                    node.Unlink(neighbors[i]);
            }
            Object.Destroy(old.gameObject);
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
