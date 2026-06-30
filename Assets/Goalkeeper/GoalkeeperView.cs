using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GoalkeeperUXF
{
    [RequireComponent(typeof(GoalkeeperTrialRunner))]
    public class GoalkeeperView : MonoBehaviour
    {
        [Header("Scene objects")]
        public Transform keeper;
        public Transform ball;
        public MeshRenderer shooterRenderer;
        public MeshRenderer shooterJersey;
        public TextMesh shooterLabel;
        public MeshRenderer goalHalfLeft;
        public MeshRenderer goalHalfRight;

        [Header("Anchors")]
        public Transform keeperHome;
        public Transform keeperDiveLeft;
        public Transform keeperDiveRight;
        public Transform ballStart;
        public Transform ballCornerLeft;
        public Transform ballCornerRight;
        // Ball stops here when keeper saves — in front of keeper dive position
        public Transform ballSaveLeft;
        public Transform ballSaveRight;

        [Header("HUD")]
        public Text  promptText;
        public Image timerFill;
        public Image outcomeBadge;
        public Text  outcomeText;
        public Text  scoreText;
        public Image getReadyPanel;
        public Text  getReadyText;
        public GameObject timerBG;
        public Image practiceBadge;
        public Image tutorialPanel;
        public Text  tutorialText;

        [Header("HUD buttons")]
        public Button btnLeft;
        public Button btnRight;
        public Image  btnLeftBG;
        public Image  btnRightBG;
        public Text   btnLeftLabel;
        public Text   btnRightLabel;

        [Header("Character colours (A B C D)")]
        public Color colA = new Color(0.11f, 0.62f, 0.46f);
        public Color colB = new Color(0.22f, 0.54f, 0.87f);
        public Color colC = new Color(0.50f, 0.47f, 0.87f);
        public Color colD = new Color(0.83f, 0.33f, 0.49f);

        [Header("State colours")]
        public Color hiddenColor     = new Color(0.20f, 0.25f, 0.23f);
        public Color litColor        = new Color(0.96f, 0.72f, 0.25f);
        public Color actualColor     = new Color(0.20f, 0.78f, 0.48f);
        public Color baseHalf        = new Color(1f, 1f, 1f, 0.05f);
        public Color btnNormal       = new Color(0.08f, 0.12f, 0.09f);
        public Color btnSelected     = new Color(0.96f, 0.72f, 0.25f);
        public Color btnTextNormal   = new Color(0.91f, 0.95f, 0.92f);
        public Color btnTextSelected = new Color(0.08f, 0.09f, 0.07f);
        public Color btnBorderNorm   = new Color(0.20f, 0.30f, 0.22f);
        public Color btnBorderSelect = new Color(0.96f, 0.72f, 0.25f);
        public Color btnDisabledBG   = new Color(0.12f, 0.12f, 0.12f);
        public Color btnDisabledText = new Color(0.42f, 0.42f, 0.42f);
        public Color outcomeBadgeColor = new Color(0.04f, 0.06f, 0.05f, 0.92f);
        public Color timerColor      = new Color(0.96f, 0.72f, 0.25f);

        [Header("Motion")]
        public float diveTime  = 0.45f;
        public float flyTime   = 0.50f;
        public float arcHeight = 1.8f;

        Color[]   charColors;
        Coroutine timerCo;
        GoalkeeperTrialRunner runner;
        string    markedSide; // "L", "R", or null — tracks which button MarkPick last highlighted

        void Awake()
        {
            charColors = new[] { colA, colB, colC, colD };
            runner = GetComponent<GoalkeeperTrialRunner>();
            if (btnLeft)  btnLeft.onClick.AddListener(()  => runner.Submit("L"));
            if (btnRight) btnRight.onClick.AddListener(() => runner.Submit("R"));
            ConfigureButton(btnLeft,  btnLeftBG,  btnLeftLabel);
            ConfigureButton(btnRight, btnRightBG, btnRightLabel);
            EnsureShooterLabelStyle();

            if (timerFill)
            {
                timerFill.enabled = true;
                timerFill.color   = timerColor;
                SetTimerFraction(0f);
            }
        }

        // Drains by shrinking the rect's right edge (anchorMax.x) toward its pinned
        // left edge (anchorMin.x=0) — NOT Image.fillAmount, which is a no-op without a
        // Sprite assigned (see GoalkeeperSceneBuilder.cs for why).
        void SetTimerFraction(float frac)
        {
            if (!timerFill) return;
            var rt = timerFill.rectTransform;
            rt.anchorMax = new Vector2(Mathf.Clamp01(frac), rt.anchorMax.y);
        }

        // ── HUD ───────────────────────────────────────────────────────────

        public void SetScore(int score)
        {
            if (scoreText) scoreText.text = score.ToString();
        }

        public void SetPrompt(string prompt)
        {
            if (!promptText) return;
            promptText.text      = prompt;
            promptText.color     = Color.white;
            promptText.fontStyle = FontStyle.Bold;
            promptText.alignment = TextAnchor.MiddleCenter;
        }

        public void ShowOutcome(string message, Color colour)
        {
            if (outcomeBadge)
            {
                outcomeBadge.color = outcomeBadgeColor;
                outcomeBadge.gameObject.SetActive(true);
            }
            if (!outcomeText) return;
            outcomeText.text      = message;
            outcomeText.color     = colour;
            outcomeText.alignment = TextAnchor.MiddleCenter;
            outcomeText.gameObject.SetActive(true);
        }

        public void HideOutcome()
        {
            if (outcomeText)  outcomeText.gameObject.SetActive(false);
            if (outcomeBadge) outcomeBadge.gameObject.SetActive(false);
        }

        // When disabling, both buttons grey out regardless of selection (so the
        // "frozen, no input accepted" state always reads visually, not just via
        // interactable). When re-enabling, restore whatever MarkPick last set.
        public void EnableButtons(bool on)
        {
            if (btnLeft)  btnLeft.interactable  = on;
            if (btnRight) btnRight.interactable = on;
            if (on)
            {
                SetButtonSelected(btnLeftBG,  btnLeftLabel,  markedSide == "L");
                SetButtonSelected(btnRightBG, btnRightLabel, markedSide == "R");
            }
            else
            {
                SetButtonDisabled(btnLeftBG,  btnLeftLabel);
                SetButtonDisabled(btnRightBG, btnRightLabel);
            }
        }

        public void MarkPick(string side)
        {
            markedSide = side;
            SetHalf(goalHalfLeft,  side == "L" ? litColor : baseHalf);
            SetHalf(goalHalfRight, side == "R" ? litColor : baseHalf);
            SetButtonSelected(btnLeftBG,  btnLeftLabel,  side == "L");
            SetButtonSelected(btnRightBG, btnRightLabel, side == "R");
        }

        public void ShowGetReady(string message)
        {
            if (getReadyText) getReadyText.text = message;
            if (getReadyPanel) getReadyPanel.gameObject.SetActive(true);
        }

        public void HideGetReady()
        {
            if (getReadyPanel) getReadyPanel.gameObject.SetActive(false);
        }

        public void ShowTimer()
        {
            if (timerBG) timerBG.SetActive(true);
        }

        public void HideTimer()
        {
            if (timerBG) timerBG.SetActive(false);
        }

        public void ShowPracticeBadge()
        {
            if (practiceBadge) practiceBadge.gameObject.SetActive(true);
        }

        public void HidePracticeBadge()
        {
            if (practiceBadge) practiceBadge.gameObject.SetActive(false);
        }

        public void ShowTutorialPanel(string message)
        {
            if (tutorialText) tutorialText.text = message;
            if (tutorialPanel) tutorialPanel.gameObject.SetActive(true);
        }

        public void HideTutorialPanel()
        {
            if (tutorialPanel) tutorialPanel.gameObject.SetActive(false);
        }

        // ── Scene ─────────────────────────────────────────────────────────

        public void ResetScene()
        {
            StopAllCoroutines();
            timerCo = null;
            if (keeper && keeperHome) keeper.SetPositionAndRotation(keeperHome.position, keeperHome.rotation);
            if (ball && ballStart)    ball.SetPositionAndRotation(ballStart.position, ballStart.rotation);
            ClearButtonHighlights();
            ShowHiddenShooter();
            HideOutcome();
            HideGetReady();
            SetPrompt("");
            if (timerFill) timerFill.enabled = true;
            SetTimerFraction(0f);
        }

        public void ShowHiddenShooter()
        {
            if (shooterRenderer) shooterRenderer.material.color = hiddenColor;
            if (shooterJersey)   shooterJersey.material.color   = hiddenColor;
            EnsureShooterLabelStyle();
            if (shooterLabel) shooterLabel.text = "?";
        }

        public void RevealShooter(string letter)
        {
            if (string.IsNullOrEmpty(letter)) return;
            int i = Mathf.Clamp(letter[0] - 'A', 0, 3);
            Color c = charColors[i];
            if (shooterRenderer) shooterRenderer.material.color = c;
            if (shooterJersey)   shooterJersey.material.color   = new Color(c.r*.70f, c.g*.70f, c.b*.70f);
            EnsureShooterLabelStyle();
            if (shooterLabel) shooterLabel.text = letter;
        }

        // ── Outcome animation — KEY FIX ───────────────────────────────────
        // saved  = keeper is in the correct direction
        // goal   = keeper is in wrong direction OR no save
        // The ball target differs: saved → stops at keeper, goal → flies to corner

        public void AnimateOutcome(string actualDir, string finalPick, string saveType, int points)
        {
            bool keeperSaved = saveType == "diving_save" || saveType == "slow_save";

            // ── Keeper dive ───────────────────────────────────────────────
            if (finalPick != null)
            {
                Transform keeperTarget = finalPick == "L" ? keeperDiveLeft : keeperDiveRight;
                if (keeper && keeperTarget)
                    StartCoroutine(MoveObj(keeper, keeperTarget, diveTime, false));
            }

            // ── Ball flight ───────────────────────────────────────────────
            Transform ballTarget;
            if (keeperSaved)
            {
                // Ball stops where the keeper caught it — use save anchors if set,
                // otherwise use the keeper dive position directly
                if (actualDir == "L")
                    ballTarget = (ballSaveLeft  != null) ? ballSaveLeft  : keeperDiveLeft;
                else
                    ballTarget = (ballSaveRight != null) ? ballSaveRight : keeperDiveRight;

                // Ball flies to keeper at reduced height (not a big arc — it's caught)
                if (ball && ballTarget)
                    StartCoroutine(BallToKeeper(ball, ballTarget, flyTime));
            }
            else
            {
                // Ball flies past keeper into corner
                ballTarget = actualDir == "L" ? ballCornerLeft : ballCornerRight;
                if (ball && ballTarget)
                    StartCoroutine(MoveObj(ball, ballTarget, flyTime, true));
            }

            // ── Goal half highlight ───────────────────────────────────────
            SetHalf(actualDir == "L" ? goalHalfLeft : goalHalfRight, actualColor);

            // ── Outcome badge text ────────────────────────────────────────
            string label; Color colour;
            if      (points > 0)  { label = "+" + points + "  Diving save!"; colour = new Color(0.20f, 0.78f, 0.48f); }
            else if (points == 0) { label = "Slow save";                     colour = new Color(0.96f, 0.72f, 0.25f); }
            else if (points == -4){ label = "No save  " + points;            colour = new Color(0.95f, 0.40f, 0.36f); }
            else                  { label = "Goal!  " + points;              colour = new Color(0.95f, 0.40f, 0.36f); }

            ShowOutcome(label, colour);
            SetPrompt("");
        }

        // Ball moves toward keeper in a short low arc — looks like it's caught
        IEnumerator BallToKeeper(Transform obj, Transform target, float dur)
        {
            Vector3    p0 = obj.position;
            Quaternion r0 = obj.rotation;
            float elapsed = 0f;
            dur = Mathf.Max(0.01f, dur);
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, elapsed / dur);
                Vector3 p = Vector3.Lerp(p0, target.position, k);
                // Small bump arc (only 30% of normal arc height) — feels like a catch
                p.y += Mathf.Sin(k * Mathf.PI) * (arcHeight * 0.3f);
                obj.position = p;
                obj.rotation = Quaternion.Slerp(r0, target.rotation, k);
                yield return null;
            }
            obj.SetPositionAndRotation(target.position, target.rotation);
        }

        // ── Timer ─────────────────────────────────────────────────────────

        public void StartTimer(float duration)
        {
            if (!timerFill) return;
            if (timerCo != null) StopCoroutine(timerCo);
            timerFill.enabled = true;
            SetTimerFraction(1f);
            timerCo = StartCoroutine(TimerRoutine(Mathf.Max(0.01f, duration)));
        }

        public void StopTimer()
        {
            if (timerCo != null) { StopCoroutine(timerCo); timerCo = null; }
            if (timerFill) { timerFill.enabled = true; SetTimerFraction(0f); }
        }

        IEnumerator TimerRoutine(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SetTimerFraction(1f - elapsed / duration);
                yield return null;
            }
            SetTimerFraction(0f);
            timerCo = null;
        }

        // ── Generic move ──────────────────────────────────────────────────

        IEnumerator MoveObj(Transform obj, Transform target, float dur, bool arc)
        {
            Vector3 p0 = obj.position; Quaternion r0 = obj.rotation;
            float elapsed = 0f; dur = Mathf.Max(0.01f, dur);
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, elapsed / dur);
                Vector3 p = Vector3.Lerp(p0, target.position, k);
                if (arc) p.y += Mathf.Sin(k * Mathf.PI) * arcHeight;
                obj.position = p; obj.rotation = Quaternion.Slerp(r0, target.rotation, k);
                yield return null;
            }
            obj.SetPositionAndRotation(target.position, target.rotation);
        }

        // ── Helpers ───────────────────────────────────────────────────────

        void SetButtonSelected(Image bg, Text label, bool selected)
        {
            if (bg)
            {
                bg.color = selected ? btnSelected : btnNormal;
                Outline o = bg.GetComponent<Outline>();
                if (o) o.effectColor = selected ? btnBorderSelect : btnBorderNorm;
            }
            if (label) label.color = selected ? btnTextSelected : btnTextNormal;
        }

        void ClearButtonHighlights()
        {
            markedSide = null;
            SetButtonSelected(btnLeftBG,  btnLeftLabel,  false);
            SetButtonSelected(btnRightBG, btnRightLabel, false);
            SetHalf(goalHalfLeft,  baseHalf);
            SetHalf(goalHalfRight, baseHalf);
        }

        void SetButtonDisabled(Image bg, Text label)
        {
            if (bg)
            {
                bg.color = btnDisabledBG;
                Outline o = bg.GetComponent<Outline>();
                if (o) o.effectColor = btnBorderNorm;
            }
            if (label) label.color = btnDisabledText;
        }

        void EnsureShooterLabelStyle()
        {
            if (!shooterLabel) return;
            shooterLabel.anchor        = TextAnchor.MiddleCenter;
            shooterLabel.alignment     = TextAlignment.Center;
            shooterLabel.fontStyle     = FontStyle.Bold;
            shooterLabel.fontSize      = 96;
            shooterLabel.characterSize = 0.19f; // matches the 1.9x shooter scale in GoalkeeperSceneBuilder
            shooterLabel.color         = Color.white;
        }

        void ConfigureButton(Button button, Image bg, Text label)
        {
            if (button) { button.transition = Selectable.Transition.None; button.targetGraphic = bg; }
            SetButtonSelected(bg, label, false);
        }

        void SetHalf(MeshRenderer r, Color c) { if (r) r.material.color = c; }
    }
}
