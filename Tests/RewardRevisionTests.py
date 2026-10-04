#!/usr/bin/env python3
"""Actual production save transactions with managed JSON/filesystem and regression mutations."""
from CastReceiptFixtureSources import include_cast_receipt_source
import os,sys,tempfile,subprocess,hashlib
from pathlib import Path
root=Path(__file__).resolve().parents[1]
dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
core=['SafeSaveFlow','GameTypes','ProgressionService','ProgressionService.Reforge','ReforgeQuote','ProgressionService.Chapter','ChapterProgression','RoomTactics','CombatBalance','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState']
with tempfile.TemporaryDirectory(prefix='chapter-transactions-') as folder:
    folder=Path(folder)
    os.environ.setdefault("DOTNET_CLI_HOME",str(folder/"dotnet-home"))
    os.environ.setdefault("DOTNET_CLI_TELEMETRY_OPTOUT","1")
    for name in core:(folder/(name+'.cs')).write_text((root/'Assets/Scripts/Core'/(name+'.cs')).read_text())
    for name in ['ProgressionTests','RewardRevisionTests','RewardLoadTransitionTests']:(folder/(name+'.cs')).write_text((root/'Tests'/(name+'.cs')).read_text())
    (folder/'Program.cs').write_text('System.Console.WriteLine(RewardRevisionTests.Run(args[0]));System.Console.WriteLine(RewardLoadTransitionTests.Run(args[0]));')
    project=folder/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');include_cast_receipt_source(project)
    config=folder/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    subprocess.run([dotnet,'restore',str(project),'--configfile',str(config)],check=True)
    command=[dotnet,'run','--project',str(project),'--no-restore','--',str(folder/'saves')]
    subprocess.run(command,check=True)
    stage=folder/'SafeSaveFlow.cs';actual_stage=stage.read_text();seam='            current.CarryPendingChestContextTo(staged);'
    assert actual_stage.count(seam)==1;stage.write_text(actual_stage.replace(seam,''))
    transition_program=(folder/'Program.cs').read_text();(folder/'Program.cs').write_text('System.Console.WriteLine(RewardLoadTransitionTests.Run(args[0]));')
    failed=subprocess.run(command,capture_output=True,text=True)
    print('NEGATIVE CONTROL: baseline real SaveSlotTransition replaces service without pending-draw context',flush=True);print(failed.stdout+failed.stderr,flush=True)
    assert failed.returncode and 'same-slot real staged replacement retains failed choice' in failed.stdout+failed.stderr
    stage.write_text(actual_stage);(folder/'Program.cs').write_text(transition_program)
    mutations=[('ProgressionService.cs','choice == 2 ? baseGold * 3 / 2 : baseGold','baseGold','supply floors each original gold roll'),
        ('ProgressionService.Chapter.cs','ChapterProgression.GrantDifficultyRewards(candidate, ChapterProgression.DifficultyRewardBit(receipt.Node, receipt.Difficulty));','/* baseline: no first difficulty grant */','base fragments independent plus exact 4'),
        ('ChapterProgression.cs','proven & 63 & ~profile.chapterDifficultyRewardMask','proven & 63','real repeat grants only base fragments')]
    for filename,old,new,expected in mutations:
        p=folder/filename;original=p.read_text();assert old in original;p.write_text(original.replace(old,new))
        result=subprocess.run(command,capture_output=True,text=True)
        # Raw negative output retained by the caller log.
        print('NEGATIVE CONTROL:', filename, old, flush=True);print(result.stdout+result.stderr,flush=True)
        p.write_text(original)
        assert result.returncode and expected in result.stdout+result.stderr,result.stdout+result.stderr
    # Actual immutable pre-change OpenDungeonChest, not a hand-written approximation.
    def method(source):
        a=source.index('        public string OpenDungeonChest(int choice)');b=source.index('        public bool AcknowledgeChestReward()',a)
        return source[a:b]
    # The repositories have distinct history; these immutable pre-change methods are byte-identical.
    baseline_refs=('5d85e47b9fab2489d5b06963a0b896ec19112740','e8b068cd29721db92fdc5f7b77166c2c9652019e')
    baseline_ref=next((ref for ref in baseline_refs if subprocess.run(
        ['git','cat-file','-e',ref+':Assets/Scripts/Core/ProgressionService.cs'],cwd=root,stderr=subprocess.DEVNULL).returncode==0),None)
    assert baseline_ref is not None,'fetch repository history for the immutable reward baseline'
    baseline=subprocess.check_output(['git','show',baseline_ref+':Assets/Scripts/Core/ProgressionService.cs'],cwd=root,text=True)
    assert hashlib.sha256(method(baseline).encode()).hexdigest()=='a2ed81e3d2b2cad7cbce2b6a4aead584bb139e75cdb6c9d6a914e3a2bb04f581','historical baseline method changed'
    p=folder/'ProgressionService.cs';original=p.read_text();program=(folder/'Program.cs').read_text()
    p.write_text(original.replace(method(original),method(baseline)))
    (folder/'Program.cs').write_text('RewardRevisionTests.BaselineSupply(args[0]);')
    result=subprocess.run(command,capture_output=True,text=True)
    print('ACTUAL BASELINE CONTROL:',baseline_ref,flush=True);print(result.stdout+result.stderr,flush=True)
    assert result.returncode and 'baseline supply must not roll fashion' in result.stdout+result.stderr
    p.write_text(original);(folder/'Program.cs').write_text(program)
    print('PASS: actual baseline supply and first-bonus/idempotence negative controls rejected')
    for name in ['ChapterProgressionTests','ChapterPresentationTests','ModeRewardTests','AdventureProgressionTests','SaveIdempotenceTests','UpgradeProgressionTests','SafeSaveFlowTests']:
        (folder/(name+'.cs')).write_text((root/'Tests'/(name+'.cs')).read_text())
    for relative in ['Assets/Scripts/Core/ChapterResultSnapshot.cs','Assets/Scripts/UI/ChapterEntryPresentation.cs']:
        (folder/Path(relative).name).write_text((root/relative).read_text())
    (folder/'Program.cs').write_text('System.Console.WriteLine(ChapterProgressionTests.Run(args[0]));System.Console.WriteLine(ChapterPresentationTests.Run());System.Console.WriteLine(ModeRewardTests.Run(args[0]));System.Console.WriteLine(AdventureProgressionTests.Run(args[0]));System.Console.WriteLine(SaveIdempotenceTests.Run(args[0]));System.Console.WriteLine(UpgradeProgressionTests.Run(args[0]));System.Console.WriteLine(SafeSaveFlowTests.Run(args[0]));')
    subprocess.run(command,check=True)
