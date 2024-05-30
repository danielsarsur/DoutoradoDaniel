using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using UltraDES;
using PlanningDES;
using PlanningDES.Problems;
using ConsoleTables;


namespace ProgramaDaniel
{
    internal class Program
    {
        private static void Main()
        {
            try
            {
                Stopwatch timer_total = new Stopwatch(); timer_total.Start();

                System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.AboveNormal;
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

                //var problem = new SmallFactory();
                //var problem = new ExtendedSmallFactory();
                //var problem = new IndustrialTransferLine();
                //var problem = new LinearClusterTool(2);
                var problem = new FlexibleManufacturingSystem();
                //var problem = new Ezpeleta();

                Console.WriteLine((problem.ToString()).Split('.').Last());
                var products = new[] { 1, 10, 100, 1000 };
                var gamma = 0.7f;
                var threshold = 0.001f;

                MDP_Monolithic(problem, products, gamma, threshold);

                if (problem.Supervisors != null)
                    MDP_LocalModular(problem, products, gamma, threshold);

                var tempo_total = timer_total.ElapsedMilliseconds; timer_total.Stop();
                Console.WriteLine($"\nTempo total {tempo_total / 1000f} s");
            }
            catch (Exception erro) { Console.WriteLine(erro.Message); }

            Console.WriteLine("\nFIM");
            Console.ReadLine();
        }

        internal static void MDP_Monolithic(PlanningDES.ISchedulingProblem problem, int[] products, float gamma, float threshold)
        {
            Console.WriteLine("\n*** MONOLITICO ***\n");
            Console.WriteLine(problem.Supervisor.States.Count() + " estados");
            Console.WriteLine(problem.Supervisor.Transitions.Count() + " transições\n");
            var table = new ConsoleTable("Batch", "Time (s)", "Makespan", "Parallelism");

            Dictionary<AbstractState, List<AbstractEvent>> PI = null;
            var transitions = problem.Transitions;
            var S = problem.Supervisor.States.ToList();

            // Calcula as transições de probabilidade. P é um dicionário cuja chave é o estado de origem e a chave
            // é um outro dicionário que mapeia o Estado-Ação para uma lista (estado_destino, probabilidade)
            (var tempo_prob, var P) = Tools.Timming(() =>
                MDP.TransitionProbability(transitions));

            Console.WriteLine($"Gerou as probabilidades em {tempo_prob} s");

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
            //Tools.print_politica(PI_value);

            for (var i = 0; i < 1; i++)
            {
                foreach (var prod in products)
                {
                    (var tempo_sequencia, var (seq, makespan)) = Tools.Timming(() =>
                        Tools.Sequence(problem, prod, PI));

                    var parallelism = problem.MetricEvaluation(seq.ToArray(), (t) => t.destination.ActiveTasks());
                    table.AddRow(prod, tempo_sequencia, makespan.Last(), parallelism);
                }
            }
            Console.WriteLine();
            table.Write(Format.Alternative);
            //var path = "resultados.csv";
            //System.IO.File.AppendAllText(path, texto);
        }

        internal static void MDP_LocalModular(PlanningDES.ISchedulingProblem problem, int[] products, float gamma, float threshold)
        {
            Console.WriteLine("\n*** MODULAR LOCAL ***\n");
            var table = new ConsoleTable("Batch", "Time (s)", "Makespan", "Parallelism");

            var events = new HashSet<AbstractEvent>();
            foreach (var sup in problem.Supervisors)
                events.UnionWith(sup.Events);

            var PI_vector = new List<Dictionary<AbstractState, List<(AbstractEvent, double)>>> { };
            var supervisors = new List<(Dictionary<AbstractState, Dictionary<AbstractEvent, AbstractState>> transitions,
                Dictionary<AbstractState, List<(AbstractEvent, double)>> PI, AbstractState state)> { };

            foreach (var s in problem.Supervisors)
            {
                Console.WriteLine(s.Name);
                Console.WriteLine(s.States.Count().ToString() + " estados");
                Console.WriteLine(s.Transitions.Count() + " transições");

                var sup = s.InverseProjection(events);
                var S = sup.States.ToList();
                var A = sup.Events.ToList();
                var transitions = sup.Transitions.GroupBy(t => t.Origin)
                    .ToDictionary(g => g.Key, g => g.ToDictionary(t => t.Trigger, t => t.Destination));

                // Calcula as transições de probabilidade. P é um dicionário cuja chave é o estado de origem e a chave
                // é um outro dicionário que mapeia o Estado-Ação para uma lista (estado_destino, probabilidade)
                (var tempo_prob, var P) = Tools.Timming(() =>
                    MDP.TransitionProbability(transitions));

                Console.WriteLine($"Gerou as probabilidades em {tempo_prob} s");

                // Calcula o valor de forma iterativa para cada estado. Recompensa é a quantidade de tarefas ativas
                (var tempo_valor, var V) = Tools.Timming(() =>
                    MDP.ValueIterationParallelismReward(S, P, gamma, threshold));

                Console.WriteLine($"Iterou os valores em {tempo_valor} s");

                // Calcula a política para cada estado. É utilizada uma lista de eventos, pois o eventoda política pode não ser temporalmente factível
                (var tempo_politica, var PI_value) = Tools.Timming(() =>
                    MDP.PolicyParallelismReward(S, P, gamma, V));
                PI_vector.Add(PI_value);

                Console.WriteLine($"Calculou a política em {tempo_politica} s\n");

                supervisors.Add((transitions: transitions, PI: PI_value, state: sup.InitialState));

                //Console.WriteLine();
                //Console.WriteLine(s.Name);
                //System.IO.File.AppendAllText("politica.txt", $"{s.Name}\n");
                //Tools.print_politica(PI_value);
                //System.IO.File.AppendAllText("politica.txt", "\n\n");
                //Console.WriteLine(); Console.WriteLine();
            }

            for (var i = 0; i < 1; i++)
            {
                foreach (var prod in products)
                {
                    (var tempo_sequencia, var (seq, makespan)) = Tools.Timming(() =>
                        Tools.SequenceModular(problem, prod, supervisors, events));

                    var parallelism = problem.MetricEvaluation(seq.ToArray(), (t) => t.destination.ActiveTasks());
                    table.AddRow(prod, tempo_sequencia, makespan.Last(), parallelism);
                }
            }
            Console.WriteLine();
            table.Write(Format.Alternative);
            //var path = "resultados.csv";
            //System.IO.File.AppendAllText(path, texto);
        }
    }
}