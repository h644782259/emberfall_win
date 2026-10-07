using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace Emberfall.Editor
{
    public static class TraversalValidation
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        public static IEnumerator Validate(GameSession game, Action<bool, string> check, Action<string> log)
        {
            log("TRAVERSAL — river crossing, solid cover, routes and stationary jumps");
            check(!game.InDungeon, "Traversal fixture begins in the actual wilderness");
            Vector3 bank = new Vector3(7, 0, -4.5f), landing = bank + Vector3.forward * 4.8f;
            check(!WorldTraversal.IsWalkable(new Vector3(7,0,-2)), "River water blocks ground movement");
            Vector3 stopped = WorldTraversal.Move(bank, Vector3.forward * 7);
            check(stopped.z < -3f && WorldTraversal.IsWalkable(stopped), "Swept fast ground movement stops at the river bank");
            check(WorldTraversal.HasGroundPath(new Vector3(0,0,-6), new Vector3(0,0,3)), "The authored bridge is a continuous walkable crossing");
            check(WorldTraversal.CanLeap(bank, landing), "A clear opposite-bank landing allows a leap across the river");
            check(!WorldTraversal.CanLeap(bank, new Vector3(7,0,-2)), "A leap cannot land in deep water");
            check(!WorldTraversal.CanLeap(new Vector3(5,0,-8), new Vector3(11,0,-8)), "A leap cannot pass through a solid boulder");
            check(WorldTraversal.HasLineOfSight(bank, landing), "Ranged combat can cross open water");
            check(!WorldTraversal.HasLineOfSight(new Vector3(5,0,-8), new Vector3(11,0,-8)), "Boulders block projectile lines");
            Vector3 routeStart = new Vector3(-8,0,-5), routeEnd = new Vector3(-8,0,7);
            var path = WorldTraversal.FindPath(routeStart, routeEnd);
            check(path.Count > 1, "Creatures find a non-direct route through the bridge");
            Vector3 previous = routeStart;
            bool clear = true, crossedBridge = false;
            foreach (Vector3 point in path)
            {
                bool segmentClear = WorldTraversal.HasGroundPath(previous, point);
                if (!segmentClear) log("Blocked route segment: " + previous + " -> " + point);
                clear &= segmentClear;
                crossedBridge |= Mathf.Abs(point.x) < 1.5f && Mathf.Abs(point.z + 1) < 1.2f;
                previous = point;
            }
            check(clear && crossedBridge && Vector3.Distance(previous, routeEnd) < .1f, "Every route segment stays walkable and crosses the visible bridge");
            var route = new WorldTraversal.Route();
            Vector3 follower = routeStart;
            bool stayedOnGround = true;
            for (int i = 0; i < 600 && Vector3.Distance(follower, routeEnd) > .3f; i++)
            {
                follower = WorldTraversal.Move(follower, route.Direction(follower, routeEnd) * .24f);
                stayedOnGround &= WorldTraversal.IsWalkable(follower);
            }
            check(stayedOnGround && Vector3.Distance(follower, routeEnd) <= .3f,
                "Actual cached steering plus collision reaches the opposite bank without cutting corners or stalling");
            foreach (EnemyController enemy in game.Enemies) check(WorldTraversal.IsWalkable(enemy.transform.position, enemy.IsBoss ? 1f : .45f), "Spawned enemies are placed on valid ground");
            PlayerController player = game.Player;
            Vector3 original = player.transform.position;
            Quaternion originalRotation = player.transform.rotation;
            bool enabled = player.enabled;
            player.enabled = false;
            player.Teleport(bank);
            MethodInfo jump = typeof(PlayerController).GetMethod("TryJump", Hidden);
            MethodInfo tick = typeof(PlayerController).GetMethod("AdvanceJump", Hidden);
            check(jump != null && tick != null, "Player jump integration is available");
            check((bool)jump.Invoke(player, null), "Player can jump in place beside the river");
            tick.Invoke(player, new object[] { .275f });
            check(player.transform.position.y > 1f && new Vector2(player.transform.position.x - bank.x, player.transform.position.z - bank.z).sqrMagnitude < .0001f,
                "Jump rises vertically without moving toward the river");
            check(player.JumpCooldown == 0 && !(bool)jump.Invoke(player, null), "Jump has no cooldown and cannot restart while airborne");
            tick.Invoke(player, new object[] { .3f });
            check(!player.IsJumping && Vector3.Distance(player.transform.position, bank) < .01f,
                "Jump lands at its original position");
            while (player.TraversalStartedThisFrame) yield return null;
            check((bool)jump.Invoke(player, null), "Player can jump again immediately after landing without waiting for a cooldown");
            tick.Invoke(player, new object[] { .55f });
            player.Teleport(new Vector3(5,0,-8));
            player.transform.forward = Vector3.right;
            while (player.TraversalStartedThisFrame) yield return null;
            Vector3 besideBoulder = player.transform.position;
            check((bool)jump.Invoke(player, null), "A boulder ahead does not block a stationary jump");
            tick.Invoke(player, new object[] { .55f });
            check(Vector3.Distance(player.transform.position, besideBoulder) < .01f && player.JumpCooldown == 0,
                "Facing an obstacle still lands in place without a cooldown");
            MethodInfo blink = typeof(PlayerController).GetMethod("TryBlink", Hidden);
            typeof(PlayerController).GetField("dodgeCooldown", Hidden).SetValue(player, 0f);
            typeof(PlayerController).GetField("traversalFrame", Hidden).SetValue(player, -1);
            player.Teleport(new Vector3(5,0,-8));
            Vector3 beforeBoulder = player.transform.position;
            check((bool)blink.Invoke(player, new object[] { Vector3.right }) && player.BlinkCooldown > 2 &&
                player.transform.position.x > beforeBoulder.x + .35f && player.transform.position.x < beforeBoulder.x + 4.8f &&
                WorldTraversal.CanLeap(beforeBoulder, player.transform.position),
                "A full-distance blocked blink shortens to a safe landing before the boulder and starts cooldown");
            typeof(PlayerController).GetField("dodgeCooldown", Hidden).SetValue(player, 0f);
            typeof(PlayerController).GetField("traversalFrame", Hidden).SetValue(player, -1);
            check(!(bool)blink.Invoke(player, new object[] { Vector3.right }) && player.BlinkCooldown == 0,
                "Blink flush against a boulder has no useful landing and consumes no cooldown");
            player.Teleport(bank);
            typeof(PlayerController).GetField("dodgeCooldown", Hidden).SetValue(player, 0f);
            typeof(PlayerController).GetField("traversalFrame", Hidden).SetValue(player, -1);
            check((bool)blink.Invoke(player, new object[] { Vector3.forward }) && Vector3.Distance(player.transform.position, landing) < .1f && player.BlinkCooldown > 2,
                "A valid blink crosses the river immediately and starts its cooldown");
            player.Teleport(bank);
            typeof(PlayerController).GetField("traversalFrame", Hidden).SetValue(player, -1);
            typeof(PlayerController).GetField("jumpInput", Hidden).SetValue(player, Vector3.forward);
            check((bool)jump.Invoke(player,null), "Moving jump starts from the river bank");
            typeof(PlayerController).GetField("jumpInput", Hidden).SetValue(player, Vector3.zero);
            tick.Invoke(player,new object[]{.55f});
            check(player.transform.position.z>0 && WorldTraversal.IsWalkable(player.transform.position) && WorldTraversal.CanLeap(bank,player.transform.position), "Moving jump crosses narrow river and lands on safe opposite bank");
            player.Teleport(original);
            player.transform.rotation = originalRotation;
            player.enabled = enabled;
            yield return null;
        }
    }
}
