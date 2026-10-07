using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameAudio
    {
        // One shared speech channel: shop greetings never layer over each other.
        private AudioSource npcVoice;
        private readonly AudioClip[] npcGreetings=new AudioClip[3];

        public static void PlayNpcGreeting(HubNpcKind kind)
        {
            if(kind==HubNpcKind.None||muted||quitting||lifecycle.BackgroundPaused||!Application.isPlaying)return;
            EnsureInstance();
            instance.EnsureListener();
            string path=HubNpcGreeting.Resource(kind);
            if(path==null)return;
            int index=(int)kind-1;
            if(instance.npcGreetings[index]==null)instance.npcGreetings[index]=Resources.Load<AudioClip>(path);
            AudioClip clip=instance.npcGreetings[index];
            if(clip==null)return;
            if(instance.npcVoice==null)
            {
                instance.npcVoice=instance.gameObject.AddComponent<AudioSource>();
                instance.npcVoice.playOnAwake=false;
                instance.npcVoice.loop=false;
                instance.npcVoice.spatialBlend=0;
                instance.npcVoice.ignoreListenerPause=true;
                instance.npcVoice.priority=64;
            }
            instance.npcVoice.Stop();
            instance.npcVoice.clip=clip;
            instance.npcVoice.mute=false;
            instance.ApplyNpcVoiceVolume();
            instance.npcVoice.Play();
        }

        public static void StopNpcGreeting()
        {if(instance!=null&&instance.npcVoice!=null)instance.npcVoice.Stop();}

        private void ApplyNpcVoiceVolume()
        {if(npcVoice!=null)npcVoice.volume=.7f*masterVolume;}
    }
}
