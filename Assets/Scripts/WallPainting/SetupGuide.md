# Руководство по настройке AR сцены для покраски стен

## Подготовка проекта

### 1. Установка необходимых пакетов
В Package Manager (Window > Package Manager) установите следующие пакеты:
- AR Foundation (минимум версии 4.1)
- ARKit XR Plugin (для iOS) или ARCore XR Plugin (для Android)
- Unity Barracuda (версия 3.0.0 или выше)
- TextMeshPro (для UI элементов)
- OpenCVForUnity (опционально, установите из Asset Store если нужна расширенная обработка)

### 2. Настройка XR плагинов
1. Откройте Project Settings > XR Plugin Management
2. Включите поддержку ARKit (iOS) или ARCore (Android)
3. В настройках ARKit/ARCore убедитесь, что включены опции:
   - Plane Detection
   - Point Clouds
   - Anchors
   - World Map (для ARKit)

### 3. Подготовка DeepLabv3 модели
1. Запустите Python скрипт для экспорта модели:
   ```
   python Assets/Scripts/Utils/export_deeplabv3_onnx.py
   ```
2. После завершения экспорта, в Unity выберите в меню Tools > Convert ONNX to NNModel
3. Выберите файл deeplabv3_mobilenet.onnx из Assets/Resources/Models
4. Убедитесь, что модель успешно сконвертирована (проверьте консоль)

## Создание AR сцены

### 1. Базовая структура AR сцены
1. Создайте новую сцену: File > New Scene
2. Добавьте объекты AR Foundation:
   - AR Session (GameObject > XR > AR Session)
   - AR Session Origin (GameObject > XR > AR Session Origin)
3. Настройте AR Session Origin:
   - Убедитесь, что AR Camera добавлена
   - Добавьте компоненты AR Plane Manager, AR Raycast Manager, AR Anchor Manager

### 2. Настройка AR камеры
1. Выберите AR Camera в AR Session Origin
2. Убедитесь, что у камеры включены:
   - AR Pose Driver
   - AR Camera Manager
   - AR Camera Background
3. Создайте для камеры RenderTexture:
   - Assets > Create > Render Texture
   - Назовите ее "ARCameraTexture", установите размер 1024x1024
   - Установите Target Texture у AR Camera на эту текстуру

### 3. Добавление компонентов для обработки стен
1. Создайте пустой объект WallProcessor
2. Добавьте к WallProcessor компоненты:
   - DeepLabDecoder
   - WallPainter
   - ARWorldMapController
3. Настройте DeepLabDecoder:
   - Перетащите модель (Assets/Resources/Models/deeplabv3_mobilenet.asset) в поле Model Asset
   - Установите Wall Class Index = 12 (индекс класса "стена" в COCO)
   - Укажите AR Camera Manager из AR Camera
   - Создайте материал SegmentationMaterial (Shader: Unlit/Transparent)
   - Перетащите материал в поле Output Material
4. Настройте WallPainter:
   - Перетащите AR Camera, AR Raycast Manager, AR Anchor Manager и AR Plane Manager
   - Перетащите DeepLabDecoder в соответствующее поле
   - Создайте префаб для покрашенной стены (см. ниже)
   - Установите Paint Distance (рекомендуется 5-10 метров)
5. Настройте ARWorldMapController:
   - Перетащите AR Session и AR Session Origin

### 4. Создание префаба покрашенной стены
1. Создайте пустой GameObject
2. Добавьте компонент Quad с MeshRenderer
3. Создайте материал WallPaintMaterial:
   - Shader: Transparent/Diffuse или подобный
   - Установите Color и Transparency
4. Присвойте материал к Quad
5. Добавьте компонент PaintedWall
6. Перетащите MeshRenderer в поле Wall Renderer
7. Настройте Alpha и Fade In параметры
8. Создайте префаб: перетащите объект в папку Prefabs

### 5. Создание UI
1. Создайте Canvas (GameObject > UI > Canvas)
2. Настройте Canvas на "Screen Space - Camera" и укажите AR Camera
3. Добавьте к Canvas компоненты:
   - Canvas Scaler (установите UI Scale Mode = Scale With Screen Size)
   - Graphic Raycaster
4. Создайте UI элементы:
   - Панель управления (кнопки, переключатели)
   - Панель цветов
   - Текст статуса
   - Панель настроек
5. Добавьте к Canvas компонент WallPaintingUIController
6. Настройте все ссылки на UI элементы и компоненты в инспекторе

## Финальные настройки и тестирование

### 1. Настройки сборки
1. File > Build Settings
2. Выберите iOS или Android
3. В Player Settings:
   - Для iOS: требуется минимум iOS 11.0, отключите Metal API Validation
   - Для Android: требуется минимум Android 7.0 (API Level 24), включите Auto Graphics API

### 2. Настройки качества
1. Project Settings > Quality
2. Оптимизируйте настройки для мобильных устройств:
   - Отключите Anti Aliasing
   - Уменьшите Shadow Distance и Shadow Resolution
   - Установите Texture Quality = Half Resolution

### 3. Тестирование
1. Подключите устройство по USB
2. Нажмите Build and Run
3. После запуска направьте камеру на стену
4. Плоскости должны автоматически определиться
5. Нажмите на экран, чтобы покрасить стену
6. Используйте UI для изменения цвета и настроек
7. Проверьте функции сохранения и загрузки

### Примечания по отладке
- Если DeepLabv3 не срабатывает, проверьте консоль Unity на ошибки
- Используйте Debug.Log для отслеживания ключевых событий
- Если есть проблемы с производительностью:
  - Уменьшите размер входного изображения для DeepLabv3
  - Оптимизируйте постобработку маски
  - Используйте более легкую модель (MobileNet вместо ResNet) 