# FlowDance
FlowDance goals is to support the following scenario:
- Interservice communication between microservices (Database-per-Service Pattern).
- Replace distributed transactions calls driven by MSDTC with common synchronous RPC-calls sharing a Correlation ID.
- Moving away from strong consistency to eventual consistency using the Compensating Transaction pattern.      

![Saga example](Docs/distributed-monolith.png)
When moving away from an monolith to an microservices solution it's easy to ends up with something like this picture.
We uphold strong consistency by using distributed transaction throughout the complete solution.
FlowDance takes a aim at reducing or eliminate the need of distributed transaction between microservices based on MSDTC.

FlowDance consist of a Client library and back-end service based on RabbitMQ and Microsoft Azure Durable Functions.

# You need
* Visual Studio 2022 or later
* Azure Functions Core Tools (Azure Functions Core Tools lets you develop and test your functions on your local computer)
 

# Inspiration
* Compensating Action - https://learn.microsoft.com/en-us/azure/architecture/patterns/compensating-transaction
* Distributed Transactions with the Saga Pattern - https://dev.to/willvelida/the-saga-pattern-3o7p

# Get started
