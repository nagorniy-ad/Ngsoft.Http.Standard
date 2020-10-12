# Ngsoft.Http.Standard
## Common usage examples
All examples targets to console application.
### GET
```c#
var url = new Uri("https://jsonplaceholder.typicode.com/posts/1");
HttpResponseMessage response = await HttpBuilder
  .Create(url)
  .SetMethod(HttpMethod.Get)
  .RequestAsync();
string data = await response.Content.ReadAsStringAsync();
Console.WriteLine(data);
Console.ReadLine();
```
### POST
```c#
var url = new Uri("https://jsonplaceholder.typicode.com/posts");
string body = JsonConvert.SerializeObject(obj);
HttpResponseMessage response = await HttpBuilder
  .Create(url)
  .SetMethod(HttpMethod.Post)
  .SetBody(body)
  .SetMediaType("application/json")
  .RequestAsync();
string data = await response.Content.ReadAsStringAsync();
Console.WriteLine(data);
Console.ReadLine();
```
