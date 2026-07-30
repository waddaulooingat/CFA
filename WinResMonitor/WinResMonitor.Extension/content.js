// Listens for warning messages from the background service worker
// and injects/updates a dismissable banner into the page.

const BANNER_ID = 'wrm-warning-banner';

chrome.runtime.onMessage.addListener((msg) => {
  if (msg.type === 'WRM_WARN') {
    showBanner(msg.message, msg.minutes);
  }
});

function showBanner(message, minutesSpent) {
  let banner = document.getElementById(BANNER_ID);

  if (!banner) {
    banner = document.createElement('div');
    banner.id = BANNER_ID;
    banner.style.cssText = `
      position: fixed;
      top: 0; left: 0; right: 0;
      z-index: 2147483647;
      background: #c0392b;
      color: white;
      font-family: Arial, sans-serif;
      font-size: 15px;
      padding: 12px 20px;
      display: flex;
      align-items: center;
      justify-content: space-between;
      box-shadow: 0 2px 10px rgba(0,0,0,0.4);
      animation: wrm-slide-in 0.3s ease;
    `;

    // Inject keyframe animation
    if (!document.getElementById('wrm-style')) {
      const style = document.createElement('style');
      style.id = 'wrm-style';
      style.textContent = `
        @keyframes wrm-slide-in {
          from { transform: translateY(-100%); }
          to   { transform: translateY(0); }
        }
      `;
      document.head.appendChild(style);
    }

    document.body.prepend(banner);
  }

  banner.innerHTML = `
    <span>
      ⚠️ <strong>${escapeHtml(message)}</strong>
      &nbsp;&nbsp;<span style="opacity:0.8;font-size:13px">${minutesSpent} min spent here today</span>
    </span>
    <button onclick="document.getElementById('${BANNER_ID}').remove()"
      style="background:transparent;border:2px solid white;color:white;
             padding:4px 14px;border-radius:4px;cursor:pointer;
             font-size:13px;margin-left:16px;white-space:nowrap">
      Dismiss
    </button>
  `;
}

function escapeHtml(str) {
  return str
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}
