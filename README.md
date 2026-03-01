# C# REST API + WebSocket Chat App

Simple REST API and WebSocket Chat application built with **ASP.NET Core 8** on Linux (WSL Ubuntu).

---

## Tech Stack

- **Runtime**: .NET 8
- **Framework**: ASP.NET Core 
- **WebSocket**: Built-in ASP.NET Core WebSocket middleware
- **Deployment**: Linux (WSL Ubuntu / VPS)
- **Reverse Proxy**: Nginx + SSL (Let's Encrypt)

---

## Features

### REST API
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/items` | Get all items |
| GET | `/api/items/{id}` | Get item by ID |
| POST | `/api/items` | Create new item |
| PUT | `/api/items/{id}` | Update item by ID |
| DELETE | `/api/items/{id}` | Delete item by ID |

> API documentation available at `/swagger`

### WebSocket Chat
- Real-time two-way communication via `/ws`
- Multi-client support with broadcast messaging
- Join/leave notifications
- Simple chat UI via browser (`/index.html`)

---

## Prerequisites

- .NET SDK 8.0
- Linux (WSL Ubuntu or VPS)

---

## Getting Started

### 1. Install .NET SDK

```bash
apt install dotnet-sdk-8.0
```

### 2. Clone / Copy Project

```bash
cd ~
# copy your project folder here
cd RestApiApp
```

### 3. Run Locally

```bash
dotnet run
```

App will run on `http://localhost:5189`

### 4. Run on VPS (accessible from outside)

```bash
dotnet run --urls "http://0.0.0.0:5189"
```

---

## API Usage Examples

### Get all items
```bash
curl http://localhost:5189/api/items
```

### Create item
```bash
curl -X POST http://localhost:5189/api/items \
  -H "Content-Type: application/json" \
  -d '{"name": "Item D", "price": 40000}'
```

### Update item
```bash
curl -X PUT http://localhost:5189/api/items/1 \
  -H "Content-Type: application/json" \
  -d '{"name": "Item A Updated", "price": 15000}'
```

### Delete item
```bash
curl -X DELETE http://localhost:5189/api/items/1
```

---

## WebSocket Chat

1. Open browser and go to `http://localhost:5189/index.html`
2. Enter your username and click **Gabung Chat**
3. Open another tab, enter a different username
4. Start chatting in real-time!

---

## Project Structure

```
RestApiApp/
├── Program.cs          # Main app - REST API + WebSocket logic
├── wwwroot/
│   └── index.html      # WebSocket Chat UI
├── appsettings.json
└── RestApiApp.csproj
```

---

## Deployment with Nginx + SSL

### Install Nginx & Certbot
```bash
apt install nginx certbot python3-certbot-nginx -y
```

### Nginx Config (`/etc/nginx/sites-available/restapi`)
```nginx
server {
    listen 80;
    server_name your-domain.duckdns.org;

    location / {
        proxy_pass http://localhost:5189;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

### Enable & Restart Nginx
```bash
ln -s /etc/nginx/sites-available/restapi /etc/nginx/sites-enabled/
nginx -t
systemctl restart nginx
```

### Generate SSL Certificate
```bash
certbot --nginx -d your-domain.duckdns.org
```

After this, your app will be accessible via `https://your-domain.duckdns.org`

---

## Notes

- Data is stored **in-memory** — restarting the app will reset data to defaults
- WebSocket endpoint: `/ws`
- Swagger UI: `/swagger`
- Chat UI: `/index.html`