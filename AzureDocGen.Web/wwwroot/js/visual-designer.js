// ビジュアルデザイナー JavaScript Interop

class VisualDesigner {
    constructor(canvasId, dotnetHelper) {
        this.canvasId = canvasId;
        this.dotnetHelper = dotnetHelper;
        this.canvas = null;
        this.resources = [];
        this.connections = [];
        this.selectedResource = null;
        this.isDragging = false;
        this.dragOffset = { x: 0, y: 0 };
        this.scale = 1;
        this.panOffset = { x: 0, y: 0 };
        this.gridSize = 20;
        this.showGrid = true;

        this.initializeCanvas();
    }

    initializeCanvas() {
        const container = document.getElementById(this.canvasId);
        if (!container) {
            console.error('Canvas container not found:', this.canvasId);
            return;
        }

        // SVGキャンバスを作成
        this.canvas = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        this.canvas.setAttribute('width', '100%');
        this.canvas.setAttribute('height', '100%');
        this.canvas.style.backgroundColor = '#f8f9fa';
        this.canvas.style.cursor = 'default';

        container.appendChild(this.canvas);

        // グリッドレイヤー
        this.gridLayer = document.createElementNS('http://www.w3.org/2000/svg', 'g');
        this.gridLayer.setAttribute('id', 'grid-layer');
        this.canvas.appendChild(this.gridLayer);

        // 接続レイヤー
        this.connectionLayer = document.createElementNS('http://www.w3.org/2000/svg', 'g');
        this.connectionLayer.setAttribute('id', 'connection-layer');
        this.canvas.appendChild(this.connectionLayer);

        // リソースレイヤー
        this.resourceLayer = document.createElementNS('http://www.w3.org/2000/svg', 'g');
        this.resourceLayer.setAttribute('id', 'resource-layer');
        this.canvas.appendChild(this.resourceLayer);

        this.drawGrid();
        this.setupEventListeners();
    }

    drawGrid() {
        if (!this.showGrid) return;

        this.gridLayer.innerHTML = '';
        const width = this.canvas.clientWidth;
        const height = this.canvas.clientHeight;

        // 垂直線
        for (let x = 0; x <= width; x += this.gridSize) {
            const line = document.createElementNS('http://www.w3.org/2000/svg', 'line');
            line.setAttribute('x1', x);
            line.setAttribute('y1', 0);
            line.setAttribute('x2', x);
            line.setAttribute('y2', height);
            line.setAttribute('stroke', '#dee2e6');
            line.setAttribute('stroke-width', '1');
            this.gridLayer.appendChild(line);
        }

        // 水平線
        for (let y = 0; y <= height; y += this.gridSize) {
            const line = document.createElementNS('http://www.w3.org/2000/svg', 'line');
            line.setAttribute('x1', 0);
            line.setAttribute('y1', y);
            line.setAttribute('x2', width);
            line.setAttribute('y2', y);
            line.setAttribute('stroke', '#dee2e6');
            line.setAttribute('stroke-width', '1');
            this.gridLayer.appendChild(line);
        }
    }

    setupEventListeners() {
        this.canvas.addEventListener('mousedown', this.onMouseDown.bind(this));
        this.canvas.addEventListener('mousemove', this.onMouseMove.bind(this));
        this.canvas.addEventListener('mouseup', this.onMouseUp.bind(this));
        this.canvas.addEventListener('mouseleave', this.onMouseUp.bind(this));
        this.canvas.addEventListener('wheel', this.onWheel.bind(this));

        // ウィンドウリサイズ時にグリッドを再描画
        window.addEventListener('resize', () => this.drawGrid());
    }

    snapToGrid(value) {
        return Math.round(value / this.gridSize) * this.gridSize;
    }

    addResource(resourceId, resourceType, name, x, y, width, height) {
        const resource = {
            id: resourceId,
            type: resourceType,
            name: name,
            x: this.snapToGrid(x),
            y: this.snapToGrid(y),
            width: width || 120,
            height: height || 80
        };

        this.resources.push(resource);
        this.drawResource(resource);
        return resource;
    }

    drawResource(resource) {
        const group = document.createElementNS('http://www.w3.org/2000/svg', 'g');
        group.setAttribute('id', `resource-${resource.id}`);
        group.setAttribute('data-resource-id', resource.id);
        group.style.cursor = 'move';

        // リソースの矩形
        const rect = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
        rect.setAttribute('x', resource.x);
        rect.setAttribute('y', resource.y);
        rect.setAttribute('width', resource.width);
        rect.setAttribute('height', resource.height);
        rect.setAttribute('fill', this.getResourceColor(resource.type));
        rect.setAttribute('stroke', '#495057');
        rect.setAttribute('stroke-width', '2');
        rect.setAttribute('rx', '5');
        rect.classList.add('resource-rect');
        group.appendChild(rect);

        // リソースアイコン（プレースホルダー）
        const icon = document.createElementNS('http://www.w3.org/2000/svg', 'text');
        icon.setAttribute('x', resource.x + resource.width / 2);
        icon.setAttribute('y', resource.y + 30);
        icon.setAttribute('text-anchor', 'middle');
        icon.setAttribute('font-size', '24');
        icon.setAttribute('fill', '#212529');
        icon.textContent = this.getResourceIcon(resource.type);
        group.appendChild(icon);

        // リソース名
        const text = document.createElementNS('http://www.w3.org/2000/svg', 'text');
        text.setAttribute('x', resource.x + resource.width / 2);
        text.setAttribute('y', resource.y + resource.height - 15);
        text.setAttribute('text-anchor', 'middle');
        text.setAttribute('font-size', '12');
        text.setAttribute('fill', '#212529');
        text.textContent = resource.name.length > 15 ? resource.name.substring(0, 12) + '...' : resource.name;
        group.appendChild(text);

        this.resourceLayer.appendChild(group);
    }

    getResourceColor(resourceType) {
        const colors = {
            'VirtualMachine': '#0078D4',
            'StorageAccount': '#7FBA00',
            'AppService': '#FF6700',
            'Database': '#E81123',
            'VirtualNetwork': '#008272',
            'LoadBalancer': '#A4373A',
            'default': '#6c757d'
        };
        return colors[resourceType] || colors['default'];
    }

    getResourceIcon(resourceType) {
        const icons = {
            'VirtualMachine': '💻',
            'StorageAccount': '📦',
            'AppService': '🌐',
            'Database': '🗄️',
            'VirtualNetwork': '🌐',
            'LoadBalancer': '⚖️',
            'default': '📋'
        };
        return icons[resourceType] || icons['default'];
    }

    onMouseDown(event) {
        const target = event.target.closest('[data-resource-id]');
        if (target) {
            const resourceId = target.getAttribute('data-resource-id');
            this.selectedResource = this.resources.find(r => r.id === resourceId);

            if (this.selectedResource) {
                this.isDragging = true;
                const rect = target.querySelector('rect');
                const x = parseFloat(rect.getAttribute('x'));
                const y = parseFloat(rect.getAttribute('y'));

                this.dragOffset = {
                    x: event.offsetX - x,
                    y: event.offsetY - y
                };

                // 選択状態を表示
                this.highlightResource(resourceId);

                // Blazorに通知
                if (this.dotnetHelper) {
                    this.dotnetHelper.invokeMethodAsync('OnResourceSelected', resourceId);
                }
            }
        }
    }

    onMouseMove(event) {
        if (this.isDragging && this.selectedResource) {
            const newX = this.snapToGrid(event.offsetX - this.dragOffset.x);
            const newY = this.snapToGrid(event.offsetY - this.dragOffset.y);

            this.updateResourcePosition(this.selectedResource.id, newX, newY);
        }
    }

    onMouseUp(event) {
        if (this.isDragging && this.selectedResource) {
            // Blazorに位置更新を通知
            if (this.dotnetHelper) {
                this.dotnetHelper.invokeMethodAsync('OnResourceMoved',
                    this.selectedResource.id,
                    this.selectedResource.x,
                    this.selectedResource.y);
            }
        }

        this.isDragging = false;
    }

    onWheel(event) {
        event.preventDefault();
        const delta = event.deltaY > 0 ? 0.9 : 1.1;
        this.scale = Math.max(0.1, Math.min(3, this.scale * delta));
        this.resourceLayer.setAttribute('transform', `scale(${this.scale})`);
        this.connectionLayer.setAttribute('transform', `scale(${this.scale})`);
    }

    updateResourcePosition(resourceId, x, y) {
        const resource = this.resources.find(r => r.id === resourceId);
        if (resource) {
            resource.x = x;
            resource.y = y;

            const group = document.getElementById(`resource-${resourceId}`);
            if (group) {
                const rect = group.querySelector('rect');
                const icon = group.querySelectorAll('text')[0];
                const text = group.querySelectorAll('text')[1];

                rect.setAttribute('x', x);
                rect.setAttribute('y', y);
                icon.setAttribute('x', x + resource.width / 2);
                icon.setAttribute('y', y + 30);
                text.setAttribute('x', x + resource.width / 2);
                text.setAttribute('y', y + resource.height - 15);

                // 接続を再描画
                this.redrawConnections();
            }
        }
    }

    removeResource(resourceId) {
        const index = this.resources.findIndex(r => r.id === resourceId);
        if (index !== -1) {
            this.resources.splice(index, 1);
            const group = document.getElementById(`resource-${resourceId}`);
            if (group) {
                group.remove();
            }

            // 関連する接続を削除
            this.connections = this.connections.filter(c =>
                c.sourceId !== resourceId && c.targetId !== resourceId
            );
            this.redrawConnections();
        }
    }

    addConnection(connectionId, sourceId, targetId, connectionType) {
        const connection = {
            id: connectionId,
            sourceId: sourceId,
            targetId: targetId,
            type: connectionType
        };

        this.connections.push(connection);
        this.drawConnection(connection);
    }

    drawConnection(connection) {
        const source = this.resources.find(r => r.id === connection.sourceId);
        const target = this.resources.find(r => r.id === connection.targetId);

        if (!source || !target) return;

        const line = document.createElementNS('http://www.w3.org/2000/svg', 'line');
        line.setAttribute('id', `connection-${connection.id}`);
        line.setAttribute('x1', source.x + source.width / 2);
        line.setAttribute('y1', source.y + source.height / 2);
        line.setAttribute('x2', target.x + target.width / 2);
        line.setAttribute('y2', target.y + target.height / 2);
        line.setAttribute('stroke', '#6c757d');
        line.setAttribute('stroke-width', '2');
        line.setAttribute('marker-end', 'url(#arrowhead)');

        this.connectionLayer.appendChild(line);
    }

    redrawConnections() {
        this.connectionLayer.innerHTML = '';

        // 矢印マーカーを追加
        const defs = document.createElementNS('http://www.w3.org/2000/svg', 'defs');
        const marker = document.createElementNS('http://www.w3.org/2000/svg', 'marker');
        marker.setAttribute('id', 'arrowhead');
        marker.setAttribute('markerWidth', '10');
        marker.setAttribute('markerHeight', '10');
        marker.setAttribute('refX', '5');
        marker.setAttribute('refY', '3');
        marker.setAttribute('orient', 'auto');

        const polygon = document.createElementNS('http://www.w3.org/2000/svg', 'polygon');
        polygon.setAttribute('points', '0 0, 10 3, 0 6');
        polygon.setAttribute('fill', '#6c757d');
        marker.appendChild(polygon);
        defs.appendChild(marker);
        this.connectionLayer.appendChild(defs);

        this.connections.forEach(conn => this.drawConnection(conn));
    }

    highlightResource(resourceId) {
        // すべてのリソースのハイライトを削除
        document.querySelectorAll('.resource-rect').forEach(rect => {
            rect.setAttribute('stroke', '#495057');
            rect.setAttribute('stroke-width', '2');
        });

        // 選択されたリソースをハイライト
        const group = document.getElementById(`resource-${resourceId}`);
        if (group) {
            const rect = group.querySelector('rect');
            rect.setAttribute('stroke', '#0d6efd');
            rect.setAttribute('stroke-width', '3');
        }
    }

    clearSelection() {
        this.selectedResource = null;
        document.querySelectorAll('.resource-rect').forEach(rect => {
            rect.setAttribute('stroke', '#495057');
            rect.setAttribute('stroke-width', '2');
        });
    }

    exportToJson() {
        return JSON.stringify({
            resources: this.resources,
            connections: this.connections
        });
    }

    loadFromJson(jsonData) {
        try {
            const data = JSON.parse(jsonData);
            this.resources = data.resources || [];
            this.connections = data.connections || [];

            this.resourceLayer.innerHTML = '';
            this.resources.forEach(resource => this.drawResource(resource));
            this.redrawConnections();
        } catch (error) {
            console.error('Error loading design:', error);
        }
    }

    clear() {
        this.resources = [];
        this.connections = [];
        this.resourceLayer.innerHTML = '';
        this.connectionLayer.innerHTML = '';
        this.selectedResource = null;
    }

    dispose() {
        if (this.canvas && this.canvas.parentNode) {
            this.canvas.parentNode.removeChild(this.canvas);
        }
        this.resources = [];
        this.connections = [];
    }
}

// グローバルデザイナーインスタンス
let designerInstance = null;

// ES6 Module exports for Blazor
export function initializeDesigner(canvasId, dotnetHelper) {
    if (designerInstance) {
        designerInstance.dispose();
    }
    designerInstance = new VisualDesigner(canvasId, dotnetHelper);
    return designerInstance;
}

export function addResource(resourceId, resourceType, name, x, y, width, height, icon) {
    if (designerInstance) {
        return designerInstance.addResource(resourceId, resourceType, name, x, y, width, height);
    }
}

export function addConnection(connectionId, sourceId, targetId, connectionType) {
    if (designerInstance) {
        designerInstance.addConnection(connectionId, sourceId, targetId, connectionType);
    }
}

export function removeResource(resourceId) {
    if (designerInstance) {
        designerInstance.removeResource(resourceId);
    }
}

export function setTool(tool) {
    if (designerInstance) {
        // ツール切り替えロジック（将来の拡張用）
        console.log('Tool changed to:', tool);
    }
}

export function zoomIn() {
    if (designerInstance) {
        designerInstance.scale = Math.min(3, designerInstance.scale * 1.2);
        designerInstance.resourceLayer.setAttribute('transform', `scale(${designerInstance.scale})`);
        designerInstance.connectionLayer.setAttribute('transform', `scale(${designerInstance.scale})`);
    }
}

export function zoomOut() {
    if (designerInstance) {
        designerInstance.scale = Math.max(0.1, designerInstance.scale * 0.8);
        designerInstance.resourceLayer.setAttribute('transform', `scale(${designerInstance.scale})`);
        designerInstance.connectionLayer.setAttribute('transform', `scale(${designerInstance.scale})`);
    }
}

export function resetZoom() {
    if (designerInstance) {
        designerInstance.scale = 1;
        designerInstance.resourceLayer.setAttribute('transform', 'scale(1)');
        designerInstance.connectionLayer.setAttribute('transform', 'scale(1)');
    }
}

export function alignLeft() {
    if (designerInstance && designerInstance.selectedResource) {
        designerInstance.updateResourcePosition(designerInstance.selectedResource.id, 50, designerInstance.selectedResource.y);
    }
}

export function alignCenter() {
    if (designerInstance && designerInstance.selectedResource && designerInstance.canvas) {
        const centerX = (designerInstance.canvas.clientWidth / 2) - (designerInstance.selectedResource.width / 2);
        designerInstance.updateResourcePosition(designerInstance.selectedResource.id, centerX, designerInstance.selectedResource.y);
    }
}

export function alignRight() {
    if (designerInstance && designerInstance.selectedResource && designerInstance.canvas) {
        const rightX = designerInstance.canvas.clientWidth - designerInstance.selectedResource.width - 50;
        designerInstance.updateResourcePosition(designerInstance.selectedResource.id, rightX, designerInstance.selectedResource.y);
    }
}

export function undo() {
    // TODO: 元に戻す機能の実装
    console.log('Undo not implemented yet');
}

export function redo() {
    // TODO: やり直す機能の実装
    console.log('Redo not implemented yet');
}

export function exportToJson() {
    if (designerInstance) {
        return designerInstance.exportToJson();
    }
    return null;
}

export function loadFromJson(jsonData) {
    if (designerInstance) {
        designerInstance.loadFromJson(jsonData);
    }
}

export function clear() {
    if (designerInstance) {
        designerInstance.clear();
    }
}

// グローバル関数として公開（後方互換性のため）
window.VisualDesigner = VisualDesigner;

window.createVisualDesigner = function (canvasId, dotnetHelper) {
    return new VisualDesigner(canvasId, dotnetHelper);
};
