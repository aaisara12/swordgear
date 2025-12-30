# Code Notes

This is a record of the principles that guide my code architecture decisions.

I'm keeping this here to make sure I'm consistent with my own principles, and to help others understand the reasoning behind my code structure.

This isn't going to be an organized document. It's just going to be a brain dump of my thoughts on code architecture.

## What to do when we encounter an error

When it comes to error handling in code, there are three main strategies I use:

### Throw an exception!
Exceptions are a way to absolutely guarantee that code will not proceed if some precondition is not met.
They are extreme measures to stop code execution. This is great for catching bugs quickly.

Of course, there's always the possibility that one such exception could end up in runtime code, but 
that's the tradeoff - throwing an exception makes the bug more obvious and thus easier to catch for
developers, but it also makes the bug more obvious for players.

Exceptions also have a place for when we simply have no idea what to do if we encounter
a certain situation. We could argue it's better than us trying to handle it gracefully in some vague way
because we literally would not be able to provide any helpful information in any error log or UI we write
and would just be spreading potential misinformation that could make it harder to resolve the bug.

Exceptions also guarantee that the caller cannot use the return value of the method.
You could also achieve a similar effect by returning null or some sentinel value, 
but exceptions absolutely guarantee that the caller cannot proceed as if nothing happened.
It's an extreme case of defensive programming.

### Handle it ourselves

We do not bother the caller with any information about the error because we have taken it upon
ourselves to handle the error for them. We believe that we have the context to handle the error
appropriately, so we do so and return a valid value to the caller.

We believe we will do a better job at handling the error than the caller would.

Note that the caller will be blissfully unaware that an error occurred unless they check the logs.
The caller may continue to use this default value as if nothing happened. Thus, if you think that
the caller would benefit from knowing that an error occurred, then maybe you should defer handling to them.

### Let the caller handle it

If we can't come up with a blanket solution to handle the error that makes sense for all callers of a method,
then we can let the caller decide how to handle failure.

#### TryMethods

TryMethods are methods that return a boolean indicating success or failure, and use an out parameter to return a value if successful.
They are a way to handle errors without throwing exceptions or logging errors. Well, it's more like we defer whether we should
log or throw an exception to our caller. 

So why would we want to do this instead of just handling things at our level?

Well there are a few reasons:
* The caller might have more context about whether the failure is critical or not. For example, it could be that the caller
is waiting for us to be initialized and would simply like to try again later.
* The caller might have a fallback plan if we fail and would like to handle it.

So basically, we can use TryMethods to give our callers more control over how to handle failures, especially if
we don't have the context to know if a failure is critical or not. 

Of course, we shouldn't overuse TryMethods. If a method is expected to always succeed and failure is truly exceptional,
then we should throw an exception or log an error instead. 

Oftentimes, TryMethods are seen when failure is not uncommon and maybe a somewhat expected
occurrence that the caller should have logic to handle. For example, the TryRaycast method.

Remember that TryMethods are passing on the problem to someone else,
not solving it for them.

#### Return Null

I think of returning null as another form of TryMethod that's a little more dangerous because it relies on the caller to check for null.
However, a null value communicates failure without needing an extra boolean return value.

If the caller forgets to check for null, well then you would be back at the "throw an exception" approach.

### Don't handle it

Consider whether the data you're working with is even an erroneous state at all. Is it valid that we might end up in this state?
Edge cases are not the same as errors.

Remember that errors are only errors if they prevent us from fulfilling our contract to the caller.

### Deciding what approach to use

Consider these points:
* How likely it is that the error could occur?
  * If the error will occur frequently, then consider whether it's actually just an edge case that we don't have the context
  to determine how to handle. If so, consider deferring handling to the caller.
* Based on the information at my current scope, can I come up with a blanket solution to handle the error that makes sense for all callers of this method?
  * If so, handle it ourselves.
  * If not, consider deferring handling to the caller (or throwing an exception)
* Is it user-facing code? 
  * I almost never throw exceptions due to the negative player experience it can cause (unless I'm super super sure that it will practically never happen, so sure that I'm willing to be lazy and not worry about more complicated error handling)
* Is it developer-facing code (like editor tools or unit tests)?
  * I'm pretty fine with exceptions here. Remember
    that some exceptions we get for free like index out of range exceptions, null reference exceptions, etc. so
    it can save us work to just let those exceptions happen instead of preemptively checking for them.
* Does the error prevent me from fulfilling my contract to the caller?
  * If so, throw an exception or use a TryMethod to indicate failure.
  * If not, handle it ourselves or fail silently.
  * In my mind, callback code generally goes well with failing silently because:
    * It has no contract to fulfill. It has no responsibility to do... anything really.
    * It may be one of many callbacks being called in sequence by the event invoker, so throwing an exception or writing complicated code 
    would prevent other callbacks from being called in a timely manner.
    * By nature, callback code happens without any regard for the handler's current state. It should be expected
    that the handler might not be in a state to do anything meaningful when the callback is invoked, and thus
    the handler should just do nothing.
    * You might argue that we could miss critical errors this way, but I think that if the error is critical,
    then we probably would have handled it earlier in the code flow before reaching the callback. Of course, if there
    is absolutely no way to handle the error earlier, then we can always try to handle things here.

In Unity, serialized dependencies can always be null if we forget to assign them in the editor.
It's not unfathomable that a developer could forget to assign a dependency, so in this case, I log an error instead of throwing an exception.

In general, there are very few cases where I would throw an exception in Unity runtime code.
My mentor told me that one big downside of exceptions is that they completely throw a wrench in the
code flow, making it hard to reason about what the code will do next and thus it's exponentially more
difficult to know how the program will behave after that point. This means the player experience
is left up to chance, which is never a good thing. However, if you're in a unit test or something that
will never reach the player, then exceptions basically lose their downside of being completely game breaking
because only developers will see them.

## Object Oriented versus Functional Programming

Functional programming is basically keeping data separate from the functions that operate on that data.
Oftentimes, I find myself trying to write code that falls on the functional side due to its lack of side effects and easier testability.
There's less possible confusion about what a function will do. It's straightforward, and if there's a bug, it's because
the core logic is wrong, not because some other part of the code changed some state that this function relies on.

HOWEVER, what I've found is that pure functional programming means passing in a lot of parameters to functions, including 
parameters that might be difficult for the caller to have access to.

This is the beauty of object-oriented programming. The caller doesn't need to know about all the dependencies of a method.
On one hand, this means we can get data that changes without us knowing (i.e. side effects), making it harder to follow the
overall effect of a method. But on the other hand, it means we have method signatures that don't get blown up with 100 different
parameters, 90% of which you as the caller don't really want to be thinking about. In this sense, OOP can help encourage
separation of concerns, because the caller only needs to care about the high-level operation they want to perform.

One thing I've noticed is that because Unity and C# are primarily object-oriented, it's often easier to follow OOP principles
than to try to shoehorn functional programming into the paradigm. Unity has a functional variant called ECS (Entity Component System)
that makes code easier to write and understand logic from a functional perspective.

Object-oriented programming and functional programming are fundamentally at odds with each other. You can't strictly adhere
to both principles at the same time. OOP is a about letting objects take care of themselves - trusting that they will perform their
end of the contract and manage side effects responsibly. Functional programming is about taking all the responsibility of the
operation into your own hands, making sure that you know exactly what data is being used and how. You can apply an analogy of
work delegation here - OOP is like being a manager who delegates tasks to employees and trusts them to get the job done. 
Functional programming is like being a mastermind who instructs employees to strictly follow his detailed plan exactly. 
The manager is less in control of the details, but can focus on the big picture. The mastermind has more control over the details,
but can drown in the stress of micromanaging every last detail. It's important to understand how much you value readability versus precision.

At the end of the day, my main objective is to write code that is easy to understand, maintain, and extend. If that means
hiding a bunch of parameters behind an object to keep the method signature clean, and readable for the team, then so be it.

## Decoupling

Decoupling is the principle of reducing dependencies between different parts of the code.

We do it so that when we change one part of the code, we don't have to change other parts of the code that depend on it.
This makes it much less of a pain to make changes to the codebase, and makes it easier to test parts of the code in isolation
due to the ability to swap in dummy implementations of dependencies.

However, it's important to note that when we apply patterns to decouple code, we aren't actually
reducing dependencies in total - we're just shifting dependencies around such that we reduce
the strength of dependencies where it matters.

When we reduce the strength of a dependency, we broaden the range of inputs we can accept without breaking
the code. This means a block of code can be applied in more situations without requiring changes on its end.


#### A word of caution
Note that there's a hidden cost that comes with decoupling - complexity. Think of direct method calls that take
explicit dependencies as the natural order of things. When we introduce patterns to decouple code,
we're expending energy to break the natural order of things and redirect code flow through our own
man-made mechanisms that can often make it harder to follow what's going on - it's no longer a simple
flow of calling a method.

More complexity means that when data isn't flowing as expected, it's harder to trace where the data is getting
lost or corrupted. 

Net dependency reduction might be 0, but we pretty much always incur some complexity cost. Therefore, it's not
always the right decision to decouple code.

### Object Oriented Design

The basis of object-oriented design is to have objects take care of themselves. This means that objects are responsible for managing their own state and behavior.
Therefore, classes will naturally HIDE dependencies from users of the class by encapsulating them behind private fields.

This is a form of decoupling because the user of the class doesn't need to know about all the dependencies
of the class. 

Here's an example of some code that is not completely object-oriented:

```
public static void PurchaseItem(Item item, PurchasingAgent agent, Inventory inventory)
{
    agent.ChargePlayer(item.Price);
    inventory.AddItem(item);
}
```

Here's the same code written in a more object-oriented way:

```
public class Shop
{
    private PurchasingAgent _agent;
    private Inventory _inventory;

    public Shop(PurchasingAgent agent, Inventory inventory)
    {
        _agent = agent;
        _inventory = inventory;
    }

    public void PurchaseItem(Item item)
    {
        _agent.ChargePlayer(item.Price);
        _inventory.AddItem(item);
    }
}
```

Notice how the user of the `Shop` class doesn't need to know about the `PurchasingAgent` or `Inventory` dependencies.
It doesn't need to know how to acquire them or initialize them or clean them up or anything! 

Notice how we could swap out the `PurchasingAgent` or `Inventory` dependencies for completely different types
like `WebStore` or even `Dictionary<string, Item>` and the caller of `Shop.PurchaseItem` wouldn't need to change at all.

Remember how I mentioned that decoupling comes with a complexity cost? It's because object-oriented design inherently
complicates data flow by hiding dependencies behind private fields that unit tests become more complicated for 
object-oriented classes. We have to set up the dependencies before we can test the class, and we have to make sure
that the dependencies are in the right state before we can test the class.

Unit tests rely on being able to control all the inputs to a method in order to verify that the outputs are correct.
When dependencies are hidden behind private fields, it becomes more complicated to control those inputs.

### Interfaces

Interfaces allow us to hide the concrete implementation of a dependency from the consumer of that dependency.

Suppose we have `ClassA` that depends on `ClassB`. If we introduce an interface `IClassB` that `ClassB` implements,
then `ClassA` can depend on `IClassB` instead of `ClassB`. This means that we can swap out `ClassB` for any other class that implements `IClassB` - for example, `ClassBmock` for testing purposes.

Notice that we haven't reduced the number of dependencies. We've just shifted `ClassA`'s dependency from `ClassB` to `IClassB`.

So if we haven't reduced the number of dependencies, why do we bother with interfaces?

Notice that `IClassB` is a much weaker dependency than `ClassB`. Weaker dependencies mean we can change more without breaking
the code. The prime example being that we can swap out `ClassB` for `ClassBmock` without changing `ClassA` at all.

I like to think of using interfaces as fitting the strength of a dependency to the needs of the consumer.
Recognize that in using an interface, we LOSE some functionality that the concrete class might have.
However, if the consumer doesn't need that functionality, then it's a win because we've reduced the strength of the dependency.
It's like we're trading power for flexibility. If we don't need the power, then it's a good trade.


### Events

Events allow us to hide who's consuming data from the provider of that data.

In this sense, we've FURTHER reduced the strength of the dependency between the provider and consumer.
The provider ONLY must know the type of data it is providing. It has no knowledge of who is consuming that data
AT ALL. It could be another class, it could be 10 different classes, it could be no classes at all.

The provider is basically just coupled to a function signature and that's it. Not even a concrete interface.

However, this doesn't come for free.
We've shifted the strength of the  dependency to the consumers - THEY now need to know about the provider
in order to subscribe to the event. 

We've gone from the provider having to know about the consumer to the consumer having to know about the provider.

A net reduction of 0!

With this in mind, why do we bother with events?

As always, it's about reducing the strength of dependencies where it matters. In general, we want to reduce
the strength of dependencies for classes that are more likely to change. In this case, the consumer
can be quite literally anything that wants the data. It could be a UI element, it could be a gameplay system,
it could be a debug logger. The provider doesn't care and therefore doesn't need to change what it's doing
despite all these things changing in all shapes and forms! So long as the data type of the event
remains the same, the provider is unaffected.

Consider this example that uses events to decouple the provider and consumer:

```

class Consumer
{
    void OnEnable()
    {
        // Notice how we have to know about the Provider to subscribe to its event
        Provider.SomeEvent += Use;
    }

    void OnDisable()
    {
        Provider.SomeEvent -= Use;
    }

    void Use(Data data)
    {
        // Do something with the data
    }
}

class Provider
{
    public static event Action<Data> BroadcastDataEvent;

    void BroadcastData()
    {
        Data data = new Data();
        
        // Notify consumers
        BroadcastDataEvent?.Invoke(data);
    }
}
```

Now compare that to this example that does NOT use events:

```

interface IConsumer
{
    void Use(Data data)
    {
        // Do something with the data
    }
}

class Provider
{
    List<IConsumer> consumers;

    void BroadcastData()
    {
        Data data = new Data();

        // Notice how we have to know about the consumer type to call its method
        foreach (var consumer in consumers)
        {
            consumer.Use(data);
        }
    }
}
```


