function toggleSidebar() {
    const sidebar = document.getElementById('sidebar');
    if (sidebar) {
        sidebar.classList.toggle('collapsed');
        localStorage.setItem('sidebarCollapsed', sidebar.classList.contains('collapsed'));
    }
}

function showCopyToast() {
    var toast = document.getElementById('copy-toast');
    if (!toast) {
        toast = document.createElement('div');
        toast.id = 'copy-toast';
        toast.className = 'copy-toast';
        toast.textContent = 'Copied!';
        document.body.appendChild(toast);
    }
    toast.classList.add('show');
    clearTimeout(toast._hide);
    toast._hide = setTimeout(function() {
        toast.classList.remove('show');
    }, 1500);
}

function copyToClipboard(text, done) {
    if (!text) { if (done) done(false); return; }
    if (navigator.clipboard && navigator.clipboard.writeText) {
        navigator.clipboard.writeText(text).then(function() { if (done) done(true); }).catch(function() {
            try {
                var ta = document.createElement('textarea');
                ta.value = text;
                ta.style.position = 'fixed'; ta.style.left = '-9999px';
                document.body.appendChild(ta);
                ta.select();
                var ok = document.execCommand('copy');
                document.body.removeChild(ta);
                if (done) done(ok);
            } catch (err) { if (done) done(false); }
        });
    } else {
        try {
            var ta = document.createElement('textarea');
            ta.value = text;
            ta.style.position = 'fixed'; ta.style.left = '-9999px';
            document.body.appendChild(ta);
            ta.select();
            var ok = document.execCommand('copy');
            document.body.removeChild(ta);
            if (done) done(ok);
        } catch (err) { if (done) done(false); }
    }
}

function getSelectedOptionText(selectEl) {
    if (!selectEl || selectEl.tagName !== 'SELECT') return '';
    var opt = selectEl.options[selectEl.selectedIndex];
    return opt ? (opt.textContent || opt.text || '').trim() : '';
}

function getCopyTextFromElement(el) {
    if (!el) return '';
    if (el.tagName === 'SELECT') {
        return getSelectedOptionText(el);
    }
    if (el.classList && el.classList.contains('combo-option')) {
        return (el.textContent || '').trim();
    }
    if (el.querySelector) {
        var input = el.querySelector('input.form-control, input.cell-input');
        if (input) return (input.value || '').trim();
        var select = el.querySelector('select');
        if (select) return getSelectedOptionText(select);
    }
    return (el.textContent || '').trim();
}

function createCopyTooltipButton() {
    var btn = document.createElement('button');
    btn.type = 'button';
    btn.className = 'copy-tooltip-btn';
    btn.setAttribute('aria-label', 'Copy');
    btn.innerHTML = '<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect><path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path></svg>';
    return btn;
}

function initCopyTooltip() {
    var copyBtn = createCopyTooltipButton();
    copyBtn.style.display = 'none';
    copyBtn.style.position = 'fixed';
    document.body.appendChild(copyBtn);
    var copyShowTimer = null;
    var copySourceEl = null;
    var COPY_DELAY_MS = 250;

    function hideCopyBtn() {
        if (copyShowTimer) clearTimeout(copyShowTimer);
        copyShowTimer = null;
        copySourceEl = null;
        copyBtn.style.display = 'none';
    }

    function showCopyBtn(el) {
        copySourceEl = el;
        var rect = el.getBoundingClientRect();
        copyBtn.style.top = (rect.top + 4) + 'px';
        copyBtn.style.left = (rect.right - 32) + 'px';
        copyBtn.style.display = 'flex';
    }

    function copyableElement(target) {
        if (!target || typeof target.closest !== 'function') return null;
        var td = target.closest('td');
        if (td) {
            var table = target.closest('table');
            if (table) return td;
        }
        var th = target.closest('th');
        if (th && th.querySelector('select.filter-cell-select')) return th;
        if (target.closest('.combo-wrapper .combo-option')) return target.closest('.combo-wrapper .combo-option');
        if (target.tagName === 'SELECT') {
            if (target.classList.contains('form-control') || target.classList.contains('filter-cell-select')) return target;
        }
        if (target.closest('select.form-control')) return target.closest('select.form-control');
        if (target.closest('select.filter-cell-select')) return target.closest('select.filter-cell-select');
        return null;
    }

    function onMouseOver(e) {
        var el = copyableElement(e.target);
        if (!el) {
            if (e.target !== copyBtn && !copyBtn.contains(e.target)) hideCopyBtn();
            return;
        }
        if (copySourceEl === el) return;
        if (copyShowTimer) clearTimeout(copyShowTimer);
        copyShowTimer = setTimeout(function() {
            copyShowTimer = null;
            showCopyBtn(el);
        }, COPY_DELAY_MS);
    }

    function onMouseOut(e) {
        var related = e.relatedTarget;
        if (related && (copyBtn === related || copyBtn.contains(related))) return;
        if (related && copySourceEl && (copySourceEl === related || copySourceEl.contains(related))) return;
        if (copyShowTimer) { clearTimeout(copyShowTimer); copyShowTimer = null; }
        if (copyableElement(e.target) === copySourceEl) hideCopyBtn();
    }

    function onDblClick(e) {
        var el = copyableElement(e.target);
        if (!el) return;
        e.preventDefault();
        var text = getCopyTextFromElement(el);
        copyToClipboard(text, function(ok) { if (ok) showCopyToast(); });
    }

    document.addEventListener('mouseover', onMouseOver, true);
    document.addEventListener('mouseout', onMouseOut, true);
    document.addEventListener('dblclick', onDblClick, true);
    copyBtn.addEventListener('click', function(ev) {
        ev.preventDefault();
        ev.stopPropagation();
        var text = copySourceEl ? getCopyTextFromElement(copySourceEl) : '';
        copyToClipboard(text, function(ok) {
            if (ok) { showCopyToast(); hideCopyBtn(); }
        });
    });
}

function initApp() {
    var sidebar = document.getElementById('sidebar');
    if (sidebar) {
        var isCollapsed = localStorage.getItem('sidebarCollapsed') === 'true';
        if (isCollapsed) sidebar.classList.add('collapsed');
    }
    try {
        initCopyTooltip();
    } catch (err) {
        console.error('Copy tooltip init error', err);
    }
}

function runWhenReady() {
    if (document.body && document.querySelector) {
        initApp();
    } else {
        setTimeout(runWhenReady, 50);
    }
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', runWhenReady);
} else {
    setTimeout(runWhenReady, 0);
}
