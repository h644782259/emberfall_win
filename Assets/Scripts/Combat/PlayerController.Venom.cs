using UnityEngine;
namespace Emberfall
{
    public partial class PlayerController
    {
        private bool ConcentratedVenom
        {
            get
            {
                if (!HasMechanic(EquipmentMechanic.VenomSpread)) return false;
                var attachment=session.Progression.Attachment(EquipmentMechanic.VenomSpread);if(attachment!=null)return attachment.variantUnlocked&&attachment.variant==1;
                var item = session.Progression.Equipped(BuildCatalog.MechanicSlot(EquipmentMechanic.VenomSpread));
                return item != null && item.mechanicVariantUnlocked && item.mechanicVariant == 1;
            }
        }
        private void CastConcentratedVenom(int rank, float range, Color color, int castId)
        {
            // Capture intent once. Bounded steering never swaps targets or bypasses collision.
            EnemyController locked = AimTarget;
            Vector3 direction = transform.forward;
            var amount = Damage(ConcentratedVenomRules.DirectCoefficient(rank)*session.Progression.MechanicPowerMultiplier(EquipmentMechanic.VenomSpread));
            Vector3 muzzle = transform.position + direction * .6f;
            if (CombatProjectile.CanLaunchFromMuzzle(this, muzzle, amount, castId))
                CombatProjectile.Friendly(this, session, muzzle, direction, amount, color,
                    arrow:true, size:1f, velocity:20f*range, skillIndex:0, castId:castId,
                    concentratedTarget:locked, concentrated:true);
        }
    }
}
