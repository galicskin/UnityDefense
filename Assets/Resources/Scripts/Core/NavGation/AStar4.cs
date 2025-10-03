using System.Collections.Generic;

public struct GridPos
{
    public int x, y;
    public GridPos(int x, int y) { this.x = x; this.y = y; }
}

public static class AStar4
{
    private class Node
    {
        public int x, y;
        public int g, f;
        public Node parent;
    }

    // mapData[y,x] = 0 (막힘), 1~255 (비용)
    public static bool FindPath(
        byte[,] mapData, GridPos start, GridPos goal,
        out List<GridPos> path)
    {
        path = new List<GridPos>();
        int W = mapData.GetLength(1), H = mapData.GetLength(0);

        // 우선순위 큐 (f값 작은 순)
        var open = new SortedSet<(int f, int h, Node n)>(
            Comparer<(int f, int h, Node n)>.Create((a, b) =>
            {
                int cmp = a.f.CompareTo(b.f);
                if (cmp != 0) return cmp;
                return a.h.CompareTo(b.h);
            }));

        var startNode = new Node { x = start.x, y = start.y, g = 0, f = Heuristic(start, goal) };
        open.Add((startNode.f, 0, startNode));

        var visited = new bool[H, W];

        // 4방향
        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };

        while (open.Count > 0)
        {
            var cur = open.Min; open.Remove(cur);
            var node = cur.n;

            if (node.x == goal.x && node.y == goal.y)
            {
                // 경로 재구성
                while (node != null)
                {
                    path.Add(new GridPos(node.x, node.y));
                    node = node.parent;
                }
                path.Reverse();
                return true;
            }

            if (visited[node.y, node.x]) continue;
            visited[node.y, node.x] = true;

            for (int k = 0; k < 4; k++)
            {
                int nx = node.x + dx[k];
                int ny = node.y + dy[k];
                if ((uint)nx >= W || (uint)ny >= H) continue;

                byte cost = mapData[ny, nx];
                if (cost == 0) continue; // 막힘

                int g = node.g + cost * 10; // 이동비용(타일 가중치 반영)
                var next = new Node
                {
                    x = nx,
                    y = ny,
                    g = g,
                    f = g + Heuristic(new GridPos(nx, ny), goal),
                    parent = node
                };
                open.Add((next.f, Heuristic(new GridPos(nx, ny), goal), next));
            }
        }

        return false; // 경로 없음
    }

    private static int Heuristic(GridPos a, GridPos b)
    {
        // 맨해튼 거리 × 10
        return (System.Math.Abs(a.x - b.x) + System.Math.Abs(a.y - b.y)) * 10;
    }
}
