// WellMonitor Dashboard JavaScript

class WellMonitorDashboard {
    constructor() {
        this.connection = null;
        this.charts = {};
        this.currentImage = null;
        this.roiCanvas = null;
        this.roiImage = null;
        this.cameraPreviewInterval = null;
        
        this.initializeSignalR();
        this.initializeEventHandlers();
        this.loadInitialData();
        this.initializeCharts();
    }

    // SignalR Connection
    async initializeSignalR() {
        try {
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/devicestatushub")
                .build();

            this.connection.on("UpdatePumpStatus", (status) => {
                this.updatePumpStatus(status);
            });

            this.connection.on("UpdateSystemStatus", (status) => {
                this.updateSystemStatus(status);
            });

            this.connection.on("UpdateOcrStatistics", (stats) => {
                this.updateOcrStatistics(stats);
            });

            this.connection.on("NewAlert", (alert) => {
                this.addAlert(alert);
            });

            this.connection.on("NewDebugImage", (imagePath) => {
                this.addDebugImage(imagePath);
            });

            await this.connection.start();
            this.updateConnectionStatus(true);
            console.log("SignalR Connected");
            
            // Join the updates group
            await this.connection.invoke("JoinGroup", "updates");
            
        } catch (err) {
            console.error("SignalR connection failed:", err);
            this.updateConnectionStatus(false);
            
            // Retry connection in 5 seconds
            setTimeout(() => this.initializeSignalR(), 5000);
        }
    }

    updateConnectionStatus(connected) {
        const statusElement = document.getElementById('connection-status');
        if (connected) {
            statusElement.className = 'badge bg-success';
            statusElement.textContent = 'Connected';
        } else {
            statusElement.className = 'badge bg-danger';
            statusElement.textContent = 'Disconnected';
        }
    }

    // Event Handlers
    initializeEventHandlers() {
        // Navigation
        document.querySelectorAll('[data-section]').forEach(link => {
            link.addEventListener('click', (e) => {
                e.preventDefault();
                this.showSection(link.dataset.section);
            });
        });

        // Debug Images
        document.getElementById('image-filter').addEventListener('change', () => {
            this.loadDebugImages();
        });

        document.getElementById('refresh-images').addEventListener('click', () => {
            this.loadDebugImages();
        });

        document.getElementById('cleanup-images').addEventListener('click', () => {
            this.cleanupImages();
        });

        // ROI Controls
        document.querySelectorAll('.roi-control').forEach(control => {
            control.addEventListener('input', () => {
                this.updateRoiDisplay();
            });
        });

        document.getElementById('save-roi').addEventListener('click', () => {
            this.saveRoi();
        });

        document.getElementById('test-roi').addEventListener('click', () => {
            this.testRoi();
        });

        document.getElementById('auto-calibrate').addEventListener('click', () => {
            this.autoCalibrateRoi();
        });

        // Camera Positioning
        document.getElementById('capture-positioning').addEventListener('click', () => {
            this.captureForPositioning();
        });

        document.getElementById('toggle-grid').addEventListener('change', (e) => {
            this.toggleGrid(e.target.checked);
        });

        document.getElementById('toggle-guides').addEventListener('change', (e) => {
            this.toggleGuides(e.target.checked);
        });

        // Manual Relay Control
        document.getElementById('manual-cycle').addEventListener('click', () => {
            this.manualRelayCycle();
        });
    }

    // Navigation
    showSection(sectionName) {
        // Hide all sections
        document.querySelectorAll('.content-section').forEach(section => {
            section.style.display = 'none';
        });

        // Update nav links
        document.querySelectorAll('.nav-link').forEach(link => {
            link.classList.remove('active');
        });

        // Show selected section
        const section = document.getElementById(`${sectionName}-section`);
        if (section) {
            section.style.display = 'block';
            
            // Update nav link
            const navLink = document.querySelector(`[data-section="${sectionName}"]`);
            if (navLink) {
                navLink.classList.add('active');
            }

            // Load section-specific data
            this.loadSectionData(sectionName);
        }
    }

    loadSectionData(sectionName) {
        switch (sectionName) {
            case 'dashboard':
                this.loadDashboardData();
                break;
            case 'debug-images':
                this.loadDebugImages();
                break;
            case 'roi-calibration':
                this.loadRoiCalibration();
                break;
            case 'camera-positioning':
                this.startCameraPreview();
                break;
        }
    }

    // Data Loading
    async loadInitialData() {
        try {
            await Promise.all([
                this.loadDeviceStatus(),
                this.loadAlerts(),
                this.loadDebugImages()
            ]);
        } catch (error) {
            console.error('Failed to load initial data:', error);
        }
    }

    async loadDashboardData() {
        try {
            await Promise.all([
                this.loadDeviceStatus(),
                this.loadAlerts(),
                this.updateCurrentChart()
            ]);
        } catch (error) {
            console.error('Failed to load dashboard data:', error);
        }
    }

    async loadDeviceStatus() {
        try {
            const response = await fetch('/api/devicestatus/status');
            const data = await response.json();
            
            this.updatePumpStatus(data.pumpStatus);
            this.updateSystemStatus(data.systemStatus);
            this.updateOcrStatistics(data.ocrStatistics);
            
        } catch (error) {
            console.error('Failed to load device status:', error);
        }
    }

    async loadAlerts() {
        try {
            const response = await fetch('/api/devicestatus/alerts');
            const alerts = await response.json();
            
            const alertsContainer = document.getElementById('alerts-container');
            alertsContainer.innerHTML = '';
            
            alerts.forEach(alert => this.addAlert(alert, false));
            
        } catch (error) {
            console.error('Failed to load alerts:', error);
        }
    }

    async loadDebugImages() {
        try {
            const filter = document.getElementById('image-filter').value;
            const url = filter === 'all' ? '/api/debugimages/recent' : `/api/debugimages/recent?filter=${filter}`;
            
            const response = await fetch(url);
            const images = await response.json();
            
            const imageList = document.getElementById('image-list');
            imageList.innerHTML = '';
            
            images.forEach(image => {
                const item = this.createImageListItem(image);
                imageList.appendChild(item);
            });
            
        } catch (error) {
            console.error('Failed to load debug images:', error);
        }
    }

    createImageListItem(image) {
        const item = document.createElement('div');
        item.className = 'image-item';
        item.dataset.imagePath = image.path;
        
        item.innerHTML = `
            <img src="/api/debugimages/image?path=${encodeURIComponent(image.path)}" 
                 alt="Debug Image" class="image-thumbnail">
            <div class="image-info">
                <div class="image-name">${image.name}</div>
                <div class="image-meta">${image.timestamp} • ${image.size}</div>
            </div>
        `;
        
        item.addEventListener('click', () => {
            this.selectImage(image.path);
        });
        
        return item;
    }

    selectImage(imagePath) {
        // Update selection
        document.querySelectorAll('.image-item').forEach(item => {
            item.classList.remove('active');
        });
        document.querySelector(`[data-image-path="${imagePath}"]`).classList.add('active');
        
        // Load image
        const imageViewer = document.getElementById('image-viewer');
        imageViewer.innerHTML = `
            <img src="/api/debugimages/image?path=${encodeURIComponent(imagePath)}" 
                 alt="Debug Image" class="img-fluid">
        `;
        
        this.currentImage = imagePath;
    }

    async cleanupImages() {
        try {
            await fetch('/api/debugimages/cleanup', { method: 'POST' });
            this.loadDebugImages();
        } catch (error) {
            console.error('Failed to cleanup images:', error);
        }
    }

    // Status Updates
    updatePumpStatus(status) {
        document.getElementById('pump-status').textContent = status.status;
        document.getElementById('pump-status').className = `status-indicator ${status.status}`;
        document.getElementById('pump-current').textContent = `${status.currentDraw} A`;
        document.getElementById('pump-power').textContent = `${status.powerConsumption} kW`;
        document.getElementById('pump-last-read').textContent = new Date(status.lastReading).toLocaleString();
    }

    updateSystemStatus(status) {
        document.getElementById('system-uptime').textContent = status.uptime;
        document.getElementById('system-cpu').textContent = `${status.cpuUsage}%`;
        document.getElementById('system-memory').textContent = `${status.memoryUsage}%`;
        document.getElementById('system-storage').textContent = `${status.storageUsage}%`;
        document.getElementById('system-temp').textContent = `${status.temperature}°C`;
    }

    updateOcrStatistics(stats) {
        document.getElementById('ocr-success-rate').textContent = `${stats.successRate}%`;
        document.getElementById('ocr-avg-confidence').textContent = `${stats.averageConfidence}%`;
        document.getElementById('ocr-total-processed').textContent = stats.totalProcessed;
        document.getElementById('ocr-provider').textContent = stats.currentProvider;
    }

    addAlert(alert, animate = true) {
        const alertsContainer = document.getElementById('alerts-container');
        
        const alertElement = document.createElement('div');
        alertElement.className = `alert alert-${alert.severity.toLowerCase()} alert-dismissible fade show`;
        if (animate) alertElement.classList.add('fade-in');
        
        alertElement.innerHTML = `
            <strong>${alert.title}</strong> ${alert.message}
            <small class="d-block text-muted">${new Date(alert.timestamp).toLocaleString()}</small>
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;
        
        alertsContainer.insertBefore(alertElement, alertsContainer.firstChild);
        
        // Auto-dismiss info alerts after 5 seconds
        if (alert.severity.toLowerCase() === 'info') {
            setTimeout(() => {
                if (alertElement.parentNode) {
                    alertElement.remove();
                }
            }, 5000);
        }
    }

    addDebugImage(imagePath) {
        // Refresh the debug images list if we're on that tab
        const debugSection = document.getElementById('debug-images-section');
        if (debugSection.style.display !== 'none') {
            this.loadDebugImages();
        }
    }

    // Charts
    initializeCharts() {
        const ctx = document.getElementById('current-chart').getContext('2d');
        
        this.charts.current = new Chart(ctx, {
            type: 'line',
            data: {
                labels: [],
                datasets: [{
                    label: 'Current Draw (A)',
                    data: [],
                    borderColor: 'rgb(75, 192, 192)',
                    backgroundColor: 'rgba(75, 192, 192, 0.2)',
                    tension: 0.1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: {
                    y: {
                        beginAtZero: true,
                        title: {
                            display: true,
                            text: 'Current (A)'
                        }
                    },
                    x: {
                        title: {
                            display: true,
                            text: 'Time'
                        }
                    }
                },
                plugins: {
                    legend: {
                        display: false
                    }
                }
            }
        });
    }

    async updateCurrentChart() {
        try {
            const response = await fetch('/api/devicestatus/history?hours=1');
            const data = await response.json();
            
            const labels = data.map(reading => new Date(reading.timestamp).toLocaleTimeString());
            const values = data.map(reading => reading.currentDraw);
            
            this.charts.current.data.labels = labels;
            this.charts.current.data.datasets[0].data = values;
            this.charts.current.update();
            
        } catch (error) {
            console.error('Failed to update current chart:', error);
        }
    }

    // ROI Calibration
    async loadRoiCalibration() {
        try {
            // Load current ROI settings
            const response = await fetch('/api/roi/current');
            const roi = await response.json();
            
            document.getElementById('roi-x').value = roi.x;
            document.getElementById('roi-y').value = roi.y;
            document.getElementById('roi-width').value = roi.width;
            document.getElementById('roi-height').value = roi.height;
            
            this.updateRoiLabels();
            
            // Load latest image for ROI preview
            await this.loadRoiPreviewImage();
            
        } catch (error) {
            console.error('Failed to load ROI calibration:', error);
        }
    }

    async loadRoiPreviewImage() {
        try {
            const response = await fetch('/api/debugimages/recent?limit=1');
            const images = await response.json();
            
            if (images.length > 0) {
                const imagePath = images[0].path;
                await this.loadImageWithRoi(imagePath);
            }
            
        } catch (error) {
            console.error('Failed to load ROI preview image:', error);
        }
    }

    async loadImageWithRoi(imagePath) {
        const roiImageContainer = document.getElementById('roi-image-container');
        
        // Create image element
        this.roiImage = document.createElement('img');
        this.roiImage.className = 'roi-canvas img-fluid';
        this.roiImage.src = `/api/debugimages/image?path=${encodeURIComponent(imagePath)}`;
        
        this.roiImage.onload = () => {
            roiImageContainer.innerHTML = '';
            roiImageContainer.appendChild(this.roiImage);
            this.updateRoiDisplay();
        };
    }

    updateRoiDisplay() {
        if (!this.roiImage) return;
        
        const x = parseFloat(document.getElementById('roi-x').value);
        const y = parseFloat(document.getElementById('roi-y').value);
        const width = parseFloat(document.getElementById('roi-width').value);
        const height = parseFloat(document.getElementById('roi-height').value);
        
        this.updateRoiLabels();
        
        // Remove existing ROI rectangle
        const existingRect = document.querySelector('.roi-rectangle');
        if (existingRect) {
            existingRect.remove();
        }
        
        // Create new ROI rectangle
        const rect = document.createElement('div');
        rect.className = 'roi-rectangle';
        
        const imageRect = this.roiImage.getBoundingClientRect();
        rect.style.left = `${x * imageRect.width / 100}px`;
        rect.style.top = `${y * imageRect.height / 100}px`;
        rect.style.width = `${width * imageRect.width / 100}px`;
        rect.style.height = `${height * imageRect.height / 100}px`;
        
        this.roiImage.parentElement.appendChild(rect);
    }

    updateRoiLabels() {
        document.getElementById('roi-x-value').textContent = document.getElementById('roi-x').value + '%';
        document.getElementById('roi-y-value').textContent = document.getElementById('roi-y').value + '%';
        document.getElementById('roi-width-value').textContent = document.getElementById('roi-width').value + '%';
        document.getElementById('roi-height-value').textContent = document.getElementById('roi-height').value + '%';
    }

    async saveRoi() {
        try {
            const roi = {
                x: parseFloat(document.getElementById('roi-x').value),
                y: parseFloat(document.getElementById('roi-y').value),
                width: parseFloat(document.getElementById('roi-width').value),
                height: parseFloat(document.getElementById('roi-height').value)
            };
            
            const response = await fetch('/api/roi/update', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(roi)
            });
            
            if (response.ok) {
                this.addAlert({
                    title: 'Success',
                    message: 'ROI settings saved successfully',
                    severity: 'Success',
                    timestamp: new Date().toISOString()
                });
            } else {
                throw new Error('Failed to save ROI');
            }
            
        } catch (error) {
            console.error('Failed to save ROI:', error);
            this.addAlert({
                title: 'Error',
                message: 'Failed to save ROI settings',
                severity: 'Danger',
                timestamp: new Date().toISOString()
            });
        }
    }

    async testRoi() {
        try {
            const response = await fetch('/api/roi/test', { method: 'POST' });
            const result = await response.json();
            
            this.addAlert({
                title: 'ROI Test Result',
                message: `OCR Result: ${result.text} (Confidence: ${result.confidence}%)`,
                severity: result.confidence > 70 ? 'Success' : 'Warning',
                timestamp: new Date().toISOString()
            });
            
        } catch (error) {
            console.error('Failed to test ROI:', error);
            this.addAlert({
                title: 'Error',
                message: 'Failed to test ROI',
                severity: 'Danger',
                timestamp: new Date().toISOString()
            });
        }
    }

    async autoCalibrateRoi() {
        try {
            document.getElementById('auto-calibrate').disabled = true;
            document.getElementById('auto-calibrate').innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Calibrating...';
            
            const response = await fetch('/api/roi/auto-calibrate', { method: 'POST' });
            const result = await response.json();
            
            if (result.success) {
                document.getElementById('roi-x').value = result.roi.x;
                document.getElementById('roi-y').value = result.roi.y;
                document.getElementById('roi-width').value = result.roi.width;
                document.getElementById('roi-height').value = result.roi.height;
                
                this.updateRoiDisplay();
                
                this.addAlert({
                    title: 'Auto-Calibration Success',
                    message: `ROI automatically calibrated with confidence: ${result.confidence}%`,
                    severity: 'Success',
                    timestamp: new Date().toISOString()
                });
            } else {
                this.addAlert({
                    title: 'Auto-Calibration Failed',
                    message: result.message || 'Unable to automatically detect text region',
                    severity: 'Warning',
                    timestamp: new Date().toISOString()
                });
            }
            
        } catch (error) {
            console.error('Failed to auto-calibrate ROI:', error);
            this.addAlert({
                title: 'Error',
                message: 'Auto-calibration failed',
                severity: 'Danger',
                timestamp: new Date().toISOString()
            });
        } finally {
            document.getElementById('auto-calibrate').disabled = false;
            document.getElementById('auto-calibrate').innerHTML = '<i class="bi bi-magic"></i> Auto-Calibrate ROI';
        }
    }

    // Camera Positioning
    startCameraPreview() {
        this.stopCameraPreview();
        
        this.cameraPreviewInterval = setInterval(() => {
            this.updateCameraPreview();
        }, 2000);
        
        this.updateCameraPreview();
    }

    stopCameraPreview() {
        if (this.cameraPreviewInterval) {
            clearInterval(this.cameraPreviewInterval);
            this.cameraPreviewInterval = null;
        }
    }

    async updateCameraPreview() {
        try {
            const response = await fetch('/api/debugimages/live-capture');
            const blob = await response.blob();
            const imageUrl = URL.createObjectURL(blob);
            
            const previewImg = document.getElementById('camera-preview-img');
            if (previewImg) {
                previewImg.src = imageUrl;
            } else {
                const cameraPreview = document.getElementById('camera-preview');
                const img = document.createElement('img');
                img.id = 'camera-preview-img';
                img.src = imageUrl;
                img.style.width = '100%';
                img.style.height = 'auto';
                cameraPreview.appendChild(img);
            }
            
        } catch (error) {
            console.error('Failed to update camera preview:', error);
        }
    }

    async captureForPositioning() {
        try {
            const response = await fetch('/api/debugimages/capture-for-positioning', { method: 'POST' });
            const result = await response.json();
            
            this.addAlert({
                title: 'Positioning Image Captured',
                message: `Image saved to ${result.imagePath}`,
                severity: 'Info',
                timestamp: new Date().toISOString()
            });
            
        } catch (error) {
            console.error('Failed to capture positioning image:', error);
        }
    }

    toggleGrid(show) {
        const gridOverlay = document.querySelector('.grid-overlay');
        if (gridOverlay) {
            gridOverlay.style.display = show ? 'block' : 'none';
        }
    }

    toggleGuides(show) {
        const guidesOverlay = document.querySelector('.positioning-guide-lines');
        if (guidesOverlay) {
            guidesOverlay.style.display = show ? 'block' : 'none';
        }
    }

    // Manual Controls
    async manualRelayCycle() {
        try {
            const response = await fetch('/api/devicestatus/manual-cycle', { method: 'POST' });
            
            if (response.ok) {
                this.addAlert({
                    title: 'Manual Relay Cycle',
                    message: 'Relay cycling initiated manually',
                    severity: 'Info',
                    timestamp: new Date().toISOString()
                });
            } else {
                throw new Error('Failed to cycle relay');
            }
            
        } catch (error) {
            console.error('Failed to cycle relay:', error);
            this.addAlert({
                title: 'Error',
                message: 'Failed to cycle relay manually',
                severity: 'Danger',
                timestamp: new Date().toISOString()
            });
        }
    }

    // Camera Exposure Mode Management
    setExposureMode(mode) {
        const exposureSelect = document.getElementById('exposure-mode');
        if (exposureSelect) {
            exposureSelect.value = mode;
            this.updateExposureMode();
        }
    }

    updateExposureMode() {
        const exposureSelect = document.getElementById('exposure-mode');
        const statusDiv = document.getElementById('exposure-mode-status');
        const messageSpan = document.getElementById('exposure-mode-message');
        
        if (exposureSelect && statusDiv && messageSpan) {
            const selectedMode = exposureSelect.value;
            const selectedOption = exposureSelect.options[exposureSelect.selectedIndex];
            const description = selectedOption.text.split(' - ')[1] || selectedOption.text;
            
            statusDiv.style.display = 'block';
            messageSpan.textContent = `Selected: ${selectedMode} - ${description}`;
            
            // Auto-hide after 3 seconds
            setTimeout(() => {
                statusDiv.style.display = 'none';
            }, 3000);
        }
    }

    async applyExposureMode() {
        const exposureSelect = document.getElementById('exposure-mode');
        const statusDiv = document.getElementById('exposure-mode-status');
        const messageSpan = document.getElementById('exposure-mode-message');
        
        if (!exposureSelect || !statusDiv || !messageSpan) {
            console.error('Exposure mode elements not found');
            return;
        }
        
        const selectedMode = exposureSelect.value;
        
        try {
            statusDiv.style.display = 'block';
            statusDiv.className = 'mt-2 alert alert-info';
            messageSpan.innerHTML = `<i class="bi bi-hourglass-split"></i> Applying exposure mode: ${selectedMode}...`;
            
            // Send the exposure mode update to the device
            const response = await fetch('/api/camera/exposure-mode', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ 
                    exposureMode: selectedMode 
                })
            });
            
            if (response.ok) {
                const result = await response.json();
                statusDiv.className = 'mt-2 alert alert-success';
                messageSpan.innerHTML = `<i class="bi bi-check-circle"></i> Successfully applied exposure mode: ${selectedMode}`;
                
                // Optionally trigger a test capture to show the effect
                setTimeout(() => {
                    this.testExposureMode();
                }, 1000);
            } else {
                const error = await response.json();
                throw new Error(error.message || 'Failed to apply exposure mode');
            }
        } catch (error) {
            console.error('Failed to apply exposure mode:', error);
            statusDiv.className = 'mt-2 alert alert-danger';
            messageSpan.innerHTML = `<i class="bi bi-exclamation-triangle"></i> Failed to apply exposure mode: ${error.message}`;
        }
        
        // Auto-hide after 5 seconds
        setTimeout(() => {
            statusDiv.style.display = 'none';
        }, 5000);
    }

    async testExposureMode() {
        const statusDiv = document.getElementById('exposure-mode-status');
        const messageSpan = document.getElementById('exposure-mode-message');
        
        if (!statusDiv || !messageSpan) {
            console.error('Exposure mode status elements not found');
            return;
        }
        
        try {
            statusDiv.style.display = 'block';
            statusDiv.className = 'mt-2 alert alert-info';
            messageSpan.innerHTML = `<i class="bi bi-camera"></i> Capturing test image with current exposure mode...`;
            
            // Trigger a test capture
            const response = await fetch('/api/camera/test-capture', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                }
            });
            
            if (response.ok) {
                const result = await response.json();
                statusDiv.className = 'mt-2 alert alert-success';
                messageSpan.innerHTML = `<i class="bi bi-check-circle"></i> Test capture completed! Check the debug images section for results.`;
                
                // If we're on the debug images section, refresh it
                if (document.getElementById('debug-images-section').style.display !== 'none') {
                    setTimeout(() => {
                        this.loadDebugImages();
                    }, 1000);
                }
            } else {
                const error = await response.json();
                throw new Error(error.message || 'Failed to capture test image');
            }
        } catch (error) {
            console.error('Failed to capture test image:', error);
            statusDiv.className = 'mt-2 alert alert-danger';
            messageSpan.innerHTML = `<i class="bi bi-exclamation-triangle"></i> Failed to capture test image: ${error.message}`;
        }
        
        // Auto-hide after 5 seconds
        setTimeout(() => {
            statusDiv.style.display = 'none';
        }, 5000);
    }

    // Load current camera configuration
    async loadCameraConfiguration() {
        try {
            const response = await fetch('/api/camera/configuration');
            if (response.ok) {
                const config = await response.json();
                
                // Update the exposure mode dropdown
                const exposureSelect = document.getElementById('exposure-mode');
                if (exposureSelect && config.exposureMode) {
                    exposureSelect.value = config.exposureMode;
                }
                
                console.log('Camera configuration loaded:', config);
            }
        } catch (error) {
            console.error('Failed to load camera configuration:', error);
        }
    }

    // Cleanup
    destroy() {
        if (this.connection) {
            this.connection.stop();
        }
        
        this.stopCameraPreview();
        
        Object.values(this.charts).forEach(chart => {
            if (chart) {
                chart.destroy();
            }
        });
    }
}

// Global functions for HTML event handlers
function showSection(sectionId) {
    if (window.dashboard) {
        window.dashboard.showSection(sectionId);
    }
}

function startCameraPreview() {
    if (window.dashboard) {
        window.dashboard.startCameraPreview();
    }
}

function stopCameraPreview() {
    if (window.dashboard) {
        window.dashboard.stopCameraPreview();
    }
}

function updateExposureMode() {
    if (window.dashboard) {
        window.dashboard.updateExposureMode();
    }
}

function setExposureMode(mode) {
    if (window.dashboard) {
        window.dashboard.setExposureMode(mode);
    }
}

function applyExposureMode() {
    if (window.dashboard) {
        window.dashboard.applyExposureMode();
    }
}

function testExposureMode() {
    if (window.dashboard) {
        window.dashboard.testExposureMode();
    }
}

// Initialize dashboard when page loads
document.addEventListener('DOMContentLoaded', () => {
    window.dashboard = new WellMonitorDashboard();
    
    // Show dashboard section by default
    window.dashboard.showSection('dashboard');
    
    // Load camera configuration when camera setup section is shown
    document.addEventListener('sectionChanged', (event) => {
        if (event.detail.sectionId === 'camera-position') {
            window.dashboard.loadCameraConfiguration();
        }
    });
    
    // Initialize tooltips
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
});

// Cleanup on page unload
window.addEventListener('beforeunload', () => {
    if (window.dashboard) {
        window.dashboard.destroy();
    }
});
