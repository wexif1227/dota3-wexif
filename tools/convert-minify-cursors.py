"""Lossless BGRA BMP to RGBA PNG conversion; keeps the Minify cursor alpha."""
import pathlib,struct,zlib,json
root=pathlib.Path(__file__).resolve().parent.parent
source=pathlib.Path.home()/'AppData/Local/Dota2-Minify/mods/D2PFX CURSORS - Necronub Cursor/Necronub Cursor/cursor'
out=root/'Assets/Arena/Resources/DotaImported/Cursors';out.mkdir(parents=True,exist_ok=True)
names=['default','default_enemy','default_team','attack_default','attack_enemy','attack_team','spell_default','spell_enemy','spell_team','move','buy']
def chunk(kind,data):return struct.pack('>I',len(data))+kind+data+struct.pack('>I',zlib.crc32(kind+data)&0xffffffff)
for name in names:
 file=source/('cursor_'+name+'.bmp');data=file.read_bytes();offset=struct.unpack_from('<I',data,10)[0];w,h=struct.unpack_from('<ii',data,18);bits=struct.unpack_from('<H',data,28)[0];assert bits==32
 rows=[]
 for y in range(abs(h)):
  row=data[offset+(abs(h)-1-y if h>0 else y)*w*4:][:w*4];rgba=bytearray()
  for x in range(w):
   b,g,r,a=row[x*4:x*4+4];rgba.extend((r,g,b,a))
  rows.append(b'\0'+rgba)
 png=b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',w,abs(h),8,6,0,0,0))+chunk(b'IDAT',zlib.compress(b''.join(rows)))+chunk(b'IEND',b'')
 (out/(name+'.png')).write_bytes(png)
print('MINIFY_CURSORS_OK',len(names))
