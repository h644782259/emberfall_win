using UnityEngine;

namespace Emberfall
{
    // All primary silhouettes are filled world-space meshes. No damage, physics,
    // screen flashes, character sprites, or camera-facing replacement billboards.
    internal sealed class FilledSkillVfx : MonoBehaviour
    {
        private sealed class Piece
        {
            public Transform Transform; public Renderer Renderer; public MeshFilter Filter;
            public Vector3 Position, Scale; public Quaternion Rotation;
            public float Delay, Phase, TravelScale=1; public int Motion; public bool Anchored, Secondary; public Mesh OwnedMesh;
        }
        private static Mesh identityBlade,identityFork,identityContract,identityProtection;
        private static Mesh icePrimary,firePrimary;
        private static Mesh crescent, crystal, flame, sword, lightning, arcane, rupture, arcaneShard, arrow, vine;
        private static Material sharedMaterial;
        private static int active;
        private readonly Piece[] pieces=new Piece[FilledVfxRecipes.MaximumParts];
        private MaterialPropertyBlock block;
        private void Awake() { EnsurePropertyBlock(); }
        private void EnsurePropertyBlock() { if (block == null) block = new MaterialPropertyBlock(); }
        private int count,epoch,allocated;
        private bool arrowGeometryReady,arrowGeometryFinal,arrowGeometryReduced;
        private int arrowGeometryRevision;
        private GameSession session;
        private bool pooled,disposing;
        private ulong rentGeneration;
        // A retained reference must identify the rental, not just the pooled component.
        internal readonly struct ArrowBatchHandle : System.IDisposable
        {
            private readonly FilledSkillVfx effect;
            private readonly ulong generation;
            internal ArrowBatchHandle(FilledSkillVfx effect)
            {this.effect=effect;generation=effect!=null?effect.rentGeneration:0;}
            public bool IsValid => effect!=null && effect.rentGeneration==generation &&
                !effect.pooled && !effect.disposing && effect.gameObject.activeInHierarchy &&
                effect.owner!=null && !effect.owner.IsDead && effect.owner.CombatEpoch==effect.epoch &&
                effect.session==GameSession.Instance;
            public void ArrowBeat(Vector3 at,float radius,bool final)
            {if(IsValid)effect.ArrowBeat(at,radius,final);}
            public void Retire(){if(IsValid)effect.Retire();}
            public void Dispose(){Retire();}
        }
        private const int PoolCapacity=8;
        private static readonly System.Collections.Generic.Stack<FilledSkillVfx> idle=new System.Collections.Generic.Stack<FilledSkillVfx>();
        private static readonly System.Collections.Generic.HashSet<FilledSkillVfx> instances=new System.Collections.Generic.HashSet<FilledSkillVfx>();
        private PlayerController owner;
        private FilledVfxKind kind;
        private Color tint;
        private float age,life,size;
        private bool registered, confirmedFinale;
        private int finaleCast;
        private float terminalAge;
        private static readonly System.Collections.Generic.List<FilledSkillVfx> finales=new System.Collections.Generic.List<FilledSkillVfx>();
        internal static void SkipFinales(GameSession game)
        {if(game==null)return;foreach(var fx in finales.ToArray())if(fx!=null&&fx.owner==game.Player)fx.Retire();}
        internal static void ConfirmFinale(PlayerController hero,int castId)
        {foreach(var fx in finales)if(fx!=null&&fx.owner==hero&&fx.epoch==hero.CombatEpoch&&fx.finaleCast==castId)fx.confirmedFinale=true;}
        private void RegisterFinale(int castId)
        {
            // Registration starts at the actual final beat, never at charge/early arrows.
            if(castId==0)return;
            finaleCast=castId;confirmedFinale=false;terminalAge=0;
            if(!finales.Contains(this))finales.Add(this);
        }
        private CombatVisualLease lease;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetAssets()
        {
            foreach(var fx in new System.Collections.Generic.List<FilledSkillVfx>(instances))if(fx!=null){fx.disposing=true;fx.gameObject.SetActive(false);Destroy(fx.gameObject);}
            instances.Clear();idle.Clear();
            finales.Clear();
            if(identityBlade!=null)Destroy(identityBlade);if(identityFork!=null)Destroy(identityFork);if(identityContract!=null)Destroy(identityContract);if(identityProtection!=null)Destroy(identityProtection);identityBlade=identityFork=identityContract=identityProtection=null;

            if(icePrimary!=null)Destroy(icePrimary);if(firePrimary!=null)Destroy(firePrimary);icePrimary=firePrimary=null;
            if(crescent!=null)Destroy(crescent);if(crystal!=null)Destroy(crystal);if(flame!=null)Destroy(flame);
            if(sword!=null)Destroy(sword);if(lightning!=null)Destroy(lightning);if(arcane!=null)Destroy(arcane);if(rupture!=null)Destroy(rupture);if(arcaneShard!=null)Destroy(arcaneShard);
            if(arrow!=null)Destroy(arrow);if(vine!=null)Destroy(vine);arrow=vine=null;
            if(sharedMaterial!=null)Destroy(sharedMaterial);crescent=crystal=flame=sword=lightning=arcane=rupture=arcaneShard=null;sharedMaterial=null;active=0;
        }
        private static Mesh Mesh(FilledMeshRecipe data,string name)
        {
            var vertices=new Vector3[data.Positions.Length/3];var uv=new Vector2[vertices.Length];
            for(int i=0;i<vertices.Length;i++){vertices[i]=new Vector3(data.Positions[i*3],data.Positions[i*3+1],data.Positions[i*3+2]);uv[i]=new Vector2(data.Uv[i*2],data.Uv[i*2+1]);}
            var mesh=new Mesh{name=name};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=data.Triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private static void EnsureAssets()
        {
            if(identityBlade==null)identityBlade=AuthoredSpellBases.Identity("BladeSlices");
            if(identityFork==null)identityFork=AuthoredSpellBases.Identity("ForkPulse");
            if(identityContract==null)identityContract=AuthoredSpellBases.Identity("ContractSigil");
            if(identityProtection==null)identityProtection=AuthoredSpellBases.Identity("ProtectionCage");
            if(icePrimary==null)icePrimary=AuthoredSpellBases.Load("IcePrimary");
            if(firePrimary==null)firePrimary=AuthoredSpellBases.Load("FirePrimary");
            if(crescent==null)crescent=AuthoredSpellBases.Load("Crescent")??Mesh(FilledVfxRecipes.Crescent(),"Filled curved crescent volume");
            if(crystal==null)crystal=AuthoredSpellBases.Load("Crystal")??Mesh(FilledVfxRecipes.Crystal(),"Faceted ice spear");
            if(flame==null)flame=AuthoredSpellBases.Load("Flame")??Mesh(FilledVfxRecipes.Flame(),"Curved flame tongue volume");
            if(sword==null)sword=AuthoredSpellBases.Load("Sword")??Mesh(FilledVfxRecipes.Sword(),"Ridged sword blade guard and grip");
            if(lightning==null)lightning=AuthoredSpellBases.Load("Lightning")??Mesh(FilledVfxRecipes.Lightning(),"Angular branched lightning volume");
            if(arcane==null)arcane=Mesh(FilledVfxRecipes.Arcane(),"Arcane cubical lattice");
            if(rupture==null)rupture=AuthoredSpellBases.Load("Rupture")??Mesh(FilledVfxRecipes.Rupture(),"Ground rupture branches");
            if(arcaneShard==null)arcaneShard=AuthoredSpellBases.Load("ArcaneShard")??Mesh(FilledVfxRecipes.ArcaneShard(),"Broken arcane strut");
            if(arrow==null)arrow=AuthoredSpellBases.Load("Arrow")??Mesh(FilledVfxRecipes.Arrow(),"Narrow arrow shaft head and fletching");
            if(vine==null)vine=AuthoredSpellBases.Load("Vine")??Mesh(FilledVfxRecipes.Vine(),"Branching poison vine");
            if(sharedMaterial==null)
            {Shader shader=Resources.Load<Shader>("FilledSpell");sharedMaterial=new Material(shader!=null?shader:Shader.Find("Sprites/Default"));sharedMaterial.renderQueue=3070;}
        }
        private static FilledSkillVfx Create(PlayerController hero,Vector3 at,Vector3 forward,FilledVfxKind type,float radius,Color color,float duration,Transform parent=null,CombatVisualPriority priority=CombatVisualPriority.Decoration)
        {
            if(hero==null||hero.IsDead||!Finite(radius)||radius<=0||!Finite(forward.x)||!Finite(forward.y)||!Finite(forward.z)||!Finite(duration)||duration<=0||!Finite(at.x)||!Finite(at.y)||!Finite(at.z))return null;
            EnsureAssets();FilledSkillVfx fx=null;
            while(idle.Count>0&&fx==null){var candidate=idle.Pop();if(candidate!=null&&!candidate.disposing)fx=candidate;}
            if(fx==null){var fresh=new GameObject("Filled effect");fresh.SetActive(false);fx=fresh.AddComponent<FilledSkillVfx>();instances.Add(fx);}
            fx.EnsurePropertyBlock();
            var root=fx.gameObject;checked{fx.rentGeneration++;}fx.pooled=false;root.name="Filled "+type+" effect";root.transform.SetParent(parent,false);root.transform.localScale=Vector3.one;root.transform.position=at;
            root.transform.rotation=Quaternion.LookRotation(forward.sqrMagnitude>.0001f?forward.normalized:Vector3.forward);
            fx.owner=hero;fx.session=GameSession.Instance;fx.epoch=hero.CombatEpoch;fx.kind=type;fx.size=Mathf.Clamp(radius,.15f,8);
            fx.tint=color;fx.life=Mathf.Clamp(duration,.12f,12);fx.age=0;fx.terminalAge=0;fx.finaleCast=0;fx.confirmedFinale=false;
            ulong generation=fx.rentGeneration;
            fx.lease=CombatVisualLease.Attach(root,priority,()=>{if(fx.rentGeneration==generation)fx.Retire();});if(fx.lease==null)return null;
            root.SetActive(true);fx.Register();return fx;
        }
        public static void Crescent(PlayerController hero,Vector3 at,Vector3 forward,float radius,Color color,int swingSide=1,CombatVisualPriority priority=CombatVisualPriority.Decoration,int castId=0)
        {
            var fx=Create(hero,at+Vector3.up*.82f,forward,FilledVfxKind.Crescent,radius,color,.34f,priority:priority);if(fx==null)return;
            if(priority==CombatVisualPriority.Finale)fx.RegisterFinale(castId);
            if(identityBlade!=null)fx.Add(identityBlade,Vector3.zero,new Vector3(fx.size,fx.size*.7f,fx.size),Quaternion.Euler(-12,0,0),0,15,swingSide,fx.size*1.1f,"Identity blade slice",true);
            else {
            fx.Add(crescent,Vector3.zero,new Vector3(fx.size,fx.size*.7f,fx.size),Quaternion.Euler(-12,0,0),0,0,swingSide);
            fx.Add(crescent,new Vector3(0,.08f,-.1f),Vector3.one*fx.size*.88f,Quaternion.Euler(8,-16*swingSide,0),.02f,0,swingSide);
            }
            for(int i=0;i<4;i++)fx.Add(crystal,new Vector3((i-1.5f)*.25f,.1f,.8f)*fx.size,new Vector3(.09f,.4f,.12f)*fx.size,Quaternion.Euler(85,i*33,0),.02f+i*.018f,4,i);
        }
        public static void Impact(PlayerController hero,Vector3 at,float radius,FilledVfxKind type,Color color,CombatVisualPriority priority=CombatVisualPriority.ActionBody,int castId=0)
        {
            if(type!=FilledVfxKind.Ice&&type!=FilledVfxKind.Fire&&type!=FilledVfxKind.Summon&&type!=FilledVfxKind.Sword&&type!=FilledVfxKind.Lightning&&type!=FilledVfxKind.Arcane)return;
            var fx=Create(hero,at,Vector3.forward,type,radius,color,type==FilledVfxKind.Fire?.8f:1.05f,priority:priority);if(fx==null)return;
            if(priority==CombatVisualPriority.Finale)fx.RegisterFinale(castId);
            float unit=Mathf.Min(1.6f,fx.size*.55f);
            // Allocate landing base, identity silhouette and contact flash BEFORE repeated ornaments.
            // Add's actual budget is still the last authority: mobile 10, reduced 7.
            fx.Add(rupture,Vector3.up*.07f,new Vector3(fx.size*.58f,.8f,fx.size*.58f),Quaternion.identity,0,5,0,fx.size*.7f,"Landing base",true);
            Mesh main=type==FilledVfxKind.Sword?sword:type==FilledVfxKind.Lightning?(identityFork??lightning):type==FilledVfxKind.Arcane?arcane:type==FilledVfxKind.Ice?(icePrimary??crystal):type==FilledVfxKind.Fire?(firePrimary??flame):(identityContract??crescent);
            int motion=type==FilledVfxKind.Sword?8:type==FilledVfxKind.Lightning?(identityFork!=null?17:9):type==FilledVfxKind.Arcane?10:type==FilledVfxKind.Ice?(icePrimary!=null?13:1):type==FilledVfxKind.Fire?(firePrimary!=null?14:2):(identityContract!=null?16:3);
            Vector3 dimensions=type==FilledVfxKind.Sword?new Vector3(.95f,3.2f,.95f)*unit:type==FilledVfxKind.Lightning?new Vector3(1.3f,2.2f,1.3f)*unit:Vector3.one*unit*1.65f;
            fx.Add(main,Vector3.zero,dimensions,Quaternion.identity,0,motion,0,
                type==FilledVfxKind.Sword?unit*.55f:type==FilledVfxKind.Lightning?unit*.85f:type==FilledVfxKind.Summon?unit*1.8f+fx.size*.3f:unit*2f,"Primary "+type,true);
            fx.Add(rupture,Vector3.up*.11f,new Vector3(fx.size*.3f,.65f,fx.size*.3f),Quaternion.Euler(0,65,0),0,5,1,fx.size*.38f,"Contact flash",true);
            if(priority==CombatVisualPriority.Finale)
                fx.Add(rupture,Vector3.up*.13f,new Vector3(fx.size*.2f,.35f,fx.size*.2f),Quaternion.identity,.12f,5,0,fx.size*.25f,"Finale short tail",true);
            int n=EffectPreferences.ReducedEffects?5:type==FilledVfxKind.Summon?6:10;
            for(int i=0;i<n;i++)
            {
                float angle=i*2.399963f,r=(.18f+(i%3)*.24f)*fx.size;Vector3 radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                if(type==FilledVfxKind.Ice)
                    fx.Add(crystal,radial*r,new Vector3(.32f,1.1f+(i%3)*.45f,.37f)*unit,Quaternion.Euler(radial.z*23,angle*Mathf.Rad2Deg,-radial.x*23),i*.016f,1,angle,unit*1.2f);
                else if(type==FilledVfxKind.Fire)
                    fx.Add(flame,radial*r*.55f,new Vector3(.95f,1.6f+(i%3)*.3f,.95f)*unit,Quaternion.Euler(radial.z*17,i*51,-radial.x*17),i*.012f,2,angle,unit*1.8f);
                else if(type==FilledVfxKind.Summon)
                    fx.Add(crescent,radial*fx.size*.25f+Vector3.up*.65f,Vector3.one*fx.size*.4f,Quaternion.Euler(90,angle*Mathf.Rad2Deg,0),i*.024f,3,angle,fx.size*.9f);
                else
                    fx.Add(type==FilledVfxKind.Sword?rupture:type==FilledVfxKind.Arcane?arcaneShard:main,radial*r+Vector3.up*.16f,Vector3.one*unit*.35f,Quaternion.Euler(0,i*51,0),(type==FilledVfxKind.Arcane?.34f:.08f)+i*.018f,type==FilledVfxKind.Sword?5:type==FilledVfxKind.Arcane?3:motion,angle,unit*.6f+(type==FilledVfxKind.Arcane?fx.size*.3f:0));
            }
        }
        internal static ArrowBatchHandle BeginArrowBatch(PlayerController hero,Vector3 at,float radius,Color color,bool final=false,CombatVisualPriority priority=CombatVisualPriority.ActionBody,int castId=0)
        {
            var fx=Create(hero,at,Vector3.forward,FilledVfxKind.ArrowRain,radius,color,12,priority:final?CombatVisualPriority.Finale:priority);
            if(fx!=null){fx.finaleCast=castId;fx.kind=FilledVfxKind.Charge;fx.Add(crescent,Vector3.up*.18f,new Vector3(radius*.3f,.2f,radius*.3f),Quaternion.identity,0,6,0,radius*.4f,"Arrow charge envelope",true);}
            return new ArrowBatchHandle(fx);
        }
        internal static ArrowBatchHandle ArrowRain(PlayerController hero,Vector3 at,float radius,Color color,bool final=false,CombatVisualPriority priority=CombatVisualPriority.ActionBody,int castId=0)
        {
            // Immediate rain has no charge phase: avoid building and clipping a discarded envelope.
            var fx=Create(hero,at,Vector3.forward,FilledVfxKind.ArrowRain,radius,color,final?1.05f:.65f,priority:final?CombatVisualPriority.Finale:priority);
            if(fx!=null){fx.finaleCast=castId;fx.ArrowBeat(at,radius,final);}
            return new ArrowBatchHandle(fx);
        }
        private void ArrowBeat(Vector3 at,float radius,bool final)
        {
            if(!gameObject.activeInHierarchy||owner==null||owner.IsDead||owner.CombatEpoch!=epoch)return;
            float nextSize=Mathf.Clamp(radius,.15f,16);
            bool reuse=arrowGeometryReady&&arrowGeometryFinal==final&&arrowGeometryReduced==EffectPreferences.ReducedEffects&&
                arrowGeometryRevision==WorldTraversal.Revision&&(transform.position-at).sqrMagnitude==0&&size==nextSize;
            if(reuse)
            {
                age=0;life=final?1.05f:.65f;
                for(int i=0;i<count;i++)Animate(pieces[i]);
                return;
            }
            ClearPieces();kind=FilledVfxKind.ArrowRain;transform.position=at;age=0;life=final?1.05f:.65f;size=nextSize;
            if(final){RegisterFinale(finaleCast);if(lease!=null)lease.Promote(CombatVisualPriority.Finale);}
            Add(rupture,Vector3.up*.08f,Vector3.one*(final?size*.65f:.3f),Quaternion.identity,0,5,0,final?size:.4f,"Arrow landing contact",true);
            Add(arrow,Vector3.zero,new Vector3(final?1.7f:.8f,final?3.2f:1.1f,final?1.7f:.8f),Quaternion.identity,0,11,0,.3f,"Primary falling arrow",true);
            Add(rupture,Vector3.up*.1f,Vector3.one*.22f,Quaternion.identity,0,4,0,final?3.5f:2.3f,"Arrow impact fragments");
            bool ultimate=finaleCast>0;
            int maximum=ultimate?(EffectPreferences.ReducedEffects?5:10):(EffectPreferences.ReducedEffects?3:4);
            for(int i=0;i<maximum;i++)
            {
                float a=i*2.39996f,r=size*(ultimate?(.25f+(i%4)*.18f):(.15f+(i%3)*.2f));var offset=new Vector3(Mathf.Cos(a)*r,.02f,Mathf.Sin(a)*r);
                Add(arrow,offset,new Vector3(ultimate?.95f:.7f,ultimate?1.6f:.55f+(i%2)*.2f,ultimate?.95f:.7f),Quaternion.Euler(0,i*47,0),ultimate?(i%3)*.035f:0,ultimate?11:12,0,.25f,"Short embedded arrow");
            }
            arrowGeometryReady=true;arrowGeometryFinal=final;arrowGeometryReduced=EffectPreferences.ReducedEffects;arrowGeometryRevision=WorldTraversal.Revision;
        }
        internal static void PoisonVines(PlayerController hero,Vector3 at,float radius,Color color)
        {
            var fx=Create(hero,at,Vector3.forward,FilledVfxKind.Vine,radius,color,1.05f,priority:CombatVisualPriority.SustainedBackground);if(fx==null)return;
            for(int i=0;i<3;i++)fx.Add(vine,Vector3.zero,new Vector3(radius*.5f,1,radius*.5f),Quaternion.Euler(0,i*120,0),0,0,0,radius,"Poison branch vine",true);
        }
        public static void Charge(Transform parent,PlayerController hero,Vector3 at,float radius,Color color,float duration,int identity=0,bool protectionEnvelope=false,int protectionStyle=0)
        {
            var fx=Create(hero,at,Vector3.forward,FilledVfxKind.Charge,Mathf.Min(radius,3.5f),color,duration,parent,CombatVisualPriority.SustainedBackground);if(fx==null)return;
            Mesh authored=identity==1?identityBlade:identity==2?identityFork:identity==3?identityContract:identity==4?identityProtection:null;
            if(authored!=null)
            {
                // Persistent protection encloses the body, independent of gameplay radius.
                // Ordinary charge/contact envelopes keep their original dimensions.
                float width=fx.size*.55f,height=width;
                if(protectionEnvelope&&identity==4){width=Mathf.Max(width,2.6f);height=Mathf.Max(height,2.6f);}
                fx.Add(authored,Vector3.up*.08f,new Vector3(width,height,width),Quaternion.identity,0,protectionEnvelope&&identity==4?20:18,protectionEnvelope&&identity==4?Mathf.Clamp(protectionStyle,0,3):identity,
                    protectionEnvelope&&identity==4?Mathf.Max(fx.size*.8f,width*.7f):fx.size*.8f,"Identity preparation",true);return;
            }
            for(int i=0;i<3;i++)fx.Add(crescent,Vector3.up*(.18f+i*.2f),new Vector3(fx.size*.65f,.8f,fx.size*.65f),Quaternion.Euler(i*12,i*120,0),0,6,i);
        }
        // Only called after the real health mutation. No unconditional success flash at full HP.
        internal static void HealingPulse(PlayerController hero,Color color,Transform recipient=null)
        {
            if(hero==null)return;
            if(recipient==null){Charge(hero.transform,hero,hero.transform.position,2.6f,color,.32f,4,true,3);return;}
            // Companion success is a short low contact accent at the actual recipient,
            // not a player-sized shield or a claim that the caster was healed.
            var fx=Create(hero,recipient.position,Vector3.forward,FilledVfxKind.Charge,.8f,color,.32f,recipient,CombatVisualPriority.RealContact);
            if(fx!=null)fx.Add(identityProtection??crescent,Vector3.up*.06f,new Vector3(.8f,.6f,.8f),Quaternion.identity,0,20,3,.8f,"Companion actual healing",true);
        }
        // Visual endpoints consume the actual caller's release/hit position; no target search or damage lives here.
        internal static bool IdentityContact(PlayerController hero,Vector3 at,Vector3 forward,float radius,Color color,int identity,CombatVisualPriority priority=CombatVisualPriority.ActionBody)
        {
            EnsureAssets();Mesh mesh=identity==2?identityFork:identity==3?identityContract:identity==4?identityProtection:identityBlade;if(mesh==null)return false;
            var fx=Create(hero,at,forward,identity==2?FilledVfxKind.Lightning:FilledVfxKind.Summon,radius,color,.6f,priority:priority);if(fx==null)return true;
            float unit=Mathf.Min(1.6f,radius);fx.Add(mesh,Vector3.up*.07f,Vector3.one*unit,Quaternion.identity,0,identity==2?17:identity==3?16:19,0,unit,"Identity actual event",true);return true;
        }
        public static void Thrust(PlayerController hero,Vector3 start,Vector3 end,Color color,float lifetime,float width,CombatVisualPriority priority=CombatVisualPriority.ActionBody)
        {
            if(!Finite(end.x)||!Finite(end.y)||!Finite(end.z)||!Finite(width)||width<=0)return;
            Vector3 delta=end-start;if(delta.sqrMagnitude<.001f)return;
            var fx=Create(hero,start,Vector3.forward,FilledVfxKind.Thrust,1,color,Mathf.Min(lifetime,.7f),priority:priority);if(fx==null)return;
            fx.Add(flame,Vector3.zero,new Vector3(Mathf.Clamp(width*3,.22f,1.5f),Mathf.Min(24,delta.magnitude),Mathf.Clamp(width*3,.22f,1.5f)),Quaternion.FromToRotation(Vector3.up,delta),0,7,0);
            fx.Add(crystal,delta*.84f,new Vector3(width*1.4f,Mathf.Min(3,delta.magnitude*.3f),width*1.4f),Quaternion.FromToRotation(Vector3.up,delta),0,7,1);
        }
        internal static void BurnContact(PlayerController hero,Vector3 at,bool finale)
        {
            var fx=Create(hero,at,Vector3.forward,FilledVfxKind.Fire,.4f,new Color(1,.56f,.16f),.32f,
                priority:CombatVisualPriority.RealContact);
            if(fx==null)return;
            fx.Add(rupture,Vector3.up*.1f,new Vector3(.35f,.2f,.35f),Quaternion.identity,0,5,0,.4f,"Burn real contact",true);
            fx.Add(flame,Vector3.zero,new Vector3(.22f,.6f,.22f),Quaternion.identity,0,2,0,.25f,"Burn contact ember",true);
        }
        private void Add(Mesh mesh,Vector3 at,Vector3 dimensions,Quaternion rotation,float delay,int motion,float phase,float footprint=0,string label="Repeated ornament",bool anchored=false)
        {
            bool secondary=label=="Repeated ornament"&&((kind==FilledVfxKind.Ice&&icePrimary!=null)||(kind==FilledVfxKind.Fire&&firePrimary!=null));
            if(secondary)dimensions*=.6f; // Keep authored primary readable; footprint reservation stays conservative.
            int cap=EffectPreferences.ReducedEffects?FilledVfxRecipes.ReducedParts:Application.isMobilePlatform?10:FilledVfxRecipes.MaximumParts;
            if(count>=cap)return;
            float fit=1;
            if(footprint>0&&!anchored)
            {
                float px,pz;
                if(!FilledVfxPlacement.TryPlace(at.x,at.z,size,footprint,(x,z,r)=>{
                    Vector3 candidate=transform.position+new Vector3(x,0,z);
                    // The center ray must be damage-visible; only the destination needs a full disk.
                    return CombatSight.Area(transform.position,candidate)&&CombatSight.VisualFootprint(candidate,candidate,r);
                },out px,out pz,out fit))return;
                at.x=px;at.z=pz;dimensions*=fit;
            }
            Mesh owned=null;
            if(anchored)
            {
                owned=AnchoredImpactMesh.Create(mesh,transform,at,dimensions*1.15f,rotation);
                mesh=owned;at=Vector3.zero;dimensions=Vector3.one/1.15f;rotation=Quaternion.identity;
            }
            Piece piece=pieces[count];
            if(piece==null){var obj=new GameObject();obj.transform.SetParent(transform,false);piece=new Piece{Transform=obj.transform,Filter=obj.AddComponent<MeshFilter>(),Renderer=obj.AddComponent<MeshRenderer>()};pieces[count]=piece;allocated=Mathf.Max(allocated,count+1);}
            var renderer=piece.Renderer;piece.Transform.gameObject.name="Filled spell surface / "+label;piece.Filter.sharedMesh=mesh;renderer.sharedMaterial=sharedMaterial;renderer.enabled=true;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.sortingOrder=10;
            piece.Position=at;piece.Scale=dimensions;piece.Rotation=rotation;piece.Delay=delay;piece.Motion=motion;piece.Phase=phase;piece.TravelScale=footprint>0?fit:1;piece.Anchored=anchored;piece.Secondary=secondary;piece.OwnedMesh=owned;count++;
            Animate(piece); // The mesh exists at the actual hit frame, before the next Update.
        }
        private void Update()
        {
            var game=GameSession.Instance;
            if(owner==null||owner.IsDead||owner.CombatEpoch!=epoch||game==null||game!=session||game.Player!=owner||!game.HasStarted){Retire();return;}
            if(game.ModeFinished)
            {
                if(!confirmedFinale){Retire();return;}
                terminalAge+=Time.unscaledDeltaTime;age+=Time.unscaledDeltaTime;
                if(terminalAge>=.65f||age>=life){Retire();return;}
                for(int i=0;i<count;i++)Animate(pieces[i]);
                return; // Visual-only: no scheduled combat or reward callbacks.
            }
            if(game.InputBlocked||Time.deltaTime<=0)return;
            age+=Time.deltaTime;if(age>=life){Retire();return;}
            for(int i=0;i<count;i++)Animate(pieces[i]);
        }
        private void Animate(Piece p)
        {
            float local=age-p.Delay;if(local<0){p.Transform.gameObject.SetActive(false);return;}
            p.Transform.gameObject.SetActive(true);var f=FilledVfxRecipes.Sample(kind,local,Mathf.Max(.1f,life-p.Delay));
            Vector3 at=p.Position,scale=p.Scale;Quaternion rotation=p.Rotation;
            float t=f.Progress;
            switch(p.Motion)
            {
                // Authored primary visual beats only: visible at release, quick rise, readable hold, then contraction.
                // Radial scale only contracts; anchored certification and actual hit time remain unchanged.
                case 15: float sliceHand=p.Phase<0?-1:1;rotation*=Quaternion.Euler(0,Mathf.Lerp(-12,42,t)*sliceHand,0);scale*=1-t*.15f;break;
                case 16: scale.y*=.82f+.18f*Mathf.Min(1,local/.08f);break;
                case 17: scale.y*=1-Mathf.Clamp01(local/.45f)*.25f;break;
                // Persistent body protection must not grow through head ornaments at birth.
                case 20: break;
                case 18: scale.y*=.65f+.35f*Mathf.Min(1,local/.18f);break;
                case 19: scale.y*=.82f+.18f*Mathf.Min(1,local/.1f);break;
                case 13: scale.y*=local<.09f?Mathf.Lerp(.45f,1,local/.09f):local<.42f?1:Mathf.Lerp(1,.62f,Mathf.Clamp01((local-.42f)/.5f));break;
                case 14: scale.y*=local<.07f?Mathf.Lerp(.55f,1,local/.07f):local<.23f?1:Mathf.Lerp(1,.48f,Mathf.Clamp01((local-.23f)/.5f));scale.x*=1-Mathf.Clamp01((local-.23f)/.5f)*.24f;scale.z*=1-Mathf.Clamp01((local-.23f)/.5f)*.24f;break;
                case 0: float handed=p.Phase<0?-1:1;scale*=.85f+t*.3f;rotation*=Quaternion.Euler(0,Mathf.Lerp(-18,38,t)*handed,-t*11*handed);break;
                case 1: scale.y*=.25f+.75f*Mathf.Min(1,local*18);at.y-=Mathf.Max(0,t-.55f)*1.3f;break;
                case 2: scale*=f.Expansion;scale.y*=1+t*.55f;at.y+=t*.65f;rotation*=Quaternion.Euler(0,t*45,0);break;
                case 3: at+=new Vector3(Mathf.Cos(p.Phase),0,Mathf.Sin(p.Phase))*t*size*.3f*p.TravelScale;at.y+=Mathf.Sin(t*Mathf.PI)*1.1f;scale*=1-t*.4f;rotation*=Quaternion.Euler(t*65,t*35,0);break;
                case 4: at+=new Vector3(Mathf.Sin(p.Phase),.4f,Mathf.Cos(p.Phase))*local*3*p.TravelScale;scale*=1-t*.8f;break;
                case 5: scale*=f.Expansion;rotation*=Quaternion.Euler(0,t*45,0);break;
                case 6: scale*=.8f+Mathf.Sin(t*Mathf.PI)*.2f;rotation*=Quaternion.Euler(0,local*(p.Phase%2==0?95:-80),0);at.y+=Mathf.Sin(local*3+p.Phase)*.06f;break;
                case 7: scale.x*=1-t*.75f;scale.z*=1-t*.75f;break;
                // The tip is already on the real landing frame; settle does not fake a delayed hit.
                case 8: at.y-=Mathf.Min(.12f,t*.5f)*p.TravelScale;scale.y*=1-Mathf.Max(0,t-.55f)*.3f;break;
                case 9: scale.x*=t<.12f?1:t<.28f?.78f:1-t*.5f;scale.z*=1-t*.4f;break;
                case 11: scale.y*=1-Mathf.Clamp01(local*8)*.75f;break; // The shaft collapses into an already visible contact, never a late fake hit.
                case 12: at.y-=Mathf.Max(0,local-.22f)*.3f;break;
                case 10: scale*=t<.18f?Mathf.Lerp(1,.58f,t/.18f):t<.42f?Mathf.Lerp(.58f,1.08f,(t-.18f)/.24f):1.08f-(t-.42f)*.55f;rotation*=Quaternion.Euler(t*30,t*55,0);break;
            }
            if(p.Anchored){at=p.Position;rotation=p.Rotation;float radial=Mathf.Min(1,Mathf.Min(scale.x,scale.z));scale.x=scale.z=radial;} // Uniform radial contraction preserves each certified LOS ray; anisotropic scaling does not.
            p.Transform.localPosition=at;p.Transform.localScale=scale;p.Transform.localRotation=rotation;
            // Placement reserved the complete motion footprint once. Do not toggle a whole
            // primary silhouette each frame when a growing bounds circle grazes a wall.
            Color color=tint;if(p.Motion==17)color.a*=local<.07f?1:local<.14f?.48f:local<.22f?.82f:.55f;if(p.Secondary)color.a*=.55f*Mathf.Clamp01((.62f-local)/.2f);color.a*=f.Opacity*(kind==FilledVfxKind.Charge?.35f:.9f)*Mathf.Lerp(.55f,1,EffectPreferences.EffectsScale);
            float opacity=f.Opacity;
            if(p.Motion==20)
            {
                // Keep the certified body envelope full-sized from birth. Distinction
                // comes from light/UV bands, never shrinking bars through the actor.
                float intensity=p.Phase<.5f?.30f:p.Phase<1.5f?(local<.28f?Mathf.Lerp(.62f,.20f,local/.28f):.20f):p.Phase<2.5f?.18f:.58f*Mathf.Clamp01(1-local/.32f);
                color=tint;color.a*=intensity*Mathf.Lerp(.55f,1,EffectPreferences.EffectsScale);
                opacity=p.Phase<2.5f?1:Mathf.Clamp01(1-local/.32f);
            }
            block.SetFloat("_Element",kind==FilledVfxKind.Fire?1:kind==FilledVfxKind.Ice?2:kind==FilledVfxKind.Lightning?3:kind==FilledVfxKind.Vine?4:kind==FilledVfxKind.Summon||kind==FilledVfxKind.Arcane?5:0);
            block.SetFloat("_Seed",p.Phase*.37f+p.Delay*3);
            block.SetColor("_Color",color);block.SetFloat("_Opacity",opacity);block.SetFloat("_Progress",t);
            block.SetFloat("_EnvelopeMode",p.Motion==20?p.Phase+1:0);block.SetFloat("_EnvelopeAge",local);
            block.SetFloat("_Style",kind==FilledVfxKind.Fire||kind==FilledVfxKind.Summon?1:.35f);p.Renderer.SetPropertyBlock(block);
        }
        private void Register(){if(registered)return;registered=true;active++;}
        private void Release(){if(!registered)return;registered=false;active=Mathf.Max(0,active-1);}
        private void OnDisable(){finales.Remove(this);Release();}
        private void OnEnable()
        {
            if(!gameObject.activeInHierarchy||owner==null||registered)return;
            Register();
        }
        internal void Retire()
        {
            if(pooled||disposing)return;
            pooled=true;gameObject.SetActive(false);finales.Remove(this);Release();
            var ticket=GetComponent<CombatVisualLease>();if(ticket!=null)ticket.Suspend();lease=null;
            ClearPieces();owner=null;session=null;epoch=0;age=life=size=terminalAge=0;kind=default(FilledVfxKind);tint=default(Color);finaleCast=0;confirmedFinale=false;
            transform.SetParent(null,false);transform.localPosition=Vector3.zero;transform.localRotation=Quaternion.identity;transform.localScale=Vector3.one;
            if(idle.Count<PoolCapacity)idle.Push(this);else{disposing=true;Destroy(gameObject);}
        }
        private void ClearPieces()
        {
            arrowGeometryReady=false;
            EnsurePropertyBlock();
            block.SetColor("_Color",new Color(0,0,0,0));block.SetFloat("_Opacity",0);block.SetFloat("_Progress",0);block.SetFloat("_Style",0);block.SetFloat("_EnvelopeMode",0);block.SetFloat("_EnvelopeAge",0);
            for(int i=0;i<allocated;i++)
            {
                var p=pieces[i];if(p==null)continue;
                if(p.OwnedMesh!=null)Destroy(p.OwnedMesh);p.OwnedMesh=null;
                if(p.Transform!=null){p.Transform.gameObject.SetActive(false);p.Transform.localPosition=Vector3.zero;p.Transform.localScale=Vector3.one;p.Transform.localRotation=Quaternion.identity;}
                if(p.Filter!=null)p.Filter.sharedMesh=null;
                if(p.Renderer!=null){p.Renderer.SetPropertyBlock(block);p.Renderer.sharedMaterial=null;p.Renderer.enabled=false;}
                p.Position=Vector3.zero;p.Scale=Vector3.one;p.Rotation=Quaternion.identity;p.Delay=p.Phase=0;p.TravelScale=1;p.Motion=0;p.Anchored=p.Secondary=false;
            }
            count=0;
        }
        private void OnDestroy(){disposing=true;instances.Remove(this);finales.Remove(this);Release();ClearPieces();owner=null;session=null;}
        private static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
    }
}
