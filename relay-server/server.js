const WebSocket = require('ws');
const http = require('http');
const fs = require('fs');
const path = require('path');

const PORT = process.env.PORT || 8080;

// Store active sessions: { sessionCode: { pc: WebSocket, phones: [WebSocket] } }
const sessions = new Map();

// Store session metadata: { sessionCode: { pcName: string, connectedAt: Date } }
const sessionMeta = new Map();

function getRemoteHTML(session) {
  // Read the full-featured HTML file
  const htmlPath = path.join(__dirname, 'remote.html');
  let html = fs.readFileSync(htmlPath, 'utf8');
  // Inject session code into the query parameter
  html = html.replace('session=', `session=${session}`);
  return html;
}

const server = http.createServer((req, res) => {
  const url = new URL(req.url, `http://${req.headers.host}`);
  
  if (url.pathname === '/health') {
    res.writeHead(200, { 'Content-Type': 'text/plain' });
    res.end('OK');
  } else if (url.pathname === '/select') {
    // Serve the PC selector page
    const htmlPath = path.join(__dirname, 'select.html');
    const html = fs.readFileSync(htmlPath, 'utf8');
    res.writeHead(200, { 'Content-Type': 'text/html' });
    res.end(html);
  } else if (url.pathname === '/remote') {
    // Serve the remote control interface
    const session = url.searchParams.get('session');
    res.writeHead(200, { 'Content-Type': 'text/html' });
    res.end(getRemoteHTML(session || ''));
  } else if (url.pathname === '/stats') {
    const stats = {
      activeSessions: sessions.size,
      totalConnections: Array.from(sessions.values()).reduce((sum, s) => 
        sum + (s.pc ? 1 : 0) + s.phones.length, 0),
      uptime: process.uptime(),
      sessions: Array.from(sessionMeta.entries()).map(([code, meta]) => ({
        code,
        pcName: meta.pcName,
        phoneCount: sessions.get(code)?.phones.length || 0,
        connectedAt: meta.connectedAt
      }))
    };
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify(stats, null, 2));
  } else {
    res.writeHead(200, { 'Content-Type': 'text/html' });
    res.end(`
      <!DOCTYPE html>
      <html>
      <head>
        <title>SofaRemote Relay Server</title>
        <style>
          body { font-family: 'Segoe UI', sans-serif; max-width: 800px; margin: 50px auto; padding: 20px; }
          h1 { color: #1976d2; }
          .stat { background: #f5f5f5; padding: 15px; margin: 10px 0; border-radius: 8px; }
          .stat strong { color: #333; }
          code { background: #eee; padding: 2px 6px; border-radius: 3px; }
        </style>
      </head>
      <body>
        <h1>🚀 SofaRemote Relay Server</h1>
        <div class="stat">
          <strong>Status:</strong> Running ✓
        </div>
        <div class="stat">
          <strong>WebSocket URL:</strong> <code>ws://${req.headers.host}</code>
        </div>
        <div class="stat">
          <strong>Active Sessions:</strong> <span id="sessions">Loading...</span>
        </div>
        <div class="stat">
          <strong>Total Connections:</strong> <span id="connections">Loading...</span>
        </div>
        <script>
          fetch('/stats')
            .then(r => r.json())
            .then(d => {
              document.getElementById('sessions').textContent = d.activeSessions;
              document.getElementById('connections').textContent = d.totalConnections;
            });
        </script>
      </body>
      </html>
    `);
  }
});

const wss = new WebSocket.Server({ server });

wss.on('connection', (ws) => {
  let sessionCode = null;
  let clientType = null; // 'pc' or 'phone'
  
  console.log('[Connection] New client connected');
  
  ws.on('message', (data) => {
    try {
      const msg = JSON.parse(data.toString());
      
      // Handle registration
      if (msg.type === 'register') {
        sessionCode = msg.sessionCode?.toUpperCase();
        clientType = msg.clientType; // 'pc' or 'phone'
        
        if (!sessionCode || !clientType) {
          ws.send(JSON.stringify({ type: 'error', message: 'Invalid registration' }));
          return;
        }
        
        // Create session if doesn't exist
        if (!sessions.has(sessionCode)) {
          sessions.set(sessionCode, { pc: null, phones: [] });
          sessionMeta.set(sessionCode, {
            pcName: msg.pcName || 'Unknown PC',
            connectedAt: new Date()
          });
        }
        
        const session = sessions.get(sessionCode);
        
        if (clientType === 'pc') {
          // Disconnect old PC if exists
          if (session.pc) {
            session.pc.close();
          }
          session.pc = ws;
          sessionMeta.get(sessionCode).pcName = msg.pcName || 'Unknown PC';
          console.log(`[PC] Registered: ${sessionCode} (${msg.pcName})`);
          ws.send(JSON.stringify({ type: 'registered', role: 'pc' }));
          
          // Notify all phones that PC is online
          session.phones.forEach(phone => {
            phone.send(JSON.stringify({ type: 'pc_status', online: true }));
          });
          
        } else if (clientType === 'phone') {
          session.phones.push(ws);
          console.log(`[Phone] Registered: ${sessionCode} (${session.phones.length} phones)`);
          ws.send(JSON.stringify({ 
            type: 'registered', 
            role: 'phone',
            pcOnline: !!session.pc 
          }));
          
          // Notify PC of new phone connection
          if (session.pc) {
            session.pc.send(JSON.stringify({ type: 'phone_connected' }));
          }
        }
      }
      
      // Handle relay messages
      else if (msg.type === 'relay') {
        if (!sessionCode || !sessions.has(sessionCode)) {
          ws.send(JSON.stringify({ type: 'error', message: 'Not registered' }));
          return;
        }
        
        const session = sessions.get(sessionCode);
        
        if (clientType === 'phone' && session.pc) {
          // Forward from phone to PC
          session.pc.send(JSON.stringify(msg.data));
        } else if (clientType === 'pc') {
          // Forward from PC to all phones
          session.phones.forEach(phone => {
            phone.send(JSON.stringify(msg.data));
          });
        }
      }
      
      // Handle ping/pong for keepalive
      else if (msg.type === 'ping') {
        ws.send(JSON.stringify({ type: 'pong' }));
      }
      
    } catch (err) {
      console.error('[Error] Message handling:', err.message);
    }
  });
  
  ws.on('close', () => {
    if (sessionCode && sessions.has(sessionCode)) {
      const session = sessions.get(sessionCode);
      
      if (clientType === 'pc' && session.pc === ws) {
        session.pc = null;
        console.log(`[PC] Disconnected: ${sessionCode}`);
        
        // Notify phones that PC is offline
        session.phones.forEach(phone => {
          phone.send(JSON.stringify({ type: 'pc_status', online: false }));
        });
        
        // Clean up session if no phones connected
        if (session.phones.length === 0) {
          sessions.delete(sessionCode);
          sessionMeta.delete(sessionCode);
          console.log(`[Session] Cleaned up: ${sessionCode}`);
        }
      } else if (clientType === 'phone') {
        session.phones = session.phones.filter(p => p !== ws);
        console.log(`[Phone] Disconnected: ${sessionCode} (${session.phones.length} remaining)`);
        
        // Clean up session if PC disconnected and no phones
        if (!session.pc && session.phones.length === 0) {
          sessions.delete(sessionCode);
          sessionMeta.delete(sessionCode);
          console.log(`[Session] Cleaned up: ${sessionCode}`);
        }
      }
    }
    console.log('[Connection] Client disconnected');
  });
  
  ws.on('error', (err) => {
    console.error('[Error] WebSocket:', err.message);
  });
});

// Cleanup stale sessions every 5 minutes
setInterval(() => {
  sessions.forEach((session, code) => {
    if (!session.pc && session.phones.length === 0) {
      sessions.delete(code);
      sessionMeta.delete(code);
      console.log(`[Cleanup] Removed stale session: ${code}`);
    }
  });
}, 5 * 60 * 1000);

server.listen(PORT, () => {
  console.log('');
  console.log('🚀 SofaRemote Relay Server');
  console.log('─────────────────────────────');
  console.log(`WebSocket: ws://localhost:${PORT}`);
  console.log(`Dashboard: http://localhost:${PORT}`);
  console.log(`Health: http://localhost:${PORT}/health`);
  console.log(`Stats: http://localhost:${PORT}/stats`);
  console.log('─────────────────────────────');
  console.log('');
});
