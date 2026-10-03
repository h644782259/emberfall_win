#!/usr/bin/env python3
"""Actual production save transactions with managed JSON/filesystem and regression mutations."""
from CastReceiptFixtureSources import include_cast_receipt_source
import os,sys,tempfile,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1]
dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
core=['GameTypes','ProgressionService','ProgressionService.Reforge','ReforgeQuote','ProgressionService.Chapter','ChapterProgression','RoomTactics','CombatBalance','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState']
with tempfile.TemporaryDirectory(prefix='chapter-transactions-') as folder:
    folder=Path(folder)
    for name in core:(folder/(name+'.cs')).write_text((root/'Assets/Scripts/Core'/(name+'.cs')).read_text())
    for name in ['ProgressionTests','ChapterProgressionTests','ModeRewardTests','AdventureProgressionTests']:(folder/(name+'.cs')).write_text((root/'Tests'/(name+'.cs')).read_text())
    (folder/'Program.cs').write_text('System.Console.WriteLine(ChapterProgressionTests.Run(args[0]));System.Console.WriteLine(ModeRewardTests.Run(args[0]));System.Console.WriteLine(AdventureProgressionTests.Run(args[0]));')
    project=folder/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');include_cast_receipt_source(project)
    config=folder/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    subprocess.run([dotnet,'restore',str(project),'--configfile',str(config)],check=True)
    command=[dotnet,'run','--project',str(project),'--no-restore','--',str(folder/'saves')]
    subprocess.run(command,check=True)
    p=folder/'ProgressionService.cs';original=p.read_text()
    old='(profile.clearedRuns > 0 || (profile.chapterCompletedMask&(1<<(int)ChapterNode.StarPlatform))!=0 || profile.chapterPriorAdventureTier>0 || profile.highestAdventureTier>profile.chapterHighestAdventureTier)'
    assert old in original
    p.write_text(original.replace(old,'(profile.clearedRuns > 0 || profile.chapterPriorAdventureTier>0 || profile.highestAdventureTier>profile.chapterHighestAdventureTier)'))
    result=subprocess.run(command,capture_output=True,text=True)
    assert result.returncode and 'only star advances shared tier without legacy chest' in result.stdout+result.stderr,result.stdout+result.stderr
    p.write_text(original)
    p=folder/'ProgressionService.Chapter.cs';original=p.read_text()
    old='receipt.Sequence!=Profile.chapterRewardSequence+1'
    assert old in original
    # Recreate the single-last-id flaw: admit an earlier completed callback after a newer reward.
    p.write_text(original.replace('!ReferenceEquals(receipt,chapterAttempt)||'+old,'receipt.Sequence>Profile.chapterRewardSequence+1'))
    result=subprocess.run(command,capture_output=True,text=True)
    assert result.returncode and 'old receipt after newer settlement is rejected' in result.stdout+result.stderr,result.stdout+result.stderr
    print('PASS: legacy reload inference and old-receipt replay mutations fail real production assertions')
