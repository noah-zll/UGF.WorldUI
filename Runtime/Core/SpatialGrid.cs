using System.Collections.Generic;
using UnityEngine;

namespace UGF.WorldUI
{
    /// <summary>
    /// 空间哈希网格 - 用于O(1)邻近查询
    /// </summary>
    internal class SpatialGrid
    {
        private readonly float _cellSize;
        private readonly float _invCellSize;
        private readonly Dictionary<long, List<IAggregatable>> _cells;

        public SpatialGrid(float cellSize)
        {
            _cellSize = Mathf.Max(1f, cellSize);
            _invCellSize = 1f / _cellSize;
            _cells = new Dictionary<long, List<IAggregatable>>();
        }

        /// <summary>
        /// 插入UI到网格
        /// </summary>
        public void Insert(IAggregatable ui, Vector3 position)
        {
            var key = GetCellKey(position);
            if (!_cells.TryGetValue(key, out var list))
            {
                list = new List<IAggregatable>();
                _cells[key] = list;
            }

            if (!list.Contains(ui))
            {
                list.Add(ui);
            }
        }

        /// <summary>
        /// 从网格移除UI
        /// </summary>
        public void Remove(IAggregatable ui, Vector3 position)
        {
            var key = GetCellKey(position);
            if (_cells.TryGetValue(key, out var list))
            {
                list.Remove(ui);
                if (list.Count == 0)
                {
                    _cells.Remove(key);
                }
            }
        }

        /// <summary>
        /// 查询指定位置附近的所有UI（包含当前单元格和相邻单元格）
        /// </summary>
        public void QueryNearby(Vector3 position, float radius, List<IAggregatable> results)
        {
            results.Clear();

            GetCellCoords(position, out int cx, out int cy, out int cz);
            int cellRadius = Mathf.CeilToInt(radius * _invCellSize);

            for (int x = cx - cellRadius; x <= cx + cellRadius; x++)
            {
                for (int y = cy - cellRadius; y <= cy + cellRadius; y++)
                {
                    for (int z = cz - cellRadius; z <= cz + cellRadius; z++)
                    {
                        var key = GetCellKeyFromCoords(x, y, z);
                        if (_cells.TryGetValue(key, out var cellList))
                        {
                            float radiusSqr = radius * radius;
                            foreach (var ui in cellList)
                            {
                                if ((ui.WorldPosition - position).sqrMagnitude <= radiusSqr)
                                {
                                    results.Add(ui);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 清空网格
        /// </summary>
        public void Clear()
        {
            foreach (var list in _cells.Values)
            {
                list.Clear();
            }
            _cells.Clear();
        }

        /// <summary>
        /// 获取网格中的总对象数
        /// </summary>
        public int TotalCount
        {
            get
            {
                int count = 0;
                foreach (var list in _cells.Values)
                {
                    count += list.Count;
                }
                return count;
            }
        }

        private long GetCellKey(Vector3 position)
        {
            GetCellCoords(position, out int x, out int y, out int z);
            return GetCellKeyFromCoords(x, y, z);
        }

        private long GetCellKeyFromCoords(int x, int y, int z)
        {
            // 使用位运算组合坐标到单个long
            return ((long)(uint)x << 42) | ((long)(uint)y << 21) | ((long)(uint)z);
        }

        private void GetCellCoords(Vector3 position, out int x, out int y, out int z)
        {
            x = Mathf.FloorToInt(position.x * _invCellSize);
            y = Mathf.FloorToInt(position.y * _invCellSize);
            z = Mathf.FloorToInt(position.z * _invCellSize);
        }
    }
}
