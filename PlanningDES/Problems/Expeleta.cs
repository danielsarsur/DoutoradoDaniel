using System.Collections.Generic;
using System.Linq;
using UltraDES;
using DFA = UltraDES.DeterministicFiniteAutomaton;

namespace PlanningDES.Problems
{
    public class Ezpeleta : ISchedulingProblem
    {
        public readonly Dictionary<int, AbstractEvent> _e;

        public Ezpeleta()
        {
            _e = new[]
            {
                1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35,
                36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49
            }.ToDictionary(alias => alias,
                alias => (AbstractEvent)new Event($"{alias}", alias % 2 == 0 ? Controllability.Uncontrollable : Controllability.Controllable));

            var s = Enumerable.Range(0, 6).ToDictionary(i => i,
                i => new ExpandedState($"{i}", i == 0 ? 0u : 1u, i == 0 ? Marking.Marked : Marking.Unmarked));

            // Esteira 1
            var C1 = new DFA(new[]
            {
                new Transition(s[0], _e[1], s[1]),
                new Transition(s[1], _e[2], s[0])
                }, s[0], "C1");

            // Esteira 2
            var C2 = new DFA(new[]
            {
                new Transition(s[0], _e[3], s[1]),
                new Transition(s[1], _e[4], s[0])
                }, s[0], "C2");

            // Esteira 3
            var C3 = new DFA(new[]
            {
                new Transition(s[0], _e[5], s[1]),
                new Transition(s[1], _e[6], s[0])
                }, s[0], "C3");

            // Esteira 4
            var C4 = new DFA(new[]
            {
                new Transition(s[0], _e[7], s[1]),
                new Transition(s[1], _e[8], s[0])
                }, s[0], "C4");

            // Esteira 5
            var C5 = new DFA(new[]
            {
                new Transition(s[0], _e[9], s[1]),
                new Transition(s[1], _e[10], s[0])
                }, s[0], "C5");

            // Esteira 6
            var C6 = new DFA(new[]
            {
                new Transition(s[0], _e[11], s[1]),
                new Transition(s[1], _e[12], s[0])
                }, s[0], "C6");

            // Maquina 1
            var M1 = new DFA(new[]
            {
                new Transition(s[0], _e[13], s[1]),
                new Transition(s[1], _e[14], s[0])
                }, s[0], "M1");

            // Maquina 2
            var M2 = new DFA(new[]
            {
                new Transition(s[0], _e[15], s[1]),
                new Transition(s[1], _e[16], s[0]),
                new Transition(s[0], _e[17], s[2]),
                new Transition(s[2], _e[18], s[0])
                }, s[0], "M2");

            // Maquina 3
            var M3 = new DFA(new[]
            {
                new Transition(s[0], _e[19], s[1]),
                new Transition(s[1], _e[20], s[0]),
                new Transition(s[0], _e[21], s[2]),
                new Transition(s[2], _e[22], s[0])
                }, s[0], "M3");

            // Maquina 4
            var M4 = new DFA(new[]
            {
                new Transition(s[0], _e[23], s[1]),
                new Transition(s[1], _e[24], s[0]),
                new Transition(s[0], _e[25], s[2]),
                new Transition(s[2], _e[26], s[0])
                }, s[0], "M4");

            // Robo 1
            var R1 = new DFA(new[]
            {
                new Transition(s[0], _e[27], s[1]),
                new Transition(s[1], _e[28], s[0]),
                new Transition(s[0], _e[31], s[2]),
                new Transition(s[2], _e[32], s[0]),
                new Transition(s[0], _e[29], s[3]),
                new Transition(s[3], _e[30], s[0])
                }, s[0], "R1");

            // Robo 2
            var R2 = new DFA(new[]
            {
                new Transition(s[0], _e[39], s[1]),
                new Transition(s[1], _e[40], s[0]),
                new Transition(s[0], _e[41], s[4]),
                new Transition(s[4], _e[42], s[0]),
                new Transition(s[0], _e[37], s[2]),
                new Transition(s[2], _e[38], s[0]),
                new Transition(s[0], _e[33], s[3]),
                new Transition(s[3], _e[34], s[0]),
                new Transition(s[0], _e[35], s[5]),
                new Transition(s[5], _e[36], s[0])
                }, s[0], "R2");

            // Robo 3
            var R3 = new DFA(new[]
            {
                new Transition(s[0], _e[43], s[1]),
                new Transition(s[0], _e[45], s[1]),
                new Transition(s[1], _e[44], s[0]),
                new Transition(s[0], _e[47], s[2]),
                new Transition(s[2], _e[48], s[0])
                }, s[0], "R3");

            // Especificações

            s = Enumerable.Range(0, 6).ToDictionary(i => i,
                i => new ExpandedState($"{i}", 0u, i == 0 ? Marking.Marked : Marking.Unmarked));

            // Buffer 1
            var E1 = new DFA(new[]
            {
                new Transition(s[0], _e[2], s[1]),
                new Transition(s[1], _e[27], s[0]),
                new Transition(s[1], _e[29], s[0])
                }, s[0], "E1");

            // Buffer 2
            var E2 = new DFA(new[]
            {
                new Transition(s[0], _e[28], s[1]),
                new Transition(s[1], _e[13], s[0]),
                new Transition(s[0], _e[14], s[2]),
                new Transition(s[2], _e[39], s[0])
                }, s[0], "E2");

            // Buffer 3
            var E3 = new DFA(new[]
            {
                new Transition(s[0], _e[4], s[1]),
                new Transition(s[1], _e[33], s[0])
                }, s[0], "E3");

            // Buffer 4
            var E4 = new DFA(new[]
            {
                new Transition(s[0], _e[34], s[1]),
                new Transition(s[1], _e[17], s[0]),
                new Transition(s[0], _e[18], s[2]),
                new Transition(s[2], _e[35], s[0]),
                new Transition(s[0], _e[40], s[3]),
                new Transition(s[3], _e[15], s[0]),
                new Transition(s[0], _e[16], s[4]),
                new Transition(s[4], _e[43], s[0])
                }, s[0], "E4");

            // Buffer 5
            var E5 = new DFA(new[]
            {
                new Transition(s[0], _e[6], s[1]),
                new Transition(s[1], _e[47], s[0])
                }, s[0], "E5");

            // Buffer 6
            var E6 = new DFA(new[]
            {
                new Transition(s[0], _e[32], s[1]),
                new Transition(s[1], _e[7], s[0])
                }, s[0], "E6");

            // Buffer 7
            var E7 = new DFA(new[]
            {
                new Transition(s[0], _e[30], s[1]),
                new Transition(s[1], _e[19], s[0]),
                new Transition(s[0], _e[20], s[2]),
                new Transition(s[2], _e[41], s[0]),
                new Transition(s[0], _e[38], s[3]),
                new Transition(s[3], _e[21], s[0]),
                new Transition(s[0], _e[22], s[4]),
                new Transition(s[4], _e[31], s[0])
                }, s[0], "E7");

            // Buffer 8
            var E8 = new DFA(new[]
            {
                new Transition(s[0], _e[36], s[1]),
                new Transition(s[1], _e[9], s[0])
                }, s[0], "E8");

            // Buffer 9
            var E9 = new DFA(new[]
            {
                new Transition(s[0], _e[48], s[1]),
                new Transition(s[1], _e[25], s[0]),
                new Transition(s[0], _e[26], s[2]),
                new Transition(s[2], _e[37], s[0]),
                new Transition(s[0], _e[42], s[3]),
                new Transition(s[3], _e[23], s[0]),
                new Transition(s[0], _e[24], s[4]),
                new Transition(s[4], _e[45], s[0])
                }, s[0], "E9");

            // Buffer 10
            var E10 = new DFA(new[]
            {
                new Transition(s[0], _e[44], s[1]),
                new Transition(s[1], _e[11], s[0])
                }, s[0], "E10");


            Supervisor = DFA.MonolithicSupervisor(new[] { C1, C2, C3, C4, C5, C6, M1, M2, M3, M4, R1, R2, R3 },
                    new[] { E1, E2, E3, E4, E5, E6, E7, E8, E9, E10 }, true);

            Events = _e.Values.ToList();

            Transitions = Supervisor.Transitions.GroupBy(t => t.Origin)
                .ToDictionary(g => g.Key, g => g.ToDictionary(t => t.Trigger, t => t.Destination));

        }

        public DFA Supervisor { get; }
        public IEnumerable<AbstractEvent> Events { get; }
        public Dictionary<AbstractState, Dictionary<AbstractEvent, AbstractState>> Transitions { get; }

        public int Depth => 38;

        public AbstractState InitialState => Supervisor.InitialState;

        public AbstractState TargetState => Supervisor.InitialState;

        public Restriction InitialRestrition(int products)
        {
            return new Restriction(new[]
            {
                (_e[1], (uint)products),  (_e[3], (uint)products),  (_e[5], (uint)products),  (_e[7], (uint)products),  (_e[9], (uint)products),
                (_e[11], (uint)products), (_e[13], (uint)products), (_e[15], (uint)products), (_e[17], (uint)products), (_e[19], (uint)products),
                (_e[21], (uint)products), (_e[23], (uint)products), (_e[25], (uint)products), (_e[27], (uint)products), (_e[29], (uint)products),
                (_e[31], (uint)products), (_e[33], (uint)products), (_e[35], (uint)products), (_e[37], (uint)products), (_e[39], (uint)products),
                (_e[41], (uint)products), (_e[43], (uint)products), (_e[45], (uint)products), (_e[47], (uint)products)
            });
        }

        public Scheduler InitialScheduler =>
            new Scheduler(_e.Select(kvp => (kvp.Value, kvp.Value.IsControllable ? 0.0f : float.PositiveInfinity)),
                new[]
                {
                    (_e[1], _e[2], 25f),   (_e[3], _e[4], 25f),    (_e[5], _e[6], 38f),    (_e[7], _e[8], 21f),   (_e[9], _e[10], 19f),
                    (_e[11], _e[12], 24f), (_e[13], _e[14], 100f), (_e[15], _e[16], 100f), (_e[17], _e[18], 70f), (_e[19], _e[20], 75f),
                    (_e[21], _e[22], 80f), (_e[23], _e[24], 200f), (_e[25], _e[26], 160f), (_e[27], _e[28], 15f), (_e[29], _e[30], 15f),
                    (_e[31], _e[32], 15f), (_e[33], _e[34], 20f),  (_e[35], _e[36], 20f),  (_e[37], _e[38], 20f), (_e[39], _e[40], 20f),
                    (_e[41], _e[42], 20f), (_e[43], _e[44], 30f),  (_e[45], _e[46], 30f),  (_e[47], _e[48], 30f)
                });

    }
}
