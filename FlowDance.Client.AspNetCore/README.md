# FlowDance.Client.AspNetCore
FlowDance.Client.AspNetCore contains a ActionFilterAttribute (CompensationSpanAttribute) that makes iy easy to start/join a CompensationSpan from a Controller-class.

Don't forget to add config to appsettings.json!
```
{
  "StorageProviderType": "RabbitMqStorage",
  "RabbitMqConnection": {
    "HostName": "localhost",
    "Username": "guest",
    "Password": "guest",
    "VirtualHost": "/"
  }
}
```

To start a new 'transaction'.
```csharp
  [CompensationSpan(CompensatingActionUrl = "http://localhost:5112/api/Compensating/compensate", CompensationSpanOption = CompensationSpanOption.RequiresNew)]
  [HttpPost("booktrip")]
  public async Task<IActionResult> BookTrip([FromBody] Trip trip)
  {
      // Access the CompensationSpan instance from the ActionFilter
      var compensationSpan = HttpContext.Items["CompensationSpan"] as CompensationSpan;
        ....

      return Ok(trip);
  }
  ```

  To "join" a existing 'transaction'.
```csharp
  [CompensationSpan(CompensatingActionUrl = "http://localhost:5043/api/Compensating/compensate")]
  [HttpPost("bookcar")]
  public async Task<IActionResult> BookCar([FromBody] Car car)
  {
      // Access the CompensationSpan instance from the ActionFilter
      var compensationSpan = HttpContext.Items["CompensationSpan"] as CompensationSpan;

      // Book a Hotel
      await _hotelService.BookHotel(car.PassportNumber, car.TripId, compensationSpan.TraceId);

      return Ok();
  }
  ```



