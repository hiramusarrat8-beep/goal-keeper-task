using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace GoalkeeperUXF
{
    public static class GoalkeeperWirer
    {
        [MenuItem("Goalkeeper/Wire References")]
        public static void WireReferences()
        {
            var gm = GameObject.Find("GameManager");
            if (gm == null) { Err("GameManager not found. Run Build Scene first."); return; }
            var view   = gm.GetComponent<GoalkeeperView>();
            var runner = gm.GetComponent<GoalkeeperTrialRunner>();
            if (view   == null) { Err("GoalkeeperView not on GameManager.");        return; }
            if (runner == null) { Err("GoalkeeperTrialRunner not on GameManager."); return; }

            // Scene objects
            view.keeper          = FindT("Keeper");
            view.ball            = FindT("Ball");
            view.shooterRenderer = FindMR("ShooterHead");
            view.shooterJersey   = FindMR("ShooterJersey");
            view.shooterLabel    = FindTM("ShooterLabel");
            view.goalHalfLeft    = FindMR("GoalHalfLeft");
            view.goalHalfRight   = FindMR("GoalHalfRight");

            // Anchors
            view.keeperHome      = FindT("KeeperHome");
            view.keeperDiveLeft  = FindT("KeeperDiveLeft");
            view.keeperDiveRight = FindT("KeeperDiveRight");
            view.ballStart       = FindT("BallStart");
            view.ballCornerLeft  = FindT("BallCornerLeft");
            view.ballCornerRight = FindT("BallCornerRight");
            view.ballSaveLeft    = FindT("BallSaveLeft");
            view.ballSaveRight   = FindT("BallSaveRight");

            // HUD
            view.scoreText    = FindUI<Text>("ScoreText");
            view.promptText   = FindUI<Text>("PromptText");
            view.outcomeText  = FindUI<Text>("OutcomeText");
            view.outcomeBadge = FindUI<Image>("OutcomeBadge");
            view.getReadyText  = FindUI<Text>("GetReadyText");
            view.getReadyPanel = FindUI<Image>("GetReadyPanel");
            view.timerBG       = FindT("TimerBG")?.gameObject;
            view.practiceBadge = FindUI<Image>("PracticeBadge");
            view.tutorialPanel = FindUI<Image>("TutorialPanel");
            view.tutorialText  = FindChildUI<Text>("TutorialPanel", "TutorialText");

            // TimerFill is now a child of TimerBG
            view.timerFill = FindChildUI<Image>("TimerBG", "TimerFill");

            // Buttons
            view.btnLeft      = FindUI<Button>("BtnLeft");
            view.btnRight     = FindUI<Button>("BtnRight");
            view.btnLeftBG    = FindUI<Image>("BtnLeft");
            view.btnRightBG   = FindUI<Image>("BtnRight");
            view.btnLeftLabel  = FindChildUI<Text>("BtnLeft",  "Label");
            view.btnRightLabel = FindChildUI<Text>("BtnRight", "Label");

            // Tutorial runner
            var tutorial = gm.GetComponent<GoalkeeperTutorialRunner>();
            if (tutorial != null) tutorial.sessionGenerator = gm.GetComponent<GoalkeeperSessionGenerator>();
            else Warn("GoalkeeperTutorialRunner not on GameManager.");

            // Session
            var session = Object.FindObjectOfType<UXF.Session>();
            if (session != null) runner.session = session;
            else Debug.LogWarning("Goalkeeper Wirer: Session not found.");

            EditorUtility.SetDirty(gm);
            var sc = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(sc);
            EditorSceneManager.SaveScene(sc);
            Debug.Log("Goalkeeper: all references wired and saved!");
            EditorUtility.DisplayDialog("Done!", "All wired and saved.\nPress Play!", "Let's go!");
        }

        static void Err(string m) { Debug.LogError("Wirer: "+m); EditorUtility.DisplayDialog("Error",m,"OK"); }
        static void Warn(string n) { Debug.LogWarning("Wirer: not found: "+n); }

        static Transform FindT(string n)
        { var g=GameObject.Find(n); if(!g) Warn(n); return g?.transform; }

        static MeshRenderer FindMR(string n)
        { var g=GameObject.Find(n); if(!g){Warn(n);return null;} var c=g.GetComponent<MeshRenderer>(); if(!c)Warn(n+" MR"); return c; }

        static TextMesh FindTM(string n)
        { var g=GameObject.Find(n); if(!g){Warn(n);return null;} var c=g.GetComponent<TextMesh>(); if(!c)Warn(n+" TM"); return c; }

        static T FindUI<T>(string n) where T:Component
        { foreach(var t in Resources.FindObjectsOfTypeAll<T>()) if(t.gameObject.name==n&&t.gameObject.scene.isLoaded) return t; Warn("UI:"+n); return null; }

        static T FindChildUI<T>(string parentName, string childName) where T:Component
        {
            var p = GameObject.Find(parentName); if(!p){Warn(parentName);return null;}
            var c = p.transform.Find(childName); if(!c){Warn(parentName+"/"+childName);return null;}
            var comp = c.GetComponent<T>(); if(!comp) Warn(parentName+"/"+childName+" "+typeof(T).Name);
            return comp;
        }
    }
}
