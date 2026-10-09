using UnityEngine;

namespace Emberfall
{
    internal static class ElementalCombatVfx
    {
        internal enum Element { Fire, Lightning, Poison }
        private static readonly Texture2D[] detailTextures=new Texture2D[3];
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetTextures(){for(int i=0;i<detailTextures.Length;i++){if(detailTextures[i]!=null)Object.Destroy(detailTextures[i]);detailTextures[i]=null;}}
        internal static System.Collections.IEnumerator Prewarm()
        {foreach(Element element in System.Enum.GetValues(typeof(Element))){DetailTexture(element);yield return null;}}
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
                else if(element==Element.Poison){float edge=Mathf.Exp(-Mathf.Pow((distance-.62f)*13,2));alpha=(edge*.7f+Mathf.Clamp01(1-distance)*.2f)*Mathf.Clamp01((1-distance)*8);}
                else alpha=Mathf.Pow(Mathf.Clamp01(1-distance),3)+Mathf.Exp(-Mathf.Abs(u)*55)*Mathf.Clamp01(1-Mathf.Abs(v))*.35f;
                pixels[y*size+x]=new Color(1,1,1,Mathf.Clamp01(alpha));
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,true){name="Element particle detail / "+element,filterMode=FilterMode.Trilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
            texture.SetPixels(pixels);texture.Apply(true,true);detailTextures[index]=texture;return texture;
        }


        public static void Area(Transform parent, float radius, Element element)
        {
            // The readable field owns its own lease; optional particles must not gate it.
            ElementalFieldVisual.Spawn(parent, element, radius, false,CombatVisualPriority.SustainedBackground);
            ParticleSystem particles = Create(parent, element == Element.Fire ? "Rising Flames" :
                element == Element.Poison ? "Poison Bubbles" : "Storm Sparks", element,
                Mathf.Clamp(radius * radius * 1.8f, 8f, 45f), radius);
            if(particles==null)return;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius * .82f;
            shape.radiusThickness = 1f;
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
            main.startLifetime = element == Element.Lightning ? .16f : element == Element.Poison ? .85f : .48f;
            main.startSpeed = element == Element.Lightning ? 2.4f : element == Element.Poison ? .8f : 1.7f;
            main.startSize = element == Element.Poison ? .2f : element == Element.Lightning ? .11f : .27f;
            main.maxParticles = DecorationBudget.Particles(EffectPreferences.EffectsScale);
            main.gravityModifier = -.08f;
            main.startRotation = new ParticleSystem.MinMaxCurve(-.4f,.4f);
            main.startColor = element == Element.Fire ? new Color(1f, .46f, .08f, .85f) :
                element == Element.Poison ? new Color(.48f, 1f, .22f, .7f) : new Color(.56f, .88f, 1f, .9f);
            var colorLife=particles.colorOverLifetime;colorLife.enabled=true;
            Color core=element==Element.Fire?new Color(1,.97f,.65f):element==Element.Poison?new Color(.85f,1,.3f):new Color(.95f,.97f,1);
            Color middle=element==Element.Fire?new Color(1,.46f,.09f):element==Element.Poison?new Color(.3f,.76f,.18f):new Color(.45f,.6f,1);
            Color end=element==Element.Fire?new Color(.35f,.045f,.018f):element==Element.Poison?new Color(.12f,.24f,.04f):new Color(.29f,.12f,.65f);
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
            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            renderer.sharedMaterial = new Material(shader);
            renderer.sharedMaterial.mainTexture=DetailTexture(element);
            renderer.sharedMaterial.SetFloat("_Mode",2);
            renderer.sharedMaterial.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            renderer.sharedMaterial.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            renderer.sharedMaterial.SetInt("_ZWrite",0);renderer.sharedMaterial.EnableKeyword("_ALPHABLEND_ON");renderer.sharedMaterial.renderQueue=3000;
            obj.AddComponent<OwnedParticleMaterial>().Value = renderer.sharedMaterial;
            return particles;
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

    internal sealed class OwnedParticleMaterial : MonoBehaviour
    {
        public Material Value;
        private void OnDestroy() { if (Value != null) Destroy(Value); }
    }
}
