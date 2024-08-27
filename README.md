# FlowDance
FlowDance aims to address several critical aspects in the context of microservices architecture. Let's delve into each of these goals:

**Support Inter-service Communication Between Microservices (Database-per-Service Pattern)**:
    In a microservices architecture, each service often manages its own database. The **Database-per-Service Pattern** encourages this separation.
    By adopting this pattern, services can communicate with each other through well-defined APIs, avoiding direct database access.
    This approach enhances modularity, scalability, and isolation, allowing services to evolve independently.

**Replacing Distributed Transactions Calls supported by MSDTC**:
    MSDTC (Microsoft Distributed Transaction Coordinator) is commonly used for distributed transactions across multiple databases.
    However, MSDTC introduces complexity, performance overhead, and potential deadlocks.
    FlowDance proposes a shift towards synchronous RPC (Remote Procedure Call) communication.
    Services share a **Correlation ID / Trace ID** to track related requests across the system.
    Instead of distributed transactions, services coordinate their actions using synchronous calls, simplifying the architecture.
    While strong consistency is essential in some business cases, FlowDance aims to minimize the need for distributed transactions.

**Moving Away from Strong Consistency to Eventual Consistency Using the Compensating Transaction (or Compensating Action) Pattern**:
    In distributed systems, achieving strong consistency (ACID properties) across all services can be challenging.
    FlowDance embraces **eventual consistency**, where operations may temporarily yield inconsistent results.
    The **Compensating Transaction Pattern** comes into play when a step in a process fails.
    If a step cannot be fully rolled back (e.g., due to concurrent changes), compensating transactions undo the effects of previous steps.
    This pattern ensures that the system eventually converges to a consistent state, even after partial failures.

 # Documentation and Examples

For more info please see [FlowDance.Documentation](https://olahallvall.github.io/FlowDance.Documentation/)

Here are some sample apps [FlowDance.Examples](https://github.com/olahallvall/FlowDance.Examples)

# You need
Docker Desktop

# Inspiration
- Compensating Action - https://learn.microsoft.com/en-us/azure/architecture/patterns/compensating-transaction
- Distributed Transactions with the Saga Pattern - https://dev.to/willvelida/the-saga-pattern-3o7p

# Get started
 Install Docker Desktop and start it up.
 
 Download the files https://github.com/olahallvall/FlowDance/blob/master/Setup/docker-compose.yml and 
 https://github.com/olahallvall/FlowDance/blob/master/Setup/db-script.sql to a local folder.
 
 Open a command prompt (not Powershell or Linux) in that folder and run the commands: 
 
 **docker-compose up -d**
 
 Wait until both RabbitMQ and SQL Server has started.
 
 Run the commands: 
 
 **docker exec rabbitmq rabbitmq-plugins enable rabbitmq_stream**
 
 **docker exec rabbitmq rabbitmqadmin declare queue --vhost=/ name=FlowDance.SpanCommands durable=true**
 
 **docker exec rabbitmq rabbitmqadmin declare queue --vhost=/ name=FlowDance.SpanEvents durable=true**
 
 **docker exec -i mssql /opt/mssql-tools/bin/sqlcmd -S . -U SA -P "Admin@123" < db-script.sql**
  
 Restart the container **flowdance** in Docker Desktop. 
 
 Here are some sample apps [FlowDance.Examples](https://github.com/olahallvall/FlowDance.Examples)

