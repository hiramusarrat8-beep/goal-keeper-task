using System.Collections;
using UnityEngine;
using UXF;

namespace GoalkeeperUXF
{
    [RequireComponent(typeof(GoalkeeperView))]
    public class GoalkeeperTrialRunner : MonoBehaviour
    {
        public Session session;
        public int score;
        int practiceScore;

        GoalkeeperView view;
        Trial  trial;
        string shooter, actualDir;
        string initialPick, finalPick;
        bool   acceptingInitial, acceptingFinal;
        bool   isPractice;
        bool   shownRealTaskIntro;
        bool   waitingForIntroPress;

        void Awake() { view = GetComponent<GoalkeeperView>(); }

        void Update()
        {
            if (waitingForIntroPress)
            {
                if (Input.GetKeyDown(KeyCode.Space)) waitingForIntroPress = false;
                return;
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow))  Submit("L");
            if (Input.GetKeyDown(KeyCode.RightArrow)) Submit("R");
        }

        public void Submit(string side)
        {
            if (acceptingInitial)
            {
                initialPick = side;
                finalPick   = side;
                acceptingInitial = false;
                view.MarkPick(side);
            }
            else if (acceptingFinal)
            {
                finalPick = side;
                acceptingFinal = false;
                view.MarkPick(side);
            }
        }

        public void OnTrialBegin(Trial t)
        {
            trial      = t;
            shooter    = t.settings.GetString("shooter");
            actualDir  = t.settings.GetString("actual_direction");
            isPractice = t.settings.GetBool("is_practice");
            StartCoroutine(RunTrial());
        }

        IEnumerator RunTrial()
        {
            if (!isPractice && !shownRealTaskIntro)
            {
                shownRealTaskIntro = true;
                view.ShowTutorialPanel(
                    "Practice is over — same task, but now each decision has a time limit.\n\n" +
                    "Press Spacebar to begin.");
                waitingForIntroPress = true;
                while (waitingForIntroPress) yield return null;
                view.HideTutorialPanel();
            }

            initialPick = null;
            finalPick   = null;
            view.ResetScene();
            view.SetScore(isPractice ? practiceScore : score);
            if (isPractice) { view.ShowPracticeBadge(); view.HideTimer(); }
            else            { view.HidePracticeBadge(); view.ShowTimer(); }

            // Defaults mirror the fixed timing table: initial 3s / ISI 1s / character
            // 1.5s / ISI 1s / outcome 2s / ITI 2s. RangeOr still honours "<key>_min"/
            // "<key>_max" if a settings file defines a jittered range instead.
            float initialWindow = session.settings.GetFloat("initial_window", 3f);
            float isi1          = RangeOr("isi_1", 1f, 1f);
            float revealWindow  = session.settings.GetFloat("reveal_window", 1.5f);
            float isi2          = RangeOr("isi_2", 1f, 1f);
            float outcomeDur    = session.settings.GetFloat("outcome_duration", 2f);
            float iti           = RangeOr("iti", 2f, 2f);

            // ── 1. Initial prediction — shooter hidden ────────────────────
            view.SetPrompt("Which way will the shot go?");
            view.EnableButtons(true);
            acceptingInitial = true;
            if (isPractice)
            {
                while (acceptingInitial) yield return null;
            }
            else
            {
                view.StartTimer(initialWindow);
                float t = 0f;
                while (t < initialWindow && acceptingInitial)
                { t += Time.deltaTime; yield return null; }
                view.StopTimer();
            }
            acceptingInitial = false;
            view.EnableButtons(false);
            if (initialPick != null) finalPick = initialPick;

            // ── 2. ISI 1 ─────────────────────────────────────────────────
            view.SetPrompt("");
            yield return new WaitForSeconds(isi1);

            // ── 3. Shooter reveal — stay or switch ────────────────────────
            view.RevealShooter(shooter);
            if (initialPick != null)
                view.SetPrompt("Stay with your guess  or  Switch?");
            else
                view.SetPrompt("You missed the first guess — pick a side now.");

            if (finalPick != null) view.MarkPick(finalPick);
            view.EnableButtons(true);
            acceptingFinal = true;
            if (isPractice)
            {
                while (acceptingFinal) yield return null;
            }
            else
            {
                view.StartTimer(revealWindow);
                float t2 = 0f;
                while (t2 < revealWindow && acceptingFinal)
                { t2 += Time.deltaTime; yield return null; }
                view.StopTimer();
            }
            acceptingFinal = false;
            view.EnableButtons(false);

            // ── 4. ISI 2 ─────────────────────────────────────────────────
            yield return new WaitForSeconds(isi2);

            // ── 5. Outcome ───────────────────────────────────────────────
            var r = Score();
            if (isPractice) practiceScore += r.points; else score += r.points;
            view.AnimateOutcome(actualDir, finalPick, r.saveType, r.points);
            view.SetScore(isPractice ? practiceScore : score);
            WriteResult(r);
            yield return new WaitForSeconds(outcomeDur);

            // ── 6. ITI ───────────────────────────────────────────────────
            view.HideOutcome();
            view.ShowGetReady(isPractice ? "Practice — next shot..." : "Next round...");
            yield return new WaitForSeconds(iti);
            view.HideGetReady();

            session.EndCurrentTrial();
            if (trial == session.LastTrial) session.End();
            else session.BeginNextTrial();
        }

        struct R { public bool changed, correct; public int points; public string saveType; }

        R Score()
        {
            bool changed = initialPick == null
                ? finalPick != null
                : (finalPick != null && finalPick != initialPick);
            bool correct = finalPick != null && finalPick == actualDir;

            string save; int pts;
            if (finalPick == null)        { save = "no_save";     pts = -4; }
            else if (correct && !changed) { save = "diving_save"; pts =  2; }
            else if (correct && changed)  { save = "slow_save";   pts =  0; }
            else                          { save = "goal";        pts = -2; }
            return new R { changed = changed, correct = correct, points = pts, saveType = save };
        }

        void WriteResult(R r)
        {
            trial.result["is_practice"]       = isPractice;
            trial.result["shooter"]           = trial.settings.GetString("shooter");
            trial.result["prev_shooter"]      = trial.settings.GetString("prev_shooter");
            trial.result["actual_direction"]  = actualDir;
            trial.result["paired_transition"] = trial.settings.GetBool("paired_transition");
            trial.result["is_block_start"]    = trial.settings.GetBool("is_block_start");
            trial.result["paired_structure"]  = trial.settings.GetString("paired_structure");
            trial.result["reversal_at_start"] = trial.settings.GetString("reversal_at_start");
            trial.result["initial_prediction"]= initialPick ?? "NA";
            trial.result["final_prediction"]  = finalPick   ?? "NA";
            trial.result["changed"]           = r.changed;
            trial.result["correct"]           = r.correct;
            trial.result["points"]            = r.points;
            trial.result["save_type"]         = r.saveType;
            trial.result["running_score"]     = isPractice ? practiceScore : score;
        }

        // Reads "<baseKey>_min"/"<baseKey>_max" and jitters uniformly between them
        // (the paper's ISI/ITI design). Falls back to a flat "<baseKey>" value for
        // fixed-duration settings profiles (e.g. settings_behavioural.json), then to
        // a jittered paper-default range if nothing is configured at all.
        float RangeOr(string baseKey, float defMin, float defMax)
        {
            var s = session.settings;
            if (s.ContainsKey(baseKey + "_min") && s.ContainsKey(baseKey + "_max"))
                return Random.Range(s.GetFloat(baseKey + "_min"), s.GetFloat(baseKey + "_max"));
            if (s.ContainsKey(baseKey))
                return s.GetFloat(baseKey);
            return Random.Range(defMin, defMax);
        }
    }
}
