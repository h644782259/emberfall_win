#!/usr/bin/env python3
"""Actual EnemyStatusEffects plus Player.ElementalAdvancedArea; final damage recipients are managed recorders."""
from CastReceiptFixtureSources import include_cast_receipt_source
import argparse,os,subprocess,tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def member(source,signature):
    start=source.index(signature);brace=source.index('{',start);depth=1;end=brace+1
    while depth:depth+=(source[end]=='{')-(source[end]=='}');end+=1
    return source[start:end]
def once(source,a,b):
    assert source.count(a)==1,a
    return source.replace(a,b,1)
def main():
    parser=argparse.ArgumentParser();parser.add_argument('dotnet_path',nargs='?');args=parser.parse_args();dotnet=args.dotnet_path or os.environ.get('DOTNET','dotnet')
    gate=member((ROOT/'Assets/Scripts/Combat/PlayerUpgradeRules.cs').read_text(),'public sealed class RecentCastGate')
    player=member((ROOT/'Assets/Scripts/Combat/PlayerController.cs').read_text(),'internal void ElementalAdvancedArea(')
    source=(ROOT/'Assets/Scripts/Combat/PlayerController.cs').read_text()
    for signature in ('private bool Melee(', 'internal void HitArea(', 'internal void ElementalAdvancedArea('):
        body=member(source,signature)
        assert body.index('CombatImpactBatch.Begin();')<body.index('try')<body.index('DestructibleProp.')<body.index('finally {CombatImpactBatch.End();}')
    assert 'model.AnimateCharge(charge.Progress,charge.SkillIndex)' in source
    # Tail ownership is a separate source contract: the actual tail emitter passes no statusSkill,
    # and the area simulation has no call into either finale entry. Component damage is tested above.
    tail=member((ROOT/'Assets/Scripts/Combat/AdvancedSkillSequence.cs').read_text(),'private void SpawnTail(')
    area=member((ROOT/'Assets/Scripts/Combat/CombatEffects.cs').read_text(),'internal sealed class CombatArea')
    assert 'statusSkill:' not in tail and 'CombatArea.Spawn(' in tail
    assert 'int statusSkill = -1' in area
    assert not any(api in area for api in ('BeginBurnFinale(', 'CompleteBurnFinale(', 'ResolveBurnFinale(', 'ElementalAdvancedArea('))
    print('PASS: production tail emitter/area source contract excludes finale cash entry (not simulated tail damage)')
    cases=[('current',None),('missing-impact-batch','actual player area defers prop arbitration until enemy resolution completes'),('claimed-not-delivered','claimed ticks without actual HP loss never report cash success'),('false-contact','direct hit keeps priority and dead target cannot receive cash or contact'),('missing-final-cash-delivery','actual player final branch preserves direct crit damage and adds only noncritical DOT cash'),('missing-due-drain','due original-strength tick must settle before strongest refresh and future cash'),('cash-without-own-burn','without existing own burn finale leaves a fresh three-second schedule'),('double-frame-advance','status Update followed by finale in same frame cannot advance burn clock twice')]
    with tempfile.TemporaryDirectory(prefix='burn-finale-') as temporary:
        for name,expected in cases:
            folder=Path(temporary)/name;folder.mkdir();method=player
            if name=='missing-impact-batch':method=once(once(method,'CombatImpactBatch.Begin();',''),'finally {CombatImpactBatch.End();}','finally {}')
            if name=='false-contact':method=once(method,'if(burnSettlement!=null&&burnSettlement.Apply())RecordBurnCash(castId,impactEpoch,enemy.transform.position);','if(burnSettlement!=null){burnSettlement.Apply();RecordBurnCash(castId,impactEpoch,enemy.transform.position);}')
            if name=='missing-final-cash-delivery':method=once(method,'if(burnSettlement!=null&&burnSettlement.Apply())RecordBurnCash(castId,impactEpoch,enemy.transform.position);','')
            (folder/'PlayerMethod.cs').write_text('using UnityEngine;namespace Emberfall{public partial class PlayerController{'+method+'}}')
            (folder/'Gate.cs').write_text('using System.Collections.Generic;namespace Emberfall{'+gate+'}')
            files=['Assets/Scripts/Core/CombatImpactBatch.cs','Assets/Scripts/Core/ScheduledTickWindow.cs','Assets/Scripts/Core/BurnFinaleReceipts.cs','Assets/Scripts/Combat/EnemyStatusEffects.cs','Assets/Scripts/Combat/PlayerController.BurnFeedback.cs','Assets/Scripts/Combat/CombatDamage.cs','Tests/BurnFinaleProductionTests.cs','Tests/ScheduledTickWindowTests.cs']
            for path in files:
                source=(ROOT/path).read_text()
                if path.endswith('EnemyStatusEffects.cs'):
                    if name=='claimed-not-delivered':source=once(source,'return settlement.Apply()?settlement.Ticks:0;','settlement.Apply();return settlement.Ticks;')
                    if name=='missing-due-drain':source=once(source,'while(burnSchedule!=null&&burnSchedule.HasDueTicks&&!enemy.IsDead&&ValidSource(source,expectedEpoch))','while(false&&burnSchedule!=null&&burnSchedule.HasDueTicks&&!enemy.IsDead&&ValidSource(source,expectedEpoch))')
                    if name=='cash-without-own-burn':source=once(source,'            if(!plan.HadOwnBurn)return null;','')
                    if name=='double-frame-advance':source=once(source,'            if(burnClockFrame==Time.frameCount)return;','')
                (folder/Path(path).name).write_text(source)
            (folder/'Program.cs').write_text('System.Console.WriteLine(ScheduledTickWindowTests.Run());System.Console.WriteLine(BurnFinaleProductionTests.Run());');config=folder/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>');project=folder/'Validation.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');include_cast_receipt_source(project)
            build=subprocess.run([dotnet,'build',str(project),'--configfile',str(config),'-v:q'],text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
            if build.returncode:print(build.stdout);build.check_returncode()
            result=subprocess.run([dotnet,str(folder/'bin/Debug/net8.0/Validation.dll')],text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
            if expected:
                if result.returncode==0 or ('System.Exception: '+expected) not in result.stdout:raise RuntimeError(name+' wrong compiled negative result:\n'+result.stdout)
                print('PASS: '+name+' compiled and failed exact assertion: '+expected)
            else:print(result.stdout,end='');result.check_returncode()
if __name__=='__main__':main()
