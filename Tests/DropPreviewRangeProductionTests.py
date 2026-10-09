"""Compare real generated item rolls against the ranges presented to players."""
from pathlib import Path
import subprocess,sys,tempfile,os
root=Path(__file__).resolve().parents[1]
s=(root/'Assets/Scripts/Core/ProgressionService.cs').read_text()
def member(key):
 a=s.index(key);b=s.index('{',a)+1;depth=1
 while depth:
  depth+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
fields=s[s.index('        private static readonly float[] EquipmentRollMinimum'):s.index('        public static string EquipmentDropPreview')]
methods='\n'.join(member(k) for k in ['public static string EquipmentDropPreview(', 'private static string EquipmentPreviewRange(', 'private static void SetRolledStats(', 'private static int RolledEquipmentStat(', 'public static int EquipmentGenerationLevel(', 'private static int Round('])
code="""using System;using System.Text.RegularExpressions;
enum ItemSlot{Weapon,Armor,Trinket} enum Rarity{Common,Rare,Epic,Legendary} enum EquipmentMechanic{None,Test}
class ItemData{public string id;public ItemSlot slot;public Rarity rarity;public int level,attack,defense,health,statRollRevision;public float criticalChance,criticalDamageBonus;}
static class GameBalance{public static string SlotName(ItemSlot s)=>s.ToString();public static string RarityName(Rarity r)=>r.ToString();}
static class BuildCatalog{public static string MechanicName(EquipmentMechanic m)=>"机制名";public static string MechanicDescription(EquipmentMechanic m)=>"实际机制效果";}
class ProgressionService{const int MaximumLevel=100;static int Clamp(int v,int a,int b)=>Math.Max(a,Math.Min(b,v));
"""+fields+methods+r"""
static void Main(){int checks=0;foreach(int level in new[]{1,10,50,100})foreach(ItemSlot slot in Enum.GetValues(typeof(ItemSlot)))foreach(Rarity rarity in Enum.GetValues(typeof(Rarity))){
 string description=EquipmentDropPreview(slot,rarity,level,EquipmentMechanic.Test);
 if(!description.Contains("实际机制效果"))throw new Exception("missing mechanic");
 for(int seed=0;seed<100;seed++){
 var item=new ItemData{id=seed.ToString(),slot=slot,rarity=rarity,level=level};SetRolledStats(item);
 foreach(var pair in new[]{("攻击",item.attack),("防御",item.defense),("生命",item.health)}){
 var m=Regex.Match(description,pair.Item1+@"  (\d+)～(\d+)");
 if(pair.Item2>0&&(!m.Success||pair.Item2<int.Parse(m.Groups[1].Value)||pair.Item2>int.Parse(m.Groups[2].Value)))throw new Exception("roll outside preview");checks++;
 }
 if(rarity==Rarity.Common&&(item.criticalChance!=0||item.criticalDamageBonus!=0))throw new Exception("common affix");
 if(item.criticalChance>0&&!description.Contains("暴击率"))throw new Exception("missing crit range");
 if(item.criticalDamageBonus>0&&!description.Contains("暴击伤害"))throw new Exception("missing crit damage range");
 }
}Console.WriteLine(checks+" generated stat / preview range checks passed");}}
"""
with tempfile.TemporaryDirectory(prefix='drop-preview-') as work:
 p=Path(work);(p/'Program.cs').write_text(code)
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 subprocess.run([sys.argv[1],'run','--project',str(p/'Test.csproj')],env=dict(os.environ,DOTNET_CLI_HOME='/tmp/emberfall-title-cli',DOTNET_NOLOGO='1'),check=True)
ui=(root/'Assets/Scripts/UI/GameUI.Modes.cs').read_text()
assert 'end+=h/u' not in ui
assert 'GUIUtility.GUIToScreenPoint(icon.position)' in ui
assert 'GUIUtility.ScreenToGUIPoint(entryRewardScreenAnchor)' in ui
assert 'entryRewardPopupRect.Contains(Mouse)' in ui
print('PASS deferred anchored popup source guards')
