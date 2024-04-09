# FlowDance
FlowDance goals is to support the following scenario:
- Breaking up a monolith into subsystem based on the Database-per-Service Pattern.
- Replace distributed transactions calls based on MSDTC with common synchronous RPC-calls (http).
- Moving away from strong consistency to eventual consistency using the Compensating Transaction pattern.      
- Replace System.Transactions class TransactionScope with FlowDance's CompensationScope. 

FlowDance consist of a Client library and back-end service based on RabbitMQ and Microsoft Azure Durable Functions.

# You need
* Visual Studio 2022 or later
* Azure Functions Core Tools (Azure Functions Core Tools lets you develop and test your functions on your local computer)
 

# Inspiration
* Compensating Action - https://learn.microsoft.com/en-us/azure/architecture/patterns/compensating-transaction
* Distributed Transactions with the Saga Pattern - https://dev.to/willvelida/the-saga-pattern-3o7p

# Get started
