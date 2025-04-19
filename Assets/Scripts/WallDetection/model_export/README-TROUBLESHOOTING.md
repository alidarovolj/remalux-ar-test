# Устранение ошибок при работе с ONNX и Barracuda на Apple Silicon (M1/M2/M3)

Данный документ содержит решения распространенных проблем при экспорте моделей DeepLabV3 для ARM64 (Apple Silicon) и их использовании в Unity с Barracuda.

## Проблемы компиляции в Unity

### Ошибка: Internal build system error. BuildProgram exited with code 134

Эта ошибка часто возникает из-за циклической зависимости между сборками:

```
Unhandled exception. System.Reflection.TargetInvocationException: Exception has been thrown by the target of an invocation.
 ---> System.ArgumentException: Action "CopyFiles Library/ScriptAssemblies/Unity.Barracuda.dll" outputs "Library/ScriptAssemblies/Unity.Barracuda.dll", but these file(s) were previously registered as inputs to "Csc Library/Bee/artifacts/200b0aE.dag/Remalux.Core.dll (+2 others)"
```

**Решение:**

В проекте созданы несколько редакторских скриптов для исправления этой проблемы:

1. Откройте Unity и выберите меню **Tools > Fix All Barracuda Issues**
2. После применения исправлений **перезапустите Unity**

Если ошибка сохраняется:

1. Закройте Unity
2. Удалите папку `Library` и `Temp` в корне проекта
3. Перезапустите Unity
4. После открытия проекта выполните **Tools > Fix All Barracuda Issues**

### Ошибка: "failed to find Unity.Barracuda.dll in assemblies"

**Решение:**

1. Проверьте статус импорта Barracuda: **Tools > Display Barracuda Import Status**
2. Если показаны проблемы, запустите **Tools > Fix Barracuda Dependency**
3. Перезапустите Unity

## Проблемы при экспорте моделей на ARM64

### Ошибка: unsupported option '-msse4.1' for target 'arm64-apple-darwin'

Эта ошибка возникает при попытке собрать ONNX из исходников на Apple Silicon:

**Решение:**

1. Используйте созданные скрипты для ARM64:
   ```bash
   ./install_arm64_deps.sh
   ./export_arm64.sh
   ```

2. При установке вручную всегда используйте флаг `--no-build-isolation`:
   ```bash
   pip install --no-build-isolation onnx onnxruntime onnx-simplifier
   ```

### Ошибка: "Submodule update failed"

При установке ONNX на ARM64 может возникать ошибка клонирования подмодулей.

**Решение:**

1. Установите Google Protocol Buffers отдельно:
   ```bash
   pip install protobuf>=3.20.0,<4.0.0
   ```

2. Затем установите ONNX:
   ```bash
   pip install --no-build-isolation onnx
   ```

## Проблемы с производительностью на Apple Silicon

### Медленный инференс моделей

**Решение:**

1. Убедитесь, что используется ускорение MPS:
   ```python
   if torch.backends.mps.is_available():
       device = torch.device("mps")
       model = model.to(device)
   ```

2. Используйте квантизацию моделей при экспорте:
   ```bash
   python export_arm64.py --model_type mobilenet --size 320 --quantize
   ```

3. Для ONNX Runtime используйте `CoreMLExecutionProvider` если он доступен:
   ```python
   providers = ['CPUExecutionProvider']
   if 'CoreMLExecutionProvider' in ort.get_available_providers():
       providers.insert(0, 'CoreMLExecutionProvider')
   ```

## Проблемы с Barracuda в Unity

### Ошибка: "The native library was not found at path <...>/barracuda.bundle"

**Решение:**

1. Проверьте, что пакет Barracuda установлен правильно:
   - Откройте Package Manager (Window > Package Manager)
   - Найдите "Barracuda" и убедитесь, что установлена ​​версия 3.0.0 или выше
   - Если нет, выберите "Install" или "Update"

2. Выполните **Tools > Fix Barracuda Dependency**

### Ошибка: MissingMethodException при использовании IWorker

**Решение:**

1. Убедитесь, что вы используете правильную версию API Barracuda:
   ```csharp
   // Для Barracuda 3.0+
   using Unity.Barracuda;
   using IWorker = Unity.Barracuda.IWorker;
   ```

2. Попробуйте использовать BarracudaLite вместо прямых ссылок:
   ```csharp
   // Вместо:
   using Unity.Barracuda;
   
   // Используйте:
   using Remalux;
   ```

## Прочие проблемы

### Ошибка при активации виртуального окружения (venv)

```
venv/bin/activate: line 3: $'\r': command not found
```

**Решение:**

Проблема возникает из-за Windows line endings (CRLF) в shell-скриптах:

```bash
# Преобразуйте CRLF в LF:
find . -name "*.sh" -exec dos2unix {} \;
```

### Проблемы с установкой PyTorch на Apple Silicon

**Решение:**

```bash
# Устанавливайте PyTorch с поддержкой MPS:
pip install torch torchvision
```

## Дополнительные ресурсы

- [Документация по Barracuda](https://docs.unity3d.com/Packages/com.unity.barracuda@3.0/manual/index.html)
- [ONNX на GitHub](https://github.com/onnx/onnx)
- [PyTorch для Apple Silicon](https://pytorch.org/get-started/locally/)
- [M1/M2 Mac fixes for Unity](https://forum.unity.com/threads/unity-on-apple-silicon-mac.1173377/) 