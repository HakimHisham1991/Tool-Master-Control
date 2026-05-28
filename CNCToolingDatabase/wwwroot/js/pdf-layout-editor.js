(function () {
    'use strict';

    var CM_TO_PX = 37.795275591;
    var PAGE_SIZES = { A4: { w: 29.7, h: 21 } };

    var state = {
        layout: null,
        selectedId: null,
        zoom: 0.85,
        drag: null,
        resize: null
    };

    var canvas, elementList, propertiesPanel, layoutNameInput, isDefaultInput;

    function init() {
        if (!window.PDF_LAYOUT_EDITOR) return;

        canvas = document.getElementById('layoutCanvas');
        elementList = document.getElementById('elementList');
        propertiesPanel = document.getElementById('propertiesPanel');
        layoutNameInput = document.getElementById('layoutName');
        isDefaultInput = document.getElementById('isDefault');

        state.layout = normalizeLayout(window.PDF_LAYOUT_EDITOR.initialLayout);

        bindEvents();
        syncPageSetupFromLayout();
        renderCanvas();
        renderElementList();
    }

    function normalizeLayout(raw) {
        var layout = raw || {};
        layout.pageSetup = layout.pageSetup || { format: 'A4', orientation: 'Landscape', marginCm: 1.5 };
        layout.styles = layout.styles || { fontName: 'Arial', headerFill: '#CCFFFF', borderWidthPt: 0.5 };
        layout.elements = (layout.elements || []).map(function (el, i) {
            el.id = el.id || uid();
            el.zIndex = el.zIndex != null ? el.zIndex : i;
            el.visible = el.visible !== false;
            return el;
        });
        return layout;
    }

    function uid() {
        return 'el_' + Math.random().toString(36).slice(2, 10);
    }

    function getContentSize() {
        var ps = state.layout.pageSetup;
        var page = PAGE_SIZES[ps.format] || PAGE_SIZES.A4;
        var pw = ps.orientation === 'Portrait' ? page.h : page.w;
        var ph = ps.orientation === 'Portrait' ? page.w : page.h;
        return {
            pageW: pw,
            pageH: ph,
            contentW: pw - 2 * ps.marginCm,
            contentH: ph - 2 * ps.marginCm,
            margin: ps.marginCm
        };
    }

    function bindEvents() {
        document.querySelectorAll('[data-add]').forEach(function (btn) {
            btn.addEventListener('click', function () { addElement(btn.getAttribute('data-add')); });
        });

        document.getElementById('btnSave').addEventListener('click', saveLayout);
        document.getElementById('btnPreview').addEventListener('click', previewLayout);
        document.getElementById('closePreview').addEventListener('click', closePreview);

        var dupBtn = document.getElementById('btnDuplicate');
        if (dupBtn) dupBtn.addEventListener('click', duplicateLayout);

        document.getElementById('pageOrientation').addEventListener('change', onPageSetupChange);
        document.getElementById('pageMargin').addEventListener('change', onPageSetupChange);
        document.getElementById('styleFont').addEventListener('change', onStyleChange);
        document.getElementById('styleHeaderFill').addEventListener('change', onStyleChange);
        document.getElementById('styleBorder').addEventListener('change', onStyleChange);

        document.getElementById('zoomIn').addEventListener('click', function () { setZoom(state.zoom + 0.1); });
        document.getElementById('zoomOut').addEventListener('click', function () { setZoom(state.zoom - 0.1); });

        window.addEventListener('resize', function () {
            if (document.getElementById('previewModal').classList.contains('active')) {
                sizePreviewToCanvas();
            }
        });

        canvas.addEventListener('mousedown', onCanvasMouseDown);
        document.addEventListener('mousemove', onMouseMove);
        document.addEventListener('mouseup', onMouseUp);
    }

    function syncPageSetupFromLayout() {
        document.getElementById('pageOrientation').value = state.layout.pageSetup.orientation || 'Landscape';
        document.getElementById('pageMargin').value = state.layout.pageSetup.marginCm || 1.5;
        document.getElementById('styleFont').value = state.layout.styles.fontName || 'Arial';
        document.getElementById('styleHeaderFill').value = state.layout.styles.headerFill || '#CCFFFF';
        document.getElementById('styleBorder').value = state.layout.styles.borderWidthPt || 0.5;
    }

    function onPageSetupChange() {
        state.layout.pageSetup.orientation = document.getElementById('pageOrientation').value;
        state.layout.pageSetup.marginCm = parseFloat(document.getElementById('pageMargin').value) || 1.5;
        renderCanvas();
    }

    function onStyleChange() {
        state.layout.styles.fontName = document.getElementById('styleFont').value;
        state.layout.styles.headerFill = document.getElementById('styleHeaderFill').value;
        state.layout.styles.borderWidthPt = parseFloat(document.getElementById('styleBorder').value) || 0.5;
        renderCanvas();
    }

    function setZoom(z) {
        state.zoom = Math.max(0.4, Math.min(1.5, z));
        document.getElementById('zoomLabel').textContent = Math.round(state.zoom * 100) + '%';
        document.getElementById('canvasWrapper').style.transform = 'scale(' + state.zoom + ')';
    }

    function renderCanvas() {
        var size = getContentSize();
        var wPx = size.contentW * CM_TO_PX;
        var hPx = size.contentH * CM_TO_PX;

        canvas.style.width = wPx + 'px';
        canvas.style.height = hPx + 'px';
        canvas.innerHTML = '';

        document.getElementById('canvasInfo').textContent =
            'A4 ' + state.layout.pageSetup.orientation + ' — content ' + size.contentW.toFixed(1) + ' × ' + size.contentH.toFixed(1) + ' cm';

        var elements = state.layout.elements.filter(function (e) { return e.visible !== false; });
        elements.sort(function (a, b) { return (a.zIndex || 0) - (b.zIndex || 0); });

        elements.forEach(function (el) {
            canvas.appendChild(createElementNode(el, size));
        });

        setZoom(state.zoom);
    }

    function createElementNode(el, size) {
        var node = document.createElement('div');
        node.className = 'ple-element type-' + el.type + (el.id === state.selectedId ? ' selected' : '');
        node.dataset.id = el.id;
        node.style.left = (el.x * CM_TO_PX) + 'px';
        node.style.top = (el.y * CM_TO_PX) + 'px';
        node.style.width = (el.width * CM_TO_PX) + 'px';
        node.style.height = (el.height * CM_TO_PX) + 'px';
        node.style.zIndex = el.zIndex || 0;

        if (el.backgroundColor) node.style.backgroundColor = el.backgroundColor;

        var label = document.createElement('div');
        label.className = 'ple-element-label';
        label.textContent = elementLabel(el);
        node.appendChild(label);

        var inner = document.createElement('div');
        inner.className = 'ple-element-inner';
        inner.style.fontSize = (el.fontSize || 8) + 'px';
        inner.style.fontWeight = el.bold ? 'bold' : 'normal';
        inner.style.fontStyle = el.italic ? 'italic' : 'normal';
        inner.style.textAlign = el.align || 'left';
        if (el.color) inner.style.color = el.color;
        inner.textContent = getElementPreviewText(el);
        node.appendChild(inner);

        if (el.id === state.selectedId) {
            ['se', 'e', 's'].forEach(function (h) {
                var handle = document.createElement('div');
                handle.className = 'ple-resize-handle ple-resize-' + h;
                handle.dataset.handle = h;
                node.appendChild(handle);
            });
        }

        return node;
    }

    function elementLabel(el) {
        var kind = el.tableKind ? ' (' + el.tableKind + ')' : '';
        return (el.type + kind).toUpperCase();
    }

    function getElementPreviewText(el) {
        if (el.type === 'image') return '[Image: ' + (el.imageSource || 'none') + ']';
        if (el.type === 'table') {
            var cols = (el.columns || []).filter(function (c) { return c.visible !== false; });
            if (cols.length) return 'Table: ' + cols.map(function (c) { return c.header || c.dataField; }).slice(0, 4).join(', ') + (cols.length > 4 ? '…' : '');
            return 'Table (' + (el.tableKind || 'generic') + ')';
        }
        if (el.text) return el.text;
        if (el.dataBinding) return '{' + el.dataBinding + '}';
        return el.type;
    }

    function renderElementList() {
        elementList.innerHTML = '';
        var sorted = state.layout.elements.slice().sort(function (a, b) { return a.y - b.y || a.x - b.x; });
        sorted.forEach(function (el) {
            var li = document.createElement('li');
            li.className = el.id === state.selectedId ? 'selected' : '';
            li.innerHTML = '<span>' + elementLabel(el) + '</span><span class="ple-el-type">' + el.y.toFixed(1) + 'cm</span>';
            li.addEventListener('click', function () { selectElement(el.id); });
            elementList.appendChild(li);
        });
    }

    function selectElement(id) {
        state.selectedId = id;
        renderCanvas();
        renderElementList();
        renderProperties();
    }

    function getSelected() {
        return state.layout.elements.find(function (e) { return e.id === state.selectedId; });
    }

    function renderProperties() {
        var el = getSelected();
        if (!el) {
            propertiesPanel.innerHTML = '<p class="ple-hint">Select an element to edit its properties.</p>';
            return;
        }

        var html = '';
        html += propInput('Type', 'type', el.type, 'select', ['text', 'image', 'box', 'cell', 'table']);
        html += propInput('Layout Mode', 'layoutMode', el.layoutMode || 'absolute', 'select', ['absolute', 'flow']);
        html += '<div class="ple-prop-grid">';
        html += propInput('X (cm)', 'x', el.x, 'number');
        html += propInput('Y (cm)', 'y', el.y, 'number');
        html += propInput('Width (cm)', 'width', el.width, 'number');
        html += propInput('Height (cm)', 'height', el.height, 'number');
        html += '</div>';
        html += propInput('Z-Index', 'zIndex', el.zIndex || 0, 'number');

        if (el.type === 'text' || el.type === 'cell') {
            html += propInput('Text', 'text', el.text || '', 'text');
            html += propInput('Data Binding', 'dataBinding', el.dataBinding || '', 'text');
            html += '<div class="ple-prop-grid">';
            html += propInput('Font Size', 'fontSize', el.fontSize || 8, 'number');
            html += propInput('Align', 'align', el.align || 'left', 'select', ['left', 'center', 'right']);
            html += '</div>';
            html += propCheckbox('Bold', 'bold', el.bold);
            html += propCheckbox('Italic', 'italic', el.italic);
            html += propInput('Text Color', 'color', el.color || '#000000', 'color');
        }

        if (el.type === 'box' || el.type === 'cell') {
            html += propInput('Background', 'backgroundColor', el.backgroundColor || '#ffffff', 'color');
            html += propCheckbox('Show Border', 'showBorder', el.showBorder);
        }

        if (el.type === 'image') {
            html += propInput('Image Source', 'imageSource', el.imageSource || '', 'select', ['logo', 'partImage', 'toolSpecs']);
            html += propCheckbox('Lock Aspect Ratio', 'lockAspectRatio', el.lockAspectRatio !== false);
        }

        if (el.type === 'table') {
            html += propInput('Table Kind', 'tableKind', el.tableKind || '', 'select', ['', 'info', 'specs', 'imageRow', 'tool', 'stamps']);
            html += propInput('Data Source', 'dataSource', el.dataSource || '', 'text');
            html += propCheckbox('Show Border', 'showBorder', el.showBorder !== false);
            html += renderColumnsEditor(el);
        }

        html += '<button type="button" class="ple-delete-btn" id="deleteElement">Delete Element</button>';
        propertiesPanel.innerHTML = html;

        propertiesPanel.querySelectorAll('[data-prop]').forEach(function (input) {
            input.addEventListener('change', onPropertyChange);
            input.addEventListener('input', onPropertyChange);
        });

        document.getElementById('deleteElement').addEventListener('click', deleteSelected);
    }

    function propInput(label, key, value, type, options) {
        var html = '<div class="ple-prop-row"><label>' + label + '</label>';
        if (type === 'select') {
            html += '<select data-prop="' + key + '">';
            (options || []).forEach(function (opt) {
                html += '<option value="' + opt + '"' + (value === opt ? ' selected' : '') + '>' + (opt || '(none)') + '</option>';
            });
            html += '</select>';
        } else {
            html += '<input type="' + type + '" data-prop="' + key + '" value="' + (value != null ? value : '') + '" step="any" />';
        }
        html += '</div>';
        return html;
    }

    function propCheckbox(label, key, checked) {
        return '<div class="ple-prop-row"><label><input type="checkbox" data-prop="' + key + '" ' + (checked ? 'checked' : '') + ' /> ' + label + '</label></div>';
    }

    function renderColumnsEditor(el) {
        if (!el.columns) el.columns = [];
        var html = '<div class="ple-columns-editor"><strong>Columns</strong>';
        el.columns.forEach(function (col, idx) {
            html += '<div class="ple-col-item" data-col-idx="' + idx + '">';
            html += '<input data-col="' + idx + '" data-col-field="header" value="' + esc(col.header || '') + '" placeholder="Header" />';
            html += '<input data-col="' + idx + '" data-col-field="dataField" value="' + esc(col.dataField || '') + '" placeholder="Data field" style="margin-top:2px" />';
            html += '<div style="display:flex;gap:4px;margin-top:2px">';
            html += '<input data-col="' + idx + '" data-col-field="width" type="number" value="' + col.width + '" style="width:50%" step="any" />';
            html += '<select data-col="' + idx + '" data-col-field="widthUnit" style="width:50%">';
            html += '<option value="cm"' + (col.widthUnit === 'cm' ? ' selected' : '') + '>cm</option>';
            html += '<option value="pt"' + (col.widthUnit === 'pt' ? ' selected' : '') + '>pt</option>';
            html += '</select></div></div>';
        });
        html += '<button type="button" id="addColumn" style="width:100%;margin-top:4px">+ Add Column</button></div>';
        setTimeout(function () {
            propertiesPanel.querySelectorAll('[data-col-field]').forEach(function (inp) {
                inp.addEventListener('change', onColumnChange);
            });
            var addCol = document.getElementById('addColumn');
            if (addCol) addCol.addEventListener('click', addColumn);
        }, 0);
        return html;
    }

    function esc(s) {
        return String(s).replace(/"/g, '&quot;');
    }

    function onPropertyChange(e) {
        var el = getSelected();
        if (!el) return;
        var key = e.target.getAttribute('data-prop');
        if (e.target.type === 'checkbox') el[key] = e.target.checked;
        else if (e.target.type === 'number') el[key] = parseFloat(e.target.value) || 0;
        else el[key] = e.target.value;
        renderCanvas();
        renderElementList();
    }

    function onColumnChange(e) {
        var el = getSelected();
        if (!el || !el.columns) return;
        var idx = parseInt(e.target.getAttribute('data-col'), 10);
        var field = e.target.getAttribute('data-col-field');
        if (field === 'width') el.columns[idx][field] = parseFloat(e.target.value) || 0;
        else el.columns[idx][field] = e.target.value;
        renderCanvas();
    }

    function addColumn() {
        var el = getSelected();
        if (!el) return;
        if (!el.columns) el.columns = [];
        el.columns.push({
            id: uid(), header: 'Column', dataField: '', width: 2, widthUnit: 'cm', visible: true,
            headerFontSize: 6, dataFontSize: 6, headerBold: true, headerAlign: 'center', dataAlign: 'center'
        });
        renderProperties();
        renderCanvas();
    }

    function addElement(type) {
        var size = getContentSize();
        var el = {
            id: uid(),
            type: type,
            x: 1,
            y: 1,
            width: type === 'table' ? size.contentW : 5,
            height: type === 'table' ? 3 : 1,
            zIndex: state.layout.elements.length,
            visible: true,
            layoutMode: type === 'table' ? 'flow' : 'absolute',
            fontSize: 8,
            align: 'left'
        };

        if (type === 'text') { el.text = 'New Text'; el.fontSize = 10; }
        if (type === 'image') { el.imageSource = 'logo'; el.height = 1.5; el.width = 2.5; }
        if (type === 'box') { el.showBorder = true; el.backgroundColor = '#f0f0f0'; }
        if (type === 'cell') { el.text = 'Cell'; el.showBorder = true; }
        if (type === 'table') {
            el.tableKind = 'generic';
            el.showBorder = true;
            el.columns = [
                { id: uid(), header: 'Col 1', dataField: '', width: 3, widthUnit: 'cm', visible: true, headerFontSize: 6, dataFontSize: 6, headerBold: true, headerAlign: 'center', dataAlign: 'center' },
                { id: uid(), header: 'Col 2', dataField: '', width: 3, widthUnit: 'cm', visible: true, headerFontSize: 6, dataFontSize: 6, headerBold: true, headerAlign: 'center', dataAlign: 'center' }
            ];
        }

        state.layout.elements.push(el);
        selectElement(el.id);
    }

    function deleteSelected() {
        if (!state.selectedId) return;
        if (!confirm('Delete this element?')) return;
        state.layout.elements = state.layout.elements.filter(function (e) { return e.id !== state.selectedId; });
        state.selectedId = null;
        renderCanvas();
        renderElementList();
        renderProperties();
    }

    function onCanvasMouseDown(e) {
        var target = e.target.closest('.ple-element');
        var handle = e.target.closest('.ple-resize-handle');

        if (handle && target) {
            var el = state.layout.elements.find(function (x) { return x.id === target.dataset.id; });
            if (!el) return;
            state.resize = {
                id: el.id,
                handle: handle.dataset.handle,
                startX: e.clientX,
                startY: e.clientY,
                orig: { x: el.x, y: el.y, width: el.width, height: el.height }
            };
            e.preventDefault();
            return;
        }

        if (target) {
            selectElement(target.dataset.id);
            var el = getSelected();
            state.drag = {
                id: el.id,
                startX: e.clientX,
                startY: e.clientY,
                origX: el.x,
                origY: el.y
            };
            e.preventDefault();
            return;
        }

        state.selectedId = null;
        renderCanvas();
        renderElementList();
        renderProperties();
    }

    function onMouseMove(e) {
        var scale = state.zoom;

        if (state.drag) {
            var el = state.layout.elements.find(function (x) { return x.id === state.drag.id; });
            if (!el) return;
            var dx = (e.clientX - state.drag.startX) / (CM_TO_PX * scale);
            var dy = (e.clientY - state.drag.startY) / (CM_TO_PX * scale);
            el.x = Math.max(0, state.drag.origX + dx);
            el.y = Math.max(0, state.drag.origY + dy);
            updateElementNode(el);
            updateElementListLabel(el);
        }

        if (state.resize) {
            var el = state.layout.elements.find(function (x) { return x.id === state.resize.id; });
            if (!el) return;
            var dx = (e.clientX - state.resize.startX) / (CM_TO_PX * scale);
            var dy = (e.clientY - state.resize.startY) / (CM_TO_PX * scale);
            var o = state.resize.orig;
            if (state.resize.handle.indexOf('e') >= 0) el.width = Math.max(0.5, o.width + dx);
            if (state.resize.handle.indexOf('s') >= 0) el.height = Math.max(0.3, o.height + dy);
            updateElementNode(el);
        }
    }

    function updateElementNode(el) {
        var node = canvas.querySelector('[data-id="' + el.id + '"]');
        if (!node) { renderCanvas(); return; }
        node.style.left = (el.x * CM_TO_PX) + 'px';
        node.style.top = (el.y * CM_TO_PX) + 'px';
        node.style.width = (el.width * CM_TO_PX) + 'px';
        node.style.height = (el.height * CM_TO_PX) + 'px';
    }

    function updateElementListLabel(el) {
        var items = elementList.querySelectorAll('li');
        items.forEach(function (li) {
            if (li.classList.contains('selected')) {
                var typeSpan = li.querySelector('.ple-el-type');
                if (typeSpan) typeSpan.textContent = el.y.toFixed(1) + 'cm';
            }
        });
        var xInput = propertiesPanel.querySelector('[data-prop="x"]');
        var yInput = propertiesPanel.querySelector('[data-prop="y"]');
        if (xInput) xInput.value = el.x;
        if (yInput) yInput.value = el.y;
    }

    function onMouseUp() {
        state.drag = null;
        state.resize = null;
    }

    function getLayoutPayload() {
        state.layout.version = 3;
        return JSON.stringify(state.layout);
    }

    function sizePreviewToCanvas() {
        var canvasEl = document.getElementById('layoutCanvas');
        var modal = document.getElementById('previewModal');
        if (!canvasEl || !modal) return;

        var aspect = canvasEl.offsetWidth / canvasEl.offsetHeight;
        var headerH = 48;
        var maxBodyW = window.innerWidth * 0.96;
        var maxBodyH = window.innerHeight * 0.92 - headerH;

        var bodyW = maxBodyW;
        var bodyH = bodyW / aspect;
        if (bodyH > maxBodyH) {
            bodyH = maxBodyH;
            bodyW = bodyH * aspect;
        }

        modal.style.setProperty('--ple-preview-width', Math.ceil(bodyW) + 'px');
        modal.style.setProperty('--ple-preview-height', Math.ceil(bodyH + headerH) + 'px');
    }

    function saveLayout() {
        var name = layoutNameInput.value.trim();
        if (!name) { alert('Please enter a layout name.'); return; }

        var layoutJson = getLayoutPayload();
        var isDefault = isDefaultInput.checked;
        var layoutId = window.PDF_LAYOUT_EDITOR.layoutId;

        var url = layoutId ? '/Settings/UpdatePdfLayout' : '/Settings/CreatePdfLayout';
        var body = layoutId
            ? 'id=' + layoutId + '&name=' + encodeURIComponent(name) + '&layoutJson=' + encodeURIComponent(layoutJson) + '&isDefault=' + isDefault
            : 'name=' + encodeURIComponent(name) + '&layoutJson=' + encodeURIComponent(layoutJson);

        fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: body
        })
        .then(function (r) { return r.json(); })
        .then(function (data) {
            if (data.success) {
                alert(data.message || 'Saved');
                if (!layoutId && data.id) {
                    window.location.href = '/Settings/PdfLayoutEditor?id=' + data.id;
                } else if (isDefault) {
                    isDefaultInput.checked = true;
                }
            } else {
                alert('Error: ' + (data.message || 'Save failed'));
            }
        })
        .catch(function (err) { alert('Error: ' + err.message); });
    }

    function previewLayout() {
        fetch('/Settings/PdfLayoutPreviewDraft', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: getLayoutPayload()
        })
        .then(function (r) { return r.blob(); })
        .then(function (blob) {
            var url = URL.createObjectURL(blob);
            sizePreviewToCanvas();
            document.getElementById('previewFrame').src = url;
            document.getElementById('previewModal').classList.add('active');
        })
        .catch(function (err) { alert('Preview failed: ' + err.message); });
    }

    function closePreview() {
        document.getElementById('previewModal').classList.remove('active');
        var frame = document.getElementById('previewFrame');
        if (frame.src && frame.src.startsWith('blob:')) {
            URL.revokeObjectURL(frame.src);
        }
        frame.src = '';
    }

    function duplicateLayout() {
        var layoutId = window.PDF_LAYOUT_EDITOR.layoutId;
        if (!layoutId) return;
        fetch('/Settings/DuplicatePdfLayout', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: 'id=' + layoutId
        })
        .then(function (r) { return r.json(); })
        .then(function (data) {
            if (data.success) window.location.href = '/Settings/PdfLayoutEditor?id=' + data.id;
            else alert('Error: ' + data.message);
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
