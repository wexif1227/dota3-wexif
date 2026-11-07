using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Rift.Core;
namespace Rift.Client
{
    public sealed class Connection : IDisposable
    {
        readonly ConcurrentQueue<string> incoming=new ConcurrentQueue<string>();
        readonly SemaphoreSlim sending=new SemaphoreSlim(1,1);
        CancellationTokenSource stop=new CancellationTokenSource();
        ClientWebSocket socket;
        public bool Connected=>socket!=null&&socket.State==WebSocketState.Open;
        public string Room="",Token="",Status="";
        public int Player;
        public async void Connect(string endpoint,Wire hello)
        {
            try
            {
                Status="Подключение к серверу…";socket=new ClientWebSocket();socket.Options.KeepAliveInterval=TimeSpan.FromSeconds(15);
                using(var timeout=CancellationTokenSource.CreateLinkedTokenSource(stop.Token)){timeout.CancelAfter(TimeSpan.FromSeconds(90));await socket.ConnectAsync(new Uri(endpoint),timeout.Token);}
                Status="Соединение установлено";await SendRaw(JsonUtility.ToJson(hello));await Receive();
            }
            catch(Exception e){if(!stop.IsCancellationRequested){Status="Нет соединения: "+e.Message;incoming.Enqueue(JsonUtility.ToJson(new Wire{type="error",message=Status}));}}
        }
        public async void Send(Wire message){try{await SendRaw(JsonUtility.ToJson(message));}catch(Exception e){Status=e.Message;}}
        async Task SendRaw(string value)
        {
            await sending.WaitAsync(stop.Token);try{if(Connected){var bytes=Encoding.UTF8.GetBytes(value);await socket.SendAsync(new ArraySegment<byte>(bytes),WebSocketMessageType.Text,true,stop.Token);}}finally{sending.Release();}
        }
        async Task Receive()
        {
            var buffer=new byte[65536];
            while(Connected&&!stop.IsCancellationRequested)
            {
                using(var stream=new MemoryStream())
                {
                    WebSocketReceiveResult part;do{part=await socket.ReceiveAsync(new ArraySegment<byte>(buffer),stop.Token);if(part.MessageType==WebSocketMessageType.Close){Status="Соединение закрыто сервером";return;}stream.Write(buffer,0,part.Count);if(stream.Length>2000000)throw new IOException("Snapshot too large");}while(!part.EndOfMessage);
                    incoming.Enqueue(Encoding.UTF8.GetString(stream.ToArray()));while(incoming.Count>12)incoming.TryDequeue(out _);
                }
            }
        }
        public bool Poll(out string value)=>incoming.TryDequeue(out value);
        public void Dispose(){stop.Cancel();socket?.Abort();socket?.Dispose();}
    }
}
