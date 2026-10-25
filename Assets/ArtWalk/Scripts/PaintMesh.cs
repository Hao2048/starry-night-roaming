using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArtWalk
{
    // One combined mesh per surface family keeps thousands of painted marks inexpensive.
    public sealed class PaintMesh
    {
        readonly List<Vector3> vertices = new();
        readonly List<Color> colors = new();
        readonly List<Vector2> uvs = new();
        readonly List<Vector2> motion = new();
        readonly List<int> triangles = new();
        readonly List<Vector4> flowDirections = new();
        readonly List<Vector4> flowCurves = new();
        bool hasFlow;
        Vector4 ribbonDirection,ribbonCurve;
        public void SetRibbonFlow(Vector3 direction,Vector3 curvature,float amplitude,float phase)
        {
            if(!hasFlow){for(int i=0;i<vertices.Count;i++){flowDirections.Add(Vector4.zero);flowCurves.Add(Vector4.zero);}hasFlow=true;}
            ribbonDirection=new Vector4(direction.x,direction.y,direction.z,amplitude);
            ribbonCurve=new Vector4(curvature.x,curvature.y,curvature.z,phase);
        }
        public int VertexCount => vertices.Count;
        public bool HasFlow => hasFlow;
        public int RibbonCount { get; private set; }
        public float LongestRibbonLength { get; private set; }
        public void Tri(Vector3 a, Vector3 b, Vector3 c, Color color, float sway = 0)
        {
            int n = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c);
            for (int k=0;k<3;k++) { colors.Add(color); motion.Add(new Vector2(sway,0)); }
            uvs.Add(Vector2.zero); uvs.Add(Vector2.right); uvs.Add(Vector2.up);
            triangles.Add(n); triangles.Add(n+1); triangles.Add(n+2);
        }
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color, float sway=0)
        {
            int n=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);
            for(int k=0;k<4;k++){colors.Add(color);motion.Add(new Vector2(k>1?sway:0,0));}
            uvs.Add(new Vector2(0,0));uvs.Add(new Vector2(1,0));uvs.Add(new Vector2(1,1));uvs.Add(new Vector2(0,1));
            triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);triangles.Add(n);triangles.Add(n+2);triangles.Add(n+3);
        }
        public void GradientQuad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color ca,Color cb,Color cc,Color cd)
        {
            int n=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);
            colors.Add(ca);colors.Add(cb);colors.Add(cc);colors.Add(cd);
            uvs.Add(Vector2.zero);uvs.Add(Vector2.up);uvs.Add(Vector2.one);uvs.Add(Vector2.right);
            for(int k=0;k<4;k++)motion.Add(Vector2.zero);
            triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);triangles.Add(n);triangles.Add(n+2);triangles.Add(n+3);
        }
        public void Ellipsoid(Vector3 center, Vector3 size, Color color, int segments=10, int rings=7, Quaternion rotation=default)
        {
            if(rotation==default)rotation=Quaternion.identity;
            int n=vertices.Count;
            for(int y=0;y<=rings;y++)for(int x=0;x<=segments;x++){
                float a=x*Mathf.PI*2/segments,b=y*Mathf.PI/rings;
                Vector3 dir=new Vector3(Mathf.Cos(a)*Mathf.Sin(b),Mathf.Cos(b),Mathf.Sin(a)*Mathf.Sin(b));
                vertices.Add(center+rotation*Vector3.Scale(dir,size));
                colors.Add(color*(.94f+.06f*Mathf.Sin(x*2.3f+y*.8f)));uvs.Add(new Vector2((float)x/segments,(float)y/rings));motion.Add(Vector2.zero);
                if(y<rings&&x<segments){int i=n+y*(segments+1)+x;triangles.Add(i);triangles.Add(i+segments+1);triangles.Add(i+1);triangles.Add(i+1);triangles.Add(i+segments+1);triangles.Add(i+segments+2);}
            }
        }
        public void Tube(Vector3[] points,float startRadius,float endRadius,Color color,int sides=7,float sway=0)
        {
            int n=vertices.Count;
            for(int j=0;j<points.Length;j++){
                Vector3 direction=(points[Mathf.Min(j+1,points.Length-1)]-points[Mathf.Max(j-1,0)]).normalized;
                Vector3 right=Vector3.Cross(direction,Mathf.Abs(direction.y)>.95f?Vector3.forward:Vector3.up).normalized;
                Vector3 up=Vector3.Cross(direction,right);float r=Mathf.Lerp(startRadius,endRadius,(float)j/(points.Length-1));
                for(int k=0;k<=sides;k++){float a=k*2*Mathf.PI/sides;vertices.Add(points[j]+(right*Mathf.Cos(a)+up*Mathf.Sin(a))*r);colors.Add(color*(.92f+.08f*Mathf.Sin(a)));uvs.Add(new Vector2((float)k/sides,(float)j/points.Length));motion.Add(new Vector2(sway*j/(points.Length-1),0));
                    if(j<points.Length-1&&k<sides){int i=n+j*(sides+1)+k;triangles.Add(i);triangles.Add(i+1);triangles.Add(i+sides+1);triangles.Add(i+1);triangles.Add(i+sides+2);triangles.Add(i+sides+1);}}
            }
        }
        // Closed elliptical sweeps with transported frames: sculptural volume from every direction.
        public void SculptedBand(Vector3[] points,float[] widths,Color[] pigments,float depthRatio=.5f,int sides=12,Vector3 normalHint=default)
        {
            float length=0;for(int j=1;j<points.Length;j++)length+=Vector3.Distance(points[j-1],points[j]);
            RibbonCount++;LongestRibbonLength=Mathf.Max(LongestRibbonLength,length);
            if(normalHint==default)normalHint=Vector3.forward;
            if(points.Length==4&&sides==4){CompactShortBand(points,widths,pigments,depthRatio,normalHint);return;}
            int first=vertices.Count;Vector3 previous=Vector3.forward,side=Vector3.right;
            for(int j=0;j<points.Length;j++){
                var tangent=(points[Mathf.Min(j+1,points.Length-1)]-points[Mathf.Max(j-1,0)]).normalized;
                if(j==0){side=Vector3.Cross(tangent,normalHint).normalized;if(side.sqrMagnitude<.1f)side=Vector3.Cross(tangent,Vector3.up).normalized;}
                else side=(Quaternion.FromToRotation(previous,tangent)*side).normalized;
                var normal=Vector3.Cross(tangent,side).normalized;previous=tangent;
                for(int k=0;k<=sides;k++){
                    float angle=k*Mathf.PI*2/sides;
                    float rough=1+.025f*Mathf.Sin(j*.71f+angle*3);
                    vertices.Add(points[j]+(side*Mathf.Cos(angle)+normal*(Mathf.Sin(angle)*depthRatio))*widths[j]*rough);
                    colors.Add(pigments[j]*(.96f+.04f*Mathf.Cos(angle*3+j*.15f)));
                    uvs.Add(new Vector2(k/(float)sides,j/(float)(points.Length-1)));motion.Add(Vector2.zero);
                    if(j>0&&k<sides){int a=first+(j-1)*(sides+1)+k,b=a+sides+1;triangles.Add(a);triangles.Add(a+1);triangles.Add(b);triangles.Add(a+1);triangles.Add(b+1);triangles.Add(b);}
                }
            }
            for(int end=0;end<2;end++){
                int point=end==0?0:points.Length-1,ring=first+point*(sides+1),center=vertices.Count;
                vertices.Add(points[point]);colors.Add(pigments[point]);uvs.Add(new Vector2(.5f,end));motion.Add(Vector2.zero);
                for(int k=0;k<sides;k++){triangles.Add(center);triangles.Add(ring+(end==0?k+1:k));triangles.Add(ring+(end==0?k:k+1));}
            }
        }
        void CompactShortBand(Vector3[] points,float[] widths,Color[] pigments,float depthRatio,Vector3 normalHint)
        {
            // A closed triangular cross-section preserves the broad face with 12 vertices / 20 triangles.
            // Four stations keep both tapered ends and the two shoulders of the short brush mark.
            int first=vertices.Count;Vector3 previous=Vector3.forward,side=Vector3.right;
            for(int j=0;j<4;j++){
                var tangent=(points[Mathf.Min(j+1,3)]-points[Mathf.Max(j-1,0)]).normalized;
                if(j==0){side=Vector3.Cross(tangent,normalHint).normalized;if(side.sqrMagnitude<.1f)side=Vector3.Cross(tangent,Vector3.up).normalized;}
                else side=(Quaternion.FromToRotation(previous,tangent)*side).normalized;
                var normal=Vector3.Cross(tangent,side).normalized;previous=tangent;
                for(int k=0;k<3;k++){
                    Vector3 section=k==0?side-normal*depthRatio*.5f:k==1?normal*depthRatio:-side-normal*depthRatio*.5f;
                    vertices.Add(points[j]+section*widths[j]);if(hasFlow){flowDirections.Add(ribbonDirection);flowCurves.Add(ribbonCurve);}colors.Add(pigments[j]*.98f);uvs.Add(new Vector2(k/3f,j/3f));motion.Add(Vector2.zero);
                    if(j>0){int a=first+(j-1)*3+k,b=first+(j-1)*3+(k+1)%3;triangles.Add(a);triangles.Add(b);triangles.Add(a+3);triangles.Add(b);triangles.Add(b+3);triangles.Add(a+3);}
                }
            }
            triangles.Add(first);triangles.Add(first+2);triangles.Add(first+1);
            triangles.Add(first+9);triangles.Add(first+10);triangles.Add(first+11);
        }
        // Short, independent brush marks share one scale across the entire painted world.
        public void ShortBands(Vector3[] points,float[] widths,Color[] pigments,float depthRatio=.18f,int sides=6,Vector3 normalHint=default)
        {
            var distance=new float[points.Length];for(int j=1;j<points.Length;j++)distance[j]=distance[j-1]+Vector3.Distance(points[j-1],points[j]);
            float total=distance[distance.Length-1];if(total<.006f)return;
            float Hash(float value)=>Mathf.Repeat(Mathf.Sin(value*127.1f+points[0].x*17.13f+points[0].y*31.71f+points[0].z*9.31f)*43758.5453f,1);
            void Sample(float d,out Vector3 p,out float w,out Color c){
                int j=0;while(j<distance.Length-2&&distance[j+1]<d)j++;
                float t=Mathf.InverseLerp(distance[j],distance[j+1],d);p=Vector3.Lerp(points[j],points[j+1],t);w=Mathf.Lerp(widths[j],widths[j+1],t);c=Color.Lerp(pigments[j],pigments[j+1],t);
            }
            for(float start=0;start<total-.006f;){
                float length=Mathf.Min(Mathf.Lerp(.42f,1.28f,Hash(start+4)),total-start);
                if(length<.035f)break;
                var path=new Vector3[4];var size=new float[4];var color=new Color[4];
                Sample(start+length*.5f,out var center,out float middleWidth,out var pigment);
                var tangent=(points[points.Length-1]-points[0]).normalized;
                var hint=normalHint==default?Vector3.forward:normalHint;
                var lateral=Vector3.Cross(tangent,hint).normalized;
                float width=Mathf.Min(Mathf.Lerp(.035f,.25f,Hash(start+11)),Mathf.Max(.015f,middleWidth));
                float shift=(Hash(start+17)-.5f)*width*.55f;float tint=Mathf.Lerp(.86f,1.08f,Hash(start+21));
                for(int j=0;j<4;j++){
                    float t=j/3f;Sample(start+length*t,out var p,out _,out var c);
                    path[j]=p+lateral*shift;size[j]=.002f+width*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.42f);
                    color[j]=Color.Lerp(c,pigment,.5f)*tint;
                }
                SculptedBand(path,size,color,Mathf.Clamp(depthRatio,.10f,.22f),4,normalHint);
                start+=length*Mathf.Lerp(1.08f,1.3f,Hash(start+31));
            }
        }
        public void Box(Vector3 center,Vector3 size,Color c,Quaternion rot=default)
        {
            if(rot==default)rot=Quaternion.identity;
            Vector3[] p=new Vector3[8];for(int i=0;i<8;i++)p[i]=center+rot*Vector3.Scale(new Vector3((i&1)==0?-.5f:.5f,(i&2)==0?-.5f:.5f,(i&4)==0?-.5f:.5f),size);
            Quad(p[0],p[1],p[3],p[2],c);Quad(p[5],p[4],p[6],p[7],c);Quad(p[4],p[0],p[2],p[6],c);Quad(p[1],p[5],p[7],p[3],c);Quad(p[2],p[3],p[7],p[6],c);Quad(p[4],p[5],p[1],p[0],c);
        }
        public void Leaf(Vector3 start,Vector3 end,float width,Color c,float sway=0,int steps=6)
        {
            Vector3 dir=end-start;Vector3 side=Vector3.Cross(dir.normalized,Vector3.forward).normalized;if(side.sqrMagnitude<.1f)side=Vector3.right;
            for(int j=0;j<steps;j++){
                float a=(float)j/steps,b=(float)(j+1)/steps;
                Vector3 p=Vector3.Lerp(start,end,a)+Vector3.back*Mathf.Sin(a*Mathf.PI)*width*.25f;
                Vector3 q=Vector3.Lerp(start,end,b)+Vector3.back*Mathf.Sin(b*Mathf.PI)*width*.25f;
                float w=Mathf.Sin(a*Mathf.PI)*width,v=Mathf.Sin(b*Mathf.PI)*width;
                Quad(p-side*w,q-side*v,q+side*v,p+side*w,c,sway*a);
                int n=uvs.Count;uvs[n-4]=new Vector2(0,a);uvs[n-3]=new Vector2(0,b);uvs[n-2]=new Vector2(1,b);uvs[n-1]=new Vector2(1,a);
            }
        }
        // A closed, curved impasto mark: real raised geometry, not a grass card.
        public void Stroke(Vector3 start,Vector3 end,float width,Color color,Vector3 normal,float arch=.025f,float sway=0)
        {
            var direction=(end-start).normalized;normal=normal.normalized;
            var side=Vector3.Cross(direction,normal).normalized;if(side.sqrMagnitude<.1f)side=Vector3.right;
            normal=Vector3.Cross(side,direction).normalized;
            const int steps=5,sides=6;int first=vertices.Count;
            for(int j=0;j<=steps;j++){
                float t=j/(float)steps,w=Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.45f)*width;
                var center=Vector3.Lerp(start,end,t)+normal*Mathf.Sin(t*Mathf.PI)*arch;
                for(int k=0;k<sides;k++){
                    float a=k*Mathf.PI*2/sides;
                    vertices.Add(center+side*(Mathf.Cos(a)*w)+normal*(Mathf.Sin(a)*w*.06f));
                    colors.Add(color*(.96f+.04f*Mathf.Sin(a)));uvs.Add(new Vector2(k/(float)sides,t));motion.Add(new Vector2(sway*(.9f+t*.1f),0));
                    if(j>0){int n=first+(j-1)*sides+k,b=first+(j-1)*sides+(k+1)%sides;triangles.Add(n);triangles.Add(b);triangles.Add(n+sides);triangles.Add(b);triangles.Add(b+sides);triangles.Add(n+sides);}
                }
            }
        }
        public GameObject Create(string name,Transform parent,Material material,bool collision=false)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);
            if(vertices.Count==0)return go;
            foreach(var v in vertices)if(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.x)||float.IsInfinity(v.y)||float.IsInfinity(v.z))throw new System.InvalidOperationException("Non-finite modeled vertex in "+name);
            var mesh=new Mesh{name=name,hideFlags=HideFlags.DontSave,indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};
            mesh.SetVertices(vertices);var packedColors=new List<Color32>(colors.Count);foreach(var color in colors)packedColors.Add(color);mesh.SetColors(packedColors);mesh.SetUVs(0,uvs);mesh.SetUVs(1,motion);
            if(hasFlow){if(flowDirections.Count!=vertices.Count)throw new System.InvalidOperationException("Flow data must cover every stroke vertex.");mesh.SetUVs(2,flowDirections);mesh.SetUVs(3,flowCurves);}mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=collision?ShadowCastingMode.On:ShadowCastingMode.Off;renderer.receiveShadows=true;
            if(collision)go.AddComponent<MeshCollider>().sharedMesh=mesh;
            return go;
        }
    }
}
