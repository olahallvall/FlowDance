**FlowDance** aims to address several critical aspects in the context of microservices architecture. Let's delve into each of these goals:

1. **Interservice Communication Between Microservices (Database-per-Service Pattern)**:
    - In a microservices architecture, each service often manages its own database. The **Database-per-Service Pattern** encourages this separation.
    - By adopting this pattern, services can communicate with each other through well-defined APIs, avoiding direct database access.
    - This approach enhances modularity, scalability, and isolation, allowing services to evolve independently.

2. **Replacing Distributed Transactions Calls Driven by MSDTC with Common Synchronous RPC-Calls Sharing a Correlation ID**:
    - **MSDTC (Microsoft Distributed Transaction Coordinator)** is commonly used for distributed transactions across multiple databases.
    - However, MSDTC introduces complexity, performance overhead, and potential deadlocks.
    - FlowDance proposes a shift towards synchronous RPC (Remote Procedure Call) communication.
    - Services share a **Correlation ID** to track related requests across the system.
    - Instead of distributed transactions, services coordinate their actions using synchronous calls, simplifying the architecture.

3. **Moving Away from Strong Consistency to Eventual Consistency Using the Compensating Transaction Pattern**:
    - In distributed systems, achieving strong consistency (ACID properties) across all services can be challenging.
    - FlowDance embraces **eventual consistency**, where operations may temporarily yield inconsistent results.
    - The **Compensating Transaction Pattern** comes into play when a step in a process fails.
    - If a step cannot be fully rolled back (e.g., due to concurrent changes), compensating transactions undo the effects of previous steps.
    - This pattern ensures that the system eventually converges to a consistent state, even after partial failures.

4. **Reducing Long-Running Locks in the Database Due to Distributed Transactions Based on ACID**:
    - Traditional distributed transactions (based on ACID properties) can lead to long-running locks.
    - These locks hinder concurrency and scalability.
    - FlowDance advocates for alternative approaches, such as:
        - **Optimized Locking**: A feature introduced in 2023 that reduces lock memory and the number of locks required for concurrent writes.
        - **Isolation Levels**: Adjusting transaction isolation levels to balance consistency and performance.
        - **Idempotent Commands**: Ensuring that commands can be safely repeated without unintended side effects.
        - **Eventual Consistency**: Accepting occasional inconsistency while allowing parallel processing.

Remember, FlowDance isn't just about dancing—it's about orchestrating microservices with grace! 🕺💃
