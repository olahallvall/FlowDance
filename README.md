**FlowDance** aims to address several critical aspects in the context of microservices architecture. Let's delve into each of these goals:

**Support Interservice Communication Between Microservices (Database-per-Service Pattern)**:
    - In a microservices architecture, each service often manages its own database. The **Database-per-Service Pattern** encourages this separation.
    - By adopting this pattern, services can communicate with each other through well-defined APIs, avoiding direct database access.
    - This approach enhances modularity, scalability, and isolation, allowing services to evolve independently.

**Replacing Distributed Transactions Calls Driven by MSDTC with Common Synchronous RPC-Calls Sharing a Correlation ID**:
    - **MSDTC (Microsoft Distributed Transaction Coordinator)** is commonly used for distributed transactions across multiple databases.
    - However, MSDTC introduces complexity, performance overhead, and potential deadlocks.
    - FlowDance proposes a shift towards synchronous RPC (Remote Procedure Call) communication.
    - Services share a **Correlation ID** to track related requests across the system.
    - Instead of distributed transactions, services coordinate their actions using synchronous calls, simplifying the architecture.
    - While strong consistency is essential in some business cases, FlowDance aims to minimize the need for distributed transactions.

**Moving Away from Strong Consistency to Eventual Consistency Using the Compensating Transaction Pattern**:
    - In distributed systems, achieving strong consistency (ACID properties) across all services can be challenging.
    - FlowDance embraces **eventual consistency**, where operations may temporarily yield inconsistent results.
    - The **Compensating Transaction Pattern** comes into play when a step in a process fails.
    - If a step cannot be fully rolled back (e.g., due to concurrent changes), compensating transactions undo the effects of previous steps.
    - This pattern ensures that the system eventually converges to a consistent state, even after partial failures.

**Where you maybe are now**:
The team(s) has been working to split the monolith or at least some steps in that direction. To uphold strong Consistency the microservices use Distributed Transactions Calls Driven by MSDTC.   
![Distributed monolith](Docs/distributed-monolith.png)

The picure below showes how easy a call chain gets created in the system. One more call can't hurt that bad! Or.. :) 
The use case is a trip book that includes car, hotel and flight. 

![Synchronous choreography-based call chains](Docs/synchronous-choreography-based-call-chains.png)

Remember that FlowDance wants to support Interservice Communication Between Microservices based on synchronous RPC-Calls. 

**Components of FlowDance**:
    - **Client Library**: The prima ballerina, guiding services in their graceful movements.
    - **Back-End Service**: A symphony of RabbitMQ and Microsoft Azure Durable Functions.
        - **Azure Durable Functions**: The conductor, orchestrating communication between services when compensating transaction has to be executed.
        - **RabbitMQ**: Stores the eventdata from each CompensationScope in a call chain based on .

Remember, FlowDance isn't just about dancing—it's about orchestrating microservices with grace when compensating transaction has to be executed! 🕺💃
