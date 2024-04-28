# FlowDance
FlowDance aims to address several critical aspects in the context of microservices architecture. Let's delve into each of these goals:

**Support Interservice Communication Between Microservices (Database-per-Service Pattern)**:
    In a microservices architecture, each service often manages its own database. The **Database-per-Service Pattern** encourages this separation.
    By adopting this pattern, services can communicate with each other through well-defined APIs, avoiding direct database access.
    This approach enhances modularity, scalability, and isolation, allowing services to evolve independently.

**Replacing Distributed Transactions Calls Driven by MSDTC with Synchronous RPC-Calls Sharing a Correlation ID/Trace ID**:
    MSDTC (Microsoft Distributed Transaction Coordinator) is commonly used for distributed transactions across multiple databases.
    However, MSDTC introduces complexity, performance overhead, and potential deadlocks.
    FlowDance proposes a shift towards synchronous RPC (Remote Procedure Call) communication.
    Services share a **Correlation ID** to track related requests across the system.
    Instead of distributed transactions, services coordinate their actions using synchronous calls, simplifying the architecture.
    While strong consistency is essential in some business cases, FlowDance aims to minimize the need for distributed transactions.

**Moving Away from Strong Consistency to Eventual Consistency Using the Compensating Transaction Pattern**:
    In distributed systems, achieving strong consistency (ACID properties) across all services can be challenging.
    FlowDance embraces **eventual consistency**, where operations may temporarily yield inconsistent results.
    The **Compensating Transaction Pattern** comes into play when a step in a process fails.
    If a step cannot be fully rolled back (e.g., due to concurrent changes), compensating transactions undo the effects of previous steps.
    This pattern ensures that the system eventually converges to a consistent state, even after partial failures.

## Where you might be today?
The team(s) has been working to split the monolith or at least some steps in that direction. To uphold strong Consistency the microservices use Distributed Transactions Calls Driven by MSDTC.   

![Distributed monolith](Docs/distributed-monolith.png)

In the picure below shows how easy a call chain gets created in the system. 
The user is attempting to book a trip that includes a car rental, hotel reservation, and flight.
The solution employs a microservices architecture, where each component (car, hotel, and flight) has its own dedicated microservice. These microservices are seamlessly integrated using a Distributed Transaction Coordinator (DTC) session.

![Synchronous choreography-based call chains](Docs/synchronous-choreography-based-call-chains.png)

So how does FlowDance help us out when we still want to base our solution on synchronous RPC-Calls but leaving MSDTC behind?
Remember that FlowDance wants to support communication between microservices based on synchronous RPC-calls. Event-driven architecture is out of scoop here.

In short - by replacing **System.Transactions.TransactionScope** with **FlowDance.Client.CompensationSpan** you leaves the world of strong consistency into eventual consistency.

![Synchronous choreography-based call chains supported by FlowDance](Docs/synchronous-choreography-based-call-chains-with-flowdance.png)

In center of **FlowDance**, there is something called a **CompensationSpan**. A **CompensationSpan** carries the information for how a transaction can be compensated.
A **CompensationSpan** is initialized using the **SpanOpened** event and closed using the **SpanClosed** event. The image below illustrates a **CompensationSpan** with a blue bracket.

The initial CompensationSpan is called the Root Span, and it serves as the starting point for subsequent calls. Subsequent CompensationSpans share the same Correlation ID as the Root Span.
When a SpanEvent is created, it´s stored in a **RabbitMQ Stream**. A **Stream** is a persistent and replicated data structure that models an append-only log with non-destructive consumer semantics. 
Unlike traditional queues, which delete messages once consumed, streams allow consumers to attach at any point in the log and read from there. They provide a powerful way to manage and process messages efficiently. 🐰📜

![Synchronous choreography-based call chains supported by FlowDance](Docs/spans-saved-in-rabbitmq.png)

In the image below, we have replaced `System.Transactions.TransactionScope` with `FlowDance.Client.CompensationSpan`. Instead of using MSDTC, a RabbitMQ is employed to store data related to a Span.

![Synchronous choreography-based call chains supported by FlowDance](Docs/synchronous-choreography-based-call-chains-with-span.png)

## A CompensationSpan in detail

A CompensationSpan ...

### How to work with Spans in code

Here we create a root span. 

```csharp

public void RootCompensationSpan()
{
    var traceId = Guid.NewGuid();

    using (CompensationSpan compSpan = 
            new CompensationSpan("http://localhost/TripBookingService/Compensation", traceId, _loggerFactory))
    {
        /* Perform transactional work here */
        // DoSomething()

        compSpan.Complete();
    }
}

```

Here we create a root span with a inner span. They are sharing the same traceId. 

```csharp
 
public void RootWithInnerCompensationSpan()
{
    var traceId = Guid.NewGuid();

    // The top-most compensation span is referred to as the root span.
    // Root scope
    using (CompensationSpan compSpanRoot = 
            new CompensationSpan("http://localhost/TripBookingService/Compensation", traceId, _loggerFactory))
    {
        /* Perform transactional work here */
        // DoSomething()

        // Inner span
        using (CompensationSpan compSpanInner = 
                new CompensationSpan("http://localhost/CarService/Compensation", traceId, _loggerFactory))
        {
            /* Perform transactional work here */
            // DoSomething()

            compSpanInner.Complete();
        }
                 
        compSpanRoot.Complete();
    }
}
```

Semantic Rollback

**Components of FlowDance**:
    - **Client Library**: The prima ballerina, guiding services in their graceful movements.
    - **Back-End Service**: A symphony of RabbitMQ and Microsoft Azure Durable Functions.
        - **Azure Durable Functions**: The conductor, orchestrating communication between services when compensating transaction has to be executed.
        - **RabbitMQ**: Stores the eventdata from each CompensationSpan in a call chain.


Remember, FlowDance isn't just about dancing—it's about orchestrating microservices with grace when compensating transaction has to be executed! 🕺💃

# You need
* Visual Studio 2022 or later
* Azure Functions Core Tools (Azure Functions Core Tools lets you develop and test your functions on your local computer)
 

# Inspiration
* Compensating Action - https://learn.microsoft.com/en-us/azure/architecture/patterns/compensating-transaction
* Distributed Transactions with the Saga Pattern - https://dev.to/willvelida/the-saga-pattern-3o7p

# Get started
