# Code Notes

This is a record of the principles that guide my code architecture decisions.

I'm keeping this here to make sure I'm consistent with my own principles, and to help others understand the reasoning behind my code structure.

This isn't going to be an organized document. It's just going to be a brain dump of my thoughts on code architecture.

## Throwing Exceptions versus Logging Errors versus TryMethods

When it comes to error handling in code, there are three main strategies I use:

### Throwing Exceptions
Exceptions are a way to absolutely guarantee that code will not proceed if some precondition is not met.
They are extreme measures to stop code execution when something is very wrong and
make the runtime throw a huge fit over it! This is great for catching bugs quickly.

Of course, there's always the possibility that one such exception could end up in runtime code, but 
that's the tradeoff - throwing an exception makes the bug more obvious and thus easier to catch for
developers, but it also makes the bug more obvious for players.

### Logging Errors
So when do we log errors instead of throwing exceptions? 
* I gauge how likely it is that the error could occur.
If it's not unfathomable, then that's a point for error logs. 
* If it's runtime game code, I almost always log errors.
* If it's developer-facing code (like editor tools or unit tests), I'm pretty fine with exceptions. Remember
that some exceptions we get for free like index out of range exceptions, null reference exceptions, etc. so
it can save us work to just let those exceptions happen instead of preemptively checking for them.

In Unity, serialized dependencies can always be null if we forget to assign them in the editor.
It's not unfathomable that a developer could forget to assign a dependency, so in this case, I log an error instead of throwing an exception. 

In general, there are very few cases where I would throw an exception in Unity runtime code.
My mentor told me that one big downside of exceptions is that they completely throw a wrench in the
code flow, making it hard to reason about what the code will do next and thus it's exponentially more
difficult to know how the program will behave after that point. This means the player experience
is left up to chance, which is never a good thing. However, if you're in a unit test or something that
will never reach the player, then exceptions basically lose their downside of being completely game breaking
because only developers will see them.

### TryMethods

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
then we should throw an exception or log an error instead. TryMethods are best used when failure is a common and expected
occurrence that the caller can reasonably handle on. Remember that TryMethods are passing on the problem to someone else,
not solving it for them.


### Object Oriented versus Functional Programming

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