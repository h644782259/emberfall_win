using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private CollectionModelPreview wearModel;
        private bool inventoryFashionOpen;
        private bool inventoryStatsVisible;
        private Vector2 inventoryStatsScroll;
        private MobilePanelLayout.Area InventoryArea(Rect area)
        {float u=TouchRatio;return new MobilePanelLayout.Area(area.x/u,area.y/u,area.width/u,area.height/u);}
        private void DrawCharacterStats(Rect area,float u)
        {
            var stats=session.Progression.GetStats();
            string[] labels={"攻击","防御","生命上限","移动速度","暴击率","暴击伤害","护甲减伤","额外减伤","能量上限","能量回复 / 秒"};
            string[] values={Mathf.RoundToInt(stats.Damage).ToString(),Mathf.RoundToInt(stats.Armor).ToString(),Mathf.RoundToInt(stats.MaxHealth).ToString(),stats.MoveSpeed.ToString("0.00"),(stats.CritChance*100).ToString("0.##")+"%",((1.65f+stats.CritDamageBonus)*100).ToString("0.##")+"%",((1-CombatBalance.ArmorDamageMultiplier(stats.Armor,session.Progression.Profile.level))*100).ToString("0.#")+"%",(stats.DamageReduction*100).ToString("0.#")+"%",SkillRuntime.MaximumEnergy.ToString("0"),(SkillRuntime.EnergyPerSecond*(1+stats.EnergyRecovery)).ToString("0.##")};
            float contentWidth=area.width-18*u,rowHeight=32*u;
            inventoryStatsScroll=BeginTouchScroll("inventory-character-stats",area,inventoryStatsScroll,new Rect(0,0,contentWidth,labels.Length*32*u));
            for(int i=0;i<labels.Length;i++)
            {
                Rect row=new Rect(0,i*rowHeight,contentWidth,rowHeight-2*u);if(i%2==0)Fill(row,new Color(.03f,.06f,.08f,.8f));
                Text(new Rect(5*u,row.y,row.width*.64f-5*u,row.height),labels[i],Mathf.RoundToInt(11*u),muted);
                Text(new Rect(row.width*.64f,row.y,row.width*.36f-5*u,row.height),values[i],Mathf.RoundToInt(12*u),i==0?gold:pale,true,false,TextAnchor.MiddleRight);
            }
            EndTouchScroll();
        }
        private void DrawCurrentWear(Rect area,float u)
        {
            var p=session.Progression;
            if(wearModel==null)wearModel=new CollectionModelPreview();
            bool fashion=mobileInventoryTab==3||inventoryFashionOpen;
            float equipmentSize=MobileControls.Active?Mathf.Min(60,(area.width/u-8)/3):44;
            Rect viewport=new Rect(area.x,area.y,area.width,Mathf.Max(64*u,area.height-(fashion?80:equipmentSize+8)*u));
            wearModel.SetCenterOnAvatar(true);wearModel.SetComposition(CollectionPreviewComposition.Full);wearModel.SetYaw(20);
            wearModel.SetViewport(viewport.width*Mathf.Abs(GUI.matrix.m00),viewport.height*Mathf.Abs(GUI.matrix.m11),MobileControls.Active);
            Texture current=wearModel.RenderSafe(p.Profile.heroClass,p.Equipped(ItemSlot.Weapon),p.Equipped(ItemSlot.Armor),p.Equipped(ItemSlot.Relic),p.EquippedFashion(FashionSlot.Wings),p.EquippedFashion(FashionSlot.Weapon));
            if(current!=null)GUI.DrawTexture(viewport,current,ScaleMode.ScaleToFit,false);
            else Text(viewport,wearModel.LastError==null?"角色预览正在恢复":"预览暂不可用，其他操作可继续",Mathf.RoundToInt(11*u),muted,false,true);
            if(fashion){DrawFashionWearSlots(area,u);return;}
            for(int slot=0;slot<3;slot++)
            {
                var item=p.Equipped((ItemSlot)slot);Rect r=new Rect(area.center.x-(equipmentSize*3+8)*u*.5f+slot*(equipmentSize+4)*u,area.yMax-equipmentSize*u,equipmentSize*u,equipmentSize*u);
                if(item!=null)DrawInventoryIcon(r,item,u);
                else
                {
                    Fill(r,card);Border(r,new Color(jade.r,jade.g,jade.b,.35f));
                    DrawIcon(new Rect(r.x+9*u,r.y+6*u,r.width-18*u,r.height-22*u),UIIconAtlas.EquipmentCardIcon((ItemSlot)slot),muted);
                    Text(new Rect(r.x,r.yMax-14*u,r.width,14*u),GameBalance.SlotName((ItemSlot)slot),Mathf.RoundToInt(9*u),muted,false,false,TextAnchor.MiddleCenter);
                }
                bool slotUpgrade=false;
                foreach(var candidate in p.Profile.inventory)if(candidate.slot==(ItemSlot)slot&&UnreviewedEquipmentUpgrade(candidate)){slotUpgrade=true;break;}
                if(slotUpgrade)DrawIcon(new Rect(r.xMax-18*u,r.yMax-31*u,18*u,18*u),UIIconAtlas.EquipmentUpgradeArrow(),new Color(.25f,1f,.4f));
                if(item!=null)DesktopInventoryGesture(r,r,item.id);
                if(MobileControls.Active&&QuietAction(r,"",item!=null&&!inventoryComparisonOpen))
                {mobileInventoryTab=0;OpenInventoryPopup(item.id,r);}
            }
        }
        private void DrawFashionWearSlots(Rect area,float u)
        {
            for(int i=0;i<2;i++)
            {
                FashionSlot slot=i==0?FashionSlot.Weapon:FashionSlot.Wings;
                var item=session.Progression.EquippedFashion(slot);
                float cell=(area.width-8*u)*.5f;
                Rect r=new Rect(area.x+i*(cell+8*u),area.yMax-76*u,cell,76*u);
                Color tint=item==null?muted:GameBalance.RarityColor(item.rarity);
                Fill(r,card);Border(r,tint,item==null?1:2);
                DrawIcon(new Rect(r.center.x-18*u,r.y+4*u,36*u,36*u),UIIconAtlas.FashionCardIcon(slot,item==null?3:(int)item.AppearanceRarity,session.Progression.Profile.heroClass),tint);
                string label=item==null?(slot==FashionSlot.Weapon?"兵装":"羽翼")+"\n未穿戴":ProgressionService.FashionName(item.slot,item.AppearanceRarity,session.Progression.Profile.heroClass);
                Text(new Rect(r.x+3*u,r.y+42*u,r.width-6*u,32*u),label,Mathf.RoundToInt(10*u),item==null?muted:pale,item!=null,true,TextAnchor.MiddleCenter);
                if(item!=null)
                {
                    Text(new Rect(r.xMax-18*u,r.y+2*u,16*u,16*u),"✓",Mathf.RoundToInt(11*u),jade,true);
                    DesktopInventoryGesture(r,r,"@fashion:"+item.id);
                    if(MobileControls.Active&&QuietAction(r,"",!inventoryComparisonOpen)){OpenInventoryPopup("@fashion:"+item.id,r);}
                }
            }
        }
        private void DrawBagFashion(MobilePanelLayout.Area area)
        {
            float u=MobileControls.Active?TouchRatio:1;Rect bounds=MobilePanelRect(area);
            var owned=new System.Collections.Generic.List<FashionData>(session.Progression.Profile.fashions);
            owned.RemoveAll(f=>f==null);owned.Sort((a,b)=>{int c=a.slot.CompareTo(b.slot);if(c==0)c=b.rarity.CompareTo(a.rarity);return c!=0?c:string.CompareOrdinal(a.id,b.id);});
            var grid=new InventoryGridGeometry(bounds.width/u-18,MobileControls.Active?60:44);float h=Mathf.Max(bounds.height,((owned.Count+grid.Columns-1)/grid.Columns)*grid.Stride*u);
            bool prior=GUI.enabled;GUI.enabled=prior&&(!MobileControls.Active||!inventoryComparisonOpen)&&inventoryPopupDismissed!=Time.frameCount;
            Vector2 before=mobileFashionScroll;mobileFashionScroll=BeginTouchScroll("inventory-fashion-grid",bounds,mobileFashionScroll,new Rect(0,0,bounds.width-18*u,h));
            string chosen=null;Rect anchor=default;
            for(int i=0;i<owned.Count;i++)
            {
                var f=owned[i];var cell=grid.Tile(i);Rect tile=new Rect(cell.X*u,cell.Y*u,cell.Width*u,cell.Height*u);
                if(tile.yMax<mobileFashionScroll.y||tile.y>mobileFashionScroll.y+bounds.height)continue;
                Color rarity=GameBalance.RarityColor(f.rarity);Fill(tile,card);Border(tile,rarity);
                DrawIcon(new Rect(tile.x+5*u,tile.y+5*u,tile.width-10*u,tile.height-17*u),UIIconAtlas.FashionCardIcon(f.slot,(int)f.AppearanceRarity,session.Progression.Profile.heroClass),rarity);
                for(int pip=0;pip<=(int)f.rarity;pip++)Fill(new Rect(tile.x+(3+pip*5)*u,tile.y+3*u,3*u,3*u),pale);
                var worn=session.Progression.EquippedFashion(f.slot);bool equipped=worn!=null&&worn.id==f.id;
                Text(new Rect(tile.x+2*u,tile.yMax-14*u,tile.width-4*u,14*u),(equipped?"✓ ":"")+(f.slot==FashionSlot.Wings?"翼":"刃"),Mathf.RoundToInt(9*u),pale,true,false,TextAnchor.MiddleRight);
                Rect screenTile=new Rect(bounds.x+tile.x,bounds.y+tile.y-mobileFashionScroll.y,tile.width,tile.height);
                DesktopInventoryGesture(tile,screenTile,"@fashion:"+f.id);
                if(GUI.Button(tile,GUIContent.none,invisibleButton)){if(MobileControls.Active){chosen="@fashion:"+f.id;anchor=screenTile;}else DesktopInventoryClick("@fashion:"+f.id);}
            }
            if(owned.Count==0)Text(new Rect(8*u,12*u,bounds.width-34*u,48*u),"暂无已拥有时装",Mathf.RoundToInt(13*u),muted);
            EndTouchScroll();GUI.enabled=prior;if(before!=mobileFashionScroll)inventoryComparisonOpen=false;
            if(chosen!=null)OpenInventoryPopup(chosen,anchor);DrawInventoryPopup(bounds,u);
        }

        private void DrawBagSupplies(MobilePanelLayout.Area area)
        {
            float u=MobileControls.Active?TouchRatio:1;Rect bounds=MobilePanelRect(area);
            float cellSize=MobileControls.Active?60:44;
            Rect tile=new Rect(bounds.x,bounds.y,cellSize*u,cellSize*u);Fill(tile,card);Border(tile,jade);
            DrawIcon(new Rect(tile.x+5*u,tile.y+3*u,tile.width-10*u,tile.height-17*u),UIIconAtlas.Utility("potion"),jade);
            Text(new Rect(tile.x,tile.yMax-14*u,tile.width-2*u,14*u),session.Progression.Profile.potions.ToString(),Mathf.RoundToInt(9*u),pale,true,false,TextAnchor.MiddleRight);
            bool prior=GUI.enabled;GUI.enabled=prior&&(!MobileControls.Active||!inventoryComparisonOpen)&&inventoryPopupDismissed!=Time.frameCount;
            DesktopInventoryGesture(tile,tile,"@potion");
            if(GUI.Button(tile,GUIContent.none,invisibleButton)){if(MobileControls.Active)OpenInventoryPopup("@potion",tile);else DesktopInventoryClick("@potion");}
            Rect stone=new Rect(bounds.x,bounds.y+(cellSize+12)*u,cellSize*u,cellSize*u);Fill(stone,card);Border(stone,gold);
            DrawIcon(new Rect(stone.x+5*u,stone.y+3*u,stone.width-10*u,stone.height-17*u),UIIconAtlas.Utility("gem"),gold);
            Text(new Rect(stone.x,stone.yMax-16*u,stone.width-3*u,16*u),session.Progression.Profile.refinementStones.ToString(),Mathf.RoundToInt(10*u),pale,true,false,TextAnchor.MiddleRight);
            Text(new Rect(stone.xMax+10*u,stone.y,bounds.width-stone.width-12*u,24*u),"装备洗练石",Mathf.RoundToInt(13*u),gold,true);
            GUI.enabled=prior;DrawInventoryPopup(bounds,u);
        }
    }
}
