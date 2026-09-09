using System.Collections.Generic;
using UnityEngine;
using MetroChicka.Core;

namespace MetroChicka
{
    public static class ToyFactory
    {
        public static readonly Color[] Palette={Hex("EB765C"),Hex("51B5B2"),Hex("B5A2D2")};
        public static Color Hex(string h) { ColorUtility.TryParseHtmlString("#"+h,out var c); return c; }
        private static readonly Dictionary<Color,Material> Materials=new Dictionary<Color,Material>();
        private static Mesh lower,upper;
        public static Material Mat(Color c)
        {
            if(Materials.TryGetValue(c,out var m) && m!=null) return m;
            m=new Material(Shader.Find("Standard")); m.color=c; m.SetFloat("_Glossiness",.26f);
            Materials[c]=m; return m;
        }
        public static Vector3 World(Point p,float h=0) => new Vector3(p.X,h,p.Y);
        public static GameObject Group(string name,Transform parent=null)
        {
            var g=new GameObject(name); if(parent!=null) g.transform.SetParent(parent,false); return g;
        }
        public static GameObject Shape(string name,PrimitiveType type,Transform parent,Vector3 pos,Vector3 size,Color c)
        {
            var g=GameObject.CreatePrimitive(type); g.name=name; g.transform.SetParent(parent,false);
            g.transform.localPosition=pos; g.transform.localScale=size; g.GetComponent<Renderer>().sharedMaterial=Mat(c);
            Object.Destroy(g.GetComponent<Collider>()); return g;
        }
        public static GameObject Line(string name,Transform parent,Vector3 a,Vector3 b,float width,Color c)
        {
            var g=Group(name,parent); var lr=g.AddComponent<LineRenderer>();
            lr.useWorldSpace=false; lr.sharedMaterial=Mat(c); lr.positionCount=2; lr.SetPositions(new[]{a,b});
            lr.startWidth=lr.endWidth=width; lr.numCapVertices=4; lr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            return g;
        }
        public static GameObject Chicka(Transform parent,Color color,int id,bool interactive=true)
        {
            var root=Group("Chicka_"+id,parent);
            if(lower==null) lower=Lathe(0,.72f);
            if(upper==null) upper=Lathe(.75f,1.72f);
            MeshPart("LowerShell",root.transform,lower,color);
            var top=MeshPart("UpperShell",root.transform,upper,color);
            Shape("Seam",PrimitiveType.Cylinder,root.transform,new Vector3(0,.735f,0),new Vector3(.96f,.018f,.96f),Hex("F9D588"));
            Shape("Face",PrimitiveType.Sphere,top.transform,new Vector3(0,1.31f,-.323f),new Vector3(.55f,.48f,.16f),Hex("FFF1CF"));
            foreach(float x in new[]{-.115f,.115f}) Shape("Eye",PrimitiveType.Sphere,top.transform,new Vector3(x,1.35f,-.414f),new Vector3(.045f,.07f,.025f),Hex("253D46"));
            var nose=Shape("Beak",PrimitiveType.Cube,top.transform,new Vector3(0,1.22f,-.432f),new Vector3(.105f,.07f,.09f),Hex("EEAA4C"));
            nose.transform.localRotation=Quaternion.Euler(0,0,45);
            Shape("Belly",PrimitiveType.Sphere,root.transform,new Vector3(0,.39f,-.409f),new Vector3(.51f,.47f,.12f),Hex("FDE9C0"));
            Shape("Crown",PrimitiveType.Sphere,top.transform,new Vector3(0,1.77f,0),new Vector3(.11f,.20f,.14f),Hex("F9D588"));
            if(interactive)
            {
                var inner=Chicka(root.transform,Color.Lerp(color,Hex("FFF1CF"),.18f),id,false);inner.name="InnerChicka";inner.transform.localPosition=new Vector3(0,.40f,0);inner.transform.localScale=Vector3.one*.45f;
                var col=root.AddComponent<CapsuleCollider>();col.radius=.55f;col.height=1.9f;col.center=new Vector3(0,.8f,0); root.AddComponent<ChickaHit>().Id=id;
            }
            return root;
        }
        private static GameObject MeshPart(string name,Transform parent,Mesh mesh,Color color)
        {
            var g=Group(name,parent);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=Mat(color);return g;
        }
        private static float Radius(float y)
        {
            float[] ys={0,.10f,.38f,.72f,.92f,1.03f,1.20f,1.42f,1.61f,1.72f};
            float[] rs={.30f,.43f,.54f,.49f,.33f,.29f,.40f,.39f,.26f,.025f};
            for(int i=1;i<ys.Length;i++) if(y<=ys[i]) return Mathf.Lerp(rs[i-1],rs[i],Mathf.InverseLerp(ys[i-1],ys[i],y));
            return .025f;
        }
        private static Mesh Lathe(float low,float high)
        {
            const int rings=18, sides=40; var verts=new List<Vector3>();var triangles=new List<int>();
            for(int r=0;r<=rings;r++)
            {
                float y=Mathf.Lerp(low,high,r/(float)rings),radius=Radius(y);
                for(int s=0;s<=sides;s++) {float a=s*Mathf.PI*2/sides;verts.Add(new Vector3(Mathf.Cos(a)*radius,y,Mathf.Sin(a)*radius));}
            }
            for(int r=0;r<rings;r++) for(int s=0;s<sides;s++)
            { int i=r*(sides+1)+s; triangles.AddRange(new[]{i,i+sides+1,i+1,i+1,i+sides+1,i+sides+2}); }
            // Close both ends so opened shells never expose a missing surface.
            foreach(int row in new[]{0,rings})
            {
                int center=verts.Count;verts.Add(new Vector3(0,row==0?low:high,0));
                for(int s=0;s<sides;s++) triangles.AddRange(row==0?new[]{center,s,s+1}:new[]{center,row*(sides+1)+s+1,row*(sides+1)+s});
            }
            var mesh=new Mesh{name="Matryoshka shell"};mesh.SetVertices(verts);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        public static GameObject Train(Transform parent,Color color)
        {
            var g=Group("Train",parent);
            Shape("Carriage",PrimitiveType.Cube,g.transform,new Vector3(0,.27f,0),new Vector3(.43f,.32f,.80f),color);
            Shape("Roof",PrimitiveType.Cube,g.transform,new Vector3(0,.47f,.07f),new Vector3(.46f,.10f,.67f),Hex("FFF1CF"));
            Shape("Window",PrimitiveType.Cube,g.transform,new Vector3(0,.30f,.414f),new Vector3(.30f,.16f,.016f),Hex("243E49"));
            foreach(float x in new[]{-.20f,.20f}) foreach(float z in new[]{-.24f,.24f}) Shape("Wheel",PrimitiveType.Sphere,g.transform,new Vector3(x,.11f,z),Vector3.one*.14f,Hex("30444B"));
            var child=Chicka(g.transform,color,-1,false);child.name="Passenger";child.transform.localPosition=new Vector3(0,.54f,-.06f);child.transform.localScale=Vector3.one*.21f;
            return g;
        }
        public static GameObject Road(Road road,Transform parent,Color color,bool preview=false)
        {
            var group=Group(preview?"PreviewRoad":"Road",parent);
            foreach(var segment in road.Segments())
            {
                var a=World(segment.a);var b=World(segment.b);float length=Vector3.Distance(a,b);var dir=(b-a).normalized;var normal=Vector3.Cross(dir,Vector3.up)*.13f;
                bool bridge=road.Kind==Shell.Bridge;
                float Height(float t)=>preview?.08f:bridge?.08f+Mathf.Sin(t*Mathf.PI)*.42f:.07f;
                int parts=bridge?16:1;
                for(int i=0;i<parts;i++) foreach(float side in new[]{-1f,1f})
                {
                    float t0=i/(float)parts,t1=(i+1f)/parts;
                    Line("Rail",group.transform,Vector3.Lerp(a,b,t0)+Vector3.up*Height(t0)+normal*side,Vector3.Lerp(a,b,t1)+Vector3.up*Height(t1)+normal*side,preview?.038f:.055f,color);
                }
                if(!preview) for(float d=.12f;d<length;d+=.34f)
                {
                    var p=Vector3.Lerp(a,b,d/length)+Vector3.up*(Height(d/length)-.023f);
                    var tie=Shape("Sleeper",PrimitiveType.Cube,group.transform,p,new Vector3(.40f,.055f,.10f),Hex("B49E7D"));tie.transform.localRotation=Quaternion.LookRotation(dir);
                }
            }
            return group;
        }
        public static void Ring(Transform parent,Vector3 pos,float radius,Color color,float width=.045f)
        {
            var g=Group("Ring",parent);var line=g.AddComponent<LineRenderer>();line.useWorldSpace=false;line.sharedMaterial=Mat(color);line.positionCount=49;line.startWidth=line.endWidth=width;
            for(int i=0;i<=48;i++){float a=i*Mathf.PI*2/48;line.SetPosition(i,pos+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius));}
        }
    }
    public sealed class ChickaHit:MonoBehaviour { public int Id; }
}
