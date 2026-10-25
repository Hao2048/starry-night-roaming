"""Editor-only brush plan. RGB samples become vertex pigments; never creates a runtime texture.
A structure tensor estimates the direction of local paint marks. Region shapes are modeled separately.
"""
from PIL import Image, ImageFilter, ImageDraw
import numpy as np, math, struct
from pathlib import Path
src=Path('Assets/ArtWalk/Resources/Paintings/StarryNight.jpg')
if not src.exists(): src=Path('Assets/StarryStudy/Editor/Reference/StarryNight.jpg')
im=Image.open(src).convert('RGB').crop((16,15,1233,983)).resize((1004,800),Image.Resampling.LANCZOS)
a=np.array(im).astype(float)/255
# Local structure at the scale of an original brush dab, rather than low frequency object gradients.
g=np.array(im.convert('L').filter(ImageFilter.GaussianBlur(.9))).astype(float)/255
gy,gx=np.gradient(g)
def smooth(v):
 scale=max(abs(v).max(),1e-8)
 return (np.asarray(Image.fromarray(np.uint8(np.clip(v/scale*.5+.5,0,1)*255)).filter(ImageFilter.GaussianBlur(2))).astype(float)/255*2-1)*scale
xx,yy,xy=smooth(gx*gx),smooth(gy*gy),smooth(gx*gy)
theta=.5*np.arctan2(2*xy,xx-yy)+np.pi/2
# Brush-only continuation behind occluders, sampled from visible sky/terrain patches.
W,H=1004,800
mask=Image.new('L',(W,H));draw=ImageDraw.Draw(mask)
poly=[(.198,.055),(.209,.079),(.209,.13),(.200,.18),(.207,.24),(.219,.28),(.226,.40),(.244,.51),(.253,.64),(.269,.69),(.280,.57),(.299,.66),(.324,.705),(.360,.74),(.379,.80),(.375,.85),(.440,.92),(.458,.97),(.445,1.03),(.180,1.03),(.140,.96),(.119,.88),(.124,.82),(.148,.73),(.160,.64),(.144,.59),(.150,.54),(.159,.49),(.156,.42),(.147,.32),(.157,.315),(.173,.42),(.182,.32),(.185,.23),(.185,.17),(.190,.11)]
draw.polygon([(int(x*W),int(y*H)) for x,y in poly],fill=255)
tree=np.array(mask)>0
boundary=np.interp(np.arange(W)/W,[0,.16,.28,.4,.48,.56,.62,.71,.78,.85,.90,.95,1],[.765,.73,.755,.72,.69,.71,.65,.635,.62,.597,.55,.515,.52])
expanded=np.array(mask.filter(ImageFilter.MaxFilter(21)))>0
sky_chroma=((a[:,:,2]>a[:,:,0]*1.05)&(a[:,:,2]>a[:,:,1]*.90))|(a.mean(axis=2)>.57)
valid_sky=(np.arange(H)[:,None]/H < boundary[None,:]-.015)&(~expanded)&sky_chroma
valid_land=(np.arange(H)[:,None]/H > .79)&(~tree)
houses=[(.490,.799,.579,.885),(.427,.814,.490,.876),(.574,.835,.640,.887),(.649,.825,.734,.899),(.762,.869,.832,.939),(.831,.895,.916,.970),(.610,.898,.722,1.025),(.673,.948,.776,1.025),(.488,.907,.611,.961),(.479,.960,.600,1.025),(.886,.871,.958,.922)]
for x0,y0,x1,y1 in houses:valid_land[int(y0*H):int(y1*H),int(x0*W):int(x1*W)]=False
valid_land[:,int(.554*W):int(.58*W)]=False
sky_rows=[np.flatnonzero(row) for row in valid_sky]
land_rows=[np.flatnonzero(row) for row in valid_land]
def continuation(ix,iy,sky=True):
 rows=sky_rows if sky else land_rows
 if sky and (iy>=H or len(rows[min(H-1,iy)])<12):iy=int((.38+.15*math.sin(iy*.006))*H)
 if not sky:iy=max(int(.8*H),min(H-1,iy))
 iy=max(0,min(H-1,iy)); candidates=rows[iy]
 if len(candidates)==0: return np.array([.2,.32,.35]),0
 # Nearby patches retain small color/line variation, rather than a smooth filler gradient.
 nearest=int(np.argmin(abs(candidates-ix))); nearest=int(np.clip(nearest+int(12*math.sin(ix*.071+iy*.13)),0,len(candidates)-1))
 sx=int(candidates[nearest]);return a[iy,sx],theta[iy,sx]
# Every stroke is its own closed mesh; a subpixel-jittered distribution avoids raster rows.
rng=np.random.default_rng(188911)
records=[]
for y in np.arange(-6,806,2.8):
 for x in np.arange(-6,1010,2.8):
  px=x+rng.uniform(-1.35,1.35); py=y+rng.uniform(-1.35,1.35)
  ix=int(np.clip(px,0,1003));iy=int(np.clip(py,0,799))
  angle=theta[iy,ix]+rng.uniform(-.11,.11)
  length=rng.uniform(6.5,12.5)/800; width=rng.uniform(1.65,2.45)/800
  c=a[iy,ix]*rng.uniform(.98,1.03)
  sky,sa=continuation(ix,iy,True);land,la=continuation(ix,iy,False)
  is_tree=bool(expanded[iy,ix] and ((a[iy,ix,2]<a[iy,ix,1]*.98) or (a[iy,ix,2]<a[iy,ix,0]*.9)) and a[iy,ix].mean()<.38)
  records.append((px/1004,py/800,float(angle),length,width,*c,*sky,float(sa),*land,float(la),1.0 if is_tree else 0.0))
# Continue the actual mesh world beyond all four edges of the reference camera.
for py in np.arange(-220,1020,3.3):
 for px in np.arange(-240,1244,3.3):
  if -6<px<1010 and -6<py<806:continue
  u=px/1004;v=py/800
  su=abs(u) if u<0 else 2-u if u>1 else u
  sv=abs(v) if v<0 else 2-v if v>1 else v
  ix=int(np.clip(su*W,0,W-1));iy=int(np.clip(sv*H,0,H-1))
  boundary_here=boundary[ix]
  hint=-1.0 if v<boundary_here else -2.0 if v<.79 else -3.0
  if hint==-1.0:c,angle=continuation(ix,iy,True)
  else:
   c=a[iy,ix];angle=theta[iy,ix]
   if tree[iy,ix]:c,angle=continuation(ix,iy,False)
  if u<0 or u>1:angle=math.pi-angle
  if v<0 or v>1:angle=-angle
  records.append((u,v,float(angle),rng.uniform(.008,.015),rng.uniform(.0022,.0031),*c,*c,float(angle),*c,float(angle),hint))
out=Path('Assets/StarryStudy/Editor/Reference/BrushPlan.bytes')
with out.open('wb') as f:
 f.write(struct.pack('<i',len(records)))
 for r in records:f.write(struct.pack('<17f',*r))
print('editor brush measurements:',len(records), 'bytes',out.stat().st_size)
im.save('.work/reference-fit/reference-cropped.png')
