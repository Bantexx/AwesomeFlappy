window.flappy = (function () {
    const images = {};
    let canvas, ctx;
    let devicePixelRatio = window.devicePixelRatio || 1;

    function initCanvas(canvasId) {
        canvas = document.getElementById(canvasId);
        ctx = canvas.getContext('2d');
        resizeCanvas();
        window.addEventListener('resize', resizeCanvas);
    }

    function resizeCanvas() {
        if (!canvas) return;
        const rect = canvas.getBoundingClientRect();
        canvas.width = Math.round(rect.width * devicePixelRatio);
        canvas.height = Math.round(rect.height * devicePixelRatio);
        canvas.style.width = rect.width + 'px';
        canvas.style.height = rect.height + 'px';
        ctx && ctx.setTransform(devicePixelRatio, 0, 0, devicePixelRatio, 0, 0);
    }

    function loadImages(paths) {
        // defensive: if paths is a JSON string, try to parse it
        try {
            if (typeof paths === 'string') {
                // попытка распарсить, т.к. иногда Blazor шлёт JSON-строку
                try {
                    const parsed = JSON.parse(paths);
                    paths = parsed;
                } catch (e) {
                    // если не JSON — обернуть в массив
                    paths = [paths];
                }
            }

            // если объект с полем 'd' или '0' (возможные формы), попробуем извлечь массив
            if (!Array.isArray(paths) && typeof paths === 'object') {
                // если это уже имеет .length и элементы — привести к массиву
                if (typeof paths.length === 'number') {
                    paths = Array.prototype.slice.call(paths);
                } else {
                    // пробуем извлечь первый пригодный массив внутри объекта
                    for (const k in paths) {
                        if (Array.isArray(paths[k])) { paths = paths[k]; break; }
                    }
                }
            }

            if (!Array.isArray(paths)) {
                // на всякий случай — обернём во внешний массив
                paths = [paths];
            }
        } catch (ex) {
            console.warn('loadImages: unexpected paths shape, normalizing. Error:', ex);
            if (!Array.isArray(paths)) paths = [paths];
        }

        const promises = paths.map(p => {
            return new Promise((resolve, reject) => {
                // p может быть объект { i: 'images/bird1.svg' } или просто строкой
                const src = (typeof p === 'string') ? p : (p && p.toString ? p.toString() : null);
                if (!src) { resolve(); return; }

                const img = new Image();
                img.onload = () => {
                    images[src] = img;
                    resolve();
                };
                img.onerror = (e) => {
                    console.warn('image load error', src, e);
                    resolve(); // не ломаем Promise.all из‑за одной неудачи
                };
                img.src = src;
            });
        });

        return Promise.all(promises);
    }

    const sounds = {};
    function loadSound(name, url) {
        const a = new Audio(url);
        a.preload = 'auto';
        sounds[name] = a;
    }
    function playSound(name) {
        try {
            const s = sounds[name];
            if (!s) return;
            s.currentTime = 0;
            s.play();
        } catch (e) {
            console.warn('audio play error', e);
        }
    }

    function drawFrame(stateJson) {
        if (!ctx) return;
        const s = JSON.parse(stateJson);

        // canvas pixel sizes (CSS width/height already set in resizeCanvas)
        const canvasCssW = canvas.width / devicePixelRatio;
        const canvasCssH = canvas.height / devicePixelRatio;

        // logical world sizes from engine (fallback to canvas size if absent)
        const worldW = (s.worldWidth && s.worldWidth > 0) ? s.worldWidth : canvasCssW;
        const worldH = (s.worldHeight && s.worldHeight > 0) ? s.worldHeight : canvasCssH;

        // compute scale from world coords -> canvas coords
        const scaleX = canvasCssW / worldW;
        const scaleY = canvasCssH / worldH;

        // clear full canvas (in CSS pixels)
        ctx.clearRect(0, 0, canvasCssW, canvasCssH);

        // draw background stretched to canvas
        const bgKey = s.background || 'images/bg0.svg';
        if (images[bgKey]) {
            ctx.drawImage(images[bgKey], 0, 0, canvasCssW, canvasCssH);
        } else {
            ctx.fillStyle = '#87CEEB';
            ctx.fillRect(0, 0, canvasCssW, canvasCssH);
        }

        // save and apply world -> canvas scaling
        ctx.save();
        ctx.scale(scaleX, scaleY);

        // draw pipes (positions are in world coords)
        if (s.pipes && s.pipes.length) {
            s.pipes.forEach(p => {
                const img = images['images/pipe.svg'];
                const pipeW = p.width;
                // top pipe: from y=0 to y=topHeight
                const topH = p.topHeight;
                // bottom pipe: from y=bottomY to worldH
                const bottomY = p.bottomY;
                const bottomH = Math.max(0, worldH - bottomY);

                if (img) {
                    // draw top pipe flipped vertically
                    // We'll draw an image of size (pipeW x topH) anchored at (p.x, 0)
                    // To flip, draw on temp transform
                    if (topH > 0) {
                        ctx.save();
                        // translate to top-left corner of area to draw
                        // We want to draw the top part with its top at y=0
                        // To flip vertically: translate to (p.x + pipeW/2, topH/2) and rotate PI
                        ctx.translate(p.x + pipeW / 2, topH / 2);
                        ctx.rotate(Math.PI);
                        // after rotation, draw image centered
                        ctx.drawImage(img, -pipeW / 2, -topH / 2, pipeW, topH);
                        ctx.restore();
                    }
                    // draw bottom pipe normally
                    if (bottomH > 0) {
                        ctx.drawImage(img, p.x, bottomY, pipeW, bottomH);
                    }
                } else {
                    // fallback rectangles if sprite missing
                    ctx.fillStyle = '#2ecc71';
                    if (topH > 0) ctx.fillRect(p.x, 0, pipeW, topH);
                    if (bottomH > 0) ctx.fillRect(p.x, bottomY, pipeW, bottomH);
                }
            });
        }

        // draw player (in world coords)
        if (s.player) {
            const player = s.player;
            const imgKey = player.sprite;
            const img = images[imgKey];
            ctx.save();
            ctx.translate(player.x, player.y);
            ctx.rotate(player.rotation || 0);
            if (img) {
                const size = player.size || 40;
                ctx.drawImage(img, -size/2, -size/2, size, size);
            } else {
                ctx.fillStyle = 'yellow';
                ctx.beginPath();
                ctx.arc(0,0,20,0,Math.PI*2);
                ctx.fill();
            }
            ctx.restore();
        }

        // restore transform back to CSS pixels for UI overlay
        ctx.restore();

        // UI: score top-center (draw in CSS pixels)
        ctx.save();
        ctx.fillStyle = 'white';
        ctx.font = 'bold 26px sans-serif';
        ctx.textAlign = 'center';
        ctx.fillText((s.score ?? 0).toString(), canvasCssW/2, 40);
        ctx.restore();

        if (s.paused) {
            ctx.save();
            ctx.fillStyle = 'rgba(0,0,0,0.4)';
            ctx.fillRect(0,0,canvasCssW,canvasCssH);
            ctx.fillStyle = 'white';
            ctx.font = '20px sans-serif';
            ctx.textAlign = 'center';
            ctx.fillText('Paused', canvasCssW/2, canvasCssH/2);
            ctx.restore();
        }
    }

    let rafId = null;
    let dotnetRef = null;

    function startLoop(dotnetObject) {
        dotnetRef = dotnetObject;
        function frame(ts) {
            if (dotnetRef) {
                dotnetRef.invokeMethodAsync('FrameCallback', ts).catch(err => console.error(err));
            }
            rafId = requestAnimationFrame(frame);
        }
        if (rafId) cancelAnimationFrame(rafId);
        rafId = requestAnimationFrame(frame);
    }

    function stopLoop() {
        if (rafId) cancelAnimationFrame(rafId);
        rafId = null;
        dotnetRef = null;
    }

    function attachTouchHandler(elementId, callbackName, dotnetObject) {
        const el = document.getElementById(elementId);
        if (!el) return;
        function handler(e) {
            e.preventDefault();
            if (dotnetObject) dotnetObject.invokeMethodAsync(callbackName);
        }
        el.addEventListener('pointerdown', handler, {passive:false});
    }

    function saveLocal(key, value) {
        try {
            localStorage.setItem(key, JSON.stringify(value));
            return true;
        } catch (e) {
            console.warn('saveLocal error', e);
            return false;
        }
    }
    function loadLocal(key) {
        try {
            return localStorage.getItem(key); // raw string or null
        } catch (e) {
            console.warn('loadLocal error', e);
            return null;
        }
    }

    return {
        initCanvas,
        loadImages,
        drawFrame,
        startLoop,
        stopLoop,
        loadSound,
        playSound,
        attachTouchHandler,
        saveLocal,
        loadLocal
    };
})();