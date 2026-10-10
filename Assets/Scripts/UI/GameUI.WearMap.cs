using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private CollectionModelPreview wearModel;
        private float wearPreviewYaw=20,wearDragX;
        private int wearDragFinger=-1000,wearDragFrame=-1,wearMouseControl;
        private void ResetWearRotationGesture()
        {
            if(wearMouseControl!=0&&GUIUtility.hotControl==wearMouseControl)GUIUtility.hotControl=0;
            wearMouseControl=0;wearDragFinger=-1000;wearDragFrame=-1;
        }
        private void HandleWearRotation(Rect viewport)
        {
            if(!GUI.enabled||inventoryComparisonOpen){ResetWearRotationGesture();return;}
            if(MobileControls.Active)
            {
                if(wearDragFrame==Time.frameCount)return;
                wearDragFrame=Time.frameCount;bool found=false;
                for(int i=0;i<Input.touchCount;i++)
                {
                    Touch touch=Input.GetTouch(i);Vector2 point=ScreenToUI(touch.position);
                    if(touch.phase==TouchPhase.Began&&wearDragFinger==-1000&&viewport.Contains(point)&&touchScroll.Finger==-1000)
                    {wearDragFinger=touch.fingerId;wearDragX=point.x;}
                    if(touch.fingerId!=wearDragFinger)continue;
                    found=true;
                    if(touch.phase==TouchPhase.Ended||touch.phase==TouchPhase.Canceled){wearDragFinger=-1000;continue;}
                    wearPreviewYaw=Mathf.Repeat(wearPreviewYaw-(point.x-wearDragX)*180/Mathf.Max(1,viewport.width),360);
                    wearDragX=point.x;
                }
                if(!found)wearDragFinger=-1000;
                return;
            }
            Event e=Event.current;int control=GUIUtility.GetControlID(FocusType.Passive);
            if(e.type==EventType.MouseDown&&e.button==0&&viewport.Contains(e.mousePosition)&&GUIUtility.hotControl==0)
            {wearMouseControl=control;GUIUtility.hotControl=control;wearDragX=e.mousePosition.x;e.Use();}
            else if(wearMouseControl==control&&GUIUtility.hotControl==control)
            {
                if(e.type==EventType.MouseDrag)
                {wearPreviewYaw=Mathf.Repeat(wearPreviewYaw-(e.mousePosition.x-wearDragX)*180/Mathf.Max(1,viewport.width),360);wearDragX=e.mousePosition.x;e.Use();}
                else if(e.type==EventType.MouseUp){ResetWearRotationGesture();e.Use();}
            }
        }
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
        private string wornLastClick;
        private float wornLastClickAt=-10;
        private void WornSlotClick(string id,ItemSlot? equipment,FashionSlot? fashion)
        {
            bool twice=wornLastClick==id&&Time.unscaledTime-wornLastClickAt<.4f;
            wornLastClick=id;wornLastClickAt=Time.unscaledTime;
            if(!twice)return;
            wornLastClick=null;
            bool saved=equipment.HasValue?session.Progression.Unequip(equipment.Value):session.Progression.UnequipFashion(fashion.Value);
            if(fashion.HasValue)MobileFashionResult(saved,"时装已脱下");else MobileInventoryResult(saved,"装备已脱下");
            if(saved){inventoryComparisonOpen=false;inventoryPopupItem=null;RebuildBagItems();CancelMobileScroll();}
        }

        private void DrawCurrentWear(Rect area,float u)
        {
            var p=session.Progression;
            if(wearModel==null)wearModel=new CollectionModelPreview();
            bool fashion=mobileInventoryTab==3||inventoryFashionOpen;
            float equipmentSize=MobileControls.Active?Mathf.Min(InventoryGridGeometry.MobileCellSize,(area.width/u-8)/3):InventoryGridGeometry.DesktopCellSize;
            Rect viewport=new Rect(area.x,area.y,area.width,Mathf.Max(64*u,area.height-(equipmentSize+8)*u));
            HandleWearRotation(viewport);
            // Fit the complete silhouette, including worn weapons and wings, in any aspect ratio.
            wearModel.SetEquipmentFraming(false,false);wearModel.SetCenterOnAvatar(false);
            wearModel.SetComposition(CollectionPreviewComposition.Full);wearModel.SetYaw(wearPreviewYaw);
            wearModel.SetViewport(viewport.width*Mathf.Abs(GUI.matrix.m00),viewport.height*Mathf.Abs(GUI.matrix.m11),MobileControls.Active);
            Texture current=wearModel.RenderSafe(p.Profile.heroClass,p.Equipped(ItemSlot.Weapon),p.Equipped(ItemSlot.Armor),p.Equipped(ItemSlot.Relic),p.EquippedFashion(FashionSlot.Wings),p.EquippedFashion(FashionSlot.Weapon));
            if(current!=null)GUI.DrawTexture(viewport,current,ScaleMode.ScaleToFit,false);
            else Text(viewport,wearModel.LastError==null?"角色预览正在恢复":"预览暂不可用，其他操作可继续",Mathf.RoundToInt(11*u),muted,false,true);
            if(!MobileControls.Active)Text(new Rect(viewport.x+6*u,viewport.y+4*u,90*u,24*u),"Lv."+p.Profile.level,Mathf.RoundToInt(15*u),gold,true);
            if(fashion){DrawFashionWearSlots(area,u);return;}
            for(int slot=0;slot<3;slot++)
            {
                var item=p.Equipped((ItemSlot)slot);Rect r=new Rect(area.center.x-(equipmentSize*3+8)*u*.5f+slot*(equipmentSize+4)*u,area.yMax-equipmentSize*u,equipmentSize*u,equipmentSize*u);
                if(item!=null){Fill(r,new Color(.035f,.095f,.11f));Border(r,GameBalance.RarityColor(item.rarity),2*u);DrawIcon(new Rect(r.x+4*u,r.y+2*u,r.width-8*u,r.height-16*u),UIIconAtlas.EquipmentCardIcon(item.slot,item.level,item.rarity,p.Profile.heroClass),GameBalance.RarityColor(item.rarity));Text(new Rect(r.x,r.yMax-15*u,r.width,14*u),GameBalance.SlotName(item.slot),Mathf.RoundToInt(10*u),pale,true,false,TextAnchor.MiddleCenter);}
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
                if(QuietAction(r,"",item!=null&&!inventoryComparisonOpen,"双击脱下装备"))
                    WornSlotClick(item.id,item.slot,null);
            }
        }
        private void DrawFashionWearSlots(Rect area,float u)
        {
            for(int i=0;i<2;i++)
            {
                FashionSlot slot=i==0?FashionSlot.Weapon:FashionSlot.Wings;
                var item=session.Progression.EquippedFashion(slot);
                float cell=(area.width-8*u)*.5f;
                Rect r=new Rect(area.x+i*(cell+8*u),area.yMax-(MobileControls.Active?InventoryGridGeometry.MobileCellSize:InventoryGridGeometry.DesktopCellSize)*u,cell,(MobileControls.Active?InventoryGridGeometry.MobileCellSize:InventoryGridGeometry.DesktopCellSize)*u);
                Color tint=item==null?muted:GameBalance.RarityColor(item.rarity);
                Fill(r,card);Border(r,tint,item==null?1:2);
                DrawIcon(new Rect(r.x+3*u,r.center.y-20*u,40*u,40*u),UIIconAtlas.FashionCardIcon(slot,item==null?3:(int)item.VisualRarity,session.Progression.Profile.heroClass),tint);
                string label=item==null?(slot==FashionSlot.Weapon?"兵装":"羽翼")+"\n未穿戴":ProgressionService.FashionName(item.slot,item.AppearanceRarity,session.Progression.Profile.heroClass);
                Text(new Rect(r.x+46*u,r.y,r.width-49*u,r.height),label,Mathf.RoundToInt(10*u),item==null?muted:pale,item!=null,true,TextAnchor.MiddleCenter);
                if(item!=null)
                {
                    Text(new Rect(r.xMax-18*u,r.y+2*u,16*u,16*u),"✓",Mathf.RoundToInt(11*u),jade,true);
                    DesktopInventoryGesture(r,r,"@fashion:"+item.id);
                    if(QuietAction(r,"",!inventoryComparisonOpen,"双击脱下时装"))WornSlotClick(item.id,null,item.slot);
                }
            }
        }
        private void DrawBagFashion(MobilePanelLayout.Area area)
        {
            float u=MobileControls.Active?TouchRatio:1;Rect bounds=MobilePanelRect(area);
            var owned=new System.Collections.Generic.List<FashionData>(session.Progression.Profile.fashions);
            owned.RemoveAll(f=>f==null||f.id==session.Progression.Profile.wingsFashionId||f.id==session.Progression.Profile.weaponFashionId);owned.Sort((a,b)=>{int c=a.slot.CompareTo(b.slot);if(c==0)c=b.rarity.CompareTo(a.rarity);return c!=0?c:string.CompareOrdinal(a.id,b.id);});
            var grid=new InventoryGridGeometry(bounds.width/u-18,MobileControls.Active?InventoryGridGeometry.MobileCellSize:InventoryGridGeometry.DesktopCellSize);float h=Mathf.Max(bounds.height,((owned.Count+grid.Columns-1)/grid.Columns)*grid.Stride*u);
            bool prior=GUI.enabled;GUI.enabled=prior&&(!MobileControls.Active||!inventoryComparisonOpen)&&inventoryPopupDismissed!=Time.frameCount;
            Vector2 before=mobileFashionScroll;mobileFashionScroll=BeginTouchScroll("inventory-fashion-grid",bounds,mobileFashionScroll,new Rect(0,0,bounds.width-18*u,h));
            string chosen=null;Rect anchor=default;
            for(int i=0;i<owned.Count;i++)
            {
                var f=owned[i];var cell=grid.Tile(i);Rect tile=new Rect(cell.X*u,cell.Y*u,cell.Width*u,cell.Height*u);
                if(tile.yMax<mobileFashionScroll.y||tile.y>mobileFashionScroll.y+bounds.height)continue;
                Color rarity=GameBalance.RarityColor(f.rarity);Fill(tile,card);Border(tile,rarity);
                DrawIcon(new Rect(tile.x+5*u,tile.y+5*u,tile.width-10*u,tile.height-17*u),UIIconAtlas.FashionCardIcon(f.slot,(int)f.VisualRarity,session.Progression.Profile.heroClass),rarity);
                for(int pip=0;pip<=(int)f.rarity;pip++)Fill(new Rect(tile.x+(3+pip*5)*u,tile.y+3*u,3*u,3*u),pale);
                var worn=session.Progression.EquippedFashion(f.slot);bool equipped=worn!=null&&worn.id==f.id;
                Text(new Rect(tile.x+2*u,tile.yMax-14*u,tile.width-4*u,14*u),(f.slot==FashionSlot.Wings?"翼":"刃"),Mathf.RoundToInt(9*u),pale,true,false,TextAnchor.MiddleRight);
                if(equipped)DrawWornIconBadge(tile,u,"");
                Rect screenTile=new Rect(bounds.x+tile.x,bounds.y+tile.y-mobileFashionScroll.y,tile.width,tile.height);
                DesktopInventoryGesture(tile,screenTile,"@fashion:"+f.id);
                if(GUI.Button(tile,GUIContent.none,invisibleButton)){if(MobileControls.Active){chosen="@fashion:"+f.id;anchor=screenTile;}else DesktopInventoryClick("@fashion:"+f.id);}
            }
            if(owned.Count==0)Text(new Rect(8*u,12*u,bounds.width-34*u,48*u),"暂无未穿戴时装",Mathf.RoundToInt(13*u),muted);
            EndTouchScroll();GUI.enabled=prior;if(before!=mobileFashionScroll)inventoryComparisonOpen=false;
            if(chosen!=null)OpenInventoryPopup(chosen,anchor);DrawInventoryPopup(bounds,u);
        }

        private string ResourceDescription(string key)
        {
            if(key.Contains("affix-reforge"))return "重新生成装备随机词条的种类、数量和数值。";
            if(key.Contains("refinement"))return "用于铁匠处洗练装备属性。";
            if(key=="shard")return "用于兑换宝石，以及宝石升阶与升华。";
            if(key=="thread")return "用于时装升阶，可通过分解时装获取。";
            if(key=="experience")return "积累经验提升角色等级。";
            if(key.Contains("potion"))return "使用后恢复50%生命。";
            return "用于购买物品和强化装备。";
        }
        private EntryRewardPreview ResourceItemPreview(string key,int count)
        {
            string name=key=="affix-reforge"?"词条重铸石":key=="thread"?"星纹":key=="shard"?"星烬碎片":key=="refinement"?"装备洗练石":"金币";
            Rarity rarity=key=="affix-reforge"?Rarity.Legendary:key=="refinement"?Rarity.Epic:key=="gold"?Rarity.Common:Rarity.Rare;
            return new EntryRewardPreview{Key=key,Name=name,Quantity=count,Rarity=rarity,
                Icon=key=="thread"?UIIconAtlas.Reward(2):UIIconAtlas.Utility(key=="refinement"||key=="affix-reforge"?"gem":key=="gold"?"coin":"shard"),
                Description=name+"\n数量  "+count};
        }
        private void DrawBagSupplies(MobilePanelLayout.Area area)
        {
            float u=MobileControls.Active?TouchRatio:1;Rect bounds=MobilePanelRect(area);
            float cellSize=MobileControls.Active?InventoryGridGeometry.MobileCellSize:InventoryGridGeometry.DesktopCellSize;
            Rect tile=new Rect(bounds.x,bounds.y,cellSize*u,cellSize*u);Fill(tile,card);Border(tile,GameBalance.RarityColor(Rarity.Common));
            DrawIcon(new Rect(tile.x+5*u,tile.y+3*u,tile.width-10*u,tile.height-17*u),UIIconAtlas.Utility("potion"),GameBalance.RarityColor(Rarity.Common));
            Text(new Rect(tile.x,tile.yMax-14*u,tile.width-2*u,14*u),session.Progression.Profile.potions.ToString(),Mathf.RoundToInt(9*u),pale,true,false,TextAnchor.MiddleRight);
            bool prior=GUI.enabled;GUI.enabled=prior&&(!MobileControls.Active||!inventoryComparisonOpen)&&inventoryPopupDismissed!=Time.frameCount;
            DesktopInventoryGesture(tile,tile,"@potion");
            if(GUI.Button(tile,GUIContent.none,invisibleButton)){if(MobileControls.Active)OpenInventoryPopup("@potion",tile);else DesktopInventoryClick("@potion");}
            var grid=new InventoryGridGeometry(bounds.width/u,cellSize);
            string[] keys={"refinement","shard","thread","affix-reforge"};
            int[] quantities={session.Progression.Profile.refinementStones,session.Progression.Profile.mechanicMaterials,session.Progression.Profile.fashionThreads,session.Progression.Profile.affixReforgeStones};
            for(int i=0;i<keys.Length;i++)
            {
                var cell=grid.Tile(i+1);
                Rect hit=new Rect(bounds.x+cell.X*u,bounds.y+cell.Y*u,cell.Width*u,cell.Height*u);
                var item=ResourceItemPreview(keys[i],quantities[i]);
                Fill(hit,card);Border(hit,item.QualityColor);
                DrawIcon(new Rect(hit.x+5*u,hit.y+3*u,hit.width-10*u,hit.height-17*u),item.Icon,item.QualityColor);
                Text(new Rect(hit.x+2*u,hit.yMax-16*u,hit.width-5*u,16*u),quantities[i].ToString(),Mathf.RoundToInt(10*u),pale,true,false,TextAnchor.MiddleRight);
                InspectRewardItem(hit,item);
            }
            GUI.enabled=prior;DrawInventoryPopup(bounds,u);
        }
    }
}
