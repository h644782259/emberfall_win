using UnityEngine;

namespace Emberfall
{
    internal static class ElementalCombatVfx
    {
        internal enum Element { Fire, Lightning, Poison, Ice }
        private static readonly Texture2D[] detailTextures=new Texture2D[4];
        private static readonly Material[] particleMaterials=new Material[4];
        private static Mesh iceChips;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetTextures(){for(int i=0;i<detailTextures.Length;i++){if(detailTextures[i]!=null)Object.Destroy(detailTextures[i]);detailTextures[i]=null;if(particleMaterials[i]!=null)Object.Destroy(particleMaterials[i]);particleMaterials[i]=null;}if(iceChips!=null)Object.Destroy(iceChips);iceChips=null;}
        internal static System.Collections.IEnumerator Prewarm()
        {foreach(Element element in System.Enum.GetValues(typeof(Element))){DetailTexture(element);ParticleMaterial(element);yield return null;}}
        private static Texture2D DetailTexture(Element element)
        {
            int index=(int)element;if(detailTextures[index]!=null)return detailTextures[index];
            const int size=256;var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float u=(x+.5f)/size*2-1,v=(y+.5f)/size*2-1;
                float n=Mathf.PerlinNoise(u*5+13,v*5+29);
                float distance=Mathf.Sqrt(u*u+v*v),alpha;
                if(element==Element.Fire){float w=.68f*(1-(v+1)*.34f);float shape=Mathf.Sqrt(u*u/(w*w)+v*v);alpha=Mathf.Pow(Mathf.Clamp01(1-shape),1.2f)*Mathf.Clamp01(n*1.6f+.1f);}
                else if(element==Element.Poison){float edge=Mathf.Exp(-Mathf.Pow((distance-.62f)*13,2));alpha=(edge*.55f+Mathf.Sqrt(Mathf.Clamp01(1-distance*distance))*.6f)*Mathf.Clamp01((1-distance)*10);}
                else if(element==Element.Ice)alpha=Mathf.Pow(Mathf.Clamp01(1-distance),1.35f)*(.65f+.35f*n);
                else alpha=Mathf.Pow(Mathf.Clamp01(1-distance),3)+Mathf.Exp(-Mathf.Abs(u)*55)*Mathf.Clamp01(1-Mathf.Abs(v))*.35f;
                float light=element==Element.Poison?Mathf.Clamp01(.42f+(.4f-u)*.32f+(.4f-v)*.2f+Mathf.Exp(-((u+.28f)*(u+.28f)+(v-.3f)*(v-.3f))*90)*.7f):1;
                pixels[y*size+x]=new Color(light,light,light,Mathf.Clamp01(alpha));
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,true){name="Element particle detail / "+element,filterMode=FilterMode.Trilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
            texture.SetPixels(pixels);texture.Apply(true,true);detailTextures[index]=texture;return texture;
        }


        public static void Area(Transform parent, float radius, Element element)
        {
            // The readable field owns its own lease; optional particles must not gate it.
            if(element!=Element.Ice)ElementalFieldVisual.Spawn(parent, element, radius, false,CombatVisualPriority.SustainedBackground);
            ParticleSystem particles = Create(parent, element == Element.Fire ? "Rising Flames" :
                element == Element.Poison ? "Poison Bubbles" : element==Element.Ice?"Frost Mist":"Storm Sparks", element,
                Mathf.Clamp(radius * radius * 3f, 12f, 52f), radius);
            if(particles==null)return;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius * .82f;
            shape.radiusThickness = 1f;shape.rotation=new Vector3(90,0,0);
            particles.transform.localPosition = Vector3.up * .16f;
            particles.gameObject.AddComponent<CoveredAreaParticles>();
            particles.Play();
        }

        public static void OnEnemy(EnemyController enemy, Element element, float duration)
        {
            if (enemy == null || enemy.IsDead) return;
            ElementalEnemyAura aura = enemy.GetComponent<ElementalEnemyAura>();
            if (aura == null) aura = enemy.gameObject.AddComponent<ElementalEnemyAura>();
            aura.BindStatus(enemy);
            aura.Refresh(element, duration, enemy.IsBoss ? 1.7f : enemy.Kind == EnemyKind.Slime ? .65f : 1.2f);
        }

        internal static void ClearFire(EnemyController enemy)
        {var aura=enemy==null?null:enemy.GetComponent<ElementalEnemyAura>();if(aura!=null)aura.ClearFire();}

        internal static void ClearPoison(EnemyController enemy)
        {var aura=enemy==null?null:enemy.GetComponent<ElementalEnemyAura>();if(aura!=null)aura.ClearPoison();}

        internal static ParticleSystem Create(Transform parent, string name, Element element, float rate, float radius)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            if(!DecorationLease.Attach(obj,0)){Object.Destroy(obj);return null;}
            ParticleSystem particles = obj.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = element == Element.Lightning ? new ParticleSystem.MinMaxCurve(.16f) : new ParticleSystem.MinMaxCurve(.7f,element==Element.Poison?1.5f:1.1f);
            main.startSpeed = element == Element.Lightning ? 2.4f : element == Element.Poison ? .8f : 1.7f;
            main.startSize = element == Element.Lightning ? new ParticleSystem.MinMaxCurve(.11f) : new ParticleSystem.MinMaxCurve(element==Element.Poison?.25f:.45f,element==Element.Ice?1.25f:.85f);
            main.maxParticles = DecorationBudget.Particles(EffectPreferences.EffectsScale);
            main.gravityModifier = element==Element.Ice?.04f:-.08f;
            main.startRotation = new ParticleSystem.MinMaxCurve(-.4f,.4f);
            main.startColor = element == Element.Fire ? new Color(1f, .46f, .08f, .85f) :
                element == Element.Poison ? new Color(.48f, 1f, .22f, .7f) : new Color(.56f, .88f, 1f, .9f);
            var colorLife=particles.colorOverLifetime;colorLife.enabled=true;
            Color core=element==Element.Fire?new Color(1,.97f,.65f):element==Element.Poison?new Color(.85f,1,.3f):new Color(.85f,.97f,1);
            Color middle=element==Element.Fire?new Color(1,.46f,.09f):element==Element.Poison?new Color(.3f,.76f,.18f):element==Element.Ice?new Color(.3f,.75f,1):new Color(.45f,.6f,1);
            Color end=element==Element.Fire?new Color(.35f,.045f,.018f):element==Element.Poison?new Color(.12f,.24f,.04f):element==Element.Ice?new Color(.2f,.48f,.85f):new Color(.29f,.12f,.65f);
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(core,0),new GradientColorKey(middle,.35f),new GradientColorKey(end,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.9f,.12f),new GradientAlphaKey(.6f,.55f),new GradientAlphaKey(0,1)});
            colorLife.color=new ParticleSystem.MinMaxGradient(gradient);main.startColor=Color.white;
            var emission = particles.emission;
            emission.rateOverTime = rate * EffectPreferences.EffectsScale;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, .35f), new Keyframe(.3f, 1f), new Keyframe(1f, 0f)));
            ParticleSystemRenderer renderer = obj.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial=ParticleMaterial(element);
            if(element!=Element.Lightning)
            {
                var noise=particles.noise;noise.enabled=true;noise.strength=element==Element.Fire?.55f:.25f;noise.frequency=.65f;noise.scrollSpeed=.6f;noise.octaveCount=1;noise.quality=ParticleSystemNoiseQuality.Low;
                var velocity=particles.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;
                velocity.x=new ParticleSystem.MinMaxCurve(-.35f,.35f);velocity.z=new ParticleSystem.MinMaxCurve(-.35f,.35f);velocity.y=new ParticleSystem.MinMaxCurve(element==Element.Fire?1.1f:.35f,element==Element.Fire?2.4f:.9f);
                var spin=particles.rotationOverLifetime;spin.enabled=true;spin.z=new ParticleSystem.MinMaxCurve(-1.2f,1.2f);
                size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.25f),new Keyframe(.3f,.8f),new Keyframe(.8f,element==Element.Poison?1.3f:1.1f),new Keyframe(1,0)));
            }
            return particles;
        }

        private static Material ParticleMaterial(Element element)
        {
            int index=(int)element;if(particleMaterials[index]!=null)return particleMaterials[index];
            Shader shader=Shader.Find("Particles/Standard Unlit");if(shader==null)shader=Shader.Find("Sprites/Default");
            var material=new Material(shader){name="Shared elemental particles / "+element,mainTexture=DetailTexture(element),hideFlags=HideFlags.HideAndDontSave};
            material.SetFloat("_Mode",2);material.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);material.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite",0);material.EnableKeyword("_ALPHABLEND_ON");material.renderQueue=3000;particleMaterials[index]=material;return material;
        }
        private static Mesh IceChips()
        {
            if(iceChips!=null)return iceChips;
            iceChips=new Mesh{name="Shared ice fragments"};
            iceChips.vertices=new[]{new Vector3(0,1,0),new Vector3(0,-.5f,0),new Vector3(.5f,0,0),new Vector3(0,0,.5f),new Vector3(-.5f,0,0),new Vector3(0,0,-.5f)};
            iceChips.triangles=new[]{0,3,2,0,4,3,0,5,4,0,2,5,1,2,3,1,3,4,1,4,5,1,5,2};
            iceChips.uv=new[]{new Vector2(.5f,1),new Vector2(.5f,0),new Vector2(1,.5f),new Vector2(.5f,1),new Vector2(0,.5f),new Vector2(.5f,0)};
            iceChips.RecalculateNormals();iceChips.RecalculateBounds();return iceChips;
        }
        internal static void Burst(PlayerController hero,Vector3 at,float radius,Element element)
        {
            if(hero==null||hero.IsDead)return;
            var particles=Create(hero.transform,"Elemental impact / "+element,element,0,Mathf.Min(radius*.45f,2.5f));if(particles==null)return;
            particles.transform.SetParent(null,true);particles.transform.position=at+Vector3.up*.18f;
            var main=particles.main;main.loop=false;main.startSpeed=new ParticleSystem.MinMaxCurve(.6f,2.4f);main.startLifetime=new ParticleSystem.MinMaxCurve(.6f,1.2f);
            var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Hemisphere;
            if(element==Element.Ice)
            {
                var renderer=particles.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Mesh;renderer.mesh=IceChips();
                main.startSize=new ParticleSystem.MinMaxCurve(.18f,.5f);main.gravityModifier=.45f;main.startRotation3D=true;main.startRotationX=new ParticleSystem.MinMaxCurve(0,6.28f);main.startRotationY=new ParticleSystem.MinMaxCurve(0,6.28f);
            }
            particles.gameObject.AddComponent<CoveredAreaParticles>();
            particles.gameObject.AddComponent<ElementalBurstLifetime>().Initialize(hero);
            particles.Play();particles.Emit(EffectPreferences.ReducedEffects?10:MobileControls.Active?22:36);
        }

        public static void Lightning(Vector3 from, Vector3 to)
        {
            GameObject obj = new GameObject("Lightning Fork");
            if(!DecorationLease.Attach(obj,1)){Object.Destroy(obj);return;}
            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            int points=EffectPreferences.ReducedEffects?7:13;
            line.positionCount = points;
            line.widthMultiplier = .13f;
            line.numCornerVertices=4;line.numCapVertices=4;
            line.sharedMaterial = CombatFx.NewGlow();
            Color color = new Color(.55f, .89f, 1f, .95f);
            line.startColor = line.endColor = color;
            Vector3 tangent = Vector3.Cross((to - from).normalized, Vector3.up);
            if (tangent.sqrMagnitude < .01f) tangent = Vector3.right;
            for (int i = 0; i < points; i++)
            {
                float t = i / (float)(points-1);
                line.SetPosition(i, Vector3.Lerp(from, to, t) + tangent * (i == 0 || i == points-1 ? 0 : Random.Range(-.3f, .3f)));
            }
            if(!EffectPreferences.ReducedEffects)
            {
                var coreObject=new GameObject("Lightning white core");coreObject.transform.SetParent(obj.transform,false);
                var core=coreObject.AddComponent<LineRenderer>();core.useWorldSpace=true;core.positionCount=points;
                for(int i=0;i<points;i++)core.SetPosition(i,line.GetPosition(i));
                core.widthMultiplier=.035f;core.numCornerVertices=4;core.numCapVertices=4;core.sharedMaterial=CombatFx.NewGlow();core.sortingOrder=line.sortingOrder+1;
                coreObject.AddComponent<FadingCombatEffect>().Setup(core,new Color(.92f,.96f,1,1),1,.14f,false);
            }
            obj.AddComponent<FadingCombatEffect>().Setup(line, color, 1f, .18f, false);
        }
    }

    internal sealed class ElementalBurstLifetime : MonoBehaviour
    {
        private PlayerController owner;private int epoch;private float age;
        internal void Initialize(PlayerController hero){owner=hero;epoch=hero.CombatEpoch;}
        private void Update(){if(owner==null||owner.IsDead||owner.CombatEpoch!=epoch){Destroy(gameObject);return;}age+=Time.deltaTime;if(age>1.6f)Destroy(gameObject);}
    }

    internal sealed class ElementalEnemyAura : MonoBehaviour
    {
        private ParticleSystem fire, poison;
        private ElementalFieldVisual fireShape, poisonShape;
        private float fireUntil, poisonUntil;
        private EnemyController statusOwner;
        private bool statusBound;
        internal void BindStatus(EnemyController enemy){statusOwner=enemy;statusBound=true;}

        public void Refresh(ElementalCombatVfx.Element element, float duration, float height)
        {
            if (element == ElementalCombatVfx.Element.Lightning) return;
            bool burning = element == ElementalCombatVfx.Element.Fire;
            // Siblings under the enemy: retiring a decoration can never retire its main shape.
            ElementalFieldVisual activeShape = burning ? fireShape : poisonShape;
            if (activeShape == null)
            {
                activeShape = ElementalFieldVisual.Spawn(transform, element, .48f, true,CombatVisualPriority.SustainedBackground);
                if (activeShape != null) activeShape.transform.localPosition = Vector3.up * height;
                if (burning) fireShape = activeShape; else poisonShape = activeShape;
            }
            if (activeShape != null) activeShape.gameObject.SetActive(true);
            if (burning) fireUntil = Mathf.Max(fireUntil, Time.time + duration);
            else poisonUntil = Mathf.Max(poisonUntil, Time.time + duration);
            ParticleSystem particles = burning ? fire : poison;
            if (particles == null)
            {
                particles = ElementalCombatVfx.Create(transform, burning ? "Burning Body" : "Poisoned Body",
                    element, burning ? 27f : 12f, .35f);
                if (particles == null) return;
                particles.transform.localPosition = Vector3.up * height;
                if (burning) fire = particles; else poison = particles;
            }
            particles.gameObject.SetActive(true);
            if (particles.gameObject.activeInHierarchy && !particles.isPlaying) particles.Play();
        }

        internal void ClearFire()
        {fireUntil=Time.time;if(fire!=null){fire.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);fire.gameObject.SetActive(false);}if(fireShape!=null)fireShape.gameObject.SetActive(false);}

        // Retire only the sustained poison channel synchronously. Keep its reusable
        // components alive so same-frame reapplication cannot inherit a queued Destroy.
        internal void ClearPoison()
        {poisonUntil=Time.time;if(poison!=null){poison.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);poison.gameObject.SetActive(false);}if(poisonShape!=null)poisonShape.gameObject.SetActive(false);}

        private void Update()
        {
            // Status schedules own pause and expiry. Wall-clock duration is only a
            // fallback for unbound decorative callers, never a second gameplay clock.
            if(statusBound)
            {
                if(statusOwner==null||statusOwner.StatusEffects==null){ClearFire();ClearPoison();return;}
                if(statusOwner.IsDead||!statusOwner.StatusEffects.IsBurning)ClearFire();
                if(statusOwner.IsDead||statusOwner.StatusEffects.PoisonStacks<=0)ClearPoison();
                return;
            }
            if (fire != null && Time.time >= fireUntil && fire.isEmitting) fire.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (poison != null && Time.time >= poisonUntil && poison.isEmitting) poison.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if(fire!=null&&Time.time>=fireUntil)fire.gameObject.SetActive(false);
            if(poison!=null&&Time.time>=poisonUntil)poison.gameObject.SetActive(false);
            if (fireShape != null && Time.time >= fireUntil) fireShape.gameObject.SetActive(false);
            if (poisonShape != null && Time.time >= poisonUntil) poisonShape.gameObject.SetActive(false);
        }
    }

}
