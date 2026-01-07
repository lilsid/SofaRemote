using System.Linq;

namespace SofaRemote
{
    public static class DashboardHtml
    {
        public static string GetHtml(string version, string pcName, string[] urls, string sessionCode, string relayUrl)
        {
            var urlList = string.Join("", urls.Select(u => $"<div class='url-item'><code>{u}</code></div>"));
            
            return $@"<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <meta name='viewport' content='width=device-width,initial-scale=1'>
  <title>SofaRemote Server</title>
  <style>
    * {{ margin: 0; padding: 0; box-sizing: border-box; }}
    body {{
      font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
      background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%);
      color: #f1f5f9;
      min-height: 100vh;
      padding: 40px 20px;
    }}
    .container {{
      max-width: 900px;
      margin: 0 auto;
    }}
    .header {{
      text-align: center;
      margin-bottom: 40px;
      animation: fadeIn 0.5s ease-out;
    }}
    .logo {{
      width: 80px;
      height: 80px;
      margin: 0 auto 20px;
    }}
    h1 {{
      font-size: 32px;
      font-weight: 600;
      margin-bottom: 8px;
      background: linear-gradient(135deg, #60a5fa, #3b82f6);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
      background-clip: text;
    }}
    .version {{
      color: #94a3b8;
      font-size: 14px;
    }}
    .card {{
      background: rgba(30, 41, 59, 0.6);
      border: 1px solid rgba(71, 85, 105, 0.3);
      border-radius: 16px;
      padding: 24px;
      margin-bottom: 20px;
      backdrop-filter: blur(10px);
      animation: slideUp 0.5s ease-out;
      box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
    }}
    .card-title {{
      font-size: 18px;
      font-weight: 600;
      margin-bottom: 16px;
      color: #e2e8f0;
      display: flex;
      align-items: center;
      gap: 10px;
    }}
    .card-title::before {{
      content: '';
      width: 4px;
      height: 20px;
      background: linear-gradient(180deg, #3b82f6, #1d4ed8);
      border-radius: 2px;
    }}
    .info-grid {{
      display: grid;
      grid-template-columns: 140px 1fr;
      gap: 12px;
      font-size: 14px;
    }}
    .info-label {{
      color: #94a3b8;
      font-weight: 500;
    }}
    .info-value {{
      color: #f1f5f9;
      font-family: 'Consolas', 'Monaco', monospace;
    }}
    .status-badge {{
      display: inline-flex;
      align-items: center;
      gap: 8px;
      padding: 6px 14px;
      background: rgba(16, 185, 129, 0.1);
      border: 1px solid rgba(16, 185, 129, 0.3);
      border-radius: 20px;
      color: #10b981;
      font-size: 13px;
      font-weight: 600;
    }}
    .status-dot {{
      width: 8px;
      height: 8px;
      background: #10b981;
      border-radius: 50%;
      box-shadow: 0 0 10px rgba(16, 185, 129, 0.6);
      animation: pulse 2s ease-in-out infinite;
    }}
    .url-item {{
      background: rgba(15, 23, 42, 0.5);
      padding: 12px;
      border-radius: 8px;
      margin-bottom: 8px;
      border: 1px solid rgba(71, 85, 105, 0.2);
    }}
    .url-item code {{
      color: #60a5fa;
      font-size: 14px;
      word-break: break-all;
    }}
    .button-grid {{
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 16px;
      margin-top: 16px;
    }}
    .btn {{
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 10px;
      padding: 16px 24px;
      background: linear-gradient(135deg, #3b82f6, #2563eb);
      border: none;
      border-radius: 12px;
      color: white;
      font-size: 15px;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.3s;
      box-shadow: 0 4px 12px rgba(59, 130, 246, 0.4);
      text-decoration: none;
    }}
    .btn:hover {{
      transform: translateY(-2px);
      box-shadow: 0 6px 16px rgba(59, 130, 246, 0.5);
    }}
    .btn:active {{
      transform: translateY(0);
    }}
    .btn-secondary {{
      background: linear-gradient(135deg, #64748b, #475569);
      box-shadow: 0 4px 12px rgba(71, 85, 105, 0.4);
    }}
    .btn-secondary:hover {{
      box-shadow: 0 6px 16px rgba(71, 85, 105, 0.5);
    }}
    .qr-section {{
      text-align: center;
      padding: 20px;
    }}
    .qr-img {{
      background: white;
      padding: 16px;
      border-radius: 12px;
      display: inline-block;
      margin: 16px 0;
      box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
    }}
    .qr-img img {{
      display: block;
      width: 200px;
      height: 200px;
    }}
    .qr-caption {{
      color: #94a3b8;
      font-size: 13px;
      margin-top: 12px;
    }}
    @keyframes fadeIn {{
      from {{ opacity: 0; transform: translateY(-20px); }}
      to {{ opacity: 1; transform: translateY(0); }}
    }}
    @keyframes slideUp {{
      from {{ opacity: 0; transform: translateY(20px); }}
      to {{ opacity: 1; transform: translateY(0); }}
    }}
    @keyframes pulse {{
      0%, 100% {{ opacity: 1; }}
      50% {{ opacity: 0.5; }}
    }}
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'>
      <svg class='logo' viewBox='0 0 64 64' xmlns='http://www.w3.org/2000/svg'>
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
      <h1>SofaRemote Server</h1>
      <div class='version'>Version {version}</div>
    </div>

    <div class='card'>
      <div class='card-title'>Server Status</div>
      <div style='margin-bottom: 16px;'>
        <span class='status-badge'>
          <span class='status-dot'></span>
          Running
        </span>
      </div>
      <div class='info-grid'>
        <div class='info-label'>Computer Name:</div>
        <div class='info-value'>{pcName}</div>
        
        <div class='info-label'>Session Code:</div>
        <div class='info-value'>{sessionCode}</div>
        
        <div class='info-label'>Local Port:</div>
        <div class='info-value'>8080</div>
      </div>
    </div>

    <div class='card'>
      <div class='card-title'>Connection URLs</div>
      <div style='margin-bottom: 12px; color: #94a3b8; font-size: 14px;'>
        Access from devices on your local network:
      </div>
      {urlList}
      
      <div style='margin-top: 20px; margin-bottom: 12px; color: #94a3b8; font-size: 14px;'>
        Remote access (works anywhere):
      </div>
      <div class='url-item'>
        <code>{relayUrl}</code>
      </div>
    </div>

    <div class='card'>
      <div class='card-title'>Quick Actions</div>
      <div class='button-grid'>
        <a href='/remote' class='btn'>
          🎮 Open Remote Control
        </a>
        <a href='/qr' class='btn-secondary btn'>
          📱 Show QR Code
        </a>
      </div>
    </div>

    <div class='card'>
      <div class='card-title'>Mobile Setup</div>
      <div class='qr-section'>
        <div style='color: #94a3b8; margin-bottom: 16px;'>
          Scan this QR code with your phone to connect:
        </div>
        <div class='qr-img'>
          <img src='/qr.png' alt='QR Code'/>
        </div>
        <div class='qr-caption'>
          Works from anywhere • Session: {sessionCode}
        </div>
      </div>
    </div>
  </div>
</body>
</html>";
        }
    }
}
