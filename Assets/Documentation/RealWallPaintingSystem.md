# Система покраски реальных стен с использованием компьютерного зрения

## Обзор

Система покраски реальных стен позволяет обнаруживать стены в реальном мире с помощью компьютерного зрения (OpenCV) и применять к ним различные цвета. Это аналог приложения Dulux Visualizer, который позволяет визуализировать, как будет выглядеть стена после покраски.

## Компоненты системы

Система состоит из следующих основных компонентов:

1. **WallDetector** - компонент для обнаружения стен с использованием OpenCV.
2. **WallMeshBuilder** - компонент для создания 3D-моделей стен на основе обнаруженных данных.
3. **RealWallPaintingController** - основной контроллер системы покраски стен.
4. **SimpleColorButton** - компонент для кнопки выбора цвета.
5. **SimpleColorPreview** - компонент для предпросмотра цвета на стене.
6. **RealWallPaintingDemo** - демонстрационный компонент с UI для управления системой.

## Требования

Для работы системы необходимы следующие компоненты:

1. Unity 2019.4 или новее
2. OpenCV for Unity (приобретается отдельно в Asset Store)
3. Веб-камера или подготовленные изображения для обработки

## Настройка системы

### Автоматическая настройка

Самый простой способ настроить систему - использовать инструмент автоматической настройки:

1. В Unity откройте меню **Window > Remalux AR > Setup Real Wall Painting System**
2. Будет создана новая сцена со всеми необходимыми компонентами
3. Сохраните сцену в удобном месте

### Ручная настройка

Если вы хотите настроить систему вручную, выполните следующие шаги:

1. Создайте пустой GameObject и назовите его "RealWallPaintingSystem"
2. Добавьте к нему компоненты:
   - RealWallPaintingController
   - WallDetector
   - WallMeshBuilder
   - RealWallPaintingDemo (опционально)
3. Настройте ссылки между компонентами:
   - Назначьте WallDetector в WallMeshBuilder
   - Назначьте WallMeshBuilder и WallDetector в RealWallPaintingController
4. Создайте UI элементы:
   - Canvas с кнопками выбора цвета
   - Кнопку сброса
   - Отладочное отображение для визуализации обнаружения стен
5. Создайте материалы для покраски стен

## Использование системы

### Обнаружение стен

1. Запустите сцену
2. Направьте камеру на стену
3. Система автоматически обнаружит стены и создаст их 3D-модели
4. Обнаруженные стены будут отображаться в отладочном окне

### Покраска стен

1. После обнаружения стен нажмите кнопку "Включить покраску"
2. Выберите цвет из палитры в нижней части экрана
3. Коснитесь стены, чтобы применить выбранный цвет
4. Для сброса всех стен к исходному цвету нажмите кнопку "Сбросить"

## Настройка параметров

### WallDetector

- **Настройки камеры**:
  - `useWebcam` - использовать веб-камеру (true) или входное изображение (false)
  - `webcamDeviceIndex` - индекс устройства веб-камеры
  - `webcamResolution` - разрешение веб-камеры
  - `inputTexture` - входное изображение (если не используется веб-камера)

- **Настройки обнаружения**:
  - `cannyThreshold1` и `cannyThreshold2` - пороги для детектора границ Canny
  - `minWallWidth` и `minWallHeight` - минимальные размеры стены (в метрах)
  - `verticalAngleThreshold` и `horizontalAngleThreshold` - пороги для классификации линий
  - `lineGroupingThreshold` - порог для группировки близких линий

- **Отладка**:
  - `showDebugVisuals` - показывать отладочную визуализацию

### WallMeshBuilder

- `wallDistance` - расстояние от камеры до стены
- `wallDepth` - толщина создаваемых стен
- `wallExtensionFactor` - коэффициент расширения стен

### RealWallPaintingController

- `raycastDistance` - максимальное расстояние для определения точки покраски
- `showColorPreview` - показывать предпросмотр цвета

## Расширение системы

Система спроектирована модульно, что позволяет легко расширять её функциональность:

1. Для добавления новых цветов создайте дополнительные материалы и добавьте их в массив `paintMaterials` в RealWallPaintingController
2. Для улучшения алгоритма обнаружения стен модифицируйте методы `ProcessFrame` и `DetectWalls` в WallDetector
3. Для изменения внешнего вида UI модифицируйте префабы кнопок и контейнеры

## Устранение неполадок

### Стены не обнаруживаются

- Убедитесь, что камера направлена на хорошо освещенную стену с четкими границами
- Попробуйте настроить параметры `cannyThreshold1` и `cannyThreshold2` в WallDetector
- Проверьте отладочное отображение, чтобы увидеть, как система обрабатывает изображение

### Проблемы с покраской

- Убедитесь, что режим покраски включен
- Проверьте, что материалы для покраски правильно настроены
- Убедитесь, что raycastDistance достаточно большой для достижения стен

## Примечания по производительности

- Обработка изображений с помощью OpenCV может быть ресурсоемкой, особенно на мобильных устройствах
- Для улучшения производительности можно:
  - Уменьшить разрешение обрабатываемого изображения
  - Уменьшить частоту обновления обнаружения стен
  - Оптимизировать алгоритмы обнаружения линий и стен

---

# Real Wall Painting System using Computer Vision

## Overview

The Real Wall Painting System allows detecting walls in the real world using computer vision (OpenCV) and applying various colors to them. It's similar to the Dulux Visualizer app, which lets you visualize how a wall would look after painting.

## System Components

The system consists of the following main components:

1. **WallDetector** - component for detecting walls using OpenCV.
2. **WallMeshBuilder** - component for creating 3D models of walls based on detected data.
3. **RealWallPaintingController** - main controller for the wall painting system.
4. **SimpleColorButton** - component for color selection button.
5. **SimpleColorPreview** - component for color preview on the wall.
6. **RealWallPaintingDemo** - demonstration component with UI for system control.

## Requirements

The system requires the following components:

1. Unity 2019.4 or newer
2. OpenCV for Unity (purchased separately from the Asset Store)
3. Webcam or prepared images for processing

## System Setup

### Automatic Setup

The easiest way to set up the system is to use the automatic setup tool:

1. In Unity, open the menu **Window > Remalux AR > Setup Real Wall Painting System**
2. A new scene will be created with all necessary components
3. Save the scene in a convenient location

### Manual Setup

If you want to set up the system manually, follow these steps:

1. Create an empty GameObject and name it "RealWallPaintingSystem"
2. Add the following components:
   - RealWallPaintingController
   - WallDetector
   - WallMeshBuilder
   - RealWallPaintingDemo (optional)
3. Set up references between components:
   - Assign WallDetector to WallMeshBuilder
   - Assign WallMeshBuilder and WallDetector to RealWallPaintingController
4. Create UI elements:
   - Canvas with color selection buttons
   - Reset button
   - Debug display for wall detection visualization
5. Create materials for wall painting

## Using the System

### Wall Detection

1. Run the scene
2. Point the camera at a wall
3. The system will automatically detect walls and create 3D models
4. Detected walls will be displayed in the debug window

### Wall Painting

1. After walls are detected, press the "Enable Painting" button
2. Select a color from the palette at the bottom of the screen
3. Touch a wall to apply the selected color
4. To reset all walls to their original color, press the "Reset" button

## Parameter Configuration

### WallDetector

- **Camera Settings**:
  - `useWebcam` - use webcam (true) or input texture (false)
  - `webcamDeviceIndex` - webcam device index
  - `webcamResolution` - webcam resolution
  - `inputTexture` - input texture (if not using webcam)

- **Detection Settings**:
  - `cannyThreshold1` and `cannyThreshold2` - thresholds for Canny edge detector
  - `minWallWidth` and `minWallHeight` - minimum wall dimensions (in meters)
  - `verticalAngleThreshold` and `horizontalAngleThreshold` - thresholds for line classification
  - `lineGroupingThreshold` - threshold for grouping close lines

- **Debug**:
  - `showDebugVisuals` - show debug visualization

### WallMeshBuilder

- `wallDistance` - distance from camera to wall
- `wallDepth` - thickness of created walls
- `wallExtensionFactor` - wall extension factor

### RealWallPaintingController

- `raycastDistance` - maximum distance for determining painting point
- `showColorPreview` - show color preview

## Extending the System

The system is designed modularly, making it easy to extend its functionality:

1. To add new colors, create additional materials and add them to the `paintMaterials` array in RealWallPaintingController
2. To improve the wall detection algorithm, modify the `ProcessFrame` and `DetectWalls` methods in WallDetector
3. To change the UI appearance, modify button prefabs and containers

## Troubleshooting

### Walls are not detected

- Make sure the camera is pointed at a well-lit wall with clear boundaries
- Try adjusting the `cannyThreshold1` and `cannyThreshold2` parameters in WallDetector
- Check the debug display to see how the system processes the image

### Painting issues

- Make sure painting mode is enabled
- Check that painting materials are properly configured
- Ensure raycastDistance is large enough to reach the walls

## Performance Notes

- Image processing with OpenCV can be resource-intensive, especially on mobile devices
- To improve performance, you can:
  - Reduce the resolution of the processed image
  - Decrease the frequency of wall detection updates
  - Optimize line and wall detection algorithms 