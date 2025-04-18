# DeepLabV3 Model Export для Unity Barracuda

Этот каталог содержит скрипты для экспорта предварительно обученной модели DeepLabV3 MobileNet для использования с Unity Barracuda.

## Доступные скрипты

1. `torch_script_export.py` - Экспортирует модель в формат TorchScript
2. `torch_onnx_export.py` - Экспортирует модель в формат ONNX напрямую через PyTorch

## Требования

- Python 3.7+
- PyTorch 1.8+
- torchvision

## Установка зависимостей

```bash
pip install torch torchvision
```

## Инструкции по запуску

### Экспорт в TorchScript (рекомендуется)

```bash
python torch_script_export.py
```

Это создаст файлы:
- `../Assets/Models/deeplabv3_mobilenet.pt` - Стандартная модель TorchScript
- `../Assets/Models/deeplabv3_mobilenet_mobile.pt` - Оптимизированная для мобильных устройств модель

### Экспорт в ONNX

```bash
python torch_onnx_export.py
```

Это создаст файл:
- `../Assets/Models/deeplabv3_mobilenet.onnx` - Модель в формате ONNX

## Интеграция в Unity

### Импорт модели

1. Запустите один из скриптов экспорта
2. Убедитесь, что модель появилась в директории `Assets/Models/`
3. Откройте Unity и дождитесь импорта модели

### Использование модели с DeepLabDecoder

1. В редакторе Unity найдите объект с компонентом `DeepLabDecoder`
2. В инспекторе найдите поле `Model Asset`
3. Перетащите файл модели (`deeplabv3_mobilenet.onnx` или `deeplabv3_mobilenet.pt`) в это поле
4. Для TorchScript модели убедитесь, что выбран правильный Input Name и Output Name

## Производительность

- TorchScript модель обычно работает быстрее чем ONNX модель на мобильных устройствах
- Используйте `DeepLabModelTester` для сравнения производительности различных моделей
- Рекомендуемое разрешение входного изображения для мобильных устройств: 320x320

## Характеристики моделей

| Модель | Размер входа | Размер файла | FPS на iPhone 12 | FPS на среднем Android |
|--------|-------------|--------------|------------------|-------------------------|
| DeepLabv3 ResNet | 320x320 | ~90MB | ~5-8 | ~3-5 |
| DeepLabv3 MobileNetV3 | 320x320 | ~20MB | ~12-15 | ~8-10 |
| DeepLabv3 MobileNetV3 (квантизированная) | 320x320 | ~10MB | ~15-20 | ~10-12 |
| DeepLabv3 MobileNetV3 | 224x224 | ~20MB | ~18-25 | ~12-15 | 