#!/usr/bin/env python3
"""Full production equipment construction with tier/rarity/upgrade independence; managed TRS."""
from pathlib import Path
# Reuse the existing actual builder extraction and the exact same hierarchy/mesh doubles.
exec((Path(__file__).with_name('EquipmentCompositionProductionTests.py')).read_text().split('with tempfile.TemporaryDirectory')[0])
with tempfile.TemporaryDirectory(prefix='class-tier-') as directory:
 p=Path(directory)
 for folder,names in [('Core',['WeaponStructure','EquipmentAppearance','EquipmentAttachmentRecipe','CostumeRecipes','CostumeLayers']),('Combat',['CombatModel.Costumes','CombatModel.CostumeLayers','CombatModel.WeaponRig','CostumeMeshLibrary','ProceduralVisuals','VisualMeshRecipes'])]:
  for n in names:(p/(n+'.cs')).write_text((root/'Assets/Scripts'/folder/(n+'.cs')).read_text())
 # Same explicit disabled optional-visual boundary as the shared builder suite.
 (p/'OptionalPilotBoundary.cs').write_text((root/'Tests/EquipmentCompositionPilotBoundary.cs').read_text())
 (p/'Model.cs').write_text(body);(p/'Types.cs').write_text(data)
 (p/'Fixture.cs').write_text((root/'Tests/EquipmentCompositionProductionTests.Fixture.cs').read_text());(p/'Tests.cs').write_text((root/'Tests/EquipmentVisualIdentityProductionTests.cs').read_text())
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><StartupObject>EquipmentVisualIdentityProductionTests</StartupObject><NoWarn>0649;0169</NoWarn></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');cmd=[dotnet,'run','--project',str(project),'--no-restore']
 subprocess.run([dotnet,'restore',str(project),'--configfile',str(p/'NuGet.Config'),'-v:q'],env=env,check=True)
 subprocess.run(cmd,env=env,check=True)
