# 1.Introduction to Minimal APIs

2012, RESTful APIs were the new trend on the internet and .NET responded to this with a new approach for developing APIs, called ASP.NET Web API. 

Later, in ASP.NET Core these frameworks were unified under the name ASP.NET Core MVC: one single framework with which to develop web applications and APIs.

 in the real world, there are scenarios and use cases where you don’t need all the features of the MVC framework or you have to factor in a constraint on performance

ASP.NET Core 6.0 has filled these gaps with minimal APIs.

# 2.Exploring Minimal APIs and Their Advantages

## Routing

将请求转交到对应的端点，url可带参数

### Route constraints

If, according to the constraints, no route matches the specified path, we don’t get an exception. Instead we obtain a 404 Not Found message, because, in fact, if the constraints do not fit, the route itself isn’t reachable

## Parameter binding

Parameter binding is the process that converts request data (i.e., URL paths, query strings, or the body) into strongly typed parameters that can be consumed by route handlers

### Special bindings

在基于controller的程序中，继承`Microsoft.AspNetCore.Mvc.ControllerBase`的class可以访问一些有关请求的上下文信息： `HttpContext`, `Request`, `Response`, `User`

Minimal API也可以通过binding访问

```c#
app.MapGet("/products", (HttpContext context, HttpRequest req, HttpResponse res, ClaimsPrincipal user) => { });
```

或者用`IHttpContextAccessor`这个接口。

### Custom binding

虽然Minimal API没有`IModelBinderProvider`和`IModelBinder`,但是仍然可以实现自定义绑定