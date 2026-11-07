import assert from 'node:assert/strict';
const url=process.argv[2]||'ws://127.0.0.1:10000/ws';
class Client {
  constructor(){this.messages=[];this.waiters=[];this.socket=new WebSocket(url);this.socket.onmessage=e=>{let m=JSON.parse(e.data);this.messages.push(m);for(const w of [...this.waiters])if(w.test(m)){this.waiters.splice(this.waiters.indexOf(w),1);clearTimeout(w.timer);w.resolve(m);}};}
  ready(){return new Promise((resolve,reject)=>{this.socket.onopen=resolve;this.socket.onerror=reject;});}
  send(value){this.socket.send(JSON.stringify(value));}
  wait(test){return new Promise((resolve,reject)=>{const w={test,resolve,timer:setTimeout(()=>reject(new Error('Message timeout')),10000)};this.waiters.push(w);});}
  async exchange(value,test){const result=this.wait(test);this.send(value);return result;}
  close(){this.socket.close();}
}
const clients=[];
try {
  const a=new Client();clients.push(a);await a.ready();
  const welcome=await a.exchange({type:'create',name:'Host',hero:3,team:0,size:1},m=>m.type==='welcome');
  assert.equal(welcome.room.length,6);assert.ok(welcome.token.length>=32);
  const b=new Client();clients.push(b);await b.ready();
  const wb=await b.exchange({type:'join',room:welcome.room,name:'Guest',hero:1,team:1},m=>m.type==='welcome');
  const lobby=await a.wait(m=>m.type==='state'&&m.players.length===2);assert.equal(lobby.phase,'lobby');
  const denied=await b.exchange({type:'order',order:{type:'start'}},m=>m.type==='error');assert.match(denied.message,/лидер/);
  const begun=await a.exchange({type:'order',order:{type:'start'}},m=>m.type==='state'&&m.phase==='playing');
  assert.ok(begun.units.some(u=>u.owner===welcome.player));assert.ok(!begun.units.some(u=>u.owner===wb.player));
  assert.ok(!('Normal' in begun.units[0].pos));assert.ok(!('path' in begun.units[0]));
  const own=begun.units.find(u=>u.owner===welcome.player);const moved=await a.exchange({type:'order',order:{type:'move',x:-35,y:-35}},m=>m.type==='state'&&m.units.some(u=>u.id===own.id&&u.pos.x>own.pos.x+1));assert.ok(moved.time>begun.time);
  const c=new Client();clients.push(c);await c.ready();await c.exchange({type:'join',room:welcome.room},m=>m.type==='error');
  a.close();await new Promise(r=>setTimeout(r,200));
  const re=new Client();clients.push(re);await re.ready();const resumed=await re.exchange({type:'resume',room:welcome.room,token:welcome.token},m=>m.type==='welcome');assert.equal(resumed.player,welcome.player);
  console.log('PASS network: create, join, capacity, host authorization, start, fog, bounded JSON, authoritative movement, reconnect');
} finally {for(const c of clients)c.close();}
