using UnityEngine;

namespace Emberfall
{
    /// <summary>Dispatches immediate skills and previews uncommitted ground-target casts.</summary>
    public sealed class SkillTargetingController : MonoBehaviour
    {
        public enum Shape { Self, Ground, Cone, Lane, Retreat, Leap }
        public struct Preview
        {
            public Shape shape;
            public float radius, distance, angle;
            public float startRadius, endRadius, startDistance, impactSpacing, arenaMargin;
            public int impactCount;
            public Preview(Shape form, float size, float length = 0, float arc = 0)
            {
                shape = form; radius = size; distance = length; angle = arc;
                startRadius = endRadius = startDistance = impactSpacing = 0;
                arenaMargin = .65f;
                impactCount = 0;
            }
        }

        internal bool ConfirmingContract { get; private set; }
        public int TargetedSkillIndex {get{return skill;}}
        public bool IsTargeting { get { return skill >= 0; } }
        public bool CancelledThisFrame { get { return cancelledFrame == Time.frameCount; } }
        public bool ConsumedThisFrame { get { return CancelledThisFrame || castFrame == Time.frameCount; } }
        public string SkillName { get { return IsTargeting ? GameBalance.SkillName(owner.HeroClass, skill) : ""; } }
        public Vector3 TargetPoint { get; private set; }
        public Preview CurrentPreview { get; private set; }
        public string Hint { get {
            string mode = CurrentPreview.shape == Shape.Self ? "以自身为中心" :
                CurrentPreview.shape == Shape.Ground ? "鼠标选点 · 最远 " + CurrentPreview.distance.ToString("0.0") + " 米" :
                CurrentPreview.shape == Shape.Leap ? "鼠标调整朝向 · 向前跃击，落地造成范围伤害" : CurrentPreview.shape == Shape.Retreat ? "鼠标调整朝向 · 向后撤步" : "鼠标调整攻击方向";
            return mode + " · 实体遮挡会截断范围   /   左键确认 · 右键单击或 Esc 取消 · 右键拖动镜头";
        } }

        private PlayerController owner;
        private GameSession session;
        private int skill = -1, epoch, cancelledFrame = -1, castFrame = -1;
        private GameObject visuals;
        private Material material;
        private LineRenderer area, range, marker, startArea, endArea;
        private readonly LineRenderer[] impacts = new LineRenderer[8];
        private readonly Vector3[] circle = new Vector3[64];
        private readonly Vector3[] fan = new Vector3[35];
        private readonly Vector3[] lane = new Vector3[5];
        private Vector3 renderedOrigin,renderedTarget,renderedForward;
        private int renderedRevision=-1,renderedSkill=-1;
        private bool previewDirty=true;

        public void Initialize(PlayerController hero, GameSession game) { owner = hero; session = game; }

        public static bool RequiresConfirmation(HeroClass hero, int index)
        {
            if (hero == HeroClass.Vanguard) return index == 9;
            if (hero == HeroClass.Arcanist) return index == 1 || index == 2 || index == 6 || index == 7 || index == 9;
            if (hero == HeroClass.Ranger) return index == 1 || index == 2 || index == 5 || index == 9;
            if (hero == HeroClass.Summoner) return index == 1 || index == 2 || index == 4 || index == 6 || index == 7 || index == 9;
            return false;
        }

        // Shared entry point for keyboard and UI. Successful immediate casts return
        // true without ever entering placement mode; rejected requests keep a preview.
        public bool CanBeginThisFrame { get { return castFrame != Time.frameCount; } }
        public bool Begin(int index)
        {
            if(castFrame==Time.frameCount||owner==null||index<0||index>=GameBalance.SkillCount||GameBalance.IsPassive(index))return false;
            if(MobileControls.Active){if(!owner.MobilePinnedActionAllowed(index,true))return false;owner.PrepareMobileSkillAim(index);}
            if(!owner.CanBeginSkillTargeting(index))return false;
            if(MobileControls.Active&&RequiresConfirmation(owner.HeroClass,index))
            {
                if(!owner.ConfirmTargetedSkill(index,owner.AimPoint))return false;
                Cancel();castFrame=Time.frameCount;return true;
            }
            if (!RequiresConfirmation(owner.HeroClass, index))
            {
                if (!owner.CastImmediateSkill(index)) return false;
                Cancel();
                castFrame = Time.frameCount;
                return true;
            }
            skill = index;previewDirty=true;
            epoch = owner.CombatEpoch;
            CurrentPreview = Describe(owner.HeroClass, index, session.Progression.Profile.skillRanks[index]);
            EnsureVisuals();
            visuals.SetActive(true);
            SetTarget(owner.AimPoint);
            return true;
        }

        public void Cancel()
        {
            if (IsTargeting) cancelledFrame = Time.frameCount;
            skill = -1;
            if (visuals != null) visuals.SetActive(false);
        }

        public void SetTarget(Vector3 desired)
        {
            if (!IsTargeting) return;
            Vector3 origin = owner.transform.position;
            desired.y = origin.y = 0;
            if (CurrentPreview.shape == Shape.Self) TargetPoint = origin;
            else
            {
                float maximum = CurrentPreview.shape == Shape.Ground ? CurrentPreview.distance : 30f;
                TargetPoint = origin + Vector3.ClampMagnitude(desired - origin, maximum);
                TargetPoint = Vector3.ClampMagnitude(TargetPoint, session.ArenaRadius);
                if(CurrentPreview.shape==Shape.Ground)TargetPoint=CombatSight.GroundPoint(origin,TargetPoint);
            }
            DrawPreview();
        }

        public bool Confirm()
        {
            if (castFrame == Time.frameCount) return false;
            if (!IsTargeting || Invalid()) { Cancel(); return false; }
            int confirmed = skill;
            Vector3 point = TargetPoint;
            bool ready = owner.CanBeginSkillTargeting(confirmed);
            Cancel();
            if (!ready) return false;
            // Only the deliberate desktop confirmation owns an override. Mobile
            // auto-confirmation and ordinary contracts inherit the team order.
            ConfirmingContract = owner.HeroClass == HeroClass.Summoner && (confirmed == 2 || confirmed == 4 || confirmed == 9);
            bool committed;
            try { committed = owner.ConfirmTargetedSkill(confirmed, point); }
            finally { ConfirmingContract = false; }
            if (!committed) return false;
            castFrame = Time.frameCount;
            return true;
        }

        // Called after movement and skill requests, before ordinary attacks. Both
        // immediate casts and confirmation clicks consume the entire current frame.
        public bool TickInput()
        {
            if (!IsTargeting) return ConsumedThisFrame;
            if (Invalid()) { Cancel(); return true; }
            // Mobile skills auto-confirm before reaching this state;
            // a joystick finger must never act as a simulated mouse confirmation.
            if (MobileControls.Active) return true;
            if (owner.GameplayCancelAllowed && (AdventureCamera.CancelSkillRequested || Input.GetKeyDown(KeyCode.Escape))) { Cancel(); return true; }
            if (!session.PointerOverUI)
            {
                Camera camera = Camera.main;
                float enter;
                if (owner.AimTarget != null) SetTarget(owner.AimTarget.transform.position);
                else if (camera != null)
                {
                    Ray ray = camera.ScreenPointToRay(Input.mousePosition);
                    if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out enter)) SetTarget(ray.GetPoint(enter));
                }
                if (Input.GetMouseButtonDown(0)) Confirm();
            }
            return true;
        }

        private bool Invalid()
        {
            return owner == null || session == null || session.Player != owner || !session.HasStarted ||
                owner.IsDead || session.InputBlocked || owner.CombatEpoch != epoch;
        }

        private void LateUpdate()
        {
            if (!IsTargeting) return;
            if (Invalid()) { Cancel(); return; }
            SetTarget(TargetPoint);
        }

        public static Preview Describe(HeroClass hero, int skill, int rank)
        {
            float r = GameBalance.SkillRangeMultiplier(rank);
            if (skill == 6) return hero==HeroClass.Vanguard?new Preview(Shape.Self,3.2f*r):hero==HeroClass.Ranger?new Preview(Shape.Lane,1.5f*r,12f*r):new Preview(Shape.Ground,(hero==HeroClass.Summoner?1.4f:3.8f)*r,9f*r);
            if (hero == HeroClass.Summoner)
            {
                if (skill == 0) return new Preview(Shape.Cone, 0, 5f * r, 110);
                if (skill == 5) return new Preview(Shape.Lane, 1f*r, 12f*r);
                if (skill == 1) return new Preview(Shape.Ground, 3.2f * r, 9f * r);
                if (skill == 7) return new Preview(Shape.Ground, 4.4f * r, 9f * r);
                if (skill == 2) return new Preview(Shape.Ground, 1.4f, 9f*r);
                if (skill == 4) return new Preview(Shape.Ground, 10.5f, 9f*r);
                if (skill == 9) return new Preview(Shape.Ground, 3.3f * r, 9f * r);
                return new Preview(Shape.Self, 2.8f * r);
            }
            if (hero == HeroClass.Vanguard)
            {
                if (skill == 0) return new Preview(Shape.Self, 3.4f * r);
                if (skill == 1) return new Preview(Shape.Cone, 0, 4.8f * r, 90 + (rank - 1) * 10);
                if (skill == 2) return new Preview(Shape.Self, 4.1f * r);
                if (skill == 4) return new Preview(Shape.Self, 3.2f * r);
                if (skill == 5) return new Preview(Shape.Lane, 1.3f * r, 7 * r)
                {
                    // HitLine has round end caps. Rank 2 adds a landing burst;
                    // rank 3 also leaves an independent blast at the starting point.
                    startRadius = (rank == 3 ? 2.5f : 1.3f) * r,
                    endRadius = (rank >= 2 ? 2.7f : 1.3f) * r
                };
                if (skill == 7) return new Preview(Shape.Lane, 2.6f * r, (2 + (rank + 4) * 1.8f) * r)
                {
                    // Match each discrete fault in AdvancedSkillSequence, including
                    // the larger awakened finisher and that sequence's arena clamp.
                    startDistance = 2 * r,
                    impactSpacing = 1.8f * r,
                    impactCount = 5 + rank,
                    endRadius = (rank == 3 ? 4f : 2.6f) * r,
                    arenaMargin = .7f
                };
                return new Preview(Shape.Ground, 6.2f * r, 9 * r);
            }
            if (hero == HeroClass.Arcanist)
            {
                if (skill == 0) return new Preview(Shape.Self, 3.7f * r);
                if (skill == 5) return new Preview(Shape.Self, 2.8f * r);
                float size = skill == 1 ? (rank == 3 ? 3.2f : 3f) : skill == 2 ? 3.9f : skill == 4 ? 6 : skill == 7 ? 5.3f : GameBalance.ArcanistFinaleRadius;
                return new Preview(Shape.Ground, size * r, 9 * r);
            }
            if (skill == 0) return new Preview(Shape.Cone, 0, 23 * r, 50);
            if (skill == 4) return new Preview(Shape.Leap, 3.2f*r, 5*r);
            // Phantom volleys seek enemies near the selected ground mark; the circle
            // shows the acquisition area, not a claim that arrows hit the whole disk.
            float radius = skill == 1 ? 3 : skill == 2 ? 4.3f : skill == 5 ? 3.7f : skill == 7 ? 10 : SkillDamageBudgets.RangerUltimateRadius;
            return new Preview(Shape.Ground, radius * r, 9 * r);
        }

        private void EnsureVisuals()
        {
            if (visuals != null) return;
            visuals = new GameObject("Skill placement preview");
            material = CombatFx.NewGlow();
            area = MakeLine("Effect boundary", .075f, new Color(.35f, 1f, .84f, .95f));
            range = MakeLine("Cast distance", .035f, new Color(.63f, .84f, 1f, .35f));
            marker = MakeLine("Selected point", .065f, new Color(1f, .85f, .4f, .95f));
            startArea = MakeLine("Starting impact boundary", .065f, new Color(.35f, 1f, .84f, .8f));
            endArea = MakeLine("Final impact boundary", .075f, new Color(1f, .85f, .4f, .95f));
            for (int i = 0; i < impacts.Length; i++)
                impacts[i] = MakeLine("Travelling impact " + (i + 1), .055f, new Color(.35f, 1f, .84f, .7f));
        }

        private LineRenderer MakeLine(string label, float thickness, Color color)
        {
            GameObject obj = new GameObject(label);
            obj.transform.SetParent(visuals.transform);
            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.widthMultiplier = thickness;
            line.startColor = line.endColor = color;
            line.numCornerVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        private void Circle(LineRenderer line, Vector3 center, float radius, bool respectCover = false)
        {
            bool groundSwing=(owner.HeroClass==HeroClass.Vanguard&&(skill==0||skill==1))||(owner.HeroClass==HeroClass.Summoner&&skill==0);
            if(respectCover)CombatSight.FillAreaBoundary(circle,center,radius,groundSwing?CombatSightKind.Melee:CombatSightKind.Area);
            else for(int i=0;i<circle.Length;i++)
            {float a=i*Mathf.PI*2/circle.Length;circle[i]=center+new Vector3(Mathf.Cos(a)*radius,.12f,Mathf.Sin(a)*radius);}
            line.loop = true; line.positionCount = circle.Length; line.SetPositions(circle);
        }

        private void DrawPreview()
        {
            if (area == null) return;
            if(!previewDirty&&renderedSkill==skill&&renderedRevision==WorldTraversal.Revision&&
                (renderedOrigin-owner.transform.position).sqrMagnitude<.000001f&&
                (renderedTarget-TargetPoint).sqrMagnitude<.000001f&&
                (renderedForward-owner.transform.forward).sqrMagnitude<.000001f)return;
            previewDirty=false;renderedSkill=skill;renderedRevision=WorldTraversal.Revision;
            renderedOrigin=owner.transform.position;renderedTarget=TargetPoint;renderedForward=owner.transform.forward;
            Preview spec = CurrentPreview;
            Vector3 origin = owner.transform.position;
            origin.y = 0;
            Vector3 dir = CombatFx.Flat(TargetPoint - origin).normalized;
            if (dir.sqrMagnitude < .001f) dir = owner.transform.forward;
            range.enabled = spec.shape == Shape.Ground;
            marker.enabled = true;
            area.enabled = true;
            startArea.enabled = endArea.enabled = false;
            for (int i = 0; i < impacts.Length; i++) impacts[i].enabled = false;
            if (range.enabled) Circle(range, origin, spec.distance);
            if (spec.shape == Shape.Self || spec.shape == Shape.Ground)
            {
                Circle(area, spec.shape == Shape.Self ? origin : TargetPoint, spec.radius,true);
                Circle(marker, spec.shape == Shape.Self ? origin : TargetPoint, .2f);
            }
            else if(spec.shape==Shape.Leap)
            {
                Vector3 landing=WorldTraversal.ResolveSkillLanding(origin,dir,spec.distance,.45f,session.ArenaRadius-spec.arenaMargin);
                Circle(area,landing,spec.radius,true);Circle(marker,landing,.22f);
            }
            else if (spec.shape == Shape.Cone)
            {
                fan[0] = origin + Vector3.up * .12f;
                for (int i = 0; i < 33; i++)
                    fan[i + 1] = CombatSight.BoundaryPoint(owner.HeroClass==HeroClass.Ranger?CombatSightKind.Direct:CombatSightKind.Melee,origin,origin + Quaternion.Euler(0, Mathf.Lerp(-spec.angle / 2, spec.angle / 2, i / 32f), 0) * dir * spec.distance) + Vector3.up * .12f;
                fan[34] = fan[0];
                area.loop = false; area.positionCount = fan.Length; area.SetPositions(fan);
                Circle(marker, origin + dir * spec.distance, .22f);
            }
            else
            {
                if (spec.shape == Shape.Retreat) dir = -dir;
                float bound = session.ArenaRadius - spec.arenaMargin;
                Vector3 end = Vector3.ClampMagnitude(origin + dir * spec.distance, bound);
                if (spec.impactCount > 0)
                {
                    // Draw the actual union of impact disks. A single rectangle would
                    // miss the end caps and drift from the curved clamping near walls.
                    area.enabled = false;
                    for (int i = 0; i < spec.impactCount && i < impacts.Length; i++)
                    {
                        Vector3 impact = CombatSight.GroundPoint(origin,Vector3.ClampMagnitude(origin + dir * (spec.startDistance + i * spec.impactSpacing), bound));
                        bool final = i == spec.impactCount - 1;
                        LineRenderer boundary = final ? endArea : impacts[i];
                        boundary.enabled = true;
                        Circle(boundary, impact, final ? spec.endRadius : spec.radius,true);
                    }
                    Circle(marker, end, .35f);
                    return;
                }
                Vector3 path = end - origin;
                Vector3 side = Vector3.Cross(Vector3.up, path.sqrMagnitude > .0001f ? path.normalized : dir) * spec.radius;
                Vector3 up = Vector3.up * .12f;
                lane[0] = origin - side + up; lane[1] = end - side + up;
                lane[2] = end + side + up; lane[3] = origin + side + up; lane[4] = lane[0];
                area.loop = false; area.positionCount = lane.Length; area.SetPositions(lane);
                Circle(marker, end, .35f);
                if (spec.startRadius > 0) { startArea.enabled = true; Circle(startArea, origin, spec.startRadius,true); }
                if (spec.endRadius > 0) { endArea.enabled = true; Circle(endArea, end, spec.endRadius,true); }
            }
        }

        private void OnDestroy()
        {
            if (visuals != null) Destroy(visuals);
            if (material != null) Destroy(material);
        }
    }
}
