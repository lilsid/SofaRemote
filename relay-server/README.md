# SofaRemote Relay Server

WebSocket relay server for SofaRemote - enables remote control across any network (hotel WiFi, cellular, etc).

## Features

- 🔐 Session-based pairing with unique codes
- 🚀 Real-time WebSocket communication
- 👥 Multi-user support (unlimited concurrent sessions)
- 📱 One phone can control multiple PCs
- 💰 Lightweight - runs on Railway free tier
- 📊 Built-in stats dashboard

## Architecture

```
PC App → Relay Server ← Phone App
   ↓         ↓            ↓
Session: WOLF-5821
```

## Deployment (Railway)

1. Create account at https://railway.app
2. Create new project
3. Deploy from GitHub repo
4. Railway auto-detects Node.js and runs `npm start`
5. Get your WebSocket URL: `wss://your-app.railway.app`

## Local Testing

```bash
cd relay-server
npm install
npm start
```

Opens on http://localhost:8080

## Protocol

### PC Registration
```json
{
  "type": "register",
  "clientType": "pc",
  "sessionCode": "WOLF-5821",
  "pcName": "My PC"
}
```

### Phone Registration
```json
{
  "type": "register",
  "clientType": "phone",
  "sessionCode": "WOLF-5821"
}
```

### Relay Message
```json
{
  "type": "relay",
  "data": { "action": "play" }
}
```

## Stats

- `GET /health` - Health check (returns OK)
- `GET /stats` - JSON stats (active sessions, connections)
- `GET /` - Web dashboard

## Cost

Railway free tier: $5/month credit
Expected usage: ~$2-3/month (well within free tier)
