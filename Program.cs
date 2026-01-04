using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using QRCoder;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using Windows.Media.Control;
using Windows.Storage.Streams;
using System.Net.Http;
using System.Text.Json;
using System.Net.WebSockets;
using System.Text;

static class Program
{
  private const string AppVersion = "2025.12.27.1";
  private const string RelayServerUrl = "wss://sofaremote-production.up.railway.app";
    private const byte VK_MEDIA_PLAY_PAUSE = 0xB3;
    private const byte VK_MEDIA_NEXT_TRACK = 0xB0;
    private const byte VK_MEDIA_PREV_TRACK = 0xB1;
    private const byte VK_VOLUME_UP = 0xAF;
    private const byte VK_VOLUME_DOWN = 0xAE;
    private const byte VK_VOLUME_MUTE = 0xAD;
    private const ushort VK_LEFT = 0x25;
    private const ushort VK_RIGHT = 0x27;
    private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_SCANCODE = 0x0008;

    // Extended scancodes for arrows (E0 prefix): Left=0x4B, Right=0x4D
    private const ushort SC_LEFT = 0x4B;
    private const ushort SC_RIGHT = 0x4D;

    private static NotifyIcon? _tray;
    private static CancellationTokenSource? _cts;
    private static string? _logFilePath;
    private static string? _primaryUrl;
    private static string[]? _allUrls;
    
    // Relay server fields
    private static ClientWebSocket? _relayClient;
    private static string? _sessionCode;
    private static bool _relayConnected = false;
    private static bool _localServerRunning = false;

    private static readonly string IndexHtml = @"<!doctype html>
<html>
<head>
  <meta charset='utf-8'>
  <meta name='viewport' content='width=device-width,initial-scale=1'>
  <title>Sofa Remote</title>
  <link rel='manifest' href='/manifest.webmanifest?v=" + AppVersion + @"'>
  <meta name='theme-color' content='#101822'>
  <meta name='apple-mobile-web-app-capable' content='yes'>
  <meta name='apple-mobile-web-app-status-bar-style' content='black-translucent'>
  <link rel='apple-touch-icon' href='/icon-192.png?v=" + AppVersion + @"'>
  <style>
    :root{--primary:#1976d2;--surface:#1e293b;--on-surface:#f1f5f9;--muted:#94a3b8;--success:#10b981}
    *{box-sizing:border-box;user-select:none;-webkit-user-select:none;-moz-user-select:none;-ms-user-select:none;-webkit-tap-highlight-color:transparent}
    html,body{height:100%;margin:0;font-family:Roboto,Segoe UI,Arial,sans-serif;background:#0f172a;color:var(--on-surface);overflow:hidden;position:fixed;width:100%;touch-action:pan-y}
    .wrap{max-width:500px;margin:0 auto;padding:20px;position:relative;isolation:isolate}
    .title{text-align:center;font-size:24px;margin-bottom:30px;font-weight:500;letter-spacing:0.5px;animation:fadeIn 0.4s ease-out}
    .app-icon{font-size:48px;margin-bottom:8px}
    .conn-status{text-align:center;margin-top:20px;padding:12px;display:flex;align-items:center;justify-content:center;gap:10px;font-size:13px;color:var(--muted)}
    .conn-dot{display:inline-block;width:12px;height:12px;border-radius:50%;background:#ef4444;transition:all 0.3s;box-shadow:0 0 8px rgba(239,68,68,0.6)}
    .conn-dot.connected{background:#10b981;box-shadow:0 0 12px rgba(16,185,129,0.8),0 0 20px rgba(16,185,129,0.4)}
    .disconnected-overlay{position:fixed;top:0;left:0;width:100%;height:100%;background:rgba(0,0,0,0.8);display:none;align-items:center;justify-content:center;z-index:1000;backdrop-filter:blur(8px)}
    .disconnected-overlay.show{display:flex}
    .disconnected-card{background:linear-gradient(135deg, #1e293b 0%, #334155 100%);padding:40px 32px;border-radius:20px;text-align:center;max-width:320px;box-shadow:0 20px 60px rgba(0,0,0,0.6),0 0 1px rgba(255,255,255,0.1) inset;animation:slideUp 0.4s ease-out;border:1px solid rgba(255,255,255,0.05)}
    .disconnected-icon{font-size:64px;margin-bottom:20px;animation:pulse 2s ease-in-out infinite;filter:drop-shadow(0 4px 12px rgba(239,68,68,0.4))}
    .disconnected-title{font-size:22px;font-weight:600;margin-bottom:12px;color:#f1f5f9;letter-spacing:0.3px}
    .disconnected-msg{font-size:14px;color:#94a3b8;line-height:1.6;font-weight:400}
    @keyframes slideUp{from{opacity:0;transform:translateY(20px) scale(0.95)}to{opacity:1;transform:translateY(0) scale(1)}}
    .install-popup{position:fixed;top:0;left:0;width:100%;height:100%;background:rgba(0,0,0,0.8);display:none;align-items:center;justify-content:center;z-index:1001;backdrop-filter:blur(8px)}
    .install-popup.show{display:flex}
    .install-card{background:linear-gradient(135deg,#1e293b 0%,#334155 100%);padding:32px 24px;border-radius:20px;text-align:center;max-width:320px;box-shadow:0 20px 60px rgba(0,0,0,0.6);animation:slideUp 0.4s ease-out;border:1px solid rgba(255,255,255,0.05)}
    .install-title{font-size:20px;font-weight:600;margin-bottom:16px;color:#f1f5f9;letter-spacing:0.3px}
    .install-msg{font-size:14px;color:#cbd5e1;line-height:1.7;margin-bottom:24px}
    .install-close-btn{width:100%;padding:12px;background:linear-gradient(135deg,#1976d2,#1565c0);color:white;border:none;border-radius:10px;font-size:15px;font-weight:600;cursor:pointer;box-shadow:0 4px 12px rgba(25,118,210,0.4);transition:all 0.2s}
    .install-close-btn:active{transform:scale(0.96);box-shadow:0 2px 8px rgba(25,118,210,0.3)}
    .tile{background:var(--surface);color:var(--on-surface);border:0;border-radius:12px;height:110px;display:flex;align-items:center;justify-content:center;flex-direction:column;box-shadow:0 2px 8px rgba(0,0,0,0.3);transition:all 0.2s cubic-bezier(0.4,0,0.2,1);cursor:pointer;position:relative;overflow:hidden}
    .tile .icon{font-size:36px;margin-bottom:4px;transition:transform 0.2s}
    .tile .label{font-size:13px;opacity:.9;font-weight:500;letter-spacing:0.3px}
    .tile:active{transform:scale(0.96);box-shadow:0 1px 4px rgba(0,0,0,0.4)}
    .tile:active .icon{transform:scale(1.1)}
    .tile.pulse{animation:pulse 0.3s ease-out}
    .ripple{position:absolute;border-radius:50%;background:rgba(255,255,255,0.3);transform:scale(0);animation:ripple 0.6s ease-out;pointer-events:none}
    .grid{display:grid;grid-template-columns:repeat(2,1fr);gap:12px;margin-top:20px}
    .grid .tile.full-width{grid-column:1 / -1}
    .volume-control{grid-column:1 / -1;display:flex;align-items:center;gap:12px;background:linear-gradient(135deg,rgba(25,118,210,0.1),rgba(66,165,245,0.05));border-radius:16px;padding:14px 18px;box-shadow:0 4px 12px rgba(0,0,0,0.2),inset 0 1px 0 rgba(255,255,255,0.1);isolation:isolate}
    .volume-btn{width:48px;height:48px;border:none;background:linear-gradient(135deg,#1976d2,#1565c0);color:white;font-size:24px;font-weight:700;border-radius:12px;cursor:pointer;transition:transform 0.15s ease,box-shadow 0.15s ease;box-shadow:0 4px 12px rgba(25,118,210,0.4);display:flex;align-items:center;justify-content:center;flex-shrink:0;outline:none;-webkit-tap-highlight-color:transparent;user-select:none;position:relative;transform-origin:center center;will-change:transform;isolation:isolate;overflow:hidden}
    .volume-btn:active{transform:scale(0.92) translateZ(0);box-shadow:0 2px 6px rgba(25,118,210,0.3)}
    .volume-btn:hover{box-shadow:0 6px 16px rgba(25,118,210,0.5)}
    .volume-btn:focus{outline:none}
    .volume-btn::before,.volume-btn::after{content:none;display:none}
    .volume-display-inline{flex:1;display:flex;align-items:center;gap:12px}
    .volume-bar-container{flex-grow:1;height:8px;background:rgba(0,0,0,0.2);border-radius:4px;overflow:hidden;cursor:pointer;position:relative;box-shadow:inset 0 2px 4px rgba(0,0,0,0.2)}
    .volume-bar{height:100%;background:linear-gradient(90deg,#1976d2,#42a5f5,#64b5f6);border-radius:4px;transition:width 0.15s ease;width:50%;pointer-events:none;box-shadow:0 0 8px rgba(66,165,245,0.6)}
    .volume-text{font-size:16px;font-weight:700;min-width:42px;text-align:center;color:#42a5f5;text-shadow:0 2px 4px rgba(0,0,0,0.3);background:rgba(0,0,0,0.2);padding:4px 8px;border-radius:8px}
    .status{margin-top:16px;text-align:center;color:var(--muted);font-size:12px;transition:color 0.3s,transform 0.3s;min-height:18px;display:none}
    .status.success{color:var(--success);transform:scale(1.05)}
    .install-btn{margin:20px auto;padding:14px 28px;background:linear-gradient(135deg,#1976d2,#1565c0);color:white;border:none;border-radius:12px;font-size:16px;font-weight:600;cursor:pointer;box-shadow:0 4px 12px rgba(25,118,210,0.4);transition:all 0.3s;display:none}
    .install-btn:active{transform:scale(0.96);box-shadow:0 2px 8px rgba(25,118,210,0.3)}
    .install-btn span{display:flex;align-items:center;justify-content:center;gap:8px}
    .ios-hint{margin:16px auto;padding:12px 20px;background:rgba(25,118,210,0.15);border:1px solid rgba(25,118,210,0.3);border-radius:10px;color:var(--on-surface);font-size:13px;text-align:center;max-width:320px;line-height:1.6}
    .page{animation:fadeIn 0.4s ease-out}
    @keyframes fadeIn{from{opacity:0;transform:translateY(10px)}to{opacity:1;transform:translateY(0)}}
    @keyframes pulse{0%,100%{transform:scale(1)}50%{transform:scale(0.95)}}
    @keyframes ripple{to{transform:scale(4);opacity:0}}
    @media (max-width:420px){.tile{height:100px}}
  </style>
</head>
<body>
  <div class='wrap'>
    <div class='page'>
      <div class='title'>
        <div class='app-icon'>
          <svg width='48' height='48' viewBox='0 0 64 64' xmlns='http://www.w3.org/2000/svg'>
            <circle cx='32' cy='32' r='30' fill='#1976d2'/>
            <g fill='#ff6b6b'>
              <rect x='15' y='25' width='34' height='3' rx='1.5'/>
              <rect x='16' y='28' width='32' height='10' rx='2'/>
              <rect x='14' y='25' width='4' height='15' rx='2'/>
              <rect x='46' y='25' width='4' height='15' rx='2'/>
            </g>
            <g fill='#34495e'>
              <rect x='35' y='35' width='11' height='21' rx='2'/>
              <circle cx='40.5' cy='42' r='2' fill='#3498db'/>
              <circle cx='37' cy='48' r='1.2' fill='#e0e0e0'/>
              <circle cx='40.5' cy='48' r='1.2' fill='#e0e0e0'/>
              <circle cx='44' cy='48' r='1.2' fill='#e0e0e0'/>
            </g>
          </svg>
        </div>
        <div>Sofa Remote</div>
      </div>
      
      <div class='grid'>
        <button id='fullscreen' class='tile' aria-label='Enter Fullscreen'><div class='icon'>⛶</div><div class='label'>Enter Full</div></button>
        <button id='exitfullscreen' class='tile' aria-label='Exit Fullscreen'><div class='icon'>⎋</div><div class='label'>Exit Full</div></button>

        <div class='volume-control full-width'>
          <button id='voldown' class='volume-btn' aria-label='Volume Down'>−</button>
          <div class='volume-display-inline'>
            <div class='volume-bar-container' id='volume-container'>
              <div class='volume-bar' id='volume-bar'></div>
            </div>
            <div class='volume-text' id='volume-text'>--</div>
          </div>
          <button id='volup' class='volume-btn' aria-label='Volume Up'>+</button>
        </div>

        <button id='mute' class='tile full-width' aria-label='Mute'><div class='icon'>🔇</div><div class='label'>Mute</div></button>

        <button id='play' class='tile full-width' aria-label='Play/Pause'><div class='icon'>⏯</div><div class='label'>Play / Pause</div></button>

        <button id='back' class='tile' aria-label='Rewind'><div class='icon'>⏪</div><div class='label'>Backward</div></button>
        <button id='forward' class='tile' aria-label='Forward'><div class='icon'>⏩</div><div class='label'>Forward</div></button>
      </div>

      <div class='conn-status'>
        <span id='conn-dot' class='conn-dot'></span>
        <span id='conn-text'>Connecting...</span>
      </div>
    </div>
  </div>

  <div id='disconnected-overlay' class='disconnected-overlay'>
    <div class='disconnected-card'>
      <div class='disconnected-icon'>⚠</div>
      <div class='disconnected-title'>Server Disconnected</div>
      <div class='disconnected-msg'>Please check if the app is running on your PC</div>
    </div>
  </div>
  <div id='install-popup' class='install-popup'>
    <div class='install-card'>
      <div class='install-title'>Install Sofa Remote</div>
      <div class='install-msg' id='install-msg'></div>
      <button id='install-close' class='install-close-btn'>Got it!</button>
    </div>
  </div>
  <script>
    // Service worker temporarily disabled for cache troubleshooting
    if ('serviceWorker' in navigator) {
      // Unregister all service workers
      navigator.serviceWorker.getRegistrations().then(regs => {
        regs.forEach(reg => reg.unregister());
      });
    }
    /*
    if ('serviceWorker' in navigator) {
      navigator.serviceWorker.register('/sw.js?v=" + AppVersion + @"').then(reg => {
        reg.update();
      }).catch(()=>{});
      
      // Clear old service workers
      navigator.serviceWorker.getRegistrations().then(regs => {
        regs.forEach(reg => {
          if(reg.active && !reg.active.scriptURL.includes('" + AppVersion + @"')){
            reg.unregister();
          }
        });
      });
    }
    */

    const lastSent = {};
    let wakeLock = null;
    let noSleepEnabled = false;
    
    // Relay server support
    let relayWs = null;
    let sessionCode = null;
    let useRelay = false;
    const RELAY_URL = 'wss://sofaremote-production.up.railway.app';
    
    // Extract session code from URL
    const urlParams = new URLSearchParams(window.location.search);
    sessionCode = urlParams.get('session');
    
    function connectRelay(){
      if(relayWs) return;
      if(!sessionCode){
        console.log('No session code, relay disabled');
        return;
      }
      
      try{
        relayWs = new WebSocket(RELAY_URL);
        
        relayWs.onopen = () => {
          console.log('Relay connected');
          // Register as phone
          relayWs.send(JSON.stringify({
            type: 'register',
            clientType: 'phone',
            sessionCode: sessionCode
          }));
        };
        
        relayWs.onmessage = (e) => {
          const msg = JSON.parse(e.data);
          if(msg.type === 'registered'){
            useRelay = msg.pcOnline;
            updateConnStatus();
            console.log('Registered with relay, PC online:', msg.pcOnline);
          } else if(msg.type === 'pc_status'){
            useRelay = msg.online;
            updateConnStatus();
          }
        };
        
        relayWs.onerror = (e) => {
          console.log('Relay error:', e);
          useRelay = false;
        };
        
        relayWs.onclose = () => {
          console.log('Relay disconnected');
          relayWs = null;
          useRelay = false;
          updateConnStatus();
          // Reconnect after 3 seconds
          setTimeout(connectRelay, 3000);
        };
      }catch(e){
        console.log('Relay connection failed:', e);
      }
    }
    
    function sendRelay(action){
      if(!relayWs || relayWs.readyState !== WebSocket.OPEN) return false;
      try{
        relayWs.send(JSON.stringify({
          type: 'relay',
          data: { action }
        }));
        return true;
      }catch(e){
        console.log('Relay send error:', e);
        return false;
      }
    }
    
    function updateConnStatus(){
      const dot = document.querySelector('.conn-dot');
      const status = document.querySelector('.conn-status span:last-child');
      if(useRelay){
        dot.classList.add('connected');
        if(status) status.textContent = 'Connected via Relay';
      }else{
        dot.classList.remove('connected');
        if(status) status.textContent = 'Connecting...';
      }
    }

    function keepScreenOn(){
      if(noSleepEnabled) return;
      
      // Modern browsers (Android)
      if('wakeLock' in navigator && !wakeLock){
        navigator.wakeLock.request('screen')
          .then(lock => {
            wakeLock = lock;
            noSleepEnabled = true;
            console.log('Wake lock active');
          })
          .catch(err => console.log('Wake lock error:', err));
      }
      
      // iOS fallback - silent audio loop
      const isIOS = /iPad|iPhone|iPod/.test(navigator.userAgent);
      if(isIOS && !noSleepEnabled){
        const audio = document.createElement('audio');
        audio.loop = true;
        audio.src = 'data:audio/wav;base64,UklGRiQAAABXQVZFZm10IBAAAAABAAEARKwAAIhYAQACABAAZGF0YQAAAAA=';
        audio.play().then(() => {
          noSleepEnabled = true;
          console.log('iOS NoSleep active');
        }).catch(e => console.log('Audio error:', e));
      }
    }

    function createRipple(e){
      const btn = e.currentTarget;
      const ripple = document.createElement('span');
      ripple.classList.add('ripple');
      const rect = btn.getBoundingClientRect();
      const size = Math.max(rect.width,rect.height);
      ripple.style.width = ripple.style.height = size + 'px';
      ripple.style.left = (e.clientX - rect.left - size/2) + 'px';
      ripple.style.top = (e.clientY - rect.top - size/2) + 'px';
      btn.appendChild(ripple);
      setTimeout(()=>ripple.remove(),600);
    }

    async function doPost(path, silent = false){
      // Try relay first if enabled
      if(useRelay){
        const action = path.replace('/','');
        if(sendRelay(action)){
          if(!silent) setStatus('✓ ' + action + ' (relay)', true);
          return;
        }
      }
      
      // Fallback to local HTTP
      try{
        const now = Date.now();
        if(lastSent[path] && now - lastSent[path] < 400) return;
        lastSent[path] = now;
        const r = await fetch(path,{method:'POST'});
        if(!r.ok){
          const t = await r.text().catch(()=>null);
          throw new Error(r.status + (t ? ' ' + t : ''));
        }
        if(!silent) setStatus('✓ ' + path.replace('/',''), true);
      }catch(e){
        const msg = (e && e.message) ? e.message : String(e);
        if(!silent) setStatus('✗ ' + path.replace('/','') + ': ' + msg, false);
        console.debug('doPost error', e);
      }
    }

    function setStatus(s, success){
      const el=document.getElementById('status');
      try{ 
        el.textContent=s;
        if(success){el.classList.add('success');}
        else{el.classList.remove('success');}
      }catch{}
      setTimeout(()=>{ try{ el.textContent='Ready'; el.classList.remove('success'); }catch{} }, 1400);
    }

    document.getElementById('play').onclick=()=>doPost('/playpause');
    document.getElementById('mute').onclick=()=>doPost('/mute');
    document.getElementById('volup').onclick=()=>{doPost('/volup', true);setTimeout(updateVolume,300);};
    document.getElementById('voldown').onclick=()=>{doPost('/voldown', true);setTimeout(updateVolume,300);};
    document.getElementById('forward').onclick=()=>doPost('/forward');
    document.getElementById('back').onclick=()=>doPost('/backward');
    document.getElementById('fullscreen').onclick=()=>doPost('/fullscreen');
    document.getElementById('exitfullscreen').onclick=()=>doPost('/exitfullscreen');

    let currentVolume = 50;
    
    // Interactive volume slider - decoupled from PC
    const volumeContainer=document.getElementById('volume-container');
    let isDragging=false;
    
    function setVolumeFromEvent(e){
      const rect=volumeContainer.getBoundingClientRect();
      const x=(e.touches?e.touches[0].clientX:e.clientX)-rect.left;
      const percent=Math.max(0,Math.min(100,Math.round((x/rect.width)*100)));
      setVolume(percent);
    }
    
    function setVolume(percent){
      currentVolume = percent;
      document.getElementById('volume-bar').style.width=percent+'%';
      document.getElementById('volume-text').textContent=percent+'%';
      fetch('/setvolume',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({volume:percent})}).catch(()=>{});
    }
    
    volumeContainer.addEventListener('mousedown',e=>{isDragging=true;setVolumeFromEvent(e);});
    volumeContainer.addEventListener('touchstart',e=>{isDragging=true;setVolumeFromEvent(e);e.preventDefault();});
    document.addEventListener('mousemove',e=>{if(isDragging)setVolumeFromEvent(e);});
    document.addEventListener('touchmove',e=>{if(isDragging){setVolumeFromEvent(e);e.preventDefault();}});
    document.addEventListener('mouseup',()=>isDragging=false);
    document.addEventListener('touchend',()=>isDragging=false);

    async function updateVolume(){
      try{
        const r = await fetch('/volume');
        if(r.ok){
          const vol = await r.json();
          currentVolume = vol.volume;
          const bar = document.getElementById('volume-bar');
          const text = document.getElementById('volume-text');
          bar.style.width = vol.volume + '%';
          text.textContent = vol.volume + '%';
        }
      }catch(e){console.debug('Volume fetch error', e);}
    }
    
    updateVolume(); // Initial update
    
    // Connect to relay if session code present
    if(sessionCode){
      connectRelay();
      console.log('Relay mode enabled with session:', sessionCode);
    }

    const connDot = document.getElementById('conn-dot');
    const connText = document.getElementById('conn-text');
    const overlay = document.getElementById('disconnected-overlay');
    const installModal = document.getElementById('install-modal');

    // Check if running in standalone mode (already installed)
    const isInstalled = window.matchMedia('(display-mode: standalone)').matches || window.navigator.standalone;
    const installPopup = document.getElementById('install-popup');
    const installMsg = document.getElementById('install-msg');
    const installClose = document.getElementById('install-close');
    
    installClose.addEventListener('click', () => installPopup.classList.remove('show'));
    
    if (!isInstalled) {
      const isIOS = /iPad|iPhone|iPod/.test(navigator.userAgent) && !window.MSStream;
      
      if (isIOS) {
        installMsg.innerHTML = '<strong>1.</strong> Tap <strong>Share &#8593;</strong><br><strong>2.</strong> Select <strong>Add to Home Screen</strong><br><strong>3.</strong> Tap <strong>Add</strong>';
        setTimeout(() => installPopup.classList.add('show'), 1000);
      } else {
        let deferredPrompt;
        window.addEventListener('beforeinstallprompt', (e) => {
          e.preventDefault();
          deferredPrompt = e;
          installMsg.innerHTML = 'Tap below to install Sofa Remote';
          installClose.textContent = 'Install Now';
          installClose.addEventListener('click', async () => {
            if (deferredPrompt) {
              deferredPrompt.prompt();
              deferredPrompt = null;
            }
          }, { once: true });
          setTimeout(() => installPopup.classList.add('show'), 1000);
        });
      }
    }

    function checkConnection(){
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), 2000); // 2 second timeout
      fetch('/health', { signal: controller.signal })
        .then(()=>{ 
          clearTimeout(timeoutId); 
          connDot.classList.add('connected'); 
          connText.textContent = 'Connected to server';
          overlay.classList.remove('show');
        })
        .catch(()=>{ 
          clearTimeout(timeoutId); 
          connDot.classList.remove('connected'); 
          connText.textContent = 'Disconnected';
          overlay.classList.add('show');
        });
    }
    checkConnection();
    setInterval(checkConnection, 3000);

    Array.from(document.querySelectorAll('button')).forEach(b=>{
      b.addEventListener('click', createRipple);
      b.addEventListener('touchstart', e=>{
        if(b.disabled) return;
        keepScreenOn();
        const touch = e.touches[0];
        const fakeEvent = {currentTarget:b, clientX:touch.clientX, clientY:touch.clientY};
        createRipple(fakeEvent);
        b.classList.add('pulse');
        setTimeout(()=>b.classList.remove('pulse'),300);
      });
      b.addEventListener('touchend', e=>{
        e.preventDefault();
        if(b.disabled) return;
        b.click();
        b.disabled = true;
        setTimeout(()=>{ try{ b.disabled = false }catch{} }, 500);
      });
    });
  </script>
</body>
</html>";

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        _cts = new CancellationTokenSource();
        _logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SofaRemote", "sofa_remote.log");
        try { Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath)!); } catch { }

        Icon? trayIcon = null;
        try
        {
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons", "app.ico");
            if (File.Exists(iconPath))
                trayIcon = new Icon(iconPath);
        }
        catch { }

        _tray = new NotifyIcon
        {
            Text = "Sofa Remote",
            Icon = trayIcon ?? SystemIcons.Application,
            Visible = true
        };
        var menu = new ContextMenuStrip();
        var showQrItem = new ToolStripMenuItem("Show QR", null, (_, __) => { try { ShowQrWindow(); } catch { } });
        var checkUpdateItem = new ToolStripMenuItem("Check for Updates", null, (_, __) => { Task.Run(() => CheckForUpdates(true)); });
        var restartAdminItem = new ToolStripMenuItem("Restart as Admin (Fix LAN)", null, (_, __) => { try { RestartElevated(); } catch { } });
        var exitItem = new ToolStripMenuItem("Exit", null, (_, __) => { try { _cts!.Cancel(); } catch { } Application.Exit(); });
        menu.Items.Add(showQrItem);
        menu.Items.Add(checkUpdateItem);
        menu.Items.Add(restartAdminItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);
        _tray.ContextMenuStrip = menu;
        _tray.MouseClick += (_, e) =>
        {
          if (e.Button == MouseButtons.Left)
          {
            try { ShowQrWindow(); } catch { }
          }
        };

        Task.Run(() => RunServerAsync(_cts.Token));
        Task.Run(() => ConnectToRelay()); // Connect to relay server
        Task.Run(() => CheckForUpdates(false)); // Check for updates on startup
        Application.Run();

        try { _tray.Visible = false; _tray.Dispose(); } catch { }
    }

    private static async Task RunServerAsync(CancellationToken token)
    {
        var ips = new System.Collections.Generic.List<string>(GetLocalIPv4Addresses());
        var best = GetBestLocalAddress(ips);
        
        // Prefer hostname.local if Bonjour is available, fallback to IP
        if (IsBonjourAvailable())
        {
          var hostname = GetLocalHostname();
          _primaryUrl = $"http://{hostname}.local:8080/";
          Log($"Using mDNS hostname: {_primaryUrl}");
        }
        else
        {
          _primaryUrl = best != null ? $"http://{best}:8080/" : "http://localhost:8080/";
          Log($"Bonjour not available, using IP: {_primaryUrl}");
        }
        
        _allUrls = new string[] { _primaryUrl }; // Only show primary URL

        HttpListener listener = new HttpListener();
        // Attempt 1: wildcard binding (restores prior behavior if URLACL exists)
        try
        {
          listener.Prefixes.Add("http://+:8080/");
          listener.Start();
          _localServerRunning = true;
          Log("Listening on: http://+:8080/ (wildcard)");
        }
        catch (Exception exWildcard)
        {
          try { Log("Wildcard listen failed: " + exWildcard.Message); } catch { }
          try { listener.Close(); } catch { }

          // Attempt to auto-fix permissions via elevation (URLACL + firewall), then retry wildcard once
          if (TryEnsureNetworkPermissions())
          {
            try
            {
              listener = new HttpListener();
              listener.Prefixes.Add("http://+:8080/");
              listener.Start();
              Log("Listening on: http://+:8080/ (wildcard after ACL)");
            }
            catch (Exception exWildcard2)
            {
              try { Log("Wildcard still failed after ACL: " + exWildcard2.Message); } catch { }
              try { listener.Close(); } catch { }
            }
          }

          // Attempt 2: explicit localhost + each IP
          listener = new HttpListener();
          var prefixes = new System.Collections.Generic.List<string> { "http://localhost:8080/" };
          foreach (var ip in ips) prefixes.Add($"http://{ip}:8080/");
          foreach (var p in prefixes) listener.Prefixes.Add(p);
          try
          {
            listener.Start();
            Log($"Listening on: {string.Join(", ", prefixes)}");
          }
          catch (Exception exSpecific)
          {
            try { Log("Listener start failed for specific IPs, fallback to localhost: " + exSpecific.Message); } catch { }
            try { listener.Close(); } catch { }
            // Attempt 3: localhost only
            try
            {
              listener = new HttpListener();
              listener.Prefixes.Add("http://localhost:8080/");
              listener.Start();
              Log("Listening on: http://localhost:8080/ (fallback)");
              _primaryUrl = "http://localhost:8080/";
              _allUrls = new[] { "http://localhost:8080/" };
            }
            catch (Exception exLocal)
            {
              try { Log("Listener localhost fallback failed: " + exLocal.Message); } catch { }
              return; // cannot start
            }
          }
        }

        while (!token.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try { ctx = await listener.GetContextAsync(); }
            catch (HttpListenerException) { break; }
            catch (ObjectDisposedException) { break; }

            _ = Task.Run(() => HandleRequestAsync(ctx));
        }

        try { listener.Stop(); } catch { }
        try { listener.Close(); } catch { }
    }

    private static async Task HandleRequestAsync(HttpListenerContext ctx)
    {
        var req = ctx.Request;
        var res = ctx.Response;
        var path = req.Url?.AbsolutePath ?? "/";
        Log($"Request: {req.HttpMethod} {path} from {req.RemoteEndPoint}");

        try
        {
        if (req.HttpMethod == "GET" && path == "/manifest.webmanifest")
        {
          var manifest = "{\n  \"name\": \"Sofa Remote\",\n  \"short_name\": \"Sofa Remote\",\n  \"start_url\": \"/\",\n  \"scope\": \"/\",\n  \"display\": \"standalone\",\n  \"background_color\": \"#101822\",\n  \"theme_color\": \"#101822\",\n  \"icons\": [\n    { \"src\": \"/icon-192.png\", \"sizes\": \"192x192\", \"type\": \"image/png\" },\n    { \"src\": \"/icon-512.png\", \"sizes\": \"512x512\", \"type\": \"image/png\" }\n  ]\n}";
          var bytesM = System.Text.Encoding.UTF8.GetBytes(manifest);
          res.ContentType = "application/manifest+json";
          res.StatusCode = 200;
          res.ContentLength64 = bytesM.Length;
          await res.OutputStream.WriteAsync(bytesM, 0, bytesM.Length);
          return;
        }
        if (req.HttpMethod == "GET" && path == "/sw.js")
        {
          // Force unregister - this service worker immediately unregisters itself
          var js = "self.addEventListener('install', () => { self.skipWaiting(); });\n" +
                   "self.addEventListener('activate', e => { e.waitUntil(self.registration.unregister().then(() => { return self.clients.matchAll(); }).then(clients => { clients.forEach(client => client.navigate(client.url)); })); });\n";
          var bytesJs = System.Text.Encoding.UTF8.GetBytes(js);
          res.ContentType = "application/javascript";
          res.StatusCode = 200;
          res.ContentLength64 = bytesJs.Length;
          await res.OutputStream.WriteAsync(bytesJs, 0, bytesJs.Length);
          return;
        }
        if (req.HttpMethod == "GET" && (path == "/icon-192.png" || path == "/icon-512.png"))
        {
          int size = path.Contains("192") ? 192 : 512;
          var png = GenerateAppIconPng(size);
          res.ContentType = "image/png";
          res.StatusCode = 200;
          res.ContentLength64 = png.Length;
          await res.OutputStream.WriteAsync(png, 0, png.Length);
          return;
        }
            if (req.HttpMethod == "GET" && path == "/")
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(IndexHtml);
                res.ContentType = "text/html; charset=utf-8";
                res.StatusCode = 200;
                res.ContentLength64 = bytes.Length;
                await res.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                return;
            }
            if (req.HttpMethod == "GET" && path == "/qr.png")
            {
              var url = _primaryUrl ?? "http://localhost:8080/";
              var png = GenerateQrPng(url);
              res.ContentType = "image/png";
              res.StatusCode = 200;
              res.ContentLength64 = png.Length;
              await res.OutputStream.WriteAsync(png, 0, png.Length);
              return;
            }
            if (req.HttpMethod == "GET" && path == "/health")
            {
                res.StatusCode = 200; res.ContentType = "text/plain"; var b = System.Text.Encoding.UTF8.GetBytes("OK"); res.ContentLength64 = b.Length; await res.OutputStream.WriteAsync(b,0,b.Length); return;
            }
            if (req.HttpMethod == "GET" && path == "/reset")
            {
                // Special page to unregister service worker
                var resetHtml = @"<!DOCTYPE html><html><head><title>Reset Cache</title><meta name='viewport' content='width=device-width,initial-scale=1'></head><body style='font-family:system-ui;padding:40px;text-align:center;background:#0f172a;color:#f1f5f9'><h1>🔄 Clearing Cache...</h1><p id='status'>Unregistering service workers...</p><script>
if('serviceWorker' in navigator){
  navigator.serviceWorker.getRegistrations().then(regs=>{
    Promise.all(regs.map(reg=>reg.unregister())).then(()=>{
      document.getElementById('status').innerHTML='✓ Cache cleared!<br><br><a href=\'/\' style=\'color:#1976d2;font-size:18px\'>Click here to open app</a>';
    });
  });
}else{
  document.getElementById('status').innerHTML='✓ No service worker found<br><br><a href=\'/\' style=\'color:#1976d2;font-size:18px\'>Click here to open app</a>';
}
</script></body></html>";
                var bytes = System.Text.Encoding.UTF8.GetBytes(resetHtml);
                res.StatusCode = 200;
                res.ContentType = "text/html";
                res.ContentLength64 = bytes.Length;
                await res.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                return;
            }
            if (req.HttpMethod == "GET" && path == "/volume")
            {
                var volume = GetSystemVolume();
                var json = "{\"volume\":" + volume + "}";
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                res.StatusCode = 200;
                res.ContentType = "application/json";
                res.ContentLength64 = bytes.Length;
                await res.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                return;
            }
            if (req.HttpMethod == "POST" && path == "/setvolume")
            {
                using (var reader = new StreamReader(req.InputStream))
                {
                    var body = await reader.ReadToEndAsync();
                    var match = System.Text.RegularExpressions.Regex.Match(body, @"""?volume""?\s*:\s*(\d+)");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int targetVolume))
                    {
                        SetSystemVolume(targetVolume);
                        var json = "{\"success\":true}";
                        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                        res.StatusCode = 200;
                        res.ContentType = "application/json";
                        res.ContentLength64 = bytes.Length;
                        await res.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                    }
                    else
                    {
                        res.StatusCode = 400;
                    }
                }
                return;
            }
            if (req.HttpMethod == "POST")
            {
                switch (path)
                {
                    case "/playpause": SendPlayPause(); res.StatusCode = 200; break;
                    case "/mute": SendMute(); res.StatusCode = 200; break;
                    case "/volup": SendVolumeUp(); res.StatusCode = 200; break;
                    case "/voldown": SendVolumeDown(); res.StatusCode = 200; break;
                    case "/forward": SendSeekForward(); res.StatusCode = 200; break;
                    case "/backward": SendSeekBackward(); res.StatusCode = 200; break;
                    case "/fullscreen": SendFullscreen(); res.StatusCode = 200; break;
                    case "/exitfullscreen": SendExitFullscreen(); res.StatusCode = 200; break;
                    default: res.StatusCode = 404; break;
                }
                return;
            }

            res.StatusCode = 404;
        }
        catch (Exception ex)
        {
            try { Log("HandleRequest error: " + ex); } catch { }
            res.StatusCode = 500;
        }
        finally
        {
            try { res.OutputStream.Close(); } catch { }
        }
    }

    private static void SendPlayPause() => keybd_event(VK_MEDIA_PLAY_PAUSE, 0, 0, UIntPtr.Zero);
    private static void SendVolumeUp() => keybd_event(VK_VOLUME_UP, 0, 0, UIntPtr.Zero);
    private static void SendVolumeDown() => keybd_event(VK_VOLUME_DOWN, 0, 0, UIntPtr.Zero);
    private static void SendMute() => keybd_event(VK_VOLUME_MUTE, 0, 0, UIntPtr.Zero);

    private static int GetSystemVolume()
    {
      try
      {
        var scriptPath = Path.Combine(Path.GetTempPath(), "getvol.ps1");
        File.WriteAllText(scriptPath, @"
Add-Type -TypeDefinition @'
using System.Runtime.InteropServices;
[Guid(""5CDF2C82-841E-4546-9722-0CF74078229A""), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IAudioEndpointVolume {
  int NotImpl1(); int NotImpl2(); int GetChannelCount(out int pnChannelCount);
  int SetMasterVolumeLevel(float fLevelDB, System.Guid pguidEventContext);
  int SetMasterVolumeLevelScalar(float fLevel, System.Guid pguidEventContext);
  int GetMasterVolumeLevel(out float pfLevelDB);
  int GetMasterVolumeLevelScalar(out float pfLevel);
}
[Guid(""D666063F-1587-4E43-81F1-B948E807363F""), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMMDevice {
  int Activate(ref System.Guid iid, int dwClsCtx, System.IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
}
[Guid(""A95664D2-9614-4F35-A746-DE8DB63617E6""), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMMDeviceEnumerator {
  int NotImpl1(); int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice ppDevice);
}
[ComImport, Guid(""BCDE0395-E52F-467C-8E3D-C4579291692E"")]
class MMDeviceEnumeratorComObject { }
public class AudioHelper {
  public static float GetVolume() {
    var enumerator = (IMMDeviceEnumerator)(new MMDeviceEnumeratorComObject());
    IMMDevice dev; enumerator.GetDefaultAudioEndpoint(0, 0, out dev);
    var iid = typeof(IAudioEndpointVolume).GUID; object obj;
    dev.Activate(ref iid, 0, System.IntPtr.Zero, out obj);
    var vol = (IAudioEndpointVolume)obj; float level;
    vol.GetMasterVolumeLevelScalar(out level); return level;
  }
}
'@
[Math]::Round([AudioHelper]::GetVolume() * 100)
");
        
        var process = new Process
        {
          StartInfo = new ProcessStartInfo
          {
            FileName = "powershell",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
          }
        };
        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        
        try { File.Delete(scriptPath); } catch { }
        
        if (!string.IsNullOrEmpty(error))
        {
          Log($"GetSystemVolume error: {error}");
        }
        
        if (int.TryParse(output.Trim(), out var volume))
        {
          return Math.Clamp(volume, 0, 100);
        }
      }
      catch (Exception ex)
      {
        Log($"GetSystemVolume exception: {ex.Message}");
      }
      return 50; // Default fallback
    }

    private static void SetSystemVolume(int volume)
    {
      try
      {
        volume = Math.Clamp(volume, 0, 100);
        
        var scriptPath = Path.Combine(Path.GetTempPath(), "setvol.ps1");
        File.WriteAllText(scriptPath, $@"
Add-Type -TypeDefinition @'
using System.Runtime.InteropServices;
[Guid(""5CDF2C82-841E-4546-9722-0CF74078229A""), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IAudioEndpointVolume {{
  int NotImpl1(); int NotImpl2(); int GetChannelCount(out int pnChannelCount);
  int SetMasterVolumeLevel(float fLevelDB, System.Guid pguidEventContext);
  int SetMasterVolumeLevelScalar(float fLevel, System.Guid pguidEventContext);
  int GetMasterVolumeLevel(out float pfLevelDB);
  int GetMasterVolumeLevelScalar(out float pfLevel);
}}
[Guid(""D666063F-1587-4E43-81F1-B948E807363F""), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMMDevice {{
  int Activate(ref System.Guid iid, int dwClsCtx, System.IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
}}
[Guid(""A95664D2-9614-4F35-A746-DE8DB63617E6""), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMMDeviceEnumerator {{
  int NotImpl1(); int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice ppDevice);
}}
[ComImport, Guid(""BCDE0395-E52F-467C-8E3D-C4579291692E"")]
class MMDeviceEnumeratorComObject {{ }}
public class AudioHelper {{
  public static void SetVolume(float level) {{
    var enumerator = (IMMDeviceEnumerator)(new MMDeviceEnumeratorComObject());
    IMMDevice dev; enumerator.GetDefaultAudioEndpoint(0, 0, out dev);
    var iid = typeof(IAudioEndpointVolume).GUID; object obj;
    dev.Activate(ref iid, 0, System.IntPtr.Zero, out obj);
    var vol = (IAudioEndpointVolume)obj;
    vol.SetMasterVolumeLevelScalar(level, System.Guid.Empty);
  }}
}}
'@
[AudioHelper]::SetVolume({volume / 100.0})
");
        
        var process = new Process
        {
          StartInfo = new ProcessStartInfo
          {
            FileName = "powershell",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true
          }
        };
        process.Start();
        process.WaitForExit(1000);
        
        try { File.Delete(scriptPath); } catch { }
      }
      catch (Exception ex)
      {
        Log($"SetSystemVolume error: {ex.Message}");
      }
    }

    private static void SendSeekForward()
    {
      TryFocusBrowserWindow();
      Thread.Sleep(200);
      keybd_event((byte)VK_RIGHT, 0, 0, UIntPtr.Zero);
      Thread.Sleep(50);
      keybd_event((byte)VK_RIGHT, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }
    
    private static void SendSeekBackward()
    {
      TryFocusBrowserWindow();
      Thread.Sleep(200);
      keybd_event((byte)VK_LEFT, 0, 0, UIntPtr.Zero);
      Thread.Sleep(50);
      keybd_event((byte)VK_LEFT, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    private static void SendFullscreen()
    {
      TryFocusBrowserWindow();
      Thread.Sleep(200);
      keybd_event((byte)'F', 0, 0, UIntPtr.Zero); // F key down (enter fullscreen)
      Thread.Sleep(50);
      keybd_event((byte)'F', 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // F key up
    }

    private static void SendExitFullscreen()
    {
      keybd_event(0x1B, 0, 0, UIntPtr.Zero); // ESC key down (exit fullscreen)
      Thread.Sleep(50);
      keybd_event(0x1B, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // ESC key up
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [StructLayout(LayoutKind.Sequential)]
    struct INPUT { public uint type; public InputUnion U; }
    [StructLayout(LayoutKind.Explicit)]
    struct InputUnion { [FieldOffset(0)] public KEYBDINPUT ki; }
    [StructLayout(LayoutKind.Sequential)]
    struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }

    [DllImport("user32.dll", SetLastError = true)]
    static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    private static void SendKeyPressSendInput(ushort vk, int holdMs)
    {
        var inputsDown = new INPUT[] { new INPUT { type = 1, U = new InputUnion { ki = new KEYBDINPUT { wVk = vk, wScan = 0, dwFlags = 0, time = 0, dwExtraInfo = IntPtr.Zero } } } };
        SendInput((uint)inputsDown.Length, inputsDown, System.Runtime.InteropServices.Marshal.SizeOf(typeof(INPUT)));
        Thread.Sleep(Math.Max(holdMs, 1));
        var inputsUp = new INPUT[] { new INPUT { type = 1, U = new InputUnion { ki = new KEYBDINPUT { wVk = vk, wScan = 0, dwFlags = 2 /* KEYEVENTF_KEYUP */, time = 0, dwExtraInfo = IntPtr.Zero } } } };
        SendInput((uint)inputsUp.Length, inputsUp, System.Runtime.InteropServices.Marshal.SizeOf(typeof(INPUT)));
    }

    private static bool SendArrowScan(ushort scan)
    {
      try
      {
        var down = new INPUT[] { new INPUT { type = 1, U = new InputUnion { ki = new KEYBDINPUT { wVk = 0, wScan = scan, dwFlags = KEYEVENTF_SCANCODE | KEYEVENTF_EXTENDEDKEY, time = 0, dwExtraInfo = IntPtr.Zero } } } };
        var up = new INPUT[] { new INPUT { type = 1, U = new InputUnion { ki = new KEYBDINPUT { wVk = 0, wScan = scan, dwFlags = KEYEVENTF_SCANCODE | KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, time = 0, dwExtraInfo = IntPtr.Zero } } } };
        SendInput((uint)down.Length, down, System.Runtime.InteropServices.Marshal.SizeOf(typeof(INPUT)));
        Thread.Sleep(60);
        SendInput((uint)up.Length, up, System.Runtime.InteropServices.Marshal.SizeOf(typeof(INPUT)));
        return true;
      }
      catch
      {
        return false;
      }
    }

    private static void Log(string message)
    {
        try { File.AppendAllText(_logFilePath!, $"{DateTime.UtcNow:O} {message}\n"); } catch { }
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    private struct WINDOWPLACEMENT { public uint length; public uint flags; public uint showCmd; public System.Drawing.Point ptMinPosition; public System.Drawing.Point ptMaxPosition; public System.Drawing.Rectangle rcNormalPosition; }

    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;

    private static void ClickCenterOfActiveWindow()
    {
      try
      {
        IntPtr hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return;
        if (GetWindowRect(hwnd, out RECT rect))
        {
          int centerX = (rect.Left + rect.Right) / 2;
          int centerY = (rect.Top + rect.Bottom) / 2;
          SetCursorPos(centerX, centerY);
          mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
          Thread.Sleep(10);
          mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
        }
      }
      catch { }
    }

    private static void TryFocusBrowserWindow()
    {
      try
      {
        string[] candidates = new[] { "msedge", "chrome", "firefox", "brave", "opera" };
        foreach (var name in candidates)
        {
          var procs = Process.GetProcessesByName(name);
          foreach (var p in procs)
          {
            var h = p.MainWindowHandle;
            if (h == IntPtr.Zero) continue;
            SetForegroundWindow(h);
            Thread.Sleep(80);
            return;
          }
        }
      }
      catch { }
    }

    private static System.Collections.Generic.IEnumerable<string> GetLocalIPv4Addresses()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                yield return ip.ToString();
        }
    }

    private static string GetLocalHostname()
    {
      try
      {
        return Dns.GetHostName().ToLower();
      }
      catch
      {
        return Environment.MachineName.ToLower();
      }
    }

    private static bool IsBonjourAvailable()
    {
      try
      {
        // Check if mDNSResponder service is running (Bonjour)
        var services = System.ServiceProcess.ServiceController.GetServices();
        foreach (var service in services)
        {
          if (service.ServiceName.Equals("Bonjour Service", StringComparison.OrdinalIgnoreCase) ||
              service.ServiceName.Equals("mDNSResponder", StringComparison.OrdinalIgnoreCase))
          {
            return service.Status == System.ServiceProcess.ServiceControllerStatus.Running;
          }
        }
        
        // Also check if Bonjour executable exists (even if service isn't running)
        if (File.Exists(@"C:\Program Files\Bonjour\mDNSResponder.exe") ||
            File.Exists(@"C:\Program Files (x86)\Bonjour\mDNSResponder.exe"))
        {
          Log("Bonjour installed but service not running, using IP fallback");
        }
      }
      catch { }
      return false;
    }

    private static string? GetBestLocalAddress(System.Collections.Generic.IEnumerable<string> ips)
    {
      string? best = null;
      foreach (var ip in ips)
      {
        if (IsPrivateIPv4(ip))
        {
          // Prefer non-VM common ranges over others
          if (ip.StartsWith("192.168.")) return ip;
          if (best == null) best = ip;
        }
      }
      return best;
    }

    private static bool IsPrivateIPv4(string ip)
    {
      // Exclude localhost and APIPA
      if (ip.StartsWith("127.")) return false;
      if (ip.StartsWith("169.254.")) return false;
      // Private ranges
      if (ip.StartsWith("10.")) return true;
      if (ip.StartsWith("192.168.")) return true;
      var parts = ip.Split('.');
      if (parts.Length == 4 && parts[0] == "172")
      {
        if (int.TryParse(parts[1], out var b) && b >= 16 && b <= 31) return true;
      }
      return false;
    }

    private static byte[] GenerateQrPng(string text)
    {
      using var generator = new QRCodeGenerator();
      using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
      using var code = new QRCode(data);
      using var bmp = code.GetGraphic(12);
      using var ms = new MemoryStream();
      bmp.Save(ms, ImageFormat.Png);
      return ms.ToArray();
    }

    private static byte[] GenerateAppIconPng(int size)
    {
      // Try to load the actual icon file first
      try
      {
        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons", $"icon-{size}.png");
        if (File.Exists(iconPath))
          return File.ReadAllBytes(iconPath);
      }
      catch { }

      // Fallback: generate icon from scratch
      using var bmp = new Bitmap(size, size);
      using var g = Graphics.FromImage(bmp);
      g.SmoothingMode = SmoothingMode.AntiAlias;
      g.Clear(Color.Transparent);
      
      float scale = size / 256.0f;
      
      // Background circle (blue)
      using (var bgBrush = new SolidBrush(Color.FromArgb(25, 118, 210)))
      {
        g.FillEllipse(bgBrush, 8*scale, 8*scale, 240*scale, 240*scale);
      }
      
      // Sofa (red)
      using (var sofaBrush = new SolidBrush(Color.FromArgb(255, 107, 107)))
      {
        g.FillRectangle(sofaBrush, 60*scale, 100*scale, 136*scale, 12*scale); // back
        g.FillRectangle(sofaBrush, 65*scale, 112*scale, 126*scale, 40*scale); // seat
        g.FillRectangle(sofaBrush, 55*scale, 100*scale, 15*scale, 60*scale); // left arm
        g.FillRectangle(sofaBrush, 186*scale, 100*scale, 15*scale, 60*scale); // right arm
      }
      
      // Remote (dark gray)
      using (var remoteBrush = new SolidBrush(Color.FromArgb(52, 73, 94)))
      {
        g.FillRectangle(remoteBrush, 140*scale, 140*scale, 45*scale, 85*scale);
      }
      
      // Remote buttons
      using (var buttonBrush = new SolidBrush(Color.FromArgb(52, 152, 219)))
      {
        g.FillEllipse(buttonBrush, 154*scale, 152*scale, 16*scale, 16*scale);
      }
      
      using (var whiteBrush = new SolidBrush(Color.FromArgb(224, 224, 224)))
      {
        g.FillEllipse(whiteBrush, 145*scale, 175*scale, 10*scale, 10*scale);
        g.FillEllipse(whiteBrush, 157*scale, 175*scale, 10*scale, 10*scale);
        g.FillEllipse(whiteBrush, 170*scale, 175*scale, 10*scale, 10*scale);
      }
      
      using var ms2 = new MemoryStream();
      bmp.Save(ms2, ImageFormat.Png);
      return ms2.ToArray();
    }

    private static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
      int d = radius * 2;
      var path = new GraphicsPath();
      var arc = new Rectangle(r.Location, new Size(d, d));
      path.AddArc(arc, 180, 90);
      arc.X = r.Right - d;
      path.AddArc(arc, 270, 90);
      arc.Y = r.Bottom - d;
      path.AddArc(arc, 0, 90);
      arc.X = r.Left;
      path.AddArc(arc, 90, 90);
      path.CloseFigure();
      return path;
    }

    private static bool TryEnsureNetworkPermissions()
    {
      try
      {
        var ps = new ProcessStartInfo
        {
          FileName = "powershell.exe",
          Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"try { netsh http add urlacl url=http://+:8080/ user=$env:USERNAME | Out-Null; netsh advfirewall firewall add rule name='SofaRemote 8080 Private' dir=in action=allow protocol=TCP localport=8080 profile=private | Out-Null; netsh advfirewall firewall add rule name='SofaRemote 8080 Public' dir=in action=allow protocol=TCP localport=8080 profile=public | Out-Null; exit 0 } catch { exit 1 }\"",
          Verb = "runas",
          UseShellExecute = true,
          WindowStyle = ProcessWindowStyle.Hidden
        };
        using var proc = Process.Start(ps);
        if (proc == null) return false;
        proc.WaitForExit(20000);
        var ok = proc.ExitCode == 0;
        try { Log("TryEnsureNetworkPermissions exit=" + proc.ExitCode); } catch { }
        return ok;
      }
      catch (Exception ex)
      {
        try { Log("TryEnsureNetworkPermissions error: " + ex.Message); } catch { }
        return false;
      }
    }

    private static string? GetCurrentWiFiSSID()
    {
      try
      {
        var process = new Process
        {
          StartInfo = new ProcessStartInfo
          {
            FileName = "netsh",
            Arguments = "wlan show interfaces",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
          }
        };
        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        
        foreach (var line in output.Split('\n'))
        {
          var trimmed = line.Trim();
          if (trimmed.StartsWith("SSID", StringComparison.OrdinalIgnoreCase) && trimmed.Contains(":"))
          {
            var parts = trimmed.Split(new[] { ':' }, 2);
            if (parts.Length == 2 && !parts[0].Contains("BSSID"))
            {
              return parts[1].Trim();
            }
          }
        }
      }
      catch { }
      return null;
    }

    private static string? GetCurrentWiFiPassword(string ssid)
    {
      try
      {
        var process = new Process
        {
          StartInfo = new ProcessStartInfo
          {
            FileName = "netsh",
            Arguments = $"wlan show profile name=\"{ssid}\" key=clear",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
          }
        };
        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        
        foreach (var line in output.Split('\n'))
        {
          var trimmed = line.Trim();
          if (trimmed.StartsWith("Key Content", StringComparison.OrdinalIgnoreCase) && trimmed.Contains(":"))
          {
            var parts = trimmed.Split(new[] { ':' }, 2);
            if (parts.Length == 2)
            {
              return parts[1].Trim();
            }
          }
        }
      }
      catch { }
      return null;
    }

    private static void ShowQrWindow()
    {
      var url = _primaryUrl ?? "http://localhost:8080/";
      
      // Create relay URL for remote access
      var relayUrl = $"https://sofaremote-production.up.railway.app/remote?session={_sessionCode}";
      
      // Create URL with session code for local network with relay fallback
      var urlWithSession = url;
      if (_sessionCode != null)
      {
        urlWithSession += $"?session={_sessionCode}";
      }
      
      var ssid = GetCurrentWiFiSSID();
      var password = ssid != null ? GetCurrentWiFiPassword(ssid) : null;
      
      // Generate QR for relay URL (works from anywhere)
      var relayPng = GenerateQrPng(relayUrl);
      using var relayMs = new MemoryStream(relayPng);
      using var relayImg = Image.FromStream(relayMs);

      byte[]? wifiPng = null;
      Image? wifiImg = null;
      if (password != null)
      {
        var wifiQrData = $"WIFI:T:WPA;S:{ssid};P:{password};;";
        wifiPng = GenerateQrPng(wifiQrData);
        using var wifiMs = new MemoryStream(wifiPng);
        wifiImg = Image.FromStream(wifiMs);
      }

      var form = new Form
      {
        Text = "Sofa Remote",
        FormBorderStyle = FormBorderStyle.FixedDialog,
        MaximizeBox = false,
        MinimizeBox = false,
        StartPosition = FormStartPosition.CenterScreen,
        TopMost = true,
        BackColor = Color.FromArgb(15, 23, 42),
        ForeColor = Color.FromArgb(241, 245, 249),
        ClientSize = new Size(400, 650),
        Padding = new Padding(20)
      };

      var titlePanel = new Panel
      {
        Dock = DockStyle.Top,
        Height = 60,
        BackColor = Color.FromArgb(15, 23, 42)
      };
      
      var title = new Label
      {
        Text = "🛋️ Sofa Remote",
        Font = new Font("Segoe UI", 16, FontStyle.Bold),
        ForeColor = Color.FromArgb(241, 245, 249),
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter
      };
      titlePanel.Controls.Add(title);
      form.Controls.Add(titlePanel);

      // WiFi network info
      var wifiLabel = new Label
      {
        Text = ssid != null ? $"📶 Network: {ssid}" : "📶 Connect to same WiFi network",
        Dock = DockStyle.Top,
        Height = 40,
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        ForeColor = Color.FromArgb(34, 197, 94),
        BackColor = Color.FromArgb(20, 30, 48),
        TextAlign = ContentAlignment.MiddleCenter,
        Padding = new Padding(10)
      };
      form.Controls.Add(wifiLabel);

      // QR Code panel
      var qrPanel = new Panel
      {
        Dock = DockStyle.Top,
        Height = 360,
        BackColor = Color.White,
        Padding = new Padding(10)
      };
      
      var qrPb = new PictureBox
      {
        Image = (Image)relayImg.Clone(),
        SizeMode = PictureBoxSizeMode.Zoom,
        Dock = DockStyle.Fill,
        Tag = "relay" // Show relay QR by default
      };
      qrPanel.Controls.Add(qrPb);
      form.Controls.Add(qrPanel);

      var qrLabel = new Label
      {
        Text = "📱 Remote Access (Any Network)",
        Dock = DockStyle.Top,
        Height = 25,
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        ForeColor = Color.FromArgb(76, 175, 80),
        TextAlign = ContentAlignment.MiddleCenter
      };
      form.Controls.Add(qrLabel);

      var urlLabel = new Label
      {
        Text = relayUrl,
        Dock = DockStyle.Top,
        Height = 45,
        Font = new Font("Consolas", 8),
        ForeColor = Color.FromArgb(148, 163, 184),
        TextAlign = ContentAlignment.MiddleCenter
      };
      form.Controls.Add(urlLabel);

      // WiFi QR slide-down panel (only if password available)
      if (password != null && wifiImg != null)
      {
        var wifiSlidePanel = new Panel
        {
          Dock = DockStyle.Top,
          Height = 0, // Start collapsed
          BackColor = Color.FromArgb(15, 23, 42),
          Padding = new Padding(0)
        };

        var wifiQrContainer = new Panel
        {
          Location = new Point(20, 10),
          Size = new Size(360, 290),
          BackColor = Color.FromArgb(20, 30, 48),
          Padding = new Padding(10)
        };

        var wifiQrWhitePanel = new Panel
        {
          Location = new Point(30, 35),
          Size = new Size(240, 240),
          BackColor = Color.White,
          Padding = new Padding(10)
        };

        var wifiQrPb = new PictureBox
        {
          Image = (Image)wifiImg.Clone(),
          SizeMode = PictureBoxSizeMode.Zoom,
          Dock = DockStyle.Fill
        };
        wifiQrWhitePanel.Controls.Add(wifiQrPb);

        var wifiQrTitle = new Label
        {
          Text = "📶 WiFi Connection",
          Location = new Point(10, 10),
          Size = new Size(280, 25),
          Font = new Font("Segoe UI", 10, FontStyle.Bold),
          ForeColor = Color.FromArgb(34, 197, 94),
          TextAlign = ContentAlignment.MiddleCenter
        };
        wifiQrContainer.Controls.Add(wifiQrTitle);
        wifiQrContainer.Controls.Add(wifiQrWhitePanel);

        wifiSlidePanel.Controls.Add(wifiQrContainer);
        form.Controls.Add(wifiSlidePanel);

        var toggleBtn = new Button
        {
          Text = "▼ Show WiFi QR",
          Dock = DockStyle.Top,
          Height = 40,
          Font = new Font("Segoe UI", 10, FontStyle.Bold),
          BackColor = Color.FromArgb(34, 197, 94),
          ForeColor = Color.White,
          FlatStyle = FlatStyle.Flat,
          Cursor = Cursors.Hand,
          Margin = new Padding(0, 5, 0, 5)
        };
        toggleBtn.FlatAppearance.BorderSize = 0;

        var slideTimer = new System.Windows.Forms.Timer();
        slideTimer.Interval = 10; // 100 FPS for ultra-smooth animation
        var isExpanded = false;
        var targetHeight = 310;
        var animationProgress = 0.0;
        var animationSpeed = 0.15; // Higher = faster (0.15 = ~400ms total)
        
        slideTimer.Tick += (_, __) =>
        {
          if (isExpanded)
          {
            // Animate towards 1.0
            animationProgress += animationSpeed;
            if (animationProgress >= 1.0)
            {
              animationProgress = 1.0;
              slideTimer.Stop();
            }
            // Bounce easing: overshoots then settles
            var eased = animationProgress < 0.5 
              ? 2 * animationProgress * animationProgress 
              : 1 - Math.Pow(-2 * animationProgress + 2, 2) / 2;
            // Add subtle bounce at the end
            if (animationProgress > 0.8)
            {
              var bounce = Math.Sin((animationProgress - 0.8) * Math.PI * 5) * 0.05;
              eased += bounce;
            }
            wifiSlidePanel.Height = (int)(targetHeight * Math.Min(eased, 1.0));
          }
          else
          {
            // Animate towards 0.0
            animationProgress -= animationSpeed;
            if (animationProgress <= 0.0)
            {
              animationProgress = 0.0;
              wifiSlidePanel.Height = 0;
              slideTimer.Stop();
            }
            else
            {
              var eased = 1 - Math.Pow(1 - animationProgress, 3); // Ease out cubic
              wifiSlidePanel.Height = (int)(targetHeight * eased);
            }
          }
        };
        
        toggleBtn.Click += (_, __) =>
        {
          isExpanded = !isExpanded;
          if (isExpanded)
          {
            animationProgress = 0.0;
            toggleBtn.Text = "▲ Hide WiFi QR";
            toggleBtn.BackColor = Color.FromArgb(239, 68, 68);
          }
          else
          {
            animationProgress = 1.0;
            toggleBtn.Text = "▼ Show WiFi QR";
            toggleBtn.BackColor = Color.FromArgb(34, 197, 94);
          }
          slideTimer.Start();
        };
        form.Controls.Add(toggleBtn);
      }

      var refreshBtn = new Button
      {
        Text = "🔄 Refresh IP",
        Dock = DockStyle.Top,
        Height = 40,
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        BackColor = Color.FromArgb(25, 118, 210),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Cursor = Cursors.Hand,
        Margin = new Padding(0, 5, 0, 10)
      };
      refreshBtn.FlatAppearance.BorderSize = 0;
      refreshBtn.Click += (_, __) =>
      {
        form.Controls.Clear();
        form.Dispose();
        ShowQrWindow();
      };
      form.Controls.Add(refreshBtn);

      var instructionLabel = new Label
      {
        Text = password != null 
          ? "Scan WiFi QR → Connect → Scan App QR\nPress ESC to close" 
          : "Scan QR code or enter URL on your phone\nPress ESC to close",
        Dock = DockStyle.Fill,
        Font = new Font("Segoe UI", 9),
        ForeColor = Color.FromArgb(148, 163, 184),
        TextAlign = ContentAlignment.TopCenter,
        Padding = new Padding(0, 10, 0, 0)
      };
      form.Controls.Add(instructionLabel);

      form.KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) form.Close(); };
      form.Show();
    }

    private static void RestartElevated()
    {
      try
      {
        var exe = Application.ExecutablePath;
        var psi = new ProcessStartInfo(exe)
        {
          UseShellExecute = true,
          Verb = "runas"
        };
        Process.Start(psi);
      }
      catch (Exception ex)
      {
        try { Log("RestartElevated error: " + ex.Message); } catch { }
      }
      finally
      {
        try { _cts?.Cancel(); } catch { }
        Application.Exit();
      }
    }

    // Update Feature
    private static async Task CheckForUpdates(bool manual)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "SofaRemote");
            client.Timeout = TimeSpan.FromSeconds(10);
            
            var response = await client.GetStringAsync("https://api.github.com/repos/lilsid/SofaRemote/releases/latest");
            var json = JsonDocument.Parse(response);
            var root = json.RootElement;
            
            var latestVersion = root.GetProperty("tag_name").GetString()?.TrimStart('v') ?? "";
            if (string.IsNullOrEmpty(latestVersion)) return;
            
            var currentVersion = AppVersion;
            if (latestVersion == currentVersion)
            {
                if (manual)
                {
                    _tray?.ShowBalloonTip(3000, "No Updates", "You're running the latest version!", ToolTipIcon.Info);
                }
                return;
            }
            
            var releaseNotes = root.GetProperty("body").GetString() ?? "";
            var downloadUrl = "";
            long fileSize = 0;
            
            if (root.TryGetProperty("assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString() ?? "";
                    if (name.StartsWith("SofaRemote-Setup-") && name.EndsWith(".exe"))
                    {
                        downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";
                        fileSize = asset.GetProperty("size").GetInt64();
                        break;
                    }
                }
            }
            
            if (string.IsNullOrEmpty(downloadUrl)) return;
            
            // Show update dialog on UI thread
            var context = SynchronizationContext.Current;
            if (context != null)
            {
                context.Post(_ => ShowUpdateDialog(latestVersion, releaseNotes, downloadUrl, fileSize), null);
            }
            else
            {
                ShowUpdateDialog(latestVersion, releaseNotes, downloadUrl, fileSize);
            }
        }
        catch
        {
            if (manual)
            {
                _tray?.ShowBalloonTip(3000, "Update Check Failed", "Could not connect to update server", ToolTipIcon.Warning);
            }
        }
    }
    
    private static void ShowUpdateDialog(string newVersion, string notes, string downloadUrl, long fileSize)
    {
        var dialog = new Form
        {
            Text = "Update Available",
            Size = new Size(380, 280),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            StartPosition = FormStartPosition.CenterScreen,
            BackColor = Color.White,
            Font = new Font("Segoe UI", 9)
        };
        
        var titleLabel = new Label
        {
            Text = "New version available",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            Location = new Point(20, 20),
            AutoSize = true,
            ForeColor = Color.FromArgb(60, 60, 60)
        };
        
        var versionLabel = new Label
        {
            Text = $"v{AppVersion}  →  v{newVersion}",
            Font = new Font("Segoe UI", 10),
            Location = new Point(20, 50),
            AutoSize = true,
            ForeColor = Color.FromArgb(25, 118, 210)
        };
        
        var sizeLabel = new Label
        {
            Text = $"Size: {fileSize / 1024.0 / 1024.0:F1} MB",
            Font = new Font("Segoe UI", 8),
            Location = new Point(20, 75),
            AutoSize = true,
            ForeColor = Color.Gray
        };
        
        var notesBox = new TextBox
        {
            Text = notes.Length > 200 ? notes.Substring(0, 200) + "..." : notes,
            Font = new Font("Segoe UI", 9),
            Location = new Point(20, 105),
            Size = new Size(320, 80),
            Multiline = true,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = Color.FromArgb(248, 248, 248)
        };
        
        var downloadBtn = new Button
        {
            Text = "Download & Install",
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Location = new Point(20, 200),
            Size = new Size(150, 35),
            BackColor = Color.FromArgb(25, 118, 210),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        downloadBtn.FlatAppearance.BorderSize = 0;
        downloadBtn.Click += (s, e) => { dialog.Hide(); Task.Run(() => DownloadUpdate(downloadUrl, newVersion)); };
        
        var laterBtn = new Button
        {
            Text = "Later",
            Font = new Font("Segoe UI", 9),
            Location = new Point(185, 200),
            Size = new Size(75, 35),
            BackColor = Color.FromArgb(240, 240, 240),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        laterBtn.FlatAppearance.BorderSize = 0;
        laterBtn.Click += (s, e) => dialog.Close();
        
        dialog.Controls.AddRange(new Control[] { titleLabel, versionLabel, sizeLabel, notesBox, downloadBtn, laterBtn });
        dialog.ShowDialog();
    }
    
    private static async Task DownloadUpdate(string url, string version)
    {
        Form? progressDialog = null;
        ProgressBar? progressBar = null;
        Label? progressLabel = null;
        
        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"SofaRemote-Setup-v{version}.exe");
            
            // Show progress dialog
            Application.OpenForms[0]?.Invoke(() =>
            {
                progressDialog = new Form
                {
                    Text = "Downloading...",
                    Size = new Size(380, 180),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    StartPosition = FormStartPosition.CenterScreen,
                    BackColor = Color.White,
                    Font = new Font("Segoe UI", 9)
                };
                
                var titleLabel = new Label
                {
                    Text = "Downloading update",
                    Font = new Font("Segoe UI", 11, FontStyle.Bold),
                    Location = new Point(20, 20),
                    AutoSize = true
                };
                
                var fileLabel = new Label
                {
                    Text = $"SofaRemote-Setup-v{version}.exe",
                    Font = new Font("Segoe UI", 8),
                    Location = new Point(20, 50),
                    AutoSize = true,
                    ForeColor = Color.Gray
                };
                
                progressBar = new ProgressBar
                {
                    Location = new Point(20, 75),
                    Size = new Size(320, 20),
                    Style = ProgressBarStyle.Continuous
                };
                
                progressLabel = new Label
                {
                    Text = "0%",
                    Font = new Font("Segoe UI", 8),
                    Location = new Point(20, 100),
                    AutoSize = true,
                    ForeColor = Color.FromArgb(100, 100, 100)
                };
                
                var cancelBtn = new Button
                {
                    Text = "Cancel",
                    Font = new Font("Segoe UI", 9),
                    Location = new Point(140, 125),
                    Size = new Size(80, 30),
                    BackColor = Color.FromArgb(240, 240, 240),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Tag = false
                };
                cancelBtn.FlatAppearance.BorderSize = 0;
                cancelBtn.Click += (s, e) => { cancelBtn.Tag = true; progressDialog.Close(); };
                
                progressDialog.Controls.AddRange(new Control[] { titleLabel, fileLabel, progressBar, progressLabel, cancelBtn });
                progressDialog.Show();
            });
            
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(10);
            
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            
            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
            
            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;
            
            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                totalRead += bytesRead;
                
                if (totalBytes > 0 && progressBar != null && progressLabel != null)
                {
                    var progress = (int)((totalRead * 100) / totalBytes);
                    progressBar.Invoke(() => progressBar.Value = progress);
                    progressLabel.Invoke(() => progressLabel.Text = $"{progress}%  •  {totalRead / 1024.0 / 1024.0:F1} / {totalBytes / 1024.0 / 1024.0:F1} MB");
                }
            }
            
            progressDialog?.Invoke(() => progressDialog.Close());
            
            // Show completion dialog
            Application.OpenForms[0]?.Invoke(() => ShowInstallDialog(tempPath, version));
        }
        catch
        {
            progressDialog?.Invoke(() => progressDialog.Close());
            _tray?.ShowBalloonTip(3000, "Download Failed", "Could not download update", ToolTipIcon.Error);
        }
    }
    
    private static void ShowInstallDialog(string installerPath, string version)
    {
        var dialog = new Form
        {
            Text = "Ready to Install",
            Size = new Size(380, 200),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            StartPosition = FormStartPosition.CenterScreen,
            BackColor = Color.White,
            Font = new Font("Segoe UI", 9)
        };
        
        var titleLabel = new Label
        {
            Text = "✓  Download complete",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            Location = new Point(20, 20),
            AutoSize = true,
            ForeColor = Color.FromArgb(76, 175, 80)
        };
        
        var infoLabel = new Label
        {
            Text = $"v{version} is ready to install",
            Font = new Font("Segoe UI", 9),
            Location = new Point(20, 55),
            AutoSize = true,
            ForeColor = Color.FromArgb(100, 100, 100)
        };
        
        var noteLabel = new Label
        {
            Text = "App will close after clicking Install",
            Font = new Font("Segoe UI", 8),
            Location = new Point(20, 80),
            AutoSize = true,
            ForeColor = Color.Gray
        };
        
        var installBtn = new Button
        {
            Text = "Install Now",
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Location = new Point(20, 120),
            Size = new Size(110, 35),
            BackColor = Color.FromArgb(76, 175, 80),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        installBtn.FlatAppearance.BorderSize = 0;
        installBtn.Click += (s, e) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = installerPath,
                    UseShellExecute = true
                });
                Application.Exit();
            }
            catch { }
        };
        
        var laterBtn = new Button
        {
            Text = "Later",
            Font = new Font("Segoe UI", 9),
            Location = new Point(145, 120),
            Size = new Size(70, 35),
            BackColor = Color.FromArgb(240, 240, 240),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        laterBtn.FlatAppearance.BorderSize = 0;
        laterBtn.Click += (s, e) => dialog.Close();
        
        var folderBtn = new Button
        {
            Text = "Open Folder",
            Font = new Font("Segoe UI", 8),
            Location = new Point(230, 120),
            Size = new Size(90, 35),
            BackColor = Color.FromArgb(240, 240, 240),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        folderBtn.FlatAppearance.BorderSize = 0;
        folderBtn.Click += (s, e) =>
        {
            try
            {
                Process.Start("explorer.exe", $"/select,\"{installerPath}\"");
            }
            catch { }
        };
        
        dialog.Controls.AddRange(new Control[] { titleLabel, infoLabel, noteLabel, installBtn, laterBtn, folderBtn });
        dialog.ShowDialog();
    }
    
    // Relay Server Methods
    private static string GenerateSessionCode()
    {
        var words = new[] { "WOLF", "LION", "BEAR", "HAWK", "TIGER", "EAGLE", "SHARK", "COBRA", "RAVEN", "PANDA" };
        var random = new Random();
        var word = words[random.Next(words.Length)];
        var number = random.Next(1000, 9999);
        return $"{word}-{number}";
    }
    
    private static async Task ConnectToRelay()
    {
        // Always generate session code
        _sessionCode = GenerateSessionCode();
        
        try
        {
            _relayClient = new ClientWebSocket();
            
            var uri = new Uri(RelayServerUrl);
            await _relayClient.ConnectAsync(uri, _cts!.Token);
            
            // Register as PC
            var registerMsg = new
            {
                type = "register",
                clientType = "pc",
                sessionCode = _sessionCode,
                pcName = Environment.MachineName
            };
            
            var json = JsonSerializer.Serialize(registerMsg);
            var bytes = Encoding.UTF8.GetBytes(json);
            await _relayClient.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
            
            _relayConnected = true;
            Log($"Connected to relay: {_sessionCode}");
            
            // Start listening for relay messages
            _ = Task.Run(() => RelayListenLoop(_cts.Token));
        }
        catch (Exception ex)
        {
            Log($"Relay connection failed: {ex.Message}");
            _relayConnected = false;
        }
    }
    
    private static async Task RelayListenLoop(CancellationToken ct)
    {
        var buffer = new byte[4096];
        
        try
        {
            while (_relayClient != null && _relayClient.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var result = await _relayClient.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
                
                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                
                // Handle messages from phone via relay
                if (root.TryGetProperty("action", out var action))
                {
                    HandlePhoneCommand(action.GetString() ?? "");
                }
            }
        }
        catch (Exception ex)
        {
            if (!ct.IsCancellationRequested)
            {
                Log($"Relay listen error: {ex.Message}");
            }
        }
        finally
        {
            _relayConnected = false;
        }
    }
    
    private static async Task SendRelayMessage(object data)
    {
        try
        {
            if (_relayClient == null || _relayClient.State != WebSocketState.Open) return;
            
            var msg = new { type = "relay", data };
            var json = JsonSerializer.Serialize(msg);
            var bytes = Encoding.UTF8.GetBytes(json);
            
            await _relayClient.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Log($"Relay send error: {ex.Message}");
        }
    }
    
    private static void HandlePhoneCommand(string action)
    {
        // Handle commands received from phone via relay
        switch (action)
        {
            case "play":
            case "pause":
                keybd_event(VK_MEDIA_PLAY_PAUSE, 0, 0, UIntPtr.Zero);
                keybd_event(VK_MEDIA_PLAY_PAUSE, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                break;
            case "next":
                keybd_event(VK_MEDIA_NEXT_TRACK, 0, 0, UIntPtr.Zero);
                keybd_event(VK_MEDIA_NEXT_TRACK, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                break;
            case "prev":
                keybd_event(VK_MEDIA_PREV_TRACK, 0, 0, UIntPtr.Zero);
                keybd_event(VK_MEDIA_PREV_TRACK, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                break;
            case "volup":
                keybd_event(VK_VOLUME_UP, 0, 0, UIntPtr.Zero);
                keybd_event(VK_VOLUME_UP, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                break;
            case "voldown":
                keybd_event(VK_VOLUME_DOWN, 0, 0, UIntPtr.Zero);
                keybd_event(VK_VOLUME_DOWN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                break;
        }
    }
}
