using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using UltraDES;

namespace ProgramaDaniel
{
    internal class Program
    {
        private static void Main()
        {
            try
            {
                System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.AboveNormal;
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

                Stopwatch timer_total = new Stopwatch(); timer_total.Start();
                Stopwatch timer_sup = new Stopwatch(); timer_sup.Start();

                //ISchedulingProblem plant = new SF();
                //ISchedulingProblem plant = new SFextended();
                //ISchedulingProblem plant = new ITL();
                ISchedulingProblem plant = new CT1R();
                //ISchedulingProblem plant = new CT2R();
                //ISchedulingProblemAB plant = new SFM();
                //ISchedulingProblemABC plant = new EZPELETA();

                var tempo_sup = timer_sup.ElapsedMilliseconds; timer_sup.Stop();
                Console.WriteLine($"\nComputou/carregou os supervisores em {tempo_sup / 1000.0} segundos\n\n");

                Console.WriteLine("*** MODULAR LOCAL ***");
                SeqFullModular(plant);

                Console.WriteLine("\n\n*** MONOLITICO ***");

                SeqFull(plant);
                //SeqControllable();

                var tempo_total = timer_total.ElapsedMilliseconds; timer_total.Stop();
                Console.WriteLine($"\nTempo total {tempo_total / 1000f} s");
            }
            catch (Exception erro) { Console.WriteLine(erro.Message); }

            Console.WriteLine("\nFIM");
            Console.ReadLine();
        }

        internal static void SeqFullModular(ISchedulingProblem plant)
        {
            foreach (var s in plant.Supervisors)
            {
                Console.WriteLine(s.Name);
                Console.WriteLine(s.States.Count().ToString());
                Console.WriteLine(s.Transitions.Count());
                Console.WriteLine();
            }

            var events = new HashSet<AbstractEvent>();
            foreach (var sup in plant.Supervisors)
                events.UnionWith(sup.Events);

            var PI_vector = new List<Dictionary<AbstractState, List<(AbstractEvent, double)>>> { };
            var transitions_vector = new List<Dictionary<AbstractState, Transition[]>> { };
            var supervisors = new List<(Dictionary<AbstractState, Transition[]> transitions, Dictionary<AbstractState, List<(AbstractEvent, double)>> PI, AbstractState state, AbstractState initial_state)> { };

            foreach (var s in plant.Supervisors)
            {
                var sup = s.InverseProjection(events);
                var S = sup.States.ToList();
                var A = sup.Events.ToList();
                var transitions = sup.Transitions.AsParallel()
                    .GroupBy(t => t.Origin)
                    .ToDictionary(gr => gr.Key, gr => gr.ToArray());
                transitions_vector.Add(transitions);

                // Calcula as transições de probabilidade. P é um dicionário cuja chave é o estado de origem e a chave
                // é um outro dicionário que mapeia o Estado-Ação para uma lista (estado_destino, probabilidade)
                (var tempo_prob, var P) = Tools.Timming(() => 
                    MDP.TransitionProbability(transitions));

                Console.WriteLine($"Gerou as probabilidades em {tempo_prob} s");

                // Desconto e limiar de parada
                var gamma = 0.7f; var threshold = 0.001f;

                // Calcula o valor de forma iterativa para cada estado. Recompensa é a quantidade de tarefas ativas
                (var tempo_valor, var V) = Tools.Timming(() =>
                    MDP.ValueIterationParallelismReward(S, P, gamma, threshold));

                Console.WriteLine($"Iterou os valores em {tempo_valor} s");

                // Calcula a política para cada estado. É utilizada uma lista de eventos, pois o eventoda política pode não ser temporalmente factível
                (var tempo_politica, var PI_value) = Tools.Timming(() => 
                    MDP.PolicyParallelismReward(S, P, gamma, V));
                PI_vector.Add(PI_value);

                Console.WriteLine($"Calculou a política em {tempo_politica} s");

                supervisors.Add((transitions: transitions, PI: PI_value, state: sup.InitialState, initial_state: sup.InitialState));

                //Console.WriteLine(s.Name);
                //System.IO.File.AppendAllText("politica.txt", $"{s.Name}\n");
                //Tools.print_politica(PI_value);
                //System.IO.File.AppendAllText("politica.txt", "\n\n");
                //Console.WriteLine(); Console.WriteLine();
            }

            Stopwatch timer_seq = new Stopwatch(); timer_seq.Start();
            List<double> tempo_seq = new List<double> { };
            List<float> makespan = new List<float> { };

            for (var i = 0; i < 1; i++)
            {
                //var products = new[] { (1000, 1000) }
                var products = new[] { 1,2,3,4,5,10,100,1000 };
                foreach (var prod in products)
                {
                    Console.WriteLine("\nLote: " + prod);
                    timer_seq.Restart();

                    // Gera a sequência com base na política calculada. A função retorna a lista de eventos e a lista com o instante
                    // de tempo em que cada evento acontece.
                    //var (seq, time) = Sequence(plant.InitialState, plant.TargetState, plant.InitialScheduler, plant.DepthAB.Item1 * prod.Item1 + plant.DepthAB.Item2 * prod.Item2,
                    //    plant.UpdateFunction, plant.InitialRestrition(prod), transitions, PI);
                    //(var tempo_sequencia, var (seq, time)) = Tools.Timming(() =>
                    //    Tools.Sequence(plant.InitialState, plant.TargetState, plant.InitialScheduler, plant.DepthAB.Item1 * prod.Item1 + plant.DepthAB.Item2 * prod.Item2,
                    //        plant.UpdateFunction, plant.InitialRestrition(prod), transitions, PI));

                    (var tempo_sequencia, var (seq, time)) = Tools.Timming(() =>
                        Tools.SequenceModular(plant.InitialScheduler, plant.Depth * prod, plant.UpdateFunction, plant.InitialRestrition(prod), supervisors, events));

                    tempo_seq.Add(tempo_sequencia);
                    makespan.Add(time.Last());

                    Console.WriteLine($"Makespan: {makespan.Last()} u.t.");
                    Console.WriteLine($"Computou a sequência em {tempo_seq.Last()} s");

                    //foreach (var e in seq)
                    //    Console.Write($"{e}, \n");
                    //Console.WriteLine();
                }
            }
            timer_seq.Stop();
            //var path = "resultados.csv";
            //System.IO.File.AppendAllText(path, texto);
        }

        internal static void SeqFull(ISchedulingProblem plant)
        {
            Dictionary<AbstractState, List<AbstractEvent>> PI = null;

            var transitions = plant.Supervisor.Transitions.AsParallel().GroupBy(t => t.Origin)
                .ToDictionary(gr => gr.Key, gr => gr.ToArray());

            //Console.WriteLine((plant.ToString()).Split('.').Last());
            //Console.WriteLine(plant.Supervisor.States.Count());
            //Console.WriteLine(plant.Supervisor.Transitions.Count());


            //if (File.Exists("Politica_Ezpeleta_mono.bin"))
            //{
            //    (var tempo_pi, PI) = Tools.Timming(() =>
            //        Tools.DeserializePolicy("Politica_Ezpeleta_mono.bin"));

            //    Console.WriteLine($"Importou a política em {tempo_pi} s");
            //}
            //else
            //{
                var S = plant.Supervisor.States.ToList();

                // Calcula as transições de probabilidade. P é um dicionário cuja chave é o estado de origem e a chave
                // é um outro dicionário que mapeia o Estado-Ação para uma lista (estado_destino, probabilidade)
                (var tempo_prob, var P) = Tools.Timming(() => 
                    MDP.TransitionProbability(transitions));

                Console.WriteLine($"Gerou as probabilidades em {tempo_prob} s");

                // Desconto e limiar de parada
                var gamma = 0.7f; var threshold = 0.001f;

                // Calcula o valor de forma iterativa para cada estado. Recompensa é a quantidade de tarefas ativas
                (var tempo_valor, var V) = Tools.Timming(() =>
                    MDP.ValueIterationParallelismReward(S, P, gamma, threshold));

                Console.WriteLine($"Iterou os valores em {tempo_valor} s");

                // Calcula a política para cada estado. É utilizada uma lista de eventos, pois o eventoda política pode não ser temporalmente factível
                (var tempo_politica, var PI_value) = Tools.Timming(() => 
                    MDP.PolicyParallelismReward(S, P, gamma, V));

                Console.WriteLine($"Calculou a política em {tempo_politica} s");

                PI = PI_value.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Select(item => item.Item1).ToList());

                //Tools.SerializePolicy(PI, "Politica_Ezpeleta_mono.bin");
            //}

            //Tools.print_politica(PI_value);
            
            Stopwatch timer_seq = new Stopwatch(); timer_seq.Start();
            List<double> tempos_seq = new List<double> { };
            List<float> makespan = new List<float> { };

            for (var i = 0; i < 1; i++)
            {
                //var products = new[] { (100, 100, 100), (150, 75, 75) };
                var products = new[] { 1, 2, 3, 4, 5, 10, 100, 1000 };
                foreach (var prod in products)
                {
                    Console.WriteLine("\nLote: " + prod);
                    timer_seq.Restart();

                    // Gera a sequência com base na política calculada. A função retorna a lista de eventos e a lista com o instante
                    // de tempo em que cada evento acontece.
                    //var (seq, time) = Sequence(plant.InitialState, plant.TargetState, plant.InitialScheduler, plant.DepthAB.Item1 * prod.Item1 + plant.DepthAB.Item2 * prod.Item2,
                    //    plant.UpdateFunction, plant.InitialRestrition(prod), transitions, PI);
                    //(var tempo_sequencia, var (seq, time)) = Tools.Timming(() =>
                    //    Tools.Sequence(plant.InitialState, plant.TargetState, plant.InitialScheduler, plant.DepthAB.Item1 * prod.Item1 + plant.DepthAB.Item2 * prod.Item2,
                    //        plant.UpdateFunction, plant.InitialRestrition(prod), transitions, PI));

                    //// SF, CT
                    (var tempo_sequencia, var (seq, time)) = Tools.Timming(() =>
                        Tools.Sequence(plant.InitialState, plant.TargetState, plant.InitialScheduler, plant.Depth * prod,
                            plant.UpdateFunction, plant.InitialRestrition(prod), transitions, PI));

                    // Ezpeleta
                    //var (depthA, depthB, depthC) = plant.DepthABC;
                    //var (nA, nB, nC) = prod;
                    //(var tempo_sequencia, var (seq, time)) = Tools.Timming(() =>
                    //    Tools.Sequence(plant.InitialState, plant.TargetState, plant.InitialScheduler, depthA * nA + depthB * nB + depthC * nC,
                    //        plant.UpdateFunction, plant.InitialRestrition(prod), transitions, PI));

                    tempos_seq.Add(tempo_sequencia);
                    makespan.Add(time.Last());

                    Console.WriteLine($"Makespan: {makespan.Last()} u.t.");
                    Console.WriteLine($"Computou a sequência em {tempos_seq.Last()} s");

                    //foreach (var e in seq)
                    //{
                    //    Console.Write($"{e}, \n");
                    //}
                    //Console.WriteLine();
                }
            }
            timer_seq.Stop();
            //var path = "resultados.csv";
            //System.IO.File.AppendAllText(path, texto);
        }

        internal static void SeqControllable()
        {
            Stopwatch timer_sup = new Stopwatch(); timer_sup.Start();

            //ISchedulingProblem plant = new SF();
            //ISchedulingProblem plant = new SFextended();
            //ISchedulingProblem plant = new ITL();
            //ISchedulingProblem plant = new CT1R();
            //ISchedulingProblem plant = new CT2R();
            ISchedulingProblemAB plant = new SFM();

            var SupervisorProjected = plant.Supervisor.Projection(plant.Supervisor.Events.Where(e => !e.IsControllable));
            SupervisorProjected.simplifyName();
            var SupervisorProjectedInv = SupervisorProjected.InverseProjection(plant.Supervisor.Events);

            var tempo_sup = timer_sup.ElapsedMilliseconds; timer_sup.Stop();

            var S = SupervisorProjected.States.ToList();
            var A = SupervisorProjected.Events.ToList();
            var transitions = SupervisorProjected.Transitions.AsParallel()
                .GroupBy(t => t.Origin)
                .ToDictionary(gr => gr.Key, gr => gr.ToArray());

            // Calcula as transições de probabilidade. P é um dicionário cuja chave é o estado de origem e a chave
            // é um outro dicionário que mapeia o Estado-Ação para uma lista (estado_destino, probabilidade).
            //Dictionary<AbstractState, KeyValuePair<(AbstractState, AbstractEvent), List<(AbstractState, float)>>[]>
            //    P = MDP.TransitionProbability(transitions);
            (var tempo_prob, var P) = Tools.Timming(() => MDP.TransitionProbability(transitions));

            // Desconto e limiar de parada
            var gamma = 0.7f;
            var threshold = 0.001f;

            var R = MDP.RewardFirstEvents(plant.ToString().Split('.').Last().ToString(), A.Where(e => e.IsControllable).ToList());

            // Calcula o valor de forma iterativa para cada estado. Recompensa é a quantidade de tarefas ativas.
            //Dictionary<AbstractState, float> V = MDP.ValueIteration(S, P, R, gamma, threshold);
            (var tempo_valor, var V) = Tools.Timming(() =>
                MDP.ValueIterationEventsReward(S, P, R, gamma, threshold));

            // Calcula a política para cada estado. É utilizada uma lista de eventos, pois o eventoda política pode não ser temporalmente factível. 
            //Dictionary<AbstractState, List<AbstractEvent>> PI = MDP.Policy(S, P, R, gamma, V);
            (var tempo_politica, var PI) = Tools.Timming(() => MDP.PolicyEventsReward(S, P, R, gamma, V));

            Console.WriteLine($"Computou supervisor em {tempo_sup} s");
            Console.WriteLine($"Gerou as probabilidades em {tempo_prob} s");
            Console.WriteLine($"Iterou os valores em {tempo_valor} s");
            Console.WriteLine($"Calculou a política em {tempo_politica} s");
            Stopwatch timer_seq = new Stopwatch(); timer_seq.Start();

            List<double> tempo_seq = new List<double> { };
            List<float> makespan = new List<float> { };

            transitions = SupervisorProjectedInv.Transitions.AsParallel()
                .GroupBy(t => t.Origin)
                .ToDictionary(gr => gr.Key, gr => gr.ToArray());

            for (var i = 0; i < 1; i++)
            {
                var products = new[] { (10, 10), (100, 100) };
                foreach (var prod in products)
                {
                    Console.WriteLine("\nLote: " + prod);
                    timer_seq.Restart();

                    // Gera a sequência com base na política calculada. A função retorna a lista de eventos e a lista com o instante
                    // de tempo em que cada evento acontece.
                    //var (seq, time) = SequenceWithControllables(plant.InitialState, plant.TargetState, plant.InitialScheduler, plant.Depth * prod,
                    //    plant.UpdateFunction, plant.InitialRestrition(prod), transitions, PI);
                    //var seq = SequenceWithControllables(SupervisorProjectedInv.InitialState, SupervisorProjectedInv.InitialState, plant.Depth * prod / 2,
                    //    plant.InitialRestrition(prod), transitions, PI);
                    //var seq = Tools.SequenceWithControllables(SupervisorProjectedInv.InitialState, SupervisorProjectedInv.InitialState,
                    //    (plant.DepthAB.Item1 / 2 + 1) * prod.Item1 + (plant.DepthAB.Item2 / 2 + 1) * prod.Item2, plant.InitialRestrition(prod), transitions, PI);

                    (var tempo_sequencia, var seq) = Tools.Timming(() =>
                        Tools.SequenceWithControllables(SupervisorProjectedInv.InitialState, SupervisorProjectedInv.InitialState,
                        (plant.DepthAB.Item1 / 2 + 1) * prod.Item1 + (plant.DepthAB.Item2 / 2 + 1) * prod.Item2, plant.InitialRestrition(prod), transitions, PI));

                    var time = Tools.TimeEvaluationControllable(plant, seq);

                    tempo_seq.Add(tempo_sequencia);
                    makespan.Add(time);

                    Console.WriteLine($"Makespan: {makespan.Last()} u.t.");
                    Console.WriteLine($"Computou a sequência em {tempo_seq.Last()} s");
                }
            }
            timer_seq.Stop();
            //var path = "resultados.csv";
            //System.IO.File.AppendAllText(path, texto);
        }


    }
}