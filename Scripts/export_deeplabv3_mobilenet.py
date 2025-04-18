import torch
import torchvision
from torchvision.models.segmentation import deeplabv3_mobilenet_v3_large
import os
import numpy as np
import argparse

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

def optimize_model(input_path, output_path, quantize=True, simplify=True):
    """
    Оптимизирует модель ONNX для мобильных устройств
    
    Args:
        input_path (str): Путь к исходной модели ONNX
        output_path (str): Путь сохранения оптимизированной модели
        quantize (bool): Применять ли квантизацию
        simplify (bool): Применять ли упрощение графа
    """
    try:
        import onnx
        from onnxoptimizer import optimize
        
        # Загружаем модель
        print(f"Загрузка модели для оптимизации: {input_path}")
        model = onnx.load(input_path)
        
        # Проверяем корректность модели
        onnx.checker.check_model(model)
        
        # Оптимизация графа
        if simplify:
            try:
                from onnxsim import simplify
                print("Упрощение графа модели...")
                model_simplified, check = simplify(model)
                if check:
                    model = model_simplified
                    print("Граф модели упрощен")
                else:
                    print("Не удалось упростить модель")
            except ImportError:
                print("Библиотека onnxsim не установлена. Упрощение пропускается.")
        
        # Оптимизация средствами onnxoptimizer
        print("Оптимизация модели...")
        passes = ['eliminate_identity', 'eliminate_nop_transpose', 
                 'fuse_consecutive_transposes', 'fuse_transpose_into_gemm']
        optimized_model = optimize(model, passes)
        
        # Квантизация для уменьшения размера
        if quantize:
            try:
                from onnxruntime.quantization import quantize_dynamic
                print("Применение квантизации...")
                quantize_dynamic(input_path,
                                output_path,
                                weight_type=onnx.TensorProto.FLOAT16)
                print(f"Модель квантизирована и сохранена: {output_path}")
                
                # Проверяем размер файла
                file_size_mb = os.path.getsize(output_path) / (1024 * 1024)
                print(f"Размер оптимизированной модели: {file_size_mb:.2f} MB")
                return True
            except ImportError:
                print("Библиотека onnxruntime не установлена. Квантизация пропускается.")
                # Если квантизация не удалась, сохраняем просто оптимизированную модель
                onnx.save(optimized_model, output_path)
        else:
            # Сохраняем оптимизированную (но не квантизированную) модель
            onnx.save(optimized_model, output_path)
            print(f"Модель оптимизирована и сохранена: {output_path}")
            
            # Проверяем размер файла
            file_size_mb = os.path.getsize(output_path) / (1024 * 1024)
            print(f"Размер оптимизированной модели: {file_size_mb:.2f} MB")
        
        return True
        
    except ImportError as e:
        print(f"Ошибка импорта необходимых библиотек: {e}")
        print("Для оптимизации требуются: onnx, onnxoptimizer, onnxruntime (опционально), onnxsim (опционально)")
        return False
    except Exception as e:
        print(f"Ошибка оптимизации модели: {e}")
        return False

def main():
    # Парсинг аргументов командной строки
    parser = argparse.ArgumentParser(description='Экспорт DeepLabv3 MobileNet в ONNX формат для Unity Barracuda')
    parser.add_argument('--output', type=str, default='Assets/Models/deeplab_mobilenet.onnx', 
                      help='Путь сохранения ONNX модели')
    parser.add_argument('--optimized_output', type=str, default='Assets/Models/deeplab_mobilenet_optimized.onnx', 
                      help='Путь сохранения оптимизированной ONNX модели')
    parser.add_argument('--input_size', type=int, default=320, 
                      help='Размер входного изображения (квадратного)')
    parser.add_argument('--opset_version', type=int, default=11, 
                      help='Версия ONNX opset (должна быть 11 или ниже для Unity Barracuda)')
    parser.add_argument('--optimize', action='store_true', 
                      help='Оптимизировать модель после экспорта')
    parser.add_argument('--quantize', action='store_true', 
                      help='Применить квантизацию для уменьшения размера модели')
    
    args = parser.parse_args()
    
    # Экспорт модели в ONNX
    success = export_deeplabv3_mobilenet(
        output_path=args.output,
        input_size=args.input_size,
        opset_version=args.opset_version
    )
    
    # Оптимизация модели
    if success and args.optimize:
        optimize_model(
            input_path=args.output,
            output_path=args.optimized_output,
            quantize=args.quantize
        )

if __name__ == "__main__":
    main() 