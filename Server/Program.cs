using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Rift.Core;

var builder=WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:"+(Environment.GetEnvironmentVariable("PORT")??"10000"));
var app=builder.Build();
var rooms=new ConcurrentDictionary<string,Room>();
var resolver=new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver();
resolver.Modifiers.Add(info=>
{
    if(info.Type!=typeof(Unit))return;
    foreach(string hidden in new[]{"path","home","goal","think","stashCount","attackTimer","waypoint","channelTarget","channelSpell","lastDamage","attackWindup","acquireDelay","attackGoal","delivered","formationSlot","aggroTarget","repath","aggroUntil","aggroOrigin","retreating","returning","manualTarget"})
    {var property=info.Properties.FirstOrDefault(p=>p.Name==hidden);if(property!=null)info.Properties.Remove(property);}
    foreach(var property in info.Properties)if(new[]{"skills","items","stash","talents","cooldown","itemCooldown"}.Contains(property.Name))property.ShouldSerialize=(obj,value)=>((Unit)obj).kind==Kind.Hero;
});
var json=new JsonSerializerOptions{IncludeFields=true,IgnoreReadOnlyProperties=true,DefaultIgnoreCondition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault,TypeInfoResolver=resolver};
int nextPlayer=0;
int maxRooms=int.TryParse(Environment.GetEnvironmentVariable("MAX_ROOMS"),out var limit)?Math.Clamp(limit,1,50):8;
app.UseWebSockets(new WebSocketOptions{KeepAliveInterval=TimeSpan.FromSeconds(20)});
app.MapGet("/",()=>Results.Content("RIFT / Dota3 community alpha server. GET /health, WebSocket /ws. No accounts or payments.","text/plain"));
app.MapGet("/health",()=>Results.Json(new{status="ok",version="0.3.1",rooms=rooms.Count,protocol=1,attackWindup=true,unitCollision=true,tacticalBots=true,mapRevision=3}));
app.Map("/ws",async context=>
{
    if(!context.WebSockets.IsWebSocketRequest){context.Response.StatusCode=400;return;}
    using var socket=await context.WebSockets.AcceptWebSocketAsync();
    var peer=new Peer(socket,json);Room room=null;Session session=null;
    using var stop=CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
    var sending=peer.SendLoop(stop.Token);
    int requests=0;var window=DateTime.UtcNow;
    try
    {
        while(socket.State==WebSocketState.Open&&!stop.IsCancellationRequested)
        {
            using var message=new MemoryStream();var buffer=new byte[4096];WebSocketReceiveResult part;
            do {part=await socket.ReceiveAsync(new ArraySegment<byte>(buffer),stop.Token);if(part.MessageType==WebSocketMessageType.Close)return;if(part.MessageType!=WebSocketMessageType.Text||message.Length+part.Count>4096){await socket.CloseAsync(WebSocketCloseStatus.MessageTooBig,"max 4096 bytes",CancellationToken.None);return;}message.Write(buffer,0,part.Count);}while(!part.EndOfMessage);
            if((DateTime.UtcNow-window).TotalSeconds>=1){requests=0;window=DateTime.UtcNow;}if(++requests>60){peer.Error("Слишком много команд");continue;}
            Wire w;try{w=JsonSerializer.Deserialize<Wire>(message.ToArray(),json);}catch(JsonException){peer.Error("Неверный JSON");continue;}if(w==null)continue;
            if(w.type=="ping"){peer.Push(new Wire{type="pong"});continue;}
            if(w.type=="create"||w.type=="join"||w.type=="resume")
            {
                if(session!=null){peer.Error("Вы уже в комнате");continue;}
                if(w.type=="create")
                {
                    if(rooms.Count>=maxRooms){peer.Error("Сервер заполнен. Попробуйте позднее.");continue;}
                    string code=Guid.NewGuid().ToString("N").Substring(0,6).ToUpperInvariant();
                    room=new Room{match=new Match(w.size,w.difficulty){Room=code}};if(!rooms.TryAdd(code,room)){peer.Error("Повторите создание комнаты");continue;}
                }
                else if(!rooms.TryGetValue((w.room??"").Trim().ToUpperInvariant(),out room)){peer.Error("Комната не найдена");continue;}
                lock(room.gate)
                {
                    if(w.type=="resume")
                    {
                        session=room.sessions.Find(s=>s.token==w.token&&s.token.Length>20);
                        if(session==null){peer.Error("Сессия истекла");continue;}
                        session.peer?.Abort();session.peer=peer;var player=room.match.Players.Find(p=>p.id==session.id);player.connected=true;var hero=room.match.Hero(session.id);if(hero!=null)hero.bot=false;
                    }
                    else
                    {
                        int id=Interlocked.Increment(ref nextPlayer);var player=room.match.AddPlayer(id,w.name,w.hero,w.team);
                        if(player==null){peer.Error("Матч уже идёт или комната заполнена");continue;}
                        session=new Session{id=id,token=Guid.NewGuid().ToString("N"),peer=peer};room.sessions.Add(session);if(room.sessions.Count==1)room.match.Host=id;
                    }
                    room.touched=DateTime.UtcNow;peer.Push(new Wire{type="welcome",room=room.match.Room,player=session.id,token=session.token});
                }
                continue;
            }
            if(w.type=="order"&&session!=null&&room!=null)
            {
                lock(room.gate){string error=room.match.Command(session.id,w.order);if(error!="")peer.Error(error);room.touched=DateTime.UtcNow;}
            }
        }
    }
    catch(OperationCanceledException){}catch(WebSocketException){}catch(Exception e){app.Logger.LogWarning(e,"Client connection ended");}
    finally
    {
        if(session!=null&&room!=null)lock(room.gate)
        {
            if(session.peer==peer){session.peer=null;var player=room.match.Players.Find(p=>p.id==session.id);if(player!=null)player.connected=false;var hero=room.match.Hero(session.id);if(hero!=null)hero.bot=true;
                if(room.match.Phase=="lobby"&&room.match.Host==session.id){var replacement=room.sessions.Find(s=>s.peer!=null);if(replacement!=null)room.match.Host=replacement.id;}}
        }
        stop.Cancel();peer.Abort();try{await sending;}catch(OperationCanceledException){}
    }
});
var ticker=Task.Run(async()=>
{
    using var timer=new PeriodicTimer(TimeSpan.FromMilliseconds(50));int tick=0;
    while(await timer.WaitForNextTickAsync(app.Lifetime.ApplicationStopping))
    {
        tick++;
        foreach(var pair in rooms)
        {
            var room=pair.Value;lock(room.gate)
            {
                if(room.sessions.All(s=>s.peer==null)&&(DateTime.UtcNow-room.touched).TotalMinutes>5){rooms.TryRemove(pair.Key,out _);continue;}
                room.match.Tick(.05f);
                if(tick%2==0)foreach(var session in room.sessions)if(session.peer!=null)session.peer.State(JsonSerializer.Serialize(room.match.View(session.id),json));
            }
        }
    }
},app.Lifetime.ApplicationStopping);
await app.RunAsync();

class Room
{
    public readonly object gate=new();public Match match;public List<Session> sessions=new();public DateTime touched=DateTime.UtcNow;
}
class Session { public int id;public string token;public Peer peer; }
class Peer
{
    readonly WebSocket socket;readonly JsonSerializerOptions json;
    readonly Channel<string> queue=Channel.CreateBounded<string>(new BoundedChannelOptions(32){FullMode=BoundedChannelFullMode.DropOldest,SingleReader=true});
    string latest;
    public Peer(WebSocket socket,JsonSerializerOptions json){this.socket=socket;this.json=json;}
    public void Push(object value)=>queue.Writer.TryWrite(JsonSerializer.Serialize(value,json));
    public void Error(string message)=>Push(new Wire{type="error",message=message});
    public void State(string value){Interlocked.Exchange(ref latest,value);queue.Writer.TryWrite("");}
    public void Abort(){socket.Abort();queue.Writer.TryComplete();}
    public async Task SendLoop(CancellationToken token)
    {
        try{await foreach(var value in queue.Reader.ReadAllAsync(token))
        {
            string msg=value==""?Interlocked.Exchange(ref latest,null):value;if(msg==null)continue;
            using var timeout=CancellationTokenSource.CreateLinkedTokenSource(token);timeout.CancelAfter(TimeSpan.FromSeconds(5));
            await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg)),WebSocketMessageType.Text,true,timeout.Token);
        }}catch(WebSocketException){}catch(OperationCanceledException){}catch(ObjectDisposedException){}
    }
}
