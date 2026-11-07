using System;
using System.Collections.Generic;
namespace Rift.Core
{
    public static class Catalog
    {
        static Ability A(string n,string d,float m,float c,float r=12,bool p=false)=>new Ability(n,d,m,c,r,p);
        static Ability P(string n,string d)=>A(n,d,0,0,0,true);
        public static readonly HeroDef[] Heroes={
            new HeroDef {name="Broodmother",title="МАТЬ ПАУКОВ",role="Осада · призыв",color="A767BA",hp=720,mp=380,damage=52,range=2.2f,speed=7.4f,description="Плетите сеть, выращивайте выводок и забирайте линии.",abilities=new[]{A("Insatiable Hunger","8 с. усиления атак и вампиризма.",70,22,0),A("Spin Web","Паутина даёт скорость и регенерацию союзникам.",40,12,16),P("Incapacitating Bite","Атаки замедляют врага. Shard: дополнительный урон."),P("—",""),P("—",""),A("Spawn Spiderlings","Урон цели; смертельный удар создаёт пауков. Aghanim: +2 паука.",100,18,14)}},
            new HeroDef {name="Shadow Fiend",title="ПОВЕЛИТЕЛЬ ДУШ",role="Урон · магия",color="E66A42",hp=620,mp=390,damage=48,range=8,speed=7,description="Собирайте души добиваниями. Накрывайте врагов койлами.",abilities=new[]{A("Shadowraze • near","Взрыв в 4 м перед героем.",65,9,4),A("Shadowraze • middle","Взрыв в 8 м перед героем.",65,9,8),A("Shadowraze • far","Взрыв в 12 м перед героем.",65,9,12),P("Necromastery","Добивания дают души и урон атаки."),P("Presence","Ближайшие враги теряют броню."),A("Requiem of Souls","Взрыв душ вокруг героя. Aghanim: вторичная волна.",140,65,0)}},
            new HeroDef {name="Templar Assassin",title="ХРАНИТЕЛЬНИЦА ТАЙН",role="Урон · засада",color="D88CDF",hp=660,mp=360,damage=58,range=6,speed=7.2f,description="Блокируйте урон рефракцией и устраивайте засады.",abilities=new[]{A("Refraction","Блокирует несколько попаданий и усиливает атаку.",75,17,0),A("Meld","Невидимость до движения. Следующая атака усилена.",50,10,0),P("Psi Blades","Атаки пробивают врагов за основной целью."),P("—",""),P("—",""),A("Psionic Trap","Ловушка взрывается при приближении врага. Shard: оглушение.",35,10,22)}},
            new HeroDef {name="Pudge",title="МЯСНИК",role="Контроль · танк",color="C3B184",hp=900,mp=320,damage=60,range=2.4f,speed=6.6f,description="Зацепите врага крюком, включите гниение и удерживайте.",abilities=new[]{A("Meat Hook","Притягивает первое существо на линии крюка.",90,14,19),A("Rot","Переключает ауру урона и замедления; ранит самого Пуджа.",0,0,0),P("Flesh Heap","Убийства героев навсегда дают здоровье."),P("—",""),P("—",""),A("Dismember","3 с. удержания, урон и лечение. Aghanim: больше урона.",100,28,3.5f)}},
            new HeroDef {name="Spirit Breaker",title="БАРАТРУМ",role="Инициация · контроль",color="66BFD6",hp=820,mp=340,damage=55,range=2.5f,speed=7.6f,description="Врывайтесь с разбега и сбивайте врагов мощными ударами.",abilities=new[]{A("Charge of Darkness","Разбег к видимой цели по всей карте; оглушение при ударе.",80,18,160),A("Bulldoze","6 с. ускорения и снижения длительности контроля.",40,18,0),P("Greater Bash","Каждая четвёртая атака оглушает и наносит урон."),P("—",""),P("—",""),A("Nether Strike","Телепортация к врагу и мощный удар. Aghanim: удар по области.",100,45,14)}},
            new HeroDef {name="Nature's Prophet",title="ФУРИОН",role="Осада · мобильность",color="8BBF70",hp=670,mp=440,damage=53,range=9,speed=7,description="Призывайте энтов и перемещайтесь между линиями.",abilities=new[]{A("Sprout","Укореняет врагов в выбранной области.",65,13,15),A("Teleportation","После 2.5 с. переносит в указанную точку карты.",65,30,160),A("Nature's Call","Призывает энтов для атаки ближайшей линии.",90,28,14),P("—",""),P("—",""),A("Wrath of Nature","Цепь ударов по видимым врагам. Aghanim: корни.",140,55,160)}}
        };
        public static readonly List<ItemDef> Items=new List<ItemDef>{new ItemDef{id=0,name="",description=""}};
        static Catalog()
        {
            Add("Tango",90,"Восстановить 180 HP за 9 секунд.","heal",16);
            Add("Healing Salve",110,"Восстановить 400 HP за 10 секунд.","salve",20);
            Add("Clarity",50,"Восстановить 180 маны.","mana",15);
            Add("Town Portal Scroll",100,"Телепорт к союзной башне или базе после 3 с.","tp",60);
            Add("Observer Ward",75,"Обзор области на 180 с.","ward",2);
            Add("Sentry Ward",100,"Раскрывает невидимость в области на 150 с.","sentry",2);
            Add("Iron Branch",50,"+30 HP, +15 маны, +2 урона.",hp:30,mp:15,damage:2);
            Add("Boots of Speed",500,"+1.2 к скорости.",speed:1.2f);
            Add("Magic Wand",450,"Мгновенно восстановить 160 HP и маны.","wand",18,hp:60,mp:40,recipe:new[]{7,7});
            Add("Bracer",505,"+160 HP, +5 урона.",hp:160,damage:5,recipe:new[]{7});
            Add("Wraith Band",505,"+80 HP, +8 урона, +2 брони.",hp:80,damage:8,armor:2,recipe:new[]{7});
            Add("Null Talisman",505,"+150 маны, +5 урона.",mp:150,damage:5,recipe:new[]{7});
            Add("Power Treads",1400,"+140 HP, +15 урона, +1.5 скорости.",hp:140,damage:15,speed:1.5f,recipe:new[]{8});
            Add("Phase Boots",1500,"Активно: ускорение на 4 с.","phase",10,damage:18,armor:4,speed:1.4f,recipe:new[]{8});
            Add("Arcane Boots",1300,"Восстановить союзникам 200 маны.","arcane",40,mp:200,speed:1.4f,recipe:new[]{8});
            Add("Blink Dagger",2250,"Прыжок на 14 м. Заблокирован 3 с. после урона.","blink",15);
            Add("Force Staff",2200,"Толчок вперёд на 9 м.","force",19,hp:175,mp:100);
            Add("Black King Bar",4050,"Невосприимчивость к магии и контролю на 6 с.","bkb",75,hp:200,damage:24);
            Add("Aghanim's Scepter",4200,"Улучшает способности героя.",hp:250,mp:200,damage:10);
            Add("Aghanim's Shard",1400,"Постоянное улучшение способностей; не занимает слот.","shard");
            Add("Desolator",3500,"+50 урона. Атаки пробивают 6 брони.",damage:50);
            Add("Daedalus",5150,"+70 урона. Каждая четвёртая атака наносит двойной урон.",damage:70);
            Add("Butterfly",5450,"+40 урона, +8 брони, +0.5 скорости.",damage:40,armor:8,speed:.5f);
            Add("Heart of Tarrasque",5200,"+900 HP. Усиленная регенерация.",hp:900);
            Add("Satanic",5050,"Вампиризм. Активно: лечение и усиление на 6 с.","satanic",45,hp:450,damage:35);
            Add("Shiva's Guard",4850,"Волна холода: урон и замедление вокруг.","shiva",27,mp:300,armor:12);
            Add("Scythe of Vyse",5200,"Вывести выбранную цель из боя на 3 с.","hex",25,mp:350,damage:15);
            Add("Orchid Malevolence",3475,"Безмолвие цели на 4 с. (канал прерывается).","silence",18,mp:200,damage:30);
            Add("Eul's Scepter",2725,"Поднять врага в воздух на 2.5 с.","cyclone",23,mp:200,speed:.6f);
            Add("Dagon",2700,"400 магического урона выбранной цели.","dagon",28,mp:150,damage:10);
            Add("Mekansm",1775,"Восстановить союзникам 300 HP.","mek",45,hp:150,armor:4);
            Add("Pipe of Insight",3475,"Союзники получают магическую защиту на 4 с.","pipe",55,hp:250);
            Add("Blade Mail",2100,"Возвращать часть урона 5 с.","reflect",25,damage:18,armor:6);
            Add("Radiance",4700,"Аура: 45 урона в секунду ближайшим врагам.",damage:55);
            Add("Assault Cuirass",5125,"+15 брони, союзникам рядом +3 брони.",armor:15);
            Add("Hand of Midas",2200,"Убить крипа: 180 золота и 200 опыта.","midas",65);
        }
        static void Add(string n,int c,string d,string a="",float cd=0,float hp=0,float mp=0,float damage=0,float armor=0,float speed=0,int[] recipe=null)
        {Items.Add(new ItemDef{id=Items.Count,name=n,cost=c,description=d,active=a,cooldown=cd,hp=hp,mp=mp,damage=damage,armor=armor,speed=speed,recipe=recipe??new int[0]});}
        public static ItemDef Item(int id)=>Items[Math.Max(0,Math.Min(Items.Count-1,id))];
        public static bool Has(Unit u,int id)=>Array.IndexOf(u.items,id)>=0;
        public static string Talent(int hero,int tier,int side)
        {string[] a={"+180 здоровья","+18 урона","+12% скорости","+350 здоровья"};string[] b={"+160 маны","+5 брони","+30 урона","−25% перезарядки"};return side==0?a[tier]:b[tier];}
    }
}
