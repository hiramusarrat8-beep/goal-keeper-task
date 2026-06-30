using System;

namespace GoalkeeperUXF
{
    /// Pure task logic: the relational structure, the reversal schedule, and the
    /// seeded character/outcome sequence. No UXF or Unity dependency, so it can be
    /// unit-tested on its own. The UXF generator turns these specs into Trials.
    public static class GoalkeeperStructure
    {
        public static readonly string[] CharLetters = { "A", "B", "C", "D" };
        public static readonly string[] PairLabel = { "AB&CD", "AC&BD", "AD&BC" };

        // partner[pairing][character] = paired partner under that matching.
        static readonly int[][] Partner =
        {
            new[] { 1, 0, 3, 2 }, // AB_CD
            new[] { 2, 3, 0, 1 }, // AC_BD
            new[] { 3, 2, 1, 0 }, // AD_BC
        };

        public struct BlockDef
        {
            public int n;            // trials in block
            public int paired;       // pairing index for 0.8 transitions
            public int rare;         // pairing index for 0.2 transitions
            public int[] prefs;      // goal preference per character (0=Left,1=Right)
            public string reversal;  // change that happens at this block's start
        }

        public struct TrialSpec
        {
            public int block;
            public int indexInBlock;
            public bool isBlockStart;
            public int shooter;
            public int prevShooter;
            public int actualDir;       // 0=Left, 1=Right
            public bool wasPaired;      // shooter is paired partner of prevShooter
            public int pairedStructure; // pairing index in effect this block
            public string reversal;     // "none" / "transition" / "preference"
        }

        /// The paper's main-task schedule (4 blocks). Transition reversals at the
        /// start of blocks 2 and 4; a preference reversal at the start of block 3.
        public static BlockDef[] DefaultSchedule(int trialsPerBlock)
        {
            int[] early = { 1, 0, 1, 0 }; // A=R, B=L, C=R, D=L
            int[] late  = { 1, 1, 0, 0 }; // after preference reversal: B,C swap
            return new[]
            {
                new BlockDef { n = trialsPerBlock, paired = 0, rare = 1, prefs = early, reversal = "none"       },
                new BlockDef { n = trialsPerBlock, paired = 1, rare = 0, prefs = early, reversal = "transition" },
                new BlockDef { n = trialsPerBlock, paired = 1, rare = 0, prefs = late,  reversal = "preference" },
                new BlockDef { n = trialsPerBlock, paired = 0, rare = 1, prefs = late,  reversal = "transition" },
            };
        }

        public static TrialSpec[] Generate(BlockDef[] schedule, double pairedProb, double rareProb, double prefProb, int seed)
        {
            var rng = new Random(seed);
            int total = 0;
            foreach (var b in schedule) total += b.n;

            var specs = new TrialSpec[total];
            int idx = 0, prev = 0;

            for (int bi = 0; bi < schedule.Length; bi++)
            {
                BlockDef b = schedule[bi];
                double[,] m = BuildMatrix(b, pairedProb, rareProb);

                for (int i = 0; i < b.n; i++)
                {
                    int shooter = idx == 0 ? rng.Next(4) : SampleNext(rng, m, prev);
                    int pref = b.prefs[shooter];
                    int actual = rng.NextDouble() < prefProb ? pref : 1 - pref;
                    bool paired = idx > 0 && shooter == Partner[b.paired][prev];

                    specs[idx] = new TrialSpec
                    {
                        block = bi,
                        indexInBlock = i,
                        isBlockStart = i == 0,
                        shooter = shooter,
                        prevShooter = idx == 0 ? shooter : prev,
                        actualDir = actual,
                        wasPaired = paired,
                        pairedStructure = b.paired,
                        reversal = i == 0 ? b.reversal : "none"
                    };

                    prev = shooter;
                    idx++;
                }
            }
            return specs;
        }

        /// Fixed, hand-authored 10-trial practice sequence — identical for every
        /// participant (not RNG-driven). All 4 letters appear at least twice, L/R
        /// is balanced 5/5, no two consecutive trials repeat the same shooter, and
        /// no pairing/transition structure is implied (pairedStructure = -1
        /// sentinel — callers must not pass it to PairLabel).
        public static TrialSpec[] PracticeSequence()
        {
            // shooter index: A=0 B=1 C=2 D=3 ; direction: 0=Left 1=Right
            var seq = new (int shooter, int dir)[]
            {
                (0,1), (2,0), (1,0), (3,1), (0,0),
                (3,0), (1,1), (2,1), (0,1), (3,0)
            };
            var specs = new TrialSpec[seq.Length];
            int prev = seq[0].shooter;
            for (int i = 0; i < seq.Length; i++)
            {
                specs[i] = new TrialSpec
                {
                    block = 0,
                    indexInBlock = i,
                    isBlockStart = i == 0,
                    shooter = seq[i].shooter,
                    prevShooter = i == 0 ? seq[i].shooter : prev,
                    actualDir = seq[i].dir,
                    wasPaired = false,
                    pairedStructure = -1,
                    reversal = "none"
                };
                prev = seq[i].shooter;
            }
            return specs;
        }

        static double[,] BuildMatrix(BlockDef b, double pp, double rp)
        {
            var m = new double[4, 4];
            for (int s = 0; s < 4; s++)
            {
                m[s, Partner[b.paired][s]] = pp;
                m[s, Partner[b.rare][s]]   = rp;
            }
            return m;
        }

        static int SampleNext(Random rng, double[,] m, int s)
        {
            double r = rng.NextDouble(), cum = 0;
            int last = s;
            for (int t = 0; t < 4; t++)
            {
                double p = m[s, t];
                if (p <= 0) continue;
                last = t;
                cum += p;
                if (r < cum) return t;
            }
            return last;
        }
    }
}
