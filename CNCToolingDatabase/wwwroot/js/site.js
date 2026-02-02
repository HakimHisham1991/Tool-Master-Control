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

document.addEventListener('DOMContentLoaded', function() {
    var sidebar = document.getElementById('sidebar');
    if (sidebar) {
        var isCollapsed = localStorage.getItem('sidebarCollapsed') === 'true';
        if (isCollapsed) {
            sidebar.classList.add('collapsed');
        }
    }

    var copyBtn = createCopyTooltipButton();
    var copyShowTimer = null;
    var copySourceEl = null;
    var COPY_DELAY_MS = 500;

    function hideCopyBtn() {
        copyShowTimer = null;
        copySourceEl = null;
        if (copyBtn.parentNode) {
            copyBtn.style.display = 'none';
            copyBtn.parentNode.removeChild(copyBtn);
        }
    }

    function showCopyBtn(el) {
        copySourceEl = el;
        if (copyBtn.parentNode) copyBtn.parentNode.removeChild(copyBtn);
        var container = el.tagName === 'SELECT' ? el.parentNode : el;
        if (container) {
            if (container.style) container.style.position = 'relative';
            container.appendChild(copyBtn);
        }
        copyBtn.style.display = 'flex';
    }

    function copyableElement(el) {
        if (!el || !el.closest) return null;
        if (el.closest('.data-table td')) return el.closest('.data-table td');
        if (el.closest('.combo-wrapper .combo-option')) return el.closest('.combo-wrapper .combo-option');
        if (el.closest('.data-table thead tr.table-filter-row th')) return el.closest('.data-table thead tr.table-filter-row th');
        if (el.tagName === 'SELECT' && (el.classList.contains('form-control') || el.classList.contains('filter-cell-select'))) return el;
        if (el.closest('select.form-control') || el.closest('select.filter-cell-select')) return el.closest('select');
        return null;
    }

    document.addEventListener('mouseover', function(e) {
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
    });

    document.addEventListener('mouseout', function(e) {
        var el = copyableElement(e.target);
        var related = e.relatedTarget;
        if (el && related && (el === related || el.contains(related))) return;
        if (copyBtn.contains(e.target) || e.target === copyBtn) return;
        if (related && copyBtn.parentNode && copyBtn.parentNode.contains(related)) return;
        if (copyShowTimer) clearTimeout(copyShowTimer);
        copyShowTimer = null;
        if (el !== copySourceEl) return;
        hideCopyBtn();
    });

    copyBtn.addEventListener('click', function(ev) {
        ev.preventDefault();
        ev.stopPropagation();
        if (!copySourceEl || !navigator.clipboard || !navigator.clipboard.writeText) return;
        var text = getCopyTextFromElement(copySourceEl);
        if (text) {
            navigator.clipboard.writeText(text).then(function() {
                showCopyToast();
                hideCopyBtn();
            }).catch(function() {});
        }
    });

    document.addEventListener('dblclick', function(e) {
        var el = copyableElement(e.target);
        if (!el) return;
        var text = getCopyTextFromElement(el);
        if (text && navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(text).then(function() {
                showCopyToast();
            }).catch(function() {});
        }
    });
});
