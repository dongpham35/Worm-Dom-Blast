using System.Collections;
using System.Collections.Generic;using System.Resources;
using UnityEngine;

public static class ShortestPathFinder
{
    private static readonly Vector2Int[] directions =
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1)
    };

    public static PathResult FindShortestPath(int[,] grid, Vector2Int start, Vector2Int end)
    {
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);

        bool[,] visited = new bool[rows, cols];
        Dictionary<Vector2Int, Vector2Int> parent = new Dictionary<Vector2Int, Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        queue.Enqueue(start);
        visited[start.x, start.y] = true;

        Vector2Int lastReachable = start; // lưu điểm xa nhất có thể đi được

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            // cập nhật điểm xa nhất (để nếu fail vẫn có path đến đây)
            lastReachable = current;

            if (current == end)
            {
                var path = ReconstructPath(parent, start, end);
                return new PathResult(path, PathState.PathSuccess);
            }

            foreach (var dir in directions)
            {
                Vector2Int next = current + dir;

                if (next.x >= 0 && next.x < rows &&
                    next.y >= 0 && next.y < cols &&
                    !visited[next.x, next.y] && grid[next.x, next.y] == 0)
                {
                    queue.Enqueue(next);
                    visited[next.x, next.y] = true;
                    parent[next] = current;
                }
            }
        }

        // nếu không đến được end -> trả về path tới điểm xa nhất
        var partialPath = ReconstructPath(parent, start, lastReachable);
        return new PathResult(partialPath, PathState.PathFailed);
    }

    private static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> parent, Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> path    = new List<Vector2Int>();
        Vector2Int       current = end;

        while (current != start)
        {
            path.Add(current);
            if (!parent.ContainsKey(current))
                break; // nếu không có parent -> dừng (trường hợp path không đầy đủ)
            current = parent[current];
        }

        // path.Add(start);  ❌ bỏ dòng này, không cần thêm start
        path.Reverse();
        return path;
    }
}


public enum PathState
{
    PathSuccess,
    PathFailed
}

public struct PathResult
{
    public List<Vector2Int> Path;
    public PathState        State;

    public PathResult(List<Vector2Int> path, PathState state)
    {
        Path  = path;
        State = state;
    }
}