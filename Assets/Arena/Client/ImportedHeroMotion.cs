using System;
using System.Linq;
using UnityEngine;
using Rift.Core;
namespace Rift.Client
{
    public sealed class ImportedHeroMotion:MonoBehaviour
    {
        public Animation player;
        public string idle,run,channel,death;
        public string[] attacks=Array.Empty<string>();
        int sequence;bool moving,channeling;string playing="";float attackUntil;
        bool initialized,dead;float diedAt;
        public bool VisibleWhileDead=>!dead||Time.time-diedAt<2.6f;
        void Awake()
        {
            if(!player)player=GetComponentInChildren<Animation>();
            if(!player)return;
            foreach(AnimationState state in player)state.wrapMode=WrapMode.Loop;
            Play(idle,.05f);initialized=true;
        }
        public void Sync(Unit unit)
        {
            if(!player)return;if(!initialized)Awake();
            if(unit.dead)
            {
                if(!dead){dead=true;diedAt=Time.time;if(!string.IsNullOrEmpty(death)&&player[death]!=null){player[death].wrapMode=WrapMode.ClampForever;player[death].speed=1;player[death].time=0;player.CrossFade(death,.08f);playing=death;}}
                return;
            }
            if(dead){dead=false;playing="";attackUntil=0;Play(idle,.05f);}
            moving=unit.moving;channeling=unit.channel>0;
            if(unit.attackSequence!=sequence)
            {
                sequence=unit.attackSequence;
                if(attacks.Length>0)
                {
                    string clip=attacks[(sequence-1)%attacks.Length];var state=player[clip];
                    if(state!=null)
                    {
                        state.wrapMode=WrapMode.ClampForever;
                        // Dota clips place the impact around the first third. Match the game's attack point.
                        float contact=state.length*.35f;
                        state.speed=contact/Mathf.Max(.08f,unit.attackPoint);state.time=0;
                        player.CrossFade(clip,.055f,PlayMode.StopAll);playing=clip;
                        attackUntil=Time.time+Mathf.Min(unit.attackLength,state.length/Mathf.Max(.1f,state.speed));
                    }
                }
            }
            if(!unit.attacking||moving)attackUntil=0;
            if(Time.time>=attackUntil)Play(channeling&&!string.IsNullOrEmpty(channel)?channel:moving?run:idle,.13f);
        }
        void Play(string clip,float fade)
        {
            if(string.IsNullOrEmpty(clip)||playing==clip||player[clip]==null)return;
            player[clip].speed=1;player[clip].wrapMode=WrapMode.Loop;player.CrossFade(clip,fade,PlayMode.StopAll);playing=clip;
        }
    }
}
