using UnityEngine;
using UnityEngine.Rendering;

namespace Emberfall
{
    /// <summary>Visible loot remains session-owned until collected or settled on exit.</summary>
    public sealed class GroundLootPickup : MonoBehaviour
    {
        public const float PickupRadius = 2f;
        public const float LandingProtection = .6f;
        public const float RetryDelay = 1f;
        public string ItemId { get; private set; }
        public bool ReadyToCollect { get { return age >= LandingProtection && !retired; } }
        private GameSession session;
        private float age;
        private float retryTime;
        private bool retired;
        private Transform model;
        private Transform label;
        private Material bodyMaterial;
        private Material accentMaterial;
        private Material glowMaterial;
        private Font font;

        public void Initialize(GameSession owner, ItemData item)
        {
            session = owner;
            ItemId = item.id;
            EquipmentAppearance look = new EquipmentAppearance(item);
            Color color = look.Accent;
            bodyMaterial = new Material(Shader.Find("Standard")) { color = look.Metal, hideFlags = HideFlags.HideAndDontSave };
            bodyMaterial.EnableKeyword("_EMISSION");
            bodyMaterial.SetColor("_EmissionColor", color * (.12f + look.UpgradeRank * .035f));
            accentMaterial = new Material(Shader.Find("Standard")) { color = color, hideFlags = HideFlags.HideAndDontSave };
            accentMaterial.EnableKeyword("_EMISSION");
            accentMaterial.SetColor("_EmissionColor", look.Glow * (.2f + look.UpgradeRank * .07f));
            Shader glowShader = Shader.Find("Sprites/Default");
            if (glowShader == null) glowShader = Shader.Find("Unlit/Color");
            glowMaterial = new Material(glowShader) { color = color, hideFlags = HideFlags.HideAndDontSave };
            model = new GameObject("Equipment miniature").transform;
            model.SetParent(transform, false);
            model.localPosition = Vector3.up * .4f;
            float growth = 1f + (look.Tier - 1) * .14f;
            if (item.slot == ItemSlot.Weapon)
            {
                Part("Blade", PrimitiveType.Cube, new Vector3(0, .23f, 0), new Vector3(.1f + look.Tier * .025f, .67f * growth, .075f));
                Part("Blade ridge", PrimitiveType.Cube, new Vector3(0, .24f, .045f), new Vector3(.035f, .55f * growth, .02f), accentMaterial);
                Part("Guard", PrimitiveType.Cube, new Vector3(0, -.1f, 0), new Vector3((.39f + look.Tier * .055f), .085f, .12f), accentMaterial);
                Part("Grip", PrimitiveType.Cube, new Vector3(0, -.23f, 0), new Vector3(.075f, .23f, .09f));
                if (look.Tier >= 3)
                    for (int side = -1; side <= 1; side += 2)
                        Part("Blade wing", PrimitiveType.Cube, new Vector3(side * .12f, .4f, 0),
                            new Vector3(.08f, .31f, .07f), accentMaterial).localRotation = Quaternion.Euler(0, 0, side * 16f);
            }
            else if (item.slot == ItemSlot.Armor)
            {
                Part("Chest", PrimitiveType.Cube, Vector3.zero, new Vector3(.44f * growth, .53f * growth, .23f));
                Part("Chest crest", PrimitiveType.Cube, new Vector3(0, .08f, .14f),
                    new Vector3(.13f + look.Tier * .025f, .19f, .045f), accentMaterial).localRotation = Quaternion.Euler(0, 0, 45);
                for (int side = -1; side <= 1; side += 2)
                {
                    Part("Shoulder", PrimitiveType.Cube, new Vector3(side * .27f * growth, .17f, 0),
                        new Vector3(.2f + look.Tier * .025f, .2f, .25f), accentMaterial);
                    if (look.Tier >= 3)
                        Part("Shoulder horn", PrimitiveType.Capsule, new Vector3(side * .32f * growth, .34f, 0),
                            new Vector3(.08f, .2f, .08f));
                }
            }
            else
            {
                Part("Relic", PrimitiveType.Sphere, Vector3.zero, new Vector3(.37f * growth, .48f * growth, .3f * growth), accentMaterial);
                Part("Relic clasp", PrimitiveType.Cube, new Vector3(0, .3f * growth, 0), new Vector3(.12f, .15f, .1f));
                if (look.Tier >= 2)
                    for (int side = -1; side <= 1; side += 2)
                        Part("Relic wing", PrimitiveType.Cube, new Vector3(side * .28f * growth, 0, 0),
                            new Vector3(.2f, .08f, .11f), accentMaterial).localRotation = Quaternion.Euler(0, 0, side * 20f);
            }
            for (int i = 0; i < look.UpgradeRank; i++)
                Part("Enhancement rune", PrimitiveType.Sphere,
                    new Vector3((i % 5 - 2) * .13f, -.3f - i / 5 * .105f, .18f),
                    Vector3.one * .065f, accentMaterial);
            if (look.HasAura)
                for (int side = -1; side <= 1; side += 2)
                    Part("Orbit shard", PrimitiveType.Sphere, new Vector3(side * .43f, .27f, 0),
                        Vector3.one * (look.HasCrown ? .16f : .11f), accentMaterial);
            LineRenderer beam = Line("Rarity beam", false, 2, .09f + look.RarityRank * .02f + look.UpgradeRank * .004f);
            beam.SetPosition(0, new Vector3(0, .12f, 0));
            beam.SetPosition(1, new Vector3(0, 2.6f, 0));
            beam.startColor = new Color(1, 1, 1, .8f);
            beam.endColor = new Color(1, 1, 1, 0);
            LineRenderer ring = Line("Rarity ring", true, 40, .025f + look.UpgradeRank * .003f);
            for (int i = 0; i < 40; i++)
            {
                float angle = i * Mathf.PI * 2 / 40;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * (.4f + look.Tier * .04f), .105f,
                    Mathf.Sin(angle) * (.4f + look.Tier * .04f)));
            }
            font = GameFont.Shared;
            label = new GameObject("Item name").transform;
            label.SetParent(transform, false);
            label.localPosition = new Vector3(0, 1.25f, 0);
            TextMesh text = label.gameObject.AddComponent<TextMesh>();
            text.font = font;
            text.fontSize = 36;
            text.characterSize = .06f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.Lerp(color, Color.white, .35f);
            string itemName = string.IsNullOrEmpty(item.name) ? "未知装备" : item.name;
            text.text = (itemName.Length > 10 ? itemName.Substring(0, 10) : itemName) +
                (item.upgradeLevel > 0 ? " +" + item.upgradeLevel : "");
            if (font != null) label.GetComponent<Renderer>().sharedMaterial = font.material;
        }

        private Transform Part(string name, PrimitiveType shape, Vector3 position, Vector3 size, Material material = null)
        {
            GameObject part = ProceduralVisuals.Create(name,shape,material == null ? bodyMaterial : material);
            part.transform.SetParent(model, false);
            part.transform.localPosition = position;
            part.transform.localScale = size;
            Renderer renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material == null ? bodyMaterial : material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return part.transform;
        }

        private LineRenderer Line(string name, bool loop, int count, float width)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(transform, false);
            LineRenderer line = child.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = loop;
            line.positionCount = count;
            line.widthMultiplier = width;
            line.sharedMaterial = glowMaterial;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        private void Update()
        {
            if (retired || session == null || session.InputBlocked || !session.IsCurrentGroundLoot(this)) return;
            age += Time.deltaTime;
            retryTime = Mathf.Max(0, retryTime - Time.deltaTime);
            if (model != null)
            {
                model.localPosition = Vector3.up * (.4f + Mathf.Sin(age * 2.5f) * .055f);
                model.localRotation = Quaternion.Euler(0, age * 35f, -15);
            }
            if (!ReadyToCollect || retryTime > 0 || session.Player == null) return;
            Vector3 offset = session.Player.transform.position - transform.position;
            offset.y = 0;
            if (offset.sqrMagnitude <= PickupRadius * PickupRadius && !session.TryCollectGroundLoot(this)) retryTime = RetryDelay;
        }

        private void LateUpdate()
        {
            if (label != null && Camera.main != null) label.rotation = Camera.main.transform.rotation;
        }

        public void Retire()
        {
            if (retired) return;
            retired = true;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (bodyMaterial != null) Destroy(bodyMaterial);
            if (accentMaterial != null) Destroy(accentMaterial);
            if (glowMaterial != null) Destroy(glowMaterial);
            GameFont.Release(ref font);
        }
    }
}
