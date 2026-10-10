using System;
namespace Emberfall
{
    // Value snapshots observe in-place equipment changes without retaining mutable items or allocating keys.
    public readonly struct CollectionPreviewAppearance : IEquatable<CollectionPreviewAppearance>
    {
        private readonly HeroClass hero;
        private readonly EquipmentSnapshot weapon,armor,relic;
        private readonly FashionSnapshot wings,fashionWeapon;
        public CollectionPreviewAppearance(HeroClass hero,ItemData weapon,ItemData armor,ItemData relic,FashionData wings,FashionData fashionWeapon)
        {this.hero=hero;this.weapon=new EquipmentSnapshot(weapon);this.armor=new EquipmentSnapshot(armor);this.relic=new EquipmentSnapshot(relic);this.wings=new FashionSnapshot(wings);this.fashionWeapon=new FashionSnapshot(fashionWeapon);}
        public bool Equals(CollectionPreviewAppearance other)
        {return hero==other.hero&&weapon.Equals(other.weapon)&&armor.Equals(other.armor)&&relic.Equals(other.relic)&&wings.Equals(other.wings)&&fashionWeapon.Equals(other.fashionWeapon);}
        private readonly struct EquipmentSnapshot
        {
            readonly bool present;readonly string id;readonly int level,rarity,upgrade,mechanic,variant;
            public EquipmentSnapshot(ItemData item)
            {present=item!=null;id=item==null?null:item.id;level=item==null?0:item.level;rarity=item==null?0:(int)item.rarity;upgrade=item==null?0:item.upgradeLevel;mechanic=item==null?0:(int)item.mechanic;variant=item==null?0:item.mechanicVariant;}
            public bool Equals(EquipmentSnapshot other)
            {return present==other.present&&id==other.id&&level==other.level&&rarity==other.rarity&&upgrade==other.upgrade&&mechanic==other.mechanic&&variant==other.variant;}
        }
        private readonly struct FashionSnapshot
        {
            readonly bool present;readonly string id;readonly int slot,rarity,appearance,upgrade;
            public FashionSnapshot(FashionData item){present=item!=null;id=item==null?null:item.id;slot=item==null?0:(int)item.slot;rarity=item==null?0:(int)item.rarity;appearance=item==null?0:(int)item.VisualRarity;upgrade=item==null?0:item.upgradeRank;}
            public bool Equals(FashionSnapshot other){return present==other.present&&id==other.id&&slot==other.slot&&rarity==other.rarity&&appearance==other.appearance&&upgrade==other.upgrade;}
        }
    }
    public sealed class CollectionPreviewState
    {
        private CollectionPreviewAppearance appearance;
        private bool hasAppearance,dirty=true;
        private int lastRenderedFrame=-1;
        public bool NeedsModel {get;private set;}=true;
        public float Yaw {get;private set;}=20;
        public void Observe(CollectionPreviewAppearance value)
        {if(hasAppearance&&appearance.Equals(value))return;appearance=value;hasAppearance=true;NeedsModel=true;dirty=true;}
        public void SetYaw(float value)
        {
            if(float.IsNaN(value)||float.IsInfinity(value))return;
            float normalized=value%360;if(normalized<0)normalized+=360;
            if(normalized==Yaw)return;Yaw=normalized;dirty=true;
        }
        public void Invalidate(){dirty=true;}
        // The old frame reservation belongs to lost pixels, not the new native surface.
        public void InvalidateTexture(){dirty=true;lastRenderedFrame=-1;}
        public bool ShouldRender(bool repaint,int frame){return repaint&&dirty&&frame!=lastRenderedFrame;}
        public void ModelReady(){NeedsModel=false;}
        public void Rendered(int frame){lastRenderedFrame=frame;dirty=false;}
    }
}
