using System;using System.IO;using System.Reflection;using System.Text.Json.Nodes;using Emberfall;using UnityEngine;
public static class SharedChestNormalizationTests {
 static int checks;static void C(bool v,string m){checks++;if(!v)throw new Exception(m);}
 static JsonObject Document(string text)=>JsonNode.Parse(text).AsObject();
 static string Serialize(JsonObject d)=>d.ToJsonString();
 public static string Run(string root){Directory.CreateDirectory(root);
 foreach(string kind in new[]{"null","missing","empty","defaults","real","partial-id","partial-gold","future-draw","future-profile","future-file","malformed-string","malformed-number","malformed-array"}){
  var service=new ProgressionService(Path.Combine(root,kind));service.NewGame(HeroClass.Vanguard);service.Save();C(string.IsNullOrEmpty(service.LastError),"save fixture "+kind);
  var doc=Document(File.ReadAllText(service.SaveFilePath));var profile=doc["profile"].AsObject();
  var empty=JsonNode.Parse(JsonUtility.ToJson(new ChestReward(),true));
  if(kind=="missing")profile.Remove("pendingChestDraw");else profile["pendingChestDraw"]=kind=="null"?null:kind=="empty"?new JsonObject():empty;
  bool invalid=kind.StartsWith("partial")||kind.StartsWith("future")||kind.StartsWith("malformed");
  if(kind=="real") {profile["pendingFashionChest"]=true;profile["pendingChestRulesRevision"]=2;profile["chestRulesRevision"]=2;profile["pendingChestDraw"]=JsonNode.Parse(JsonUtility.ToJson(ProgressionService.BuildSingleChestRoll(service.Profile,99,0,0,false,"frozen-identity"),true));}
  if(kind=="malformed-string")profile["pendingChestDraw"]="broken";
  if(kind=="malformed-number")profile["pendingChestDraw"]=17;
  if(kind=="malformed-array")profile["pendingChestDraw"]=new JsonArray();
  if(kind=="partial-id")profile["pendingChestDraw"]["id"]="do-not-discard";
  if(kind=="partial-gold")profile["pendingChestDraw"]["gold"]=1;
  if(kind=="future-draw")profile["pendingChestDraw"]["rulesRevision"]=99;
  if(kind=="future-profile")profile["chestRulesRevision"]=99;
  if(kind=="future-file")doc["version"]=99;
  string stored=Serialize(doc);File.WriteAllText(service.SaveFilePath,stored);string backup=File.ReadAllText(service.SaveFilePath+".bak");
  var fresh=new ProgressionService(service.SaveDirectory);bool loaded=fresh.LoadSlot(service.CurrentSlotId);
  C(loaded!=invalid,kind+" load/fail-closed outcome: "+fresh.LastError);
  C(File.ReadAllText(service.SaveFilePath)==stored&&File.ReadAllText(service.SaveFilePath+".bak")==backup,kind+" preserves primary and backup during read");
  if(!invalid){if(kind=="real")C(fresh.Profile.pendingChestDraw!=null&&fresh.Profile.pendingChestDraw.id=="frozen-identity","real draw survives unchanged");else{C(fresh.Profile.pendingChestDraw==null,kind+" normalizes exact absence");fresh.Save();C(string.IsNullOrEmpty(fresh.LastError),kind+" can save normalized profile");C(new ProgressionService(service.SaveDirectory).LoadSlot(service.CurrentSlotId),kind+" reload after save");}}
 }
 // Every populated field independently prevents normalization; zeros and sentinel -1 define the exact placeholder.
 var normalize=typeof(ProgressionService).GetMethod("NormalizeEmptyChestDraw",BindingFlags.Static|BindingFlags.NonPublic);
 foreach(var field in typeof(ChestReward).GetFields(BindingFlags.Public|BindingFlags.Instance)){
  var draw=new ChestReward();object value=field.FieldType==typeof(string)?"x":field.FieldType==typeof(bool)?(object)true:field.FieldType.IsEnum?Enum.ToObject(field.FieldType,1):(object)((int)field.GetValue(draw)+1);
  field.SetValue(draw,value);var profile=new GameProfile{pendingChestDraw=draw};normalize.Invoke(null,new object[]{profile});C(object.ReferenceEquals(profile.pendingChestDraw,draw),"populated field retained: "+field.Name);
 }
 return "PASS "+checks+" exact-placeholder/read/write/frozen/future/file-preservation checks (managed JSON, not Unity serializer)";
 }
}
