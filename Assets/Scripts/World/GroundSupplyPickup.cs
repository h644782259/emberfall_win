using UnityEngine;
namespace Emberfall {
 public sealed class GroundSupplyPickup:MonoBehaviour {
  private GameSession session;private int gold,potions;private float age,retry;private Material coin,potion;
  public void Initialize(GameSession owner,int coins,int bottles){session=owner;gold=coins;potions=bottles;
   coin=new Material(Shader.Find("Standard")){color=new Color(1,.73f,.16f)};
   potion=new Material(Shader.Find("Standard")){color=new Color(.85f,.13f,.25f)};
   Part(PrimitiveType.Cylinder,new Vector3(0,.16f,0),new Vector3(.45f,.06f,.45f),coin);
   if(potions>0){Part(PrimitiveType.Sphere,new Vector3(.45f,.25f,0),new Vector3(.25f,.35f,.25f),potion);Part(PrimitiveType.Cylinder,new Vector3(.45f,.46f,0),new Vector3(.12f,.07f,.12f),coin);}
  }
  private void Part(PrimitiveType type,Vector3 position,Vector3 size,Material material){var p=GameObject.CreatePrimitive(type);p.transform.SetParent(transform,false);p.transform.localPosition=position;p.transform.localScale=size;p.GetComponent<Renderer>().sharedMaterial=material;Destroy(p.GetComponent<Collider>());}
  private void Update(){age+=Time.deltaTime;if(age<.6f||session==null||session.Player==null||session.IsDead||Time.unscaledTime<retry)return;
   if(Vector3.Distance(session.Player.transform.position,transform.position)>2)return;retry=Time.unscaledTime+1;
   if(!session.Progression.CollectGroundSupplies(gold,potions))return;
   enabled=false;session.LogSystem("拾取金币 × "+gold+(potions>0?" · 药剂 × "+potions:""));GameAudio.Play(SoundCue.Loot);Destroy(gameObject);
  }
  private void OnDestroy(){if(coin!=null)Destroy(coin);if(potion!=null)Destroy(potion);}
 }
}
