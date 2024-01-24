using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cannabismod
{
    public class Core : ModSystem
    { 
        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterItemClass("JointLit",typeof(ItemJointLit));
        }
    }
	
	public class ItemJointLit : Item
	{
	
		float curX;
        float curY;
        
		public static SimpleParticleProperties smokeParticles;
		
        float prevSecUsed;
        LCGRandom rnd;
	
		/*
		static ItemJointLit()
        {
            
            ItemJointLit.smokeParticles = new SimpleParticleProperties(9f, 14f, ColorUtil.ToRgba(180, 110, 110, 80), new Vec3d(-0.3f, -0.3f, -0.3f),
            new Vec3d(0.3f, 0.3f, 0.3f), new Vec3f(-0.125f, 0.01f, -0.125f),
            new Vec3f(0.125f, 0.3f, 0.125f), 2f, -0.008f, 1.0f, 1.9f, EnumParticleModel.Quad);
            ItemJointLit.smokeParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.25f);
		    ItemJointLit.smokeParticles.SelfPropelled = true;
            ItemJointLit.smokeParticles.WindAffectednes = 0.7f;
            ItemJointLit.smokeParticles.OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.25f);
            
        }
		*/
		
        public override void OnLoaded(ICoreAPI api)
        {
			base.OnLoaded(api);
			
			rnd = new LCGRandom(api.World.Seed);
		
            if (api.Side != EnumAppSide.Client) return;
            ICoreClientAPI capi = api as ICoreClientAPI;
            
            // Ugh this is going to be ugly
            ItemJointLit.smokeParticles = new SimpleParticleProperties(this.Attributes["smokePNumMin"].AsFloat(9f), this.Attributes["smokePNumMax"].AsFloat(14f),
            ColorUtil.ToRgba(this.Attributes["smokePColorR"].AsInt(190), this.Attributes["smokePColorG"].AsInt(140), this.Attributes["smokePColorB"].AsInt(140),
            this.Attributes["smokePColorA"].AsInt(70)), new Vec3d(this.Attributes["smokePPosMinX"].AsFloat(-0.4f), this.Attributes["smokePPosMinY"].AsFloat(-0.4f), 
            this.Attributes["smokePPosMinZ"].AsFloat(-0.4f)), new Vec3d(this.Attributes["smokePPosMaxX"].AsFloat(0.4f), this.Attributes["smokePPosMaxY"].AsFloat(0.4f), 
            this.Attributes["smokePPosMaxZ"].AsFloat(0.4f)), new Vec3f(this.Attributes["smokePVelMinX"].AsFloat(-0.125f), this.Attributes["smokePVelMinY"].AsFloat(0.01f), 
            this.Attributes["smokePVelMinZ"].AsFloat(-0.125f)), new Vec3f(this.Attributes["smokePVelMaxX"].AsFloat(0.125f), this.Attributes["smokePVelMaxY"].AsFloat(0.3f), 
            this.Attributes["smokePVelMaxZ"].AsFloat(0.125f)), this.Attributes["smokePLifeTime"].AsFloat(2f), this.Attributes["smokePGrav"].AsFloat(-0.04f), 
            this.Attributes["smokePSizeMin"].AsFloat(1.0f), this.Attributes["smokePSizeMax"].AsFloat(1.9f), EnumParticleModel.Quad);
            ItemJointLit.smokeParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, this.Attributes["smokePSizeEvolve"].AsFloat(-0.25f));
		    ItemJointLit.smokeParticles.SelfPropelled = true;
            ItemJointLit.smokeParticles.WindAffectednes = this.Attributes["smokePWindFactor"].AsFloat(1.2f);
            ItemJointLit.smokeParticles.OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, this.Attributes["smokePOpacityEvolve"].AsFloat(-0.25f));
		}
		
		public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (slot.Itemstack.TempAttributes.GetBool("consumed") == true) return;

            handling = EnumHandHandling.PreventDefault;

            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            if (byPlayer == null) return;

            byEntity.World.RegisterCallback((dt) =>
            {
                if (byEntity.Controls.HandUse == EnumHandInteract.HeldItemInteract)
                {
                    byPlayer.Entity.World.PlaySoundAt(new AssetLocation("sounds/player/messycraft"), byPlayer, byPlayer);
                }
            }, 250);
        }
		
		public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (byEntity.World is IClientWorldAccessor)
            {
                ModelTransform tf = new ModelTransform();
                tf.EnsureDefaultValues();

                float nowx = 0, nowy = 0;

                if (secondsUsed > 0.5f)
                {
                    int cnt = (int)(secondsUsed * 10);
                    rnd.InitPositionSeed(cnt, 0);

                    float targetx = 0.8f * (rnd.NextFloat() - 0.5f);
                    float targety = 0.5f * (rnd.NextFloat() - 0.5f);

                    float dt = secondsUsed - prevSecUsed;

                    nowx = (curX - targetx) * dt * 2;
                    nowy = (curY - targety) * dt * 2;
                }

                tf.Translation.Set(nowx - Math.Min(1.5f, secondsUsed*4), nowy, 0);
                byEntity.Controls.UsingHeldItemTransformBefore = tf;

                curX = nowx;
                curY = nowy;

                prevSecUsed = secondsUsed;
            }

            if (api.World.Side == EnumAppSide.Server) return true;

            return secondsUsed < 4.0f;
        }
		
		public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            return false;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (secondsUsed > 4.0f)
            {
                if (api.Side == EnumAppSide.Server)
                {
                    slot.TakeOut(1);
                    slot.MarkDirty();
					ItemJointLit.smokeParticles.MinPos = byEntity.SidedPos.AheadCopy(this.Attributes["smokePVelFOffset"].AsDouble(2.0)).XYZ.Add(0.0, byEntity.LocalEyePos.Y, 0.0);
					byEntity.World.SpawnParticles(ItemJointLit.smokeParticles, null);
					if (byEntity.GetBehavior<EntityBehaviorTemporalStabilityAffected>() != null)
					{
						double stability = byEntity.WatchedAttributes.GetDouble("temporalStability");
						byEntity.WatchedAttributes.SetDouble("temporalStability",Math.Min(1.0,stability+0.30));
					}
                } else
                {
                    slot.Itemstack.TempAttributes.SetBool("consumed", true);
                }
            }
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return new WorldInteraction[]
            {
                new WorldInteraction()
                {
                    ActionLangCode = "heldhelp-smokejoint",
                    MouseButton = EnumMouseButton.Right
                }
            };
        }
	
	}
	
}
