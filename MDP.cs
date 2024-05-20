using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UltraDES;

namespace ProgramaDaniel
{
    public static class MDP
    {
        public static Dictionary<AbstractEvent, double> RewardFirstEvents(string name, List<AbstractEvent> ev)
        {
            Dictionary<AbstractEvent, double> R = ev.ToDictionary(key => key, value => (double)0);

            switch (name)
            {
                case "SFM":
                    foreach (var e in ev)
                    {
                        switch (e.ToString())
                        {
                            case "11":
                                R[e] = 1 / 25;
                                break;
                            case "21":
                                R[e] = 1 / 25;
                                break;
                            case "31":
                                R[e] = 1 / 21;
                                break;
                            case "33":
                                R[e] = 1 / 19;
                                break;
                            case "41":
                                R[e] = 1 / 30;
                                break;
                            case "51":
                                R[e] = 1 / 8;
                                break;
                            case "53":
                                R[e] = 1 / 32;
                                break;
                            case "35":
                                R[e] = 1 / 16;
                                break;
                            case "37":
                                R[e] = 1 / 24;
                                break;
                            case "39":
                                R[e] = 1 / 20;
                                break;
                            case "61":
                                R[e] = 1 / 15;
                                break;
                            case "63":
                                R[e] = 1 / 16;
                                break;
                            case "71":
                                R[e] = 1 / 16;
                                break;
                            case "81":
                                R[e] = 1 / 16;
                                break;
                            case "73":
                                R[e] = 1 / 16;
                                break;
                            case "65":
                                R[e] = 1 / 16;
                                break;
                        }
                    }
                    break;
                case "SF":

                    break;
                case "SFextended":
                    foreach (var e in ev)
                    {
                        switch (e.ToString())
                        {
                            case "1":
                                R[e] = 3;
                                break;
                            case "3":
                                R[e] = 2;
                                break;
                            case "5":
                                R[e] = 1;
                                break;
                        }
                    }
                    break;
            }
            return R;
        }

        public static Dictionary<AbstractState, KeyValuePair<(AbstractState, AbstractEvent), List<(AbstractState, float)>>[]> TransitionProbability
            (Dictionary<AbstractState, Transition[]> transitions)
        {
            var P = new Dictionary<(AbstractState, AbstractEvent), List<(AbstractState, float)>>();

            foreach (var transition in transitions)
            {
                var quant_nao_controlaveis = (float)transition.Value.Where(t => !t.IsControllableTransition).Count();

                foreach (var t in transition.Value)
                {
                    P.Add((t.Origin, t.Trigger), new List<(AbstractState, float)> { });

                    if (t.IsControllableTransition)
                    {
                        P[(t.Origin, t.Trigger)].Add((t.Destination, 1.0f));
                    }
                    else
                    {
                        var p = 1 / quant_nao_controlaveis;
                        P[(t.Origin, t.Trigger)].Add((t.Destination, p));

                        var destinos = transitions[t.Origin].Where(tt => !tt.IsControllableTransition && tt.Trigger != t.Trigger).ToList();
                        if (destinos.Any())
                        {
                            foreach (var dest in destinos)
                            {
                                p = 1 / quant_nao_controlaveis;
                                P[(t.Origin, t.Trigger)].Add((dest.Destination, p));
                            }
                        }
                    }
                }
            }
            return P.AsParallel().GroupBy(t => t.Key.Item1).ToDictionary(gr => gr.Key, gr => gr.ToArray());
        }

        public static Dictionary<AbstractState, double> ValueIterationParallelismReward
            (List<AbstractState> S, Dictionary<AbstractState, KeyValuePair<(AbstractState, AbstractEvent), List<(AbstractState, float)>>[]> P, float gamma, float threshold)
        {
            Dictionary<AbstractState, double> V = S.ToDictionary(key => key, value => 0.0);

            var V_anterior = new Dictionary<AbstractState, double>(V);
            var it = 0;

            do
            {
                V_anterior = new Dictionary<AbstractState, double>(V);
                foreach (var kvp in P)
                {
                    var v_max = 0.0;
                    foreach (var estado_acao in kvp.Value)
                    {

                        var soma = 0.0;
                        var origem = estado_acao.Key.Item1;
                        var evento = estado_acao.Key.Item2;
                        foreach (var destino_prob in estado_acao.Value)
                        {
                            var destino = destino_prob.Item1;
                            var prob = destino_prob.Item2;
                            soma += (prob * (destino.ActiveTasks() + (gamma * V_anterior[destino])));
                        }
                        if (soma > v_max) v_max = soma;
                    }
                    V[kvp.Key] = v_max;
                }
                it++;
            } while (Math.Abs(V.Sum(kvp => kvp.Value) - V_anterior.Sum(kvp => kvp.Value)) > threshold);

            Console.WriteLine($"Iterações: {it}");

            return V;
        }

        public static Dictionary<AbstractState, float> ValueIterationEventsReward
            (List<AbstractState> S, Dictionary<AbstractState, KeyValuePair<(AbstractState, AbstractEvent), List<(AbstractState, float)>>[]> P,
            Dictionary<AbstractEvent, double> R, float gamma, float threshold)
        {
            Dictionary<AbstractState, float> V = S.ToDictionary(key => key, value => 0.0f);

            var V_anterior = new Dictionary<AbstractState, float>(V);
            var it = 0;

            do
            {
                V_anterior = new Dictionary<AbstractState, float>(V);
                foreach (var kvp in P)
                {
                    var v_max = 0.0f;
                    foreach (var estado_acao in kvp.Value)
                    {
                        var soma = 0.0f;
                        var origem = estado_acao.Key.Item1;
                        var evento = estado_acao.Key.Item2;
                        foreach (var destino_prob in estado_acao.Value)
                        {
                            var destino = destino_prob.Item1;
                            var prob = destino_prob.Item2;
                            soma += prob * ((float)R[evento] + (gamma * V_anterior[destino]));
                        }
                        if (soma > v_max) v_max = soma;
                    }
                    V[kvp.Key] = v_max;
                }
                it++;
            } while (Math.Abs(V.Sum(kvp => kvp.Value) - V_anterior.Sum(kvp => kvp.Value)) > threshold);

            Console.WriteLine($"Iterações: {it}");

            return V;
        }

        public static Dictionary<AbstractState, float> ValueIterationBuffer
            (List<AbstractState> S, Dictionary<AbstractState, KeyValuePair<(AbstractState, AbstractEvent), List<(AbstractState, float)>>[]> P, float gamma, float threshold)
        {
            Dictionary<AbstractState, float> V = S.ToDictionary(key => key, value => 0.0f);

            var V_anterior = new Dictionary<AbstractState, float>(V);
            var it = 0;

            do
            {
                V_anterior = new Dictionary<AbstractState, float>(V);
                foreach (var kvp in P)
                {
                    var v_max = 0.0f;
                    foreach (var estado_acao in kvp.Value)
                    {
                        var soma = 0.0f;
                        var origem = estado_acao.Key.Item1;
                        var evento = estado_acao.Key.Item2;
                        foreach (var destino_prob in estado_acao.Value)
                        {
                            var destino = destino_prob.Item1;
                            var prob = destino_prob.Item2;
                            soma += prob * (destino.BufferCount() + (gamma * V_anterior[destino]));
                        }
                        if (soma > v_max) v_max = soma;
                    }
                    V[kvp.Key] = v_max;
                }
                it++;
            } while (Math.Abs(V.Sum(kvp => kvp.Value) - V_anterior.Sum(kvp => kvp.Value)) > threshold);

            Console.WriteLine($"Iterações: {it}");

            return V;
        }

        public static Dictionary<AbstractState, List<(AbstractEvent, double)>> PolicyParallelismReward
            (List<AbstractState> S, Dictionary<AbstractState, KeyValuePair<(AbstractState, AbstractEvent), List<(AbstractState, float)>>[]> P,
            float gamma, Dictionary<AbstractState, double> V)
        {
            Dictionary<AbstractState, List<(AbstractEvent, double)>> PI = S.ToDictionary(key => key, value => new List<(AbstractEvent, double)> { });
            foreach (var kvp in P)
            {
                var lista = new List<(AbstractEvent, double)> { };

                foreach (var estado_acao in kvp.Value)
                {
                    var soma = 0.0;
                    var origem = estado_acao.Key.Item1;
                    var evento = estado_acao.Key.Item2;

                    foreach (var destino_prob in estado_acao.Value)
                    {
                        var destino = destino_prob.Item1;
                        var prob = destino_prob.Item2;
                        soma += prob * (destino.ActiveTasks() + (gamma * V[destino]));
                    }
                    lista.Add((evento, soma));
                }
                // Ordena os possíveis eventos pela ordem descrescente de valor e, em seguida, em ordem descrescente de evento (para o SFM) ou ordem crescente para priorizar
                // os eventos do início.

                PI[kvp.Key] = lista.OrderByDescending(p => p.Item2).ThenBy(p => p.Item1.IsControllable).ThenBy(p => p.Item1.ToString()).ToList();
            }
            return PI;
        }

        public static Dictionary<AbstractState, List<AbstractEvent>> PolicyEventsReward
            (List<AbstractState> S, Dictionary<AbstractState, KeyValuePair<(AbstractState, AbstractEvent), List<(AbstractState, float)>>[]> P,
            Dictionary<AbstractEvent, double> R, float gamma, Dictionary<AbstractState, float> V)
        {
            Dictionary<AbstractState, List<AbstractEvent>> PI = S.ToDictionary(key => key, value => new List<AbstractEvent> { });
            foreach (var kvp in P)
            {
                var lista = new List<(AbstractEvent, float)> { };

                foreach (var estado_acao in kvp.Value)
                {
                    var soma = 0.0f;
                    var origem = estado_acao.Key.Item1;
                    var evento = estado_acao.Key.Item2;
                    foreach (var destino_prob in estado_acao.Value)
                    {
                        var destino = destino_prob.Item1;
                        var prob = destino_prob.Item2;
                        soma += prob * ((float)R[evento] + (gamma * V[destino]));
                    }
                    lista.Add((evento, soma));
                }
                // Ordena os possíveis eventos pela ordem descrescente de valor e, em seguida, em ordem descrescente de evento (para o SFM) ou ordem crescente para priorizar
                // os eventos do início.
                PI[kvp.Key] = lista.OrderByDescending(p => p.Item2).ThenBy(pp => pp.Item1.ToString()).Select(v => v.Item1).ToList();
            }
            return PI;
        }
    }
}