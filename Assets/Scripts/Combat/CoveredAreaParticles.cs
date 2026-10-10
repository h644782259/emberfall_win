using System;
using UnityEngine;
namespace Emberfall
{
    // Cosmetic clipping only; damage continues to use CombatSight.Area at the hit.
    internal sealed class CoveredAreaParticles : MonoBehaviour
    {
        private ParticleSystem system;
        private readonly ParticleSystem.Particle[] samples=new ParticleSystem.Particle[90];
        private Func<float,float,float,bool> clear;
        private bool Clear(float x,float z,float radius)=>Visible(transform.position+new Vector3(x,0,z),radius);
        private void Awake(){system=GetComponent<ParticleSystem>();clear=Clear;}
        private bool Visible(Vector3 point,float radius)
        {return CombatSight.Area(transform.position,point)&&CombatSight.VisualFootprint(point,point,radius);}
        private void LateUpdate()
        {
            if(system==null)return;int n=system.GetParticles(samples);bool changed=false;
            for(int i=0;i<n;i++)
            {
                Vector3 point=samples[i].position;
                float extent=Mathf.Max(.02f,samples[i].startSize*.5f);
                if(Visible(point,extent))continue;
                changed=true;
                // Redirect freshly emitted cosmetic particles into a clear sector before their
                // first render; do not repeatedly teleport old particles across the field.
                float age=samples[i].startLifetime-samples[i].remainingLifetime;
                float x,z,fit;Vector3 relative=point-transform.position;
                if(age<=Time.deltaTime*2+.04f&&FilledVfxPlacement.TryPlace(relative.x,relative.z,
                    Mathf.Max(.2f,system.shape.radius),extent,clear,out x,out z,out fit))
                {
                    samples[i].position=new Vector3(transform.position.x+x,point.y,transform.position.z+z);
                    Vector3 velocity=samples[i].velocity;velocity.x=velocity.z=0;samples[i].velocity=velocity;
                    samples[i].startSize*=fit;
                }
                else samples[i].remainingLifetime=0;
            }
            if(changed)system.SetParticles(samples,n);
        }
    }
}
