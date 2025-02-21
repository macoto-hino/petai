using Vintagestory.API.Config;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using System.IO;
using Vintagestory.GameContent;

namespace PetAI
{
    public class EntityPet : EntityAgent
    {
        protected InventoryPetGear gearInv;

        public InventorySlotBound backpackInv;
        public override IInventory GearInventory => gearInv;
        public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
        {
            base.Initialize(properties, api, InChunkIndex3d);
            if (gearInv == null) gearInv = new InventoryPetGear(Code.Path, "petInv-" + EntityId, api);
            else gearInv.Api = api;
            gearInv.LateInitialize(gearInv.InventoryID, api);
            var slots = new ItemSlot[gearInv.Count];
            for (int i = 0; i < gearInv.Count; i++)
            {
                slots[i] = gearInv[i];
            }
            backpackInv = new InventorySlotBound("petBackPackInv-", api, slots);

            if (api.Side == EnumAppSide.Server)
            {
                GetBehavior<EntityBehaviorHealth>().onDamaged += (dmg, dmgSource) => applyPetArmor(dmg, dmgSource);
            }
        }


        public override void OnTesselation(ref Shape entityShape, string shapePathForLogging)
        {
            base.OnTesselation(ref entityShape, shapePathForLogging);
            foreach (var slot in GearInventory)
            {
                addGearToShape(slot, entityShape, shapePathForLogging);
            }
        }

        public override void FromBytes(BinaryReader reader, bool forClient)
        {
            base.FromBytes(reader, forClient);

            if (gearInv == null) { gearInv = new InventoryPetGear(Code.Path, "petInv-" + EntityId, null); }
            gearInv.FromTreeAttributes(getInventoryTree());
        }

        public override void ToBytes(BinaryWriter writer, bool forClient)
        {
            gearInv.ToTreeAttributes(getInventoryTree());

            base.ToBytes(writer, forClient);
        }

        public override string GetInfoText()
        {
            if (!HasBehavior<EntityBehaviorTameable>() || GetBehavior<EntityBehaviorTameable>().owner == null) return base.GetInfoText();

            return String.Concat(base.GetInfoText(),
                    "\n",
                    Lang.Get("petai:gui-pet-owner", GetBehavior<EntityBehaviorTameable>().owner?.PlayerName),
                    "\n",
                    Lang.Get("petai:gui-pet-obedience", Math.Round(GetBehavior<EntityBehaviorTameable>().obedience * 100, 2)));
        }

        public void DropInventoryOnGround()
        {
            for (int i = gearInv.Count - 1; i >= 0; i--)
            {
                if (gearInv[i].Empty) { continue; }

                Api.World.SpawnItemEntity(gearInv[i].TakeOutWhole(), ServerPos.XYZ);
                gearInv.MarkSlotDirty(i);
            }
        }

        private ITreeAttribute getInventoryTree()
        {
            if (!WatchedAttributes.HasAttribute("petinventory"))
            {
                ITreeAttribute tree = new TreeAttribute();
                gearInv.ToTreeAttributes(tree);
                WatchedAttributes.SetAttribute("petinventory", tree);
            }
            return WatchedAttributes.GetTreeAttribute("petinventory");
        }
        private float applyPetArmor(float dmg, DamageSource dmgSource)
        {
            if (dmgSource.SourceEntity != null && dmgSource.Type != EnumDamageType.Heal)
            {
                foreach (var slot in GearInventory)
                {
                    if (!slot.Empty)
                    {
                        dmg *= 1 - (slot.Itemstack.Item as ItemPetAccessory).damageReduction;
                    }
                }
            }
            return dmg;
        }
    }
}