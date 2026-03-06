// Mosaic loading screen shown while Blazor bootstraps.
// Same behavior as in the Student app.

(function () {
    const root = document.getElementById('mosaic-loader-root');
    if (!root) return;

    const cols = 16;
    const rows = 10;
    const total = cols * rows;

    const tileColors = [
        'tile-primary',
        'tile-accent',
        'tile-secondary',
        'tile-muted',
        'tile-card'
    ];

    root.innerHTML = `
<div class="mosaic-container">
    <div class="mosaic-content">
        <div class="mosaic-grid" style="grid-template-columns: repeat(${cols}, 1fr); grid-template-rows: repeat(${rows}, 1fr);"></div>
        <div class="mosaic-footer">
            <p class="mosaic-text">Loading</p>
            <div class="mosaic-progress-track">
                <div class="mosaic-progress-bar"></div>
            </div>
            <p class="mosaic-percentage">0%</p>
        </div>
    </div>
</div>`;

    const grid = root.querySelector('.mosaic-grid');
    const progressBar = root.querySelector('.mosaic-progress-bar');
    const percentageEl = root.querySelector('.mosaic-percentage');

    if (!grid || !progressBar || !percentageEl) return;

    const tiles = [];
    for (let i = 0; i < total; i++) {
        const cell = document.createElement('div');
        cell.className = 'mosaic-cell';
        const tile = document.createElement('div');
        tile.className = 'mosaic-tile';
        cell.appendChild(tile);
        grid.appendChild(cell);
        tiles.push(tile);
    }

    function shuffle(arr) {
        for (let i = arr.length - 1; i > 0; i--) {
            const j = Math.floor(Math.random() * (i + 1));
            [arr[i], arr[j]] = [arr[j], arr[i]];
        }
        return arr;
    }

    let order = shuffle(Array.from({ length: total }, (_, i) => i));
    let currentIndex = 0;
    let phase = 'filling'; // 'filling' | 'clearing'
    let timer = null;

    function setProgress(pct) {
        const clamped = Math.max(0, Math.min(100, pct | 0));
        progressBar.style.width = `${clamped}%`;
        percentageEl.textContent = `${clamped}%`;
    }

    function fillStep() {
        if (currentIndex >= total) {
            clearInterval(timer);
            timer = null;
            setProgress(100);
            setTimeout(() => {
                phase = 'clearing';
                order = shuffle(Array.from({ length: total }, (_, i) => i));
                currentIndex = 0;
                timer = setInterval(onTick, 20);
            }, 800);
            return;
        }

        const batch = Math.min(3, total - currentIndex);
        for (let b = 0; b < batch; b++) {
            const tileIdx = order[currentIndex];
            const tile = tiles[tileIdx];
            tile.classList.add('visible');
            tile.classList.add(tileColors[Math.floor(Math.random() * tileColors.length)]);
            currentIndex++;
        }

        const progress = (currentIndex / total) * 100;
        setProgress(progress);
    }

    function clearStep() {
        if (currentIndex >= total) {
            clearInterval(timer);
            timer = null;
            setTimeout(() => {
                phase = 'filling';
                order = shuffle(Array.from({ length: total }, (_, i) => i));
                currentIndex = 0;
                setProgress(0);
                timer = setInterval(onTick, 30);
            }, 500);
            return;
        }

        const batch = Math.min(4, total - currentIndex);
        for (let b = 0; b < batch; b++) {
            const tileIdx = order[currentIndex];
            const tile = tiles[tileIdx];
            tile.classList.remove('visible', 'tile-primary', 'tile-accent', 'tile-secondary', 'tile-muted', 'tile-card');
            currentIndex++;
        }

        const visibleCount = tiles.filter(t => t.classList.contains('visible')).length;
        const progress = (visibleCount / total) * 100;
        setProgress(progress);
    }

    function onTick() {
        if (phase === 'filling') {
            fillStep();
        } else if (phase === 'clearing') {
            clearStep();
        }
    }

    timer = setInterval(onTick, 30);
})();

