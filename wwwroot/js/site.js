/* Legacy client bundle (French UI + Gemini key in localStorage).
 * NOT referenced by Views/Shared/_Layout.cshtml. The MVC app uses Razor views,
 * Assessment "AI explain" + server Gemini:ApiKey instead. Safe to ignore or delete
 * if you are not hosting a standalone index.html against /api/checklist/*. */
'use strict';

// ============================================================
// STATE
// ============================================================
let allRequirements = [];   // {id, area, category, level, cwe, nist, verificationRequirement, isNotApplicable, valid, sourceCodeReference, comment, toolUsed, updatedAt}
let allCategories  = [];    // [{name, total, conforme, nonConforme, enCours, areas:[{name, total, conforme, requirements:[]}]}]
let activeLevel    = null;  // null | 1 | 2 | 3
let activeCategory = null;  // null | string (category name e.g. "Authentication")
let activeArea     = null;  // null | string (area name e.g. "Password Security")
let searchText     = '';
let catSearchText  = '';
let expandedRow    = null;
let chartInstance  = null;

// sessionStorage: sidebar category open/close state
function getSbState() {
    try { return JSON.parse(sessionStorage.getItem('asvs_sb') || '{}'); } catch { return {}; }
}
function setSbState(s) {
    try { sessionStorage.setItem('asvs_sb', JSON.stringify(s)); } catch {}
}

// ============================================================
// API CALLS
// ============================================================
async function apiFetch(url, opts = {}) {
    const r = await fetch(url, { headers: { 'Content-Type': 'application/json' }, ...opts });
    if (!r.ok) throw new Error(`HTTP ${r.status}`);
    return r.json();
}

async function loadRequirements() {
    const params = new URLSearchParams();
    if (activeLevel)    params.set('level', activeLevel);
    if (activeCategory) params.set('category', activeCategory);
    if (searchText)     params.set('search', searchText);

    const url = '/api/checklist/requirements' + (params.toString() ? '?' + params : '');
    allRequirements = await apiFetch(url);
}

async function loadCategories() {
    allCategories = await apiFetch('/api/checklist/categories');
}

async function loadStats() {
    return apiFetch('/api/checklist/stats');
}

async function saveProgress(id, data) {
    return apiFetch(`/api/checklist/progress/${encodeURIComponent(id)}`, {
        method: 'POST',
        body: JSON.stringify(data)
    });
}

async function resetAllProgress() {
    return apiFetch('/api/checklist/progress', { method: 'DELETE' });
}

// ============================================================
// RENDERING UTILITIES
// ============================================================
function escapeHtml(s) {
    return (s || '').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
}

function formatDate(ts) {
    if (!ts) return '—';
    return new Date(ts).toLocaleDateString('fr-FR', { day:'2-digit', month:'2-digit', year:'2-digit' });
}

function getLevelClass(lvl) {
    if (lvl === 1) return 'l1';
    if (lvl === 2) return 'l2';
    if (lvl === 3) return 'l3';
    return 'lx';
}

function getLevelLabel(lvl) {
    if (lvl === 1) return 'Niv. 1';
    if (lvl === 2) return 'Niv. 2';
    if (lvl === 3) return 'Niv. 3';
    return lvl ? 'Niv. ' + lvl : '—';
}

function getValidClass(val) {
    if (val === 'Conforme')     return 'v-ok';
    if (val === 'Non conforme') return 'v-ko';
    if (val === 'En cours')     return 'v-wip';
    if (val === 'Not Applicable') return 'v-na';
    return '';
}

function getStatusIcon(val) {
    if (val === 'Conforme')     return { icon: '✓', cls: 'ri-ok' };
    if (val === 'Non conforme') return { icon: '✗', cls: 'ri-ko' };
    if (val === 'En cours')     return { icon: '⟳', cls: 'ri-wip' };
    if (val === 'Not Applicable') return { icon: '–', cls: 'ri-na' };
    return { icon: '○', cls: 'ri-empty' };
}

// ============================================================
// RENDER HEADER STATS CHIPS
// ============================================================
function renderHeaderStats(stats) {
    const set = (id, v) => { const el = document.getElementById(id); if (el) el.textContent = v; };
    set('stat-total',     stats.total);
    set('stat-validated', stats.conforme);
    set('stat-remaining', stats.total - stats.conforme);
    set('stat-pct',       stats.pct + '%');
}

// ============================================================
// RENDER STATS DASHBOARD CARDS
// ============================================================
function renderDashboard(stats) {
    const cards = [
        { id: 'dash-total',    val: stats.total,                              label: 'Total',           color: 'cyan'   },
        { id: 'dash-conf',     val: stats.conforme,                           label: 'Conformes',       color: 'green'  },
        { id: 'dash-nconf',    val: stats.nonConforme,                        label: 'Non conformes',   color: 'red'    },
        { id: 'dash-cours',    val: stats.enCours,                            label: 'En cours',        color: 'orange' },
        { id: 'dash-na',       val: stats.na,                                 label: 'N/A',             color: ''       },
        { id: 'dash-pct',      val: stats.pct + '%',                          label: 'Progression',     color: 'green'  },
    ];
    cards.forEach(c => {
        const el = document.getElementById(c.id);
        if (el) el.textContent = c.val;
    });

    // Progress bar
    const fill = document.getElementById('prog-fill');
    const pctEl = document.getElementById('prog-pct');
    const lblEl = document.getElementById('prog-lbl');
    if (fill) fill.style.width = stats.pct + '%';
    if (pctEl) pctEl.innerHTML = stats.pct + '<span>%</span>';
    if (lblEl) lblEl.textContent = `${stats.conforme} validé${stats.conforme !== 1 ? 's' : ''} sur ${stats.total} exigences applicables`;
}

// ============================================================
// RENDER CHART
// ============================================================
function renderChart(stats) {
    const canvas = document.getElementById('progress-chart');
    if (!canvas || !window.Chart) return;

    const cats = stats.byCategory || [];
    const labels = cats.map(c => c.name);
    const conformeData = cats.map(c => c.conforme);
    const totalData = cats.map(c => c.total - c.conforme);

    if (chartInstance) chartInstance.destroy();

    chartInstance = new Chart(canvas, {
        type: 'bar',
        data: {
            labels,
            datasets: [
                {
                    label: 'Conforme',
                    data: conformeData,
                    backgroundColor: 'rgba(59,130,246,0.9)',
                    borderColor: 'rgba(59,130,246,1)',
                    borderWidth: 0,
                    borderRadius: 6,
                    barThickness: 14,
                },
                {
                    label: 'Restant',
                    data: totalData,
                    backgroundColor: 'rgba(249,115,22,0.85)',
                    borderColor: 'rgba(249,115,22,1)',
                    borderWidth: 0,
                    borderRadius: 6,
                    barThickness: 14,
                }
            ]
        },
        options: {
            indexAxis: 'y',
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'top',
                    align: 'end',
                    labels: {
                        color: '#475569',
                        font: { size: 12, family: 'Inter', weight: '500' },
                        boxWidth: 8, boxHeight: 8, usePointStyle: true, pointStyle: 'circle',
                        padding: 16,
                    }
                },
                tooltip: {
                    backgroundColor: '#0f172a',
                    titleColor: '#fff',
                    bodyColor: '#e2e8f0',
                    borderColor: 'transparent',
                    padding: 10,
                    cornerRadius: 8,
                    callbacks: {
                        label: ctx => {
                            const cat = cats[ctx.dataIndex];
                            if (!cat) return ctx.formattedValue;
                            if (ctx.datasetIndex === 0) return ` ${cat.conforme} / ${cat.total} conforme(s)`;
                            return ` ${cat.total - cat.conforme} restant(s)`;
                        }
                    }
                }
            },
            scales: {
                x: {
                    stacked: true,
                    ticks: { color: '#94a3b8', font: { size: 11, family: 'Inter' } },
                    grid: { color: '#f1f5f9', drawBorder: false }
                },
                y: {
                    stacked: true,
                    ticks: { color: '#475569', font: { size: 11, family: 'Inter', weight: '500' } },
                    grid: { display: false, drawBorder: false }
                }
            }
        }
    });
}

// ============================================================
// RENDER SIDEBAR
// ============================================================
function renderSidebar() {
    const sbState = getSbState();
    const q = catSearchText.toLowerCase();
    const container = document.getElementById('cat-list');
    if (!container) return;

    let cats = allCategories;
    if (q) {
        cats = cats.filter(c =>
            c.name.toLowerCase().includes(q) ||
            c.areas.some(a => a.name.toLowerCase().includes(q))
        );
    }

    container.innerHTML = cats.map(cat => {
        const isActive = activeCategory === cat.name;
        const isDone = cat.total > 0 && cat.conforme === cat.total;
        const isExp = sbState[cat.name] === true || (!!q);
        const dotColor = isDone ? 'var(--green)' : cat.conforme > 0 ? 'var(--cyan)' : 'var(--txt3)';

        let areasHtml = '';
        if (isExp) {
            const filteredAreas = q
                ? cat.areas.filter(a => a.name.toLowerCase().includes(q) || a.requirements.some(r => r.id.includes(q)))
                : cat.areas;

            areasHtml = filteredAreas.map(area => {
                const areaDone = area.total > 0 && area.conforme === area.total;
                const areaActive = activeArea === area.name;
                const areaDotColor = areaDone ? 'var(--green)' : area.conforme > 0 ? 'var(--cyan)' : 'var(--txt3)';

                const reqsHtml = area.requirements
                    .filter(r => !activeLevel || r.level === activeLevel)
                    .map(r => {
                        const si = getStatusIcon(r.isNotApplicable ? 'Not Applicable' : r.valid);
                        return `<div class="req-item" onclick="openExplainModal('${escapeHtml(r.id)}')" style="cursor:pointer" title="Cliquer pour l'explication IA">
                            <span class="req-item-id">${escapeHtml(r.id)}</span>
                            <span class="req-item-desc">${escapeHtml(r.verificationRequirement.slice(0,60))}${r.verificationRequirement.length > 60 ? '…' : ''}</span>
                            <span class="req-item-icon ${si.cls}">${si.icon}</span>
                        </div>`;
                    }).join('');

                return `<div class="area-block">
                    <div class="area-header ${areaActive ? 'active' : ''}" onclick="setArea('${escapeHtml(cat.name)}', '${escapeHtml(area.name)}')">
                        <span class="cat-dot" style="background:${areaDotColor}"></span>
                        <span class="cat-name" style="font-size:11px">${escapeHtml(area.name)}</span>
                        <span class="cat-badge ${areaDone ? 'done' : ''}">${area.conforme}/${area.total}</span>
                    </div>
                    <div class="cat-reqs">${reqsHtml}</div>
                </div>`;
            }).join('');
        }

        return `<div class="cat-block">
            <div class="cat-item ${isActive ? 'active' : ''}" onclick="toggleCat('${escapeHtml(cat.name)}')">
                <span class="cat-dot" style="background:${dotColor}"></span>
                <span class="cat-name">${escapeHtml(cat.name)}</span>
                <span class="cat-badge ${isDone ? 'done' : ''}">${cat.conforme}/${cat.total}</span>
                <span class="cat-arrow ${isExp ? 'open' : ''}">›</span>
            </div>
            ${isExp ? `<div class="cat-areas">${areasHtml}</div>` : ''}
        </div>`;
    }).join('');
}

// ============================================================
// RENDER TABLE
// ============================================================
function renderTable() {
    const tbody = document.getElementById('req-tbody');
    const countEl = document.getElementById('req-count');
    if (!tbody) return;

    const rows = allRequirements;

    if (countEl) countEl.innerHTML = `<strong>${rows.length}</strong> exigence${rows.length !== 1 ? 's' : ''}`;

    if (rows.length === 0) {
        tbody.innerHTML = `<tr><td colspan="7"><div class="empty-st"><div class="ico">&#x1F50D;</div><p>Aucune exigence trouvée pour cette sélection.</p></div></td></tr>`;
        return;
    }

    tbody.innerHTML = rows.map(r => {
        const isNA = r.isNotApplicable || r.valid === 'Not Applicable';
        const isValid = r.valid === 'Conforme';
        const rowClass = isNA ? 'row-na' : isValid ? 'row-valid' : '';
        const isExpanded = expandedRow === r.id;
        const lvlClass = getLevelClass(r.level);
        const vClass = getValidClass(isNA ? 'Not Applicable' : r.valid);
        const rowId = 'row-' + r.id.replace(/\./g, '-');
        const expandId = 'expand-' + r.id.replace(/\./g, '-');

        const mainRow = `<tr class="req-row ${rowClass}" id="${rowId}" ${!isNA ? `onclick="openExplainModal('${escapeHtml(r.id)}')" style="cursor:pointer"` : ''}>
            <td><span class="req-id">${escapeHtml(r.id)}</span></td>
            <td><span class="req-area" title="${escapeHtml(r.area)}">${escapeHtml(r.area)}</span></td>
            <td><span class="lvl-badge ${lvlClass}">${getLevelLabel(r.level)}</span></td>
            <td class="req-desc-cell">
                <div class="req-desc ${isExpanded ? '' : 'trunc'}">${escapeHtml(r.verificationRequirement)}</div>
                ${r.cwe ? `<span class="cwe-badge">CWE-${escapeHtml(r.cwe)}</span>` : ''}
            </td>
            <td onclick="event.stopPropagation()">
                ${isNA
                    ? `<span class="valid-badge v-na">N/A</span>`
                    : `<select class="valid-sel ${vClass}" onchange="setValid('${escapeHtml(r.id)}', this.value)">
                        <option value="">—</option>
                        <option value="Conforme" ${r.valid === 'Conforme' ? 'selected' : ''}>&#x2713; Conforme</option>
                        <option value="Non conforme" ${r.valid === 'Non conforme' ? 'selected' : ''}>&#x2715; Non conforme</option>
                        <option value="En cours" ${r.valid === 'En cours' ? 'selected' : ''}>&#x27F3; En cours</option>
                        <option value="Not Applicable" ${r.valid === 'Not Applicable' ? 'selected' : ''}>&#x2014; N/A</option>
                    </select>`
                }
            </td>
            <td><span class="status-dt">${formatDate(r.updatedAt)}</span></td>
            <td onclick="event.stopPropagation()">
                ${!isNA ? `<button class="btn-expand ${isExpanded ? 'expanded' : ''}" onclick="toggleRow('${escapeHtml(r.id)}')" title="Détails">&#x25BC;</button>` : ''}
            </td>
        </tr>`;

        const expandRow = isExpanded && !isNA ? `<tr class="expand-row" id="${expandId}">
            <td colspan="7">
                <div class="expand-panel">
                    <div class="expand-grid">
                        <div class="expand-field">
                            <label>Référence code source</label>
                            <input type="text" placeholder="Ex: src/auth/LoginController.cs:42"
                                value="${escapeHtml(r.sourceCodeReference || '')}"
                                onchange="setField('${escapeHtml(r.id)}', 'sourceCodeReference', this.value)" />
                        </div>
                        <div class="expand-field">
                            <label>Outil utilisé</label>
                            <input type="text" placeholder="Ex: OWASP ZAP, Burp Suite, SonarQube..."
                                value="${escapeHtml(r.toolUsed || '')}"
                                onchange="setField('${escapeHtml(r.id)}', 'toolUsed', this.value)" />
                        </div>
                        <div class="expand-field full">
                            <label>Commentaire</label>
                            <textarea placeholder="Notes, observations, recommandations..."
                                onchange="setField('${escapeHtml(r.id)}', 'comment', this.value)">${escapeHtml(r.comment || '')}</textarea>
                        </div>
                    </div>
                    ${r.nist ? `<div class="expand-meta"><span class="meta-badge">NIST: ${escapeHtml(r.nist)}</span></div>` : ''}
                </div>
            </td>
        </tr>` : '';

        return mainRow + expandRow;
    }).join('');
}

// ============================================================
// FULL RENDER
// ============================================================
async function render() {
    try {
        await loadRequirements();
        renderTable();

        const stats = await loadStats();
        renderHeaderStats(stats);
        renderDashboard(stats);
        renderChart(stats);

        await loadCategories();
        renderSidebar();
    } catch (err) {
        console.error('Render error:', err);
    }
}

async function renderStats() {
    try {
        const stats = await loadStats();
        renderHeaderStats(stats);
        renderDashboard(stats);
        renderChart(stats);
        await loadCategories();
        renderSidebar();
    } catch (err) {
        console.error('Stats render error:', err);
    }
}

// ============================================================
// ACTIONS
// ============================================================
window.setValid = async function(id, val) {
    // Optimistically update in-memory
    const req = allRequirements.find(r => r.id === id);
    if (req) {
        req.valid = val;
        req.updatedAt = val ? new Date().toISOString() : null;
    }
    renderTable();

    try {
        const existing = req || {};
        await saveProgress(id, {
            valid: val,
            sourceCodeReference: existing.sourceCodeReference || '',
            comment: existing.comment || '',
            toolUsed: existing.toolUsed || ''
        });
        await renderStats();
    } catch (err) {
        console.error('Save error:', err);
    }
};

const fieldDebounce = {};
window.setField = function(id, field, val) {
    // Optimistically update in-memory
    const req = allRequirements.find(r => r.id === id);
    if (req) {
        req[field] = val;
        req.updatedAt = new Date().toISOString();
    }

    clearTimeout(fieldDebounce[id + field]);
    fieldDebounce[id + field] = setTimeout(async () => {
        try {
            const existing = req || {};
            await saveProgress(id, {
                valid: existing.valid || '',
                sourceCodeReference: existing.sourceCodeReference || '',
                comment: existing.comment || '',
                toolUsed: existing.toolUsed || ''
            });
        } catch (err) {
            console.error('Field save error:', err);
        }
    }, 600);
};

window.toggleRow = function(id) {
    expandedRow = expandedRow === id ? null : id;
    renderTable();
};

window.setLevel = function(lvl) {
    activeLevel = activeLevel === lvl ? null : lvl;
    document.querySelectorAll('.btn-lvl').forEach(b => {
        b.className = 'btn-lvl';
        if (parseInt(b.dataset.lvl) === lvl && activeLevel === lvl) b.classList.add('al' + lvl);
    });
    expandedRow = null;
    render();
};

window.setArea = function(catName, areaName) {
    if (activeArea === areaName) {
        activeArea = null;
        activeCategory = null;
    } else {
        activeArea = areaName;
        activeCategory = catName;
    }
    expandedRow = null;
    render();
};

window.toggleCat = function(catName) {
    const sbState = getSbState();
    if (sbState[catName]) {
        delete sbState[catName];
        // Also clear area filter if this category was active
        if (activeCategory === catName) {
            activeCategory = null;
            activeArea = null;
        }
    } else {
        sbState[catName] = true;
        activeCategory = catName;
        activeArea = null;
    }
    setSbState(sbState);
    render();
};

window.openRequirement = function(id) {
    const req = allRequirements.find(r => r.id === id);
    if (req) {
        // If area is set, clear area filter to show all in this category
        activeArea = req.area;
        activeCategory = req.category;
        searchText = '';
        const si = document.getElementById('search-input');
        if (si) si.value = '';
    }

    loadRequirements().then(() => {
        renderTable();
        setTimeout(() => {
            const rowId = 'row-' + id.replace(/\./g, '-');
            const row = document.getElementById(rowId);
            if (row) {
                row.scrollIntoView({ behavior: 'smooth', block: 'center' });
                row.classList.add('row-highlight');
                setTimeout(() => row.classList.remove('row-highlight'), 2000);
            }
            expandedRow = id;
            renderTable();
        }, 80);
    });
};

window.resetFilters = function() {
    activeLevel = null;
    activeCategory = null;
    activeArea = null;
    searchText = '';
    document.querySelectorAll('.btn-lvl').forEach(b => b.className = 'btn-lvl');
    const si = document.getElementById('search-input');
    if (si) si.value = '';
    expandedRow = null;
    render();
};

window.toggleSidebar = function() {
    document.querySelector('.app-sidebar')?.classList.toggle('open');
};

window.exportCSV = function() {
    const params = new URLSearchParams();
    if (activeLevel) params.set('level', activeLevel);
    if (activeCategory) params.set('category', activeCategory);
    if (searchText) params.set('search', searchText);
    const url = '/api/checklist/export' + (params.toString() ? '?' + params : '');
    window.open(url, '_blank');
};

window.resetAllData = async function() {
    if (!confirm('Réinitialiser toutes les données de progression ? Cette action est irréversible.')) return;
    try {
        await resetAllProgress();
        expandedRow = null;
        await render();
    } catch (err) {
        alert('Erreur lors de la réinitialisation: ' + err.message);
    }
};

// ============================================================
// API KEY MANAGEMENT
// ============================================================
function getStoredApiKey() {
    try { return localStorage.getItem('gemini_api_key') || ''; } catch { return ''; }
}

window.toggleApiKeyBar = function() {
    const bar = document.getElementById('apikey-bar');
    const inp = document.getElementById('apikey-input');
    bar.classList.toggle('open');
    if (bar.classList.contains('open')) {
        inp.value = getStoredApiKey();
        inp.focus();
        inp.type = 'text';
        setTimeout(() => inp.type = 'password', 2000);
    }
};

window.saveApiKey = function() {
    const val = document.getElementById('apikey-input').value.trim();
    try { localStorage.setItem('gemini_api_key', val); } catch {}
    updateApiKeyBtn();
    document.getElementById('apikey-bar').classList.remove('open');
    if (val) showToast('Clé API enregistrée ✓', 'green');
};

window.clearApiKey = function() {
    try { localStorage.removeItem('gemini_api_key'); } catch {}
    document.getElementById('apikey-input').value = '';
    updateApiKeyBtn();
    showToast('Clé API effacée', 'red');
};

function updateApiKeyBtn() {
    const btn = document.querySelector('.btn-apikey');
    if (!btn) return;
    const hasKey = !!getStoredApiKey();
    btn.classList.toggle('key-set', hasKey);
    btn.textContent = hasKey ? '✓ Clé API configurée' : '🔑 Configurer l\u2019API Key';
}

function showToast(msg, color) {
    const colors = {
        green:  { border: '#16a34a', text: '#15803d' },
        red:    { border: '#dc2626', text: '#b91c1c' },
        purple: { border: '#8b5cf6', text: '#7c3aed' },
        orange: { border: '#f59e0b', text: '#b45309' },
    };
    const c = colors[color] || colors.green;
    const t = document.createElement('div');
    t.style.cssText = `position:fixed;bottom:24px;right:24px;z-index:9999;background:#fff;
        border:1px solid ${c.border};color:${c.text};padding:12px 18px;border-radius:12px;
        font-size:13px;font-weight:500;font-family:Inter,system-ui,sans-serif;
        box-shadow:0 10px 24px rgba(15,23,42,0.12);animation:fadeIn .2s ease`;
    t.textContent = msg;
    document.body.appendChild(t);
    setTimeout(() => t.remove(), 2500);
}

// allow Enter key in API key input
document.addEventListener('DOMContentLoaded', () => {
    const inp = document.getElementById('apikey-input');
    if (inp) inp.addEventListener('keydown', e => { if (e.key === 'Enter') window.saveApiKey(); });
    updateApiKeyBtn();
});

// ============================================================
// EXPLAIN MODAL
// ============================================================
let modalCurrentId = null;
let modalCurrentModel = 'gemini-2.0-flash';
let modalCurrentTech = '';

function getSavedTech() {
    try { return localStorage.getItem('asvs_tech') || ''; } catch { return ''; }
}
function saveTech(val) {
    try { localStorage.setItem('asvs_tech', val); } catch {}
}

window.onTechChange = function(val) {
    modalCurrentTech = val;
    saveTech(val);
};

function markdownToHtml(md) {
    if (!md) return '';
    return md
        .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
        .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
        .replace(/\*(.+?)\*/g, '<em>$1</em>')
        .replace(/`([^`]+)`/g, '<code>$1</code>')
        .replace(/^### (.+)$/gm, '<h3>$1</h3>')
        .replace(/^## (.+)$/gm, '<h2>$1</h2>')
        .replace(/^# (.+)$/gm, '<h1>$1</h1>')
        .replace(/^\d+\.\s(.+)$/gm, '<li>$1</li>')
        .replace(/^[-*]\s(.+)$/gm, '<li>$1</li>')
        .replace(/(<li>.*<\/li>(\n|$))+/g, m => '<ul>' + m + '</ul>')
        .replace(/\n\n+/g, '</p><p>')
        .replace(/^(?!<[hulo])(.+)$/gm, '$1')
        .replace(/(<p>)?(<h[123]>)/g, '$2')
        .replace(/(<\/h[123]>)(<\/p>)?/g, '$1');
}

async function openExplainModal(id) {
    const req = allRequirements.find(r => r.id === id) ||
                allCategories.flatMap(c => c.areas.flatMap(a => a.requirements)).find(r => r.id === id);
    if (!req) return;

    modalCurrentId = id;

    // Populate modal fields
    const fullReq = allRequirements.find(r => r.id === id);

    document.getElementById('modal-req-id').textContent = id;
    document.getElementById('modal-cat').textContent = fullReq?.category || '';
    document.getElementById('modal-area').textContent = fullReq?.area || '';
    document.getElementById('modal-req-text').textContent = fullReq?.verificationRequirement || req.verificationRequirement || '';

    // Restore saved tech
    modalCurrentTech = getSavedTech();
    const techSel = document.getElementById('modal-tech-sel');
    if (techSel) techSel.value = modalCurrentTech;

    // Load model list
    try {
        const data = await apiFetch('/api/checklist/gemini-models');
        const sel = document.getElementById('modal-model-sel');
        sel.innerHTML = data.models.map(m =>
            `<option value="${m.id}" ${m.id === data.current ? 'selected' : ''}>${m.label}</option>`
        ).join('');
        modalCurrentModel = data.current;
        document.getElementById('modal-current-model-lbl').textContent = data.current;
    } catch {}

    // Show modal
    document.getElementById('explain-overlay').classList.add('active');
    document.body.style.overflow = 'hidden';

    // Sync validation buttons to current status
    const currentValid = fullReq?.valid || '';
    syncValidButtons(currentValid);

    // Hide validation row for N/A items
    const validRow = document.getElementById('modal-valid-row');
    if (validRow) validRow.style.display = fullReq?.isNotApplicable ? 'none' : 'flex';

    // Reset explain box
    resetExplainBox();

    // Trigger explanation
    generateExplanation(id, modalCurrentModel);
}

function resetExplainBox() {
    const loading = document.getElementById('modal-loading');
    const content = document.getElementById('modal-explain-content');
    loading.classList.remove('hidden');
    content.classList.remove('visible');
    content.innerHTML = '';
}

async function generateExplanation(id, model) {
    const loading = document.getElementById('modal-loading');
    const content = document.getElementById('modal-explain-content');

    loading.classList.remove('hidden');
    content.classList.remove('visible');

    try {
        const data = await apiFetch(`/api/checklist/explain/${encodeURIComponent(id)}`, {
            method: 'POST',
            body: JSON.stringify({ model, apiKey: getStoredApiKey() || undefined, technology: modalCurrentTech || undefined })
        });

        loading.classList.add('hidden');
        content.innerHTML = `<p>${markdownToHtml(data.explanation)}</p>`;
        content.classList.add('visible');
    } catch (err) {
        loading.classList.add('hidden');
        const msg = err.message || '';
        let hint = '';
        if (msg.includes('429') || msg.includes('quota')) {
            hint = '<br><small>&#x1F4A1; Quota dépassé. Utilisez le bouton <strong>🔑 API Key</strong> en haut pour saisir une autre clé, ou obtenez-en une sur <strong>aistudio.google.com</strong>.</small>';
        } else if (msg.includes('403') || msg.includes('401')) {
            hint = '<br><small>&#x1F511; Clé API invalide. Vérifiez <code>Gemini:ApiKey</code> dans <code>appsettings.json</code>.</small>';
        } else if (msg.includes('ApiKey') || msg.includes('configured')) {
            hint = '<br><small>&#x2699;&#xFE0F; Ajoutez votre clé dans <code>appsettings.json</code> → <code>Gemini:ApiKey</code>.</small>';
        }
        content.innerHTML = `<div class="modal-explain-error">&#x26A0; ${escapeHtml(msg)}${hint}</div>`;
        content.classList.add('visible');
    }
}

window.onModelChange = function(val) {
    modalCurrentModel = val;
    document.getElementById('modal-current-model-lbl').textContent = val;
};

window.regenExplanation = function() {
    if (!modalCurrentId) return;
    resetExplainBox();
    generateExplanation(modalCurrentId, modalCurrentModel);
};

function syncValidButtons(val) {
    document.querySelectorAll('.modal-valid-btn').forEach(btn => {
        btn.classList.toggle('active', btn.dataset.val === val);
    });
}

window.setValidFromModal = async function(val) {
    if (!modalCurrentId) return;
    const req = allRequirements.find(r => r.id === modalCurrentId);
    if (!req) return;

    req.valid = val;
    req.updatedAt = new Date().toISOString();
    syncValidButtons(val);
    renderTable();

    try {
        await saveProgress(modalCurrentId, {
            valid: val,
            sourceCodeReference: req.sourceCodeReference || '',
            comment: req.comment || '',
            toolUsed: req.toolUsed || ''
        });
        await renderStats();
        showToast(
            val === 'Conforme' ? '✓ Marqué comme Validé' :
            val === 'Non conforme' ? '✗ Marqué comme Non validé' :
            val === 'En cours' ? '⟳ Marqué En cours' : '— Marqué Non applicable',
            val === 'Conforme' ? 'green' : val === 'Non conforme' ? 'red' : 'purple'
        );
    } catch (err) {
        console.error('Validate error:', err);
    }
};

window.closeExplainModal = function(e) {
    if (e && e.target !== document.getElementById('explain-overlay')) return;
    document.getElementById('explain-overlay').classList.remove('active');
    document.body.style.overflow = '';
    modalCurrentId = null;
};

// ============================================================
// DEBOUNCE
// ============================================================
function debounce(fn, ms) {
    let t;
    return function(...a) { clearTimeout(t); t = setTimeout(() => fn(...a), ms); };
}

// ============================================================
// INIT
// ============================================================
document.addEventListener('DOMContentLoaded', async function() {
    // Search input
    const si = document.getElementById('search-input');
    if (si) {
        si.addEventListener('input', debounce(function() {
            searchText = si.value.trim();
            expandedRow = null;
            render();
        }, 280));
    }

    // Cat search
    const cs = document.getElementById('cat-search');
    if (cs) {
        cs.addEventListener('input', debounce(function() {
            catSearchText = cs.value.trim();
            renderSidebar();
        }, 180));
    }

    // Close sidebar on outside click (mobile)
    document.addEventListener('click', function(e) {
        const sb = document.querySelector('.app-sidebar');
        const tog = document.querySelector('.sidebar-toggle');
        if (sb?.classList.contains('open') && !sb.contains(e.target) && e.target !== tog) {
            sb.classList.remove('open');
        }
    });

    // Initial load
    await render();
});
