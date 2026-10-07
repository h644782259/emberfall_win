from pathlib import Path
import importlib.util,tempfile,subprocess,os,sys
root=Path(__file__).resolve().parents[1];spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
with tempfile.TemporaryDirectory(prefix='shared-chest-normalization-') as d:
 p=Path(d);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 names=['SkillRuntime','GameTypes','ProgressionService','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow']
 sources=[root/('Assets/Scripts/Core/'+n+'.cs') for n in names]+[root/'Tests/ProgressionTests.cs',root/'Tests/SharedChestNormalizationTests.cs']
 # Explicitly model Unity writing a null inline class as a fully default object.
 fixture=p/'ProgressionFixture.cs';stub=(root/'Tests/ProgressionTests.cs').read_text()
 original='return JsonSerializer.Serialize(value, value.GetType(), Options);'
 replacement='string json=JsonSerializer.Serialize(value, value.GetType(), Options); var tree=JsonNode.Parse(json); if(tree is JsonObject obj){var profile=obj["profile"] as JsonObject ?? obj; if(profile.ContainsKey("pendingChestDraw") && profile["pendingChestDraw"]==null)profile["pendingChestDraw"]=JsonSerializer.SerializeToNode(new ChestReward(),Options);} return tree.ToJsonString();'
 assert original in stub;fixture.write_text(stub.replace(original,replacement));sources=[fixture if f==root/'Tests/ProgressionTests.cs' else f for f in sources]
 project=cv.write_project(p/'project',sources,'using System;class Program{static void Main(string[] a){Console.WriteLine(SharedChestNormalizationTests.Run(a[0]));}}')
 subprocess.run([sys.argv[1],'run','--project',str(project),'--',str(p/'saves')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))
