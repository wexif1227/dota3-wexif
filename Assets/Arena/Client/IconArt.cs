using UnityEngine;
namespace Rift.Client
{
    public static class IconArt
    {
        static Texture2D items,abilities;
        static readonly int[][] indices={new[]{0,1,2,-1,-1,3},new[]{4,5,6,7,8,9},new[]{10,11,12,-1,-1,13},new[]{14,15,16,-1,-1,17},new[]{18,19,20,-1,-1,21},new[]{22,23,24,-1,-1,25}};
        public static Texture2D Items=>items?items:(items=Resources.Load<Texture2D>("Art/Items"));
        public static Texture2D Abilities=>abilities?abilities:(abilities=Resources.Load<Texture2D>("Art/Abilities"));
        public static int AbilityIndex(int hero,int slot)=>indices[hero][slot];
        public static void Draw(Rect rect,bool item,int index,float brightness=1)
        {
            var texture=item?Items:Abilities;if(!texture||index<0)return;int cols=item?6:4,rows=item?6:7;
            float px=1f/texture.width,py=1f/texture.height;var uv=new Rect(index%cols/(float)cols+px,1-(index/cols+1)/(float)rows+py,1f/cols-2*px,1f/rows-2*py);
            var old=GUI.color;GUI.color=new Color(brightness,brightness,brightness,1);GUI.DrawTextureWithTexCoords(rect,texture,uv);GUI.color=old;
        }
    }
}
