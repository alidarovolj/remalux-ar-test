# Barracuda Fix для Apple Silicon

Это решение предназначено для исправления циклических зависимостей между Unity.Barracuda.dll и другими сборками проекта, особенно на Apple Silicon (M1/M2/M3).

## Проблема

При сборке проекта может возникать ошибка:

```
Internal build system error. BuildProgram exited with code 134.
System.ArgumentException: Action "CopyFiles Library/ScriptAssemblies/Unity.Barracuda.dll" outputs "Library/ScriptAssemblies/Unity.Barracuda.dll", but these file(s) were previously registered as inputs...
```

Это происходит из-за циклической зависимости: Unity пытается скопировать Unity.Barracuda.dll, который уже используется как входной файл для других сборок.

## Решение

Для решения этой проблемы мы используем несколько подходов:

1. **BarracudaWrapper** - класс-обертка, который использует рефлексию для работы с API Barracuda, устраняя прямые зависимости.
2. **Заглушка BarracudaStub** - минимальная заглушка API Barracuda для компиляции при отсутствии настоящей библиотеки.
3. **Инструменты Fix** - редакторские скрипты для исправления проблем.

## Как использовать BarracudaWrapper

Вместо прямых вызовов API Barracuda:

```csharp
// НЕ ДЕЛАЙТЕ ТАК
using Unity.Barracuda;

// Прямое использование API
var model = ModelLoader.Load(path);
using (var tensor = new Tensor(texture))
using (var worker = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharp, model))
{
    worker.Execute(tensor);
    var output = worker.PeekOutput();
    // ...
}
```

Используйте класс BarracudaWrapper:

```csharp
// ДЕЛАЙТЕ ТАК
using Remalux;

// Использование через рефлексию
var model = BarracudaWrapper.LoadModelFromFile(path);
var tensor = BarracudaWrapper.CreateTensorFromTexture(texture);
var worker = BarracudaWrapper.CreateWorker(model);

BarracudaWrapper.ExecuteModel(worker, tensor);
var output = BarracudaWrapper.PeekOutput(worker);

// Важно освободить ресурсы
BarracudaWrapper.DisposeTensor(tensor);
BarracudaWrapper.DisposeWorker(worker);
```

## Важные моменты

1. **Не добавляйте** прямые `using Unity.Barracuda` в классы проекта
2. **Всегда используйте** `BarracudaWrapper` вместо прямых вызовов API
3. **Освобождайте ресурсы** вызовами `DisposeTensor` и `DisposeWorker`
4. **Используйте символ условной компиляции** `USE_BARRACUDA_INDIRECTLY` для переключения между реализациями

## Полное исправление проекта

Для полного исправления циклических зависимостей выполните:

1. В верхнем меню выберите **Remalux → Fix All Project Issues**
2. Дождитесь завершения процесса
3. Перезапустите Unity 