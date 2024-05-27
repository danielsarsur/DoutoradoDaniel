using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UltraDES;
using DFA = UltraDES.DeterministicFiniteAutomaton;
using Restriction = System.Collections.Generic.Dictionary<UltraDES.AbstractEvent, uint>;
using Scheduler = System.Collections.Generic.Dictionary<UltraDES.AbstractEvent, float>;
using StochasticUpdate = System.Func<System.Collections.Generic.Dictionary<UltraDES.AbstractEvent, float>, UltraDES.AbstractEvent, double, System.Collections.Generic.Dictionary<UltraDES.AbstractEvent, float>>;
using Update = System.Func<System.Collections.Generic.Dictionary<UltraDES.AbstractEvent, float>, UltraDES.AbstractEvent, System.Collections.Generic.Dictionary<UltraDES.AbstractEvent, float>>;


namespace ProgramaDaniel
{
    internal class SF : ISchedulingProblem
    {
        private readonly Dictionary<int, Event> _e;

        public SF()
        {
            var s = new[] { new ExpandedState("0", 0, Marking.Marked), new ExpandedState("1", 1, Marking.Unmarked) };

            _e = new[] { 1, 2, 3, 4 }.ToDictionary(alias => alias, alias =>
                  new Event(alias.ToString(),
                      alias % 2 == 0 ? Controllability.Uncontrollable : Controllability.Controllable));

            var m1 = new DeterministicFiniteAutomaton(
                new[]
                {
                    new Transition(s[0], _e[1], s[1]),
                    new Transition(s[1], _e[2], s[0])
                },
                s[0], "M1");

            var m2 = new DeterministicFiniteAutomaton(
                new[]
                {
                    new Transition(s[0], _e[3], s[1]),
                    new Transition(s[1], _e[4], s[0])
                },
                s[0], "M2");

            s = new[] { new ExpandedState("E", 0, Marking.Marked, 1), new ExpandedState("F", 0, Marking.Unmarked) };

            var e1 = new DeterministicFiniteAutomaton(
                new[]
                {
                    new Transition(s[0], _e[2], s[1]),
                    new Transition(s[1], _e[3], s[0])
                },
            s[0], "E");

            Supervisor = DFA.MonolithicSupervisor(new[] { m1, m2 },
                new[] { e1 }, true);

            Supervisors = DFA.LocalModularSupervisor(new[] { m1, m2 }, new[] { e1 });
        }

        public int Depth => 4;

        public Scheduler InitialScheduler =>
            _e.ToDictionary(kvp => kvp.Value as AbstractEvent,
                kvp => kvp.Value.IsControllable ? 0.0f : float.PositiveInfinity);

        public AbstractState InitialState => Supervisor.InitialState;

        public DFA Supervisor { get; }

        public IEnumerable<DFA> Supervisors { get; }

        public AbstractState TargetState => Supervisor.InitialState;

        public Update UpdateFunction => (old, ev) =>
        {
            var sch = old.ToDictionary(kvp => kvp.Key, kvp =>
            {
                if (kvp.Key.IsControllable) return 0;

                var v = kvp.Value - old[ev];

                return v < 0 ? float.PositiveInfinity : v;
            });

            if (!ev.IsControllable) sch[ev] = float.PositiveInfinity;

            switch (ev.ToString())
            {
                case "1":
                    sch[_e[2]] = 25 + 1;
                    break;
                case "3":
                    sch[_e[4]] = 25 + 1;
                    break;
            }
            return sch;
        };

        public Restriction InitialRestrition(int products)
        {
            return new Restriction
            {
                {_e[1], (uint) (1*products)},
                {_e[3], (uint) (1*products)}
            };
        }
    }

    internal class SFextended : ISchedulingProblem
    {
        private readonly Dictionary<int, Event> _e;

        public SFextended()
        {
            var s = new[] { new ExpandedState("0", 0, Marking.Marked), new ExpandedState("1", 1, Marking.Unmarked) };

            _e = new[] { 1, 2, 3, 4, 5, 6 }.ToDictionary(alias => alias, alias =>
                  new Event(alias.ToString(),
                      alias % 2 == 0 ? Controllability.Uncontrollable : Controllability.Controllable));

            var m1 = new DeterministicFiniteAutomaton(
                new[]
                {
                    new Transition(s[0], _e[1], s[1]),
                    new Transition(s[1], _e[2], s[0])
                },
                s[0], "M1");

            var m2 = new DeterministicFiniteAutomaton(
                new[]
                {
                    new Transition(s[0], _e[3], s[1]),
                    new Transition(s[1], _e[4], s[0])
                },
                s[0], "M2");

            var m3 = new DeterministicFiniteAutomaton(
                new[]
                {
                    new Transition(s[0], _e[5], s[1]),
                    new Transition(s[1], _e[6], s[0])
                },
                s[0], "M3");

            s = new[] { new ExpandedState("E", 0, Marking.Marked, 1), new ExpandedState("F", 0, Marking.Unmarked) };

            var e1 = new DeterministicFiniteAutomaton(
                new[]
                {
                    new Transition(s[0], _e[2], s[1]),
                    new Transition(s[1], _e[3], s[0])
                },
            s[0], "E1");

            var e2 = new DeterministicFiniteAutomaton(
                new[]
                {
                    new Transition(s[0], _e[4], s[1]),
                    new Transition(s[1], _e[5], s[0])
                },
            s[0], "E2");

            Supervisor = DFA.MonolithicSupervisor(new[] { m1, m2, m3 }, new[] { e1, e2 }, true);

            // Divisão das tarefas ativas da Máquina 2 por 2 supervisores modulares locais
            s = new[] { new ExpandedState("0", 0, Marking.Marked), new ExpandedState("1", 0.5, Marking.Unmarked) };

            m2 = new DeterministicFiniteAutomaton(
                new[]
                {
                    new Transition(s[0], _e[3], s[1]),
                    new Transition(s[1], _e[4], s[0])
                },
                s[0], "M2");

            Supervisors = DFA.LocalModularSupervisor(new[] { m1, m2, m3 }, new[] { e1, e2 });
        }

        public int Depth => 6;

        public Scheduler InitialScheduler =>
            _e.ToDictionary(kvp => kvp.Value as AbstractEvent,
                kvp => kvp.Value.IsControllable ? 0.0f : float.PositiveInfinity);

        public AbstractState InitialState => Supervisor.InitialState;

        public DFA Supervisor { get; }

        public IEnumerable<DFA> Supervisors { get; }

        public AbstractState TargetState => Supervisor.InitialState;

        public Update UpdateFunction => (old, ev) =>
        {
            var sch = old.ToDictionary(kvp => kvp.Key, kvp =>
            {
                if (kvp.Key.IsControllable) return 0;

                var v = kvp.Value - old[ev];

                return v < 0 ? float.PositiveInfinity : v;
            });

            if (!ev.IsControllable) sch[ev] = float.PositiveInfinity;

            switch (ev.ToString())
            {
                case "1":
                    sch[_e[2]] = 25 + 1;
                    break;
                case "3":
                    sch[_e[4]] = 25 + 1;
                    break;
                case "5":
                    sch[_e[6]] = 25 + 1;
                    break;
            }
            return sch;
        };

        public Restriction InitialRestrition(int products)
        {
            return new Restriction
            {
                {_e[1], (uint) (1*products)},
                {_e[3], (uint) (1*products)},
                {_e[5], (uint) (1*products)}
            };
        }
    }

    internal class SFM : ISchedulingProblemAB
    {
        public readonly Dictionary<int, Event> _e;

        public SFM()
        {
            _e = new[]
             {
                11, 12, 21, 22, 41,
                42, 51, 52, 53, 54, 31,
                32, 33, 34, 35, 36, 37, 38, 39, 30, 61,
                63, 65, 64, 66, 71, 72, 73, 74, 81, 82
            }.ToDictionary(alias => alias,
                 alias =>
                     new Event(alias.ToString(),
                         alias % 2 == 0 ? Controllability.Uncontrollable : Controllability.Controllable));

            var s = Enumerable.Range(0, 6)
            .ToDictionary(i => i, i =>
            new ExpandedState(i.ToString(), i == 0 ? 0u : 1u, i == 0 ? Marking.Marked : Marking.Unmarked));

            // C1
            var c1 = new DFA(
                new[]
                {
                        new Transition(s[0], _e[11], s[1]),
                        new Transition(s[1], _e[12], s[0])
                },
                s[0], "C1");

            // C2
            var c2 = new DFA(
                new[]
                {
                        new Transition(s[0], _e[21], s[1]),
                        new Transition(s[1], _e[22], s[0])
                },
                s[0], "C2");

            // Fresa
            var fresa = new DFA(
                new[]
                {
                        new Transition(s[0], _e[41], s[1]),
                        new Transition(s[1], _e[42], s[0])
                },
                s[0], "Fresa");

            // MP
            var mp = new DeterministicFiniteAutomaton(
                new[]
                {
                        new Transition(s[0], _e[81], s[1]),
                        new Transition(s[1], _e[82], s[0])
                },
                s[0], "MP");

            // Torno
            var torno = new DeterministicFiniteAutomaton(
                new[]
                {
                        new Transition(s[0], _e[51], s[1]),
                        new Transition(s[1], _e[52], s[0]),
                        new Transition(s[0], _e[53], s[2]),
                        new Transition(s[2], _e[54], s[0])
                },
                s[0], "Torno");

            // C3
            var c3 = new DeterministicFiniteAutomaton(
                new[]
                {
                        new Transition(s[0], _e[71], s[1]),
                        new Transition(s[1], _e[72], s[0]),
                        new Transition(s[0], _e[73], s[2]),
                        new Transition(s[2], _e[74], s[0])
                },
                s[0], "C3");

            // Robô
            var robot = new DeterministicFiniteAutomaton(
                new[]
                {
                        new Transition(s[0], _e[31], s[1]),
                        new Transition(s[1], _e[32], s[0]),
                        new Transition(s[0], _e[33], s[2]),
                        new Transition(s[2], _e[34], s[0]),
                        new Transition(s[0], _e[35], s[3]),
                        new Transition(s[3], _e[36], s[0]),
                        new Transition(s[0], _e[37], s[4]),
                        new Transition(s[4], _e[38], s[0]),
                        new Transition(s[0], _e[39], s[5]),
                        new Transition(s[5], _e[30], s[0])
                },
                s[0], "Robot");

            // MM
            var mm = new DeterministicFiniteAutomaton(
                new[]
                {
                        new Transition(s[0], _e[61], s[1]),
                        new Transition(s[1], _e[63], s[2]),
                        new Transition(s[1], _e[65], s[3]),
                        new Transition(s[2], _e[64], s[0]),
                        new Transition(s[3], _e[66], s[0])
                },
                s[0], "MM");

            // Especificações

            s = Enumerable.Range(0, 6)
            .ToDictionary(i => i, i =>
            new ExpandedState(i.ToString(), 0, i == 0 ? Marking.Marked : Marking.Unmarked));

            // E1
            var e1 = new DeterministicFiniteAutomaton(
                new[]
                {
                        new Transition(s[0], _e[12], s[1]),
                        new Transition(s[1], _e[31], s[0])
                },
                s[0], "E1");

            // E2
            var e2 = new DeterministicFiniteAutomaton(
                new[]
                {
                        new Transition(s[0], _e[22], s[1]),
                        new Transition(s[1], _e[33], s[0])
                },
                s[0], "E2");

            // E5
            var e5 = new DeterministicFiniteAutomaton(
                new[]
                {
                        new Transition(s[0], _e[36], s[1]),
                        new Transition(s[1], _e[61], s[0])
                },
                s[0], "E5");

            // E6
            var e6 = new DeterministicFiniteAutomaton(
                new[]
                {
                        new Transition(s[0], _e[38], s[1]),
                        new Transition(s[1], _e[63], s[0])
                },
                s[0], "E6");

            // E3
            var e3 = new DeterministicFiniteAutomaton(
                new[]
                {
                        new Transition(s[0], _e[32], s[1]),
                        new Transition(s[1], _e[41], s[0]),
                        new Transition(s[0], _e[42], s[2]),
                        new Transition(s[2], _e[35], s[0])
                },
                s[0], "E3");

            // E7
            var e7 = new DeterministicFiniteAutomaton(
                new[]
                {
                        new Transition(s[0], _e[30], s[1]),
                        new Transition(s[1], _e[71], s[0]),
                        new Transition(s[0], _e[74], s[2]),
                        new Transition(s[2], _e[65], s[0])
                },
                s[0], "E7");

            // E8
            var e8 = new DeterministicFiniteAutomaton(
                new[]
                {
                        new Transition(s[0], _e[72], s[1]),
                        new Transition(s[1], _e[81], s[0]),
                        new Transition(s[0], _e[82], s[2]),
                        new Transition(s[2], _e[73], s[0])
                },
                s[0], "E8");

            // E4
            var e4 = new DeterministicFiniteAutomaton(
                new[]
                {
                        new Transition(s[0], _e[34], s[1]),
                        new Transition(s[1], _e[51], s[0]),
                        new Transition(s[1], _e[53], s[0]),
                        new Transition(s[0], _e[52], s[2]),
                        new Transition(s[2], _e[37], s[0]),
                        new Transition(s[0], _e[54], s[3]),
                        new Transition(s[3], _e[39], s[0])
                },
            s[0], "E4");

            Supervisor = DFA.MonolithicSupervisor(new[] { c1, c2, fresa, torno, robot, mm, c3, mp },
                new[] { e1, e2, e3, e4, e5, e6, e7, e8 }, true);


            // Divisão das tarefas ativas do robô por 7 supervisores modulares locais
            s = Enumerable.Range(0, 6)
                .ToDictionary(i => i, i =>
                new ExpandedState(i.ToString(), i == 0 ? 0u : 1 / 7, i == 0 ? Marking.Marked : Marking.Unmarked));

            robot = new DeterministicFiniteAutomaton(
                new[]
                {
                    new Transition(s[0], _e[31], s[1]),
                    new Transition(s[1], _e[32], s[0]),
                    new Transition(s[0], _e[33], s[2]),
                    new Transition(s[2], _e[34], s[0]),
                    new Transition(s[0], _e[35], s[3]),
                    new Transition(s[3], _e[36], s[0]),
                    new Transition(s[0], _e[37], s[4]),
                    new Transition(s[4], _e[38], s[0]),
                    new Transition(s[0], _e[39], s[5]),
                    new Transition(s[5], _e[30], s[0])
                },
                s[0], "Robot");

            // Divisão das tarefas ativas da Máquian de Montagem por 3 supervisores modulares locais
            s = Enumerable.Range(0, 6)
           .ToDictionary(i => i,
               i => new ExpandedState(i.ToString(), i == 0 ? 0u : 1 / 3, i == 0 ? Marking.Marked : Marking.Unmarked));

            mm = new DeterministicFiniteAutomaton(
                new[]
                {
                    new Transition(s[0], _e[61], s[1]),
                    new Transition(s[1], _e[63], s[2]),
                    new Transition(s[1], _e[65], s[3]),
                    new Transition(s[2], _e[64], s[0]),
                    new Transition(s[3], _e[66], s[0])
                },
                s[0], "MM");

            var e7_e8 = DFA.ParallelComposition(e7, e8);

            Supervisors = DFA.LocalModularSupervisor(new[] { c1, c2, fresa, torno, robot, mm, c3, mp }, new[] { e1, e2, e3, e4, e5, e6, e7_e8 });
        }

        public DFA Supervisor { get; }

        public IEnumerable<DFA> Supervisors { get; }

        public int Depth => 44;

        public (int, int) DepthAB => (19, 25);

        public AbstractState InitialState => Supervisor.InitialState;

        public AbstractState TargetState => Supervisor.InitialState;

        public Restriction InitialRestrition(int products)
        {
            return new Restriction
            {
                {_e[11], (uint) (2*products)},
                {_e[21], (uint) (2*products)},
                {_e[31], (uint) (2*products)},
                {_e[33], (uint) (2*products)},
                {_e[35], (uint) (2*products)},
                {_e[37], (uint) (1*products)},
                {_e[39], (uint) (1*products)},
                {_e[41], (uint) (2*products)},
                {_e[51], (uint) (1*products)},
                {_e[53], (uint) (1*products)},
                {_e[61], (uint) (2*products)},
                {_e[63], (uint) (1*products)},
                {_e[65], (uint) (1*products)},
                {_e[71], (uint) (1*products)},
                {_e[73], (uint) (1*products)},
                {_e[81], (uint) (1*products)}
            };
        }

        public Restriction InitialRestrition((int a, int b) produto)
        {
            var (prod_a, prod_b) = produto;
            return new Restriction
            {
                {_e[11], (uint) (prod_a + prod_b)},
                {_e[21], (uint) (prod_a + prod_b)},
                {_e[31], (uint) (prod_a + prod_b)},
                {_e[33], (uint) (prod_a + prod_b)},
                {_e[35], (uint) (prod_a + prod_b)},
                {_e[37], (uint) prod_a},
                {_e[39], (uint) prod_b},
                {_e[41], (uint) (prod_a + prod_b)},
                {_e[51], (uint) prod_a},
                {_e[53], (uint) prod_b},
                {_e[61], (uint) (prod_a + prod_b)},
                {_e[63], (uint) prod_a},
                {_e[65], (uint) prod_b},
                {_e[71], (uint) prod_b},
                {_e[73], (uint) prod_b},
                {_e[81], (uint) prod_b}
            };
        }

        public Scheduler InitialScheduler =>
                _e.ToDictionary(kvp => kvp.Value as AbstractEvent,
                    kvp => kvp.Value.IsControllable ? 0.0f : float.PositiveInfinity);

        public Update UpdateFunction => (old, ev) =>
        {
            var sch = old.ToDictionary(kvp => kvp.Key, kvp =>
            {
                var v = kvp.Value - old[ev];

                if (kvp.Key.IsControllable) return v < 0 ? 0 : v;
                if (v < 0) return float.NaN;
                return v;
            });

            if (!ev.IsControllable) sch[ev] = float.PositiveInfinity;

            switch (ev.ToString())
            {
                case "11":
                    sch[_e[12]] = 25;
                    break;
                case "21":
                    sch[_e[22]] = 25;
                    break;
                case "31":
                    sch[_e[32]] = 21;
                    break;
                case "33":
                    sch[_e[34]] = 19;
                    break;
                case "35":
                    sch[_e[36]] = 16;
                    break;
                case "37":
                    sch[_e[38]] = 24;
                    break;
                case "39":
                    sch[_e[30]] = 20;
                    break;
                case "41":
                    sch[_e[42]] = 30;
                    break;
                case "51":
                    sch[_e[52]] = 38;
                    break;
                case "53":
                    sch[_e[54]] = 32;
                    break;
                case "61":
                    sch[_e[63]] = 15;
                    sch[_e[65]] = 15;
                    break;
                case "63":
                    sch[_e[64]] = 25;
                    break;
                case "65":
                    sch[_e[66]] = 25;
                    break;
                case "71":
                    sch[_e[72]] = 25;
                    break;
                case "73":
                    sch[_e[74]] = 25;
                    break;
                case "81":
                    sch[_e[82]] = 24;
                    break;
            }
            return sch;
        };

        public StochasticUpdate StochasticUpdateFunction => (old, ev, stdDev) =>
        {
            var sch = old.ToDictionary(kvp => kvp.Key, kvp =>
            {
                var v = kvp.Value - old[ev];


                if (kvp.Key.IsControllable) return v < 0 ? 0 : v;

                if (v < 0)
                {
                    foreach (var s in old)
                    {
                        Console.WriteLine(s.Key + " - " + s.Value);
                    }
                    Console.WriteLine($"evento:{ev} valor de v para o evento {kvp.Key}: {kvp.Value} - {old[ev]} = {v}");

                    return float.NaN;
                }

                return v;
            });

            if (!ev.IsControllable) sch[ev] = float.PositiveInfinity;

            Random rand = new Random();
            var u1 = 1.0 - rand.NextDouble();
            var u2 = 1.0 - rand.NextDouble();
            var stdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);

            var NormalSample = 0 + stdDev * stdNormal;

            switch (ev.ToString())
            {
                case "11":
                    sch[_e[12]] = 25 + (float)NormalSample;
                    break;
                case "21":
                    sch[_e[22]] = 25 + (float)NormalSample;
                    break;
                case "31":
                    sch[_e[32]] = 21 + (float)NormalSample;
                    break;
                case "33":
                    sch[_e[34]] = 19 + (float)NormalSample;
                    break;
                case "35":
                    sch[_e[36]] = 16 + (float)NormalSample;
                    break;
                case "37":
                    sch[_e[38]] = 24 + (float)NormalSample;
                    break;
                case "39":
                    sch[_e[30]] = 20 + (float)NormalSample;
                    break;
                case "41":
                    sch[_e[42]] = 30 + (float)NormalSample;
                    break;
                case "51":
                    sch[_e[52]] = 38 + (float)NormalSample;
                    break;
                case "53":
                    sch[_e[54]] = 32 + (float)NormalSample;
                    break;
                case "61":
                    sch[_e[63]] = 15 + (float)NormalSample;
                    sch[_e[65]] = 15 + (float)NormalSample;
                    break;
                case "63":
                    sch[_e[64]] = 25 + (float)NormalSample;
                    break;
                case "65":
                    sch[_e[66]] = 25 + (float)NormalSample;
                    break;
                case "71":
                    sch[_e[72]] = 25 + (float)NormalSample;
                    break;
                case "73":
                    sch[_e[74]] = 25 + (float)NormalSample;
                    break;
                case "81":
                    sch[_e[82]] = 24 + (float)NormalSample;
                    break;
            }
            return sch;
        };
    }

    public class ITL : ISchedulingProblem
    {
        private readonly Dictionary<int, AbstractEvent> _e;

        public ITL()
        {
            _e = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }.ToDictionary(alias => alias,
                alias => (AbstractEvent)new Event($"{alias}", alias % 2 == 0 ? Controllability.Uncontrollable : Controllability.Controllable));

            var s = new[] { new ExpandedState("I", 0, Marking.Marked), new ExpandedState("W", 1, Marking.Unmarked) };

            var m1 = new DFA(new[] { new Transition(s[0], _e[1], s[1]), new Transition(s[1], _e[2], s[0]) }, s[0], "M1");

            var m2 = new DFA(new[] { new Transition(s[0], _e[3], s[1]), new Transition(s[1], _e[4], s[0]) }, s[0], "M2");

            var m3 = new DFA(new[] { new Transition(s[0], _e[5], s[1]), new Transition(s[1], _e[6], s[0]) }, s[0], "M3");

            var m4 = new DFA(new[] { new Transition(s[0], _e[7], s[1]), new Transition(s[1], _e[8], s[0]) }, s[0], "M4");

            var m5 = new DFA(new[] { new Transition(s[0], _e[9], s[1]), new Transition(s[1], _e[10], s[0]) }, s[0], "M5");

            var m6 = new DFA(new[] { new Transition(s[0], _e[11], s[1]), new Transition(s[1], _e[12], s[0]) }, s[0], "M6");

            s = Enumerable.Range(0, 4)
                .Select(i => new ExpandedState(i.ToString(), 0, i == 0 ? Marking.Marked : Marking.Unmarked)).ToArray();

            var e1 = new DFA(new[] { new Transition(s[0], _e[2], s[1]), new Transition(s[1], _e[3], s[0]) }, s[0], "E1");

            var e2 = new DFA( new[] { new Transition(s[0], _e[6], s[1]), new Transition(s[1], _e[7], s[0]) }, s[0], "E2");

            var e3 = new DFA(new[]{
                    new Transition(s[0], _e[4], s[1]), new Transition(s[1], _e[8], s[2]),
                    new Transition(s[0], _e[8], s[3]), new Transition(s[3], _e[4], s[2]),
                    new Transition(s[2], _e[9], s[0])
                }, s[0], "E3");

            var e4 = new DFA(new[] { new Transition(s[0], _e[10], s[1]), new Transition(s[1], _e[11], s[0]) }, s[0], "E4");

            Supervisor = DFA.MonolithicSupervisor(new[] { m1, m2, m3, m4, m5, m6 }, new[] { e1, e2, e3, e4 }, true);

            Events = _e.Values.ToList();

            Transitions = Supervisor.Transitions.GroupBy(t => t.Origin)
                .ToDictionary(g => g.Key, g => g.ToDictionary(t => t.Trigger, t => t.Destination));

            // Divisão das tarefas ativas das máquina 2, 4 e 5 por 2 supervisores modulares locais
            s = new[] { new ExpandedState("I", 0, Marking.Marked), new ExpandedState("W", 0.5, Marking.Unmarked) };

            m2 = new DFA(new[] { new Transition(s[0], _e[3], s[1]), new Transition(s[1], _e[4], s[0]) }, s[0], "M2");

            m4 = new DFA(new[] { new Transition(s[0], _e[7], s[1]), new Transition(s[1], _e[8], s[0]) }, s[0], "M4");

            m5 = new DFA(new[] { new Transition(s[0], _e[9], s[1]), new Transition(s[1], _e[10], s[0]) }, s[0], "M5");

            Supervisors = DFA.LocalModularSupervisor(new[] { m1, m2, m3, m4, m5, m6 }, new[] { e1, e2, e3, e4 });
        }

        public DFA Supervisor { get; }

        public IEnumerable<DFA> Supervisors { get; }

        public IEnumerable<AbstractEvent> Events { get; }

        public Dictionary<AbstractState, Dictionary<AbstractEvent, AbstractState>> Transitions { get; }

        public int Depth => 12;

        public Scheduler InitialScheduler =>
            _e.ToDictionary(kvp => kvp.Value as AbstractEvent,
                kvp => kvp.Value.IsControllable ? 0.0f : float.PositiveInfinity);

        public AbstractState InitialState => Supervisor.InitialState;

        public AbstractState TargetState => Supervisor.InitialState;

        public Update UpdateFunction => (old, ev) =>
        {
            var sch = old.ToDictionary(kvp => kvp.Key, kvp =>
            {
                if (kvp.Key.IsControllable) return 0;

                var v = kvp.Value - old[ev];

                return v < 0 ? float.PositiveInfinity : v;
            });

            if (!ev.IsControllable) sch[ev] = float.PositiveInfinity;

            switch (ev.ToString())
            {
                case "1":
                    sch[_e[2]] = 25;
                    break;
                case "3":
                    sch[_e[4]] = 25;
                    break;
                case "5":
                    sch[_e[6]] = 38;
                    break;
                case "7":
                    sch[_e[8]] = 21;
                    break;
                case "9":
                    sch[_e[10]] = 19;
                    break;
                case "11":
                    sch[_e[12]] = 24;
                    break;
            }
            return sch;
        };

        public Restriction InitialRestrition(int products)
        {
            return new Restriction
            {
                {_e[01], (uint) (1 * products) },
                {_e[03], (uint) (1 * products) },
                {_e[05], (uint) (1 * products) },
                {_e[07], (uint) (1 * products) },
                {_e[09], (uint) (1 * products) },
                {_e[11], (uint) (1 * products) }
            };
        }
    }

    internal class EZPELETA : ISchedulingProblemABC
    {
        private readonly Dictionary<int, Event> _e = Enumerable.Range(1, 49).ToDictionary(i => i,
                i => new Event($"{i}", i % 2 == 1 ? Controllability.Controllable : Controllability.Uncontrollable));

        public EZPELETA()
        {
            //var s = new Dictionary<int, AbstractState>
            //{
            //    {0, new PowerState("0", 000, Marking.Marked)},
            //    {1, new PowerState("1", 100, Marking.Unmarked)}
            //};

            if (File.Exists("EZPELETA.bin"))
            {
                Console.WriteLine("Carregando supervisor");
                Supervisor = Utilidades.DeserializeAutomaton("EZPELETA.bin");
            }
            else
            {
                Console.WriteLine("Novo Supervisor");
                var s = new[] {
                new ExpandedState("0", 0, Marking.Marked),
                new ExpandedState("1", 1, Marking.Unmarked),
                new ExpandedState("2", 1, Marking.Unmarked),
                new ExpandedState("3", 1, Marking.Unmarked),
                new ExpandedState("4", 1, Marking.Unmarked),
                new ExpandedState("5", 1, Marking.Unmarked)
                };

                //_e = Enumerable.Range(1, 49).ToDictionary(i => i,
                //    i => new Event($"{i}", i % 2 == 1 ? Controllability.Controllable : Controllability.Uncontrollable));

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

                //s = new Dictionary<int, AbstractState>
                //{
                //    {0, new PowerState("0", 000, Marking.Marked)},
                //    {1, new PowerState("1", 500, Marking.Unmarked)}
                //};

                // Maquina 1
                var M1 = new DFA(new[]
                {
                new Transition(s[0], _e[13], s[1]),
                new Transition(s[1], _e[14], s[0])
                }, s[0], "M1");

                //s = new Dictionary<int, AbstractState>
                //{
                //    {0, new PowerState("0", 000, Marking.Marked)},
                //    {1, new PowerState("1", 500, Marking.Unmarked)}, // 500
                //    {2, new PowerState("2", 600, Marking.Unmarked)}
                //};

                // Maquina 2
                var M2 = new DFA(new[]
                {
                new Transition(s[0], _e[15], s[1]),
                new Transition(s[1], _e[16], s[0]),
                new Transition(s[0], _e[17], s[2]),
                new Transition(s[2], _e[18], s[0])
                }, s[0], "M2");

                //s = new Dictionary<int, AbstractState>
                //{
                //    {0, new PowerState("0", 000, Marking.Marked)},
                //    {1, new PowerState("1", 300, Marking.Unmarked)}, //300
                //    {2, new PowerState("2", 400, Marking.Unmarked)}
                //};

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

                //s = new Dictionary<int, AbstractState>
                //{
                //    {0, new PowerState("0", 000, Marking.Marked)},
                //    {1, new PowerState("1", 200, Marking.Unmarked)},
                //    {2, new PowerState("2", 200, Marking.Unmarked)},
                //    {3, new PowerState("3", 200, Marking.Unmarked)},
                //    {4, new PowerState("4", 200, Marking.Unmarked)},
                //    {5, new PowerState("5", 200, Marking.Unmarked)}
                //};

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


                //s = new Dictionary<int, AbstractState>
                //{
                //    {0, new PowerState("0", 000, Marking.Marked)},
                //    {1, new PowerState("1", 000, Marking.Unmarked)},
                //    {2, new PowerState("2", 000, Marking.Unmarked)},
                //    {3, new PowerState("3", 000, Marking.Unmarked)},
                //    {4, new PowerState("4", 000, Marking.Unmarked)},
                //    {5, new PowerState("5", 000, Marking.Unmarked)}
                //};

                // Especificações

                s = new[] {
                new ExpandedState("0", 0, Marking.Marked),
                new ExpandedState("1", 0, Marking.Unmarked),
                new ExpandedState("2", 0, Marking.Unmarked),
                new ExpandedState("3", 0, Marking.Unmarked),
                new ExpandedState("4", 0, Marking.Unmarked),
                new ExpandedState("5", 0, Marking.Unmarked)
                };

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

                try
                {
                    Supervisors = DFA.LocalModularSupervisor(new[] { C1, C2, C3, C4, C5, C6, M1, M2, M3, M4, R1, R2, R3 },
                    new[] { E1, E2, E3, E4, E5, E6, E7, E8, E9, E10 });
                }
                catch (Exception erro) { Console.WriteLine(erro.Message); }

                //var s2int = new Dictionary<AbstractState, int>();
                ////var consumo = new List<double>();
                //var k = 0;

                //var estados = Supervisor.States.ToArray();

                //foreach (var state in estados)
                //{
                //    s2int.Add(state, k++);
                //    //consumo.Add(ConsumoEstado((AbstractCompoundState)state));
                //}

                //Utilidades.Serialize(new[] { s2int[Supervisor.InitialState] }, "initial.bin");

                //var trans = Supervisor.Transitions.ToList();

                //var orig = trans.AsParallel().AsOrdered().WithDegreeOfParallelism(4).Select(t => s2int[t.Origin]).ToArray();
                //Utilidades.SerializeArr(Utilidades.ConverteInt2Byte(orig), "transicoes_ori.bin");
                //var evs = trans.AsParallel().AsOrdered().WithDegreeOfParallelism(4).Select(t => t.Trigger).ToArray();
                //Utilidades.SerializeArr(evs.Select(ev => byte.Parse(ev.ToString())).ToArray(), "transicoes_tri.bin");
                //var dest = trans.AsParallel().AsOrdered().WithDegreeOfParallelism(4).Select(t => s2int[t.Destination]).ToArray();
                //Utilidades.SerializeArr(Utilidades.ConverteInt2Byte(dest), "transicoes_des.bin");

                //Utilidades.Serialize(Utilidades.ConverteInt2Byte(new[] { s2int[Supervisor.InitialState] }), "initial.bin");
                //Utilidades.Serialize(Supervisor.Events.Select(ev => (ev.ToString(), ev.IsControllable)).ToArray(), "eventos.bin");

                //Utilidades.Serialize(consumo, "energia.bin");

                //Supervisors = DFA.LocalModularSupervisor(new[] { m1, m2 }, new[] { e1 });
                Utilidades.SerializeAutomaton(Supervisor, "EZPELETA.bin");
            }
        }

        public int Depth => 38;

        public (int, int, int) DepthABC => (14, 10, 14);

        public Scheduler InitialScheduler =>
            _e.ToDictionary(kvp => kvp.Value as AbstractEvent,
                kvp => kvp.Value.IsControllable ? 0.0f : float.PositiveInfinity);

        public AbstractState InitialState => Supervisor.InitialState;

        public DFA Supervisor { get; }

        public IEnumerable<DFA> Supervisors { get; }

        public AbstractState TargetState => Supervisor.InitialState;

        public Update UpdateFunction => (old, ev) =>
        {
            var sch = old.ToDictionary(kvp => kvp.Key, kvp =>
            {
                if (kvp.Key.IsControllable) return 0;

                var v = kvp.Value - old[ev];

                return v < 0 ? float.PositiveInfinity : v;
            });

            if (!ev.IsControllable) sch[ev] = float.PositiveInfinity;

            switch (ev.ToString())
            {
                case "1":
                    sch[_e[2]] = 25;
                    break; //C1
                case "3":
                    sch[_e[4]] = 25;
                    break; //C2
                case "5":
                    sch[_e[6]] = 38;
                    break; //C3
                case "7":
                    sch[_e[8]] = 21;
                    break; //C4
                case "9":
                    sch[_e[10]] = 19;
                    break; //C4
                case "11":
                    sch[_e[12]] = 24;
                    break; //C6
                case "13":
                    sch[_e[14]] = 100;
                    break; //M1
                case "15":
                    sch[_e[16]] = 100;
                    break; //M2
                case "17":
                    sch[_e[18]] = 70;
                    break; //M2
                case "19":
                    sch[_e[20]] = 75;
                    break; //M3
                case "21":
                    sch[_e[22]] = 80;
                    break; //M3
                case "23":
                    sch[_e[24]] = 200;
                    break; //M4
                case "25":
                    sch[_e[26]] = 160;
                    break; //M4
                case "27":
                    sch[_e[28]] = 15;
                    break; //R1
                case "29":
                    sch[_e[30]] = 15;
                    break; //R1      
                case "31":
                    sch[_e[32]] = 15;
                    break; //R1
                case "33":
                    sch[_e[34]] = 20;
                    break; //R2
                case "35":
                    sch[_e[36]] = 20;
                    break; //R2
                case "37":
                    sch[_e[38]] = 20;
                    break; //R2
                case "39":
                    sch[_e[40]] = 20;
                    break; //R2
                case "41":
                    sch[_e[42]] = 20;
                    break; //R2
                case "43":
                    sch[_e[44]] = 30;
                    break; //R3
                case "45":
                    sch[_e[44]] = 30;
                    break; //R3
                case "47":
                    sch[_e[48]] = 30;
                    break; //R3
            }
            return sch;
        };

        public Restriction InitialRestrition(int products)
        {
            return new Restriction { };
        }

        public Restriction InitialRestrition((int a, int b, int c) produto)
        {
            var (pA, pB, pC) = produto;
            return new Restriction //23 eventos
            {
                {_e[1], (uint) (1 * pA + 0 * pB + 0 * pC) },
                {_e[3], (uint) (0 * pA + 1 * pB + 0 * pC) },
                {_e[5], (uint) (0 * pA + 0 * pB + 1 * pC) },
                {_e[7], (uint) (0 * pA + 0 * pB + 1 * pC) },
                {_e[9], (uint) (0 * pA + 1 * pB + 0 * pC) },
                {_e[11], (uint) (1 * pA + 0 * pB + 0 * pC) },
                {_e[13], (uint) (1 * pA + 0 * pB + 0 * pC) },
                {_e[15], (uint) (1 * pA + 0 * pB + 0 * pC) },
                {_e[17], (uint) (0 * pA + 1 * pB + 0 * pC) },
                {_e[19], (uint) (1 * pA + 0 * pB + 0 * pC) },
                {_e[21], (uint) (0 * pA + 0 * pB + 1 * pC) },
                {_e[23], (uint) (1 * pA + 0 * pB + 0 * pC) },
                {_e[25], (uint) (0 * pA + 0 * pB + 1 * pC) },
                {_e[27], (uint) (1 * pA + 0 * pB + 0 * pC) },
                {_e[29], (uint) (1 * pA + 0 * pB + 0 * pC) },
                {_e[31], (uint) (0 * pA + 0 * pB + 1 * pC) },
                {_e[33], (uint) (0 * pA + 1 * pB + 0 * pC) },
                {_e[35], (uint) (0 * pA + 1 * pB + 0 * pC) },
                {_e[37], (uint) (0 * pA + 0 * pB + 1 * pC) },
                {_e[39], (uint) (1 * pA + 0 * pB + 0 * pC) },
                {_e[41], (uint) (1 * pA + 0 * pB + 0 * pC) },
                {_e[43], (uint) (1 * pA + 0 * pB + 0 * pC) },
                {_e[45], (uint) (1 * pA + 0 * pB + 0 * pC) },
                {_e[47], (uint) (0 * pA + 0 * pB + 1 * pC) }
            };
        }
    }

    internal class CT1R : ISchedulingProblem
    {
        public Dictionary<string, Event> e;

        // Cluster Tool radial com 1 robô e 4 câmaras de processamento
        public CT1R()
        {
            try
            {
                var e_c = new[] {
                    "r1_a1", "r1_a2", "r1_a3", "r1_a4", "r1_a5",
                    "m1_a1", "m2_a1", "m3_a1", "m4_a1"
                    }.Select(alias => (alias, new Event(alias.ToString(), Controllability.Controllable)));

                var e_uc = new[] {
                    "r1_b1", "r1_b2", "r1_b3", "r1_b4", "r1_b5",
                    "m1_b1", "m2_b1", "m3_b1", "m4_b1"
                    }.Select(alias => (alias, new Event(alias.ToString(), Controllability.Uncontrollable)));

                e = e_c.Concat(e_uc).ToDictionary(t => t.alias, t => t.Item2);

                var s = Enumerable.Range(0, 6).ToDictionary(i => i, i => new ExpandedState(i.ToString(), i == 0 ? 0u : 1u, i == 0 ? Marking.Marked : Marking.Unmarked));

                // Plantas

                var m1 = new DFA(new[]
                {
                    new Transition(s[0], e["m1_a1"], s[1]),
                    new Transition(s[1], e["m1_b1"], s[0])
                }, s[0], "M1");

                var m2 = new DFA(new[]
                {
                    new Transition(s[0], e["m2_a1"], s[1]),
                    new Transition(s[1], e["m2_b1"], s[0])
                }, s[0], "M2");

                var m3 = new DFA(new[]
                {
                    new Transition(s[0], e["m3_a1"], s[1]),
                    new Transition(s[1], e["m3_b1"], s[0])
                }, s[0], "M3");

                var m4 = new DFA(new[]
                {
                    new Transition(s[0], e["m4_a1"], s[1]),
                    new Transition(s[1], e["m4_b1"], s[0])
                }, s[0], "M4");

                var r1 = new DFA(new[]
                {
                    new Transition(s[0], e["r1_a1"], s[1]),
                    new Transition(s[1], e["r1_b1"], s[0]),
                    new Transition(s[0], e["r1_a2"], s[2]),
                    new Transition(s[2], e["r1_b2"], s[0]),
                    new Transition(s[0], e["r1_a3"], s[3]),
                    new Transition(s[3], e["r1_b3"], s[0]),
                    new Transition(s[0], e["r1_a4"], s[4]),
                    new Transition(s[4], e["r1_b4"], s[0]),
                    new Transition(s[0], e["r1_a5"], s[5]),
                    new Transition(s[5], e["r1_b5"], s[0])
                }, s[0], "R1");

                // Especificações

                s = Enumerable.Range(0, 6).ToDictionary(i => i, i => new ExpandedState(i.ToString(), 0u, i == 0 ? Marking.Marked : Marking.Unmarked));

                var e1 = new DFA(new[]
                {
                    new Transition(s[0], e["r1_b1"], s[1]),
                    new Transition(s[1], e["m1_a1"], s[0]),
                    new Transition(s[0], e["m1_b1"], s[2]),
                    new Transition(s[2], e["r1_a2"], s[0])
                }, s[0], "E1");

                var e2 = new DFA(new[]
                {
                    new Transition(s[0], e["r1_b2"], s[1]),
                    new Transition(s[1], e["m2_a1"], s[0]),
                    new Transition(s[0], e["m2_b1"], s[2]),
                    new Transition(s[2], e["r1_a3"], s[0])
                }, s[0], "E2");

                var e3 = new DFA(new[]
                {
                    new Transition(s[0], e["r1_b3"], s[1]),
                    new Transition(s[1], e["m3_a1"], s[0]),
                    new Transition(s[0], e["m3_b1"], s[2]),
                    new Transition(s[2], e["r1_a4"], s[0])
                }, s[0], "E3");

                var e4 = new DFA(new[]
                {
                    new Transition(s[0], e["r1_b4"], s[1]),
                    new Transition(s[1], e["m4_a1"], s[0]),
                    new Transition(s[0], e["m4_b1"], s[2]),
                    new Transition(s[2], e["r1_a5"], s[0])
                }, s[0], "E4");

                Supervisor = DFA.MonolithicSupervisor(new[] { m1, m2, m3, m4, r1 }, new[] { e1, e2, e3, e4 }, true);

                // Divisão das tarefas ativas do robô por 4 supervisores modulares locais
                s = Enumerable.Range(0, 6).ToDictionary(i => i, i => new ExpandedState(i.ToString(), i == 0 ? 0 : 1 / 4.0, i == 0 ? Marking.Marked : Marking.Unmarked));

                r1 = new DFA(new[]
                {
                    new Transition(s[0], e["r1_a1"], s[1]),
                    new Transition(s[1], e["r1_b1"], s[0]),
                    new Transition(s[0], e["r1_a2"], s[2]),
                    new Transition(s[2], e["r1_b2"], s[0]),
                    new Transition(s[0], e["r1_a3"], s[3]),
                    new Transition(s[3], e["r1_b3"], s[0]),
                    new Transition(s[0], e["r1_a4"], s[4]),
                    new Transition(s[4], e["r1_b4"], s[0]),
                    new Transition(s[0], e["r1_a5"], s[5]),
                    new Transition(s[5], e["r1_b5"], s[0])
                }, s[0], "R1");

                Supervisors = DFA.LocalModularSupervisor(new[] { m1, m2, m3, m4, r1 }, new[] { e1, e2, e3, e4 });
            }
            catch (Exception erro) { Console.WriteLine(erro.Message); }
        }

        public DFA Supervisor { get; }

        public IEnumerable<DFA> Supervisors { get; }

        public int Depth => 18;

        public AbstractState InitialState => Supervisor.InitialState;

        public AbstractState TargetState => Supervisor.InitialState;

        public Restriction InitialRestrition(int products)
        {
            return e.Values.Where(ev => ev.IsControllable).ToDictionary(a => (AbstractEvent)a, a => (uint)products);
        }

        public Scheduler InitialScheduler =>
                e.Values.ToDictionary(alias => alias as AbstractEvent,
                    alias => alias.IsControllable ? 0.0f : float.PositiveInfinity);

        public Update UpdateFunction => (old, ev) =>
        {
            var sch = old.ToDictionary(kvp => kvp.Key, kvp =>
            {
                var v = kvp.Value - old[ev];

                if (kvp.Key.IsControllable) return v < 0 ? 0 : v;
                if (v < 0) return float.NaN;
                return v;
            });

            if (!ev.IsControllable) sch[ev] = float.PositiveInfinity;

            switch (ev.ToString())
            {
                case "r1_a1":
                    sch[e["r1_b1"]] = 10;
                    break;
                case "r1_a2":
                    sch[e["r1_b2"]] = 10;
                    break;
                case "r1_a3":
                    sch[e["r1_b3"]] = 10;
                    break;
                case "r1_a4":
                    sch[e["r1_b4"]] = 10;
                    break;
                case "r1_a5":
                    sch[e["r1_b5"]] = 10;
                    break;
                case "m1_a1":
                    sch[e["m1_b1"]] = 5;
                    break;
                case "m2_a1":
                    sch[e["m2_b1"]] = 5;
                    break;
                case "m3_a1":
                    sch[e["m3_b1"]] = 5;
                    break;
                case "m4_a1":
                    sch[e["m4_b1"]] = 5;
                    break;
            }
            return sch;
        };
    }

    internal class CT2R : ISchedulingProblem
    {
        public Dictionary<string, Event> e;

        // Cluster Tool radial com 2 robôs e 4 câmaras de processamento
        public CT2R()
        {
            try
            {
                var e_c = new[] {
                    "r1_a1", "r1_a2", "r1_a3", "r1_a4",
                    "r2_a1", "r2_a2", "r2_a3",
                    "m1_a1", "m2_a1", "m3_a1", "m4_a1"
                    }.Select(alias => (alias, new Event(alias.ToString(), Controllability.Controllable)));

                var e_uc = new[] {
                    "r1_b1", "r1_b2", "r1_b3", "r1_b4",
                    "r2_b1", "r2_b2", "r2_b3",
                    "m1_b1", "m2_b1", "m3_b1", "m4_b1"
                    }.Select(alias => (alias, new Event(alias.ToString(), Controllability.Uncontrollable)));

                e = e_c.Concat(e_uc).ToDictionary(t => t.alias, t => t.Item2);

                var s = Enumerable.Range(0, 6).ToDictionary(i => i, i => new ExpandedState(i.ToString(), i == 0 ? 0u : 1u, i == 0 ? Marking.Marked : Marking.Unmarked));

                // Plantas

                var m1 = new DFA(new[]
                {
                    new Transition(s[0], e["m1_a1"], s[1]),
                    new Transition(s[1], e["m1_b1"], s[0])
                }, s[0], "M1");

                var m2 = new DFA(new[]
                {
                    new Transition(s[0], e["m2_a1"], s[1]),
                    new Transition(s[1], e["m2_b1"], s[0])
                }, s[0], "M2");

                var m3 = new DFA(new[]
                {
                    new Transition(s[0], e["m3_a1"], s[1]),
                    new Transition(s[1], e["m3_b1"], s[0])
                }, s[0], "M3");

                var m4 = new DFA(new[]
                {
                    new Transition(s[0], e["m4_a1"], s[1]),
                    new Transition(s[1], e["m4_b1"], s[0])
                }, s[0], "M4");

                var r1 = new DFA(new[]
                {
                    new Transition(s[0], e["r1_a1"], s[1]),
                    new Transition(s[1], e["r1_b1"], s[0]),
                    new Transition(s[0], e["r1_a2"], s[2]),
                    new Transition(s[2], e["r1_b2"], s[0]),
                    new Transition(s[0], e["r1_a3"], s[3]),
                    new Transition(s[3], e["r1_b3"], s[0]),
                    new Transition(s[0], e["r1_a4"], s[4]),
                    new Transition(s[4], e["r1_b4"], s[0])
                }, s[0], "R1");

                var r2 = new DFA(new[]
                {
                    new Transition(s[0], e["r2_a1"], s[1]),
                    new Transition(s[1], e["r2_b1"], s[0]),
                    new Transition(s[0], e["r2_a2"], s[2]),
                    new Transition(s[2], e["r2_b2"], s[0]),
                    new Transition(s[0], e["r2_a3"], s[3]),
                    new Transition(s[3], e["r2_b3"], s[0])
                }, s[0], "R2");

                // Especificações

                s = Enumerable.Range(0, 6).ToDictionary(i => i, i => new ExpandedState(i.ToString(), 0u, i == 0 ? Marking.Marked : Marking.Unmarked));

                var e1 = new DFA(new[]
                {
                    new Transition(s[0], e["r1_b1"], s[1]),
                    new Transition(s[1], e["m1_a1"], s[0]),
                    new Transition(s[0], e["m1_b1"], s[2]),
                    new Transition(s[2], e["r1_a2"], s[0])
                }, s[0], "E1");

                var e2 = new DFA(new[]
                {
                    new Transition(s[0], e["r1_b2"], s[1]),
                    new Transition(s[1], e["r2_a1"], s[0]),
                    new Transition(s[0], e["r2_b3"], s[2]),
                    new Transition(s[2], e["r1_a3"], s[0])
                }, s[0], "E2");

                var e3 = new DFA(new[]
                {
                    new Transition(s[0], e["r2_b1"], s[1]),
                    new Transition(s[1], e["m2_a1"], s[0]),
                    new Transition(s[0], e["m2_b1"], s[2]),
                    new Transition(s[2], e["r2_a2"], s[0])
                }, s[0], "E3");

                var e4 = new DFA(new[]
                {
                    new Transition(s[0], e["r2_b2"], s[1]),
                    new Transition(s[1], e["m3_a1"], s[0]),
                    new Transition(s[0], e["m3_b1"], s[2]),
                    new Transition(s[2], e["r2_a3"], s[0])
                }, s[0], "E4");

                var e5 = new DFA(new[]
                {
                    new Transition(s[0], e["r1_b3"], s[1]),
                    new Transition(s[1], e["m4_a1"], s[0]),
                    new Transition(s[0], e["m4_b1"], s[2]),
                    new Transition(s[2], e["r1_a4"], s[0])
                }, s[0], "E5");

                //foreach (var a in new[] { m1, m2, m3, m4, r1, r2, e1, e2, e3, e4, e5 })
                //    a.showAutomaton();

                var e234 = DFA.ParallelComposition(e2, e3, e4);


                Supervisor = DFA.MonolithicSupervisor(new[] { m1, m2, m3, m4, r1, r2 }, new[] { e1, e2, e3, e4, e5 }, true);

                // Divisão das tarefas ativas do robô por 3 supervisores modulares locais
                s = Enumerable.Range(0, 6).ToDictionary(i => i, i => new ExpandedState(i.ToString(), i == 0 ? 0 : 1 / 3.0, i == 0 ? Marking.Marked : Marking.Unmarked));

                r1 = new DFA(new[]
                {
                    new Transition(s[0], e["r1_a1"], s[1]),
                    new Transition(s[1], e["r1_b1"], s[0]),
                    new Transition(s[0], e["r1_a2"], s[2]),
                    new Transition(s[2], e["r1_b2"], s[0]),
                    new Transition(s[0], e["r1_a3"], s[3]),
                    new Transition(s[3], e["r1_b3"], s[0]),
                    new Transition(s[0], e["r1_a4"], s[4]),
                    new Transition(s[4], e["r1_b4"], s[0])
                }, s[0], "R1");

                try { Supervisors = DFA.LocalModularSupervisor(new[] { m1, m2, m3, m4, r1, r2 }, new[] { e1, e234, e5 }); }
                catch (Exception erro) { Console.WriteLine(erro.Message); }
            }
            catch (Exception erro)
            {
                Console.WriteLine("Erro 1: " + erro.Message);
            }
        }

        public DFA Supervisor { get; }

        public IEnumerable<DFA> Supervisors { get; }

        public int Depth => 22;

        public AbstractState InitialState => Supervisor.InitialState;

        public AbstractState TargetState => Supervisor.InitialState;

        public Restriction InitialRestrition(int products)
        {
            return e.Values.ToDictionary(a => (AbstractEvent)a, a => (uint)products);
        }

        public Scheduler InitialScheduler =>
                e.Values.ToDictionary(alias => alias as AbstractEvent,
                    alias => alias.IsControllable ? 0.0f : float.PositiveInfinity);

        public Update UpdateFunction => (old, ev) =>
        {
            var sch = old.ToDictionary(kvp => kvp.Key, kvp =>
            {
                var v = kvp.Value - old[ev];

                if (kvp.Key.IsControllable) return v < 0 ? 0 : v;
                if (v < 0) return float.NaN;
                return v;
            });

            if (!ev.IsControllable) sch[ev] = float.PositiveInfinity;

            switch (ev.ToString())
            {
                case "r1_a1":
                    sch[e["r1_b1"]] = 10;
                    break;
                case "r1_a2":
                    sch[e["r1_b2"]] = 10;
                    break;
                case "r1_a3":
                    sch[e["r1_b3"]] = 10;
                    break;
                case "r1_a4":
                    sch[e["r1_b4"]] = 10;
                    break;
                case "r2_a1":
                    sch[e["r2_b1"]] = 10;
                    break;
                case "r2_a2":
                    sch[e["r2_b2"]] = 10;
                    break;
                case "r2_a3":
                    sch[e["r2_b3"]] = 10;
                    break;
                case "m1_a1":
                    sch[e["m1_b1"]] = 5;
                    break;
                case "m2_a1":
                    sch[e["m2_b1"]] = 5;
                    break;
                case "m3_a1":
                    sch[e["m3_b1"]] = 5;
                    break;
                case "m4_a1":
                    sch[e["m4_b1"]] = 5;
                    break;
            }
            return sch;
        };
    }

    internal static class Utilidades
    {
        public static void SerializeAutomaton(DeterministicFiniteAutomaton G, string path)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, G);
            stream.Close();
        }

        public static DFA DeserializeAutomaton(string path)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var obj = formatter.Deserialize(stream) as DFA;
            stream.Close();
            return obj;
        }
    }

    public sealed class SinglyLinkedList<T> : IEnumerable<T>
    {
        readonly T _head;
        readonly SinglyLinkedList<T> _tail;

        private SinglyLinkedList()
        {
            IsEmpty = true;
        }

        private SinglyLinkedList(T head)
        {
            IsEmpty = false;
            _head = head;
        }

        private SinglyLinkedList(T head, SinglyLinkedList<T> tail)
        {
            IsEmpty = false;
            _head = head;
            _tail = tail;
        }

        public static SinglyLinkedList<T> Empty { get; } = new SinglyLinkedList<T>();

        public int Count
        {
            get
            {
                var list = this;
                var count = 0;
                while (!list.IsEmpty)
                {
                    count++;
                    list = list._tail;
                }
                return count;
            }
        }

        public bool IsEmpty { get; }

        public T Head
        {
            get
            {
                if (IsEmpty)
                    throw new InvalidOperationException("The list is empty.");
                return _head;
            }
        }

        public SinglyLinkedList<T> Tail
        {
            get
            {
                if (_tail == null)
                    throw new InvalidOperationException("This list has no tail.");
                return _tail;
            }
        }

        public static SinglyLinkedList<T> FromEnumerable(IEnumerable<T> e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            return FromArrayInternal(e.ToArray());
        }

        public static SinglyLinkedList<T> FromArray(params T[] a)
        {
            if (a == null)
                throw new ArgumentNullException(nameof(a));
            return FromArrayInternal(a);
        }

        public SinglyLinkedList<T> Append(T value)
        {
            var array = new T[Count + 1];
            var list = this;
            var index = 0;
            while (!list.IsEmpty)
            {
                array[index++] = list._head;
                list = list._tail;
            }
            array[index] = value;
            return FromArrayInternal(array);
        }

        public SinglyLinkedList<T> Prepend(T value)
        {
            return new SinglyLinkedList<T>(value, this);
        }

        public SinglyLinkedList<T> Insert(int index, T value)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), "Cannot be less than zero.");
            var count = Count;
            if (index >= count)
                throw new ArgumentOutOfRangeException(nameof(index), "Cannot be greater than count.");
            var array = new T[Count + 1];
            var list = this;
            var arrayIndex = 0;
            while (!list.IsEmpty)
            {
                if (arrayIndex == index)
                {
                    array[arrayIndex++] = value;
                }
                array[arrayIndex++] = list._head;
                list = list._tail;
            }
            return FromArrayInternal(array);
        }

        public IEnumerator<T> GetEnumerator()
        {
            var list = this;
            while (!list.IsEmpty)
            {
                yield return list._head;
                list = list._tail;
            }
        }

        public override string ToString()
        {
            return IsEmpty ? "[]" : $"[{_head}...]";
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private static SinglyLinkedList<T> FromArrayInternal(IReadOnlyList<T> a)
        {
            var result = Empty;
            for (var i = a.Count - 1; i >= 0; i--)
            {
                result = result.Prepend(a[i]);
            }
            return result;
        }
    }
}
