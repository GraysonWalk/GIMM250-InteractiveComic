namespace StateMachine
{
    /// <summary>
    /// The interface for predicates for my implementation of the state machine and strategy pattern.
    /// Predicates are functions that return a boolean value, used to determine if certain conditions are met.
    /// </summary>
    public interface IPredicate
    {
        bool Evaluate();
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