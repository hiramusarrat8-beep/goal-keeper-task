using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

namespace GoalkeeperUXF
{
    public static class GoalkeeperSceneBuilder
    {
        static readonly Color BG          = Hex("#0a1510");
        static readonly Color TURF_A      = Hex("#1a7a47");
        static readonly Color TURF_B      = Hex("#178043");
        static readonly Color POST_COL    = new Color(0.95f, 0.97f, 0.95f);
        static readonly Color HALF_BASE   = new Color(1f, 1f, 1f, 0.05f);
        static readonly Color KEEPER_BODY = Hex("#f2c14e");
        static readonly Color KEEPER_HEAD = Hex("#f7d889");
        static readonly Color BALL_WHITE  = new Color(0.93f, 0.94f, 0.92f);
        static readonly Color BALL_BLACK  = new Color(0.08f, 0.08f, 0.08f);
        static readonly Color HIDDEN      = Hex("#2a3530");
        static readonly Color INK         = Hex("#e9f1ec");
        static readonly Color MUTED       = Hex("#9bb1a5");
        static readonly Color AMBER       = Hex("#f4b740");
        static readonly Color DARK_PANEL  = Hex("#0e1813");
        static readonly Color BTN_DARK    = Hex("#111f18");
        static readonly Color BORDER      = new Color(0.13f, 0.28f, 0.20f);

        [MenuItem("Goalkeeper/Build Scene")]
        public static void BuildScene()
        {
            if (!EditorUtility.DisplayDialog("Build Goalkeeper Scene",
                "Rebuilds the entire Goalkeeper scene from scratch.\nContinue?",
                "Build it", "Cancel")) return;

            // Auto-delete old objects
            foreach (var n in new[]{"Pitch","Anchors","HUD_Canvas","GameManager"})
            { var o = GameObject.Find(n); if (o) Object.DestroyImmediate(o); }

            // ── Camera ────────────────────────────────────────────────────
            var cam = Camera.main ?? new GameObject("Main Camera").AddComponent<Camera>();
            cam.gameObject.tag   = "MainCamera";
            cam.orthographic     = true;
            cam.orthographicSize = 5f;
            cam.transform.position = new Vector3(0, 1, -10);
            cam.transform.rotation = Quaternion.identity;
            cam.backgroundColor  = BG;
            cam.clearFlags       = CameraClearFlags.SolidColor;
            if (!cam.GetComponent<AudioListener>()) cam.gameObject.AddComponent<AudioListener>();

            // ── Pitch ─────────────────────────────────────────────────────
            var pitch = new GameObject("Pitch").transform;
            for (int i = 0; i < 6; i++)
                MakeQuad("Turf_"+i, pitch, new Vector3(0,-5f+i*2.2f,0.2f),
                    new Vector3(16,2.2f,1), i%2==0 ? TURF_A : TURF_B);

            // ── Goal ──────────────────────────────────────────────────────
            var goal = new GameObject("Goal").transform;
            goal.SetParent(pitch);
            MakeQuad("Net",goal,new Vector3(0,3.8f,0.06f),new Vector3(7.2f,3.4f,1),new Color(1,1,1,0.07f));
            var halfL = MakeQuad("GoalHalfLeft",  goal, new Vector3(-1.85f,3.8f,0.05f), new Vector3(3.5f,3.4f,1), HALF_BASE);
            var halfR = MakeQuad("GoalHalfRight", goal, new Vector3( 1.85f,3.8f,0.05f), new Vector3(3.5f,3.4f,1), HALF_BASE);
            MakeQuad("Crossbar", goal, new Vector3(0,5.6f,-0.05f),    new Vector3(7.5f,0.18f,1), POST_COL);
            MakeQuad("PostLeft",  goal, new Vector3(-3.7f,3.7f,-0.05f), new Vector3(0.18f,3.8f,1), POST_COL);
            MakeQuad("PostRight", goal, new Vector3( 3.7f,3.7f,-0.05f), new Vector3(0.18f,3.8f,1), POST_COL);
            MakeQuad("Spot", pitch, new Vector3(0,-0.8f,0), new Vector3(0.16f,0.08f,1), new Color(1,1,1,0.45f));

            // ── Keeper ────────────────────────────────────────────────────
            var keeperRoot = new GameObject("Keeper").transform;
            keeperRoot.SetParent(pitch);
            keeperRoot.localPosition = new Vector3(0, 2.8f, -0.1f);
            MakeQuad("KeeperBody", keeperRoot, new Vector3(0,-0.05f,0), new Vector3(0.52f,0.78f,1), KEEPER_BODY);
            var kHead = new GameObject("KeeperHead").transform;
            kHead.SetParent(keeperRoot);
            kHead.localPosition = new Vector3(0,0.55f,0);
            kHead.localScale    = new Vector3(0.36f,0.36f,1);
            kHead.gameObject.AddComponent<MeshFilter>().mesh = CircleMesh(20);
            kHead.gameObject.AddComponent<MeshRenderer>().sharedMaterial = Mat(KEEPER_HEAD);

            // ── Shooter — player figure ───────────────────────────────────
            // Scaled up (~1.9x, ~1.6x taller than the keeper) so the shooter reads as
            // the clear focal point during the character reveal, not a small dot.
            // Kept low enough that its feet still clear the button bar at the bottom
            // of the screen (verified against the camera's world-space view bounds).
            const float shooterScale = 1.9f;
            var shooter = new GameObject("Shooter").transform;
            shooter.SetParent(pitch);
            shooter.localPosition = new Vector3(0, -2.65f, -0.1f);

            // Head circle
            var sHead = new GameObject("ShooterHead").transform;
            sHead.SetParent(shooter);
            sHead.localPosition = new Vector3(0, 0.70f * shooterScale, 0);
            sHead.localScale    = new Vector3(0.28f * shooterScale, 0.28f * shooterScale, 1);
            sHead.gameObject.AddComponent<MeshFilter>().mesh = CircleMesh(24);
            var shMR = sHead.gameObject.AddComponent<MeshRenderer>();
            shMR.sharedMaterial = Mat(HIDDEN);

            // Jersey body
            var jerseyT  = MakeQuad("ShooterJersey", shooter, new Vector3(0, 0.12f * shooterScale, 0),
                new Vector3(0.44f * shooterScale, 0.50f * shooterScale, 1), HIDDEN);
            var jerseyMR = jerseyT.GetComponent<MeshRenderer>();

            // Letter on jersey — characterSize scales with the jersey so it still fits neatly
            var labelGO = new GameObject("ShooterLabel");
            labelGO.transform.SetParent(shooter);
            labelGO.transform.localPosition = new Vector3(0, 0.12f * shooterScale, -0.15f);
            labelGO.transform.localScale    = Vector3.one; // scale via characterSize instead
            var tm = labelGO.AddComponent<TextMesh>();
            tm.text          = "?";
            tm.fontSize      = 96;
            tm.characterSize = 0.10f * shooterScale;
            tm.alignment     = TextAlignment.Center;
            tm.anchor        = TextAnchor.MiddleCenter;
            tm.color         = Color.white;
            tm.fontStyle     = FontStyle.Bold;

            // ── Ball — football with hexagon patches ──────────────────────
            // Ball sits just above the shooter's (now bigger) head, small gap.
            const float ballRadius = 0.25f; // was 0.16 — drives the base circle and hex layout below
            var ballRoot = new GameObject("Ball").transform;
            ballRoot.SetParent(pitch);
            ballRoot.localPosition = new Vector3(0, -0.65f, -0.12f);
            ballRoot.localScale    = Vector3.one;

            // White circle base
            var ballBase = new GameObject("BallBase").transform;
            ballBase.SetParent(ballRoot);
            ballBase.localPosition = Vector3.zero;
            ballBase.localScale    = new Vector3(ballRadius * 2f, ballRadius * 2f, 1);
            ballBase.gameObject.AddComponent<MeshFilter>().mesh = CircleMesh(32);
            ballBase.gameObject.AddComponent<MeshRenderer>().sharedMaterial = Mat(BALL_WHITE);

            // Black hexagon patches to look like a football (sized proportionally to ballRadius
            // so the pattern keeps the same look as the ball grows)
            // Centre patch
            MakeHexPatch("Patch_C", ballRoot, Vector3.zero, ballRadius * 0.45f, BALL_BLACK);
            // 6 surrounding patches
            float r = ballRadius * 0.594f;
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, -0.01f);
                MakeHexPatch("Patch_"+i, ballRoot, pos, ballRadius * 0.34f, BALL_BLACK);
            }

            // ── Anchors ───────────────────────────────────────────────────
            var anchors = new GameObject("Anchors").transform;
            var keeperHome  = Anchor("KeeperHome",       anchors, new Vector3( 0,    2.8f,  -0.1f));
            var diveL       = Anchor("KeeperDiveLeft",   anchors, new Vector3(-2.9f, 2.1f,  -0.1f));
            var diveR       = Anchor("KeeperDiveRight",  anchors, new Vector3( 2.9f, 2.1f,  -0.1f));
            var ballStart   = Anchor("BallStart",        anchors, new Vector3( 0,   -0.65f, -0.12f));
            var ballCornerL = Anchor("BallCornerLeft",   anchors, new Vector3(-3.3f, 5.0f,  -0.12f));
            var ballCornerR = Anchor("BallCornerRight",  anchors, new Vector3( 3.3f, 5.0f,  -0.12f));
            // Ball stops here when keeper makes a save — in front of keeper dive position
            var ballSaveL   = Anchor("BallSaveLeft",      anchors, new Vector3(-2.4f, 2.4f,  -0.1f));
            var ballSaveR   = Anchor("BallSaveRight",     anchors, new Vector3( 2.4f, 2.4f,  -0.1f));
            diveL.localRotation = Quaternion.Euler(0,0, 60f);
            diveR.localRotation = Quaternion.Euler(0,0,-60f);

            // ── Canvas ────────────────────────────────────────────────────
            var canvasGO = new GameObject("HUD_Canvas");
            var cv = canvasGO.AddComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 10;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920,1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            // Score card — top left
            var scoreCard = MakePanel("ScoreCard", canvasGO.transform,
                new Vector2(0,1), new Vector2(0,1), new Vector2(20,-90), new Vector2(180,-20));
            AddOutline(scoreCard, BORDER);
            var scoreLbl = MakeText("ScoreLabel", scoreCard.transform,
                new Vector2(0.5f,0.75f), new Vector2(150,22), 13, MUTED, FontStyle.Normal);
            scoreLbl.text = "POINTS"; scoreLbl.alignment = TextAnchor.MiddleCenter;
            var scoreText = MakeText("ScoreText", scoreCard.transform,
                new Vector2(0.5f,0.32f), new Vector2(150,44), 40, INK, FontStyle.Bold);
            scoreText.text = "0"; scoreText.alignment = TextAnchor.MiddleCenter;

            // Prompt — large bold centred
            var promptGO = new GameObject("PromptText");
            promptGO.transform.SetParent(canvasGO.transform, false);
            var pRT = promptGO.AddComponent<RectTransform>();
            pRT.anchorMin = new Vector2(0.1f, 0.33f);
            pRT.anchorMax = new Vector2(0.9f, 0.43f);
            pRT.offsetMin = pRT.offsetMax = Vector2.zero;
            var promptText = promptGO.AddComponent<Text>();
            promptText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            promptText.fontSize  = 30;
            promptText.color     = INK;
            promptText.alignment = TextAnchor.MiddleCenter;
            promptText.fontStyle = FontStyle.Bold;

            // Outcome badge — dark pill centre screen
            var badgeGO = new GameObject("OutcomeBadge");
            badgeGO.transform.SetParent(canvasGO.transform, false);
            var badgeRT = badgeGO.AddComponent<RectTransform>();
            badgeRT.anchorMin = new Vector2(0.5f,0.42f);
            badgeRT.anchorMax = new Vector2(0.5f,0.42f);
            badgeRT.pivot     = new Vector2(0.5f,0.5f);
            badgeRT.sizeDelta = new Vector2(340, 56);
            badgeRT.anchoredPosition = Vector2.zero;
            var badgeImg = badgeGO.AddComponent<Image>();
            badgeImg.color = new Color(0.04f,0.06f,0.05f,0.92f);
            AddOutline(badgeGO, BORDER);

            var outcomeText = MakeText("OutcomeText", badgeGO.transform,
                new Vector2(0.5f,0.5f), new Vector2(320,48), 24, INK, FontStyle.Bold);
            outcomeText.alignment = TextAnchor.MiddleCenter;
            badgeGO.SetActive(false);

            // Get-ready panel — shown during the ITI, ~30% of the screen, centred.
            var readyGO = new GameObject("GetReadyPanel");
            readyGO.transform.SetParent(canvasGO.transform, false);
            var readyRT = readyGO.AddComponent<RectTransform>();
            readyRT.anchorMin = new Vector2(0.35f, 0.35f);
            readyRT.anchorMax = new Vector2(0.65f, 0.65f);
            readyRT.offsetMin = readyRT.offsetMax = Vector2.zero;
            var readyImg = readyGO.AddComponent<Image>();
            readyImg.color = new Color(0.04f, 0.06f, 0.05f, 0.92f);
            AddOutline(readyGO, BORDER);

            var readyText = MakeText("GetReadyText", readyGO.transform,
                new Vector2(0.5f,0.5f), new Vector2(0,0), 26, INK, FontStyle.Bold);
            var readyTextRT = readyText.GetComponent<RectTransform>();
            readyTextRT.anchorMin = Vector2.zero;
            readyTextRT.anchorMax = Vector2.one;
            readyTextRT.offsetMin = readyTextRT.offsetMax = Vector2.zero;
            readyText.text      = "Next round...";
            readyText.alignment = TextAnchor.MiddleCenter;
            readyGO.SetActive(false);

            // Practice badge — small amber tag, top right, only shown during the
            // 10-trial untimed practice block so participants never mistake it for
            // a real, scored trial.
            var practiceBadgeGO = MakePanel("PracticeBadge", canvasGO.transform,
                new Vector2(1,1), new Vector2(1,1), new Vector2(-200,-90), new Vector2(-20,-20));
            AddOutline(practiceBadgeGO, BORDER);
            var practiceBadgeImg = practiceBadgeGO.GetComponent<Image>();
            practiceBadgeImg.color = AMBER;
            var practiceBadgeText = MakeText("PracticeBadgeText", practiceBadgeGO.transform,
                new Vector2(0.5f,0.5f), new Vector2(160,40), 20, Hex("#0a1510"), FontStyle.Bold);
            practiceBadgeText.text = "PRACTICE";
            practiceBadgeGO.SetActive(false);

            // Tutorial panel — large centred panel for the pre-task onboarding flow.
            var tutorialGO = new GameObject("TutorialPanel");
            tutorialGO.transform.SetParent(canvasGO.transform, false);
            var tutRT = tutorialGO.AddComponent<RectTransform>();
            tutRT.anchorMin = new Vector2(0.2f, 0.3f);
            tutRT.anchorMax = new Vector2(0.8f, 0.7f);
            tutRT.offsetMin = tutRT.offsetMax = Vector2.zero;
            var tutImg = tutorialGO.AddComponent<Image>();
            tutImg.color = new Color(0.04f, 0.06f, 0.05f, 0.95f);
            AddOutline(tutorialGO, BORDER);
            var tutorialText = MakeText("TutorialText", tutorialGO.transform,
                new Vector2(0.5f,0.5f), new Vector2(0,0), 24, INK, FontStyle.Normal);
            var tutTextRT = tutorialText.GetComponent<RectTransform>();
            tutTextRT.anchorMin = new Vector2(0.08f, 0.1f);
            tutTextRT.anchorMax = new Vector2(0.92f, 0.9f);
            tutTextRT.offsetMin = tutTextRT.offsetMax = Vector2.zero;
            tutorialText.alignment = TextAnchor.MiddleCenter;
            tutorialGO.SetActive(false);

            // Timer bar — amber strip above buttons, always visible.
            // Tall (28px) and higher-contrast track so depletion is actually legible.
            var timerBG = new GameObject("TimerBG");
            timerBG.transform.SetParent(canvasGO.transform, false);
            var tbRT = timerBG.AddComponent<RectTransform>();
            tbRT.anchorMin = new Vector2(0.1f,0); tbRT.anchorMax = new Vector2(0.9f,0);
            tbRT.offsetMin = new Vector2(0,92);   tbRT.offsetMax = new Vector2(0,120);
            var tbImg = timerBG.AddComponent<Image>();
            tbImg.color = new Color(1,1,1,0.18f); // brighter background track
            AddOutline(timerBG, BORDER);

            // Depletion is driven by shrinking this rect's anchorMax.x (left edge pinned
            // at anchorMin.x=0, right edge moves in from anchorMax.x=1 down to 0) rather
            // than Image.fillAmount: Image.Type.Filled is a silent no-op with no Sprite
            // assigned (Unity falls back to drawing a plain full rect and ignores
            // fillAmount/fillMethod entirely), which is why the bar never moved before.
            var timerFillGO = new GameObject("TimerFill");
            timerFillGO.transform.SetParent(timerBG.transform, false); // child of BG so it fills it
            var tfRT = timerFillGO.AddComponent<RectTransform>();
            tfRT.anchorMin = Vector2.zero;
            tfRT.anchorMax = Vector2.one;
            tfRT.offsetMin = Vector2.zero;
            tfRT.offsetMax = Vector2.zero;
            var timerFill = timerFillGO.AddComponent<Image>();
            timerFill.color   = AMBER;
            timerFill.enabled = true;

            // ── Buttons — centred, not edge to edge ───────────────────────
            var btnLGO = MakePanel("BtnLeft", canvasGO.transform,
                new Vector2(0.5f,0), new Vector2(0.5f,0),
                new Vector2(-420,16), new Vector2(-20,80), new Vector2(1,0));
            AddOutline(btnLGO, BORDER);
            var blImg = btnLGO.GetComponent<Image>(); blImg.color = BTN_DARK;
            var btnL = btnLGO.AddComponent<Button>();
            btnL.targetGraphic = blImg;
            btnL.transition = Selectable.Transition.None;
            var blLbl = MakeText("Label", btnLGO.transform,
                new Vector2(0.5f,0.5f), new Vector2(380,56), 22, INK, FontStyle.Bold);
            blLbl.text = "← Left"; blLbl.alignment = TextAnchor.MiddleCenter;

            var btnRGO = MakePanel("BtnRight", canvasGO.transform,
                new Vector2(0.5f,0), new Vector2(0.5f,0),
                new Vector2(20,16), new Vector2(420,80), new Vector2(0,0));
            AddOutline(btnRGO, BORDER);
            var brImg = btnRGO.GetComponent<Image>(); brImg.color = BTN_DARK;
            var btnR = btnRGO.AddComponent<Button>();
            btnR.targetGraphic = brImg;
            btnR.transition = Selectable.Transition.None;
            var brLbl = MakeText("Label", btnRGO.transform,
                new Vector2(0.5f,0.5f), new Vector2(380,56), 22, INK, FontStyle.Bold);
            brLbl.text = "Right →"; brLbl.alignment = TextAnchor.MiddleCenter;

            // ── GameManager ───────────────────────────────────────────────
            var gm        = new GameObject("GameManager");
            var generator = gm.AddComponent<GoalkeeperSessionGenerator>();
            var runner    = gm.AddComponent<GoalkeeperTrialRunner>();
            var view      = gm.AddComponent<GoalkeeperView>();
            var tutorial  = gm.AddComponent<GoalkeeperTutorialRunner>();
            tutorial.sessionGenerator = generator;

            view.keeper          = keeperRoot;
            view.ball            = ballRoot;      // root of the football
            view.shooterRenderer = shMR;
            view.shooterJersey   = jerseyMR;
            view.shooterLabel    = tm;
            view.goalHalfLeft    = halfL.GetComponent<MeshRenderer>();
            view.goalHalfRight   = halfR.GetComponent<MeshRenderer>();
            view.keeperHome      = keeperHome;
            view.keeperDiveLeft  = diveL;
            view.keeperDiveRight = diveR;
            view.ballStart       = ballStart;
            view.ballCornerLeft  = ballCornerL;
            view.ballCornerRight = ballCornerR;
            view.ballSaveLeft    = ballSaveL;
            view.ballSaveRight   = ballSaveR;
            view.scoreText       = scoreText;
            view.promptText      = promptText;
            view.outcomeText     = outcomeText;
            view.outcomeBadge    = badgeImg;
            view.getReadyPanel   = readyImg;
            view.getReadyText    = readyText;
            view.timerBG         = timerBG;
            view.practiceBadge   = practiceBadgeImg;
            view.tutorialPanel   = tutImg;
            view.tutorialText    = tutorialText;
            view.timerFill       = timerFill;
            view.btnLeft         = btnL;
            view.btnRight        = btnR;
            view.btnLeftBG       = blImg;
            view.btnRightBG      = brImg;
            view.btnLeftLabel    = blLbl;
            view.btnRightLabel   = brLbl;

            // UXF wiring
            var session = Object.FindObjectOfType<UXF.Session>();
            if (session != null)
            {
                runner.session = session;
                WireEvent(session.onSessionBegin, gm,
                    typeof(GoalkeeperTutorialRunner), "BeginTutorial");
                WireEvent(session.onTrialBegin, gm,
                    typeof(GoalkeeperTrialRunner), "OnTrialBegin");
                Debug.Log("Goalkeeper: UXF events wired.");
            }
            else Debug.LogWarning("Goalkeeper: [UXF_Rig] not found.");

            var sc = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(sc);
            EditorSceneManager.SaveScene(sc);
            Debug.Log("Goalkeeper scene built and saved!");
            EditorUtility.DisplayDialog("Done!", "Scene built and saved.\nPress Play!", "Let's go!");
        }

        // ── Helpers ───────────────────────────────────────────────────────

        static Transform MakeQuad(string name, Transform parent, Vector3 pos, Vector3 scale, Color col)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<MeshCollider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale    = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = Mat(col);
            return go.transform;
        }

        // Hexagon patch for football
        static void MakeHexPatch(string name, Transform parent, Vector3 localPos, float size, Color col)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale    = new Vector3(size, size, 1);
            go.AddComponent<MeshFilter>().mesh = HexMesh();
            go.AddComponent<MeshRenderer>().sharedMaterial = Mat(col);
        }

        static Mesh HexMesh()
        {
            var mesh  = new Mesh { name = "Hex" };
            var verts = new Vector3[7];
            var tris  = new int[18];
            var uvs   = new Vector2[7];
            verts[0] = Vector3.zero; uvs[0] = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad + 30f * Mathf.Deg2Rad; // flat-top hex
                verts[i+1] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * 0.5f;
                uvs[i+1]   = new Vector2(Mathf.Cos(a)*0.5f+0.5f, Mathf.Sin(a)*0.5f+0.5f);
                int next = (i+1)%6+1;
                tris[i*3]=0; tris[i*3+1]=i+1; tris[i*3+2]=next;
            }
            mesh.vertices = verts; mesh.triangles = tris; mesh.uv = uvs;
            mesh.RecalculateNormals();
            return mesh;
        }

        static Transform Anchor(string name, Transform parent, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            return go.transform;
        }

        static Material Mat(Color col)
        {
            var m = new Material(Shader.Find("Sprites/Default")); m.color = col; return m;
        }

        static Mesh CircleMesh(int seg)
        {
            var mesh  = new Mesh { name = "Circle" };
            var verts = new Vector3[seg+1];
            var tris  = new int[seg*3];
            var uvs   = new Vector2[seg+1];
            verts[0] = Vector3.zero; uvs[0] = new Vector2(0.5f,0.5f);
            for (int i = 0; i < seg; i++)
            {
                float a = i/(float)seg*Mathf.PI*2f;
                verts[i+1] = new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*0.5f;
                uvs[i+1]   = new Vector2(Mathf.Cos(a)*0.5f+0.5f,Mathf.Sin(a)*0.5f+0.5f);
                int next = (i+1)%seg+1;
                tris[i*3]=0; tris[i*3+1]=i+1; tris[i*3+2]=next;
            }
            mesh.vertices=verts; mesh.triangles=tris; mesh.uv=uvs;
            mesh.RecalculateNormals(); return mesh;
        }

        static GameObject MakePanel(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            Vector2? pivot = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>(); img.color = DARK_PANEL;
            var rt  = go.GetComponent<RectTransform>();
            // Pivot must be set BEFORE offsetMin/offsetMax: Unity bakes the pivot at
            // the time offsets are applied into anchoredPosition/sizeDelta, so changing
            // pivot afterwards silently shifts the rect instead of just re-anchoring it.
            if (pivot.HasValue) rt.pivot = pivot.Value;
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            return go;
        }

        static Text MakeText(string name, Transform parent,
            Vector2 anchor, Vector2 size, int fontSize, Color col, FontStyle style)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot     = new Vector2(0.5f,0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            var t = go.AddComponent<Text>();
            t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize  = fontSize; t.color = col; t.fontStyle = style;
            t.alignment = TextAnchor.MiddleCenter;
            return t;
        }

        static void AddOutline(GameObject go, Color col)
        {
            var o = go.AddComponent<Outline>();
            o.effectColor = col; o.effectDistance = new Vector2(1,-1);
        }

        static void WireEvent<T>(UnityEngine.Events.UnityEvent<T> evt,
            GameObject target, System.Type componentType, string methodName)
        {
            var comp   = target.GetComponent(componentType); if (comp==null) return;
            var method = componentType.GetMethod(methodName, new[]{typeof(T)}); if (method==null) return;
            var action = (UnityEngine.Events.UnityAction<T>)System.Delegate.CreateDelegate(
                typeof(UnityEngine.Events.UnityAction<T>), comp, method);
            // [UXF_Rig] (and its Session component) isn't in BuildScene's auto-delete
            // list, so it survives every rebuild — but AddPersistentListener only ever
            // appends, never replaces. Without clearing first, each rebuild piles a new
            // listener on top of whatever was wired by the previous one, and which call
            // actually "wins" at runtime stops being predictable. Clear first so every
            // rebuild leaves exactly one, correct, persistent listener.
            for (int i = evt.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEditor.Events.UnityEventTools.RemovePersistentListener(evt, i);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(evt, action);
        }

        static Color Hex(string h){ ColorUtility.TryParseHtmlString(h,out Color c); return c; }
    }
}
#endif
