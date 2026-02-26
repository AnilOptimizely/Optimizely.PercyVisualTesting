/* percy-plugin.js — Percy Visual Testing admin UI logic */
(function () {
  'use strict';

  const BUILDS_API = '/api/percy/builds';
  const TRIGGER_API = '/percy-admin/trigger';
  const POLL_INTERVAL_MS = 30 * 1000;

  let pollTimer = null;

  // ── Helpers ──────────────────────────────────────────────
  function el(id) { return document.getElementById(id); }

  function showResult(element, message, isSuccess) {
    element.textContent = message;
    element.className = 'percy-trigger-result ' + (isSuccess ? 'percy-trigger-result--success' : 'percy-trigger-result--error');
    element.hidden = false;
  }

  function badgeClass(state) {
    const map = { finished: 'percy-badge--success', failed: 'percy-badge--danger', pending: 'percy-badge--warning', processing: 'percy-badge--warning' };
    return map[state] || 'percy-badge--neutral';
  }

  function formatDate(iso) {
    try {
      return new Date(iso).toISOString().replace('T', ' ').slice(0, 16) + ' UTC';
    } catch { return iso; }
  }

  function buildCardHtml(build) {
    const stateLabel = (build.state || 'unknown').charAt(0).toUpperCase() + (build.state || '').slice(1);
    const unreviewedBadge = build.unreviewedComparisons > 0
      ? `<span class="percy-badge percy-badge--warning percy-badge--sm">${build.unreviewedComparisons} unreviewed</span>`
      : '';
    const labelBadge = build.triggerLabel ? `<span>🏷️ ${escapeHtml(build.triggerLabel)}</span>` : '';
    const link = build.buildUrl
      ? `<a href="${escapeHtml(build.buildUrl)}" class="percy-build-card__link" target="_blank" rel="noopener noreferrer">View build on Percy →</a>`
      : '';

    return `
      <div class="percy-build-card">
        <div class="percy-build-card__row">
          <span class="percy-badge ${badgeClass(build.state)}">${escapeHtml(stateLabel)}</span>
          ${unreviewedBadge}
          <span class="percy-build-card__date">${formatDate(build.createdAt)}</span>
        </div>
        <div class="percy-build-card__meta">
          <span>📸 ${build.totalSnapshots} snapshots</span>
          <span>🔍 ${build.totalComparisons} comparisons</span>
          ${labelBadge}
        </div>
        ${link}
      </div>`;
  }

  function escapeHtml(str) {
    return String(str)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;');
  }

  // ── Load builds ───────────────────────────────────────────
  function loadBuilds() {
    const list = el('percy-builds-list');
    if (!list) return;

    fetch(`${BUILDS_API}?count=10`, { credentials: 'same-origin' })
      .then(r => {
        if (!r.ok) throw new Error(`HTTP ${r.status}`);
        return r.json();
      })
      .then(builds => {
        if (!Array.isArray(builds) || builds.length === 0) {
          list.innerHTML = '<p class="percy-empty">No recent builds found.</p>';
          return;
        }
        list.innerHTML = builds.map(buildCardHtml).join('');
      })
      .catch(err => {
        console.warn('[Percy] Failed to load builds:', err);
        list.innerHTML = '<p class="percy-empty percy-empty--error">⚠️ Could not load builds.</p>';
      });
  }

  // ── Trigger snapshot ─────────────────────────────────────
  function triggerSnapshot() {
    const urlsEl = el('percy-urls');
    const labelEl = el('percy-label');
    const btn = el('percy-trigger-btn');
    const resultEl = el('percy-trigger-result');

    if (!urlsEl || !resultEl) return;

    const rawUrls = (urlsEl.value || '').trim();
    if (!rawUrls) {
      showResult(resultEl, '⚠️ Please enter at least one URL.', false);
      return;
    }

    const urls = rawUrls.split('\n').map(u => u.trim()).filter(Boolean);
    const label = (labelEl && labelEl.value.trim()) || null;

    btn.disabled = true;
    btn.textContent = '⏳ Triggering…';
    resultEl.hidden = true;

    // Get CSRF token from meta tag if present
    const csrfMeta = document.querySelector('meta[name="RequestVerificationToken"]');
    const headers = { 'Content-Type': 'application/json' };
    if (csrfMeta) {
      const csrfToken = csrfMeta.getAttribute('content');
      if (csrfToken) headers['RequestVerificationToken'] = csrfToken;
    }

    fetch(TRIGGER_API, {
      method: 'POST',
      credentials: 'same-origin',
      headers,
      body: JSON.stringify({ urls, label })
    })
      .then(r => r.json().then(data => ({ ok: r.ok, data })))
      .then(({ ok, data }) => {
        if (ok && data.success) {
          showResult(resultEl, `✅ Snapshot triggered for ${data.snapshotCount} URL(s).`, true);
          setTimeout(loadBuilds, 3000);
        } else {
          showResult(resultEl, `❌ ${data.message || data.error || 'Unknown error'}`, false);
        }
      })
      .catch(err => {
        showResult(resultEl, `❌ Request failed: ${err.message}`, false);
      })
      .finally(() => {
        btn.disabled = false;
        btn.textContent = 'Trigger Snapshot';
      });
  }

  // ── Poll ──────────────────────────────────────────────────
  function startPolling() {
    if (pollTimer) clearInterval(pollTimer);
    pollTimer = setInterval(loadBuilds, POLL_INTERVAL_MS);
  }

  // ── Init ──────────────────────────────────────────────────
  document.addEventListener('DOMContentLoaded', function () {
    loadBuilds();
    startPolling();

    const triggerBtn = el('percy-trigger-btn');
    if (triggerBtn) triggerBtn.addEventListener('click', triggerSnapshot);

    const refreshBtn = el('percy-refresh-btn');
    if (refreshBtn) refreshBtn.addEventListener('click', loadBuilds);
  });
}());
