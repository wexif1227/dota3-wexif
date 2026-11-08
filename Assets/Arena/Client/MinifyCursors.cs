using System.Collections.Generic;
using UnityEngine;
namespace Rift.Client
{
    public static class MinifyCursors
    {
        static readonly Dictionary<string,Texture2D> textures=new Dictionary<string,Texture2D>();
        static string current;
        public static void Set(string name)
        {
            if(current==name)return;current=name;
            if(!textures.TryGetValue(name,out var texture)){texture=Resources.Load<Texture2D>("DotaImported/Cursors/"+name);textures[name]=texture;}
            Cursor.SetCursor(texture,name=="buy"||name.StartsWith("spell")?new Vector2(14,14):Vector2.zero,CursorMode.Auto);
        }
        public static void Reset(){current=null;Set("default");}
    }
}
