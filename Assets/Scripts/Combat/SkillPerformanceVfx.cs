using UnityEngine;
namespace Emberfall
{
    internal enum SkillPerformanceBody { Tornado, Gravity, Meteor, ArrowVolley, Poison, Spirit, Frost, Fault, Fire, Lightning }
    // Real 3D bodies, textured turbulent energy and bounded particles. This never selects or damages targets.
    internal sealed class SkillPerformanceVfx : MonoBehaviour
    {
        private static Mesh funnel,windWall,orbit,orb;
        private static Material energy;
        private static int lights;
        private PlayerController owner;private GameSession session;private int epoch;
        private Transform authority;private bool bound,retired,ownsLight;
        private SkillPerformanceBody kind;private float age,life,size,beatAge,beatDuration;
        private Vector3 launch,landing;private CombatModel model;
        private Transform[] parts;private Renderer[] surfaces;private MaterialPropertyBlock block;
        private Light glow;private ParticleSystem particles;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){if(funnel!=null)Destroy(funnel);if(windWall!=null)Destroy(windWall);if(orbit!=null)Destroy(orbit);if(orb!=null)Destroy(orb);if(energy!=null)Destroy(energy);funnel=windWall=orbit=orb=null;energy=null;lights=0;}
        private static void Assets()
        {
            if(energy==null)energy=new Material(Resources.Load<Shader>("SpellEnergySurface"));
            if(funnel==null)funnel=Tube(true);if(windWall==null)windWall=WindWall();if(orbit==null)orbit=Tube(false);
            if(orb==null){var temp=GameObject.CreatePrimitive(PrimitiveType.Sphere);temp.SetActive(false);orb=Instantiate(temp.GetComponent<MeshFilter>().sharedMesh);orb.name="Spell absorption sphere";Destroy(temp);}
        }
        private static Mesh Tube(bool cone)
        {
            const int length=64,sides=6;var vertices=new Vector3[(length+1)*sides];var uv=new Vector2[vertices.Length];var triangles=new int[length*sides*6];
            for(int i=0;i<=length;i++)
            {
                float t=i/(float)length,a=t*Mathf.PI*(cone?6:2),r=cone?.16f+t*.8f:1;
                Vector3 center=new Vector3(Mathf.Cos(a)*r,cone?t:Mathf.Sin(a*3)*.04f,Mathf.Sin(a)*r);
                Vector3 radial=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));float thickness=cone?.025f+t*.065f:.075f;
                for(int j=0;j<sides;j++){float q=j*Mathf.PI*2/sides;int v=i*sides+j;vertices[v]=center+(radial*Mathf.Cos(q)+Vector3.up*Mathf.Sin(q))*thickness;uv[v]=new Vector2(t,j/(float)sides);
                    if(i<length){int at=(i*sides+j)*6,n=i*sides+(j+1)%sides;triangles[at]=v;triangles[at+1]=v+sides;triangles[at+2]=n;triangles[at+3]=n;triangles[at+4]=v+sides;triangles[at+5]=n+sides;}}
            }
            var mesh=new Mesh{name=cone?"Thick helical funnel volume":"Warped accretion volume"};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private static Mesh WindWall()
        {
            const int rings=16,sides=40;var vertices=new Vector3[(rings+1)*(sides+1)];var uv=new Vector2[vertices.Length];var triangles=new int[rings*sides*6];
            for(int y=0;y<=rings;y++)for(int x=0;x<=sides;x++)
            {
                float t=y/(float)rings,a=x/(float)sides*Mathf.PI*2;
                float r=(.16f+t*.8f)*(1+.10f*Mathf.Sin(a*3+t*13)+.06f*Mathf.Cos(a*5-t*9));
                int v=y*(sides+1)+x;vertices[v]=new Vector3(Mathf.Cos(a)*r,t+Mathf.Sin(a*3+t*9)*.022f,Mathf.Sin(a)*r);uv[v]=new Vector2(x/(float)sides,t);
                if(y<rings&&x<sides){int at=(y*sides+x)*6,n=v+sides+1;triangles[at]=v;triangles[at+1]=n;triangles[at+2]=v+1;triangles[at+3]=v+1;triangles[at+4]=n;triangles[at+5]=n+1;}
            }
            var mesh=new Mesh{name="Turbulent translucent funnel walls"};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        internal static SkillPerformanceVfx Sustain(PlayerController hero,Vector3 at,float radius,float duration,SkillPerformanceBody type,Transform follow=null)
        {
            if(hero==null||hero.IsDead)return null;
            var root=new GameObject("Skill performance / "+type);root.transform.position=at;
            if(CombatVisualLease.Attach(root,CombatVisualPriority.SustainedBackground)==null)return null;
            Assets();var fx=root.AddComponent<SkillPerformanceVfx>();fx.owner=hero;fx.session=GameSession.Instance;fx.epoch=hero.CombatEpoch;fx.kind=type;fx.life=Mathf.Max(.8f,duration);fx.size=radius;
            fx.authority=follow;fx.bound=follow!=null;fx.model=hero.GetComponentInChildren<CombatModel>();fx.block=new MaterialPropertyBlock();
            fx.Build();return fx;
        }
        private void Add(int i,Mesh mesh,Vector3 scale,Vector3 at,Quaternion rotation,Color color,bool dark=false)
        {
            var part=new GameObject(dark?"Absorption depth core":"Turbulent volumetric layer");part.transform.SetParent(transform,false);part.transform.localPosition=at;part.transform.localScale=scale;part.transform.localRotation=rotation;
            part.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=part.AddComponent<MeshRenderer>();renderer.sharedMaterial=energy;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            parts[i]=part.transform;surfaces[i]=renderer;var props=new MaterialPropertyBlock();props.SetColor("_Tint",color);props.SetFloat("_DarkCore",dark?1:0);props.SetFloat("_Heat",dark?0:kind==SkillPerformanceBody.Meteor&&i==0?.18f:1);props.SetFloat("_Wispy",kind==SkillPerformanceBody.Tornado?1:0);renderer.SetPropertyBlock(props);
        }
        private void Build()
        {
            int count=kind==SkillPerformanceBody.ArrowVolley?(EffectPreferences.ReducedEffects?5:12):kind==SkillPerformanceBody.Meteor?3:EffectPreferences.ReducedEffects?3:5;
            parts=new Transform[count];surfaces=new Renderer[count];
            Color tint=kind==SkillPerformanceBody.Gravity?new Color(.58f,.22f,1f):kind==SkillPerformanceBody.Meteor||kind==SkillPerformanceBody.Fire?new Color(1,.32f,.045f):kind==SkillPerformanceBody.Poison?new Color(.3f,.8f,.1f):kind==SkillPerformanceBody.Frost||kind==SkillPerformanceBody.Lightning?new Color(.3f,.75f,1f):GameBalance.ClassColor(owner.HeroClass);
            for(int i=0;i<count;i++)
            {
                if(kind==SkillPerformanceBody.ArrowVolley)Add(i,AuthoredSpellBases.Load("Arrow"),new Vector3(.38f,1.15f,.38f),Vector3.zero,Quaternion.identity,new Color(1,.8f,.35f));
                else if(kind==SkillPerformanceBody.Meteor)Add(i,i==0?(AuthoredProjectileMeshes.Load("MeteorRock")??orb):AuthoredSpellBases.Load("Flame"),i==0?Vector3.one*2.1f:new Vector3(.8f,3f,.8f),Vector3.up*12,Quaternion.identity,i==0?new Color(.4f,.1f,.025f):tint);
                else if(kind==SkillPerformanceBody.Gravity&&i==0)Add(i,orb,Vector3.one*size*.38f,Vector3.up*1.4f,Quaternion.identity,tint,true);
                else if(kind==SkillPerformanceBody.Gravity)Add(i,orbit,new Vector3(size*(.32f+i*.13f),size*.3f,size*(.32f+i*.13f)),Vector3.up*1.4f,Quaternion.Euler(i%2==0?8:-12,i*49,0),tint);
                else if(kind==SkillPerformanceBody.Spirit)Add(i,orbit,new Vector3(size*.65f,size*.12f,size*.65f),Vector3.up*(1.1f+i*.14f),Quaternion.Euler(65+i*8,i*72,0),tint);
                else if(kind==SkillPerformanceBody.Lightning)Add(i,AuthoredSpellBases.Load("Lightning"),new Vector3(.5f,3.2f+i*.35f,.5f),new Vector3(Mathf.Cos(i*2.4f),.2f,Mathf.Sin(i*2.4f))*size*.55f,Quaternion.Euler(0,i*71,12),tint);
                else if(kind==SkillPerformanceBody.Fault)Add(i,AuthoredSpellBases.Load("Crystal"),new Vector3(.5f,2.3f,.5f),new Vector3((i-count*.5f)*.8f,0,0),Quaternion.Euler(0,0,(i-count*.5f)*18),tint);
                else Add(i,kind==SkillPerformanceBody.Tornado?(i<2?windWall:funnel):kind==SkillPerformanceBody.Frost?AuthoredSpellBases.Load("Crystal"):AuthoredSpellBases.Load(kind==SkillPerformanceBody.Poison?"Vine":"Flame"),
                    kind==SkillPerformanceBody.Tornado?new Vector3(size*(i==0?.64f:.70f),4.3f+(i%2)*.28f,size*(i==0?.64f:.70f)):new Vector3(.6f,1.7f+i*.35f,.6f),kind==SkillPerformanceBody.Tornado?Vector3.up*.12f:new Vector3(Mathf.Cos(i*2.4f),.12f,Mathf.Sin(i*2.4f))*size*.5f,Quaternion.Euler(0,i*120,0),tint);
            }
            var element=kind==SkillPerformanceBody.Lightning?ElementalCombatVfx.Element.Lightning:kind==SkillPerformanceBody.Meteor||kind==SkillPerformanceBody.Fire?ElementalCombatVfx.Element.Fire:kind==SkillPerformanceBody.Poison?ElementalCombatVfx.Element.Poison:ElementalCombatVfx.Element.Ice;
            particles=ElementalCombatVfx.Create(transform,"Field sparks / dust motes",element,EffectPreferences.ReducedEffects?12:35,Mathf.Min(size*.65f,3));
            if(particles!=null)
            {
                particles.useAutoRandomSeed=false;particles.randomSeed=127;var main=particles.main;main.maxParticles=EffectPreferences.ReducedEffects?36:96;main.startSize=new ParticleSystem.MinMaxCurve(.07f,.22f);
                var velocity=particles.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.Local;
                velocity.orbitalY=kind==SkillPerformanceBody.Gravity?-3:kind==SkillPerformanceBody.Tornado?5:1;
                velocity.radial=kind==SkillPerformanceBody.Gravity?-2f:kind==SkillPerformanceBody.Tornado?.7f:0;
                // Create supplies two-constant curves; reset every axis together before switching to constants.
                velocity.x=0f;velocity.z=0f;velocity.y=kind==SkillPerformanceBody.Tornado?1.8f:.25f;particles.Play();
            }
            if(!EffectPreferences.ReducedEffects&&lights<3)
            {glow=gameObject.AddComponent<Light>();glow.type=LightType.Point;glow.color=tint;glow.range=Mathf.Min(8,size+2);glow.intensity=1.3f;glow.shadows=LightShadows.None;ownsLight=true;lights++;}
        }
        internal void PrepareBeat(Vector3 at,float until)
        {
            landing=at;beatAge=0;beatDuration=Mathf.Max(.1f,until);launch=owner.transform.position+Vector3.up*1.2f;
            Vector3 muzzle;if(model!=null&&model.TryGetWeaponVisualAnchor(WeaponVisualAnchor.BowArrowRest,out muzzle))launch=muzzle;
        }
        internal void Beat()
        {
            if(particles!=null)particles.Emit(EffectPreferences.ReducedEffects?5:16);
        }
        private void LateUpdate()
        {
            if(retired)return;
            if(owner==null||owner.IsDead||session==null||session!=GameSession.Instance||!session.HasStarted||session.Player!=owner||owner.CombatEpoch!=epoch||session.CombatEffectsEnded||bound&&(authority==null||!authority.gameObject.activeInHierarchy)){Retire();return;}
            if(session.InputBlocked||Time.deltaTime<=0)return;
            age+=Time.deltaTime;beatAge+=Time.deltaTime;if(age>=life){Retire();return;}
            if(bound)transform.position=authority.position;
            float envelope=Mathf.Min(Mathf.Clamp01(age/.22f),Mathf.Clamp01((life-age)/.55f));
            for(int i=0;i<parts.Length;i++)
            {
                if(kind==SkillPerformanceBody.Tornado)parts[i].localRotation=Quaternion.Euler(0,age*(i%2==0?230:-165)+i*120,0);
                else if(kind==SkillPerformanceBody.Gravity)parts[i].Rotate(0,Time.deltaTime*(i%2==0?95:-75),0,Space.Self);
                else if(kind==SkillPerformanceBody.ArrowVolley)
                {
                    float t=Mathf.Clamp01((beatAge-i*.012f)/Mathf.Max(.1f,beatDuration-i*.012f));
                    Vector3 offset=new Vector3(Mathf.Cos(i*2.4f),0,Mathf.Sin(i*2.4f))*Mathf.Min(size*.32f,1.8f);
                    Vector3 point=Vector3.Lerp(launch,landing+offset,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*9;
                    Vector3 tangent=(landing+offset-launch)+Vector3.up*Mathf.Cos(t*Mathf.PI)*9*Mathf.PI;
                    parts[i].position=point;parts[i].rotation=Quaternion.FromToRotation(Vector3.up,tangent.normalized);
                }
                else if(kind==SkillPerformanceBody.Meteor)
                {
                    float t=Mathf.Clamp01(beatAge/Mathf.Max(.1f,beatDuration));Vector3 at=landing+new Vector3((1-t)*3,12*(1-t*t),-(1-t)*2);
                    parts[i].position=at+Vector3.up*(i*.65f);parts[i].rotation=Quaternion.Euler(i==0?age*95:15,age*65+i*90,20);
                }
                else if(kind!=SkillPerformanceBody.Fault)parts[i].localRotation=Quaternion.Euler(Mathf.Sin(age*2+i)*12,age*32+i*71,0);
                surfaces[i].GetPropertyBlock(block);block.SetFloat("_Opacity",envelope*(kind==SkillPerformanceBody.Tornado?(i<2?.52f:.23f):kind==SkillPerformanceBody.Gravity&&i>0?.68f:1));block.SetFloat("_Phase",age+i*.7f);surfaces[i].SetPropertyBlock(block);
            }
            if(glow!=null)glow.intensity=envelope*(1.1f+.3f*Mathf.Sin(age*11));
        }
        internal void Retire(){if(retired)return;retired=true;gameObject.SetActive(false);Destroy(gameObject);}
        private void OnDisable(){if(ownsLight){ownsLight=false;lights=Mathf.Max(0,lights-1);}}
    }
}
