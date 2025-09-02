using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// 资源层
public class ResourceTilemapService : MonoBehaviour
{
    public Tilemap resourceTilemap;

    private readonly Dictionary<Vector2Int, int> _hp = new();

    public IResourceSource GetSourceAt(Vector2Int cell)
    {
        var tile = resourceTilemap ? resourceTilemap.GetTile((Vector3Int)cell) as ResourceTile : null;
        if (tile == null) return null;
        return new TileSource(this, cell, tile);
    }

    private class TileSource : IResourceSource
    {
        private readonly ResourceTilemapService _svc;
        private readonly Vector2Int _cell;
        private readonly ResourceTile _tile;

        public TileSource(ResourceTilemapService svc, Vector2Int cell, ResourceTile tile)
        {
            _svc = svc;
            _cell = cell;
            _tile = tile;
        }

        public ItemDef YieldItem => _tile.yieldItem;

        public bool TryConsumeOnce()
        {
            if (_tile.infinite) return true;
            int left = _svc.GetHp(_cell);
            if (left <= 0) return false;
            left -= 1;
            _svc.SetHp(_cell, left);
            if (left <= 0)
            {
                _svc.resourceTilemap.SetTile((Vector3Int)_cell, null);
                _svc.ClearHp(_cell);
            }

            return true;
        }
    }

    private int GetHp(Vector2Int cell)
    {
        if (_hp.TryGetValue(cell, out var v)) return v;
        var tile = resourceTilemap.GetTile((Vector3Int)cell) as ResourceTile;
        if (tile == null) return 0;
        int init = tile.infinite ? int.MaxValue : Mathf.Max(0, tile.baseRichness);
        _hp[cell] = init;
        return init;
    }

    private void SetHp(Vector2Int cell, int v) => _hp[cell] = v;
    private void ClearHp(Vector2Int cell) => _hp.Remove(cell);
}