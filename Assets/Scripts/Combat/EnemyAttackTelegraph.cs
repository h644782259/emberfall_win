using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    /// <summary>Controller-owned warnings: fixed footprints live for the entire windup, including stun.</summary>
    internal sealed class EnemyAttackTelegraph : MonoBehaviour
    {
        private readonly List<LineRenderer> lines = new List<LineRenderer>();
        private Material material;
        private float progress;
        private bool interruptible;
        private LineRenderer clock,interruptMark;
        private Vector3 timingCenter;
        private Transform attacker;private Vector3 impactCenter,lastAttacker;private float impactRadius;private int lastRevision=-1;
        private Mesh impactMesh;private MeshFilter impactFilter;
        private readonly Vector3[] timingPoints=new Vector3[33];
        private static readonly Color Danger=ThreatVisualStyle.Danger;

        public static EnemyAttackTelegraph Circle(Vector3 center,float radius,Transform attacker)
        {
            var warning=Create("Enemy Warning - Reachable Impact Circle");warning.attacker=attacker;warning.impactCenter=center;warning.impactRadius=radius;
            var obj=new GameObject("Actual reachable impact contour");obj.transform.SetParent(warning.transform,false);
            warning.impactFilter=obj.AddComponent<MeshFilter>();var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=warning.material;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.sortingOrder=120;
            warning.RefreshCircle();warning.Timing(center);return warning;
        }
        private void RefreshCircle()
        {
            if(attacker==null||impactFilter==null)return;
            Vector3 from=attacker.position;if(lastRevision==WorldTraversal.Revision&&(from-lastAttacker).sqrMagnitude<.000001f)return;
            lastRevision=WorldTraversal.Revision;lastAttacker=from;
            var segments=EnemyImpactRegion.Outline(from,impactCenter,impactRadius);
            var vertices=new Vector3[segments.Count*2];var colors=new Color[vertices.Length];var triangles=new int[segments.Count*3];
            for(int i=0;i<segments.Count;i+=2)
            {
                Vector3 a=WorldTerrain.Ground(segments[i],.16f),b=WorldTerrain.Ground(segments[i+1],.16f);
                Vector3 side=Vector3.Cross(Vector3.up,(b-a).normalized)*.055f;int v=i*2,t=i*3;
                vertices[v]=a-side;vertices[v+1]=a+side;vertices[v+2]=b-side;vertices[v+3]=b+side;
                for(int n=0;n<4;n++)colors[v+n]=Danger;
                triangles[t]=v;triangles[t+1]=v+1;triangles[t+2]=v+2;triangles[t+3]=v+2;triangles[t+4]=v+1;triangles[t+5]=v+3;
            }
            if(impactMesh!=null)Destroy(impactMesh);
            impactMesh=new Mesh{name="Clipped actual enemy damage boundary",vertices=vertices,colors=colors,triangles=triangles};impactMesh.RecalculateBounds();impactFilter.sharedMesh=impactMesh;
        }
        private void LateUpdate(){RefreshCircle();}

        public static EnemyAttackTelegraph Charge(Vector3 start, Vector3 end, float halfWidth)
        {
            EnemyAttackTelegraph warning = Create("Enemy Warning - Charge Corridor");
            warning.Lane(start, end, halfWidth, .1f);
            warning.Timing(start);
            return warning;
        }

        public static EnemyAttackTelegraph Fan(Vector3 muzzle, Vector3[] directions, float[] lengths, float halfWidth)
        {
            EnemyAttackTelegraph warning = Create("Enemy Warning - Projectile Lanes");
            for (int i = 0; i < directions.Length; i++)
                if (lengths[i] > .05f) warning.Lane(muzzle, muzzle + directions[i] * lengths[i], halfWidth, .055f);
            warning.Timing(muzzle);
            return warning;
        }

        private static EnemyAttackTelegraph Create(string title)
        {
            var root = new GameObject(title);
            var warning = root.AddComponent<EnemyAttackTelegraph>();
            warning.material = ThreatVisualStyle.Material();
            return warning;
        }

        private void Lane(Vector3 start, Vector3 end, float halfWidth, float width)
        {
            Vector3 forward = CombatFx.Flat(end - start).normalized;
            if (forward.sqrMagnitude < .01f) return;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            // Rounded ends match the swept-circle collision used by charge and projectiles.
            var outline = new Vector3[34];
            for (int i = 0; i <= 16; i++)
            {
                float angle = i * Mathf.PI / 16f;
                outline[i] = end + (right * Mathf.Cos(angle) + forward * Mathf.Sin(angle)) * halfWidth;
                outline[i + 17] = start + (-right * Mathf.Cos(angle) - forward * Mathf.Sin(angle)) * halfWidth;
            }
            Line(outline, true, width);
            Line(new[] { start, end }, false, width * .7f);
            float arrow = Mathf.Min(.65f, Vector3.Distance(start, end) * .25f);
            Line(new[] { end - forward * arrow + right * arrow * .6f, end, end - forward * arrow - right * arrow * .6f }, false, width * 1.4f);
        }

        private LineRenderer Line(Vector3[] points, bool loop, float width)
        {
            var obj = new GameObject("Warning Geometry");
            obj.transform.SetParent(transform, false);
            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = loop;
            line.positionCount = points.Length;
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 point = CombatFx.Flat(points[i]);
                point.y = WorldTraversal.SurfaceHeight(point,.04f)+.16f;
                line.SetPosition(i, point);
            }
            line.widthMultiplier = Mathf.Max(.07f, width);
            line.sortingOrder = 120;
            line.sharedMaterial = material;
            line.startColor = line.endColor = new Color(1f, .32f, .12f, .85f);
            lines.Add(line);
            return line;
        }

        private void Timing(Vector3 center)
        {
            timingCenter=CombatFx.Flat(center)+Vector3.up*.2f;
            clock=Line(new[]{timingCenter,timingCenter},false,.10f);
            interruptMark=Line(new[]{timingCenter+new Vector3(-.2f,0,.19f),timingCenter+new Vector3(.03f,0,-.19f),
                timingCenter+new Vector3(.2f,0,.19f)},false,.09f);
            interruptMark.startColor=interruptMark.endColor=new Color(.2f,1f,.9f,1f);
            SetProgress(0);
        }
        public void SetInterruptible(bool value)
        {interruptible=value;if(interruptMark!=null)interruptMark.enabled=value;}
        public void SetProgress(float value)
        {
            progress=Mathf.Clamp01(value);
            foreach(var line in lines)if(line!=null&&line!=interruptMark)line.startColor=line.endColor=Danger;
            if(clock!=null)
            {
                int count=Mathf.Max(2,Mathf.CeilToInt(progress*32)+1);clock.positionCount=count;
                for(int i=0;i<count;i++)
                {float a=2*Mathf.PI*progress*i/(count-1);timingPoints[i]=timingCenter+new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*.52f;clock.SetPosition(i,WorldTerrain.Ground(timingPoints[i],.16f));}
            }
            if(interruptMark!=null)interruptMark.enabled=interruptible;
        }

        private void OnDestroy() { if (material != null) Destroy(material);if(impactMesh!=null)Destroy(impactMesh); }
    }
}
