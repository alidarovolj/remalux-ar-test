#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
Скрипт для экспорта модели DeepLabv3 в формат ONNX (opset 11),
совместимый с Unity Barracuda.
"""

import os
import torch
import torchvision
import onnx

def export_deeplabv3_to_onnx(output_path, image_size=(224, 224), opset_version=11):
    """
    Экспортирует модель DeepLabv3 в формат ONNX с указанным opset_version.
    
    Args:
        output_path (str): Путь для сохранения модели ONNX
        image_size (tuple): Размер входного изображения (ширина, высота)
        opset_version (int): Версия opset для ONNX (для Barracuda рекомендуется 11 или ниже)
    """
    print(f"Загрузка предобученной модели DeepLabv3...")
    
    # Загружаем модель DeepLabv3 с предобученными весами
    model = torchvision.models.segmentation.deeplabv3_resnet50(pretrained=True)
    model.eval()  # Переключаем модель в режим оценки
    
    print(f"Модель загружена. Подготовка к экспорту ONNX...")
    
    # Создаем фиктивный входной тензор нужного размера
    # Batch size=1, 3 канала (RGB), размер изображения
    dummy_input = torch.randn(1, 3, image_size[1], image_size[0])
    
    # Имена входного и выходного слоев
    input_names = ["input"]
    output_names = ["output"]
    
    # Информация о динамических размерах
    dynamic_axes = {
        'input': {0: 'batch_size', 2: 'height', 3: 'width'},
        'output': {0: 'batch_size', 2: 'height', 3: 'width'}
    }
    
    # Экспортируем модель в формат ONNX с указанным opset_version
    torch.onnx.export(
        model,                      # Модель для экспорта
        dummy_input,                # Фиктивный входной тензор
        output_path,                # Путь для сохранения
        export_params=True,         # Сохранять веса модели
        opset_version=opset_version,# Версия opset (для Barracuda <= 11)
        do_constant_folding=True,   # Оптимизация констант
        input_names=input_names,    # Имена входных слоев
        output_names=output_names,  # Имена выходных слоев
        dynamic_axes=dynamic_axes,  # Динамические размеры
        verbose=True               # Подробный вывод
    )
    
    print(f"Модель экспортирована в {output_path}")
    
    # Проверяем корректность модели
    onnx_model = onnx.load(output_path)
    onnx.checker.check_model(onnx_model)
    
    print(f"Модель проверена и корректна.")
    print(f"\nДля использования в Unity Barracuda:")
    print(f"1. Импортируйте файл {output_path} в Unity")
    print(f"2. Unity автоматически конвертирует его в формат NNModel")
    print(f"3. Подключите модель к компоненту DeepLabDecoder")

def export_deeplabv3_mobilenet(output_path, image_size=(224, 224), opset_version=11):
    """
    Экспортирует DeepLabv3 с MobileNetV3 в формат ONNX для мобильных устройств.
    
    Args:
        output_path (str): Путь для сохранения модели ONNX
        image_size (tuple): Размер входного изображения (ширина, высота)
        opset_version (int): Версия opset для ONNX (для Barracuda рекомендуется 11 или ниже)
    """
    print(f"Загрузка предобученной модели DeepLabv3 с MobileNetV3...")
    
    # Загружаем модель DeepLabv3 с MobileNetV3 для более эффективной работы на мобильных устройствах
    model = torchvision.models.segmentation.deeplabv3_mobilenet_v3_large(pretrained=True)
    model.eval()  # Переключаем модель в режим оценки
    
    print(f"Модель загружена. Подготовка к экспорту ONNX...")
    
    # Создаем фиктивный входной тензор нужного размера
    dummy_input = torch.randn(1, 3, image_size[1], image_size[0])
    
    # Имена входного и выходного слоев
    input_names = ["input"]
    output_names = ["output"]
    
    # Информация о динамических размерах
    dynamic_axes = {
        'input': {0: 'batch_size', 2: 'height', 3: 'width'},
        'output': {0: 'batch_size', 2: 'height', 3: 'width'}
    }
    
    # Экспортируем модель в формат ONNX с указанным opset_version
    torch.onnx.export(
        model,                      # Модель для экспорта
        dummy_input,                # Фиктивный входной тензор
        output_path,                # Путь для сохранения
        export_params=True,         # Сохранять веса модели
        opset_version=opset_version,# Версия opset (для Barracuda <= 11)
        do_constant_folding=True,   # Оптимизация констант
        input_names=input_names,    # Имена входных слоев
        output_names=output_names,  # Имена выходных слоев
        dynamic_axes=dynamic_axes,  # Динамические размеры
        verbose=True               # Подробный вывод
    )
    
    print(f"Модель экспортирована в {output_path}")
    
    # Проверяем корректность модели
    onnx_model = onnx.load(output_path)
    onnx.checker.check_model(onnx_model)
    
    print(f"Модель проверена и корректна.")

if __name__ == "__main__":
    # Определяем пути для сохранения моделей
    script_dir = os.path.dirname(os.path.abspath(__file__))
    models_dir = os.path.join(script_dir, "..", "..", "Resources", "Models")
    
    # Создаем директорию, если она не существует
    os.makedirs(models_dir, exist_ok=True)
    
    # Пути для сохранения моделей
    deeplabv3_path = os.path.join(models_dir, "deeplabv3_resnet50.onnx")
    mobilenet_path = os.path.join(models_dir, "deeplabv3_mobilenet.onnx")
    
    # Размер входного изображения
    image_size = (224, 224)  # (ширина, высота)
    
    # Версия opset для ONNX - Barracuda поддерживает до 11 версии
    opset_version = 11
    
    # Экспортируем модели
    print("\n=== Экспорт DeepLabv3 с ResNet50 ===")
    export_deeplabv3_to_onnx(deeplabv3_path, image_size, opset_version)
    
    print("\n=== Экспорт DeepLabv3 с MobileNetV3 ===")
    export_deeplabv3_mobilenet(mobilenet_path, image_size, opset_version)
    
    print("\n=== Экспорт завершен ===")
    print(f"Модели сохранены в каталог: {models_dir}")
    print(f"- DeepLabv3 с ResNet50: {deeplabv3_path}")
    print(f"- DeepLabv3 с MobileNetV3: {mobilenet_path}") 