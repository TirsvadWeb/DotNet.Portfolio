# Check methods- and Classname convention

```plaintext
check methods- and Classname convention

Helper - static class with pure functions. Very generic, no state, very small
Manager - collection of methods pertaining to some kind of context. No state outside of injected classes. Generally performing some business logic that didn’t fit into a domain model. Small/Med size methods
Mapper - transforms one object to another
Service - does some behavior that has an effect outside of the application or as part of an operational process. generally has state. Uses various previously mentioned classes to perform task. Can have some complex orchestration of logic
Handler - responses to a particular request and executes some business logic (through some combinations of previously mentioned classes)
```