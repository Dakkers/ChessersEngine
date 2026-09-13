using System.Diagnostics;

namespace ChessersEngine {
    /// <summary>A tally of engine work performed since the last <see cref="EngineCounters.Reset"/>.</summary>
    public struct CounterSnapshot {
        public long nodes;
        public long moveGen;
        public long tilesProduced;
        public long boardClones;
        public long copyStates;
        public long moveApplies;
        public long moveUndos;
        public long evals;
    }

    /// <summary>
    /// Hardware-independent tallies of engine work, for the performance oracle. Every increment
    /// method is <c>[Conditional("BENCH")]</c>, so the compiler erases the calls -- argument
    /// evaluation included -- in any build without that symbol. Only ChessersEngine.Bench defines it.
    ///
    /// The increment points are a cross-language contract; see ChessersEngine.Bench/README.md.
    /// Single-threaded by design: the harness never runs workloads concurrently.
    /// </summary>
    public static class EngineCounters {
        static long _nodes;
        static long _moveGen;
        static long _tilesProduced;
        static long _boardClones;
        static long _copyStates;
        static long _moveApplies;
        static long _moveUndos;
        static long _evals;

        /// <summary>Whether counting was compiled into this build.</summary>
        public static bool Enabled =>
#if BENCH
            true;
#else
            false;
#endif

        [Conditional("BENCH")]
        public static void Node () => _nodes++;

        [Conditional("BENCH")]
        public static void MoveGen (int tilesProduced) {
            _moveGen++;
            _tilesProduced += tilesProduced;
        }

        [Conditional("BENCH")]
        public static void BoardClone () => _boardClones++;

        [Conditional("BENCH")]
        public static void CopyState () => _copyStates++;

        [Conditional("BENCH")]
        public static void MoveApply () => _moveApplies++;

        [Conditional("BENCH")]
        public static void MoveUndo () => _moveUndos++;

        [Conditional("BENCH")]
        public static void Eval () => _evals++;

        public static void Reset () {
            _nodes = 0;
            _moveGen = 0;
            _tilesProduced = 0;
            _boardClones = 0;
            _copyStates = 0;
            _moveApplies = 0;
            _moveUndos = 0;
            _evals = 0;
        }

        public static CounterSnapshot Take () => new CounterSnapshot {
            nodes = _nodes,
            moveGen = _moveGen,
            tilesProduced = _tilesProduced,
            boardClones = _boardClones,
            copyStates = _copyStates,
            moveApplies = _moveApplies,
            moveUndos = _moveUndos,
            evals = _evals,
        };
    }
}
