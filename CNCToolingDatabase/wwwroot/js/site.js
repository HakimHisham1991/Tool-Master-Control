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

function getCopyTextFromElement(el) {
    if (!el) return '';
    if (el.tagName === 'SELECT') {
        var opt = el.options[el.selectedIndex];
        return opt ? (opt.textContent || opt.text || '').trim() : '';
    }
    if (el.classList && el.classList.contains('combo-option')) {
        return (el.textContent || '').trim();
    }
    if (el.querySelector) {
        var input = el.querySelector('input.form-control, input.cell-input');
        var select = el.querySelector('select.form-control, select.filter-cell-select');
        if (input) return (input.value || '').trim();
        if (select) {
            var opt = select.options[select.selectedIndex];
            return opt ? (opt.textContent || opt.text || '').trim() : '';
        }
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
    var copyShowTimer = null;
    var copySourceEl = null;
    var COPY_DELAY_MS = 500;

    function hideCopyBtn() {
        if (copyShowTimer) clearTimeout(copyShowTimer);
        copyShowTimer = null;
        copySourceEl = null;
        if (copyBtn.parentNode) {
            copyBtn.parentNode.removeChild(copyBtn);
        }
    }

    function showCopyBtn(el) {
        copySourceEl = el;
        if (copyBtn.parentNode) copyBtn.parentNode.removeChild(copyBtn);
        var container = (el && el.tagName === 'SELECT') ? el.parentNode : el;
        if (!container) return;
        if (container.style) container.style.position = 'relative';
        container.appendChild(copyBtn);
        copyBtn.style.display = 'flex';
    }

    function copyableElement(target) {
        if (!target || !target.closest) return null;
        var td = target.closest ? target.closest('td');
        var table = target.closest ? target.closest('table');
        if (td && table && table.classList && table.classList.contains('data-table')) return td;
        if (target.closest('.combo-wrapper .combo-option')) return target.closest('.combo-wrapper .combo-option');
        var th = target.closest ? target.closest('th');
        if (th && th.querySelector && th.querySelector('select.filter-cell-select')) return th;
        if (target.tagName === 'SELECT' && (target.classList.contains('form-control') || target.classList.contains('filter-cell-select'))) return target;
        if (target.closest('select.form-control')) return target.closest('select.form-control');
        if (target.closest('select.filter-cell-select')) return target.closest('select.filter-cell-select');
        return null;
    }

    document.body.addEventListener('mouseover', function(e) {
        var el = copyableElement(e.target);
        if (!el) {
            if (e.target !== copyBtn && (!copyBtn.parentNode || !copyBtn.parentNode.contains(e.target))) hideCopyBtn();
            return;
        }
        if (copySourceEl === el) return;
        if (copyShowTimer) clearTimeout(copyShowTimer);
        copyShowTimer = setTimeout(function() {
            copyShowTimer = null;
            showCopyBtn(el);
        }, COPY_DELAY_MS);
    }, true);

    document.body.addEventListener('mouseout', function(e) {
        var related = e.relatedTarget;
        if (related && copyBtn.parentNode && (copyBtn === related || copyBtn.contains(related))) return;
        if (related && copySourceEl && (copySourceEl === related || copySourceEl.contains(related))) return;
        if (copyShowTimer) { clearTimeout(copyShowTimer); copyShowTimer = null; }
        var el = copyableElement(e.target);
        if (el === copySourceEl) hideCopyBtn();
    }, true);

    copyBtn.addEventListener('click', function(ev) {
        ev.preventDefault();
        ev.stopPropagation();
        var text = copySourceEl ? getCopyTextFromElement(copySourceEl) : '';
        copyToClipboard(text, function(ok) {
            if (ok) { showCopyToast(); hideCopyBtn(); }
        });
    });

    document.body.addEventListener('dblclick', function(e) {
        var el = copyableElement(e.target);
        if (!el) return;
        var text = getCopyTextFromElement(el);
        copyToClipboard(text, function(ok) { if (ok) showCopyToast(); });
    }, true);
}

function initApp() {
    var sidebar = document.getElementById('sidebar');
    if (sidebar) {
        var isCollapsed = localStorage.getItem('sidebarCollapsed') === 'true';
        if (isCollapsed) sidebar.classList.add('collapsed');
    }
    initCopyTooltip();
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initApp);
} else {
    initApp();
}
