using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos
{
    /// <summary>
    /// Where the yoyo's string bends around blocks (patch 8). A list of corner points ("pivots") between the hand and the yoyo, fixed to the
    /// tiles: when the straight string between two neighbors would pass through a solid block, the corner of that block with the shortest
    /// detour is added (a little outside it); a corner goes away again as soon as its two neighbors can see each other. The string is then
    /// laid along hand, corners, yoyo (YoyoRope.TightenAlong). Line of sight is sampled every few pixels against full solid blocks and half
    /// blocks; sloped tiles and platforms do not block it. When the hand or the yoyo is inside a block there is no wrap (the string is simply
    /// straight). Visual only: every machine runs its own, nothing is synced. Client only.
    /// </summary>
    public class YoyoRopeWrap
    {
        readonly List<Vector2> pivots = new List<Vector2>();
        readonly float sampleStepPx = 2f;

        public IReadOnlyList<Vector2> Pivots => pivots;

        public bool Active => pivots.Count > 0;

        public void Clear()
        {
            pivots.Clear();
        }

        // One tick: from the hand (anchor) to the yoyo (end). maxPivots caps the corners, clearance is how far outside a corner the string goes.
        public void Update(Vector2 anchor, Vector2 end, int maxPivots, float clearance)
        {
            if (Main.dedServ || maxPivots <= 0 || Vector2.DistanceSquared(anchor, end) < 4f || Vector2.DistanceSquared(anchor, end) > 1500f * 1500f)
            {
                pivots.Clear();
                return;
            }
            if (SolidAt(anchor) || SolidAt(end))
            {
                pivots.Clear();
                return;
            }

            // Corners that are not needed any more: the neighbors on both sides see each other (or the corner ended up inside a block).
            bool changed = true;
            int guard = 0;
            while (changed && pivots.Count > 0 && guard++ < 16)
            {
                changed = false;
                for (int i = pivots.Count - 1; i >= 0; i--)
                {
                    Vector2 before = i == 0 ? anchor : pivots[i - 1];
                    Vector2 after = i == pivots.Count - 1 ? end : pivots[i + 1];
                    if (SolidAt(pivots[i]) || !Blocked(before, after, out _))
                    {
                        pivots.RemoveAt(i);
                        changed = true;
                    }
                }
            }

            // New corners where a piece of the string is blocked.
            for (int attempt = 0; attempt < maxPivots + 2; attempt++)
            {
                int blockedAt = -1;
                Vector2 segFrom = default;
                Vector2 segTo = default;
                Point tile = default;
                for (int k = 0; k <= pivots.Count; k++)
                {
                    Vector2 a = k == 0 ? anchor : pivots[k - 1];
                    Vector2 b = k == pivots.Count ? end : pivots[k];
                    if (Blocked(a, b, out tile))
                    {
                        blockedAt = k;
                        segFrom = a;
                        segTo = b;
                        break;
                    }
                }
                if (blockedAt < 0 || pivots.Count >= maxPivots)
                    break;
                if (!FindPivot(segFrom, segTo, tile, clearance, out Vector2 pivot))
                    break;
                pivots.Insert(blockedAt, pivot);
            }
        }

        // The corner of 'tile' the string from a to b goes around: the free corner (a little outside it) with the shortest path a, corner, b;
        // a corner from which both pieces are clear wins over any other.
        bool FindPivot(Vector2 a, Vector2 b, Point tile, float clearance, out Vector2 pivot)
        {
            pivot = default;
            float left = tile.X * 16f;
            float top = tile.Y * 16f;
            float right = left + 16f;
            float bottom = top + 16f;
            if (Main.tile[tile.X, tile.Y].IsHalfBlock)
                top += 8f;
            float best = float.MaxValue;
            bool found = false;
            for (int corner = 0; corner < 4; corner++)
            {
                float sx = (corner & 1) == 0 ? -1f : 1f;
                float sy = (corner & 2) == 0 ? -1f : 1f;
                Vector2 candidate = new Vector2(sx < 0f ? left : right, sy < 0f ? top : bottom) + new Vector2(sx, sy) * clearance;
                if (SolidAt(candidate) || Vector2.DistanceSquared(candidate, a) < 1f || Vector2.DistanceSquared(candidate, b) < 1f)
                    continue;
                float cost = Vector2.Distance(a, candidate) + Vector2.Distance(candidate, b);
                if (Blocked(a, candidate, out _))
                    cost += 500f;
                if (Blocked(candidate, b, out _))
                    cost += 500f;
                if (cost < best)
                {
                    best = cost;
                    pivot = candidate;
                    found = true;
                }
            }
            return found;
        }

        // Is the straight piece from a to b inside a block anywhere? (a itself is not looked at.) 'tile' is the first block it runs into.
        bool Blocked(Vector2 a, Vector2 b, out Point tile)
        {
            tile = default;
            Vector2 d = b - a;
            float length = d.Length();
            if (length < 1f)
                return false;
            int steps = (int)Math.Ceiling(length / Math.Max(0.5f, sampleStepPx));
            for (int s = 1; s <= steps; s++)
            {
                if (SolidAt(a + d * (s / (float)steps), out tile))
                    return true;
            }
            return false;
        }

        static bool SolidAt(Vector2 point)
        {
            return SolidAt(point, out _);
        }

        // A full solid block (or the lower half of a half block). Sloped tiles, platforms and everything not solid do not count.
        static bool SolidAt(Vector2 point, out Point tile)
        {
            int x = (int)Math.Floor(point.X / 16f);
            int y = (int)Math.Floor(point.Y / 16f);
            tile = new Point(x, y);
            if (!WorldGen.InWorld(x, y, 1))
                return false;
            Tile t = Main.tile[x, y];
            if (!t.HasUnactuatedTile || !Main.tileSolid[t.TileType] || Main.tileSolidTop[t.TileType] || t.Slope != SlopeType.Solid)
                return false;
            if (t.IsHalfBlock && point.Y - y * 16f < 8f)
                return false;
            return true;
        }
    }
}