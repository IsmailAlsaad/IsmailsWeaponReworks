using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// The geometry of bouncing a moving player off something: reflecting a velocity, finding the normal of the tile the player just ran
    /// into (by probing Collision.TileCollision, the same test the game moves the player with), and a normal for an enemy. Pure
    /// functions plus one probe; nothing here changes the player.
    /// </summary>
    public static class BounceMath
    {
        // The mirror image of 'velocity' about a surface with this (unit) normal.
        public static Vector2 Reflect(Vector2 velocity, Vector2 normal)
        {
            return velocity - 2f * Vector2.Dot(velocity, normal) * normal;
        }

        // Makes sure the velocity leaves the surface with at least 'minAway' speed (a bounce must never stay glued to the wall).
        public static Vector2 PushAway(Vector2 velocity, Vector2 normal, float minAway)
        {
            float away = Vector2.Dot(velocity, normal);
            return away < minAway ? velocity + normal * (minAway - away) : velocity;
        }

        // The velocity with the part that points INTO the surface removed: what sliding along it looks like.
        public static Vector2 Slide(Vector2 velocity, Vector2 normal)
        {
            return velocity - normal * Vector2.Dot(velocity, normal);
        }

        // A normal for hitting an enemy: away from its center, blended with straight back along the dash so a hit near an edge
        // does not send the player off sideways.
        public static Vector2 NpcNormal(Vector2 playerCenter, NPC npc, Vector2 dashDirection)
        {
            Vector2 away = (playerCenter - npc.Center).SafeNormalize(-dashDirection);
            return (away - dashDirection).SafeNormalize(away);
        }

        // Is there a tile right ahead of the player in 'direction' (within a few pixels)? If so 'normal' points away from it.
        // Each axis is probed on its own (so a wall and a floor are told apart), and a corner that only the combined move hits
        // gets the opposite of the direction.
        public static bool TileNormal(Player player, Vector2 direction, out Vector2 normal)
        {
            normal = Vector2.Zero;
            float reach = 4f;
            if (Math.Abs(direction.X) > 0.12f && Blocked(player, new Vector2(Math.Sign(direction.X) * reach, 0f)))
                normal.X = -Math.Sign(direction.X);
            if (Math.Abs(direction.Y) > 0.12f && Blocked(player, new Vector2(0f, Math.Sign(direction.Y) * reach)))
                normal.Y = -Math.Sign(direction.Y);

            if (normal == Vector2.Zero)
            {
                if (!Blocked(player, direction * reach))
                    return false;
                normal = -direction;
            }
            normal = normal.SafeNormalize(-direction);
            return true;
        }

        static bool Blocked(Player player, Vector2 probe)
        {
            Vector2 allowed = Collision.TileCollision(player.position, probe, player.width, player.height, player.controlDown, player.controlDown, (int)player.gravDir);
            return Vector2.DistanceSquared(allowed, probe) > 0.01f;
        }
    }
}