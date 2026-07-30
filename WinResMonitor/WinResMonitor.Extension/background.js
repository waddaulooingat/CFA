// ─── Config API sync ──────────────────────────────────────────────────────────
const CONFIG_API      = 'http://localhost:8878/config';
const SYNC_ALARM      = 'wrm-sync';
const SYNC_MINUTES    = 5;

async function syncFromApp() {
  try {
    const res  = await fetch(CONFIG_API, { signal: AbortSignal.timeout(3000) });
    if (!res.ok) return;
    const cfg  = await res.json();
    const patch = {};
    if (Array.isArray(cfg.timeLimitSites) && cfg.timeLimitSites.length)
      patch.timeLimitSites = cfg.timeLimitSites.map(d => d.toLowerCase());
    if (Array.isArray(cfg.whitelist) && cfg.whitelist.length)
      patch.whitelist = cfg.whitelist.map(d => d.toLowerCase());
    if (Object.keys(patch).length) await chrome.storage.local.set(patch);
  } catch { /* app not running — keep existing settings */ }
}

// ─── Constants ────────────────────────────────────────────────────────────────
const TICK_SECONDS    = 30;
const WARN_SECONDS    = 60 * 60;       // 1 hour
const BLOCK_SECONDS   = 60 * 75;       // 1h 15m
const BLOCK_HOURS     = 24;
const TICK_ALARM      = 'wrm-tick';

const MESSAGES = [
  "Bruh!! What are you doing??",
  "Do you think this is helping in any way, shape, or form?",
  "Bro... seriously? Still here?",
  "Your future self is judging you right now.",
  "This is definitely not on your to-do list.",
  "Time is a non-renewable resource. Think about it.",
  "Your goals called. They miss you.",
  "Netflix and procrastinate? Really?",
  "Is this really how you want to spend your day?",
  "Somewhere, a version of you who studied is doing great.",
  "Every minute here is a minute stolen from your future.",
  "You've been here for over an hour. Just saying.",
];

const DEFAULT_TIME_LIMIT_SITES = [
  "youtube.com", "discord.com", "tiktok.com", "netflix.com",
  "twitch.tv", "reddit.com", "instagram.com", "twitter.com",
  "x.com", "facebook.com", "snapchat.com", "pinterest.com",
  "tumblr.com", "9gag.com", "imgur.com", "roblox.com",
];

const DEFAULT_WHITELIST = [
  "khanacademy.org", "coursera.org", "edx.org", "udemy.com", "udacity.com",
  "duolingo.com", "quizlet.com", "wolframalpha.com", "wikipedia.org",
  "britannica.com", "ixl.com", "mathway.com", "desmos.com", "geogebra.org",
  "grammarly.com", "merriam-webster.com", "dictionary.com",
  "github.com", "stackoverflow.com", "w3schools.com",
  "docs.microsoft.com", "learn.microsoft.com",
  "google.com", "docs.google.com", "classroom.google.com",
  "scholar.google.com", "ted.com", "archive.org", "pbs.org", "nasa.gov",
];

// ─── State ────────────────────────────────────────────────────────────────────
let activeDomain  = null;   // domain currently in the focused active tab
let windowFocused = true;   // whether Chrome/Edge window has OS focus

// ─── Startup ──────────────────────────────────────────────────────────────────
chrome.runtime.onInstalled.addListener(async () => {
  const data = await chrome.storage.local.get(['timeLimitSites', 'whitelist']);
  if (!data.timeLimitSites) await chrome.storage.local.set({ timeLimitSites: DEFAULT_TIME_LIMIT_SITES });
  if (!data.whitelist)      await chrome.storage.local.set({ whitelist: DEFAULT_WHITELIST });
  chrome.alarms.create(TICK_ALARM, { periodInMinutes: TICK_SECONDS / 60 });
  chrome.alarms.create(SYNC_ALARM, { periodInMinutes: SYNC_MINUTES });
  syncFromApp();
});

chrome.runtime.onStartup.addListener(() => {
  chrome.alarms.create(TICK_ALARM, { periodInMinutes: TICK_SECONDS / 60 });
  chrome.alarms.create(SYNC_ALARM, { periodInMinutes: SYNC_MINUTES });
  syncFromApp();
});

// ─── Track active tab domain ──────────────────────────────────────────────────
chrome.tabs.onActivated.addListener(async ({ tabId }) => {
  try {
    const tab = await chrome.tabs.get(tabId);
    activeDomain = extractDomain(tab.url);
  } catch { activeDomain = null; }
});

chrome.tabs.onUpdated.addListener(async (tabId, change, tab) => {
  if (change.status !== 'loading' || !tab.active) return;

  const domain = extractDomain(tab.url);
  activeDomain = domain;

  // Intercept navigation to blocked domains
  if (domain && await isTempBlocked(domain)) {
    const remaining = await getBlockRemainingMinutes(domain);
    const blockedUrl = chrome.runtime.getURL(
      `blocked.html?domain=${encodeURIComponent(domain)}&minutes=${remaining}`
    );
    chrome.tabs.update(tabId, { url: blockedUrl });
  }
});

chrome.windows.onFocusChanged.addListener(windowId => {
  windowFocused = windowId !== chrome.windows.WINDOW_ID_NONE;
});

// ─── Tick ─────────────────────────────────────────────────────────────────────
chrome.alarms.onAlarm.addListener(async alarm => {
  if (alarm.name === SYNC_ALARM) { syncFromApp(); return; }
  if (alarm.name !== TICK_ALARM) return;
  if (!windowFocused || !activeDomain) return;

  const data       = await chrome.storage.local.get(['timeLimitSites', 'whitelist', 'timeData', 'tempBlocked']);
  const limitSites = data.timeLimitSites || DEFAULT_TIME_LIMIT_SITES;
  const whitelist  = data.whitelist      || DEFAULT_WHITELIST;
  const timeData   = data.timeData       || {};
  const tempBlocked = data.tempBlocked   || {};

  // Only track time-limit sites, skip whitelisted
  if (!isTimeLimitSite(activeDomain, limitSites)) return;
  if (isWhitelisted(activeDomain, whitelist))      return;

  const today = todayKey();
  if (!timeData[activeDomain])        timeData[activeDomain] = {};
  if (!timeData[activeDomain][today]) timeData[activeDomain][today] = 0;

  timeData[activeDomain][today] += TICK_SECONDS;
  const totalSeconds = timeData[activeDomain][today];

  await chrome.storage.local.set({ timeData });

  // Clean up expired temp blocks
  const now = Date.now();
  for (const [d, expiry] of Object.entries(tempBlocked)) {
    if (expiry < now) delete tempBlocked[d];
  }
  await chrome.storage.local.set({ tempBlocked });

  // Threshold actions
  if (totalSeconds >= BLOCK_SECONDS && !tempBlocked[activeDomain]) {
    tempBlocked[activeDomain] = now + BLOCK_HOURS * 60 * 60 * 1000;
    await chrome.storage.local.set({ tempBlocked });

    // Redirect all tabs on this domain to the block page
    const tabs = await chrome.tabs.query({});
    const remaining = BLOCK_HOURS * 60;
    for (const tab of tabs) {
      if (extractDomain(tab.url) === activeDomain) {
        const blockedUrl = chrome.runtime.getURL(
          `blocked.html?domain=${encodeURIComponent(activeDomain)}&minutes=${remaining}`
        );
        chrome.tabs.update(tab.id, { url: blockedUrl });
      }
    }
    return;
  }

  if (totalSeconds >= WARN_SECONDS) {
    const msg = MESSAGES[Math.floor(totalSeconds / 300) % MESSAGES.length];
    const minutesSpent = Math.floor(totalSeconds / 60);

    // Send banner message to active tab
    const tabs = await chrome.tabs.query({ active: true, currentWindow: true });
    if (tabs[0]) {
      chrome.tabs.sendMessage(tabs[0].id, {
        type: 'WRM_WARN',
        message: msg,
        minutes: minutesSpent,
      }).catch(() => {});
    }
  }
});

// ─── Message handler (from popup) ─────────────────────────────────────────────
chrome.runtime.onMessage.addListener((msg, _sender, reply) => {
  if (msg.type === 'GET_TODAY_STATS') {
    getTodayStats().then(reply);
    return true;
  }
  if (msg.type === 'GET_SETTINGS') {
    chrome.storage.local.get(['timeLimitSites', 'whitelist', 'tempBlocked']).then(reply);
    return true;
  }
  if (msg.type === 'ADD_TIME_LIMIT_SITE') {
    addToList('timeLimitSites', msg.domain, DEFAULT_TIME_LIMIT_SITES).then(reply);
    return true;
  }
  if (msg.type === 'REMOVE_TIME_LIMIT_SITE') {
    removeFromList('timeLimitSites', msg.domain).then(reply);
    return true;
  }
  if (msg.type === 'ADD_WHITELIST') {
    addToList('whitelist', msg.domain, DEFAULT_WHITELIST).then(reply);
    return true;
  }
  if (msg.type === 'REMOVE_WHITELIST') {
    removeFromList('whitelist', msg.domain).then(reply);
    return true;
  }
  if (msg.type === 'UNBLOCK_DOMAIN') {
    unblockDomain(msg.domain).then(reply);
    return true;
  }
  if (msg.type === 'GET_REPORT_DATA') {
    chrome.storage.local.get(['timeData', 'timeLimitSites']).then(data => {
      reply({
        timeData:      data.timeData      || {},
        timeLimitSites: data.timeLimitSites || DEFAULT_TIME_LIMIT_SITES,
      });
    });
    return true;
  }
});

// ─── Helpers ──────────────────────────────────────────────────────────────────
function extractDomain(url) {
  try {
    const host = new URL(url).hostname;
    return host.startsWith('www.') ? host.slice(4) : host;
  } catch { return null; }
}

function todayKey() {
  return new Date().toISOString().slice(0, 10);
}

function isTimeLimitSite(domain, limitSites) {
  if (!domain) return false;
  return limitSites.some(s => domain === s || domain.endsWith('.' + s));
}

function isWhitelisted(domain, whitelist) {
  if (!domain) return false;
  return whitelist.some(s => domain === s || domain.endsWith('.' + s));
}

async function isTempBlocked(domain) {
  const data = await chrome.storage.local.get('tempBlocked');
  const tempBlocked = data.tempBlocked || {};
  const expiry = tempBlocked[domain];
  return expiry && expiry > Date.now();
}

async function getBlockRemainingMinutes(domain) {
  const data = await chrome.storage.local.get('tempBlocked');
  const expiry = (data.tempBlocked || {})[domain] || 0;
  return Math.max(0, Math.ceil((expiry - Date.now()) / 60000));
}

async function getTodayStats() {
  const data = await chrome.storage.local.get(['timeData', 'tempBlocked', 'timeLimitSites']);
  const timeData    = data.timeData    || {};
  const tempBlocked = data.tempBlocked || {};
  const limitSites  = data.timeLimitSites || DEFAULT_TIME_LIMIT_SITES;
  const today = todayKey();
  const now   = Date.now();

  const stats = limitSites.map(domain => ({
    domain,
    seconds: (timeData[domain] || {})[today] || 0,
    blocked: !!(tempBlocked[domain] && tempBlocked[domain] > now),
    remainingMinutes: tempBlocked[domain]
      ? Math.max(0, Math.ceil((tempBlocked[domain] - now) / 60000))
      : null,
  })).sort((a, b) => b.seconds - a.seconds);

  return stats;
}

async function addToList(key, domain, defaults) {
  const data = await chrome.storage.local.get(key);
  const list = data[key] || defaults;
  const clean = domain.trim().toLowerCase();
  if (!list.includes(clean)) {
    list.push(clean);
    await chrome.storage.local.set({ [key]: list });
  }
  return { ok: true };
}

async function removeFromList(key, domain) {
  const data = await chrome.storage.local.get(key);
  const list = (data[key] || []).filter(d => d !== domain.trim().toLowerCase());
  await chrome.storage.local.set({ [key]: list });
  return { ok: true };
}

async function unblockDomain(domain) {
  const data = await chrome.storage.local.get('tempBlocked');
  const tempBlocked = data.tempBlocked || {};
  delete tempBlocked[domain];
  await chrome.storage.local.set({ tempBlocked });
  return { ok: true };
}
