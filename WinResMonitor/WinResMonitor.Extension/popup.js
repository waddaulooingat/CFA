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

// ─── Time limit sites ─────────────────────────────────────────────────────────
async function loadLimits() {
  const data = await sendMsg({ type: 'GET_SETTINGS' });
  const el   = document.getElementById('limit-list');
  const list = data.timeLimitSites || [];

  el.innerHTML = list.length
    ? list.map(d => `
        <div class="list-item">
          <span>${d}</span>
          <button class="remove-btn" data-key="limit" data-domain="${d}">✕</button>
        </div>`).join('')
    : '<div class="empty">No sites configured.</div>';

  el.querySelectorAll('.remove-btn').forEach(btn => {
    btn.addEventListener('click', async () => {
      await sendMsg({ type: 'REMOVE_TIME_LIMIT_SITE', domain: btn.dataset.domain });
      loadLimits();
    });
  });
}

document.getElementById('limit-add-btn').addEventListener('click', async () => {
  const input = document.getElementById('limit-input');
  const domain = input.value.trim().toLowerCase();
  if (!domain) return;
  await sendMsg({ type: 'ADD_TIME_LIMIT_SITE', domain });
  input.value = '';
  loadLimits();
});

// ─── Whitelist ────────────────────────────────────────────────────────────────
async function loadWhitelist() {
  const data = await sendMsg({ type: 'GET_SETTINGS' });
  const el   = document.getElementById('white-list');
  const list = data.whitelist || [];

  el.innerHTML = list.length
    ? list.map(d => `
        <div class="list-item">
          <span>${d}</span>
          <button class="remove-btn" data-key="white" data-domain="${d}">✕</button>
        </div>`).join('')
    : '<div class="empty">No sites whitelisted.</div>';

  el.querySelectorAll('.remove-btn').forEach(btn => {
    btn.addEventListener('click', async () => {
      await sendMsg({ type: 'REMOVE_WHITELIST', domain: btn.dataset.domain });
      loadWhitelist();
    });
  });
}

document.getElementById('white-add-btn').addEventListener('click', async () => {
  const input = document.getElementById('white-input');
  const domain = input.value.trim().toLowerCase();
  if (!domain) return;
  await sendMsg({ type: 'ADD_WHITELIST', domain });
  input.value = '';
  loadWhitelist();
});

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

// ─── Init ─────────────────────────────────────────────────────────────────────
loadTodayStats();
loadLimits();
loadWhitelist();
