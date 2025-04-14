from flask import Flask, request, send_file
import torch
import torchvision
from torchvision.models.segmentation import deeplabv3_resnet101
import numpy as np
from PIL import Image
import io
import cv2

app = Flask(__name__)

# Загружаем предобученную модель DeepLabV3+
model = deeplabv3_resnet101(pretrained=True)
model.eval()

# Если есть GPU, используем его
device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
model = model.to(device)

def preprocess_image(image_bytes):
    # Загружаем изображение
    image = Image.open(io.BytesIO(image_bytes))
    
    # Преобразуем в тензор
    preprocess = torchvision.transforms.Compose([
        torchvision.transforms.ToTensor(),
        torchvision.transforms.Normalize(
            mean=[0.485, 0.456, 0.406],
            std=[0.229, 0.224, 0.225]
        )
    ])
    
    input_tensor = preprocess(image)
    input_batch = input_tensor.unsqueeze(0)
    
    return input_batch.to(device)

def process_output(output):
    # Получаем маску сегментации
    output_predictions = output['out'][0]
    output_predictions = output_predictions.argmax(0)
    
    # Конвертируем в numpy array
    mask = output_predictions.byte().cpu().numpy()
    
    # Создаем цветную маску (стены = белый, остальное = черный)
    colored_mask = np.zeros((mask.shape[0], mask.shape[1], 3), dtype=np.uint8)
    # Класс 12 в DeepLabV3+ = стены
    colored_mask[mask == 12] = [255, 255, 255]
    
    return colored_mask

@app.route('/segment', methods=['POST'])
def segment_image():
    # Получаем изображение из запроса
    image_bytes = request.get_data()
    
    # Предобработка
    input_batch = preprocess_image(image_bytes)
    
    with torch.no_grad():
        output = model(input_batch)
    
    # Обработка результата
    segmentation_mask = process_output(output)
    
    # Конвертируем результат в bytes
    is_success, buffer = cv2.imencode(".png", segmentation_mask)
    io_buf = io.BytesIO(buffer)
    
    return send_file(
        io_buf,
        mimetype='image/png',
        as_attachment=True,
        download_name='segmentation.png'
    )

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000) 