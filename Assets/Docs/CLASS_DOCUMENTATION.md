# Документация Архитектуры и Классов Проекта

Этот документ описывает общую архитектуру проекта RPG Table, основные подсистемы, паттерны проектирования, потоки данных, а также содержит подробное описание назначения всех ключевых классов.

---

## 1. Архитектура проекта и Слои (Layers)

Проект построен по принципу четкого разделения данных (Data/Models), представления (Views/Presentation) и управления (Logic/Controllers). Это позволяет изолировать логику симуляции от визуального представления в Unity, а также легко поддерживать многопользовательский режим и отображение на двух экранах.

```mermaid
graph TD
    subgraph Data Layer (Модели)
        BT[BoardToken]
        AC[AbilityCard]
        IC[ItemCard]
        MD[SavedMapData / CampaignPlayerData]
    end

    subgraph Logic Layer (Контроллеры и Менеджеры)
        CGS[CampaignGameSession]
        CGL[CampaignGameLoader]
        CML[CampaignMapLoader]
        CM[CombatManager]
        WSM[WebServerManager]
    end

    subgraph View Layer (Представление и UI)
        CRT[CampaignRuntimeToken]
        BGV[BoardGridVisual]
        CGUI[CampaignGameUI]
        EIV[EntityInspectorView]
    end

    BT --> Logic Layer
    AC --> Logic Layer
    IC --> Logic Layer
    MD --> Logic Layer

    Logic Layer --> CRT
    Logic Layer --> BGV
    Logic Layer --> CGUI
    Logic Layer --> EIV
```

### Основные Архитектурные Решения:
1. **Разделение Модели и Отображения (Data/View Separation)**:
   Игровые сущности не привязаны жестко к GameObject-ам сцены. GameObject в сцене является лишь визуальным представлением данных, находящихся в сессии или моделях. Например, `BoardToken` хранит HP, броню и статы токена, в то время как `CampaignRuntimeToken` отвечает за отрисовку спрайта, полосок здоровья, анимаций и прием кликов.
2. **Два Экрана (GM & Player Views)**:
   Приложение поддерживает вывод для Мастера (GM Screen) с полной информацией (все токены, скрытые зоны, панели управления) и вывод для Игроков (Player Screen, обычно второй монитор по HDMI) с обрезанным/отфильтрованным отображением (`CampaignPlayerViewManager`), где скрыты невидимые объекты и активен туман войны.
3. **Локальный Веб-Сервер (Mobile Web Client)**:
   Скрипт `WebServerManager` запускает асинхронный TCP-сервер прямо внутри игры. Это позволяет игрокам за столом подключаться со своих смартфонов/планшетов без установки приложения, импортировать своих персонажей, управлять своими токенами и бросать виртуальные кубики.
4. **Автоматизация Сборки UI (Editor Builders)**:
   Все основные сцены и интерфейсы (Главное меню, Редактор Карт, Редактор Персонажей, Стол игры) могут быть автоматически пересобраны с помощью редакторских скриптов в папке `Editor/` на основе префабов и изображений, что минимизирует ручную работу в Инспекторе Unity.

---

## 2. Подсистемы и Описание Классов

Ниже приведен список всех классов проекта по директориям, с указанием путей и ссылок на исходные файлы.

### 2.1 Core (Ядро игры)
Содержит чистые модели данных, шаблоны карт способностей, предметов и эффектов.

*   **[AbilityCard.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Core/AbilityCard.cs)**:
    ScriptableObject, представляющий карту способности персонажа. Хранит стоимость (энергию/очки действия), дальность применения, тип атаки, наносимый урон, область действия (AoE) и описание.
*   **[ItemCard.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Core/ItemCard.cs)**:
    ScriptableObject, описывающий предмет снаряжения или зелье. Содержит описание, вес, стоимость, а также модификаторы характеристик и эффекты при использовании.
*   **[BoardToken.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Core/BoardToken.cs)**:
    Базовый класс данных токена на поле. Хранит информацию о здоровье (HP), броне, фракции/команде (`TokenTeam`), размере (footprint) и списке способностей/предметов.
*   **[CombatAttribute.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Core/CombatAttribute.cs)**:
    Реализует атрибуты персонажа (например, силу, ловкость, инициативу) с поддержкой базового значения, временных модификаторов и расчета текущего эффективного значения.
*   **[ActiveStatusEffect.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Core/ActiveStatusEffect.cs)**:
    Структура данных для описания активных баффов/дебаффов, наложенных на токен, с указанием оставшихся ходов.
*   **[AttackType.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Core/AttackType.cs)**:
    Enum со списком типов атак и урона (ближний бой, дальний бой, заклинание, лечение и т.д.).
*   **[RuntimeSpriteFactory.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Core/RuntimeSpriteFactory.cs)**:
    Утилита для генерации стандартных текстур и спрайтов во время выполнения (например, однотонные цветные квадраты, круги или текстура сетки), что позволяет не зависеть от жестких файловых ассетов при сборке прототипа.

### 2.2 Board (Игровое Поле и Сетка)
Управляет математикой сетки, визуализацией линий и скрытием областей карты.

*   **[BoardGrid.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Board/BoardGrid.cs)**:
    Определяет размерность игрового поля (ширину, высоту), размер ячейки и содержит методы для конвертации мировых координат Unity в координаты сетки (и обратно).
*   **[BoardGridVisual.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Board/BoardGridVisual.cs)**:
    Рисует линии сетки поверх игрового поля, помогая ориентироваться в масштабе карты.
*   **[FogOfWarController.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Board/FogOfWarController.cs)**:
    Управляет туманом войны (Fog of War) на уровне отдельных ячеек сетки. Позволяет мастеру скрывать или открывать части карты для игроков.
*   **[GridHighlighter.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Board/GridHighlighter.cs)**:
    Визуализирует доступные для перемещения клетки (зону досягаемости), траектории движения, а также зоны действия способностей (AoE) при наведении.

### 2.3 Runtime (Игровой Сеанс и Логика)
Центральный слой, оркеструющий игровой процесс, инициативу, переходы по карте кампании и визуальные компоненты токенов.

*   **[CampaignGameContext.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Runtime/CampaignGameContext.cs)**:
    Статический класс-контейнер (Service Locator), предоставляющий глобальный доступ к текущим инстансам загрузчиков, спавнеров и интерфейсов во время игровой сессии.
*   **[CampaignGameLoader.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Runtime/CampaignGameLoader.cs)**:
    Управляет инициализацией и очисткой игровой сессии. Загружает JSON-данные о выбранной кампании, расставляет стартовые токены и инициализирует UI стола.
*   **[CampaignGameSession.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Runtime/CampaignGameSession.cs)**:
    Хранит текущее динамическое состояние сессии: список подключенных игроков (`CampaignPlayerData`), состояние выживших NPC на картах и глобальные переменные переходов.
*   **[CampaignGameUI.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Runtime/CampaignGameUI.cs)**:
    Координирует всю логику пользовательского интерфейса на экране GM во время игры (переключение панелей, броски кубов, инспектирование токенов, управление инициативой).
*   **[CampaignMapLoader.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Runtime/CampaignMapLoader.cs)**:
    Считывает файлы карт с диска, создает спрайты окружения, строит сетку игрового поля, настраивает зоны спавна и зоны выхода, а также центрирует камеру.
*   **[CampaignPlayerViewManager.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Runtime/CampaignPlayerViewManager.cs)**:
    Управляет выводом изображения на второй экран (Player View). Синхронизирует положение камеры, скрывает невидимые для игроков токены и отображает только разрешенные зоны.
*   **[CampaignRuntimeToken.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Runtime/CampaignRuntimeToken.cs)**:
    Компонент в сцене Unity, представляющий физический токен. Обрабатывает позиционирование по сетке, индикацию выбранной цели, цвет рамки фракции и анимацию перемещения.
*   **[CampaignTokenSpawner.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Runtime/CampaignTokenSpawner.cs)**:
    Создает GameObject-ы токенов в точках спавна при загрузке карты.
*   **[CampaignSelectionController.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Runtime/CampaignSelectionController.cs)** и **[CampaignSelectionButton.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Runtime/CampaignSelectionButton.cs)**:
    Обрабатывают UI выбора доступной кампании из сохраненных файлов.
*   **[CampaignTokenContextClick.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Runtime/CampaignTokenContextClick.cs)**:
    Открывает контекстное меню токена по правому клику мыши (удаление токена, изменение фракции, скрытие от игроков, добавление в список инициативы).
*   **[CampaignTransitionController.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Runtime/CampaignTransitionController.cs)**:
    Отвечает за переходы между экраном выбора кампании, картой приключения и боевым столом с плавным затуханием экрана.
*   **[CombatManager.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Runtime/CombatManager.cs)**:
    Менеджер пошагового боя. Управляет очередью инициативы, переключением ходов, наложением эффектов статуса и применением карт действий/способностей на цели.
*   **[PlayerViewTokenMover.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Runtime/PlayerViewTokenMover.cs)**:
    Синхронизирует плавное перемещение токенов на экране игроков при перетаскивании их мастером на экране GM.
*   **[TokenAttackAnimator.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Runtime/TokenAttackAnimator.cs)**:
    Визуализирует атаки токенов с помощью небольших анимаций покачивания/рывка в сторону цели и всплывающих эффектов.
*   **[TokenHealthArmorBars.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Runtime/TokenHealthArmorBars.cs)**:
    Управляет 3D/World UI-индикаторами здоровья (HP) и брони, отображаемыми прямо над головой токена на карте.

### 2.4 Runtime/Networking (Сетевые функции)
Отвечает за сетевое взаимодействие с мобильными телефонами игроков.

*   **[WebServerManager.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Runtime/Networking/WebServerManager.cs)**:
    Запускает локальный веб-сервер на TCP-сокетах. Отдает статический контент веб-приложения (клиент на HTML/JS) и обрабатывает WebSocket-подключения игроков. С его помощью игроки могут регистрироваться в сессии, загружать свои аватары, просматривать характеристики персонажей и кидать кубы со смартфона.

### 2.5 UI / Runtime UI (Интерфейс Мастера)
Элементы управления интерфейсом GM во время игрового процесса и главного меню.

*   **[CampaignUIManager.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Runtime/UI/CampaignUIManager.cs)**:
    Предоставляет методы для открытия/закрытия панелей игрового стола.
*   **[EntityInspectorView.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Runtime/UI/EntityInspectorView.cs)**:
    Боковая панель подробного просмотра выделенной сущности (токен игрока или монстра). Позволяет GM вручную редактировать HP, броню, добавлять баффы и просматривать инвентарь/способности.
*   **[GMBottomToolsView.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Runtime/UI/GMBottomToolsView.cs)**:
    Нижняя панель инструментов мастера (инструменты рисования сетки, переключение режимов выделения, включение тумана войны и спавн токенов).
*   **[InitiativeRowView.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Runtime/UI/InitiativeRowView.cs)**:
    Элемент списка инициативы, отображающий аватар, имя и показатель инициативы участника боя.
*   **[MapCardView.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Runtime/UI/MapCardView.cs)** и **[TokenCardView.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Runtime/UI/TokenCardView.cs)**:
    Компоненты карточек предварительного просмотра карт и токенов в списках выбора.
*   **[TokenWorldBarsView.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Runtime/UI/TokenWorldBarsView.cs)**:
    Связующее звено между UI-барами здоровья на сцене и изменениями данных.

### 2.6 UI (Главное Меню)
Скрипты управления стартовым интерфейсом игры.

*   **[MainMenuController.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/UI/MainMenuController.cs)**:
    Главный диспетчер переключения экранов в стартовом меню (выбор кампании, переход к редактору карт, создание персонажей, выход).
*   **[MainMenuButton.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/UI/MainMenuButton.cs)**:
    Универсальный компонент для UI-кнопки меню, автоматически связывающий действие `MainMenuAction` с контроллером.
*   **[MainMenuAction.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/UI/MainMenuAction.cs)**:
    Enum со списком действий меню (старт, создание карты, создание токена и др.).
*   **[MainMenuPlayerViewManager.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/UI/MainMenuPlayerViewManager.cs)**:
    Управляет изображением на втором мониторе, пока игра находится в главном меню (выводит красивую заставку или логотип).
*   **[MainMenuFitToSafeArea.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/UI/MainMenuFitToSafeArea.cs)**:
    Утилита адаптации UI под безопасные зоны (Safe Area) экранов (например, вырезы на телефонах или рамки телевизоров).
*   **[PrototypeHud.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/UI/PrototypeHud.cs)**:
    Черновой отладочный интерфейс для вывода логов и инициативы в самых ранних прототипах.

### 2.7 MapEditor (Редактор Карт и Кампаний)
Инструменты создания графа приключения (узлы-карты) и наполнения отдельных карт объектами.

*   **[CampaignEditorController.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/MapEditor/CampaignEditorController.cs)**:
    Основной скрипт редактора кампаний. Позволяет создавать ноды карт, соединять их линиями (переходами), редактировать свойства связей и сохранять граф кампании в JSON.
*   **[CampaignEditorDialog.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/MapEditor/CampaignEditorDialog.cs)**:
    Диалоговое окно настройки параметров карты в редакторе (выбор фонового изображения, название, тип ноды).
*   **[CampaignMapNode.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/MapEditor/CampaignMapNode.cs)**:
    GameObject-представление узла кампании на экране редактора. Содержит визуальное превью карты и обрабатывает клики и перетаскивание узла.
*   **[CampaignLinkView.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/MapEditor/CampaignLinkView.cs)**:
    Рисует стрелки и линии связи между узлами кампании.
*   **[CampaignExitPin.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/MapEditor/CampaignExitPin.cs)**:
    Управляет точками выхода (Exit Pins) на узле карты кампании, к которым привязываются переходы.
*   **[CampaignBoardPanZoom.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/MapEditor/CampaignBoardPanZoom.cs)**:
    Позволяет перемещать камеру и масштабировать рабочее поле редактора кампании.
*   **[MapEditorElementPalette.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/MapEditor/MapEditorElementPalette.cs)**:
    Панель выбора импортированных ассетов (изображений) для размещения на боевой карте. Управляет также сохранением/загрузкой файлов карт.
*   **[MapEditorElementSpawner.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/MapEditor/MapEditorElementSpawner.cs)**:
    Создает объект на карте при выборе его в палитре и клике на сетку.
*   **[PlacedMapElement.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/MapEditor/PlacedMapElement.cs)**:
    Помещается на каждый объект, установленный на карте. Отвечает за его перемещение, поворот и масштабирование мышью.
*   **[UserElementAssetStore.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/MapEditor/UserElementAssetStore.cs)**:
    Управляет импортом пользовательских PNG/JPG файлов, сохраняя их в папку `Application.persistentDataPath/RPGTable/UserElements` и загружая их как Sprite. В этом же файле объявлен статический класс **`UserMapStore`** для сериализации/десериализации карт в JSON.

### 2.8 CharacterEditor (Редактор Персонажей)
Конструктор карточек героев и монстров.

*   **[CharacterEditorController.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/CharacterEditor/CharacterEditorController.cs)**:
    Главный класс редактора персонажей. Управляет распределением характеристик, выбором портрета, привязкой способностей и предметов к персонажу.
*   **[CharacterEditorDialog.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/CharacterEditor/CharacterEditorDialog.cs)**:
    Вспомогательные всплывающие окна и диалоги редактора.
*   **[AbilityDragItem.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/CharacterEditor/AbilityDragItem.cs)**, **[AbilityDropSlot.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/CharacterEditor/AbilityDropSlot.cs)**, **[AbilitySelectDialog.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/CharacterEditor/AbilitySelectDialog.cs)**:
    Реализуют drag-and-drop интерфейс для перетаскивания карт способностей из общего пула в слоты способностей персонажа.
*   **[ItemDragItem.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/CharacterEditor/ItemDragItem.cs)**, **[ItemDropSlot.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/CharacterEditor/ItemDropSlot.cs)**, **[ItemSelectDialog.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/CharacterEditor/ItemSelectDialog.cs)**:
    Реализуют аналогичный drag-and-drop интерфейс для добавления предметов экипировки в инвентарь персонажа.
*   **[ItemTooltipTrigger.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/CharacterEditor/ItemTooltipTrigger.cs)** и **[ItemTooltip.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/CharacterEditor/ItemTooltip.cs)**:
    Показывают всплывающие окна с описанием и свойствами предметов при наведении курсора.
*   **[UserCharacterStore.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/CharacterEditor/UserCharacterStore.cs)**:
    Отвечает за сериализацию и чтение созданных персонажей в JSON файлы в папке `Application.persistentDataPath/RPGTable/Characters`.

### 2.9 TokenEditor (Редактор Токенов)
Визуальный конфигуратор круглых/квадратных фишек для поля.

*   **[TokenEditorController.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/TokenEditor/TokenEditorController.cs)**:
    Управляет процессом настройки внешнего вида токена: выбор картинки лица, выбор рамки фракции (рамка игрока, монстра, дружественного NPC) и указание размера занимаемых клеток (footprint).
*   **[UserTokenStore.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/TokenEditor/UserTokenStore.cs)**:
    Сохраняет метаданные токенов (путь к картинке, рамку, размер) в JSON файлы.

### 2.10 Input & GameMaster (Ввод и Навигация)
Классы, отвечающие за трансляцию кликов мыши и клавиатуры в игровые команды.

*   **[MouseCameraController.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Input/MouseCameraController.cs)**:
    Управляет ортографической камерой игрового поля. Позволяет панорамировать карту (зажав среднюю или правую кнопку мыши) и масштабировать вид колесиком.
*   **[TokenDragController.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Input/TokenDragController.cs)**:
    Управляет перетаскиванием токенов по сетке мышью (drag-and-drop) на экране мастера, ограничивая движение рамками поля и подсвечивая путь движения.
*   **[ViewModeController.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/GameMaster/ViewModeController.cs)**:
    Позволяет GM быстро переключаться между режимом просмотра Мастера (показываются скрытые токены и отключен туман войны) и режимом Игрока.

### 2.11 Editor (Скрипты Сборки в Редакторе Unity)
Эти скрипты выполняются только внутри редактора Unity (унаследованы от `Editor` или используют namespace `UnityEditor`) и помогают собирать готовые сцены.

*   **[CampaignUIBuilder.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Editor/CampaignUIBuilder.cs)**: Строит UI-элементы для экрана выбора кампаний.
*   **[RPGTableAbilityCardGenerator.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Editor/RPGTableAbilityCardGenerator.cs)**: Генерирует дефолтные ассеты карт способностей.
*   **[RPGTableCampaignFlowBuilder.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Editor/RPGTableCampaignFlowBuilder.cs)**: Автоматически генерирует сцену редактора и выбора кампаний.
*   **[RPGTableCharacterEditorBuilder.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Editor/RPGTableCharacterEditorBuilder.cs)**: Собирает сложную сцену редактора персонажей с UI-сетками, кнопками и слотами drag-and-drop.
*   **[RPGTableItemCardGenerator.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Editor/RPGTableItemCardGenerator.cs)**: Генерирует ассеты предметов.
*   **[RPGTableMainMenuBuilder.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Editor/RPGTableMainMenuBuilder.cs)**: Автоматически собирает сцену главного меню (расставляет Canvas, кнопки, задний фон и вешает обработчики).
*   **[RPGTableMapEditorBuilder.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Editor/RPGTableMapEditorBuilder.cs)**: Создает сцену редактора карт (большое поле, палитра, кнопки сохранения).
*   **[RPGTablePrefabBuilder.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Editor/RPGTablePrefabBuilder.cs)**: Генерирует необходимые префабы UI и игрового мира.
*   **[RPGTablePrefabCreator.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Editor/RPGTablePrefabCreator.cs)**: Создает базовые префабы токенов и сеток.
*   **[RPGTablePrototypeBuilder.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Editor/RPGTablePrototypeBuilder.cs)**: Собирает прототип сцены игрового стола со всеми необходимыми менеджерами.
*   **[RPGTableTokenEditorBuilder.cs](file:///d:/programs/Unity/UnityProgekt/T-D-master/Assets/RPGTable/Editor/RPGTableTokenEditorBuilder.cs)**: Автоматически собирает сцену редактора токенов.

---

## 3. Потоки данных и Паттерны

### Сериализация и хранилище данных (Persistence):
Все пользовательские данные сохраняются в формате JSON в директорию `Application.persistentDataPath/RPGTable/`:
*   Карты: `/Maps/*.json`
*   Кампании: `/Campaigns/*.json`
*   Персонажи: `/Characters/*.json`
*   Токены: `/Tokens/*.json`
*   Импортированная графика: `/UserElements/*.png`

### Паттерн Command и реактивные события:
Изменение характеристик токенов, ходов в бою или перемещений передается через события. Например:
*   `CampaignGameSession.OnTokenDataChanged` сообщает всем UI-панелям и второму экрану о необходимости обновить HP/броню конкретного токена.
*   `CampaignGameSession.OnPlayersChanged` вызывается при подключении нового игрока по сети, автоматически обновляя список лобби в главном меню или на столе.
*   `WebServerManager` принимает JSON-команды от веб-клиента игрока (например, `MovePayload`, `AttackPayload`), трансформирует их во внутриигровые вызовы `CombatManager` или `CampaignGameSession` и транслирует изменения остальным подключенным веб-клиентам.
