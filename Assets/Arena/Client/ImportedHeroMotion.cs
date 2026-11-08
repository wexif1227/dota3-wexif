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
        readonly float[] previousCooldown=new float[6];
        bool observed;float baseMoveSpeed=7,castUntil,playbackSpeed=1;
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
            if(!observed){observed=true;baseMoveSpeed=unit.kind==Kind.Hero?Catalog.Heroes[unit.hero].speed:Mathf.Max(1,unit.speed);for(int i=0;i<6;i++)previousCooldown[i]=unit.cooldown[i];}
            if(unit.dead)
            {
                if(!dead){dead=true;diedAt=Time.time;if(!string.IsNullOrEmpty(death)&&player[death]!=null){player[death].wrapMode=WrapMode.ClampForever;player[death].speed=1;player[death].time=0;player.CrossFade(death,.08f);playing=death;}}
                return;
            }
            if(dead){dead=false;playing="";attackUntil=castUntil=0;Play(idle,.05f);}
            moving=unit.moving;channeling=unit.channel>0;
            for(int i=0;i<6;i++)
            {
                if(unit.kind==Kind.Hero&&unit.cooldown[i]>previousCooldown[i]+.25f)Cast(unit.hero,i);
                previousCooldown[i]=unit.cooldown[i];
            }
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
                        state.speed=playbackSpeed=contact/Mathf.Max(.08f,unit.attackPoint);state.time=0;
                        // Snapshots can arrive after the windup has already begun.
                        if(unit.attackWindup>0)state.time=Mathf.Clamp((unit.attackPoint-unit.attackWindup)*state.speed,0,contact);
                        player.CrossFade(clip,.055f,PlayMode.StopAll);playing=clip;
                        attackUntil=Time.time+Mathf.Min(unit.attackLength,state.length/Mathf.Max(.1f,state.speed));
                    }
                }
            }
            if(!unit.attacking||moving)attackUntil=0;
            if(moving)castUntil=0;
            if(Time.time>=attackUntil&&Time.time>=castUntil)
            {
                string channelClip=unit.kind==Kind.Hero&&unit.hero==4?FindClip("charge"):unit.kind==Kind.Hero&&unit.hero==5?FindClip("cast_teleportation"):channel;
                Play(channeling&&!string.IsNullOrEmpty(channelClip)?channelClip:moving?run:idle,.13f);
                playbackSpeed=moving?Mathf.Clamp(unit.speed*(unit.slow>0?.55f:1)*(unit.haste>0?1.5f:1)/baseMoveSpeed,.4f,2.3f):1;
            }
            if(player[playing]!=null)player[playing].speed=unit.stun>0?0:playbackSpeed;
        }
        string FindClip(string term)
        {foreach(AnimationState state in player)if(state.name.IndexOf(term,StringComparison.OrdinalIgnoreCase)>=0)return state.name;return "";}
        void Cast(int hero,int slot)
        {
            string term=hero==0?(slot==1?"spin_web":slot==5?"spawn_spiderlings":""):hero==1?(slot<3?"shadowraze_"+(slot==0?"a":slot==1?"b":"c"):slot==5?"requiem":""):hero==3&&slot==0?"meathook_start":hero==4&&slot==5?"ultimate":hero==5?(slot==0?"cast_sprout":slot==1?"cast_teleportation":slot==5?"wrath_of_nature":""):"";
            if(term=="")return;string clip=FindClip(term);if(clip=="")return;
            var state=player[clip];state.time=0;state.speed=playbackSpeed=1;state.wrapMode=WrapMode.ClampForever;player.CrossFade(clip,.07f,PlayMode.StopAll);playing=clip;attackUntil=0;castUntil=Time.time+Mathf.Min(.9f,state.length);
        }
        void Play(string clip,float fade)
        {
            if(string.IsNullOrEmpty(clip)||playing==clip||player[clip]==null)return;
            player[clip].speed=1;player[clip].wrapMode=WrapMode.Loop;player.CrossFade(clip,fade,PlayMode.StopAll);playing=clip;
        }
    }
}
