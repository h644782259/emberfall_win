"""Execute current authored room builders; replace only rendering and prop instances.
The real shell, tactical walls/river, pillar registration and pocket placement run.
This is managed navigation coverage, not Unity scene/model lifecycle validation.
"""
def attach(root, fixture, math, originals, member):
    def source(name): return (root/'Assets/Scripts/World'/name).read_text()
    fixture=fixture.replace('public static class WorldBuilder {','public static partial class WorldBuilder {')
    safe_stub='bool TrySafeSpawn(Vector3 desired,float radius,float safe,out Vector3 p){p=desired;return true;}'
    if safe_stub in fixture:
        fixture=fixture.replace(safe_stub,'private float ArenaRadius=>InDungeon?18f:22f;')
        originals['RoomSpawnHost.cs']='using UnityEngine;namespace Emberfall{public sealed partial class GameSession{'+member((root/'Assets/Scripts/Core/GameSession.cs').read_text(),'private bool TrySafeSpawn(')+'}}'
    start=fixture.index('public static GameObject Build(')
    end=fixture.index('\n }',start)
    original=fixture[start:end]
    # Keep injected scene-build failure behavior; execute production room builders.
    signature=original[:original.index('{')]
    fixture=fixture[:start]+signature+'''{if(FailNextBuild){FailNextBuild=false;throw new Exception("injected world failure");}
      WorldTraversal.Reset(kind);DestructiblePropFactory.Reset();var root=new GameObject("world");var resources=new WorldResources();
      if(kind==ZoneKind.Dungeon){if(layout>=20)BuildTacticalRoom(root.transform,resources,layout);else if(layout>=10)BuildLinkedRoom(root.transform,resources,layout-10);else throw new Exception("unexpected arena layout");BuildBreakablePockets(root.transform,layout);}return root;}'''+fixture[end:]
    fixture=fixture.replace('public bool PracticeActive=>false;', 'public static GameSession Instance=>null;public bool PracticeActive=>false;')
    math=math.replace('public static Vector3 zero=>', 'public static Vector3 up=>new Vector3(0,1,0);public static Vector3 zero=>')
    originals['RoomWorldBuilders.cs']='using UnityEngine;namespace Emberfall{public static partial class WorldBuilder{'+ '\n'.join([
        member(source('WorldBuilder.LinkedRooms.cs'),'private static void BuildLinkedRoom('),
        member(source('WorldBuilder.TacticalRooms.cs'),'private static void BuildTacticalRoom('),
        member(source('WorldBuilder.ChallengeArenas.cs'),'private static void BuildBreakablePockets('),
        member(source('WorldBuilder.cs'),'private static void Pillar(')])+'}}'
    originals['RoomWorldBoundary.cs']=(root/'Tests/RoomWorldBoundary.cs').read_text()
    return fixture,math
