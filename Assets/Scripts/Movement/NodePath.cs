using System.Collections.Generic;

namespace PerspectivePuzzle.Movement
{
    public static class NodePath
    {
        public static bool TryFind(PathNode from, PathNode to, List<PathNode> result)
        {
            result.Clear();
            if (from == null || to == null || !from.Walkable || !to.Walkable)
                return false;
            if (from == to)
            {
                result.Add(from);
                return true;
            }

            var queue = new Queue<PathNode>();
            var previous = new Dictionary<PathNode, PathNode>();
            queue.Enqueue(from);
            previous.Add(from, null);

            while (queue.Count > 0)
            {
                PathNode current = queue.Dequeue();
                IReadOnlyList<PathNode> neighbors = current.Neighbors;
                for (int i = 0; i < neighbors.Count; i++)
                {
                    PathNode next = neighbors[i];
                    if (next == null || !next.Walkable || previous.ContainsKey(next))
                        continue;

                    previous.Add(next, current);
                    if (next == to)
                    {
                        WritePath(to, previous, result);
                        return true;
                    }

                    queue.Enqueue(next);
                }
            }

            return false;
        }

        static void WritePath(PathNode to, Dictionary<PathNode, PathNode> previous, List<PathNode> result)
        {
            var stack = new Stack<PathNode>();
            PathNode cursor = to;
            while (cursor != null)
            {
                stack.Push(cursor);
                previous.TryGetValue(cursor, out cursor);
            }

            while (stack.Count > 0)
                result.Add(stack.Pop());
        }
    }
}
