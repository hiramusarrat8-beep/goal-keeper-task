using System.Collections;
using UnityEngine;
using UXF;

namespace GoalkeeperUXF
{
    // Runs the untimed onboarding flow (intro -> character explainer -> stay/switch
    // explainer) before handing off to GoalkeeperSessionGenerator, which builds the
    // practice block + the real task blocks and starts the first trial. Wired as the
    // onSessionBegin target in place of GenerateExperiment (see GoalkeeperSceneBuilder).
    // Every screen advances on Spacebar — kept separate from the Left/Right prediction
    // buttons so "continue" never gets confused with an in-game directional choice.
    [RequireComponent(typeof(GoalkeeperView))]
    public class GoalkeeperTutorialRunner : MonoBehaviour
    {
        public GoalkeeperSessionGenerator sessionGenerator;

        GoalkeeperView view;
        bool waitingForPress;

        void Awake() { view = GetComponent<GoalkeeperView>(); }

        void Update()
        {
            if (waitingForPress && Input.GetKeyDown(KeyCode.Space)) waitingForPress = false;
        }

        public void BeginTutorial(Session session)
        {
            StartCoroutine(RunTutorial(session));
        }

        IEnumerator RunTutorial(Session session)
        {
            // ── 1. Intro ────────────────────────────────────────────────
            view.ShowTutorialPanel(
                "Welcome! Each round you'll see a goalkeeper (you) facing a shooter.\n\n" +
                "First, predict which way the shot will go: press Left or Right.\n" +
                "Then, once the shooter is revealed, you can stay with your guess or switch.\n\n" +
                "Press Spacebar to continue.");
            yield return WaitForPress();

            // ── 2. Character explainer ─────────────────────────────────
            view.ShowTutorialPanel(
                "There are 4 possible shooters — A, B, C, and D — each shown in its own color. Watch:");
            yield return new WaitForSeconds(1f);

            foreach (var letter in GoalkeeperStructure.CharLetters)
            {
                view.RevealShooter(letter);
                view.ShowTutorialPanel("This is Shooter " + letter + ".");
                yield return new WaitForSeconds(1.2f);
            }

            view.ShowHiddenShooter();
            view.ShowTutorialPanel(
                "Each shooter could go either left or right — you won't know which until " +
                "they're revealed.\n\nPress Spacebar to continue.");
            yield return WaitForPress();

            // ── 3. Stay-or-switch explainer ─────────────────────────────
            view.ShowHiddenShooter();
            view.ShowTutorialPanel("Example: say you guess LEFT before seeing the shooter...");
            view.MarkPick("L");
            yield return new WaitForSeconds(1.2f);

            view.ShowTutorialPanel("Then the shooter is revealed...");
            view.RevealShooter("B");
            yield return new WaitForSeconds(1.2f);

            view.ShowTutorialPanel(
                "Now you can STAY with your guess, or SWITCH to the other side — your choice, " +
                "no clock.\n\nPress Spacebar to begin the practice round.");
            yield return WaitForPress();

            view.HideTutorialPanel();

            sessionGenerator.GenerateExperiment(session);
        }

        IEnumerator WaitForPress()
        {
            waitingForPress = true;
            while (waitingForPress) yield return null;
        }
    }
}
