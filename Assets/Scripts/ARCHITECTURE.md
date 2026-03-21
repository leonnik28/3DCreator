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
│   │   ├── IDragTarget.cs
│   │   ├── ISceneCapture.cs
│   │   └── IDecalEditorDependencies.cs
│   └── CompositionRoot.cs
│
├── Decal/                    # Вся подсистема декалей
│   ├── Runtime/              # 3D decal runtime (manager/controller/projector)
│   ├── UI/                   # UI-редактор и контролы трансформации
│   ├── Preview/              # Preview window, layers, visual strategies
│   ├── Resize/               # Стратегии resize по углам
│   ├── Factories/            # Фабрики декалей
│   └── Services/             # Сервисы преобразования 2D->3D
│
├── Model/                    # 3D-модели, окрашивание
├── UI/                       # Панели, ColorPicker
│   └── Controllers/          # UI flow controllers
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
- legacy-блок стратегий: `PreviewWindowStrategyFactory`, `AspectRatioSizeStrategy`, `WorldToUIPositioningStrategy`, `ImageDragStrategy`, `PreviewWindowValidator`
- неиспользуемые интерфейсы: `IDecal`, `IImagePositioningStrategy`, `IImageSizeStrategy`, `IDragHandlerStrategy`, `IStateValidator`
- неиспользуемые компоненты: `DecalUIEditor`, `UIDecalEditorPanel`, `MugCanvasBinder`
