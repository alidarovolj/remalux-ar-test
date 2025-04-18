import torch
import torch.nn as nn
import torchvision
from torchvision.models.segmentation import deeplabv3_mobilenet_v3_large
import os
import sys

def export_deeplabv3_mobilenet(output_path="Assets/Models/deeplab_mobilenet.onnx", 
                              input_size=320, 
                              opset_version=11,
                              pretrained=True):
    """
    Экспортирует предобученную модель DeepLabv3 с MobileNetV3 бэкбоном в формат ONNX.
    
    Args:
        output_path (str): Путь сохранения модели в формате ONNX
        input_size (int): Размер входного изображения (квадратного)
        opset_version (int): Версия ONNX opset (должна быть 11 или ниже для Unity Barracuda)
        pretrained (bool): Использовать предобученные веса или нет
    """
    print(f"Загрузка DeepLabv3 MobileNetV3 Large (pretrained={pretrained})...")
    
    # Загружаем предобученную модель
    model = deeplabv3_mobilenet_v3_large(pretrained=pretrained, progress=True)
    model.eval()  # Переключаем модель в режим оценки

    # Создаем директорию для модели если нужно
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    
    # Генерируем случайный входной тензор для трассировки модели
    dummy_input = torch.randn(1, 3, input_size, input_size)
    
    # Задаем имена входного и выходного тензора
    input_names = ["input"]
    output_names = ["output"]
    
    print(f"Экспорт модели в формат ONNX (opset_version={opset_version})...")
    torch.onnx.export(model, 
                      dummy_input, 
                      output_path,
                      verbose=True,
                      opset_version=opset_version,
                      input_names=input_names,
                      output_names=output_names,
                      export_params=True,
                      do_constant_folding=True)
    
    print(f"Модель успешно экспортирована: {output_path}")
    
    # Проверяем размер файла
    file_size_mb = os.path.getsize(output_path) / (1024 * 1024)
    print(f"Размер модели: {file_size_mb:.2f} MB")
    
    return True

if __name__ == "__main__":
    # Определяем путь для сохранения
    output_path = "Assets/Models/deeplab_mobilenet.onnx"
    if len(sys.argv) > 1:
        output_path = sys.argv[1]
    
    # Определяем размер входа
    input_size = 320
    if len(sys.argv) > 2:
        input_size = int(sys.argv[2])
    
    # Экспортируем модель
    export_deeplabv3_mobilenet(
        output_path=output_path,
        input_size=input_size,
        opset_version=11  # Важно использовать opset 11 для совместимости с Barracuda
    ) 