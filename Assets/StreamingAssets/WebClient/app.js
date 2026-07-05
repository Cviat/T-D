document.addEventListener('DOMContentLoaded', () => {
    const joinForm = document.getElementById('join-form');
    const joinScreen = document.getElementById('join-screen');
    const waitingScreen = document.getElementById('waiting-screen');
    const waitingName = document.getElementById('waiting-name');
    const waitingPhoto = document.getElementById('waiting-photo');
    const tokenCarousel = document.getElementById('token-carousel');
    const selectedTokenIdInput = document.getElementById('selected-token-id');
    const btnLeft = document.getElementById('carousel-left');
    const btnRight = document.getElementById('carousel-right');
    const joinBtn = document.getElementById('join-btn');

    let tokens = [];
    let currentIndex = 0;

    // Fetch tokens from server
    async function loadTokens() {
        try {
            const response = await fetch('/api/lobby/tokens');
            if (response.ok) {
                tokens = await response.json();
                renderCarousel();
                if (tokens.length > 0) {
                    joinBtn.disabled = false;
                    joinBtn.textContent = 'Войти в игру';
                } else {
                    tokenCarousel.innerHTML = '<p>Нет доступных фишек</p>';
                    joinBtn.textContent = 'Ошибка (пусто)';
                }
            }
        } catch (e) {
            console.error('Failed to load tokens', e);
            tokenCarousel.innerHTML = '<p>Ошибка сети</p>';
        }
    }

    function renderCarousel() {
        tokenCarousel.innerHTML = '';
        tokens.forEach((token, index) => {
            const el = document.createElement('div');
            el.className = 'token-item';
            
            // Render portrait under the frame
            let html = `<div class="token-visual">`;
            if (token.portraitUrl && !token.portraitUrl.endsWith('path=')) {
                html += `<img src="${token.portraitUrl}" class="token-portrait">`;
            }
            if (token.frameUrl && !token.frameUrl.endsWith('path=')) {
                html += `<img src="${token.frameUrl}" class="token-frame">`;
            }
            html += `</div><div class="token-name">${token.name}</div>`;
            
            el.innerHTML = html;
            tokenCarousel.appendChild(el);
        });
        updateCarousel();
    }

    function updateCarousel() {
        if (tokens.length === 0) return;
        const items = document.querySelectorAll('.token-item');
        items.forEach((item, index) => {
            item.className = 'token-item'; // reset
            if (index === currentIndex) {
                item.classList.add('active');
            } else if (index < currentIndex) {
                item.classList.add('prev');
            }
        });
        selectedTokenIdInput.value = tokens[currentIndex].id;
    }

    btnLeft.addEventListener('click', () => {
        if (currentIndex > 0) {
            currentIndex--;
            updateCarousel();
        }
    });

    btnRight.addEventListener('click', () => {
        if (currentIndex < tokens.length - 1) {
            currentIndex++;
            updateCarousel();
        }
    });

    loadTokens();

    const playerPhotoInput = document.getElementById('player-photo');
    const photoPreviewContainer = document.getElementById('photo-preview-container');
    const photoPreviewImg = document.getElementById('photo-preview-img');

    let currentPhotoBase64 = null;

    playerPhotoInput.addEventListener('change', (e) => {
        const file = e.target.files[0];
        if (file) {
            const reader = new FileReader();
            reader.onload = (evt) => {
                const img = new Image();
                img.onload = () => {
                    const canvas = document.createElement('canvas');
                    const MAX_WIDTH = 512;
                    const MAX_HEIGHT = 512;
                    let width = img.width;
                    let height = img.height;

                    if (width > height) {
                        if (width > MAX_WIDTH) {
                            height *= MAX_WIDTH / width;
                            width = MAX_WIDTH;
                        }
                    } else {
                        if (height > MAX_HEIGHT) {
                            width *= MAX_HEIGHT / height;
                            height = MAX_HEIGHT;
                        }
                    }
                    canvas.width = width;
                    canvas.height = height;
                    const ctx = canvas.getContext('2d');
                    ctx.drawImage(img, 0, 0, width, height);
                    
                    currentPhotoBase64 = canvas.toDataURL('image/jpeg', 0.85);
                    photoPreviewImg.src = currentPhotoBase64;
                    photoPreviewContainer.style.display = 'block';
                };
                img.src = evt.target.result;
            };
            reader.readAsDataURL(file);
        } else {
            currentPhotoBase64 = null;
            photoPreviewContainer.style.display = 'none';
        }
    });

    let myPlayerId = null;
    let globalPlayerName = "";
    let globalPlayerDesc = "";

    // Form submit logic
    joinForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        
        const playerName = document.getElementById('player-name').value.trim();
        const playerDesc = document.getElementById('player-desc').value.trim();
        const tokenId = selectedTokenIdInput.value;
        
        if (!playerName || !tokenId || !currentPhotoBase64) {
            alert('Пожалуйста, заполните все поля и выберите фото.');
            return;
        }

        const payload = {
            name: playerName,
            description: playerDesc,
            photoBase64: currentPhotoBase64,
            tokenId: tokenId
        };

        joinBtn.textContent = 'Подключение...';
        joinBtn.disabled = true;

        try {
            const response = await fetch('/api/lobby/join', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(payload)
            });

            if (response.ok) {
                const resultData = await response.json();
                myPlayerId = resultData.playerId;
                globalPlayerName = playerName;
                globalPlayerDesc = playerDesc;

                joinScreen.classList.remove('active');
                waitingScreen.classList.add('active');
                waitingName.textContent = playerName;
                
                // Show uploaded photo instead of token
                waitingPhoto.style.backgroundImage = `url(${currentPhotoBase64})`;
                
                // Start polling for game start
                pollGameStatus(myPlayerId);
            } else {
                alert('Ошибка при подключении к столу.');
                joinBtn.textContent = 'Войти в игру';
                joinBtn.disabled = false;
            }
        } catch (error) {
            console.error('Error joining:', error);
            alert('Не удалось связаться с сервером.');
            joinBtn.textContent = 'Войти в игру';
            joinBtn.disabled = false;
        }
    });

    async function pollGameStatus(playerId) {
        try {
            const response = await fetch(`/api/lobby/status?playerId=${playerId}`);
            if (response.ok) {
                const data = await response.json();
                if (data.status === 'game_started') {
                    // Transition to game screen
                    waitingScreen.classList.remove('active');
                    document.getElementById('game-screen').classList.add('active');
                    
                    document.getElementById('game-name').textContent = globalPlayerName;
                    document.getElementById('game-photo').style.backgroundImage = `url(${currentPhotoBase64})`;
                    document.getElementById('modal-desc').textContent = globalPlayerDesc;
                    
                    startGameStatePolling(playerId);
                    
                    return; // Stop polling
                }
            }
        } catch (error) {
            console.error('Error polling status:', error);
        }
        
        setTimeout(() => pollGameStatus(playerId), 2000);
    }

    async function startGameStatePolling(playerId) {
        try {
            const response = await fetch(`/api/game/state?playerId=${playerId}`);
            if (response.ok) {
                const data = await response.json();
                const modal = document.getElementById('transition-modal');
                if (data.prompt) {
                    document.getElementById('transition-text').textContent = data.prompt;
                    modal.classList.add('active');
                } else {
                    modal.classList.remove('active');
                }
            }
        } catch (error) {
            console.error('Error polling game state:', error);
        }
        setTimeout(() => startGameStatePolling(playerId), 2000);
    }

    // --- GAME CONTROLS ---

    async function sendMoveCommand(dx, dy) {
        if (!myPlayerId) return;
        try {
            await fetch('/api/action/move', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ playerId: myPlayerId, dirX: dx, dirY: dy })
            });
        } catch (e) {
            console.error('Move error:', e);
        }
    }

    function bindDpadButton(btnId, dx, dy) {
        const btn = document.getElementById(btnId);
        if (!btn) return;
        
        let intervalId = null;

        const startMoving = (e) => {
            e.preventDefault(); // Prevents double firing on mobile
            if (intervalId !== null) return; // Already moving
            
            sendMoveCommand(dx, dy); // First immediate step
            intervalId = setInterval(() => {
                sendMoveCommand(dx, dy);
            }, 300); // 300ms per step when holding
        };

        const stopMoving = (e) => {
            if (intervalId !== null) {
                clearInterval(intervalId);
                intervalId = null;
            }
        };

        btn.addEventListener('mousedown', startMoving);
        btn.addEventListener('touchstart', startMoving, { passive: false });

        btn.addEventListener('mouseup', stopMoving);
        btn.addEventListener('mouseleave', stopMoving);
        btn.addEventListener('touchend', stopMoving);
        btn.addEventListener('touchcancel', stopMoving);
    }

    bindDpadButton('btn-up', 0, 1);
    bindDpadButton('btn-down', 0, -1);
    bindDpadButton('btn-left', -1, 0);
    bindDpadButton('btn-right', 1, 0);
    
    bindDpadButton('btn-up-left', -1, 1);
    bindDpadButton('btn-up-right', 1, 1);
    bindDpadButton('btn-down-left', -1, -1);
    bindDpadButton('btn-down-right', 1, -1);

    // Modal logic
    const abilitiesModal = document.getElementById('abilities-modal');
    document.getElementById('btn-abilities').addEventListener('click', () => {
        abilitiesModal.classList.add('active');
    });
    document.getElementById('btn-close-modal').addEventListener('click', () => {
        abilitiesModal.classList.remove('active');
    });

    // Transition Modal logic
    async function sendTransitionCommand(action) {
        if (!myPlayerId) return;
        try {
            await fetch('/api/action/transition', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ playerId: myPlayerId, action: action })
            });
            document.getElementById('transition-modal').classList.remove('active');
        } catch (e) {
            console.error('Transition error:', e);
        }
    }

    document.getElementById('btn-transition-confirm').addEventListener('click', () => {
        sendTransitionCommand('confirm');
    });
    document.getElementById('btn-transition-cancel').addEventListener('click', () => {
        sendTransitionCommand('cancel');
    });
});
