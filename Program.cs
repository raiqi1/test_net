using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "REST API + WebSocket Chat", 
        Version = "v1",
        Description = "Simple REST API with CRUD operations and WebSocket Chat"
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "REST API v1");
    c.RoutePrefix = "swagger";
});

// ===== In-memory storage =====
var items = new List<Item>
{
    new Item { id = 1, name = "Item A", price = 10000 },
    new Item { id = 2, name = "Item B", price = 20000 },
    new Item { id = 3, name = "Item C", price = 30000 },
};

// ===== REST API CRUD =====

// GET all
app.MapGet("/api/items", () => Results.Ok(items))
    .WithName("GetAllItems")
    .WithSummary("Get all items")
    .WithDescription("Returns a list of all items");

// GET by id
app.MapGet("/api/items/{id}", (int id) =>
{
    var item = items.FirstOrDefault(i => i.id == id);
    return item is null ? Results.NotFound(new { message = $"Item with id {id} not found" }) : Results.Ok(item);
})
    .WithName("GetItemById")
    .WithSummary("Get item by ID")
    .WithDescription("Returns a single item by its ID");

// POST
app.MapPost("/api/items", (CreateItemRequest body) =>
{
    var newItem = new Item { id = items.Count + 1, name = body.name, price = body.price };
    items.Add(newItem);
    return Results.Created("/api/items", new { message = "Item created!", data = newItem });
})
    .WithName("CreateItem")
    .WithSummary("Create an item")
    .WithDescription("Creates a new item");

// PUT
app.MapPut("/api/items/{id}", (int id, UpdateItemRequest body) =>
{
    var item = items.FirstOrDefault(i => i.id == id);
    if (item is null) return Results.NotFound(new { message = $"Item with id {id} not found" });

    item.name = body.name;
    item.price = body.price;
    return Results.Ok(new { message = "Item updated!", data = item });
})
    .WithName("UpdateItem")
    .WithSummary("Update an item")
    .WithDescription("Updates an existing item by its ID");

// DELETE
app.MapDelete("/api/items/{id}", (int id) =>
{
    var item = items.FirstOrDefault(i => i.id == id);
    if (item is null) return Results.NotFound(new { message = $"Item with id {id} not found" });

    items.Remove(item);
    return Results.Ok(new { message = $"Item with id {id} deleted!" });
})
    .WithName("DeleteItem")
    .WithSummary("Delete an item")
    .WithDescription("Deletes an item by its ID");

// ===== WebSocket Chat =====
var clients = new ConcurrentDictionary<string, (WebSocket ws, string username)>();

async Task Broadcast(string message, string? excludeId = null)
{
    var bytes = Encoding.UTF8.GetBytes(message);
    foreach (var (id, (ws, _)) in clients)
    {
        if (id == excludeId) continue;
        if (ws.State == WebSocketState.Open)
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }
}

async Task SendToOne(WebSocket ws, string message)
{
    var bytes = Encoding.UTF8.GetBytes(message);
    if (ws.State == WebSocketState.Open)
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
}

app.UseStaticFiles();
app.UseWebSockets();

app.Map("/ws", async (HttpContext context) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("WebSocket only.");
        return;
    }

    var ws = await context.WebSockets.AcceptWebSocketAsync();
    var clientId = Guid.NewGuid().ToString();
    var buffer = new byte[1024 * 4];
    string username = "";

    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
    username = Encoding.UTF8.GetString(buffer, 0, result.Count).Trim();

    clients[clientId] = (ws, username);
    Console.WriteLine($"{username} joined!");

    await Broadcast($"[{username} joined the chat]");
    await SendToOne(ws, $"[Welcome {username}! There are {clients.Count} user(s) online]");

    while (ws.State == WebSocketState.Open)
    {
        result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        if (result.MessageType == WebSocketMessageType.Close)
        {
            clients.TryRemove(clientId, out _);
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            await Broadcast($"[{username} left the chat]");
            Console.WriteLine($"{username} disconnected.");
            break;
        }

        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
        Console.WriteLine($"{username}: {message}");
        await Broadcast($"{username}: {message}");
    }
});

app.Run();

class Item
{
    public int id { get; set; }
    public string name { get; set; } = "";
    public int price { get; set; }
}

class CreateItemRequest
{
    public string name { get; set; } = "";
    public int price { get; set; }
}

class UpdateItemRequest
{
    public string name { get; set; } = "";
    public int price { get; set; }
}