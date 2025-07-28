namespace FSMTest
{
    using System;
    using System.Collections.Generic;


    public delegate bool TransitionCondition(object[] context);

    public enum State
    {
        Start,
        InProgress,
        Completed,
        Error
    }

    public class TransitionKey : IEquatable<TransitionKey>
    {
        public State FromState { get; }
        public State ToState { get; }
        public Delegate Condition { get; }
        public object[] Arguments { get; }

        public TransitionKey(State fromState, 
            State toState, 
            Delegate condition, 
            params object[] arguments) {
            FromState = fromState;
            ToState = toState;
            Condition = condition ?? throw new ArgumentNullException(nameof(condition));
            Arguments = arguments ?? Array.Empty<object>();
        }

        public override bool Equals(object? obj) {
            return obj is TransitionKey other && Equals(other);
        }

        public bool Equals(TransitionKey? other) {
            if (other == null ||
                FromState != other.FromState ||
                ToState != other.ToState ||
                Condition != other.Condition)
                return false;

            if (Arguments.Length != other.Arguments.Length)
                return false;

            for (int i = 0; i < Arguments.Length; i++) {
                if (!Equals(Arguments[i], other.Arguments[i]))
                    return false;
            }
            return true;
        }

        public override int GetHashCode() {
            int hash = HashCode.Combine(FromState, ToState, Condition);
            foreach (var arg in Arguments) {
                hash = HashCode.Combine(hash, arg);
            }
            return hash;
        }
    }
    public class StateMachine
    {
        private State _currentState;
        public State CurrentState => _currentState;

        public event Action<State> StateChanged;

        private readonly Dictionary<TransitionKey, TransitionCondition> _transitionConditions;

        public StateMachine(State initialState) {
            _currentState = initialState;
            _transitionConditions = new Dictionary<TransitionKey, TransitionCondition>();
        }

        public void AddCondition(State fromState, State toState, TransitionCondition condition, params object[] arguments) {
            var key = new TransitionKey(fromState, toState, condition, arguments);

            if (_transitionConditions.ContainsKey(key)) {
                throw new InvalidOperationException($"A condition for transition {fromState} -> {toState} already exists with the same condition and arguments.");
            }

            _transitionConditions[key] = condition;
        }

        public bool TryTransition(State newState, params object[] context) {
            foreach (var entry in _transitionConditions) {
                var key = entry.Key;
                var condition = entry.Value;

                if (key.FromState == _currentState && key.ToState == newState && MatchArguments(key.Arguments, context)) {
                    if (condition(context)) {
                        _currentState = newState;
                        StateChanged?.Invoke(_currentState);
                        return true;
                    }
                }
            }
            return false;
        }

        private bool MatchArguments(object[] expectedArgs, object[] providedArgs) {
            if (expectedArgs.Length != providedArgs.Length)
                return false;

            for (int i = 0; i < expectedArgs.Length; i++) {
                if (!Equals(expectedArgs[i], providedArgs[i]))
                    return false;
            }
            return true;
        }
    }

    internal class Program
    {
        static void Main(string[] args) {
            var stateMachine = new StateMachine(State.Start);

            stateMachine.StateChanged += state => 
                Console.WriteLine($"State changed to: {state}");

            // Define some conditions
            TransitionCondition isConditionMet = context =>
            {
                return true; // Assume the condition is met for simplicity
            };
            int test = 1;
            string testKey = "test";
            int fail = 2;
            string failKey = "fail";
            // Add transitions with specific arguments
            stateMachine.AddCondition(State.Start, 
                    State.InProgress, isConditionMet, 1, "test");
            stateMachine.AddCondition(State.Start, 
                State.Error, isConditionMet, 2, "fail");

            Console.WriteLine("Initial state: " + stateMachine.CurrentState);
            
            // Trigger transitions
            Console.WriteLine("Triggering transition to 'InProgress' " +
                "with arguments (1, \"test\")...");
            
            if (stateMachine.TryTransition(State.InProgress, 1, "test")) {
                Console.WriteLine("Transition successful.");
            }
            else {
                Console.WriteLine("Transition failed.");
            }

            Console.WriteLine("Triggering transition to 'Error' " +
                "with arguments (2, \"fail\")...");
            
            if (stateMachine.TryTransition(State.Error, 2, "fail")) {
                Console.WriteLine("Transition successful.");
            }
            else {
                Console.WriteLine("Transition failed.");
            }

            Console.WriteLine("Attempting invalid transition " +
                "with arguments (3, \"other\")...");
            
            if (stateMachine.TryTransition(State.InProgress, 3, "other")) {
                Console.WriteLine("Transition successful.");
            }
            else {
                Console.WriteLine("Transition failed.");
            }
        }
    }
}
