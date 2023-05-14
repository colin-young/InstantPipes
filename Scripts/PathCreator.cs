using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PathCreator
{
    public float Height = 5;
    public float Radius;
    public float GridSize = 3;

    public float Chaos = 0;
    public float StraightPathPriority = 10;
    public float NearObstaclesPriority = 0;

    public bool LastPathSuccess = true;

    public List<Vector3> Points = new();

    public List<Vector3> Create(Vector3 startPosition, Vector3 startNormal, Vector3 endPosition, Vector3 endNormal)
    {

        var path = new List<Vector3>();
        var pathStart = startPosition + startNormal.normalized * Height;
        var pathEnd = endPosition + endNormal.normalized * Height;
        var baseDirection = (pathEnd - pathStart).normalized;

        Point startPoint = new(pathStart);
        Point endPoint = new(pathEnd);

        var pathPoints = FindPath(startPoint, endPoint, startNormal.normalized);

        path.Add(startPosition);
        path.Add(startPoint.Position);

        LastPathSuccess = true;

        if (pathPoints != null)
        {
            foreach (var extraPoint in pathPoints)
            {
                path.Add(extraPoint.Position);
            }
        }
        else
        {
            LastPathSuccess = false;
        }

        path.Add(endPoint.Position);
        path.Add(endPosition);

        return path;
    }

    private List<Point> FindPath(Point start, Point target, Vector3 startNormal)
    {
        var toSearch = new List<Point> { start };
        var visited = new List<Vector3>();

        int iterations = 0;
        Points = new();

        Dictionary<Vector3, Point> pointDictionary = new();

        while (toSearch.Count > 0 && iterations < 200)
        {
            iterations++;
            var current = toSearch[0];
            foreach (var t in toSearch)
                if (t.F < current.F || t.F == current.F && t.H < current.H) current = t;

            visited.Add(current.Position);
            toSearch.Remove(current);

            Points.Add(current.Position);

            if (Vector3.Distance(current.Position, target.Position) <= GridSize)
            {
                var currentPathPoint = current;
                var path = new List<Point>();
                while (currentPathPoint != start)
                {
                    if (path.Count == 0 || !AreOnSameLine(path[^1].Position, currentPathPoint.Position, currentPathPoint.Connection.Position))
                    {
                        path.Add(currentPathPoint);
                    }
                    currentPathPoint = currentPathPoint.Connection;
                }
                path.Reverse();
                return path;
            }

            var neighborPositions = current.GetNeighbors(GridSize, Radius);
            foreach (var position in neighborPositions)
            {
                if (!pointDictionary.ContainsKey(position))
                {
                    pointDictionary.Add(position, new Point(position));
                }
                current.Neighbors.Add(pointDictionary[position]);
            }

            foreach (var neighbor in current.Neighbors)
            {
                if (visited.Contains(neighbor.Position)) continue;

                var costToNeighbor = current.G + 1;

                if (current.Connection != null && (current.Connection.Position - current.Position).normalized != (current.Connection.Position - neighbor.Position).normalized)
                {
                    costToNeighbor += StraightPathPriority;
                }

                costToNeighbor += Random.Range(-Chaos, Chaos);

                costToNeighbor += neighbor.GetDistanceToNearestObstacle() / 20 * NearObstaclesPriority;

                if (!toSearch.Contains(neighbor) || costToNeighbor < neighbor.G)
                {
                    neighbor.G = costToNeighbor;
                    neighbor.Connection = current;

                    if (!toSearch.Contains(neighbor))
                    {
                        neighbor.H = neighbor.GetDistance(target);
                        toSearch.Add(neighbor);
                    }
                }
            }
        }

        Debug.Log(iterations);

        return null;
    }

    private bool AreOnSameLine(Vector3 point1, Vector3 point2, Vector3 point3)
    {
        return Vector3.Cross(point2 - point1, point3 - point1).sqrMagnitude < 0.0001f;
    }

}

public class Point
{
    public Vector3 Position;
    public List<Point> Neighbors;
    public Point Connection;

    public float G;
    public float H;
    public float F => G + H;

    public Point(Vector3 position)
    {
        Position = position;
        Neighbors = new();
    }

    public float GetDistance(Point other)
    {
        var distance = Vector3.Distance(other.Position, Position);
        return distance * 10;
    }

    private List<Vector3> _directions = new() { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };

    public float GetDistanceToNearestObstacle()
    {
        float minDistance = 200;

        foreach (var direction in _directions)
            if (Physics.Raycast(Position, direction, out RaycastHit hitPoint, 200))
                Mathf.Min(minDistance, hitPoint.distance);

        return minDistance;
    }

    public List<Vector3> GetNeighbors(float gridSize, float radius)
    {
        var radiusVector = new Vector3(radius * 2f, radius * 2, radius * 2);

        var list = new List<Vector3>();

        foreach (var direction in _directions)
            if (!Physics.CheckBox(Position + direction * gridSize, radiusVector))
                list.Add(Position + direction * gridSize);

        return list;
    }
}