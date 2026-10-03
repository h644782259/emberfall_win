#!/usr/bin/env python3
"""Full FilledSkillVfx/leases and actual Ranger-9 event body, with managed engine/recipient substitutes."""
from CastReceiptFixtureSources import include_cast_receipt_source
import argparse,os,subprocess,tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def once(s,a,b):
    assert s.count(a)==1,a
    return s.replace(a,b,1)
def member(s,sig):
    a=s.index(sig);b=s.index('{',a)+1;n=1
    while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
    return s[a:b]
def main():
    parser=argparse.ArgumentParser();parser.add_argument('dotnet_path',nargs='?');parser.add_argument('--dotnet');args=parser.parse_args();dotnet=args.dotnet or args.dotnet_path or os.environ.get('DOTNET','dotnet')
    fixture=(ROOT/'Tests/FilledVfxAllocationTests.cs').read_text();fixture='using System;using System.Linq;using System.Collections.Generic;using System.Reflection;using UnityEngine;'+fixture[fixture.index('namespace Emberfall'):]
    fixture=fixture.replace(member(fixture,'public static class CombatSight'),'').replace('    public enum CombatSightKind { Area }','')
    math=(ROOT/'Tests/DestructibleTraversalTests.cs').read_text()
    fixture=fixture.replace(member(fixture,'public struct Vector2'),member(math,'public struct Vector2'))
    fixture=fixture.replace('public struct Vector3\n','public partial struct Vector3\n').replace('public static class Mathf\n','public static partial class Mathf\n')
    fixture=once(fixture,'public static class CombatFx{','public static partial class CombatFx{')
    mathShell=(ROOT/'Tests/AnchoredImpactCoverageTests.cs').read_text().split('public static class AnchoredImpactCoverageTests')[0]
    fixture=fixture.replace('sealed class PlayerController' ,'sealed partial class PlayerController')
    sequence=(ROOT/'Assets/Scripts/Combat/AdvancedSkillSequence.cs').read_text();ranger=sequence[sequence.index('        private void Ranger()'):];start=ranger.index('                case 9:')+len('                case 9:');end=ranger.index('                    break;',start);event=ranger[start:end]
    opening=(ROOT/'Assets/Scripts/Combat/PlayerController.cs').read_text();opening=opening[opening.index('else {var field=SkillDamageBudgets.EarlyField(HeroClass,rank);CombatArea.Spawn(this,session,target,4.3f*range'):];opening=opening[opening.index('{')+1:opening.index('}')];
    files=['Core/SkillVisualRecipe','Core/FilledVfxRecipes','Core/FilledVfxPlacement','Core/CombatVisualBudget','Core/DecorationBudget','Core/SkillDamageBudgets','Combat/CombatDamage','Combat/FilledSkillVfx','Combat/AnchoredImpactMesh','Combat/CombatVisualLease','Combat/DecorationLease','Core/CombatSightRules','Combat/CombatSight','World/WorldTraversal']
    cases=[('current',None),('old-empty-anchored','EMPTY_REQUIRED_SHAPE: actual primary/contact requires positive area'),('old-poison-recipe','actual non-poison arrow rain must request the independent arrow recipe'),('old-reject-priority','new main silhouette must evict old sustained mesh'),('old-independent-arrows','all ultimate arrow events must share one visual owner'),('old-relocated-primary','primary actual impact anchor must not relocate to clear ground')]
    with tempfile.TemporaryDirectory(prefix='combat-readability-') as temporary:
        for name,expected in cases:
            folder=Path(temporary)/name;folder.mkdir();(folder/'Fixture.cs').write_text(fixture);(folder/'MathShell.cs').write_text(mathShell)
            (folder/'BuildCatalogDamage.cs').write_text('namespace Emberfall{public static class BuildCatalog{'+member((ROOT/'Assets/Scripts/Core/GameTypes.cs').read_text(),'public static float CinderTrailTickMultiplier(')+'}}')
            for file in files:
                s=(ROOT/('Assets/Scripts/'+file+'.cs')).read_text()
                if name=='old-empty-anchored' and file=='Combat/AnchoredImpactMesh':
                    s=once(s,'CombatSight.VisualTriangle(origin,a,b,c)','CombatSight.VisualFootprint(origin,(a+b+c)/3,Mathf.Max(CombatFx.Flat(a-(a+b+c)/3).magnitude,Mathf.Max(CombatFx.Flat(b-(a+b+c)/3).magnitude,CombatFx.Flat(c-(a+b+c)/3).magnitude))+.131f)')
                if name=='old-reject-priority'  and file=='Core/CombatVisualBudget':
                    start=s.index('            while(tickets.Count>=maximum)');end=s.index('            var result=',start);s=s[:start]+'            if(tickets.Count>=maximum)return null;\n'+s[end:]
                if name=='old-relocated-primary' and file=='Combat/FilledSkillVfx':
                    for label in ['"Landing base",true','"Primary "+type,true','"Contact flash",true']:s=once(s,label,label.replace(',true',''))
                (folder/(Path(file).name+'.cs')).write_text(s)
            rain=opening.replace('SkillVisualRecipe.ArrowRain','SkillVisualRecipe.Poison') if name=='old-poison-recipe' else opening
            (folder/'Opening.cs').write_text('using UnityEngine;namespace Emberfall{public sealed partial class PlayerController{private CombatDamage Damage(float value)=>new CombatDamage(value*10,true,2);public void CastRain(int rank){var HeroClass=Emberfall.HeroClass.Ranger;var session=GameSession.Instance;Vector3 target=Vector3.zero;float range=1;int castId=91;'+rain+'}}}')
            body=event
            if name=='old-independent-arrows':body=once(body,'if(arrowBatch.IsValid)arrowBatch.ArrowBeat(rainAt,3.4f*range,false);','FilledSkillVfx.ArrowRain(owner,rainAt,3.4f*range,color);')
            (folder/'ArrowEvent.cs').write_text('using UnityEngine;namespace Emberfall{public partial class ArrowSequenceFixture{private CastFirstHitReceipt castReceipt;private void Event(){'+body+'}'+sequence[sequence.index('        private void OnDisable()'):sequence.index('        private Vector3 Clamp(')]+'}}')
            (folder/'Tests.cs').write_text((ROOT/'Tests/CombatReadabilityVisualTests.cs').read_text());(folder/'Program.cs').write_text('System.Console.WriteLine(CombatReadabilityVisualTests.Run());')
            project=folder/'Validation.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');include_cast_receipt_source(project);config=folder/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
            build=subprocess.run([dotnet,'build',str(project),'--configfile',str(config),'-v:q'],text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
            if build.returncode:print(build.stdout);build.check_returncode()
            result=subprocess.run([dotnet,str(folder/'bin/Debug/net8.0/Validation.dll')],text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
            if expected:
                if result.returncode==0 or ('System.Exception: '+expected) not in result.stdout:raise RuntimeError(name+' wrong negative result:\n'+result.stdout)
                print('PASS: '+name+' compiled then rejected: '+expected)
            else:print(result.stdout,end='');result.check_returncode()
if __name__=='__main__':main()
