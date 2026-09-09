#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MetroChicka
{
    public sealed class StudySmoke:MonoBehaviour
    {
        private string output;private Prototype game;private float started;private bool finished;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-metro-study-smoke");
            if(i<0||i+1>=args.Length)return;new GameObject("Study smoke").AddComponent<StudySmoke>().output=args[i+1];
        }
        private IEnumerator Start()
        {
            Directory.CreateDirectory(output);started=Time.realtimeSinceStartup;Application.logMessageReceived+=Log;
            yield return null;yield return null;game=FindFirstObjectByType<Prototype>();Require(game!=null,"Prototype loads");
            yield return Capture("01-first-unfold");
            var start=game.View.WorldToScreenPoint(new Vector3(-5,.85f,-2));
            Require(game.PointerDown(start),"Physics pointer selects Chicka");
            var end=game.View.WorldToScreenPoint(new Vector3(-1,0,-2));game.PointerMove(end);game.PointerUp(end);
            Require(game.Busy,"Dragging commits a peel");
            yield return new WaitForSecondsRealtime(.78f);Require(game.ActiveTrains>0,"Smaller train appears during movement");yield return Capture("02-train-moving");
            yield return Idle();Require(game.Model.Remaining==2,"One layer consumed");Require(game.ActiveTrains==0,"Train removed after disembark");
            yield return Capture("03-arrived");
            yield return Click("Scenario1");yield return Capture("04-chain-preview");
            Require(game.CurrentPreview.Moves.Count==3,"Three dolls shown in forecast");
            yield return Click("Unfold");yield return new WaitForSecondsRealtime(.75f);yield return Capture("05-chain-moving");yield return Idle();
            Require(game.Model.Score==60&&game.Model.Remaining==6,"Three-layer chain outcome");yield return Capture("06-chain-arrived");
            yield return Click("Undo");Require(game.Model.Remaining==9&&game.Model.Score==0&&game.Model.Roads.Count==0,"Undo whole chain");
            yield return Click("Relay");Require(game.CurrentPreview.Moves.Count==1,"Relay OFF preview");yield return Click("Unfold");yield return Idle();
            Require(game.Model.Remaining==8&&game.Model.Score==10,"A/B single result");yield return Capture("07-relay-off");
            yield return Click("Scenario2");yield return Capture("08-shell-order");
            yield return Click("Help");yield return Capture("09-help");yield return Click("CloseHelp");
            yield return Click("Motion");yield return Click("Unfold");yield return Idle();Require(game.ActiveTrains==0,"Reduced motion completes");
            yield return Click("Restart");yield return Click("Scenario0");yield return Click("Rotate");
            Require(game.CurrentPreview.Error==null,"Rotation works");yield return Click("Rotate");Require(game.CurrentPreview.Error=="OUTSIDE","Out-of-bounds preview rejected");
            Require(!game.Commit(),"Invalid peel does not spend layers");Require(game.Model.Remaining==3,"Rejected move preserves layers");
            yield return Click("Restart");game.Capture(Path.Combine(output,"10-widescreen.png"),1600,900);
            finished=true;
            File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: native Unity player; 3D physics drag; UI raycasts; visible train; disembark and train removal; 3-way relay; A/B OFF; whole-action undo; four shell types; reduced motion; boundaries; two capture aspects.\nAutomated execution is not a human fun or purchase-intent test.\n");
            Debug.Log("STUDY_SMOKE_PASS");Application.Quit(0);
        }
        private IEnumerator Idle()
        {
            float deadline=Time.realtimeSinceStartup+10;while(game.Busy&&Time.realtimeSinceStartup<deadline)yield return null;
            Require(!game.Busy,"Animation must terminate");yield return null;yield return null;
        }
        private IEnumerator Capture(string name)
        {yield return null;Canvas.ForceUpdateCanvases();game.Capture(Path.Combine(output,name+".png"));yield return null;}
        private IEnumerator Click(string name)
        {
            yield return null;Canvas.ForceUpdateCanvases();
            var button=FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b=>b.name==name&&b.gameObject.activeInHierarchy);
            var rect=(RectTransform)button.transform;var position=RectTransformUtility.WorldToScreenPoint(game.View,rect.TransformPoint(rect.rect.center));
            var pointer=new PointerEventData(EventSystem.current){position=position};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            Require(hits.Count>0&&hits[0].gameObject==button.gameObject,"UI hit "+name+" at "+position+" screen "+Screen.width+"x"+Screen.height+" camera "+game.View.pixelRect+" hits "+string.Join(",",hits.Select(h=>h.gameObject.name)));
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);yield return null;
        }
        private void Update(){if(!finished&&started>0&&Time.realtimeSinceStartup-started>90)Fail("Smoke timeout");}
        private void Require(bool condition,string label){if(!condition){Fail(label);throw new InvalidOperationException(label);}}
        private void Fail(string reason){if(finished)return;finished=true;File.WriteAllText(Path.Combine(output,"result.txt"),"FAIL: "+reason);Application.Quit(1);}
        private void Log(string message,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error)Fail(message+"\n"+trace);}
        private void OnDestroy(){Application.logMessageReceived-=Log;}
    }
}
#endif
