using System;
using System.Collections.Generic;
using System.Linq;
using csDelaunay;
using UnityEngine;
using Random = UnityEngine.Random;
using Vector2 = System.Numerics.Vector2;
using Vector2Unity = UnityEngine.Vector2;

public class VoronoiGenerator : MonoBehaviour
{
    public enum PaintMode
    {
        Distance,
        CellID,
        EdgeDistance
    }
    
    [SerializeField] private int _seed = 0;
    [SerializeField] private bool _randomSeed;
    [SerializeField] private uint _pointsAmount = 5;
    [SerializeField] private int _relaxationSteps = 0;
    [SerializeField] private Vector2Unity _size = new Vector2Unity(100, 100);
    [SerializeField] private Renderer _renderer;
    [SerializeField] private Vector2Int _textureQuality = Vector2Int.one * 256;
    [SerializeField] private FilterMode _textureFilterMode;
    [SerializeField] private TextureWrapMode _textureWrapMode;
    [SerializeField] private PaintMode _paintMode;

    private Voronoi _voronoi;
    private Texture2D _texture;
    
    private void Start()
    {
        Generate();
        GenerateTexture();
    }

    private void Generate()
    {
        if(!_randomSeed)
            Random.InitState(_seed);
        
        var points = new List<Vector2>();
        var boundary = new Rectf(0, 0, _size.x, _size.y);

        for (int i = 0; i < _pointsAmount; i++)
        {
            points.Add(new (Random.Range(0, _size.x), Random.Range(0, _size.y)));
        }

        _voronoi = new Voronoi(points, boundary, _relaxationSteps);
        Debug.Log($"Voronoi generated: {_voronoi.Edges.Count};");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Generate"))
        {
            Generate();
            GenerateTexture();
        }
        
        if (GUILayout.Button("Relax"))
        {
            _voronoi.LloydRelaxation(1);
            GenerateTexture();
        }
    }

    private void OnDrawGizmos()
    {
        if(_voronoi == null)
            return;

        Gizmos.color = Color.red;
        
        foreach (var siteCoord in _voronoi.SiteCoords())
        {
            Gizmos.DrawWireSphere(siteCoord.ToUnityVector(), .1f);
        }
        
        Gizmos.color = Color.green;
        
        foreach (var edge in _voronoi.Edges)
        {
            if(edge.ClippedEnds == null)
                continue;
            
            Vector2Unity leftVertex = edge.ClippedEnds[LR.LEFT].ToUnityVector();
            Gizmos.DrawWireSphere(leftVertex, .1f);
            
            Vector2Unity rightVertex = edge.ClippedEnds[LR.RIGHT].ToUnityVector();
            Gizmos.DrawWireSphere(rightVertex, .1f);
            
            Gizmos.DrawLine(leftVertex, rightVertex);
        }

        Gizmos.color = Color.blue;

        var firstSite = _voronoi.SitesIndexedByLocation[_voronoi.SiteCoords()[0]];

        foreach (var r in firstSite.Region(_voronoi.PlotBounds))
        {
            Gizmos.DrawWireSphere(r.ToUnityVector(), .1f);
        }
        
        Gizmos.color = Color.magenta;
        
        Gizmos.DrawLine(new Vector2Unity(_voronoi.PlotBounds.left, _voronoi.PlotBounds.bottom), new Vector2Unity(_voronoi.PlotBounds.left, _voronoi.PlotBounds.top));
        Gizmos.DrawLine(new Vector2Unity(_voronoi.PlotBounds.right, _voronoi.PlotBounds.bottom), new Vector2Unity(_voronoi.PlotBounds.right, _voronoi.PlotBounds.top));
        
        Gizmos.DrawLine(new Vector2Unity(_voronoi.PlotBounds.left, _voronoi.PlotBounds.bottom), new Vector2Unity(_voronoi.PlotBounds.right, _voronoi.PlotBounds.bottom));
        Gizmos.DrawLine(new Vector2Unity(_voronoi.PlotBounds.left, _voronoi.PlotBounds.top), new Vector2Unity(_voronoi.PlotBounds.right, _voronoi.PlotBounds.top));
    }

    private void GenerateTexture()
    {
        _texture = new Texture2D(_textureQuality.x, _textureQuality.y);
        _texture.filterMode = _textureFilterMode;
        _texture.wrapMode = _textureWrapMode;

        for (int x = 0; x < _texture.width; x++)
        {
            for (int y = 0; y < _texture.height; y++)
            {
                var siteID = GetClosestSiteId(x, y, _texture.width, _texture.height, out var distance);
                var color = Color.magenta;

                switch (_paintMode)
                {
                    case PaintMode.Distance:
                        distance = 1 - distance;
                        color = new Color(distance, distance, distance);
                        break;
                    case PaintMode.CellID:
                        color = GetColorFromID(siteID);
                        break;
                    case PaintMode.EdgeDistance:
                        var p = new Vector2(x, y);
                        var site = GetSiteForPoint(p);

                        if (site == null)
                        {
                            color = Color.magenta;
                            break;
                        }
                        
                        float minEdgeDistance = GetMinimumDistanceDistanceFromPixelToSiteEdge(p, _texture.width, _texture.height, site);
                        color = new Color(minEdgeDistance, minEdgeDistance, minEdgeDistance);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
                _texture.SetPixel(x, y, color);
            }
        }
        
        _texture.Apply();
        _renderer.material.mainTexture = _texture;
    }

    private int GetClosestSiteId(int x, int y, int width, int height, out float distanceNormalized)
    {
        int closestSiteIndex = -1;
        float minDistance = float.MaxValue;
        Vector2Unity pixelPosNormalized = new Vector2Unity(x / (float)width, y / (float)height);
        
        foreach (var siteCoord in _voronoi.SiteCoords())
        {
            Vector2Unity sitePosNormalized = new Vector2Unity(siteCoord.X / (float) _voronoi.PlotBounds.width, siteCoord.Y / (float) _voronoi.PlotBounds.height);
            float distance = Vector2Unity.Distance(sitePosNormalized, pixelPosNormalized);
            
            if(distance >= minDistance)
                continue;

            minDistance = distance;
            closestSiteIndex = _voronoi.SitesIndexedByLocation[siteCoord].SiteIndex;
        }

        if (closestSiteIndex < 0)
            throw new IndexOutOfRangeException("Could not find closes site index");

        distanceNormalized = minDistance;
        return closestSiteIndex;
    }

    private Color GetColorFromID(int id)
    {
        float hue = id / (float)_voronoi.SitesIndexedByLocation.Count;
        
        return Color.HSVToRGB(hue, 1, 1);
    }

    private Site GetSiteByIndex(int index)
    {
        var siteCoord = _voronoi.SiteCoords()[index];
        return _voronoi.SitesIndexedByLocation[siteCoord];
    }

    private float GetMinimumDistanceDistanceFromPixelToSiteEdge(Vector2 p, int width, int height, Site site)
    {
        var textureSize = new Vector2(width, height);
        p /= textureSize;

        float minEdgeDistance = float.MaxValue;

        foreach (var siteEdge in site.Edges)
        {
            if(siteEdge.ClippedEnds == null)
                continue;
                            
            var left = new Vector2(siteEdge.ClippedEnds[LR.LEFT].X, siteEdge.ClippedEnds[LR.LEFT].Y);
            left /= textureSize;
            var right = new Vector2(siteEdge.ClippedEnds[LR.RIGHT].X, siteEdge.ClippedEnds[LR.RIGHT].Y);
            right /= textureSize;
            
            float edgeDistance = GetDistanceFromPointToEdge(p.X, p.Y, left.X, left.Y, right.X, right.Y);

            if (edgeDistance < minEdgeDistance)
                minEdgeDistance = edgeDistance;
        }

        return minEdgeDistance;
    }
    
    private float GetDistanceFromPointToEdge(float x, float y, float x1, float y1, float x2, float y2) {

        float A = x - x1; // position of point rel one end of line
        float B = y - y1;
        float C = x2 - x1; // vector along line
        float D = y2 - y1;
        float E = -D; // orthogonal vector
        float F = C;

        float dot = A * E + B * F;
        float len_sq = E * E + F * F;

        return (float) Mathf.Abs(dot) / Mathf.Sqrt(len_sq);
    }

    private Site GetSiteForPoint(Vector2 p)
    {
        foreach (var site in _voronoi.SitesIndexedByLocation.Values)
        {
            if (IsPointInPolygon(site.Region(_voronoi.PlotBounds), p))
                return site;
        }

        return null;
    }
    
    /// <summary>
    /// Determines if the given point is inside the polygon
    /// </summary>
    /// <param name="polygon">the vertices of polygon</param>
    /// <param name="point">the given point</param>
    /// <returns>true if the point is inside the polygon; otherwise, false</returns>
    private bool IsPointInPolygon(List<Vector2> polygon, Vector2 point)
    {
        bool result = false;
        var a = polygon.Last();
        foreach (var b in polygon)
        {
            if ((b.X == point.X) && (b.Y == point.Y))
                return true;

            if ((b.Y == a.Y) && (point.Y == a.Y))
            {
                if ((a.X <= point.X) && (point.X <= b.X))
                    return true;

                if ((b.X <= point.X) && (point.X <= a.X))
                    return true;
            }

            if ((b.Y < point.Y) && (a.Y >= point.Y) || (a.Y < point.Y) && (b.Y >= point.Y))
            {
                if (b.X + (point.Y - b.Y) / (a.Y - b.Y) * (a.X - b.X) <= point.X)
                    result = !result;
            }
            a = b;
        }
        return result;
    }
}

public static class NumericsToUnityExtension
{
    public static Vector2Unity ToUnityVector(this Vector2 vector)
    {
        return new Vector2Unity(vector.X, vector.Y);
    }
    
    public static Rect ToUnityRect(this Rectf rect)
    {
        return new Rect(rect.x, rect.y, rect.width, rect.height);
    }
}
