# Происхождение ассетов

- В локальном проекте используются модели, текстуры и анимации Broodmother, Shadow Fiend, Templar Assassin, Pudge, Spirit Breaker и Nature's Prophet из установленной пользователем Dota 2. Источник: `game/dota/pak01_dir.vpk`. Экспорт через [Source 2 Viewer / ValveResourceFormat](https://github.com/ValveResourceFormat/ValveResourceFormat), конвертация glTF в FBX через Blender. Каталог `Assets/Arena/Resources/DotaImported` исключён из Git; права на исходные ассеты сохраняются за их владельцами.
- `tools/export-dota-heroes.ps1` и `tools/convert-dota-heroes.py` сохраняют шаги локального переноса. `DotaArt.cs` собирает префабы и материалы, `ImportedHeroMotion.cs` переключает скелетные анимации.
- Собственные параметрические модели в `HeroArt.cs` и `Resources/Heroes` оставлены как резерв для проекта без локального экспорта. Движение и атаки резервных моделей выполняет `HeroMotion.cs`.
- Модели окружения, башен, крипов, Рошана и курьера строятся из Unity primitives и собственных meshes в `WorldView.cs`.
- Иконки созданы встроенным imagegen: 36 предметов в `Resources/Art/Items.png` и 28 способностей/улучшений в `Resources/Art/Abilities.png`. Unity импортирует их как отдельные sprites; интерфейс использует атласные UV. Промпты сохранены в `ART_PROMPTS.md`.
- Рельеф, карта, миникарта, шейдеры поверхности, героев и тумана войны созданы в этом проекте.
- Звуки кнопок, попаданий, заклинаний и смерти синтезируются программно в `ArenaApp.cs`; внешние аудиофайлы не используются.
- Шрифт — встроенный Unity LegacyRuntime.ttf.
- Платные ассеты и генерация через fal не использовались.
- Названия и идеи игровых механик отсылают к Dota 2; принадлежность этих обозначений не меняется.
