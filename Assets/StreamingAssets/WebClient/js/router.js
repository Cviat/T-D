const screens = new Map();

export function initRouter() {
    document.querySelectorAll('.screen').forEach(screen => {
        screens.set(screen.id.replace('screen-', ''), screen);
    });

    document.querySelectorAll('[data-route]').forEach(button => {
        button.addEventListener('click', () => showScreen(button.dataset.route));
    });
}

export function showScreen(name) {
    screens.forEach(screen => screen.classList.remove('active'));
    const screen = screens.get(name);
    if (screen) {
        screen.classList.add('active');
    }
}
