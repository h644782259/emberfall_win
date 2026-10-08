using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    /// <summary>Independent touch ownership keeps movement, combat and skill dragging separate.</summary>
    [DefaultExecutionOrder(-200)]
    public sealed partial class MobileControls : MonoBehaviour
    {
        private enum Role { Move, Attack, Skill, Aim, Camera, Consumed }
        private readonly Dictionary<int, Role> fingers = new Dictionary<int, Role>();
        private readonly MobileCameraGesture cameraGesture=new MobileCameraGesture();
        private PlayerController worldPointerOwner;private EnemyController worldPointerTarget;private int worldPointerEpoch;
        private readonly List<int> staleFingers = new List<int>();
        private static MobileControls instance;
        public static bool SimulationEnabled { get; set; }
#if UNITY_EDITOR
        public static bool ValidationUsesSimulation { get; set; }
#endif
        public static bool Active
        {
            get
            {
#if UNITY_EDITOR
                if (ValidationUsesSimulation) return SimulationEnabled;
#endif
#if UNITY_IOS || UNITY_ANDROID
                return true;
#else
                return SimulationEnabled;
#endif
            }
        }
        public static Vector2 Move { get; private set; }
        public static bool AttackHeld { get; private set; }
        private static bool dodge, potion, jump;
        private int moveFinger = -1000;
        private GameSession session;
        private GameUI ui;
        private Texture2D disc;
        public static bool ConsumeDodge() { bool value = dodge; dodge = false; return Active && value; }
        public static bool ConsumePotion() { bool value = potion; potion = false; return Active && value; }
        public static bool ConsumeJump() { bool value = jump; jump = false; return Active && value; }
        public static Rect SafeArea { get { return Active && Screen.safeArea.width > 0 ? Screen.safeArea : new Rect(0, 0, Screen.width, Screen.height); } }
        private Rect lastSafe;
        private Vector2 joystickOrigin;
        private bool hasJoystickOrigin;
        private static MobileControlLayout cachedLayout;
        private static Vector3 cachedLayoutInputs;
        private static int cachedPosition;
        public static MobileControlLayout Layout
        {
            get { Rect safe=SafeArea;Vector3 input=new Vector3(safe.width,safe.height,Screen.dpi);
                if(cachedLayout==null||input!=cachedLayoutInputs||cachedPosition!=EffectPreferences.TouchPosition){cachedLayout=new MobileControlLayout(input.x,input.y,input.z,EffectPreferences.TouchPosition);cachedLayoutInputs=input;cachedPosition=EffectPreferences.TouchPosition;}
                return cachedLayout; }
        }
        private float Scale { get { return Layout.Scale; } }
        private Vector2 Offset { get { Rect safe=SafeArea;return new Vector2(safe.x,Screen.height-safe.yMax); } }
        private static Rect Area(MobileControlLayout.Area a) { return new Rect(a.X,a.Y,a.Width,a.Height); }
        private Rect Joystick { get { return Area(Layout.Joystick); } }
        private Rect Attack { get { return Area(Layout.Attack); } }
        private Rect Dodge { get { return Area(Layout.Dodge); } }
        private Rect Potion { get { return Area(Layout.Potion); } }
        private Rect Jump { get { return Area(Layout.Jump); } }
        private Rect Cancel { get { return Area(Layout.Cancel); } }
        private bool CanCancel
        {
            get { if(session==null||session.Player==null)return false;
                var targeting=session.Player.GetComponent<SkillTargetingController>();var charge=session.Player.GetComponent<SkillChargeController>();
                return targeting!=null&&targeting.IsTargeting||charge!=null&&charge.IsCharging; }
        }
        public static bool IsCancelPoint(Vector2 screen)
        { return instance!=null&&instance.CanCancel&&instance.Cancel.Contains(instance.ToUI(screen)); }

        public void Initialize(GameSession owner) { instance = this; session = owner; ui = owner.GetComponent<GameUI>(); ResetInput(); }
        public static void ResetInput()
        {
            Move = Vector2.zero; AttackHeld = dodge = potion = jump = false;
            if (instance != null) { instance.fingers.Clear(); instance.cameraGesture.Cancel(); instance.worldPointerOwner=null; instance.worldPointerTarget=null; instance.moveFinger = -1000; instance.hasJoystickOrigin=false; if(instance.ui!=null)instance.ui.CancelMobileCast(); }
        }
        private Vector2 ToUI(Vector2 screen) { return (new Vector2(screen.x, Screen.height - screen.y) - Offset) / Scale; }
        public Vector2 ControlScreenPoint(string name)
        {
            Rect control = name == "move" ? Joystick : name == "dodge" ? Dodge : name == "potion" ? Potion : name == "jump" ? Jump : name == "cancel" ? Cancel : name == "interact" ? Area(ui==null?Layout.Interact:ui.MobileInteractionArea) : Attack;
            Vector2 point = control.center * Scale + Offset;
            return new Vector2(point.x, Screen.height - point.y);
        }
        public static bool IsScreenPointOverControls(Vector2 screen)
        {
            if (!Active || instance == null || instance.session == null || instance.session.InputBlocked) return false;
            Vector2 point = instance.ToUI(screen);
            return instance.IsOpportunityPoint(point) || (instance.ui!=null&&instance.ui.CompanionCommandsVisible&&(Area(Layout.FocusCommand).Contains(point)||Area(Layout.RecallCommand).Contains(point))) || instance.IsMovementStart(screen) || instance.Attack.Contains(point) || instance.Dodge.Contains(point) || instance.Potion.Contains(point) || (instance.ui!=null&&instance.ui.MobileInteractionVisible&&Area(instance.ui.MobileInteractionArea).Contains(point)) || instance.Jump.Contains(point) || instance.Cancel.Contains(point);
        }
        // Screen-space third, intersected with the safe area; all visible HUD wins.
        private bool IsMovementStart(Vector2 screen)
        { return SafeArea.Contains(screen) && screen.x < Screen.width / 3f && (ui == null || !ui.IsScreenPointOverHUD(screen)); }
        private void Update()
        {
            if (!Active || session == null || !session.HasStarted) { ResetInput(); return; }
            Rect safe=SafeArea;
            if(safe!=lastSafe) { ResetInput();lastSafe=safe; }
            if (session.InputBlocked)
            {
                Move = Vector2.zero; AttackHeld = dodge = potion = jump = false; moveFinger = -1000;hasJoystickOrigin=false;cameraGesture.Cancel();worldPointerOwner=null;worldPointerTarget=null;
                staleFingers.Clear();
                foreach (KeyValuePair<int, Role> finger in fingers) if (finger.Value != Role.Skill) staleFingers.Add(finger.Key);
                foreach (int finger in staleFingers) fingers.Remove(finger);
            }
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                ProcessPointer(touch.fingerId, touch.phase, touch.position);
            }
            if(!SimulationEnabled)
            {
                staleFingers.Clear();
                foreach(var owned in fingers)
                {
                    bool present=owned.Key==-2&&(Input.GetMouseButton(0)||Input.GetMouseButtonUp(0));for(int t=0;t<Input.touchCount;t++)if(Input.GetTouch(t).fingerId==owned.Key){present=true;break;}
                    if(!present)staleFingers.Add(owned.Key);
                }
                foreach(int stale in staleFingers)ProcessPointer(stale,TouchPhase.Canceled,Vector2.zero);
            }
            // Touches are authoritative while present. A real attached mouse or
            // editor simulation uses the same owner, never a second GUI action.
            if (Input.touchCount == 0 && (SimulationEnabled || Input.mousePresent))
            {
                if (Input.GetMouseButtonDown(0)) ProcessPointer(-2, TouchPhase.Began, Input.mousePosition);
                else if (Input.GetMouseButtonUp(0)) ProcessPointer(-2, TouchPhase.Ended, Input.mousePosition);
                else if (Input.GetMouseButton(0)) ProcessPointer(-2, TouchPhase.Moved, Input.mousePosition);
            }
        }
        public bool ProcessPointer(int finger, TouchPhase phase, Vector2 screen)
        {
            if (!Active || session == null || !session.HasStarted) return false;
            if(ui!=null)ui.RefreshTouchViewport();
            if(ui!=null&&ui.LifecycleTouchBlocked){ResetInput();return false;}
            Vector2 point = ToUI(screen);
            SkillTargetingController targeting = session.Player == null ? null : session.Player.GetComponent<SkillTargetingController>();
            Role role;
            bool ended = phase == TouchPhase.Ended || phase == TouchPhase.Canceled;
            if (phase == TouchPhase.Began)
            {
                if(fingers.ContainsKey(finger))return true;
                if (ui != null && ui.TryBeginTouchSkill(finger, screen)) role = Role.Skill;
                else if (session.InputBlocked) return false;
                else if (IsOpportunityPoint(point)) role = Role.Consumed;
                else if (Attack.Contains(point))
                {
                    if (targeting != null && targeting.IsTargeting) { targeting.Confirm(); role = Role.Consumed; }
                    else role = Role.Attack;
                }
                else if (ui!=null&&ui.CompanionCommandsVisible&&(Area(Layout.FocusCommand).Contains(point)||Area(Layout.RecallCommand).Contains(point)))
                {ui.ActivateFreeCommand(Area(Layout.RecallCommand).Contains(point));role=Role.Consumed;}
                else if (ui!=null&&ui.MobileInteractionVisible&&Area(ui.MobileInteractionArea).Contains(point)) { if(ui!=null)ui.ActivateMobileInteraction(finger);role=Role.Consumed; }
                else if (Dodge.Contains(point)) { CheckDodgeFeedback(); dodge = true; role = Role.Consumed; }
                else if (Potion.Contains(point)) { CheckPotionFeedback(); potion = true; role = Role.Consumed; }
                else if (Cancel.Contains(point) && CanCancel)
                {
                    if (targeting != null) targeting.Cancel();
                    SkillChargeController charge = session.Player == null ? null : session.Player.GetComponent<SkillChargeController>();
                    if (charge != null) charge.Cancel();
                    role = Role.Consumed;
                }
                else if (Jump.Contains(point)) { jump = true; role = Role.Consumed; }
                else if (IsMovementStart(screen))
                {
                    role = moveFinger == -1000 ? Role.Move : Role.Consumed;
                    if (role == Role.Move) { moveFinger = finger; joystickOrigin = point; hasJoystickOrigin = true; }
                }
                else if (targeting != null && targeting.IsTargeting && (ui == null || !ui.IsScreenPointOverUI(screen))) role = Role.Aim;
                else if(SafeArea.Contains(screen)&&(ui==null||!ui.IsScreenPointOverUI(screen)))
                {
                    bool available=true;foreach(var existing in fingers.Values)if(existing!=Role.Move){available=false;break;}
                    role=available&&cameraGesture.Begin(finger,screen.x,screen.y)?Role.Camera:Role.Consumed;
                    if(role==Role.Camera){worldPointerOwner=session.Player;worldPointerEpoch=worldPointerOwner.CombatEpoch;worldPointerTarget=worldPointerOwner.PickMobileTarget(screen);}
                }
                else {cameraGesture.Cancel();return false;}
                if(role!=Role.Move&&role!=Role.Camera)cameraGesture.Cancel();
                fingers[finger] = role;
            }
            else if (!fingers.TryGetValue(finger, out role)) return false;
            if (role == Role.Skill) ui.UpdateTouchSkill(finger, screen, ended, phase == TouchPhase.Canceled);
            else if (role == Role.Move)
            {
                Vector2 delta = (point - joystickOrigin) / 52f;
                float length=delta.magnitude;float strength=MobileControlLayout.DeadZone(length);
                Move = ended || session.InputBlocked || length<.001f ? Vector2.zero : new Vector2(delta.x,-delta.y)/length*strength;
                if (ended) { moveFinger = -1000;hasJoystickOrigin=false; }
            }
            else if(role==Role.Camera)
            {
                bool valid=worldPointerOwner!=null&&session.Player==worldPointerOwner&&worldPointerOwner.CombatEpoch==worldPointerEpoch&&!session.InputBlocked;
                float delta=cameraGesture.Move(finger,screen.x,screen.y,Scale,ended,phase==TouchPhase.Canceled||!valid);
                if(cameraGesture.TapCompleted&&valid&&(ui==null||!ui.IsScreenPointOverUI(screen))&&!IsScreenPointOverControls(screen)&&SafeArea.Contains(screen))
                {
                    var picked=worldPointerOwner.PickMobileTarget(screen);
                    if(picked==worldPointerTarget)worldPointerOwner.PinMobileTarget(picked);
                }
                if(ended){worldPointerOwner=null;worldPointerTarget=null;}
                if(delta!=0&&Camera.main!=null){var camera=Camera.main.GetComponent<AdventureCamera>();if(camera!=null)camera.ApplyMobilePitch(delta);}
            }
            else if (role == Role.Aim && targeting != null && targeting.IsTargeting)
            {
                if(Camera.main==null)return false;
                Ray ray = Camera.main.ScreenPointToRay(screen);
                float distance;
                if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out distance)) targeting.SetTarget(ray.GetPoint(distance));
                if (ended) { if (phase == TouchPhase.Canceled) targeting.Cancel(); else targeting.Confirm(); }
            }
            if (ended) fingers.Remove(finger);
            AttackHeld = !session.InputBlocked && fingers.ContainsValue(Role.Attack);
            return true;
        }
        private void OnGUI()
        {
            if (!Active || session == null || session.InputBlocked) return;
            if (disc == null)
            {
                disc = new Texture2D(96, 96, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp };
                var pixels = new Color[96 * 96];
                for (int y = 0; y < 96; y++) for (int x = 0; x < 96; x++) pixels[y * 96 + x] = new Color(1, 1, 1, Mathf.Clamp01(47 - Vector2.Distance(new Vector2(x, y), new Vector2(47.5f, 47.5f))));
                disc.SetPixels(pixels); disc.Apply(false,true);
            }
            Matrix4x4 oldMatrix = GUI.matrix; Color oldColor = GUI.color;
            GUI.matrix = Matrix4x4.TRS(Offset, Quaternion.identity, new Vector3(Scale, Scale, 1));
            if (hasJoystickOrigin)
            {
                Vector2 origin=joystickOrigin;
                Rect baseRect=new Rect(origin.x-64,origin.y-64,128,128);
                Circle(baseRect, new Color(.10f, .19f, .23f, .55f), "",false);
                Rect thumb = new Rect(origin.x - 25 + Move.x * 45, origin.y - 25 - Move.y * 45, 50, 50);
                Circle(thumb, new Color(.38f, .78f, .71f, .82f), "",false);
            }
            SkillTargetingController targeting = session.Player.GetComponent<SkillTargetingController>();
            SkillChargeController charge = session.Player.GetComponent<SkillChargeController>();
            var hero=session.Player;
            ActionCircle(VisualRect(Attack),targeting!=null&&targeting.IsTargeting?"confirm":"attack",hero!=null&&hero.BasicActionReady);
            ActionCircle(VisualRect(Dodge),"blink",hero!=null&&!hero.IsJumping&&hero.DodgeCooldown<=0);
            ActionCircle(PotionVisualRect(),"potion",hero!=null&&PotionCount>0&&hero.Health<hero.MaxHealth-.5f);
            if(CanCancel)ActionCircle(VisualRect(Cancel),"cancel",true);
            else ActionCircle(VisualRect(Jump),"jump",hero!=null&&!hero.IsJumping);
            DrawAvailability();
            GUI.matrix = oldMatrix; GUI.color = oldColor;
        }
        private void ActionCircle(Rect rect,string icon,bool ready)
        {
            float opacity=EffectPreferences.TouchOpacity;
            if(ready)
            {
                GUI.color=new Color(.35f,1f,.76f,.85f*opacity);float radius=rect.width*.47f;
                for(int i=0;i<40;i++){float angle=i*9*Mathf.Deg2Rad;GUI.DrawTexture(new Rect(rect.center.x+Mathf.Cos(angle)*radius-1,rect.center.y+Mathf.Sin(angle)*radius-1,2,2),Texture2D.whiteTexture);}
            }
            GUI.color=ready?new Color(1,1,1,opacity):new Color(.38f,.42f,.46f,.58f*opacity);
            float size=rect.width*.66f;GUI.DrawTexture(new Rect(rect.center.x-size*.5f,rect.center.y-size*.5f,size,size),UIIconAtlas.Utility(icon),ScaleMode.ScaleToFit,true);GUI.color=Color.white;
        }
        private Rect PotionVisualRect()
        { Rect hit=Potion;float size=36*EffectPreferences.TouchVisualScale;return new Rect(hit.center.x-size*.5f,hit.center.y-size*.5f,size,size); }
        private void Circle(Rect rect, Color color, string icon,bool button=true)
        {
            if(button){rect=VisualRect(rect);color.a*=EffectPreferences.TouchOpacity;}
            GUI.color = color;
            GUI.DrawTexture(rect, disc);
            GUI.color = Color.white;
            if (string.IsNullOrEmpty(icon)) return;
            float size = Mathf.Min(rect.width, rect.height) * .52f;
            Rect centered = new Rect(rect.center.x - size * .5f, rect.center.y - size * .5f, size, size);
            // Texture glyphs cannot inherit a temporary GUIContent string from
            // another MonoBehaviour's OnGUI (for example the notification toast).
            GUI.color=new Color(1,1,1,EffectPreferences.TouchOpacity);
            GUI.DrawTexture(centered, UIIconAtlas.Utility(icon), ScaleMode.ScaleToFit, true);
        }
        private void OnApplicationFocus(bool focus) { if (!focus) ResetInput(); }
        private void OnApplicationPause(bool paused) { if(paused)ResetInput(); }
        private void OnDisable() { ResetInput(); }
        private void OnDestroy() { if (disc != null) Destroy(disc); if (instance == this) { ResetInput(); instance = null; } }
    }
}
