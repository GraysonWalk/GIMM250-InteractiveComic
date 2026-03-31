using System;

namespace StateMachine
{
    /// <summary>
    /// A simple predicate implementation which doesn't require storing any state of its own
    /// It simply wraps a Func delegate to evaluate the condition.
    /// </summary>
    public class FuncPredicate : IPredicate
    {
        readonly Func<bool> _func; // Store the function to evaluate
        
        public FuncPredicate(Func<bool> func) // Constructor takes the stored Func<bool> to define the condition
        {
            this._func = func;
        }

        public bool Evaluate() => _func.Invoke(); // Evaluate the condition by invoking the stored function delegate
        
        /*
         * Because this expression is so simple, an expression-bodied member can be used to define it in a single line.
         * It is the same as writing:
         * public bool Evaluate()
         * {
         *     return func.Invoke();
         * }
         */
    }
}

/*
 * In C# "A delegate is a type that represents references to methods with a particular parameter list
 * and return type. When you instantiate a delegate, you can associate the delegate instance with any
 * method that has a compatible signature and return type. You can invoke (or call) the method through
 * the delegate instance."
 * https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/delegates/
 *
 * A Predicate is a delegate that takes no parameters and returns a boolean value.
 * A Func is a delegate that may take parameters and returns a value.
 *
 * Delegates are type safe and allow you to treat methods as objects.
 */