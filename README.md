# FlowDance
FlowDance goals is to support the following scenario:
- Interservice communication between microservices (Database-per-Service Pattern).
- Replacing distributed transactions calls driven by MSDTC with common synchronous RPC-calls sharing a Correlation ID.
- Moving away from strong consistency to eventual consistency using the Compensating Transaction pattern.
- Reducing long-running locks in the database due to distributed transactions based on ACID.      

## Breaking up the distributed monolith or at least one step in that direction
![Distributed monolith](Docs/distributed-monolith.png)

When moving away from an monolith to an microservices solution it's easy to ends up with something like the picture above. 
Synchronous choreography-based call chains that´s cuts through the entire solution. The solution upholds strong consistency by using distributed transaction based on MSDTC throughout the complete solution.

The picure below showes how easy a call chain gets created in the solution. One more call can't hurt that bad :) 
![Synchronous choreography-based call chains](Docs/synchronous-choreography-based-call-chains.png)


FlowDance takes an aim at reducing or eliminate the need of distributed transaction between microservices based on MSDTC. There still can be some business case needing strong consistency.

By extracting data from synchronous choreography based call chains into a Saga, FlowDance can support Compensating Transaction pattern.
      

FlowDance consist of a Client library and back-end service based on RabbitMQ and Microsoft Azure Durable Functions.

# You need
* Visual Studio 2022 or later
* Azure Functions Core Tools (Azure Functions Core Tools lets you develop and test your functions on your local computer)
 

# Inspiration
* Compensating Action - https://learn.microsoft.com/en-us/azure/architecture/patterns/compensating-transaction
* Distributed Transactions with the Saga Pattern - https://dev.to/willvelida/the-saga-pattern-3o7p

# Get started
