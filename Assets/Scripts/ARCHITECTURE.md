# Fotocentr — архитектура проекта

## SOLID и паттерны

### Single Responsibility (SRP)
- **DecalLayerDragHandler** — только логика перемещения слоя
- **DecalCenterDragZone** — только делегирование события перетаскивания
- **CornerResizeStrategy** — только расчёт resize для своего угла

### Open/Closed (OCP)
- **ICornerResizeStrategy** — новые углы добавляются без изменения существующего кода
- **ISelectionStrategy**, **ILayerOrderStrategy** — можно менять стратегии через конфигурацию

### Liskov Substitution (LSP)
- **ShaderBasedVisualStrategy** использует только `IDecalLayer`, без приведения к `UIDecalLayer`
- **DecalCenterDragZone** работает с `IDragTarget`, а не с конкретным `UIDecalLayer`

### Interface Segregation (ISP)
- **IDragTarget** — минимальный контракт: `HandlePointerDrag`
- **ISceneCapture** — только методы съёмки
- **IDecal** — интерфейс 3D-декали (для будущего использования)

### Dependency Inversion (DIP)
- **CompositionRoot** — точка внедрения зависимостей
- **DecalEditPanel** получает `DecalManager` и `ISceneCapture` через `Inject()`
- `FindObjectOfType` остаётся fallback, но предпочтительно связывание через Inspector

---

## Структура

```
Assets/Scripts/
├── Core/                     # Ядро, интерфейсы, DI
│   ├── Interfaces/
│   │   ├── IDecal.cs
│   │   ├── IDragTarget.cs
│   │   ├── ISceneCapture.cs
│   │   └── IDecalEditorDependencies.cs
│   └── CompositionRoot.cs
│
├── Decal/                    # 3D-декали
│   ├── DecalController.cs
│   ├── DecalManager.cs
│   ├── DecalEditPanel.cs
│   ├── DecalFactory.cs
│   └── Services/DecalTransformService.cs
│
├── DecalSystem/              # UI и логика декалей
│   ├── CornerResize/         # Strategy для углов
│   ├── PreviewSystem/        # Окно превью, слои
│   └── UI/                   # DecalCenterDragZone, DecalLayerDragHandler
│
├── Interfaces/IDecalEditor.cs
├── Model/                    # 3D-модели, окрашивание
├── UI/                       # Панели, ColorPicker
├── Viewport/                 # Камера, съёмка
└── TextureLoad/
```

---

## CompositionRoot

Добавьте на сцену GameObject с компонентом **CompositionRoot**:
- Укажите **DecalManager** и **SceneCaptureService**
- Укажите **DecalEditPanel** в Consumers

Зависимости будут внедрены в `Awake` (выполняется до `Start`).

---

## Удалено

- `DecalRepository`, `DecalPlacementService`, `DecalSelectionService`, `DecalData` — не использовались
