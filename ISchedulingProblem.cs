using System;
using System.Linq;
using System.Collections.Generic;
using UltraDES;

namespace ProgramaDaniel
{
    public interface ISchedulingProblem
    {

        DeterministicFiniteAutomaton Supervisor { get; }
        IEnumerable<DeterministicFiniteAutomaton> Supervisors { get; }
        int Depth { get; }
        Dictionary<AbstractEvent, float> InitialScheduler { get; }
        AbstractState InitialState { get; }
        AbstractState TargetState { get; }
        Func<Dictionary<AbstractEvent, float>, AbstractEvent, Dictionary<AbstractEvent, float>> UpdateFunction { get; }
        Dictionary<AbstractEvent, uint> InitialRestrition(int products);
    }

    public interface ISchedulingProblemAB : ISchedulingProblem
    {
        (int, int) DepthAB { get; }
        Dictionary<AbstractEvent, uint> InitialRestrition((int a, int b) produto);
        Func<Dictionary<AbstractEvent, float>, AbstractEvent, double, Dictionary<AbstractEvent, float>> StochasticUpdateFunction { get; }
    }

    public interface ISchedulingProblemABC : ISchedulingProblem
    {
        (int, int, int) DepthABC { get; }

        Dictionary<AbstractEvent, uint> InitialRestrition((int a, int b, int c) produto);
    }

    public static class Extensions
    {
        public static double ActiveTasks(this AbstractState state)
        {
            if (state is AbstractCompoundState)
                return (double)(state as AbstractCompoundState).S.OfType<ExpandedState>().Sum(s => s.Tasks);
            if (state is ExpandedState)
                return (state as ExpandedState).Tasks;
            return 0;
        }

        public static uint BufferCount(this AbstractState state)
        {
            if (state is AbstractCompoundState)
                return (uint)(state as AbstractCompoundState).S.OfType<ExpandedState>().Sum(s => s.Buffer);
            if (state is ExpandedState)
                return (state as ExpandedState).Buffer;
            return 0;
        }
    }
}