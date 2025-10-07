# AutofacFilterAsyncContextLoss

Minimal reproducible example demonstrating **loss of ASP.NET async context when using `IAutofacActionFilter`** in **Autofac.WebApi2 6.1.1** under **.NET Framework 4.8**.

---

## Summary

When an `IAutofacActionFilter` awaits an asynchronous operation (e.g., `await Task.Delay()`), the **ASP.NET `SynchronizationContext` and `HttpContext.Current` are `null` inside the target action method**.

Implementing the same logic with `IAutofacContinuationActionFilter` preserves the context as expected.

This behavior contradicts the Autofac Web API docs, which state that regular `IAutofacActionFilter` instances run inside a continuation filter and **should preserve async context**.

---

## Expected vs Actual

**Expected (both interfaces):**
- `HttpContext.Current` and `SynchronizationContext.Current` remain available from the start of the filter through to the controller action.

**Actual with `IAutofacActionFilter`:**
- Valid at the start of `OnActionExecutingAsync`.
- Still valid after `await`.
- **Null** at the first line of the action method.

**Actual with `IAutofacContinuationActionFilter`:**
- Context is preserved at all three points.

---

## Environment

| Component | Version |
|----------|---------|
| .NET Framework | 4.8 |
| ASP.NET Web API 2 | System.Web |
| Autofac | 8.4.0 |
| Autofac.WebApi2 | 6.1.1 |
| Autofac.Extensions.DependencyInjection | 10.0.0 |
| Hosting | IIS Express (also reproduced on full IIS) |
| IDE | Visual Studio 2022 |

---

## How to Run

1. Open `AutofacFilterAsyncContextLoss.sln` in Visual Studio.
2. Run under IIS Express.
3. Hit the routes below (browser/Postman/curl):
   - `http://localhost:<port>/repro`  → uses `IAutofacActionFilter` (shows context loss at action).
   - `http://localhost:<port>/continuation` → uses `IAutofacContinuationActionFilter` (context preserved).
4. Open **View → Output → Show output from: Debug** to see the logs emitted via `System.Diagnostics.Debug.WriteLine`.

---

## Output Samples (abridged)

**`/repro` — using `IAutofacActionFilter`**

```
15:28:10:310	9: before await; HttpContext.Current is null: False; SynchronizationContext.Current is null: False
15:28:13:549	9: after await; HttpContext.Current is null: False; SynchronizationContext.Current is null: False
15:28:13:549	8: at action; HttpContext.Current is null: True; SynchronizationContext.Current is null: True
```

**`/continuation` — using `IAutofacContinuationActionFilter`**

```
15:30:32:330	9: before await; HttpContext.Current is null: False; SynchronizationContext.Current is null: False
15:30:35:319	10: after await; HttpContext.Current is null: False; SynchronizationContext.Current is null: False
15:30:35:319	10: at action; HttpContext.Current is null: False; SynchronizationContext.Current is null: False
```


---

## Project Structure

- `Controllers/ReproController.cs`  
  Logs context at the first line of the action. Bound to the `IAutofacActionFilter` repro.

- `Controllers/ContinuationController.cs`  
  Logs context at the first line of the action. Bound to the continuation filter.

- `Filters/ActionFilter.cs` (`IAutofacActionFilter`)  
  Logs context **before** and **after** an `await Task.Delay(3000)`.

- `Filters/ContinuationFilter.cs` (`IAutofacContinuationActionFilter`)  
  Logs context **before** and **after** an `await Task.Delay(3000)`.

- `WebApiConfig.cs`  
  Registers controllers, Autofac Web API filter provider, and wires each filter to its controller via:
  ```csharp
  builder.RegisterType<ActionFilter>()
         .AsWebApiActionFilterFor<ReproController>()
         .InstancePerRequest();

  builder.RegisterType<ContinuationFilter>()
         .AsWebApiActionFilterFor<ContinuationController>()
         .InstancePerRequest();

---

## Key Observations

* In the `IAutofacActionFilter` path:

  * Context is valid at filter start and after the `await`.
  * Context is **lost only after the transition** from filter to action.

* No `ConfigureAwait(false)`, `Task.Run`, or `ContinueWith` are present anywhere prior to the action.

* Swapping to `IAutofacContinuationActionFilter` with the same async pattern **fully preserves** the context.

---

## Minimal Code Snippets

**Filter exhibiting the issue (`IAutofacActionFilter`):**

```csharp
public class ActionFilter : IAutofacActionFilter
{
    public Task OnActionExecutedAsync(HttpActionExecutedContext ctx, CancellationToken t)
    {
        return Task.CompletedTask;
    }

    public async Task OnActionExecutingAsync(HttpActionContext ctx, CancellationToken t)
    {
        Debug.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: before await; " +
                        $"HttpContext.Current is null: {HttpContext.Current == null}; " +
                        $"SynchronizationContext.Current is null: {SynchronizationContext.Current == null}");

        await Task.Delay(3000);

        Debug.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: after await; " +
                        $"HttpContext.Current is null: {HttpContext.Current == null}; " +
                        $"SynchronizationContext.Current is null: {SynchronizationContext.Current == null}");
    }
}
```

**Filter that works (`IAutofacContinuationActionFilter`):**

```csharp
public class ContinuationFilter : IAutofacContinuationActionFilter
{
    public async Task<HttpResponseMessage> ExecuteActionFilterAsync(
        HttpActionContext ctx, CancellationToken token, Func<Task<HttpResponseMessage>> next)
    {
        Debug.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: before await; " +
                        $"HttpContext.Current is null: {HttpContext.Current == null}; " +
                        $"SynchronizationContext.Current is null: {SynchronizationContext.Current == null}");

        await Task.Delay(3000);

        Debug.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: after await; " +
                        $"HttpContext.Current is null: {HttpContext.Current == null}; " +
                        $"SynchronizationContext.Current is null: {SynchronizationContext.Current == null}");

        return await next();
    }
}
```

**Controller actions (log at first line):**

```csharp
[HttpGet, Route("repro")]
public IHttpActionResult Get()
{
    Debug.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: at action; " +
                    $"HttpContext.Current is null: {HttpContext.Current == null}; " +
                    $"SynchronizationContext.Current is null: {SynchronizationContext.Current == null}");
    return Ok();
}

[HttpGet, Route("continuation")]
public IHttpActionResult Get()
{
    Debug.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: at action; " +
                    $"HttpContext.Current is null: {HttpContext.Current == null}; " +
                    $"SynchronizationContext.Current is null: {SynchronizationContext.Current == null}");
    return Ok();
}
```

---

## Workaround

Use `IAutofacContinuationActionFilter`, which explicitly executes filters via a continuation and (in practice) preserves the ASP.NET async context across the `await` and into the controller action.

---
