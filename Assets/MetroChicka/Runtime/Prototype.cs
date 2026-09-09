using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MetroChicka.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MetroChicka
{
    public sealed class Prototype : MonoBehaviour
    {
        public Board Model { get; private set; }
        public bool Busy { get; private set; }
        public int Selected { get; private set; }
        public int ScenarioIndex { get; private set; }
        public int ActiveTrains => trains==null?0:trains.childCount;
        public Camera View { get; private set; }
        public float AnimationSpeed=1;
        private Transform scene,actors,roads,ghost,trains,labels;
        private readonly Dictionary<int,GameObject> dolls=new Dictionary<int,GameObject>();
        private readonly Stack<Board> undo=new Stack<Board>();
        private readonly Dictionary<string,Text> texts=new Dictionary<string,Text>();
        private readonly List<Button> buttons=new List<Button>();
        private Font font; private Canvas canvas; private GameObject help;
        private bool dragging,muted,motion=true;
        private Vector3 dragStart; private int aimed;
        private AudioSource sound; private AudioClip pop;
        private static readonly string[] Names={"PIP","MOMO","LULU"};
        public static string LayerName(Shell kind)=>new[]{"직선","굽은 길","교량","분기"}[(int)kind];
        public static string ScenarioName(int i)=>new[]{"01  첫 펼침","02  이어지는 운행","03  겹의 순서"}[i];
        private void Awake()
        {
            Application.runInBackground=true; Application.targetFrameRate=60;
            SetupWorld();SetupUI();SetupAudio();LoadScenario(0);
        }
        private void SetupWorld()
        {
            View=Camera.main;
            if(View==null) { var cameraObject=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));cameraObject.tag="MainCamera";View=cameraObject.GetComponent<Camera>(); }
            View.clearFlags=CameraClearFlags.SolidColor;View.backgroundColor=ToyFactory.Hex("172A35");
            View.orthographic=true;View.orthographicSize=8.65f;View.allowHDR=false;View.transform.position=new Vector3(0,17,-19);View.transform.LookAt(new Vector3(0,0,.35f));
            View.nearClipPlane=.1f;View.farClipPlane=100;
            RenderSettings.ambientLight=ToyFactory.Hex("8B99AA");RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowDistance=45;QualitySettings.antiAliasing=4;
            var sun=new GameObject("Softbox").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=.85f;sun.color=ToyFactory.Hex("FFF1D4");sun.shadows=LightShadows.Soft;sun.shadowStrength=.42f;sun.transform.rotation=Quaternion.Euler(48,-30,0);
            scene=ToyFactory.Group("Diorama").transform;
            ToyFactory.Shape("Desk",PrimitiveType.Cube,scene,new Vector3(0,-.65f,0),new Vector3(70,.5f,70),ToyFactory.Hex("233A45"));
            ToyFactory.Shape("Board rim",PrimitiveType.Cube,scene,new Vector3(0,-.23f,0),new Vector3(18.2f,.55f,12.2f),ToyFactory.Hex("BC9D78"));
            ToyFactory.Shape("Ivory board",PrimitiveType.Cube,scene,new Vector3(0,-.07f,0),new Vector3(18,.24f,12),ToyFactory.Hex("E9E2D2"));
            for(int x=-8;x<=8;x++) for(int y=-5;y<=5;y++)
                ToyFactory.Shape("Grid dot",PrimitiveType.Sphere,scene,new Vector3(x,.059f,y),new Vector3(.047f,.018f,.047f),ToyFactory.Hex("ADAFA4"));
            // Corner details establish a tangible tabletop, without obscuring the playing surface.
            foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1}) ToyFactory.Shape("Brass screw",PrimitiveType.Cylinder,scene,new Vector3(x*8.65f,.075f,y*5.65f),new Vector3(.15f,.012f,.15f),ToyFactory.Hex("B39870"));
            roads=ToyFactory.Group("Permanent roads",scene).transform;actors=ToyFactory.Group("Chickas",scene).transform;ghost=ToyFactory.Group("Preview",scene).transform;trains=ToyFactory.Group("Trains",scene).transform;labels=ToyFactory.Group("Board labels",scene).transform;
        }
        private void SetupUI()
        {
            font=Resources.Load<Font>("Fonts/NotoSansKR-Regular");
            if(font==null) font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var g=new GameObject("Interface",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas=g.GetComponent<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=View;canvas.planeDistance=1;
            var scale=g.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1440,900);scale.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            if(FindFirstObjectByType<EventSystem>()==null) new GameObject("Event System",typeof(EventSystem),typeof(StandaloneInputModule));
            Label("Brand","METRO CHICKA",30,24,420,48,30,ToyFactory.Hex("FFF0D1"));
            Label("Edition","UNFOLD STUDY  /  3D PLAYTEST",32,71,500,26,11,ToyFactory.Hex("B5C7CA"));
            Label("Score","0000",1110,22,290,45,30,ToyFactory.Hex("FFCF81"),TextAnchor.MiddleRight);
            Label("Resources","",980,67,420,30,13,ToyFactory.Hex("CBD7D4"),TextAnchor.MiddleRight);
            for(int i=0;i<3;i++){int index=i;Button("Scenario"+i,ScenarioName(i),30+i*235,113,220,43,()=>LoadScenario(index));}
            Button("Help","도움말  H",1260,114,140,42,ToggleHelp);
            Label("Message","",35,171,1060,35,17,ToyFactory.Hex("F5E9CE"));
            Label("Combo","",420,218,600,65,34,ToyFactory.Hex("FFD38A"),TextAnchor.MiddleCenter);
            Label("Selection","",32,715,550,39,21,ToyFactory.Hex("FFF0D1"));
            Label("Layers","",33,755,800,32,16,ToyFactory.Hex("C8D7D6"));
            Label("Forecast","",33,793,1020,29,15,ToyFactory.Hex("F5CC8B"));
            Button("Rotate","회전  Q / E",1080,724,148,43,()=>Rotate(1));
            Button("Unfold","한 겹 펼치기  SPACE",1240,724,160,43,()=>Commit());
            Button("Relay","연쇄 ON",30,839,160,37,ToggleRelay);
            Button("Undo","되돌리기  Z",205,839,160,37,Undo);
            Button("Restart","처음부터  R",380,839,160,37,()=>LoadScenario(ScenarioIndex));
            Button("Motion","동작 기본",1000,839,125,37,()=>{motion=!motion;RefreshUI();});
            Button("Sound","소리 ON",1140,839,115,37,()=>{muted=!muted;RefreshUI();});
            Button("Quit","종료",1270,839,130,37,()=>Application.Quit());
            Label("Hint","Chicka를 잡아 방향으로 당긴 뒤 놓으세요 · 클릭 선택 / 방향키 회전",550,844,450,26,11,ToyFactory.Hex("A7BCBE"));
            help=new GameObject("Help sheet",typeof(RectTransform),typeof(Image));help.transform.SetParent(canvas.transform,false);Rect(help.GetComponent<RectTransform>(),230,215,980,470);help.GetComponent<Image>().color=ToyFactory.Hex("19323E");
            Label("HelpTitle","작은 몸 안에, 다음 길이 있습니다.",265,244,890,65,26,ToyFactory.Hex("FFF0D1"),TextAnchor.MiddleLeft,help.transform);
            Label("HelpBody","01   Chicka를 클릭해 선택하고 드래그로 방향을 잡으세요.\n02   놓으면 한 겹이 길이 되고, 작은 열차가 출발합니다.\n03   열차에서 내부 Chicka가 내리면 열차는 사라집니다.\n04   연쇄 ON에서는 연결된 길의 신호가 다른 Chicka를 엽니다.\n\n직선  4칸 직진     굽은 길  2칸 + 왼쪽 2칸\n교량  중간 교차점의 신호를 건너뜀     분기  옆으로 신호 전달\n\n겹은 보충되지 않습니다. 한 연쇄에서 같은 Chicka는 한 번만 열립니다.\n되돌리기로 같은 행동을 ON / OFF 상태에서 비교해 보세요.",265,315,890,315,16,ToyFactory.Hex("D8E4DC"),TextAnchor.UpperLeft,help.transform);
            Button("CloseHelp","닫기  ESC",1020,617,155,43,ToggleHelp,help.transform);help.SetActive(false);
        }
        private static void Rect(RectTransform r,float x,float y,float w,float h)
        {
            r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);
        }
        private Text Label(string name,string value,float x,float y,float w,float h,int size,Color color,TextAnchor align=TextAnchor.MiddleLeft,Transform parent=null)
        {
            var g=new GameObject(name,typeof(RectTransform),typeof(Text));g.transform.SetParent(parent??canvas.transform,false);
            // Help uses absolute reference coordinates translated to its panel origin.
            Rect(g.GetComponent<RectTransform>(),parent==null?x:x-230,parent==null?y:y-215,w,h);
            var t=g.GetComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.color=color;t.alignment=align;t.raycastTarget=false;t.lineSpacing=.85f;
            t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Overflow;texts[name]=t;return t;
        }
        private void Button(string name,string value,float x,float y,float w,float h,Action action,Transform parent=null)
        {
            var g=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));g.transform.SetParent(parent??canvas.transform,false);
            Rect(g.GetComponent<RectTransform>(),parent==null?x:x-230,parent==null?y:y-215,w,h);
            g.GetComponent<Image>().color=ToyFactory.Hex(name=="Unfold"?"B86D50":"304B55");
            var b=g.GetComponent<Button>();b.onClick.AddListener(()=>{if(!Busy)action();});buttons.Add(b);
            var child=new GameObject("Caption",typeof(RectTransform),typeof(Text));child.transform.SetParent(g.transform,false);
            var r=child.GetComponent<RectTransform>();r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(5,0);r.offsetMax=new Vector2(-5,0);
            var text=child.GetComponent<Text>();text.font=font;text.fontSize=14;text.text=value;text.alignment=TextAnchor.MiddleCenter;text.color=ToyFactory.Hex("FFF0D1");text.raycastTarget=false;text.lineSpacing=.85f;texts[name+"Caption"]=text;
        }
        private void SetupAudio()
        {
            sound=gameObject.AddComponent<AudioSource>();sound.playOnAwake=false;
            const int rate=22050;var samples=new float[4400];
            for(int i=0;i<samples.Length;i++){float t=i/(float)rate;samples[i]=(Mathf.Sin(t*1800*Mathf.PI)+Mathf.Sin(t*2900*Mathf.PI)*.3f)*Mathf.Exp(-t*28)*.22f;}
            pop=AudioClip.Create("Wooden snap",samples.Length,1,rate,false);pop.SetData(samples,0);
        }
        private void TickSound(int i){if(muted)return;sound.pitch=1+i*.16f;sound.PlayOneShot(pop);}
        public void LoadScenario(int index)
        {
            if(Busy)return;ScenarioIndex=index;Model=Board.Scenario(index);Selected=0;aimed=Model.Chickas[0].Direction;undo.Clear();dragging=false;
            Rebuild();Message(index==0?"한 겹을 열어 보세요. 길은 남고, 내부 Chicka가 다음 위치로 이동합니다.":index==1?"PIP을 오른쪽으로 펼치면 3개의 Chicka가 이어집니다. 연쇄 OFF와 비교해 보세요.":"분기는 신호를 나누고, 교량은 교차 신호를 건너뜁니다. 다음 겹까지 살펴보세요.");
        }
        public bool Select(int id)
        {
            if(Busy||Model.Chickas.All(c=>c.Id!=id))return false;
            Selected=id;aimed=Model.Chickas.Find(c=>c.Id==id).Direction;Preview();return true;
        }
        public void Rotate(int delta)
        {
            if(Busy)return;aimed=(aimed+delta+4)%4;Model.Aim(Selected,aimed);Preview();
        }
        public void ToggleRelay(){if(Busy)return;Model.SetRelay(!Model.Relay);Preview();}
        public void Undo()
        {
            if(Busy||undo.Count==0)return;Model=undo.Pop();aimed=Model.Chickas.Find(c=>c.Id==Selected).Direction;Rebuild();Message("직전 펼침을 되돌렸습니다. 같은 배치를 다른 방향이나 연쇄 설정으로 비교해 보세요.");
        }
        public void ToggleHelp(){PeelFeedback(0);help.SetActive(!help.activeSelf);dragging=false;}
        private void Message(string s){texts["Message"].text=s;}
        private void Clear(Transform parent)
        {
            foreach(Transform c in parent){c.gameObject.SetActive(false);Destroy(c.gameObject);}
        }
        private void Rebuild()
        {
            Clear(actors);Clear(roads);Clear(ghost);Clear(labels);dolls.Clear();
            foreach(var road in Model.Roads)ToyFactory.Road(road,roads,ToyFactory.Palette[road.Owner%3]*.78f);
            // Offset only the visible dolls when two arrivals share a cell; rules remain on the grid.
            foreach(var c in Model.Chickas)
            {
                var g=ToyFactory.Chicka(actors,c.Remaining>0?ToyFactory.Palette[c.Id%3]:ToyFactory.Hex("B5B2A8"),c.Id);
                var same=Model.Chickas.Where(x=>x.Position.Equals(c.Position)).OrderBy(x=>x.Id).ToList();
                float offset=same.Count>1?(same.FindIndex(x=>x.Id==c.Id)-(same.Count-1)*.5f)*.75f:0;
                g.transform.position=ToyFactory.World(c.Position)+new Vector3(offset,.08f,0);dolls[c.Id]=g;
                WorldLabel(Names[c.Id]+"  "+c.Remaining+"겹",g.transform.position+new Vector3(0,2.05f,0),c.Remaining>0?ToyFactory.Hex("293E47"):ToyFactory.Hex("777E7A"));
                var direction=ToyFactory.World(Point.Rotate(new Point(1,0),c.Direction));
                ToyFactory.Line("Direction",labels,g.transform.position+Vector3.up*.04f,g.transform.position+direction*.9f+Vector3.up*.04f,.065f,ToyFactory.Palette[c.Id%3]);
            }
            Preview();
        }
        private void WorldLabel(string value,Vector3 position,Color color)
        {
            var g=ToyFactory.Group("Label",labels);g.transform.position=position;g.transform.rotation=View.transform.rotation;
            var t=g.AddComponent<TextMesh>();t.text=value;t.font=font;t.fontSize=48;t.characterSize=.058f;t.anchor=TextAnchor.MiddleCenter;t.color=color;
            var renderer=g.GetComponent<MeshRenderer>();renderer.sharedMaterial=font.material;
        }
        public Plan CurrentPreview=>Model.Preview(Selected,aimed);
        private void Preview()
        {
            Clear(ghost);var c=Model.Chickas.Find(x=>x.Id==Selected);var p=CurrentPreview;
            ToyFactory.Ring(ghost,ToyFactory.World(c.Position,.10f),.72f,ToyFactory.Hex("E4AA4D"),.07f);
            if(p.Error==null)foreach(var move in p.Moves)
            {
                ToyFactory.Road(move.Road,ghost,ToyFactory.Palette[move.Id%3],true);
                ToyFactory.Ring(ghost,ToyFactory.World(move.To,.11f),.40f,ToyFactory.Palette[move.Id%3]);
            }
            RefreshUI();
        }
        private void RefreshUI()
        {
            var c=Model.Chickas.Find(x=>x.Id==Selected);var plan=CurrentPreview;
            texts["Score"].text=Model.Score.ToString("D4")+"  POINTS";
            texts["Resources"].text=$"남은 겹 {Model.Remaining}   /   직접 펼침 {Model.Actions}회";
            texts["Selection"].text=Names[c.Id]+"   ·   "+c.Remaining+"겹 남음";
            texts["Layers"].text=c.Remaining==0?"가장 안쪽의 Chicka입니다. 더는 펼칠 수 없어요.":"바깥  "+string.Join("   →   ",c.Layers.Skip(c.Used).Select(LayerName))+"  안쪽";
            texts["Forecast"].text=plan.Error=="NO_LAYERS"?"다른 Chicka를 선택하거나 되돌려 보세요.":plan.Error=="OUTSIDE"?"길이 보드 밖으로 나갑니다. 방향을 바꿔 주세요.":$"예상  {plan.Moves.Count}개 펼침  ·  +{plan.Score}점  ·  {plan.Moves.Count}겹 사용"+(plan.Blocked.Count>0?$"  /  {plan.Blocked.Count}개는 경계에서 멈춤":"");
            if(Busy){texts["Forecast"].text="운행 중 · Chicka가 하차하면 다음 펼침을 선택할 수 있어요.";texts["Layers"].text="길이 펼쳐지고, 안쪽의 Chicka가 열차로 이동합니다.";}
            texts["RelayCaption"].text=Model.Relay?"연쇄 ON":"연쇄 OFF";
            texts["MotionCaption"].text=motion?"동작 기본":"동작 줄임";texts["SoundCaption"].text=muted?"소리 OFF":"소리 ON";
            foreach(var b in buttons)b.interactable=!Busy;
            buttons.Find(b=>b.name=="Unfold").interactable=!Busy&&plan.Error==null;
            buttons.Find(b=>b.name=="Undo").interactable=!Busy&&undo.Count>0;
        }
        public bool Commit()
        {
            if(Busy||help.activeSelf)return false;var plan=CurrentPreview;if(plan.Error!=null){RefreshUI();return false;}
            undo.Push(Model.Copy());if(!Model.Apply(plan)){undo.Pop();return false;}dragging=false;StartCoroutine(Perform(plan));return true;
        }
        private IEnumerator Perform(Plan plan)
        {
            Busy=true;Clear(ghost);Clear(labels);RefreshUI();int done=0;
            texts["Combo"].text=plan.Moves.Count>1?$"{plan.Moves.Count} CHICKA  /  +{plan.Score}":"";
            for(int i=0;i<plan.Moves.Count;i++)
            {
                StartCoroutine(AnimateMove(plan.Moves[i],i,()=>done++));
                yield return new WaitForSecondsRealtime((motion?.20f:.05f)/AnimationSpeed);
            }
            while(done<plan.Moves.Count)yield return null;
            // A render frame lets completed train objects be destroyed before rebuilding the stationary toys.
            yield return null;Busy=false;Rebuild();texts["Combo"].text="";
            Message(Model.Remaining==0?$"운행 끝 · {Model.Score}점. 다른 겹 순서와 연쇄 OFF를 비교해 보세요.":$"{plan.Moves.Count}개 펼침 완료. 길은 남고, {Model.Remaining}겹으로 다음 연결을 만들 수 있어요.");
        }
        private IEnumerator AnimateMove(Move move,int index,Action complete)
        {
            var toy=dolls[move.Id];var top=toy.transform.Find("UpperShell");var original=toy.transform.position;
            var direction=(ToyFactory.World(move.Road.Main[1])-ToyFactory.World(move.From)).normalized;
            var rail=ToyFactory.Road(move.Road,roads,ToyFactory.Palette[move.Id%3]);rail.SetActive(false);
            TickSound(index);
            float openTime=(motion?.32f:.06f)/AnimationSpeed;
            for(float elapsed=0;elapsed<openTime;elapsed+=Time.unscaledDeltaTime)
            {
                float t=Mathf.SmoothStep(0,1,elapsed/openTime);top.localPosition=Vector3.up*t*.60f+direction*t*.35f;top.localRotation=Quaternion.AngleAxis(t*40,Vector3.Cross(Vector3.up,direction));
                toy.transform.localScale=Vector3.one*(1-.22f*t);yield return null;
            }
            rail.SetActive(true);float spreadTime=(motion?.22f:.04f)/AnimationSpeed;
            for(float elapsed=0;elapsed<spreadTime;elapsed+=Time.unscaledDeltaTime)
            {
                float t=Mathf.SmoothStep(0,1,elapsed/spreadTime);
                rail.transform.position=ToyFactory.World(move.From)*(1-t);rail.transform.localScale=new Vector3(Mathf.Max(.01f,t),1,Mathf.Max(.01f,t));
                toy.transform.localScale=new Vector3(.78f,.78f*(1-t)+.025f,.78f);yield return null;
            }
            rail.transform.position=Vector3.zero;rail.transform.localScale=Vector3.one;toy.SetActive(false);
            var train=ToyFactory.Train(trains,ToyFactory.Palette[move.Id%3]);train.transform.position=ToyFactory.World(move.From,.1f);
            // Unfold a pair of colored shell strips along the new path before the carriage crosses it.
            var signal=ToyFactory.Shape("Signal",PrimitiveType.Sphere,scene,ToyFactory.World(move.From,.2f),Vector3.one*.17f,ToyFactory.Hex("FFCF71"));
            float duration=(motion?1.15f:.24f)/AnimationSpeed;
            for(float elapsed=0;elapsed<duration;elapsed+=Time.unscaledDeltaTime)
            {
                float t=Mathf.Clamp01(elapsed/duration);var point=Along(move.Road,t,out var tangent);
                train.transform.position=point;train.transform.rotation=Quaternion.LookRotation(tangent);signal.transform.position=Along(move.Road,Mathf.Clamp01(t+.16f),out _)+Vector3.up*.12f;yield return null;
            }
            train.transform.position=Along(move.Road,1,out _);Destroy(signal);
            var child=ToyFactory.Chicka(actors,ToyFactory.Palette[move.Id%3],move.Id,false);child.name="Disembarking Chicka";
            var destination=ToyFactory.World(move.To,.08f);float arrival=(motion?.38f:.08f)/AnimationSpeed;
            train.transform.Find("Passenger").gameObject.SetActive(false);
            for(float elapsed=0;elapsed<arrival;elapsed+=Time.unscaledDeltaTime)
            {
                float t=Mathf.Clamp01(elapsed/arrival);child.transform.position=destination+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.32f+.5f*(1-t));child.transform.localScale=Vector3.one*Mathf.Lerp(.21f,1,t);
                train.transform.localScale=Vector3.one*(1-Mathf.SmoothStep(0,1,t));yield return null;
            }
            child.transform.position=destination;child.transform.localScale=Vector3.one;Destroy(train);complete();
        }
        private static Vector3 Along(Road road,float t,out Vector3 direction)
        {
            float length=0;for(int i=1;i<road.Main.Length;i++)length+=Vector3.Distance(ToyFactory.World(road.Main[i-1]),ToyFactory.World(road.Main[i]));
            float d=t*length;
            for(int i=1;i<road.Main.Length;i++)
            {
                var a=ToyFactory.World(road.Main[i-1],.1f);var b=ToyFactory.World(road.Main[i],.1f);float segment=Vector3.Distance(a,b);
                if(d<=segment||i==road.Main.Length-1){direction=(b-a).normalized;var p=Vector3.Lerp(a,b,d/segment);if(road.Kind==Shell.Bridge)p.y+=Mathf.Sin(t*Mathf.PI)*.42f;return p;}d-=segment;
            }
            direction=Vector3.right;return ToyFactory.World(road.End,.1f);
        }
        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.Escape)){if(help.activeSelf)ToggleHelp();else{PeelFeedback(0);dragging=false;Preview();}return;}
            if(Busy)return;
            if(Input.GetKeyDown(KeyCode.H)){ToggleHelp();return;}if(help.activeSelf)return;
            if(Input.GetKeyDown(KeyCode.Alpha1))LoadScenario(0);if(Input.GetKeyDown(KeyCode.Alpha2))LoadScenario(1);if(Input.GetKeyDown(KeyCode.Alpha3))LoadScenario(2);
            if(Input.GetKeyDown(KeyCode.Z))Undo();if(Input.GetKeyDown(KeyCode.R))LoadScenario(ScenarioIndex);
            if(Input.GetKeyDown(KeyCode.E)||Input.GetKeyDown(KeyCode.RightArrow))Rotate(-1);
            if(Input.GetKeyDown(KeyCode.Q)||Input.GetKeyDown(KeyCode.LeftArrow))Rotate(1);
            if(Input.GetKeyDown(KeyCode.Space))Commit();
            if(Input.GetMouseButtonDown(0)&&!(EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject()))PointerDown(Input.mousePosition);
            if(Input.GetMouseButton(0))PointerMove(Input.mousePosition);
            if(Input.GetMouseButtonUp(0))PointerUp(Input.mousePosition);
        }
        public bool PointerDown(Vector3 screen)
        {
            if(Busy||help.activeSelf)return false;
            if(Physics.Raycast(View.ScreenPointToRay(screen),out var hit,100))
            {var actor=hit.collider.GetComponent<ChickaHit>();if(actor!=null&&Select(actor.Id)){dragging=true;dragStart=screen;return true;}}
            return false;
        }
        public void PointerMove(Vector3 screen)
        {
            if(dragging)PeelFeedback(Mathf.Clamp01(Vector3.Distance(screen,dragStart)/90));
            if(dragging&&Vector3.Distance(screen,dragStart)>16)
            {
                var plane=new Plane(Vector3.up,Vector3.zero);var ray=View.ScreenPointToRay(screen);
                if(plane.Raycast(ray,out float distance))
                {
                    var c=Model.Chickas.Find(x=>x.Id==Selected);var delta=ray.GetPoint(distance)-ToyFactory.World(c.Position);
                    int d=Mathf.Abs(delta.x)>Mathf.Abs(delta.z)?(delta.x>=0?0:2):(delta.z>=0?1:3);
                    if(d!=aimed){aimed=d;Model.Aim(Selected,d);Preview();}
                }
            }
        }
        public void PointerUp(Vector3 screen)
        {
            if(dragging){PeelFeedback(0);dragging=false;if(Vector3.Distance(screen,dragStart)>16)Commit();}
        }
        private void PeelFeedback(float amount)
        {
            if(Busy||!dolls.TryGetValue(Selected,out var toy)||toy==null)return;
            var c=Model.Chickas.Find(x=>x.Id==Selected);if(c.Remaining==0)return;
            var top=toy.transform.Find("UpperShell");top.localPosition=Vector3.up*(amount*.38f);top.localRotation=Quaternion.Euler(0,0,amount*-8);
        }
        public void Capture(string path,int width=1440,int height=900)
        {
            var scaler=canvas.GetComponent<CanvasScaler>();bool scaling=scaler.enabled;float oldScale=canvas.scaleFactor;
            var rt=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.antiAliasing=4;var previous=View.targetTexture;var active=RenderTexture.active;Texture2D texture=null;
            try
            {
                // Generate font geometry for the capture resolution, even when batch mode reports 640x480.
                scaler.enabled=false;View.targetTexture=rt;canvas.scaleFactor=Mathf.Min(width/1440f,height/900f);
                foreach(var text in texts.Values){text.cachedTextGenerator.Invalidate();text.SetAllDirty();}
                Canvas.ForceUpdateCanvases();
                View.Render();RenderTexture.active=rt;texture=new Texture2D(width,height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();
                System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());
            }
            finally
            {
                View.targetTexture=previous;RenderTexture.active=active;View.ResetAspect();canvas.scaleFactor=oldScale;scaler.enabled=scaling;canvas.enabled=false;canvas.enabled=true;Canvas.ForceUpdateCanvases();rt.Release();Destroy(rt);if(texture!=null)Destroy(texture);
            }
        }
        private void OnDestroy(){if(pop!=null)Destroy(pop);}
    }
}
