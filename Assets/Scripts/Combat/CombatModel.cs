using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    // Each model owns its palette. Materials are reused across all of its parts.
    public sealed partial class CombatModel : MonoBehaviour
    {
        private struct SurfaceKey
        {
            public Color Color;
            public VisualSurface Surface;
            public SurfaceKey(Color color, VisualSurface surface) { Color = color; Surface = surface; }
        }
        private readonly Dictionary<SurfaceKey, Material> palette = new Dictionary<SurfaceKey, Material>();
        private TailoredCloth tailoredCloth;
        private LargeBossRig largeBossRig;
        private bool articulatedEnemy, quadruped;
        private Transform tailRig;
        private readonly Transform[] paws = new Transform[4];
        private Vector3[] pawOrigins;
        private float smoothedSpeed;
        private Transform leftLeg, rightLeg, leftArm, rightArm, body, decoration;
        private Transform spine, pelvis, headRig, leftElbow, rightElbow, leftKnee, rightKnee, cloak;
        private Transform swordRig, staffRig, bowRig, arrowRig, castingOrb;
        private Transform fashionWings, fashionWeapon;
        private string fashionWingsId, fashionWeaponId;
        private Transform equipmentWeapon, equipmentArmor, equipmentRelic;
        private Transform equipmentLeftShoulder, equipmentRightShoulder, equipmentHead;
        private string equipmentWeaponKey, equipmentArmorKey, equipmentRelicKey;
        private LineRenderer bowstring;
        private HeroClass heroClass;
        private bool isHero, actionBasic;
        private int actionSkill, swingCount;
        private int weaponActionId,lastRibbonAction;
        public int WeaponActionId {get{return weaponActionId;}}
        public bool SwordActionActive {get{return isHero&&heroClass==HeroClass.Vanguard&&actionDuration>0&&actionAge<actionDuration;}}
        public bool TryClaimSwordRibbon(int identity)
        {if(!SwordActionActive||identity!=weaponActionId||lastRibbonAction==identity)return false;lastRibbonAction=identity;return true;}
        private float actionAge, actionDuration, gaitPhase;
        private int actionStartedFrame;
        private bool slime, floating, treantCompanion;
        private Transform wolfJaw;
        private Vector3 companionBodyScale;
        private float phase;
        private bool isolatedPreview;
        private float previewTime;
        private float recoilStarted = -10f, recoilStrength;
        private Vector3 recoilDirection;
        private EnemyController enemyOwner;
        private Quaternion bodyRestRotation = Quaternion.identity;
        private bool dying;
        private float deathOpacity = 1f;

        public void BeginDeath()
        {
            if (dying) return;
            RestoreKnockdownForDeath();
            dying = true;
            SetBlenderPilotVisible(false);
            foreach (Material material in palette.Values)
            {
                if (material == null) continue;
                material.SetFloat("_Mode", 3f);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
            }
        }

        public void SetDeathOpacity(float opacity) { deathOpacity = Mathf.Clamp01(opacity); }

        public void Recoil(Vector3 worldDirection, float strength)
        {
            recoilStarted = Time.time;
            recoilStrength = Mathf.Clamp(strength, .35f, 1.6f);
            recoilDirection = transform.parent.InverseTransformDirection(worldDirection.normalized);
        }

        private void ApplyRecoil()
        {
            float age = Time.time - recoilStarted;
            float recovery = Mathf.Clamp01(1f - age / .22f);
            float impulse = recovery * recovery * recoilStrength;
            transform.localPosition += recoilDirection * (.12f * impulse);
            if (body != null) body.localRotation *= Quaternion.Euler(recoilDirection.z * 9f * impulse, 0, -recoilDirection.x * 9f * impulse);
        }

        private void LateUpdate()
        {
            if (isHero) { SampleVanguardDeath(); return; }
            if (dying)
            {
                foreach (var pair in palette)
                    if (pair.Value != null)
                    {
                        Color color = pair.Key.Color;
                        color.a = deathOpacity;
                        pair.Value.color = color;
                    }
                return;
            }
            float flash = Mathf.Clamp01(1f - (Time.time - recoilStarted) / .11f) * .35f * EffectPreferences.EffectsScale;
            foreach (var pair in palette)
                if (pair.Value != null) pair.Value.color = Color.Lerp(pair.Key.Color, new Color(1f, .92f, .73f), flash);
        }

        public static CombatModel Hero(Transform parent, HeroClass hero)
        {
            CombatModel model = Create(parent);
            model.BuildHero(hero);
            model.EnhanceHero(hero);
            if (hero == HeroClass.Summoner) model.SummonerCrown();
            model.BuildClassCostume();
            model.CaptureBaseCostume();
            model.ApplyWeaponArt(null);
            model.ConfigureBlenderPilot();
            model.ConfigureVanguardArt();
            return model;
        }

        public void ApplyFashion(FashionData wings, FashionData weapon)
        {
            pilotHasFashion = wings != null || weapon != null;
            if(pilotHasFashion)SetBlenderPilotVisible(false);
            string wingId = wings == null ? null : wings.id+":"+wings.upgradeRank+":"+(int)wings.VisualRarity;
            string weaponId = weapon == null ? null : weapon.id+":"+weapon.upgradeRank+":"+(int)weapon.VisualRarity;
            if (wingId == fashionWingsId && weaponId == fashionWeaponId) return;
            InvalidatePilotRendererGroup(); // Component-only replacements must invalidate all display owners.
            fashionWingsId = wingId;
            fashionWeaponId = weaponId;
            activeWeaponFashion = weapon;
            if (fashionWings != null) { fashionWings.gameObject.SetActive(false); Destroy(fashionWings.gameObject); }
            if (fashionWeapon != null) { fashionWeapon.gameObject.SetActive(false); Destroy(fashionWeapon.gameObject); }
            fashionWings = fashionWeapon = null;
            if (wings != null && spine != null)
            {
                fashionWings = new GameObject("Fashion Wings").transform;
                fashionWings.SetParent(spine, false);
                fashionWings.localPosition = new Vector3(0, .26f, -.43f);
                Color color = GameBalance.RarityColor(wings.AppearanceRarity);
                BuildFashionWingShape(wings,color);
            }
            Transform weaponAnchor = heroClass == HeroClass.Ranger ? bowRig :
                heroClass == HeroClass.Vanguard ? swordRig : staffRig;
            if (weapon != null && (weaponAnchor != null || rightElbow != null))
            {
                fashionWeapon = new GameObject("Fashion Weapon").transform;
                fashionWeapon.SetParent(weaponAnchor == null ? rightElbow : weaponAnchor, false);
                fashionWeapon.localPosition = weaponAnchor == null ? new Vector3(0, -.26f, .17f) : Vector3.zero;
                BuildWeaponFashionShape(weapon);
            }
            ApplyWeaponFashionArt();
        }

        public void ApplyEquipment(ItemData weapon, ItemData armor, ItemData relic)
        {
            if (!isHero || spine == null) return;
            InvalidatePilotRendererGroup(); // Includes renderer replacement without a Transform change.
            bool resumePilot=pilotVisible;
            SetBlenderPilotVisible(false); // Restore owned renderer states before equipment builders edit them.
            pilotHasGear = !PilotStarterCompatible(weapon,ItemSlot.Weapon) || !PilotStarterCompatible(armor,ItemSlot.Armor) || !PilotStarterCompatible(relic,ItemSlot.Relic);

            string weaponKey = EquipmentKey(weapon);
            string armorKey = EquipmentKey(armor);
            string relicKey = EquipmentKey(relic);
            if (weaponKey != equipmentWeaponKey)
            {
                equipmentWeaponKey = weaponKey;
                if (equipmentWeapon != null) { equipmentWeapon.gameObject.SetActive(false); Destroy(equipmentWeapon.gameObject); }
                equipmentWeapon = null;
                SetBaseWeaponVisible(weapon == null);
                weaponStructure = new WeaponStructure(weapon == null ? 0 : new EquipmentAppearance(weapon).Tier,heroClass==HeroClass.Arcanist);
                if (bowstring != null)
                {
                    bowstring.SetPosition(0, WeaponAnchorLocal(WeaponVisualAnchor.BowUpperTip));
                    bowstring.SetPosition(2, WeaponAnchorLocal(WeaponVisualAnchor.BowLowerTip));
                }
                if (weapon != null) BuildEquipmentWeapon(new EquipmentAppearance(weapon));
                RefreshWeaponFashion();
            }
            if (armorKey != equipmentArmorKey)
            {
                equipmentArmorKey = armorKey;
                if (equipmentArmor != null) { equipmentArmor.gameObject.SetActive(false); Destroy(equipmentArmor.gameObject); }
                if (equipmentLeftShoulder != null) { equipmentLeftShoulder.gameObject.SetActive(false); Destroy(equipmentLeftShoulder.gameObject); }
                if (equipmentRightShoulder != null) { equipmentRightShoulder.gameObject.SetActive(false); Destroy(equipmentRightShoulder.gameObject); }
                if(equipmentHead!=null){equipmentHead.gameObject.SetActive(false);Destroy(equipmentHead.gameObject);}
                equipmentHead=null;
                equipmentArmor = null;
                equipmentLeftShoulder = equipmentRightShoulder = null;
                SetBaseCostumeVisible(armor == null);
                if (armor != null) BuildEquipmentArmor(new EquipmentAppearance(armor));
            }
            if (relicKey != equipmentRelicKey)
            {
                equipmentRelicKey = relicKey;
                if (equipmentRelic != null) { equipmentRelic.gameObject.SetActive(false); Destroy(equipmentRelic.gameObject); }
                equipmentRelic = null;
                if (relic != null) BuildEquipmentRelic(new EquipmentAppearance(relic));
            }
            ApplyWeaponArt(weapon);
            if(resumePilot&&!pilotHasGear)SetBlenderPilotVisible(true); // Capture the rebuilt procedural state, then hide it.
        }

        private static string EquipmentKey(ItemData item)
        {
            return item == null ? null : item.id + "/" + item.level + "/" + (int)item.rarity + "/" + item.upgradeLevel;
        }

        private static void SetVisible(Transform parent, string name, bool visible)
        {
            if (parent == null) return;
            foreach (Transform child in parent)
                if (child.name == name)
                {
                    Renderer renderer = child.GetComponent<Renderer>();
                    if (renderer != null) renderer.enabled = visible;
                }
        }

        private void SetBaseWeaponVisible(bool visible)
        {
            if (swordRig != null)
                foreach (string name in new[] { "Wrapped Hilt", "Gold Pommel", "Crossguard", "Forged Sword", "Blade Fuller" })
                    SetVisible(swordRig, name, visible);
            if (staffRig != null)
                foreach (string name in new[] { "Staff", "Staff Gold", "Arcane Crystal", "Staff Crystal Crown" })
                    SetVisible(staffRig, name, visible);
            if (bowRig != null)
                foreach (string name in new[] { "Bow Limb", "Bow Grip" })
                    SetVisible(bowRig, name, visible);
        }

        private Transform GearRoot(string name, Transform parent)
        {
            return NewJoint(name, parent, Vector3.zero);
        }

        private Transform GlowingPart(string name, PrimitiveType shape, Vector3 at, Vector3 size, Color color, Transform parent)
        {
            Transform part = Part(name, shape, at, size, color, parent);
            Material material = Mat(color, VisualSurface.Crystal);
            if (part != null) part.GetComponent<Renderer>().sharedMaterial = material;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * .7f);
            return part;
        }

        private void BuildEquipmentWeapon(EquipmentAppearance look)
        {
            Transform anchor = swordRig != null ? swordRig : staffRig != null ? staffRig : bowRig;
            if (anchor == null) return;
            equipmentWeapon = GearRoot("Equipped Weapon", anchor);
            Color dark = Color.Lerp(look.Metal, Color.black, .56f);
            float growth = 1f + (look.Tier - 1) * .13f;
            if (swordRig != null)
            {
                Part("Wrapped grip", PrimitiveType.Cylinder, WeaponAnchorLocal(WeaponVisualAnchor.SwordGrip), new Vector3(.105f, .16f, .105f), dark, equipmentWeapon);
                Part("Faceted pommel", PrimitiveType.Sphere, WeaponAnchorLocal(WeaponVisualAnchor.SwordPommel), Vector3.one * (.14f + look.Tier * .02f), look.Accent, equipmentWeapon);
                Part("Swept guard", PrimitiveType.Cube, WeaponAnchorLocal(WeaponVisualAnchor.SwordGuard), new Vector3(.42f + look.Tier * .08f, .09f, .15f), look.Accent, equipmentWeapon);
                Blade("Tiered blade", equipmentWeapon, WeaponAnchorLocal(WeaponVisualAnchor.SwordRoot), .17f + look.Tier * .035f,
                    weaponStructure.SwordTip - weaponStructure.SwordRoot, .075f, look.Metal);
                Part("Blade spine", PrimitiveType.Cube, new Vector3(0, .64f * growth, .043f),
                    new Vector3(.045f, .77f * growth, .018f), look.Accent, equipmentWeapon);
                for (int side = -1; side <= 1; side += 2)
                {
                    if (look.Tier >= 2)
                        Part("Guard wing", PrimitiveType.Cube, new Vector3(side * .29f, .22f, 0),
                            new Vector3(.24f, .075f, .13f), look.Metal, equipmentWeapon).localRotation = Quaternion.Euler(0, 0, side * 25f);
                    if (look.Tier >= 3)
                        Part("Blade fang", PrimitiveType.Cube, new Vector3(side * .13f, .68f * growth, 0),
                            new Vector3(.075f, .33f, .06f), look.Accent, equipmentWeapon).localRotation = Quaternion.Euler(0, 0, side * 14f);
                }
                if (look.HasRunes)
                    for (int i = 0; i < 3; i++)
                        GlowingPart("Blade rune", PrimitiveType.Sphere, new Vector3(0, .44f + i * .22f, .067f),
                            Vector3.one * (.055f + Mathf.Min(9,look.UpgradeRank) * .002f), look.Glow, equipmentWeapon);
                for (int i = 0; i < Mathf.Min(9,look.UpgradeRank); i++)
                    GlowingPart("Forging mark", PrimitiveType.Sphere,
                        new Vector3((i % 5 - 2) * .1f, .13f - i / 5 * .11f, .09f),
                        Vector3.one * .055f, look.Glow, equipmentWeapon);
            }
            else if (staffRig != null)
            {
                Part("Inlaid staff", PrimitiveType.Cylinder, new Vector3(0, weaponStructure.StaffShaftCenter, .03f),
                    new Vector3(.09f + look.Tier * .012f, weaponStructure.StaffShaftHalfLength, .09f + look.Tier * .012f), dark, equipmentWeapon);
                Part("Staff collar", PrimitiveType.Cylinder, WeaponAnchorLocal(WeaponVisualAnchor.StaffCollar),
                    new Vector3(.22f + look.Tier * .025f, .1f, .22f + look.Tier * .025f), look.Accent, equipmentWeapon);
                GlowingPart("Focus crystal", PrimitiveType.Sphere, WeaponAnchorLocal(WeaponVisualAnchor.StaffCore),
                    Vector3.one * weaponStructure.StaffCoreDiameter, look.Glow, equipmentWeapon);
                for (int side = -1; side <= 1; side += 2)
                {
                    if (look.Tier >= 2)
                        Part("Crystal prong", PrimitiveType.Cube, new Vector3(side * .24f, weaponStructure.StaffCore - .11f, .03f),
                            new Vector3(.09f, .44f + look.Tier * .07f, .1f), look.Metal, equipmentWeapon)
                            .localRotation = Quaternion.Euler(0, 0, -side * 24f);
                    if (look.Tier >= 4)
                        GlowingPart("Floating shard", PrimitiveType.Sphere, new Vector3(side * .43f, weaponStructure.StaffCore + .14f, .03f),
                            Vector3.one * .14f, look.Glow, equipmentWeapon);
                }
                if (look.HasRunes)
                    for (int i = 0; i < 3; i++)
                        GlowingPart("Staff rune", PrimitiveType.Sphere, new Vector3(0, .16f + i * .3f, .13f),
                            Vector3.one * .065f, look.Glow, equipmentWeapon);
                for (int i = 0; i < Mathf.Min(9,look.UpgradeRank); i++)
                    GlowingPart("Staff forging mark", PrimitiveType.Sphere,
                        new Vector3(Mathf.Cos(i*Mathf.PI*2/Mathf.Min(9,look.UpgradeRank))*.28f, weaponStructure.StaffCore+Mathf.Sin(i*Mathf.PI*2/Mathf.Min(9,look.UpgradeRank))*.28f, .14f),
                        Vector3.one * .055f, look.Glow, equipmentWeapon);
            }
            else
            {
                Part("Bow grip", PrimitiveType.Cube, WeaponAnchorLocal(WeaponVisualAnchor.BowGrip),
                    new Vector3(.12f, .22f, .12f), dark, equipmentWeapon);
                for (int side = -1; side <= 1; side += 2)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        float t = (i + .5f) / 4f;
                        Part("Layered bow limb", PrimitiveType.Capsule,
                            new Vector3(0, side * t * weaponStructure.BowReach, .25f - t * .19f),
                            new Vector3(.095f + look.Tier * .012f, .17f * growth, .085f), look.Metal, equipmentWeapon)
                            .localRotation = Quaternion.Euler(side * -18f, 0, 0);
                    }
                    Part("Bow tip", PrimitiveType.Sphere, new Vector3(0, side * weaponStructure.BowReach, .05f),
                        Vector3.one * (.12f + look.Tier * .015f), look.Accent, equipmentWeapon);
                    if (look.Tier >= 3)
                        Part("Bow horn", PrimitiveType.Capsule, new Vector3(0, side * .47f * growth, .22f),
                            new Vector3(.09f, .24f, .09f), look.Accent, equipmentWeapon)
                            .localRotation = Quaternion.Euler(side * -35f, 0, 0);
                }
                if (look.HasRunes)
                    GlowingPart("Bow focus", PrimitiveType.Sphere, new Vector3(0, 0, .37f),
                        Vector3.one * (.17f + Mathf.Min(9,look.UpgradeRank) * .008f), look.Glow, equipmentWeapon);
                for (int i = 0; i < Mathf.Min(9,look.UpgradeRank); i++)
                    GlowingPart("Bow forging mark", PrimitiveType.Sphere,
                        new Vector3(0, (i - 4.5f) * .1f, .32f - Mathf.Abs(i - 4.5f) * .025f),
                        Vector3.one * .055f, look.Glow, equipmentWeapon);
            }
            if (look.HasAura)
                for (int side = -1; side <= 1; side += 2)
                    GlowingPart("Weapon aura shard", PrimitiveType.Sphere,
                        new Vector3(side * .32f, swordRig != null ? .76f : staffRig != null ? weaponStructure.StaffCore + .15f : 0, .08f),
                        Vector3.one * (look.HasCrown ? .16f : .11f), look.Glow, equipmentWeapon);
            BuildGearSignature(look,equipmentWeapon,swordRig!=null?WeaponAnchorLocal(WeaponVisualAnchor.SwordGuard):staffRig!=null?WeaponAnchorLocal(WeaponVisualAnchor.StaffCollar):new Vector3(0,0,.27f));
        }

        private void BuildEquipmentArmor(EquipmentAppearance look)
        {
            if(heroClass!=HeroClass.Vanguard){BuildClassEquipmentArmor(look);BuildGearSignature(look,equipmentArmor,new Vector3(0,1.33f,.40f));return;}
            equipmentArmor = GearRoot("Equipped Armor", spine);
            equipmentArmor.localPosition = Vector3.down * 1.12f;
            float width = .68f + look.Tier * .065f;
            Part("Raised breastplate", PrimitiveType.Cube, new Vector3(0, 1.33f, .32f),
                new Vector3(width, .5f, .16f), look.Metal, equipmentArmor);
            Part("Central crest", PrimitiveType.Cube, new Vector3(0, 1.38f, .42f),
                new Vector3(.18f + look.Tier * .03f, .26f + look.Tier * .025f, .04f), look.Accent, equipmentArmor)
                .localRotation = Quaternion.Euler(0, 0, 45f);
            for (int side = -1; side <= 1; side += 2)
            {
                Transform shoulder = GearRoot("Equipped Shoulder", side < 0 ? leftArm : rightArm);
                if (side < 0) equipmentLeftShoulder = shoulder;
                else equipmentRightShoulder = shoulder;
                Part("Shoulder shell", PrimitiveType.Sphere, new Vector3(0, 0, .05f),
                    new Vector3(.3f + look.Tier * .04f, .23f + look.Tier * .025f, .38f), look.Metal, shoulder, VisualSurface.Metal);
                if (look.Tier >= 2)
                    Part("Layered pauldron", PrimitiveType.Cube, new Vector3(side * .13f, -.12f, .12f),
                        new Vector3(.3f, .17f + look.Tier * .03f, .3f), look.Accent, shoulder)
                        .localRotation = Quaternion.Euler(0, 0, side * 18f);
                if (look.Tier >= 3)
                    Part("Armor horn", PrimitiveType.Capsule, new Vector3(side * .17f, .2f, .02f),
                        new Vector3(.12f, .26f + look.Tier * .04f, .12f), look.Metal, shoulder)
                        .localRotation = Quaternion.Euler(0, 0, -side * 35f);
                if (look.HasRunes)
                    GlowingPart("Armor rune", PrimitiveType.Sphere, new Vector3(side * .23f, 1.37f, .43f),
                        Vector3.one * .095f, look.Glow, equipmentArmor);
            }
            for (int i = 0; i < look.Tier; i++)
                Part("Waist plate", PrimitiveType.Cube, new Vector3(0, 1.07f - i * .105f, .3f),
                    new Vector3(width - i * .055f, .1f, .13f), i % 2 == 0 ? look.Metal : look.Accent, equipmentArmor);
            for (int i = 0; i < Mathf.Min(9,look.UpgradeRank); i++)
                GlowingPart("Armor forging mark", PrimitiveType.Sphere,
                    new Vector3((i % 5 - 2) * .11f, 1.58f - i / 5 * .09f, .41f),
                    Vector3.one * .055f, look.Glow, equipmentArmor);
            if (look.HasAura)
                GlowingPart("Armor heart", PrimitiveType.Sphere, new Vector3(0, 1.39f, .46f),
                    Vector3.one * (look.HasCrown ? .2f : .13f), look.Glow, equipmentArmor);
            BuildGearSignature(look,equipmentArmor,new Vector3(0,1.33f,.40f));
        }

        private void BuildEquipmentRelic(EquipmentAppearance look)
        {
            equipmentRelic = GearRoot("Equipped Relic", spine);
            equipmentRelic.localPosition = Vector3.down * 1.12f;
            foreach(var piece in EquipmentAttachmentRecipe.Relic(look.Tier,look.UpgradeRank))
            {
                Vector3 at=new Vector3(piece.X,piece.Y,piece.Z),scale=new Vector3(piece.Width,piece.Height,piece.Depth);
                Color color=piece.Kind==RelicPartKind.Mount?look.Metal:piece.Kind==RelicPartKind.Wing?look.Accent:look.Glow;
                bool luminous=piece.Kind==RelicPartKind.Gem||piece.Kind==RelicPartKind.Halo;
                PrimitiveType shape=piece.Shape==AttachmentShape.Disc?PrimitiveType.Cylinder:piece.Shape==AttachmentShape.Box?PrimitiveType.Cube:PrimitiveType.Sphere;
                Transform part=luminous?GlowingPart("Relic "+piece.Kind,shape,at,scale,color,equipmentRelic):
                    Part("Relic "+piece.Kind,shape,at,scale,color,equipmentRelic,piece.Kind==RelicPartKind.Gem?VisualSurface.Crystal:VisualSurface.Metal);
                part.localRotation=Quaternion.Euler(piece.Shape==AttachmentShape.Disc?90:0,0,piece.Roll);
            }
            BuildGearSignature(look,equipmentRelic,new Vector3(0,.88f,.43f));
        }

        public static CombatModel Enemy(Transform parent, EnemyKind kind, bool boss)
        {
            CombatModel model = Create(parent);
            model.BuildEnemy(kind, boss);
            model.EnhanceEnemy(kind, boss);
            EnemySilhouetteArt.ApplyEnemy(model);
            return model;
        }

        internal static CombatModel LargeExpedition(Transform parent)
        {
            CombatModel model = Create(parent);
            model.largeBossRig = model.gameObject.AddComponent<LargeBossRig>();
            model.largeBossRig.Build((color, surface) => model.Mat(color, surface));
            return model;
        }

        public static CombatModel Companion(Transform parent, SummonedCompanion.Kind form)
        {
            CombatModel model = Create(parent);
            if(form==SummonedCompanion.Kind.Wisp)
            {
                model.floating=true;
                model.body=model.Part("Ember wisp core",PrimitiveType.Sphere,new Vector3(0,1.2f,0),new Vector3(.45f,.65f,.45f),new Color(1f,.55f,.18f));
                model.companionBodyScale=model.body.localScale;
                for(int side=-1;side<=1;side+=2)model.Part("Ember feather",PrimitiveType.Capsule,new Vector3(side*.5f,1.35f,0),new Vector3(.22f,.85f,.16f),new Color(1f,.84f,.38f),model.CompanionRigidParent()).localRotation=Quaternion.Euler(0,0,side*60);
            }
            else if (form == SummonedCompanion.Kind.Spirit)
            {
                model.Part("Turret stone base",PrimitiveType.Cylinder,new Vector3(0,.2f,0),new Vector3(1.05f,.2f,1.05f),new Color(.18f,.32f,.34f));
                model.Part("Turret pillar",PrimitiveType.Cube,new Vector3(0,.64f,0),new Vector3(.38f,.75f,.38f),new Color(.3f,.52f,.5f));
                model.floating = true;
                model.body = model.Part("Star spirit", PrimitiveType.Sphere, new Vector3(0,1.2f,0), new Vector3(.54f,.7f,.54f), new Color(.4f,1f,.84f));
                model.companionBodyScale = model.body.localScale;
                model.decoration = model.Part("Star crown", PrimitiveType.Cube, new Vector3(0,1.75f,0), Vector3.one*.25f, new Color(1f,.87f,.47f),model.CompanionRigidParent());
                for(int i=-1;i<=1;i+=2) model.Part("Spirit wing",PrimitiveType.Capsule,new Vector3(i*.42f,1.35f,0),new Vector3(.18f,.5f,.15f),new Color(.67f,1f,.88f),model.CompanionRigidParent()).localRotation=Quaternion.Euler(0,0,i*45);
            }
            else if (form == SummonedCompanion.Kind.Treant)
            {
                model.treantCompanion = true;
                model.Humanoid(new Color(.35f,.26f,.15f), new Color(.28f,.32f,.14f), new Color(.4f,.69f,.32f), 1.6f, "Treant bark torso");
                model.transform.localScale = Vector3.one * 1.3f;
                for (int i = -1; i <= 1; i++)
                    model.Part("Leaf crown", PrimitiveType.Sphere, new Vector3(i * .35f, 2.23f, -.08f), new Vector3(.75f,.65f,.65f), new Color(.35f,.69f,.37f), model.CompanionRigidParent(), surface: VisualSurface.Foliage);
            }
            else
            {
                Color fur = new Color(.57f,.82f,.77f);
                model.body = model.Part("Spirit wolf torso", PrimitiveType.Capsule, new Vector3(0,.66f,0), new Vector3(.48f,.57f,.52f), fur);
                model.body.localRotation = Quaternion.Euler(90,0,0);
                model.bodyRestRotation = model.body.localRotation;
                model.Part("Wolf head", PrimitiveType.Cube, new Vector3(0,.9f,.55f), new Vector3(.42f,.42f,.52f), fur);
                model.wolfJaw = model.Joint("Wolf bite hinge", new Vector3(0,.79f,.72f));
                model.Part("Muzzle", PrimitiveType.Cube, new Vector3(0,0,.18f), new Vector3(.28f,.2f,.3f), new Color(.27f,.47f,.45f), model.wolfJaw, meshModule:"Wolf muzzle");
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    model.Part("Ear", PrimitiveType.Cube, new Vector3(sign*.17f,1.2f,.5f), new Vector3(.19f,.32f,.19f), fur, meshModule:"Wolf ear").localRotation=Quaternion.Euler(-15,0,sign*15);
                    model.Part("Luminous eye", PrimitiveType.Sphere, new Vector3(sign*.16f,.96f,.64f), Vector3.one*.09f, new Color(.75f,1f,.6f));
                }
                model.quadruped = true;
                model.pawOrigins = new Vector3[4];
                for (int i = 0; i < 4; i++)
                {
                    model.pawOrigins[i] = new Vector3(i % 2 == 0 ? -.22f : .22f, .47f, i < 2 ? .4f : -.4f);
                    model.paws[i] = model.Joint("Wolf articulated leg", model.pawOrigins[i]);
                    model.Part("Paw", PrimitiveType.Capsule, new Vector3(0,-.18f,.035f), new Vector3(i<2?.23f:.28f,.24f,.25f), fur, model.paws[i], meshModule:i<2?"Wolf foreleg":"Wolf hindleg");
                }
                model.tailRig = model.Joint("Wolf tail base", new Vector3(0,.75f,-.5f));
                model.Part("Tail",PrimitiveType.Capsule,new Vector3(0,0,-.22f),new Vector3(.27f,.4f,.27f),fur,model.tailRig,meshModule:"Wolf tail").localRotation=Quaternion.Euler(-60,0,0);
            }
            return model;
        }

        private void SummonerCrown()
        {
            RemovePart(headRig.Find("Pointed Wizard Hat"));
            RemovePart(headRig.Find("Hat Brim"));
            RemovePart(headRig.Find("Hat Gem"));
            Color jade = new Color(.54f,1f,.76f);
            for(int side=-1;side<=1;side+=2)
            {
                Part("Spirit antler",PrimitiveType.Capsule,new Vector3(side*.25f,.42f,-.06f),new Vector3(.09f,.36f,.1f),jade,headRig).localRotation=Quaternion.Euler(-12,0,-side*25);
                Part("Spirit antler branch",PrimitiveType.Capsule,new Vector3(side*.39f,.55f,-.06f),new Vector3(.07f,.16f,.08f),jade,headRig).localRotation=Quaternion.Euler(0,0,-side*65);
            }
            Part("Moonstone circlet",PrimitiveType.Sphere,new Vector3(0,.32f,.22f),new Vector3(.18f,.22f,.08f),jade,headRig);
        }


        private static CombatModel Create(Transform parent)
        {
            GameObject obj = new GameObject("Character Model");
            obj.transform.SetParent(parent, false);
            CombatModel model = obj.AddComponent<CombatModel>();
            model.phase = Random.value * 6.28f;
            model.enemyOwner = parent.GetComponent<EnemyController>();
            return model;
        }

        private Material Mat(Color color, VisualSurface surface = VisualSurface.Cloth)
        {
            Material material;
            SurfaceKey key = new SurfaceKey(color, surface);
            if (palette.TryGetValue(key, out material)) return material;
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            material = new Material(shader) { color = color };
            ProceduralVisuals.ApplySurface(material, surface);
            palette.Add(key, material);
            return material;
        }

        private Transform Part(string name, PrimitiveType shape, Vector3 position, Vector3 size, Color color, Transform parent = null, VisualSurface? surface = null, string meshModule = null)
        {
            GameObject obj = ProceduralVisuals.Create(name, shape, Mat(color, surface ?? ProceduralVisuals.SurfaceFor(name)));
            AuthoredActorMeshes.Apply(obj,meshModule ?? name,shape);
            ActorSilhouetteF1.Apply(obj,name,treantCompanion);
            obj.transform.SetParent(parent == null ? transform : parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = size;
            return obj.transform;
        }

        private Transform Joint(string name, Vector3 at)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = at;
            return obj.transform;
        }

        private void Humanoid(Color skin, Color cloth, Color armor, float bulk, string bodyModule = "Breastplate")
        {
            Color boot = new Color(.12f, .15f, .21f);
            body = Part("Breastplate", PrimitiveType.Capsule, new Vector3(0, 1.22f, 0), new Vector3(.75f * bulk, .49f, .48f * bulk), cloth, meshModule:bodyModule);
            Part("Belt", PrimitiveType.Cube, new Vector3(0, .88f, .02f), new Vector3(.66f * bulk, .13f, .49f), boot);
            Part("Buckle", PrimitiveType.Cube, new Vector3(0, .88f, .29f), new Vector3(.16f, .14f, .06f), armor);
            Part("Head", PrimitiveType.Sphere, new Vector3(0, 1.97f, 0), new Vector3(.51f, .56f, .49f), skin);
            Part("Eyes L", PrimitiveType.Sphere, new Vector3(-.105f, 2f, .221f), new Vector3(.06f, .075f, .035f), boot);
            Part("Eyes R", PrimitiveType.Sphere, new Vector3(.105f, 2f, .221f), new Vector3(.06f, .075f, .035f), boot);
            leftLeg = Joint("Left Hip", new Vector3(-.21f, .83f, 0));
            rightLeg = Joint("Right Hip", new Vector3(.21f, .83f, 0));
            foreach (Transform leg in new[] { leftLeg, rightLeg })
            {
                Part("Trouser", PrimitiveType.Capsule, new Vector3(0, -.29f, 0), new Vector3(.23f, .29f, .24f), cloth, leg);
                Part("Boot", PrimitiveType.Cube, new Vector3(0, -.66f, .075f), new Vector3(.28f, .25f, .42f), boot, leg);
            }
            leftArm = Joint("Left Shoulder", new Vector3(-.47f * bulk, 1.57f, 0));
            rightArm = Joint("Right Shoulder", new Vector3(.47f * bulk, 1.57f, 0));
            foreach (Transform arm in new[] { leftArm, rightArm })
            {
                Part("Pauldrons", PrimitiveType.Sphere, Vector3.zero, new Vector3(.42f, .32f, .45f), armor, arm);
                Part("Sleeve", PrimitiveType.Capsule, new Vector3(0, -.28f, 0), new Vector3(.23f, .26f, .24f), cloth, arm);
                Part("Glove", PrimitiveType.Sphere, new Vector3(0, -.52f, .04f), new Vector3(.24f, .26f, .25f), skin, arm);
            }
        }

        private void Cape(Color color, HeroClass hero)
        {
            GameObject obj = new GameObject("Tailored Cloak");
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = new Vector3(0, 1.66f, -.23f);
            cloak = obj.transform;
            tailoredCloth = obj.AddComponent<TailoredCloth>();
            tailoredCloth.Initialize(Mat(color, VisualSurface.Cloth), hero);
        }

        private void BuildHero(HeroClass hero)
        {
            Color accent = GameBalance.ClassColor(hero);
            Color skin = new Color(.94f,.76f,.59f);
            Color steel = new Color(.64f,.75f,.85f);
            Humanoid(skin, accent * .62f, hero == HeroClass.Vanguard ? steel : accent, hero == HeroClass.Vanguard ? 1.15f : 1f, hero == HeroClass.Vanguard ? "Breastplate" : "Tailored cloth torso");
            Cape(accent * .48f, hero);
            if (hero == HeroClass.Vanguard)
            {
                Part("Helmet", PrimitiveType.Sphere, new Vector3(0,2.14f,-.03f), new Vector3(.57f,.4f,.52f), steel);
                Part("Helmet Crest", PrimitiveType.Cube, new Vector3(0,2.38f,-.04f), new Vector3(.1f,.31f,.43f), accent);
                Part("Nose Guard", PrimitiveType.Cube, new Vector3(0,2.05f,.265f), new Vector3(.06f,.24f,.055f), steel);
                Transform shield = Part("Round Shield", PrimitiveType.Cylinder, new Vector3(-.14f,-.39f,.16f), new Vector3(.72f,.075f,.72f), accent * .6f, leftArm);
                shield.localRotation = Quaternion.Euler(90,0,0);
                Part("Shield Boss", PrimitiveType.Sphere, new Vector3(-.14f,-.39f,.27f), new Vector3(.24f,.24f,.12f), steel, leftArm);
                Part("Sword Handle", PrimitiveType.Cylinder, new Vector3(0,-.53f,.11f), new Vector3(.1f,.2f,.1f), new Color(.19f,.16f,.15f), rightArm);
                Part("Sword Guard", PrimitiveType.Cube, new Vector3(0,-.32f,.11f), new Vector3(.4f,.085f,.12f), accent, rightArm);
                Part("Silver Blade", PrimitiveType.Cube, new Vector3(0,.2f,.11f), new Vector3(.14f,.95f,.055f), steel, rightArm);
                Part("Blade Tip", PrimitiveType.Sphere, new Vector3(0,.68f,.11f), new Vector3(.14f,.2f,.055f), steel, rightArm);
            }
            else if (hero == HeroClass.Arcanist || hero == HeroClass.Summoner)
            {
                Part("Hat Brim", PrimitiveType.Cylinder, new Vector3(0,2.21f,0), new Vector3(.88f,.05f,.88f), accent * .6f);
                Part("Wizard Hat", PrimitiveType.Capsule, new Vector3(0,2.38f,-.045f), new Vector3(.43f,.3f,.43f), accent * .48f);
                Part("Hat Gem", PrimitiveType.Sphere, new Vector3(0,2.3f,.24f), new Vector3(.15f,.2f,.1f), new Color(.55f,.94f,1f));
                Part("Staff", PrimitiveType.Cylinder, new Vector3(0,-.22f,.15f), new Vector3(.085f,.98f,.085f), new Color(.46f,.29f,.19f), rightArm);
                Part("Staff Gold", PrimitiveType.Sphere, new Vector3(0,.63f,.15f), new Vector3(.31f,.21f,.31f), new Color(.95f,.76f,.3f), rightArm);
                decoration = Part("Arcane Crystal", PrimitiveType.Cube, new Vector3(0,.86f,.15f), new Vector3(.22f,.34f,.22f), new Color(.4f,.92f,1f), rightArm);
                decoration.localRotation = Quaternion.Euler(15,0,45);
                Part("Orb", PrimitiveType.Sphere, new Vector3(0,-.42f,.29f), new Vector3(.29f,.29f,.29f), accent, leftArm);
            }
            else
            {
                Part("Forest Hood", PrimitiveType.Sphere, new Vector3(0,2.15f,-.1f), new Vector3(.62f,.4f,.56f), accent * .48f);
                Part("Feather", PrimitiveType.Capsule, new Vector3(.22f,2.35f,-.09f), new Vector3(.085f,.25f,.05f), new Color(1f,.81f,.36f));
                Part("Quiver", PrimitiveType.Cylinder, new Vector3(.34f,1.4f,-.36f), new Vector3(.25f,.4f,.25f), new Color(.39f,.23f,.13f));
                for (int i=0; i<3; i++) Part("Spare Arrow", PrimitiveType.Cylinder, new Vector3(.27f+i*.07f,1.91f,-.36f), new Vector3(.035f,.3f,.035f), steel);
                Transform bow = Joint("Bow", new Vector3(-.52f,1.1f,.23f));
                for (int i=0; i<9; i++)
                {
                    float a = (-80f+i*20f)*Mathf.Deg2Rad;
                    Transform wood = Part("Bow Limb", PrimitiveType.Capsule, new Vector3(0,Mathf.Sin(a)*.59f,Mathf.Cos(a)*.28f), new Vector3(.075f,.115f,.07f), new Color(.73f,.47f,.22f), bow, VisualSurface.Wood);
                    wood.localRotation=Quaternion.Euler(i*20-80,0,0);
                }
                Part("Bowstring",PrimitiveType.Cylinder,new Vector3(0,0,.05f),new Vector3(.014f,.6f,.014f),steel,bow);
                Part("Nocked Arrow",PrimitiveType.Cube,new Vector3(0,0,.45f),new Vector3(.035f,.035f,.85f),steel,bow);
            }
        }

        // Original, self-contained character art: articulated costume pieces share one palette.
        // The spine and limbs are separate so locomotion never cancels an attack's upper-body pose.
        private void EnhanceHero(HeroClass hero)
        {
            isHero = true;
            heroClass = hero;
            Color accent = GameBalance.ClassColor(hero);
            Color gold = new Color(.94f, .73f, .32f);
            Color steel = new Color(.73f, .83f, .91f);
            Color leather = new Color(.24f, .16f, .12f);
            Color hair = hero == HeroClass.Arcanist ? new Color(.81f, .73f, .61f) : new Color(.24f, .12f, .075f);
            for (int i = -1; i <= 1; i += 2)
            {
                Transform lockOfHair = Part("Hair Side Lock", PrimitiveType.Capsule,
                    new Vector3(i * .23f, 1.98f, -.025f), new Vector3(.13f, .22f, .16f), hair);
                lockOfHair.localRotation = Quaternion.Euler(-12f, 0, -i * 11f);
                Part("Ear", PrimitiveType.Sphere, new Vector3(i * .26f, 1.97f, .015f),
                    new Vector3(.085f, .15f, .1f), new Color(.94f, .76f, .59f));
            }
            Part("Nose", PrimitiveType.Sphere, new Vector3(0, 1.96f, .243f),
                new Vector3(.075f, .09f, .075f), new Color(.94f, .76f, .59f));
            Part("Collar", PrimitiveType.Cylinder, new Vector3(0, 1.7f, -.01f), new Vector3(.38f, .055f, .35f), gold);
            if (hero == HeroClass.Vanguard)
            {
                Transform chest = Part("Cuirass", PrimitiveType.Cube, new Vector3(0, 1.35f, .235f),
                    new Vector3(.64f, .44f, .15f), steel, surface: VisualSurface.Metal);
                chest.localRotation = Quaternion.Euler(-8f, 0, 0);
                for (int i = 0; i < 3; i++)
                    Part("Overlapping Armor", PrimitiveType.Cube, new Vector3(0, 1.13f - i * .11f, .255f),
                        new Vector3(.57f - i * .045f, .13f, .11f), i % 2 == 0 ? steel * .85f : steel);
                Part("Chest Crest", PrimitiveType.Cube, new Vector3(0, 1.38f, .33f), new Vector3(.2f, .2f, .045f), gold)
                    .localRotation = Quaternion.Euler(0, 0, 45);
                for (int i = -1; i <= 1; i += 2)
                {
                    Part("Armor Tasset", PrimitiveType.Cube, new Vector3(i * .29f, .77f, .15f),
                        new Vector3(.23f, .32f, .13f), steel).localRotation = Quaternion.Euler(-8f, 0, i * 12f);
                    Part("Helmet Cheek Guard", PrimitiveType.Cube, new Vector3(i * .25f, 1.96f, .13f),
                        new Vector3(.085f, .23f, .14f), steel).localRotation = Quaternion.Euler(-12f, 0, i * 8f);
                }
            }
            else if (hero == HeroClass.Arcanist || hero == HeroClass.Summoner)
            {
                RemovePart(transform.Find("Wizard Hat"));
                Tapered("Pointed Wizard Hat", transform, new Vector3(0, 2.24f, -.02f),
                    .29f, .018f, .62f, new Vector3(-.14f, 0, -.11f), accent * .48f, 18);
                Tapered("Layered Robe", transform, new Vector3(0, .4f, 0), .49f, .31f, .62f,
                    Vector3.zero, accent * .68f, 20);
                for (int i = -1; i <= 1; i += 2)
                {
                    Transform stole = Part("Embroidered Stole", PrimitiveType.Cube, new Vector3(i * .22f, 1.3f, .246f),
                        new Vector3(.12f, .67f, .065f), gold);
                    stole.localRotation = Quaternion.Euler(0, 0, i * 10f);
                    Part("Robe Hem", PrimitiveType.Cube, new Vector3(i * .25f, .53f, .36f),
                        new Vector3(.1f, .27f, .06f), gold).localRotation = Quaternion.Euler(-10f, 0, i * 15f);
                }
                Part("Arcane Brooch", PrimitiveType.Sphere, new Vector3(0, 1.48f, .285f), new Vector3(.17f, .22f, .09f),
                    new Color(.4f, .91f, 1f));
            }
            else
            {
                for (int i = -1; i <= 1; i += 2)
                {
                    Part("Leather Vest", PrimitiveType.Cube, new Vector3(i * .2f, 1.31f, .18f),
                        new Vector3(.28f, .57f, .16f), leather).localRotation = Quaternion.Euler(0, 0, i * -9f);
                    Part("Vest Buckle", PrimitiveType.Cube, new Vector3(i * .18f, 1.34f, .285f),
                        new Vector3(.075f, .075f, .035f), gold);
                }
                Part("Scarf", PrimitiveType.Cube, new Vector3(0, 1.68f, .16f), new Vector3(.48f, .18f, .25f), accent);
                Part("Scarf Tail", PrimitiveType.Cube, new Vector3(.3f, 1.47f, .23f), new Vector3(.17f, .38f, .045f), accent)
                    .localRotation = Quaternion.Euler(-8f, 0, -15f);
                Part("Cross Body Strap", PrimitiveType.Cube, new Vector3(.015f, 1.29f, .29f),
                    new Vector3(.095f, .78f, .055f), gold * .65f).localRotation = Quaternion.Euler(0, 0, -37f);
            }

            spine = Joint("Spine", new Vector3(0, 1.12f, 0));
            pelvis = Joint("Pelvis", new Vector3(0, .83f, 0));
            var rootParts = new List<Transform>();
            foreach (Transform part in transform) if (part != spine && part != pelvis) rootParts.Add(part);
            foreach (Transform part in rootParts) part.SetParent(part == leftLeg || part == rightLeg ? pelvis : spine, true);
            headRig = NewJoint("Neck", spine, new Vector3(0, .68f, 0));
            foreach (Transform part in rootParts)
            {
                string n = part.name;
                if (n == "Head" || n == "Ear" || n == "Nose" || n.StartsWith("Eyes") || n.StartsWith("Hair") ||
                    n.StartsWith("Helmet") || n.StartsWith("Hat") || n == "Nose Guard" ||
                    n == "Pointed Wizard Hat" || n == "Forest Hood" || n == "Feather") part.SetParent(headRig, true);
            }
            if (hero == HeroClass.Ranger)
            {
                leftArm.localPosition = new Vector3(-.36f, .45f, 0);
                rightArm.localPosition = new Vector3(.36f, .45f, 0);
            }
            leftElbow = ArticulateArm(leftArm, hero == HeroClass.Vanguard ? steel : leather);
            rightElbow = ArticulateArm(rightArm, hero == HeroClass.Vanguard ? steel : leather);
            leftKnee = ArticulateLeg(leftLeg, hero == HeroClass.Vanguard ? steel : leather);
            rightKnee = ArticulateLeg(rightLeg, hero == HeroClass.Vanguard ? steel : leather);

            if (hero == HeroClass.Vanguard)
            {
                foreach (string name in new[] { "Sword Handle", "Sword Guard", "Silver Blade", "Blade Tip" })
                    RemovePart(rightElbow.Find(name));
                swordRig = NewJoint("Sword Wrist", rightElbow, new Vector3(0, -.23f, .04f));
                Part("Wrapped Hilt", PrimitiveType.Cylinder, Vector3.zero, new Vector3(.105f, .15f, .105f), leather, swordRig);
                Part("Gold Pommel", PrimitiveType.Sphere, new Vector3(0, -.17f, 0), Vector3.one * .15f, gold, swordRig);
                Part("Crossguard", PrimitiveType.Cube, new Vector3(0, .16f, 0), new Vector3(.42f, .08f, .13f), gold, swordRig);
                Blade("Forged Sword", swordRig, new Vector3(0, .2f, 0), .18f, 1.1f, .065f, steel);
                Part("Blade Fuller", PrimitiveType.Cube, new Vector3(0, .66f, .037f), new Vector3(.035f, .84f, .012f),
                    new Color(.35f, .58f, .73f), swordRig);
                for (int i = -1; i <= 1; i += 2)
                    Part("Shield Inlay", PrimitiveType.Cube, new Vector3(-.14f + i * .17f, -.09f, .254f),
                        new Vector3(.065f, .43f, .055f), gold, leftElbow).localRotation = Quaternion.Euler(0, 0, i * -20f);
            }
            else if (hero == HeroClass.Arcanist || hero == HeroClass.Summoner)
            {
                staffRig = NewJoint("Staff Wrist", rightElbow, new Vector3(0, -.23f, .12f));
                foreach (string name in new[] { "Staff", "Staff Gold", "Arcane Crystal" })
                {
                    Transform part = rightElbow.Find(name);
                    if (part != null) part.SetParent(staffRig, true);
                }
                for (int i = -1; i <= 1; i += 2)
                {
                    Transform prong = Part("Staff Crystal Crown", PrimitiveType.Cube,
                        new Vector3(i * .14f, 1.26f, .03f), new Vector3(.065f, .37f, .07f), gold, staffRig);
                    prong.localRotation = Quaternion.Euler(0, 0, i * -18f);
                }
                Transform shaft = staffRig.Find("Staff"), collar = staffRig.Find("Staff Gold");
                shaft.localPosition = new Vector3(0, weaponStructure.StaffShaftCenter, .03f);
                shaft.localScale = new Vector3(.085f, weaponStructure.StaffShaftHalfLength, .085f);
                collar.localPosition = WeaponAnchorLocal(WeaponVisualAnchor.StaffCollar);
                decoration.localPosition = WeaponAnchorLocal(WeaponVisualAnchor.StaffCore);
                decoration.localRotation = Quaternion.identity;
                decoration.localScale = Vector3.one * weaponStructure.StaffCoreDiameter;
                castingOrb = leftElbow.Find("Orb");
            }
            else
            {
                bowRig = spine.Find("Bow");
                bowRig.SetParent(leftElbow, false);
                bowRig.localPosition = new Vector3(0, -.23f, -.20f);
                RemovePart(bowRig.Find("Bowstring"));
                RemovePart(bowRig.Find("Nocked Arrow"));
                Part("Bow Grip", PrimitiveType.Cube, WeaponAnchorLocal(WeaponVisualAnchor.BowGrip), new Vector3(.1f, .21f, .095f), leather, bowRig);
                Part("Arrow rest", PrimitiveType.Cube, WeaponAnchorLocal(WeaponVisualAnchor.BowArrowRest) + new Vector3(.035f, -.025f, 0),
                    new Vector3(.14f, .025f, .10f), leather, bowRig, VisualSurface.Wood);
                GameObject stringObject = new GameObject("Drawn Bowstring");
                stringObject.transform.SetParent(bowRig, false);
                bowstring = stringObject.AddComponent<LineRenderer>();
                bowstring.useWorldSpace = false;
                bowstring.positionCount = 3;
                bowstring.startWidth = bowstring.endWidth = .012f;
                bowstring.sharedMaterial = Mat(new Color(.85f, .82f, .67f));
                bowstring.SetPosition(0, WeaponAnchorLocal(WeaponVisualAnchor.BowUpperTip));
                bowstring.SetPosition(1, new Vector3(0, 0, .05f));
                bowstring.SetPosition(2, WeaponAnchorLocal(WeaponVisualAnchor.BowLowerTip));
                arrowRig = NewJoint("Arrow Nock", bowRig, new Vector3(0, 0, .05f));
                Part("Arrow Shaft", PrimitiveType.Cube, new Vector3(0, 0, .42f), new Vector3(.025f, .025f, .84f), gold, arrowRig);
                Transform tip = Blade("Arrow Head", arrowRig, new Vector3(0, 0, .82f), .09f, .17f, .025f, steel);
                tip.localRotation = Quaternion.Euler(90f, 0, 0);
                for (int i = 0; i < 3; i++)
                    Part("Arrow Fletching", PrimitiveType.Cube, new Vector3(0, 0, .095f), new Vector3(.11f, .012f, .16f), accent, arrowRig)
                        .localRotation = Quaternion.Euler(0, 0, i * 120f);
            }
        }

        private static Transform NewJoint(string name, Transform parent, Vector3 at)
        {
            Transform joint = new GameObject(name).transform;
            joint.SetParent(parent, false);
            joint.localPosition = at;
            return joint;
        }

        private static void RemovePart(Transform part)
        {
            if (part == null) return;
            part.gameObject.SetActive(false);
            Destroy(part.gameObject);
        }

        private Transform ArticulateArm(Transform arm, Color cuff)
        {
            var moving = new List<Transform>();
            foreach (Transform part in arm) if (part.name != "Pauldrons" && part.name != "Sleeve") moving.Add(part);
            Transform sleeve = arm.Find("Sleeve");
            sleeve.localPosition = new Vector3(0, -.14f, 0);
            sleeve.localScale = new Vector3(.235f, .17f, .24f);
            Transform elbow = NewJoint("Elbow", arm, new Vector3(0, -.30f, 0));
            foreach (Transform part in moving) part.SetParent(elbow, true);
            Part("Forearm Bracer", PrimitiveType.Capsule, new Vector3(0, -.1f, 0), new Vector3(.245f, .14f, .25f), cuff, elbow);
            Part("Cuff Trim", PrimitiveType.Cylinder, new Vector3(0, -.18f, 0), new Vector3(.25f, .026f, .25f),
                new Color(.83f, .66f, .3f), elbow);
            return elbow;
        }

        private Transform ArticulateLeg(Transform leg, Color greave)
        {
            Transform trouser = leg.Find("Trouser");
            trouser.localPosition = new Vector3(0, -.16f, 0);
            trouser.localScale = new Vector3(.24f, .20f, .25f);
            Transform knee = NewJoint("Knee", leg, new Vector3(0, -.37f, 0));
            leg.Find("Boot").SetParent(knee, true);
            Part("Shin Guard", PrimitiveType.Capsule, new Vector3(0, -.11f, .015f), new Vector3(.25f, .17f, .26f), greave, knee);
            Part("Knee Guard", PrimitiveType.Sphere, new Vector3(0, 0, .105f), new Vector3(.27f, .25f, .16f), greave, knee);
            return knee;
        }

        private Transform MeshPart(string name, Transform parent, Vector3 at, Vector3[] vertices, int[] triangles, Color color, VisualSurface? surface = null)
        {
            Transform part = NewJoint(name, parent, at);
            Mesh mesh = new Mesh { name = name, vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            part.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            part.gameObject.AddComponent<MeshRenderer>().sharedMaterial = Mat(color, surface ?? ProceduralVisuals.SurfaceFor(name));
            part.gameObject.AddComponent<OwnedCombatMesh>().Value = mesh;
            return part;
        }

        private Transform Blade(string name, Transform parent, Vector3 at, float width, float length, float depth, Color color)
        {
            float w = width * .5f, d = depth * .5f;
            return MeshPart(name, parent, at, new[] {
                new Vector3(-w,0,0), new Vector3(w,0,0), new Vector3(-w,length*.79f,0),
                new Vector3(w,length*.79f,0), new Vector3(0,length,0),
                new Vector3(0,0,d),new Vector3(0,length*.78f,d),
                new Vector3(0,0,-d),new Vector3(0,length*.78f,-d)
            }, new[] { 0,6,5,0,2,6,2,4,6,4,3,6,3,1,6,1,5,6,
                0,7,8,0,8,2,2,8,4,4,8,3,3,8,1,1,8,7,0,5,1,0,1,7 }, color, VisualSurface.Metal);
        }

        private Transform Tapered(string name, Transform parent, Vector3 at, float bottom, float top, float height,
            Vector3 offset, Color color, int count)
        {
            Vector3[] vertices = new Vector3[count * 2 + 2];
            var indices = new List<int>();
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count;
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                vertices[i] = radial * bottom;
                vertices[i + count] = radial * top + Vector3.up * height + offset;
                int next = (i + 1) % count;
                indices.AddRange(new[] { i, i + count, next, next, i + count, next + count,
                    count * 2, i, next, count * 2 + 1, next + count, i + count });
            }
            vertices[count * 2 + 1] = Vector3.up * height + offset;
            return MeshPart(name, parent, at, vertices, indices.ToArray(), color);
        }

        // Called only after the corresponding attack has successfully started / consumed resources.
        public void PlayAction(int skill, bool basic, float basicInterval = 0)
        {
            if (!isHero) return;
            if (actionDuration > 0 && actionAge < actionDuration || recoveryAge < .12f) BeginVisualRecovery(false);
            weaponActionId++;
            pilotCharging=false;
            actionSkill = skill;
            actionBasic = basic;
            actionAge = 0;
            actionStartedFrame = Time.frameCount;
            swingCount++;
            actionDuration = basic ? BasicActionTimeline.Duration(heroClass == HeroClass.Ranger, basicInterval > 0 ? basicInterval : SkillDamageBudgets.BasicInterval(heroClass))
                : SkillDamageBudgets.SkillPoseDuration(heroClass, skill, false);
            actionAge = actionDuration * (basic ? BasicActionTimeline.Contact(heroClass == HeroClass.Ranger) : SkillDamageBudgets.SkillPoseStart(heroClass, skill, false));
            CommitActionPose();
        }

        public bool BasicActionBlocked { get { return BasicActionTimeline.BlocksBasic(actionBasic, actionAge, actionDuration); } }
        public void CancelAction() { BeginVisualRecovery(); weaponActionId++; actionAge = actionDuration = 0; }

        public void ReleaseCharge(int skill)
        {
            PlayAction(skill, false);
            actionDuration = SkillDamageBudgets.SkillPoseDuration(heroClass, skill, true);
            actionAge = actionDuration * SkillDamageBudgets.SkillPoseStart(heroClass, skill, true);
            CommitActionPose();
        }

        // May run after Update (charged release). Evaluate without advancing any clock,
        // so anchors and effects observe the committed pose in this same frame.
        private void CommitActionPose()
        {if(isHero&&spine!=null){AdvanceVisualMotion(Time.deltaTime);AnimateHero(locomotion.Speed,1,false,0);}}

        private static Quaternion Pose(Vector3 idle, Vector3 windup, Vector3 release, float normalizedTime)
        {
            // A readable anticipation, a fast release, and a longer settling phase.
            if (normalizedTime < .30f)
                return Quaternion.Slerp(Quaternion.Euler(idle), Quaternion.Euler(windup), Mathf.SmoothStep(0, 1, normalizedTime / .30f));
            if (normalizedTime < .52f)
                return Quaternion.Slerp(Quaternion.Euler(windup), Quaternion.Euler(release), Mathf.SmoothStep(0, 1, (normalizedTime - .30f) / .22f));
            return Quaternion.Slerp(Quaternion.Euler(release), Quaternion.Euler(idle), Mathf.SmoothStep(0, 1, (normalizedTime - .52f) / .48f));
        }

        private void AnimateHero(float speed, float attack, bool hurt, float dt)
        {
            if(dt>0)AdvanceVisualMotion(dt);
            speed = smoothedSpeed = locomotion.Speed;
            if (tailoredCloth != null) tailoredCloth.SetMotion(speed, actionDuration > 0 && actionAge < actionDuration ? 1 : 0);
            gaitPhase = locomotion.Phase;
            if (actionDuration > 0 && Time.frameCount != actionStartedFrame) actionAge = Mathf.Min(actionAge + dt, actionDuration);
            float t = actionDuration > 0 ? actionAge / actionDuration : 1f;
            bool acting = t < 1f;
            if (!(acting && actionBasic && actionSkill == -2) && !(isolatedPreview && actionSkill == -3))
            { if (SampleBlenderPilot(acting,t,hurt)) return; }
            else SetBlenderPilotVisible(false);
            float stride = Mathf.Sin(gaitPhase) * speed;
            float lift = Mathf.Abs(Mathf.Sin(gaitPhase));
            float breathing = Mathf.Sin((isolatedPreview?previewTime:Time.time) * 2f + phase);
            HeroMotionStyle motionStyle=HeroMotionStyle.For(heroClass);
            float landing = Mathf.Pow(lift, 2f);
            transform.localPosition = Vector3.up * (landing * .032f * speed * motionStyle.Bob);
            pelvis.localPosition = new Vector3(stride * .024f, .83f, 0);
            pelvis.localRotation = Quaternion.Euler(0, stride * -4f, stride * 2f);
            leftLeg.localRotation = Quaternion.Euler(stride * 30f, 0, -2f);
            rightLeg.localRotation = Quaternion.Euler(-stride * 30f, 0, 2f);
            leftKnee.localRotation = Quaternion.Euler(Mathf.Max(0, -stride) * 42f, 0, 0);
            rightKnee.localRotation = Quaternion.Euler(Mathf.Max(0, stride) * 42f, 0, 0);
            spine.localPosition = new Vector3(0, 1.12f - motionStyle.Crouch + breathing * .009f, hurt ? -.035f : 0);
            spine.localRotation = Quaternion.Euler(speed * 5f * motionStyle.Torso, stride * 4f * motionStyle.Torso, -stride * 1.5f * motionStyle.Torso);
            headRig.localRotation = Quaternion.Euler(-speed * 3f * motionStyle.Torso, -stride * 3f * motionStyle.Torso + (acting ? 0 : breathing * motionStyle.Observation), stride * motionStyle.Torso);
            leftArm.localRotation = Quaternion.Euler(-stride * 18f, 0, -8f);
            rightArm.localRotation = Quaternion.Euler(stride * 18f, 0, 8f);
            leftElbow.localRotation = Quaternion.Euler(-12f, 0, 0);
            rightElbow.localRotation = Quaternion.Euler(-16f, 0, 0);
            cloak.localRotation = Quaternion.Euler(8f + speed * 13f + Mathf.Sin(gaitPhase * .5f) * 4f, stride * -5f, stride * 3f);
            ApplyHeroLocomotion();

            if (heroClass == HeroClass.Vanguard)
            {
                Vector3 idleR = new Vector3(-18f + stride * 13f, 0, 12f);
                swordRig.localRotation = Quaternion.Euler(24f, 0, -8f);
                if (acting)
                {
                    bool overhead = !actionBasic && (actionSkill == 2 || actionSkill == 7 || actionSkill == 9);
                    float direction = WeaponSwingSide;
                    spine.localRotation *= Pose(Vector3.zero, new Vector3(-8, direction * -29f, -7),
                        new Vector3(overhead ? 21f : 10f, direction * 34f, 9), t);
                    rightArm.localRotation = Pose(idleR,
                        overhead ? new Vector3(-158f, 12f, 18f) : new Vector3(-72f, -52f * direction, 67f * direction),
                        overhead ? new Vector3(-37f, -12f, -8f) : new Vector3(-48f, 65f * direction, -48f * direction), t);
                    rightElbow.localRotation = Pose(new Vector3(-16, 0, 0), new Vector3(-66, 0, 0), new Vector3(-4, 0, 0), t);
                    swordRig.localRotation = Pose(new Vector3(24, 0, -8), new Vector3(-32, 0, -18), new Vector3(65, 0, 10), t);
                    leftArm.localRotation = Pose(new Vector3(-10, 0, -8), new Vector3(-47, 0, -19), new Vector3(-34, 0, -22), t);
                    leftElbow.localRotation = Quaternion.Euler(-35f, 0, 0);
                    if(actionBasic && actionSkill == -2)
                    {
                        spine.localRotation = Pose(Vector3.zero,new Vector3(-5,-12,0),new Vector3(9,0,0),t);
                        rightArm.localRotation = Pose(idleR,new Vector3(-60,-8,12),new Vector3(-92,0,4),t);
                        rightElbow.localRotation = Pose(new Vector3(-16,0,0),new Vector3(-75,0,0),new Vector3(-3,0,0),t);
                        swordRig.localRotation = Pose(new Vector3(24,0,-8),new Vector3(140,0,0),new Vector3(180,0,0),t);
                    }
                }
                else rightArm.localRotation = Quaternion.Euler(idleR);
            }
            else if (heroClass == HeroClass.Arcanist || heroClass == HeroClass.Summoner)
            {
                staffRig.localRotation = Quaternion.Euler(12, 0, -12);
                if(acting&&!actionBasic)ApplyCasterSkillPose(t);
                else if (acting)
                {
                    spine.localRotation *= Pose(Vector3.zero, new Vector3(-9, -13, -3), new Vector3(14, 16, 3), t);
                    rightArm.localRotation = Pose(new Vector3(-10, 0, 10), new Vector3(-115, -12, 32),
                        new Vector3(-64, 22, 17), t);
                    rightElbow.localRotation = Pose(new Vector3(-12, 0, 0), new Vector3(-42, 0, 0), new Vector3(-9, 0, 0), t);
                    staffRig.localRotation = Pose(new Vector3(12, 0, -12), new Vector3(85, 0, -20), new Vector3(118, 0, -5), t);
                    leftArm.localRotation = Pose(new Vector3(-12, 0, -10), new Vector3(-58, 12, -38), new Vector3(-84, 0, -17), t);
                    leftElbow.localRotation = Pose(new Vector3(-20, 0, 0), new Vector3(-78, 0, 0), new Vector3(-12, 0, 0), t);
                }
                float charge = acting ? Mathf.Sin(Mathf.Clamp01(t / .52f) * Mathf.PI) : .1f + breathing * .04f;
                if (castingOrb != null) castingOrb.localScale = Vector3.one * (.21f + charge * .18f);
            }
            else
            {
                float draw = acting && actionBasic ? BasicActionTimeline.BowDraw(t) : acting ? (t < .32f ? Mathf.SmoothStep(0, 1, t / .32f) :
                    t < .48f ? 1f - Mathf.SmoothStep(0, 1, (t - .32f) / .16f) : 0) : 0;
                float ready = acting ? Mathf.Min(1f, Mathf.Min(t / .16f, (1 - t) / .28f)) : .3f;
                spine.localRotation *= Quaternion.Euler(0, -13f * ready, -3f * ready);
                Vector3 bowHand = Vector3.Lerp(new Vector3(-.32f, .1f, .28f), new Vector3(-.035f, .45f, .415f), ready);
                AimArm(leftArm, leftElbow, bowHand, new Vector3(-1, -.5f, 0));
                bowRig.rotation = spine.rotation * Quaternion.Euler(0, 10f, -7f);
                bowRig.localPosition = new Vector3(0, -.23f, .04f) -
                    bowRig.localRotation * WeaponAnchorLocal(WeaponVisualAnchor.BowGrip);
                Vector3 nock = new Vector3(0, 0, .05f - draw * .30f);
                bowstring.SetPosition(1, nock);
                arrowRig.localPosition = nock;
                Vector3 stringHand = Vector3.Lerp(new Vector3(.30f, .04f, .22f),
                    spine.InverseTransformPoint(bowRig.TransformPoint(nock)), ready);
                AimArm(rightArm, rightElbow, stringHand, new Vector3(1, -.2f, -.2f));
                arrowRig.gameObject.SetActive(actionBasic ? BasicActionTimeline.ArrowVisible(t, acting) : !acting || t < .44f || t > .83f);
            }
            ApplyAuthoredVanguardPose(acting,t,hurt);
            ApplyVisualRecovery(dt);
            if (decoration != null) decoration.Rotate(0, dt * (acting ? 145f : 42f), 0, Space.Self);
        }

        public void AnimateCharge(float progress) { AnimateCharge(progress,actionSkill); }
        public void AnimateCharge(float progress,int skill)
        {
            pilotCharging=true;SetBlenderPilotVisible(false);
            if(float.IsNaN(progress)||float.IsInfinity(progress))return;
            if (!isHero || spine == null) return;
            float ready = .3f + .7f * Mathf.SmoothStep(0, 1, Mathf.Clamp01(progress));
            spine.localRotation = Quaternion.Euler(-7f * ready, -12f * ready, 0);
            headRig.localRotation = Quaternion.Euler(4f * ready, 8f * ready, 0);
            if (heroClass == HeroClass.Vanguard)
            {
                rightArm.localRotation = Quaternion.Euler(Mathf.Lerp(-18, -150, ready), 12, 18);
                rightElbow.localRotation = Quaternion.Euler(-55f * ready, 0, 0);
                swordRig.localRotation = Quaternion.Euler(Mathf.Lerp(24, -25, ready), 0, -14);
                leftArm.localRotation = Quaternion.Euler(-50f * ready, 0, -20);
                leftElbow.localRotation = Quaternion.Euler(-35f, 0, 0);
            }
            else if (heroClass == HeroClass.Arcanist || heroClass == HeroClass.Summoner)
            {
                // Charge samples the same family curve up to the exact committed release
                // coordinate; the caller supplies the real skill before actionSkill changes.
                spine.localRotation=Quaternion.identity;
                float poseTime=SkillDamageBudgets.SkillPoseStart(heroClass,skill,true)*Mathf.SmoothStep(0,1,Mathf.Clamp01(progress));
                ApplyCasterSkillPose(poseTime,skill);
                if (castingOrb != null) castingOrb.localScale = Vector3.one * Mathf.Lerp(.23f, .49f, ready);
            }
            else
            {
                Vector3 bowHand = Vector3.Lerp(new Vector3(-.32f, .1f, .28f), new Vector3(-.035f, .45f, .415f), ready);
                AimArm(leftArm, leftElbow, bowHand, new Vector3(-1, -.5f, 0));
                bowRig.rotation = spine.rotation * Quaternion.Euler(0, 10, -7);
                bowRig.localPosition = new Vector3(0, -.23f, .04f) - bowRig.localRotation * WeaponAnchorLocal(WeaponVisualAnchor.BowGrip);
                Vector3 nock = new Vector3(0, 0, .05f - ready * .3f);
                bowstring.SetPosition(1, nock);
                arrowRig.localPosition = nock;
                arrowRig.gameObject.SetActive(true);
                AimArm(rightArm, rightElbow, spine.InverseTransformPoint(bowRig.TransformPoint(nock)), new Vector3(1, -.2f, -.2f));
            }
        }

        private static void AimArm(Transform shoulder, Transform elbow, Vector3 target, Vector3 bendHint)
        {
            const float upper = .30f, lower = .23f;
            Vector3 delta = target - shoulder.localPosition;
            float distance = Mathf.Clamp(delta.magnitude, .08f, upper + lower - .005f);
            Vector3 direction = delta.sqrMagnitude > .00001f ? delta.normalized : Vector3.forward;
            Vector3 bend = Vector3.ProjectOnPlane(bendHint, direction).normalized;
            if (bend.sqrMagnitude < .01f) bend = Vector3.down;
            float along = (upper * upper - lower * lower + distance * distance) / (2f * distance);
            Vector3 upperVector = direction * along + bend * Mathf.Sqrt(Mathf.Max(0, upper * upper - along * along));
            Vector3 lowerVector = direction * distance - upperVector;
            shoulder.localRotation = Quaternion.FromToRotation(Vector3.down, upperVector);
            elbow.localRotation = Quaternion.Inverse(shoulder.localRotation) * Quaternion.FromToRotation(Vector3.down, lowerVector);
        }

        private void BuildEnemy(EnemyKind kind, bool boss)
        {
            Color dark = new Color(.18f,.16f,.25f);
            if (kind == EnemyKind.Slime)
            {
                slime=true;
                body=Part("Slime Body",PrimitiveType.Sphere,new Vector3(0,.51f,0),new Vector3(1.16f,.98f,1.03f),new Color(.42f,.74f,.39f));
                Part("Slime Crown",PrimitiveType.Sphere,new Vector3(.15f,1f,-.06f),new Vector3(.35f,.3f,.3f),new Color(.62f,.9f,.48f));
                for(int i=-1;i<=1;i+=2)
                {
                    Part("Eye White",PrimitiveType.Sphere,new Vector3(i*.22f,.64f,.44f),new Vector3(.23f,.28f,.1f),Color.white);
                    Part("Eye Pupil",PrimitiveType.Sphere,new Vector3(i*.22f,.64f,.5f),new Vector3(.1f,.15f,.06f),dark);
                }
                Part("Smile",PrimitiveType.Cube,new Vector3(0,.4f,.5f),new Vector3(.24f,.045f,.035f),dark);
            }
            else if(kind == EnemyKind.Wisp)
            {
                floating=true;
                body=Part("Spirit Core",PrimitiveType.Sphere,new Vector3(0,1.4f,0),new Vector3(.74f,.87f,.74f),new Color(.57f,.42f,.93f));
                decoration=Part("Spirit Crown",PrimitiveType.Cube,new Vector3(0,1.96f,0),new Vector3(.24f,.24f,.24f),new Color(.91f,.7f,1f));
                for(int i=-1;i<=1;i+=2)
                    Part("Spirit Eyes",PrimitiveType.Sphere,new Vector3(i*.17f,1.47f,.33f),new Vector3(.1f,.13f,.07f),new Color(1f,.85f,.98f));
                for(int i=0;i<3;i++) Part("Spirit Tail",PrimitiveType.Sphere,new Vector3(0,.88f-i*.2f,-i*.12f),Vector3.one*(.4f-i*.09f),new Color(.39f,.29f,.62f));
            }
            else
            {
                bool brute=kind==EnemyKind.Guardian;
                Color skin=brute?new Color(.57f,.39f,.31f):new Color(.4f,.62f,.28f);
                Humanoid(skin,dark,brute?new Color(.43f,.48f,.57f):new Color(.49f,.31f,.18f),brute?1.45f:1f);
                for(int i=-1;i<=1;i+=2)
                {
                    Transform horn=Part(brute?"Stone Horn":"Long Ear",PrimitiveType.Capsule,new Vector3(i*.31f,2.12f,0),new Vector3(.13f,.22f,.13f),brute?new Color(.93f,.84f,.66f):skin);
                    horn.localRotation=Quaternion.Euler(0,0,-i*40f);
                }
                if(brute)
                {
                    Part("Iron Crown",PrimitiveType.Cube,new Vector3(0,2.15f,0),new Vector3(.62f,.24f,.48f),new Color(.3f,.32f,.41f));
                    Part("Crown Crystal",PrimitiveType.Sphere,new Vector3(0,2.24f,.26f),new Vector3(.19f,.23f,.12f),new Color(1f,.34f,.22f));
                    Part("Hammer Grip",PrimitiveType.Cylinder,new Vector3(0,-.11f,.1f),new Vector3(.14f,.65f,.14f),new Color(.37f,.22f,.14f),rightArm);
                    Part("Great Hammer",PrimitiveType.Cube,new Vector3(0,.55f,.1f),new Vector3(.85f,.42f,.42f),new Color(.56f,.57f,.64f),rightArm);
                }
                else
                {
                    Part("Goblin Knife",PrimitiveType.Cube,new Vector3(0,-.2f,.13f),new Vector3(.14f,.6f,.065f),new Color(.72f,.75f,.73f),rightArm);
                    Part("Leather Cap",PrimitiveType.Sphere,new Vector3(0,2.15f,-.03f),new Vector3(.55f,.3f,.5f),new Color(.45f,.27f,.17f));
                }
                transform.localScale=Vector3.one*(brute?(boss?1.48f:1.15f):.8f);
            }
        }

        private void EnhanceEnemy(EnemyKind kind, bool boss)
        {
            if (kind == EnemyKind.Slime)
            {
                Part("Slime soft highlight", PrimitiveType.Sphere, new Vector3(-.23f,.84f,.29f),
                    new Vector3(.2f,.09f,.1f), new Color(.79f,.95f,.59f));
                return;
            }
            if (kind == EnemyKind.Wisp)
            {
                for (int side = -1; side <= 1; side += 2)
                    Tapered("Spirit swept horn", transform, new Vector3(side*.31f,1.7f,0), .11f,.012f,.4f,
                        new Vector3(side*.2f,0,-.12f), new Color(.74f,.64f,1f), 12);
                return;
            }
            bool guardian = kind == EnemyKind.Guardian;
            Color metal = guardian ? new Color(.38f,.43f,.53f) : new Color(.38f,.28f,.19f);
            if (guardian)
            {
                // A heavy chest shell and broad stepped shoulders read at gameplay zoom.
                Part("Guardian chest plate", PrimitiveType.Cube, new Vector3(0,1.35f,.35f),
                    new Vector3(.99f,.56f,.22f), metal);
                Part("Guardian ember crystal", PrimitiveType.Sphere, new Vector3(0,1.43f,.49f),
                    new Vector3(.18f,.26f,.08f), new Color(1f,.38f,.18f));
                for (int side = -1; side <= 1; side += 2)
                {
                    Transform shoulder = side < 0 ? leftArm : rightArm;
                    Part("Guardian layered pauldron", PrimitiveType.Cube, new Vector3(side*.1f,.08f,0),
                        new Vector3(.56f,.23f,.58f), metal, shoulder).localRotation = Quaternion.Euler(0,0,side*12f);
                    if (boss) Tapered("Guardian crown spike", transform, new Vector3(side*.28f,2.24f,0),
                        .085f,.008f,.35f,new Vector3(side*.09f,0,-.04f),new Color(.84f,.64f,.37f),12);
                }
            }
            else
            {
                Part("Goblin brow left", PrimitiveType.Capsule, new Vector3(-.12f,2.075f,.23f),
                    new Vector3(.055f,.1f,.045f), new Color(.24f,.36f,.16f)).localRotation = Quaternion.Euler(0,0,60f);
                Part("Goblin brow right", PrimitiveType.Capsule, new Vector3(.12f,2.075f,.23f),
                    new Vector3(.055f,.1f,.045f), new Color(.24f,.36f,.16f)).localRotation = Quaternion.Euler(0,0,-60f);
                Part("Goblin nose", PrimitiveType.Sphere, new Vector3(0,1.98f,.28f),new Vector3(.13f,.14f,.17f),new Color(.42f,.59f,.25f));
            }
            spine = Joint("Enemy spine", new Vector3(0,1.12f,0));
            pelvis = Joint("Enemy pelvis", new Vector3(0,.83f,0));
            var parts = new List<Transform>();
            foreach (Transform part in transform) if (part != spine && part != pelvis) parts.Add(part);
            foreach (Transform part in parts) part.SetParent(part == leftLeg || part == rightLeg ? pelvis : spine, true);
            headRig = NewJoint("Enemy neck", spine, new Vector3(0,.68f,0));
            foreach (Transform part in parts)
                if (part.name == "Head" || part.name.StartsWith("Eyes") || part.name.Contains("Crown") ||
                    part.name.Contains("crown spike") || part.name.Contains("Horn") || part.name.Contains("Ear") ||
                    part.name.Contains("brow") || part.name.Contains("nose") || part.name.Contains("Cap")) part.SetParent(headRig,true);
            leftElbow = ArticulateArm(leftArm,metal); rightElbow = ArticulateArm(rightArm,metal);
            leftKnee = ArticulateLeg(leftLeg,metal); rightKnee = ArticulateLeg(rightLeg,metal);
            articulatedEnemy = true;
        }

        public void Animate(float speed, float attack, bool hurt)
        {
            if (isHero) { pilotCharging=false; AnimateHero(speed, attack, hurt, Time.deltaTime); return; }
            RestoreKnockdownBase();
            // Rebuild the authored pose each frame; recoil is an additive layer, never its replacement.
            if (body != null) body.localRotation = bodyRestRotation;
            if (largeBossRig != null) { transform.localPosition = Vector3.zero; largeBossRig.Animate(speed, attack); ApplyRecoil(); return; }
            float dt = Time.deltaTime;
            smoothedSpeed = Mathf.Lerp(smoothedSpeed, Mathf.Clamp01(speed), 1f-Mathf.Exp(-dt*10f));
            if (articulatedEnemy) { gaitPhase=locomotion.Phase;smoothedSpeed=locomotion.Speed; }
            else gaitPhase += dt * Mathf.Lerp(2.2f,10.2f,smoothedSpeed);
            float stride = Mathf.Sin(gaitPhase), walk = stride * smoothedSpeed * 27f;
            if (slime)
            {
                float bounce = Mathf.Sin(gaitPhase + phase);
                float squash = bounce * (.035f + smoothedSpeed*.055f) - attack*.055f;
                body.localScale = new Vector3(1.16f*(1-squash*.5f),.98f*(1+squash),1.03f*(1-squash*.5f));
                transform.localPosition = new Vector3(0,Mathf.Max(0,bounce)*(.025f+smoothedSpeed*.09f),0);
            }
            else if (floating)
            {
                transform.localPosition = Vector3.up*(Mathf.Sin(Time.time*2.5f+phase)*.11f);
                if (body != null)
                {
                    body.localRotation = Quaternion.Euler(stride*3f-attack*16f,0,Mathf.Sin(Time.time*1.7f+phase)*5f);
                    if (companionBodyScale.sqrMagnitude > 0) body.localScale = Vector3.Scale(companionBodyScale, new Vector3(1-attack*.2f,1+attack*.22f,1-attack*.2f));
                }
            }
            else if (quadruped)
            {
                for (int i=0;i<4;i++)
                {
                    float legPhase = gaitPhase + (i == 0 || i == 3 ? 0 : Mathf.PI);
                    paws[i].localRotation = Quaternion.Euler(Mathf.Sin(legPhase)*smoothedSpeed*29f + attack*(i<2?-38f:28f),0,0);
                    paws[i].localPosition = pawOrigins[i] + Vector3.up*Mathf.Max(0,Mathf.Cos(legPhase))*.04f*smoothedSpeed;
                }
                if (tailRig != null) tailRig.localRotation = Quaternion.Euler(stride*5f,Mathf.Sin(gaitPhase*.65f)*16f,0);
                // The release frame is the bite contact; recovery opens the jaw
                // and settles the local pounce without moving the navigation body.
                if (wolfJaw != null) wolfJaw.localRotation = Quaternion.Euler(-Mathf.Sin(attack*Mathf.PI)*26f,0,0);
                transform.localPosition = new Vector3(0,Mathf.Abs(stride)*.035f*smoothedSpeed+Mathf.Sin(attack*Mathf.PI)*.12f,attack*.15f);
            }
            else
            {
                if (leftLeg != null) leftLeg.localRotation=Quaternion.Euler(walk,0,-2f);
                if (rightLeg != null) rightLeg.localRotation=Quaternion.Euler(-walk,0,2f);
                if (leftArm != null) leftArm.localRotation=Quaternion.Euler(-walk*.6f,0,-8f);
                if (articulatedEnemy)
                {
                    // Explicit controller phase: the contact pose appears on the real impact frame.
                    float signedStride = Mathf.Sin(gaitPhase)*locomotion.Forward;
                    float sideStride = Mathf.Sin(gaitPhase)*locomotion.Side;
                    leftLeg.localRotation=Quaternion.Euler(signedStride*27,0,-2+sideStride*22);
                    rightLeg.localRotation=Quaternion.Euler(-signedStride*27,0,2-sideStride*22);
                    float chargeBrace = EnemyActionPose.Charge(enemyActionPhase);
                    float release = EnemyActionPose.Contact(enemyActionPhase,enemyActionProgress);
                    float windup = EnemyActionPose.Windup(enemyActionPhase,enemyActionProgress);
                    spine.localRotation=Quaternion.Euler(smoothedSpeed*6f-windup*12f+release*13f+chargeBrace*23f,windup*-18f+release*19f, -stride*2f*smoothedSpeed);
                    headRig.localRotation=Quaternion.Euler(windup*8f-chargeBrace*12f,windup*10f,-stride*1.5f*smoothedSpeed);
                    pelvis.localRotation=Quaternion.Euler(0,stride*-4f*smoothedSpeed,stride*2f*smoothedSpeed);
                    rightArm.localRotation=Quaternion.Euler(walk*.5f-windup*133f-release*26f-chargeBrace*58f,windup*-12f,12f+windup*23f);
                    rightElbow.localRotation=Quaternion.Euler(-12f-windup*49f+release*8f-chargeBrace*43f,0,0);
                    leftElbow.localRotation=Quaternion.Euler(-18f-windup*20f-chargeBrace*32f,0,0);
                    leftArm.localRotation=Quaternion.Euler(-walk*.6f-chargeBrace*42f,0,-8f-chargeBrace*14f);
                    leftKnee.localRotation=Quaternion.Euler(Mathf.Max(0,-stride)*smoothedSpeed*35f+chargeBrace*22f,0,0);
                    rightKnee.localRotation=Quaternion.Euler(Mathf.Max(0,stride)*smoothedSpeed*35f+chargeBrace*12f,0,0);
                }
                else if (treantCompanion)
                {
                    float recover = Mathf.Sin(Mathf.Clamp01(attack)*Mathf.PI);
                    if (leftArm != null) leftArm.localRotation=Quaternion.Euler(-attack*48f-recover*72f,0,-10f);
                    if (rightArm != null) rightArm.localRotation=Quaternion.Euler(-attack*48f-recover*72f,0,10f);
                    if (body != null) body.localRotation=Quaternion.Euler(attack*15f,0,0);
                }
                else if(rightArm!=null) rightArm.localRotation=Quaternion.Euler(attack>0?-75f+attack*160f:walk*.6f,attack>0?-40f:0,8f);
                transform.localPosition=Vector3.up*(Mathf.Abs(stride)*.028f*smoothedSpeed);
            }
            if(decoration!=null) decoration.Rotate(0,dt*65f,0,Space.Self);
            ApplyRecoil();
            ApplyCompanionPose();
            if (enemyOwner != null && enemyOwner.StatusEffects != null)
                transform.localPosition += Vector3.up * enemyOwner.StatusEffects.AirborneHeight;
        }

        private void OnDestroy()
        {
            ReleasePilotRendererGroup();
            foreach(Material material in palette.Values) if(material!=null) Destroy(material);
        }
    }

    internal sealed class OwnedCombatMesh : MonoBehaviour
    {
        public Mesh Value;
        private void OnDestroy() { if(Value!=null) Destroy(Value); }
    }
}
