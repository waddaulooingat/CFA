document.getElementById('today-date').textContent =
  new Date().toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' });

// ─── Tabs ─────────────────────────────────────────────────────────────────────
document.querySelectorAll('.tab').forEach(tab => {
  tab.addEventListener('click', () => {
    document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
    document.querySelectorAll('.panel').forEach(p => p.classList.remove('active'));
    tab.classList.add('active');
    document.getElementById('tab-' + tab.dataset.tab).classList.add('active');
  });
});

// ─── Today stats ──────────────────────────────────────────────────────────────
async function loadTodayStats() {
  const stats = await sendMsg({ type: 'GET_TODAY_STATS' });
  const el = document.getElementById('stats-list');

  const active = stats.filter(s => s.seconds > 0 || s.blocked);
  if (!active.length) {
    el.innerHTML = '<div class="empty">No activity tracked today.</div>';
    return;
  }

  const maxSec = Math.max(...active.map(s => s.seconds), 1);

  el.innerHTML = active.map(s => {
    const pct  = Math.min(100, Math.round(s.seconds / maxSec * 100));
    const barClass = s.blocked ? 'block' : s.seconds >= 60*60 ? 'warn' : '';
    const badge = s.blocked
      ? `<span class="badge blocked">Blocked ${s.remainingMinutes}m left</span>`
      : s.seconds >= 60*60
        ? `<span class="badge warning">⚠ ${fmt(s.seconds)}</span>`
        : `<span class="badge ok">${fmt(s.seconds)}</span>`;

    const unblockBtn = s.blocked
      ? `<button class="unblock-btn" data-domain="${s.domain}">Unblock</button>`
      : '';

    return `<div class="site-row">
      <div style="flex:1;min-width:0">
        <div style="display:flex;justify-content:space-between;align-items:center">
          <div class="site-name">${s.domain}</div>
          ${badge}
        </div>
        <div class="bar-wrap"><div class="bar-fill ${barClass}" style="width:${pct}%"></div></div>
        <div class="site-time">${fmt(s.seconds)} today</div>
      </div>
      ${unblockBtn ? `<div style="margin-left:8px">${unblockBtn}</div>` : ''}
    </div>`;
  }).join('');

  el.querySelectorAll('.unblock-btn').forEach(btn => {
    btn.addEventListener('click', async () => {
      await sendMsg({ type: 'UNBLOCK_DOMAIN', domain: btn.dataset.domain });
      loadTodayStats();
    });
  });
}

// ─── Time limit sites (read-only — managed by WinResMonitor app) ──────────────
async function loadLimits() {
  const data = await sendMsg({ type: 'GET_SETTINGS' });
  const el   = document.getElementById('limit-list');
  const list = data.timeLimitSites || [];

  el.innerHTML = list.length
    ? list.map(d => `<div class="list-item"><span>${d}</span></div>`).join('')
    : '<div class="empty">No sites configured.</div>';
}

// ─── Whitelist (read-only — managed by WinResMonitor app) ────────────────────
async function loadWhitelist() {
  const data = await sendMsg({ type: 'GET_SETTINGS' });
  const el   = document.getElementById('white-list');
  const list = data.whitelist || [];

  el.innerHTML = list.length
    ? list.map(d => `<div class="list-item"><span>${d}</span></div>`).join('')
    : '<div class="empty">No sites whitelisted.</div>';
}

// ─── Utils ────────────────────────────────────────────────────────────────────
function fmt(seconds) {
  if (seconds <= 0) return '0m';
  const h = Math.floor(seconds / 3600);
  const m = Math.floor((seconds % 3600) / 60);
  if (h > 0) return `${h}h ${m}m`;
  if (m > 0) return `${m}m`;
  return `${seconds}s`;
}

function sendMsg(msg) {
  return new Promise(resolve => chrome.runtime.sendMessage(msg, resolve));
}

// ─── Report ───────────────────────────────────────────────────────────────────
function dateKey(d) {
  return d.toISOString().slice(0, 10);
}

function lastNDays(n) {
  const days = [];
  for (let i = n - 1; i >= 0; i--) {
    const d = new Date();
    d.setDate(d.getDate() - i);
    days.push(dateKey(d));
  }
  return days;
}

function fmtDate(key) {
  const [y, m, d] = key.split('-');
  return new Date(y, m - 1, d).toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' });
}

async function generateReport(days) {
  const data = await sendMsg({ type: 'GET_REPORT_DATA', days });
  const { timeData, timeLimitSites } = data;

  // Aggregate totals per domain across the date range
  const domainTotals = {};
  const dailyTotals  = {};

  for (const date of days) {
    dailyTotals[date] = 0;
    for (const domain of timeLimitSites) {
      const secs = (timeData[domain] || {})[date] || 0;
      domainTotals[domain] = (domainTotals[domain] || 0) + secs;
      dailyTotals[date] += secs;
    }
  }

  const totalSecs   = Object.values(domainTotals).reduce((a, b) => a + b, 0);
  const totalHours  = (totalSecs / 3600).toFixed(1);
  const activeDays  = days.filter(d => dailyTotals[d] > 0).length;
  const avgMins     = activeDays ? Math.round(totalSecs / activeDays / 60) : 0;
  const topDomain   = Object.entries(domainTotals).sort((a, b) => b[1] - a[1])[0];

  const rangeLabel  = `${fmtDate(days[0])} – ${fmtDate(days[days.length - 1])}`;
  const generated   = new Date().toLocaleDateString('en-US', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' });

  const domainRows = Object.entries(domainTotals)
    .filter(([, s]) => s > 0)
    .sort((a, b) => b[1] - a[1])
    .map(([domain, secs]) => {
      const pct = totalSecs ? Math.round(secs / totalSecs * 100) : 0;
      const warn = secs >= 3600 * days.length * 0.5 ? ' style="color:#c0392b;font-weight:700"' : '';
      return `<tr>
        <td${warn}>${domain}</td>
        <td>${fmt(secs)}</td>
        <td>
          <div style="background:#eee;border-radius:3px;height:10px;width:120px;display:inline-block;vertical-align:middle">
            <div style="background:${secs >= 4500 ? '#c0392b' : secs >= 3600 ? '#e67e22' : '#3498db'};width:${pct}%;height:10px;border-radius:3px"></div>
          </div>
          <span style="font-size:11px;color:#888;margin-left:6px">${pct}%</span>
        </td>
      </tr>`;
    }).join('');

  const dayRows = days.map(date => {
    const secs = dailyTotals[date] || 0;
    const perSite = timeLimitSites
      .filter(d => ((timeData[d] || {})[date] || 0) > 0)
      .map(d => `${d}: ${fmt((timeData[d] || {})[date] || 0)}`)
      .join(' &nbsp;·&nbsp; ');
    return `<tr>
      <td>${fmtDate(date)}</td>
      <td>${secs > 0 ? fmt(secs) : '<span style="color:#bbb">—</span>'}</td>
      <td style="font-size:11px;color:#888">${perSite || '—'}</td>
    </tr>`;
  }).join('');

  const html = `<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<title>WinResMonitor Report — ${rangeLabel}</title>
<style>
  * { box-sizing: border-box; margin: 0; padding: 0; }
  body { font-family: Segoe UI, Arial, sans-serif; background: #f4f6f9; color: #333; padding: 32px; }
  h1 { color: #2C3E50; font-size: 22px; margin-bottom: 4px; }
  .sub { color: #888; font-size: 13px; margin-bottom: 28px; }
  .cards { display: flex; gap: 16px; margin-bottom: 28px; flex-wrap: wrap; }
  .card { background: white; border-radius: 8px; padding: 18px 24px; flex: 1; min-width: 140px;
          box-shadow: 0 1px 4px rgba(0,0,0,0.08); }
  .card .val { font-size: 28px; font-weight: 700; color: #2C3E50; }
  .card .lbl { font-size: 12px; color: #999; margin-top: 4px; }
  h2 { font-size: 15px; color: #2C3E50; margin-bottom: 12px; padding-bottom: 6px;
       border-bottom: 2px solid #eee; }
  section { background: white; border-radius: 8px; padding: 20px 24px;
            box-shadow: 0 1px 4px rgba(0,0,0,0.08); margin-bottom: 20px; }
  table { width: 100%; border-collapse: collapse; font-size: 13px; }
  th { text-align: left; color: #888; font-weight: 600; font-size: 11px;
       text-transform: uppercase; letter-spacing: 0.5px; padding: 0 0 8px; }
  td { padding: 7px 0; border-bottom: 1px solid #f0f0f0; vertical-align: middle; }
  tr:last-child td { border-bottom: none; }
  .footer { text-align: center; color: #bbb; font-size: 11px; margin-top: 24px; }
</style>
</head>
<body>
  <h1>🛡️ WinResMonitor — Screen Time Report</h1>
  <div class="sub">${rangeLabel} &nbsp;·&nbsp; Generated ${generated}</div>

  <div class="cards">
    <div class="card"><div class="val">${totalHours}h</div><div class="lbl">Total time on tracked sites</div></div>
    <div class="card"><div class="val">${activeDays}</div><div class="lbl">Active days</div></div>
    <div class="card"><div class="val">${avgMins}m</div><div class="lbl">Avg per active day</div></div>
    <div class="card"><div class="val">${topDomain ? topDomain[0].split('.')[0] : '—'}</div><div class="lbl">Most visited site</div></div>
  </div>

  <section>
    <h2>Time Per Site</h2>
    ${domainRows ? `<table><thead><tr><th>Site</th><th>Total Time</th><th>Share</th></tr></thead><tbody>${domainRows}</tbody></table>`
                 : '<p style="color:#bbb;font-size:13px">No activity recorded in this period.</p>'}
  </section>

  <section>
    <h2>Daily Breakdown</h2>
    <table>
      <thead><tr><th>Day</th><th>Total</th><th>Detail</th></tr></thead>
      <tbody>${dayRows}</tbody>
    </table>
  </section>

  <div class="footer">WinResMonitor Browser Extension &nbsp;·&nbsp; Time only counted while tab is active and window is focused</div>
</body>
</html>`;

  return html;
}

async function downloadReport(numDays) {
  const statusEl = document.getElementById('report-status');
  statusEl.textContent = 'Generating…';
  try {
    const days = lastNDays(numDays);
    const html = await generateReport(days);
    const blob = new Blob([html], { type: 'text/html' });
    const url  = URL.createObjectURL(blob);
    const a    = document.createElement('a');
    const label = numDays === 7 ? '7day' : '30day';
    a.href     = url;
    a.download = `WinResMonitor_Report_${label}_${dateKey(new Date())}.html`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
    statusEl.textContent = 'Report downloaded!';
    setTimeout(() => { statusEl.textContent = ''; }, 3000);
  } catch (e) {
    statusEl.textContent = 'Error generating report.';
  }
}

document.getElementById('report-week-btn').addEventListener('click',  () => downloadReport(7));
document.getElementById('report-month-btn').addEventListener('click', () => downloadReport(30));

// ─── Init ─────────────────────────────────────────────────────────────────────
loadTodayStats();
loadLimits();
loadWhitelist();
