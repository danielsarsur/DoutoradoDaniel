using PlanningDES;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using UltraDES;
//using Restriction = System.Collections.Generic.Dictionary<UltraDES.AbstractEvent, uint>;
//using Scheduler = System.Collections.Generic.Dictionary<UltraDES.AbstractEvent, float>;
//using Update = System.Func<System.Collections.Generic.Dictionary<UltraDES.AbstractEvent, float>, UltraDES.AbstractEvent, System.Collections.Generic.Dictionary<UltraDES.AbstractEvent, float>>;
using Restriction = PlanningDES.Restriction;
using Scheduler = PlanningDES.Scheduler;
using Update = System.Func<System.Collections.Generic.Dictionary<UltraDES.AbstractEvent, float>, UltraDES.AbstractEvent, System.Collections.Generic.Dictionary<UltraDES.AbstractEvent, float>>;



namespace ProgramaDaniel
{
    public static class Tools
    {
        public static Tuple<List<AbstractEvent>, List<float>> Sequence(PlanningDES.ISchedulingProblem problem, int products, Dictionary<AbstractState, List<AbstractEvent>> PI)
        {
            var state = problem.InitialState;
            var destination = problem.TargetState;
            var depth = products * problem.Depth;
            var res = problem.InitialRestrition(products);
            var sch = problem.InitialScheduler;
            var transitions = problem.Transitions;
            var uncontrollables = problem.Events.Where(e => !e.IsControllable).ToSet();

            AbstractEvent e = null;
            AbstractState dest = null;
            var events = new List<AbstractEvent>();
            var times = new List<float>();

            for (var it = 0; it < depth; it++)
            {
                //Console.Write(it+1 + " - ");

                foreach (var pi in PI[state])
                {
                    if (!sch.Internal.Where(sc => !sc.Key.IsControllable && sc.Value < sch[pi]).Any()) // Trata infactibilidades temporais
                    {
                        try
                        {
                            (e, dest) = transitions[state].Where(
                                    t => (t.Key.IsControllable && res[t.Key] > 0) || !t.Key.IsControllable)
                                    .Single(t => t.Key == pi);
                            break;
                        }
                        catch { }
                    }
                }

                events.Add(e);
                if (e.IsControllable) res = res.Update(e);
                sch = sch.Update(e);
                times.Add(sch.ElapsedTime);
                state = dest;
            }
            if (state != destination) times.Add(float.PositiveInfinity);

            return Tuple.Create(events, times);
        }

        public static Tuple<List<AbstractEvent>, List<float>> SequenceModular(PlanningDES.ISchedulingProblem problem, int products,
            List<(Dictionary<AbstractState, Dictionary<AbstractEvent, AbstractState>> transitions, Dictionary<AbstractState, List<(AbstractEvent, double)>> PI,
            AbstractState state)> supervisors, HashSet<AbstractEvent> all_events)
        {
            var initial_state = supervisors.Select(s => s.state);
            //var final_state = problem.TargetState;
            var depth = products * problem.Depth;
            var res = problem.InitialRestrition(products);
            var sch = problem.InitialScheduler;
            //var transitions = problem.Transitions;
            var uncontrollables = problem.Events.Where(e => !e.IsControllable).ToSet();

            //res = new Restriction(res);

            //var initial_state_const = supervisors.Select(s => s.state);
            //var initial_state = supervisors.Select(s => s.state);
            //var initial_state = supervisors.Select(s => s.state.Clone()).ToList();
            var events = new List<AbstractEvent>();

            var tempo = 0.0f;
            var times = new List<float>();

            int c51 = 0;
            int c53 = 0;
            var e51 = new Event("51", Controllability.Controllable);
            var e53 = new Event("53", Controllability.Controllable);
            var e63 = new Event("63", Controllability.Controllable);
            var e65 = new Event("65", Controllability.Controllable);

            for (var it = 0; it < depth; it++)
            {
                //Console.Write(it+1 + " - ");


                AbstractEvent e = null;
                var enabled_events = new HashSet<AbstractEvent>(all_events);
                var sumByEvent = new Dictionary<AbstractEvent, double>();

                // Encontra a interseção dos eventos habilitados em cada supervisor
                foreach (var sup in supervisors)
                    enabled_events = enabled_events.Intersect(sup.transitions[sup.state].Select(t => t.Key)).ToSet();

                // Soma os valores das políticas de cada supervisor para cada evento
                foreach (var (transitions, policy, state) in supervisors)
                    if (policy.TryGetValue(state, out var ev))
                        foreach (var (abstractEvent, value) in ev)
                            if (sumByEvent.TryGetValue(abstractEvent, out var somaAtual))
                                sumByEvent[abstractEvent] = somaAtual + value;
                            else
                                sumByEvent.Add(abstractEvent, value);

                var ordered_events = sumByEvent
                    .Where(kv => enabled_events.Contains(kv.Key))
                    .ToDictionary(kv => kv.Key, kv => kv.Value)
                    .OrderByDescending(v => v.Value)
                    .ThenByDescending(k => k.Key.ToString()).ToList(); // Última ordenação necessária para bater com o monolítico. No caso do SFM, deve ser ThenByDescending. No caso
                                                                       // do Cluster Tool, deve ser thenBy.


                // Se só tem eventos não controláveis
                if (!ordered_events.Select(c => c.Key).Where(cc => cc.IsControllable).Any())
                {
                    var min = sch.Internal
                        .Where(kvp => !float.IsInfinity(kvp.Value) && !kvp.Key.IsControllable)
                        .Select(kvp => kvp.Value).Append(float.PositiveInfinity)
                        .Min();

                    var enable = sch.Internal
                        .Where(kvp => kvp.Value <= min && !float.IsInfinity(kvp.Value))
                        .Select(kvp => kvp.Key).ToSet();

                    enable.IntersectWith(ordered_events.Select(c => c.Key).ToSet());

                    e = enable.OrderByDescending(ev => ev.ToString()).First();
                }
                // Se tem algum evento controlável
                else
                {
                    // Para cada evento habilitado
                    foreach (var ev in ordered_events.Select(ev => ev.Key))
                    {
                        if (!sch.Internal.Where(sc => !sc.Key.IsControllable && sc.Value < sch[ev]).Any() &&
                            (!(ev == e63 && sch[e63] > 0 || ev == e65 && sch[e65] > 0) || res.Internal.Select(r => (int)r.Value).ToArray().Sum() == 1)
                            )// Trata infactibilidades temporais
                        {
                            // Se o evento for controlável e ainda estiver disponível em restriction
                            if (ev.IsControllable && res[ev] > 0)
                            {
                                // Se não for o evento 53 - para produção alternando 51 e 53 
                                if (ev != e53)
                                {
                                    e = ev;
                                    break;
                                }
                                // Se for o evento 53, se a quantidade de eventos 51 forem iguais a quantidade de eventos 53
                                else if (c51 == c53)
                                {
                                    e = ev;
                                    break;
                                }
                                else
                                    e = ev;
                            }
                            else
                                e = ev;
                        }
                    }
                }

                if (e == e51)
                    c51++;
                if (e == e53)
                    c53++;
                events.Add(e);

                if (e.IsControllable) res = res.Update(e);
                sch = sch.Update(e);
                times.Add(sch.ElapsedTime);

                // Atualiza os estados atuais de todos os supervisores
                for (int i = 0; i < supervisors.Count; i++)
                {
                    var sup = supervisors[i];
                    sup.state = supervisors[i].transitions[supervisors[i].state].Where(ev => ev.Key == e).Single().Value;
                    supervisors[i] = sup;
                }

                if (sch.Internal.Any(kvp => kvp.Value == Double.NaN)) throw new Exception("Incorrect sequence");
            }

            var final_state = supervisors.Select(s => s.state);

            foreach (var states in initial_state.Zip(final_state, (i, f) => (i, f)))
                if (states.i != states.f)
                {
                    times.Add(float.PositiveInfinity);
                    Console.WriteLine("Incorrect sequence");
                    break;
                }

            return Tuple.Create(events, times);
        }

        //public static List<AbstractEvent> SequenceWithControllables(AbstractState initial, AbstractState destination, int depth,
        //            Restriction res, Dictionary<AbstractState, Transition[]> transitions, Dictionary<AbstractState, List<AbstractEvent>> PI)
        //{
        //    res = new Restriction(res);

        //    var state = initial;
        //    var events = new List<AbstractEvent>();

        //    for (var it = 0; it < depth; it++)
        //    {
        //        Transition trans = null;

        //        foreach (var pi in PI[state])
        //        {
        //            if (res[pi] > 0)
        //            {
        //                try
        //                {
        //                    trans = transitions[state].Single(t => t.Trigger == pi);
        //                    break;
        //                }
        //                catch { }
        //            }
        //        }

        //        var e = trans.Trigger;
        //        events.Add(e);
        //        if (e.IsControllable) res[e]--;
        //        state = trans.Destination;
        //    }
        //    if (state != destination) Console.WriteLine("Não chegou no estado marcado"); ;

        //    return events;
        //}

        //public static Tuple<List<AbstractEvent>, List<float>> SequenceOccupancyRate(AbstractState initial, AbstractState destination, Scheduler sch, int depth,
        //            Update update, Restriction res, Dictionary<AbstractState, Transition[]> transitions, Dictionary<AbstractState, List<AbstractEvent>> PI)
        //{
        //    double robot = 0;
        //    double occupancy = 0;

        //    res = new Restriction(res);

        //    var state = initial;
        //    var events = new List<AbstractEvent>();

        //    var tempo = 0.0f;
        //    var time = new List<float>();

        //    for (var it = 0; it < depth; it++)
        //    {
        //        Transition trans = null;
        //        foreach (var pi in PI[state])
        //        {
        //            if (!sch.Where(sc => !sc.Key.IsControllable && sc.Value < sch[pi]).Any()) // Trata infactibilidades temporais
        //            {
        //                try
        //                {
        //                    if (occupancy > 0.7)
        //                    {
        //                        trans = transitions[state]
        //                            .Where(t => (t.Trigger.IsControllable && res[t.Trigger] > 0) || !t.Trigger.IsControllable)
        //                            .Where(t => t.Trigger.ToString() != "31" && t.Trigger.ToString() != "33" && t.Trigger.ToString() != "35" &&
        //                            t.Trigger.ToString() != "37" && t.Trigger.ToString() != "39")
        //                            .Single(t => t.Trigger == pi);
        //                        break;
        //                    }
        //                    else
        //                    {
        //                        trans = transitions[state]
        //                            .Where(t => (t.Trigger.IsControllable && res[t.Trigger] > 0) || !t.Trigger.IsControllable)
        //                            .Single(t => t.Trigger == pi);
        //                        break;
        //                    }
        //                }
        //                catch { }
        //            }
        //        }

        //        // Caso não tenha mais nenhum evento possível que não seja do robô, espera o tempo e executa algum evento do robô
        //        if (trans == null)
        //        {
        //            trans = transitions[state]
        //                .Where(t => (t.Trigger.IsControllable && res[t.Trigger] > 0) || !t.Trigger.IsControllable)
        //                .Single(t => t.Trigger == PI[state].First());
        //            tempo = (float)(robot / 0.7);
        //        }

        //        var e = trans.Trigger;
        //        events.Add(e);
        //        if (e.IsControllable) res[e]--;
        //        tempo += sch[e];
        //        time.Add(tempo);
        //        sch = update(sch, e);
        //        robot += GetRobotTime(e);
        //        occupancy = robot / time.Last();

        //        state = trans.Destination;

        //        if (sch.Any(kvp => kvp.Value == Double.NaN)) throw new Exception("Incorrect sequence");
        //    }
        //    if (state != destination) time.Add(float.PositiveInfinity);

        //    return Tuple.Create(events, time);
        //}

        //public static float GetRobotTime(AbstractEvent e)
        //{
        //    switch (e.ToString())
        //    {
        //        case "31":
        //            return 21;
        //        case "33":
        //            return 19;
        //        case "35":
        //            return 16;
        //        case "37":
        //            return 24;
        //        case "39":
        //            return 20;
        //        default: return 0;
        //    }
        //}

        //public static void ThroughputEvaluation(List<AbstractEvent> seq, List<float> time)
        //{
        //    var seq_time = seq.Zip(time, (e, t) => (e, t)).ToList();

        //    var sequencia = seq_time.Where(kvp => kvp.e.ToString() == "11" || kvp.e.ToString() == "21" || kvp.e.ToString() == "64" || kvp.e.ToString() == "66");

        //    List<float> start = new List<float> { };
        //    int count11 = 0;
        //    int count21 = 0;

        //    foreach (var item in sequencia)
        //    {
        //        var evento = item.e;
        //        var tempo = item.t;

        //        if (evento.ToString() == "11") count11++;
        //        if (evento.ToString() == "21") count21++;

        //        if (evento.ToString() == "11" & count11 >= count21)
        //            start.Add(tempo);

        //        if (evento.ToString() == "21" & count11 >= count21)
        //            start.Add(tempo);

        //        if (evento.ToString() == "64")
        //        {
        //            Console.WriteLine($"Produto A - Início: {start.First()} - Fim: {tempo} - Tempo: {tempo - start.First()}");
        //            start.RemoveRange(0, 1);
        //        }

        //        if (evento.ToString() == "66")
        //        {
        //            Console.WriteLine($"Produto B - Início: {start.First()} - Fim: {tempo} - Tempo: {tempo - start.First()}");
        //            start.RemoveRange(0, 1);
        //        }
        //    }
        //}

        //public static float TimeEvaluationControllable(ISchedulingProblem problem, List<AbstractEvent> sequence, AbstractState target = null)
        //{
        //    float time = 0;

        //    target = target ?? problem.TargetState;
        //    var state = problem.InitialState;
        //    var transitions = problem.Supervisor.Transitions.GroupBy(t => t.Origin)
        //        .ToDictionary(g => g.Key, g => g.ToDictionary(t => t.Trigger, t => t.Destination));
        //    var events = new List<AbstractEvent>();
        //    var sch = problem.InitialScheduler;
        //    var k = 0;

        //    while (true)
        //    {
        //        var e63 = new Event("63", Controllability.Controllable);
        //        var e65 = new Event("65", Controllability.Controllable);
        //        Transition trans = null;

        //        if (k < sequence.Count())
        //        {
        //            if (transitions[state].ContainsKey(sequence[k]))
        //            {
        //                var min = sch.Where(kvp => !float.IsInfinity(kvp.Value) && !kvp.Key.IsControllable)
        //                    .Select(kvp => kvp.Value).Append(float.PositiveInfinity).Min();
        //                if (sch[e63] > min) { }
        //                else
        //                {
        //                    trans = new Transition(state, sequence[k], transitions[state][sequence[k]]);
        //                    k++;
        //                }
        //            }
        //        }
        //        if (trans == null)
        //        {
        //            var min = sch.Where(kvp => !float.IsInfinity(kvp.Value) && !kvp.Key.IsControllable)
        //            .Select(kvp => kvp.Value).Append(float.PositiveInfinity).Min();

        //            var enable = sch.Where(kvp => kvp.Value <= min && !float.IsInfinity(kvp.Value)).Select(kvp => kvp.Key).ToSet();

        //            enable.IntersectWith(transitions[state].Keys);

        //            if (enable.All(e => e.IsControllable)) break;

        //            trans = new Transition(state, enable.First(e => !e.IsControllable), transitions[state][enable.First(e => !e.IsControllable)]);

        //            if (trans == null) break;
        //        }

        //        var ev = trans.Trigger;
        //        state = trans.Destination;

        //        events.Add(ev);
        //        time += sch[ev];
        //        sch = problem.UpdateFunction(sch, ev);
        //    }

        //    if (state != target) throw new Exception($"The target state ({target}) was not reached!");

        //    return (time);
        //}

        //public static float TimeEvaluationControllableStochastic(ISchedulingProblemAB problem, List<AbstractEvent> seq, double stdDev,
        //    AbstractState target = null)
        //{
        //    var sequence = new List<AbstractEvent> { };
        //    foreach (AbstractEvent e in seq)
        //    {
        //        if (e.IsControllable) sequence.Add(e);
        //    }

        //    target = target ?? problem.TargetState;
        //    var state = problem.InitialState;
        //    var transitions = problem.Supervisor.Transitions.GroupBy(t => t.Origin)
        //        .ToDictionary(g => g.Key, g => g.ToDictionary(t => t.Trigger, t => t.Destination));
        //    var events = new List<AbstractEvent>();
        //    var sch = problem.InitialScheduler;
        //    var k = 0;

        //    float time = 0;

        //    while (true)
        //    {
        //        Transition trans = null;
        //        var e63 = new Event("63", Controllability.Controllable);
        //        var e65 = new Event("65", Controllability.Controllable);

        //        if (k < sequence.Count())
        //        {
        //            if (transitions[state].ContainsKey(sequence[k]))
        //            {
        //                if ((sequence[k].ToString() == "63" && sch[e63] > 0 || sequence[k].ToString() == "65" && sch[e65] > 0) && k != sequence.Count() - 1) { }
        //                else
        //                {
        //                    trans = new Transition(state, sequence[k], transitions[state][sequence[k]]);
        //                    k++;
        //                }

        //            }
        //        }
        //        if (trans == null)
        //        {
        //            var min = sch.Where(kvp => !float.IsInfinity(kvp.Value) && !kvp.Key.IsControllable)
        //            .Select(kvp => kvp.Value).Append(float.PositiveInfinity).Min();

        //            //if (sch[e63] > 0 && transitions[state].Any(t => t.Value.ToString() == "63"))
        //            //    if (sch[e63] < min) min = sch[e63];
        //            //if (sch[e65] > 0 && transitions[state].Any(t => t.Value.ToString() == "65"))
        //            //    if (sch[e65] < min) min = sch[e65];
        //            var aux = transitions[state];

        //            var enable = sch.Where(kvp => kvp.Value <= min && !float.IsInfinity(kvp.Value)).Select(kvp => kvp.Key).ToSet();

        //            enable.IntersectWith(transitions[state].Keys);

        //            if (enable.All(e => e.IsControllable))
        //                break;

        //            trans = new Transition(state, enable.First(e => !e.IsControllable), transitions[state][enable.First(e => !e.IsControllable)]);

        //            if (trans == null)
        //                break;

        //        }

        //        var ev = trans.Trigger;
        //        state = trans.Destination;

        //        events.Add(ev);
        //        time += sch[ev];

        //        //Console.WriteLine($"{k} - Evento: {ev} - Tempo: {time}");

        //        sch = problem.StochasticUpdateFunction(sch, ev, stdDev);
        //        //if (sch.Values.Any(v => float.IsNaN(v)))
        //        //    Console.WriteLine("NaN");

        //    }

        //    if (state != target)
        //    {

        //        //var aaa = 0;
        //        //var bbb = transitions[state];
        //        throw new Exception($"The target state ({target}) was not reached!");
        //    }


        //    return (time);
        //}

        //private static double NormalSample(Random rand, double mean = 0, double stdDev = 1)
        //{
        //    var u1 = 1.0 - rand.NextDouble();
        //    var u2 = 1.0 - rand.NextDouble();
        //    var stdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        //    return mean + stdDev * stdNormal;
        //}

        public static void print_politica(Dictionary<AbstractState, List<(AbstractEvent, double)>> PI, bool normalizado = false)
        {
            double max = 0;
            if (normalizado)
                max = PI.SelectMany(kvp => kvp.Value).Select(tupla => tupla.Item2).Max();
            else
                max = 1;
            foreach (var kvp in PI)
            {
                var state = kvp.Key;
                var events = kvp.Value;

                Console.WriteLine($"Estado: {state} - {state.ActiveTasks()}");
                System.IO.File.AppendAllText("politica.txt", $"Estado: {state} - {state.ActiveTasks()}\n");

                foreach (var evento in events)
                {
                    Console.WriteLine($"  Evento: {evento.Item1}   Valor: {evento.Item2 / max:f3}");
                    System.IO.File.AppendAllText("politica.txt", $"  Evento: {evento.Item1}   Valor: {evento.Item2 / max:f3}\n");
                }
            }

            //var max = PI.SelectMany(kvp => kvp.Value).Select(tupla => tupla.Item2).Max();
            //Console.WriteLine(  max);
        }

        static void SerializeData(string fileName, Dictionary<AbstractState, List<(AbstractEvent, double)>> data)
        {
            try
            {
                using (Stream stream = File.Open(fileName, FileMode.Create))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, data);
                }
            }
            catch (IOException e)
            {
                Console.WriteLine("Erro ao salvar os dados: " + e.Message);
            }
        }

        public static void SerializePolicyValue(Dictionary<AbstractState, List<(AbstractEvent, double)>> piValue, string path)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, piValue);
            stream.Close();
        }

        public static Dictionary<AbstractState, List<(AbstractEvent, double)>> DeserializePolicyValue(string path)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var obj = (Dictionary<AbstractState, List<(AbstractEvent, double)>>)formatter.Deserialize(stream);
            stream.Close();
            return obj;
        }

        public static void SerializePolicy(Dictionary<AbstractState, List<AbstractEvent>> pi, string path)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, pi);
            stream.Close();
        }

        public static Dictionary<AbstractState, List<AbstractEvent>> DeserializePolicy(string path)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var obj = (Dictionary<AbstractState, List<AbstractEvent>>)formatter.Deserialize(stream);
            stream.Close();
            return obj;
        }

        public static (double time, T result) Timming<T>(this Func<T> f)
        {
            var timer = new Stopwatch();
            timer.Start();
            var result = f();
            timer.Stop();

            return (timer.ElapsedMilliseconds / 1000.0, result);
        }

        public static double Timming<T>(this Action f)
        {
            var timer = new Stopwatch();
            timer.Start();
            f();
            timer.Stop();

            return timer.ElapsedMilliseconds / 1000.0;
        }

        public static HashSet<T> ToSet<T>(this IEnumerable<T> seq) => new HashSet<T>(seq);
    }
}
