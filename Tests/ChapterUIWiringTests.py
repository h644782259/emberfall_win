"""Chapter consumer routing contracts and old-routing mutations; not rendered UI evidence."""
from pathlib import Path
r=Path(__file__).resolve().parents[1]/'Assets/Scripts/UI'
ui=(r/'GameUI.cs').read_text();hubs=(r/'GameUI.Hubs.cs').read_text();chapter=(r/'GameUI.Chapter.cs').read_text();mobile=(r/'GameUI.Mobile.cs').read_text();modes=(r/'GameUI.Modes.cs').read_text()
def routed(main,picker):
 return 'else if (panel == Panel.Chapter) DrawChapterSelection();' in main and '"星路章节"' in picker and 'OpenChapterSelection();' in picker and 'session.CancelDungeonSelection();' in picker
assert routed(ui,modes),'unified dungeon picker reaches chapter selection'
assert not routed(ui.replace('else if (panel == Panel.Chapter) DrawChapterSelection();',''),modes),'missing entry dispatch mutation rejected'
assert not routed(ui,modes.replace('OpenChapterSelection();','')),'missing unified entry mutation rejected'
service=hubs[hubs.index('private void OpenHubNpcService()'):hubs.index('private void HandleHubNpcShortcut')]
assert 'OpenChapterSelection();' not in service and 'NavigateMerchantExchange();' in service,'obsolete NPC no longer owns chapter entry'
assert 'OpenChapterExchange' not in chapter and '"机制兑换"' not in chapter,'exchange leaves chapter footer'
assert '"返回副本选择"' in chapter and 'session.EnterDungeon();' in chapter,'distinct parent navigation remains reachable'
merchant=(r/'GameUI.Merchant.cs').read_text()
assert '"购买 / 兑换"' in merchant and 'PrepareMerchantPurchase' in merchant,'merchant owns validated exchange'
assert 'if(session.NearChapterExit)session.EnterNextChapterRoom();' in mobile,'real touch action advances chapter, not legacy room'
assert 'session.ChapterObjectiveCompact' in modes and modes.index('if(session.ChapterActive)')<modes.index('if(session.RoomChainRun!=null)'),'chapter mobile state precedes nullable legacy mode access'
assert ui.index('else if(session.ChapterFinished)DrawChapterResult();')<ui.index('else if(session.ModeFinished)'),'chapter result does not fall into legacy mode reward recap'
presentation=(r/'ChapterEntryPresentation.cs').read_text()
assert 'ChapterEntryPresentation.Result(session.ChapterResult)' in chapter and presentation.index('if(!result.Saved)return')<presentation.index('text=ChapterDefinition.Get(result.Node).Name',presentation.index('if(!result.Saved)return')),'committed result uses saved actual snapshot only'
assert 'else if (session.IsDead) {if(session.ChapterFinished)DrawChapterResult();else DrawDeath();}' in ui,'chapter failure routes evidence while nonchapter death retains original UI'
assert 'if(!session.ChapterResultReady)' in chapter and 'session.ContinueChapterResult()' in chapter,'chapter result waits for actual boss visual and explicit continue'
assert 'if(panel==Panel.Chapter||panel==Panel.HubDialogue||session.ChapterFinished)return;' in ui,'toast cannot cover chapter or dialogue actions'
assert all(name not in chapter for name in ['SelectedArenaMode','SelectedDungeonTier','SelectedChallengeMode']),'chapter choices never change legacy selections'
print('PASS: unified chapter/merchant/exit/result wiring and old-routing mutation controls')
