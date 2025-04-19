#!/bin/bash
# Скрипт для исправления циклической зависимости Barracuda в Unity на Apple Silicon

# Перейдем в корень проекта
PROJECT_PATH=$(pwd)
echo "Выполняется исправление в директории: $PROJECT_PATH"

# Удалим служебные папки Unity
echo "Удаление служебных папок Unity..."
rm -rf Library Temp Obj

# Создадим папку для плагина Barracuda
echo "Создание папки для плагина Barracuda..."
mkdir -p Assets/Plugins/BarracudaFix

# Создадим файл с принудительными исправлениями
echo "Создание файла с исправлениями..."
echo "USE_BARRACUDA_INDIRECTLY=1" > forcefixes.txt
echo "FIXED_BUILD_ORDER=1" >> forcefixes.txt
echo "UNITY_APPLE_SILICON=1" >> forcefixes.txt

# Создадим asmref файл для правильного порядка сборки
echo "Создание asmref файла..."
echo '{
    "reference": "Unity.Barracuda"
}' > Assets/barracuda-reference.asmref

# Создадим сценарий очистки ссылок в asmdef файлах
echo "Очистка ссылок в asmdef файлах..."
find Assets -name "*.asmdef" -exec sed -i '' 's/"Unity.Barracuda",//g' {} \;
find Assets -name "*.asmdef" -exec sed -i '' 's/,"Unity.Barracuda"//g' {} \;

# Проверим наличие пакета Barracuda и скопируем его DLL в Plugins
echo "Поиск и копирование Unity.Barracuda.dll..."
BARRACUDA_PATH=$(find ~/Library/Unity/cache -name "Unity.Barracuda.dll" | head -n 1)
if [ -n "$BARRACUDA_PATH" ]; then
    cp "$BARRACUDA_PATH" Assets/Plugins/BarracudaFix/
    echo "Скопирован файл $BARRACUDA_PATH в Assets/Plugins/BarracudaFix/"
else
    echo "Unity.Barracuda.dll не найден в кэше Unity"
    
    # Попробуем найти в другом месте
    BARRACUDA_PATH=$(find ~/Library/PackageCache -name "Unity.Barracuda.dll" | head -n 1)
    if [ -n "$BARRACUDA_PATH" ]; then
        cp "$BARRACUDA_PATH" Assets/Plugins/BarracudaFix/
        echo "Скопирован файл $BARRACUDA_PATH в Assets/Plugins/BarracudaFix/"
    else
        echo "ПРЕДУПРЕЖДЕНИЕ: Unity.Barracuda.dll не найден. Возможно, потребуется скопировать его вручную."
    fi
fi

# Создадим метафайл для DLL в Plugins
echo "Создание meta-файла для DLL..."
echo 'fileFormatVersion: 2
guid: a5d45a35f5e0b49b8b32f799c9f31c5f
PluginImporter:
  externalObjects: {}
  serializedVersion: 2
  iconMap: {}
  executionOrder: {}
  defineConstraints: []
  isPreloaded: 0
  isOverridable: 0
  isExplicitlyReferenced: 1
  validateReferences: 1
  platformData:
  - first:
      Any: 
    second:
      enabled: 1
      settings: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: ' > Assets/Plugins/BarracudaFix/Unity.Barracuda.dll.meta

echo ""
echo "======================="
echo "Все исправления применены! Теперь можно открыть Unity заново."
echo "После загрузки выполните Remalux → Fix All Project Issues"
echo "=======================" 