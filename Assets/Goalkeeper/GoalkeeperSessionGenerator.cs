using UnityEngine;
using UXF;

namespace GoalkeeperUXF
{
    /// Experiment specification (the "what"). Hook GenerateExperiment to the
    /// Session's OnSessionBegin UnityEvent. It builds the four blocks, samples the
    /// trial sequence with a logged seed, and writes each trial's independent
    /// variables into trial.settings.
    public class GoalkeeperSessionGenerator : MonoBehaviour
    {
        public void GenerateExperiment(Session session)
        {
            int   tpb  = IntOr(session, "trials_per_block", 40);
            float pp   = FloatOr(session, "paired_prob", 0.8f);
            float rp   = FloatOr(session, "rare_prob", 0.2f);
            float pref = FloatOr(session, "preference_prob", 0.8f);

            int seed = IntOr(session, "seed", System.Environment.TickCount);
            session.settings.SetValue("resolved_seed", seed); // captured in settings.json

            var schedule = GoalkeeperStructure.DefaultSchedule(tpb);
            var specs = GoalkeeperStructure.Generate(schedule, pp, rp, pref, seed);
            var practiceSpecs = GoalkeeperStructure.PracticeSequence();

            // Block 0 is the practice block; the real schedule blocks shift by one.
            var blocks = new Block[schedule.Length + 1];
            blocks[0] = session.CreateBlock(practiceSpecs.Length);
            for (int i = 0; i < schedule.Length; i++)
                blocks[i + 1] = session.CreateBlock(schedule[i].n);

            foreach (var s in practiceSpecs)
            {
                Trial t = blocks[0].GetRelativeTrial(s.indexInBlock + 1); // 1-indexed
                t.settings.SetValue("shooter",          GoalkeeperStructure.CharLetters[s.shooter]);
                t.settings.SetValue("prev_shooter",     GoalkeeperStructure.CharLetters[s.prevShooter]);
                t.settings.SetValue("actual_direction", s.actualDir == 1 ? "R" : "L");
                t.settings.SetValue("paired_transition", s.wasPaired);
                t.settings.SetValue("is_block_start",    s.isBlockStart);
                t.settings.SetValue("paired_structure",  "practice"); // pairedStructure is a -1 sentinel here, never index PairLabel with it
                t.settings.SetValue("reversal_at_start", s.reversal);
                t.settings.SetValue("is_practice",       true);
            }

            foreach (var s in specs)
            {
                Trial t = blocks[s.block + 1].GetRelativeTrial(s.indexInBlock + 1); // 1-indexed
                t.settings.SetValue("shooter",          GoalkeeperStructure.CharLetters[s.shooter]);
                t.settings.SetValue("prev_shooter",     GoalkeeperStructure.CharLetters[s.prevShooter]);
                t.settings.SetValue("actual_direction", s.actualDir == 1 ? "R" : "L");
                t.settings.SetValue("paired_transition", s.wasPaired);
                t.settings.SetValue("is_block_start",    s.isBlockStart);
                t.settings.SetValue("paired_structure",  GoalkeeperStructure.PairLabel[s.pairedStructure]);
                t.settings.SetValue("reversal_at_start", s.reversal);
                t.settings.SetValue("is_practice",       false);
            }

            // Kick off the run.
            session.FirstTrial.Begin();
        }

        // Settings helpers that fall back to a default if the key is absent,
        // so the scene still runs without a settings profile selected.
        public static int IntOr(Session s, string key, int def)
        {
            try { return s.settings.GetInt(key); } catch { return def; }
        }
        public static float FloatOr(Session s, string key, float def)
        {
            try { return s.settings.GetFloat(key); } catch { return def; }
        }
    }
}
