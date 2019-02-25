#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software,
// and you are welcome to redistribute it under certain conditions; See
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using System.Collections.Generic;
using System.Linq;
using System;

namespace ProjectPorcupine.Pathfinding
{
    // This class will only work with reference types. If this should support value types it needs to use Nullable<T>
    public class CircularBuffer<T> where T : class
    {
        private T[] buffer;
        private int index;

        public CircularBuffer(int size)
        {
            buffer = new T[size];
            index = 0;
        }

        public T Enqueue(T newValue)
        {
            // Remove all references to this value and then add the value
            IEqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < buffer.Length; i++)
            {
                if (comparer.Equals(buffer[i], newValue))
                {
                    buffer[i] = default(T);
                }
            }

            index = (index + 1) % buffer.Length;
            T oldValue = buffer[index];
            buffer[index] = newValue;
            return oldValue;
        }
    }

    public class Cache
    {
        private const int MaxCacheSize = 20;
        
        private CircularBuffer<CacheKey> oldPaths;
        private Dictionary<CacheKey, List<Tile>> pathLookup;

        public Cache()
        {
            oldPaths = new CircularBuffer<CacheKey>(MaxCacheSize);
            pathLookup = new Dictionary<CacheKey, List<Tile>>(new CacheKeyEqualityComparer());
        }

        public void Insert(List<Tile> path)
        {
            CacheKey key = new CacheKey(path);

            CacheKey oldKey = oldPaths.Enqueue(key);

            if (oldKey != null)
            {
                pathLookup.Remove(oldKey);
            }
        }
        
        public bool TryGetPath(CacheKey key, out List<Tile> path)
        {
            return pathLookup.TryGetValue(key, out path);
        }

        public bool TryGetPath(Tile start, Tile end, out List<Tile> path)
        {
            return TryGetPath(new CacheKey(start, end), out path);
        }

        public List<Tile> GetPath(Tile start, Tile end)
        {
            return GetPath(new CacheKey(start, end));
        }

        public List<Tile> GetPath(CacheKey key)
        {
            return pathLookup[key];
        }
        
        public class CacheKey
        {
            private Tile start;
            private Tile end;

            public CacheKey(Tile startTile, Tile endTile)
            {
                start = startTile;
                end = endTile;
            }

            public CacheKey(List<Tile> path)
            {
                start = path.First();
                end = path.Last();
            }

            public override int GetHashCode()
            {
                return start.GetHashCode() ^ end.GetHashCode();
            }

            public bool Equals(CacheKey key)
            {
                return start.Equals(key.start) && end.Equals(key.end);
            }
        }

        public class CacheKeyEqualityComparer : IEqualityComparer<CacheKey>
        {
            public bool Equals(CacheKey b1, CacheKey b2)
            {
                if (b2 == null && b1 == null)
                {
                    return true;
                }
                else if (b1 == null | b2 == null)
                {
                    return false;
                }
                else if (b1.Equals(b2))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public int GetHashCode(CacheKey bx)
            {
                return bx.GetHashCode();
            }
        }
    }
}
