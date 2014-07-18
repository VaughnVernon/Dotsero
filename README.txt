Dotsero
=======

This is a MVP .NET Actor Model toolkit that follows the
Akka API as closely as possible, but as idomatic C#. It
is licensed under Apache 2.0.

Currently supported:
--------------------
1. ActorSystem
   - /user
   - /sys
   - /sys/deadLetters
2. Actor, Props, ActorContext, and ActorRef
   - Any number of typed OnReceive() methods
   - Stack-based Become/Unbecome
3. ActorPath (very lightweight)
4. ActorSelection (currently no wildcards)
5. SupervisorStrategy
   - OneForOneStrategy
   - AllForOneStrategy (not fully implemented)

- Requires Retlang: https://code.google.com/p/retlang/
- Currently seeing approximately 0.5 million messages per second,
  with only two threads, on a Intel i7 Quad Core 4700HQ 2.4 GHz.
- See unit tests for usage.

Currently unsupported (big items missing):
------------------------------------------
1. Configuration
2. EventBus/EventStream
3. Remoting and Clustering (LocalActorRef/RemoteActorRef)
4. Creating and stopping actors is currently not asyncrhonous,
   and the ActorKilledException/Stop messages are currently
   not supported. Further, suspending an actor due to a restart
   currently blocks rather than stashing messages. This is
   because stashing requires another incoming message to cause
   the stashed messages to be delivered. A scheduler would be
   the thing to cause the stash to be emptied.
5. Scheduling (timers)
6. Whatever else is not listed above as supported.
