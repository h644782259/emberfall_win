using UnityEngine;

namespace Emberfall
{
    // The kill is resolved immediately, while the visible body remains for a short time.
    internal sealed class EnemyDeathDissolve : MonoBehaviour
    {
        private const float FallTime = .48f;
        private const float RestTime = 1.25f;
        private const float FadeTime = 1.45f;
        private CombatModel model;
        private Transform visual;
        private Quaternion startRotation;
        private Vector3 startPosition, startScale;
        private ParticleSystem ash;
        private Transform[] motes;
        private Material moteMaterial;
        private float age;
        private GameSession session;private PlayerController owner;private int epoch;
        private static readonly System.Collections.Generic.List<EnemyDeathDissolve> active=new System.Collections.Generic.List<EnemyDeathDissolve>();
        internal static bool IsPresenting(GameSession game)
        {foreach(var v in active)if(v!=null&&v.boss&&v.gameObject.activeInHierarchy&&v.session==game&&v.owner!=null&&game.Player==v.owner&&v.owner.CombatEpoch==v.epoch)return true;return false;}
        private void OnDisable(){active.Remove(this);}
        private bool slime, boss;

        public void Initialize(CombatModel body, bool isSlime, bool boss)
        {
            model = body;
            visual = body.transform;
            slime = isSlime;
            this.boss = boss;
            session=GameSession.Instance;owner=session==null?null:session.Player;epoch=owner==null?0:owner.CombatEpoch;if(boss)active.Add(this);
            model.BeginDeath(); // Finalize the displayed pose before capturing death ownership.
            startRotation = visual.localRotation;
            startPosition = visual.localPosition;
            startScale = visual.localScale;
            ash = ElementalCombatVfx.Create(transform, "Dissolving Ash", ElementalCombatVfx.Element.Fire,
                boss ? 35f : 16f, boss ? 1.3f : .55f);
            if(ash!=null){
            ash.transform.localPosition = Vector3.up * (boss ? 1.4f : isSlime ? .35f : .8f);
            var main = ash.main;
            main.startColor = new Color(.66f, .75f, .8f, .5f);
            main.startSpeed = .65f;
            var emission = ash.emission;
            emission.enabled = false;
            }
            moteMaterial = new Material(Shader.Find("Unlit/Color"));
            moteMaterial.color = new Color(.7f, .78f, .84f);
            var moteRoot=new GameObject("Bounded death motes");moteRoot.transform.SetParent(transform,false);
            bool allowed=DecorationLease.Attach(moteRoot,2);
            motes = new Transform[allowed?DecorationBudget.DeathMotes(boss,EffectPreferences.ReducedEffects):0];
            for (int i = 0; i < motes.Length; i++)
            {
                GameObject mote = ProceduralVisuals.Create("Evaporating Ash",PrimitiveType.Sphere,moteMaterial);
                mote.transform.SetParent(moteRoot.transform, false);
                mote.GetComponent<Renderer>().sharedMaterial = moteMaterial;
                mote.SetActive(false);
                motes[i] = mote.transform;
            }
        }

        private void Update()
        {
            if (model == null) { Destroy(gameObject); return; }
            if(session!=null&&(owner==null||session.Player!=owner||owner.CombatEpoch!=epoch)){Destroy(gameObject);return;}
            if(session!=null&&(session.Paused||session.BackgroundPaused))return;
            age += Time.unscaledDeltaTime;
            float stagger=boss?.25f:0f,fallDuration=boss?.8f:FallTime,restDuration=boss?.12f:RestTime;
            float fall = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((age-stagger) / fallDuration));
            visual.localRotation = Quaternion.Slerp(startRotation,
                startRotation * Quaternion.Euler(slime ? 0f : 9f, 0f, slime ? 0f : 84f), fall);
            if(boss&&age<stagger)visual.localRotation=startRotation*Quaternion.Euler(0,0,Mathf.Sin(age*23)*5*(1-age/stagger));
            visual.localPosition = startPosition + Vector3.down * (slime ? .3f : boss?.22f:.08f) * fall;
            Vector3 fallenScale = slime ? Vector3.Scale(startScale,
                new Vector3(1f + .3f * fall, 1f - .55f * fall, 1f + .3f * fall)) : startScale;
            visual.localScale = fallenScale;
            float fade = Mathf.Clamp01((age - stagger - fallDuration - restDuration) / (boss?.95f:FadeTime));
            if (fade > 0f)
            {
                if (ash != null && !ash.isPlaying)
                {
                    var emission = ash.emission;
                    emission.enabled = true;
                    ash.Play();
                }
                model.SetDeathOpacity(1f - Mathf.SmoothStep(0f, 1f, fade));
                visual.localScale = fallenScale * Mathf.Lerp(1f, .78f, fade);
                visual.localPosition += Vector3.down * (.25f * fade);
                for (int i = 0; i < motes.Length; i++)
                {
                    Transform mote = motes[i];
                    if (!mote.gameObject.activeSelf) mote.gameObject.SetActive(true);
                    float angle = i * 2.39996f + fade * 2.4f;
                    float distance = (boss ? 1f : .48f) * (.4f + fade * 1.2f);
                    mote.localPosition = new Vector3(Mathf.Cos(angle) * distance,
                        (boss ? 1.3f : slime ? .4f : .9f) + fade * (1f + i % 3 * .25f),
                        Mathf.Sin(angle) * distance);
                    mote.localScale = Vector3.one * (.11f * (1f - fade) + .025f);
                }
            }
            if (fade >= 1f) Destroy(gameObject);
        }

        private void OnDestroy() { if (moteMaterial != null) Destroy(moteMaterial); }
    }
}
