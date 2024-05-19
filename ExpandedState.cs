using System;
using UltraDES;

namespace ProgramaDaniel
{
    [Serializable]
    class ExpandedState : State
    {
        public double Tasks { get; private set; }
        public uint Buffer { get; private set; }

        public ExpandedState(string alias, double tasks, Marking marking = Marking.Unmarked, uint buffer = 0)
            : base(alias, marking)
        {
            Tasks = tasks;
            Buffer = buffer;
        }

        public override AbstractState ToMarked
        {
            get
            {
                return IsMarked ? this : new ExpandedState(Alias, Tasks, Marking.Marked);
            }
        }

        public override AbstractState ToUnmarked
        {
            get
            {
                return !IsMarked ? this : new ExpandedState(Alias, Tasks, Marking.Unmarked);
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;

            // If parameter cannot be cast to Point return false.
            var p = obj as State;
            if ((Object)p == null) return false;

            // Return true if the fields match:
            return Alias == p.Alias && Marking == p.Marking;
        }

        public override int GetHashCode()
        {
            return Alias.GetHashCode();
        }

        public override string ToString()
        {
            return Alias;
        }
    }
}
