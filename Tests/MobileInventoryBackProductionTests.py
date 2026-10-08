"""Run production inline bag Back handling with engine doubles, not Unity delivery."""
from pathlib import Path
import os,sys,tempfile,subprocess
r=Path(__file__).resolve().parents[1]
source=(r/'Assets/Scripts/UI/GameUI.cs').read_text();assert source.count('if(CloseMobileInventoryDetail())return;')==1
shell='''using System;namespace Emberfall{
public sealed partial class GameUI{
enum Panel{None,Inventory,Skills}class Session{public bool Paused;}
private Panel panel=Panel.Inventory;private Session session=new Session();private bool inventoryFashionOpen,inventoryComparisonOpen,mobileInventoryDetail;private object collectionTrial;private int cancels,blocks;
private void CancelMobileScroll(){cancels++;}private void BlockUITransition(){blocks++;}
static void Check(bool b,string s){if(!b)throw new Exception(s);}
public static void Main(){var ui=new GameUI{inventoryComparisonOpen=true,mobileInventoryDetail=true};Check(ui.CloseMobileInventoryDetail()&&!ui.inventoryComparisonOpen&&!ui.mobileInventoryDetail&&ui.panel==Panel.Inventory,"Back closes inline comparison while retaining bag");Check(ui.cancels==1&&ui.blocks==1,"navigation releases drag capture");Check(!ui.CloseMobileInventoryDetail(),"next Back passes to outer close");ui.inventoryFashionOpen=true;ui.collectionTrial=new object();Check(ui.CloseMobileInventoryDetail()&&!ui.inventoryFashionOpen,"fashion route Back closes pending route");ui.inventoryComparisonOpen=true;ui.panel=Panel.Skills;Check(!ui.CloseMobileInventoryDetail(),"stale bag state cannot capture another panel");ui.panel=Panel.Inventory;ui.session.Paused=true;Check(!ui.CloseMobileInventoryDetail(),"pause retains outer close");Console.WriteLine("PASS 6 production inline bag Back cases (engine doubles)");}
}}
'''
with tempfile.TemporaryDirectory(prefix='EmberfallBagBack-') as d:
 p=Path(d);(p/'Program.cs').write_text(shell);(p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup><ItemGroup><Compile Include="'+str(r/'Assets/Scripts/UI/GameUI.MobileInventoryNavigation.cs')+'" /></ItemGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>');os.environ.setdefault('DOTNET_CLI_HOME',str(p/'dotnet-home'));subprocess.run([sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet'),'run','--project',str(p/'Test.csproj'),'--configuration','Release'],check=True)
