# JSON и XML через рефлексию в Native AOT (.NET 10)

Для моделей этого проекта рабочая отправная точка — `PublishAot`, явное сохранение
сборки DTO через `TrimmerRootAssembly`, сохранение необходимых закрытых типов
коллекций и явный `DefaultJsonTypeInfoResolver` для JSON. `TrimMode=partial` для
этой схемы не требуется. Атрибуты `RequiresDynamicCode` и
`RequiresUnreferencedCode` не включают поддержку сериализации.

Это рецепт для известного набора моделей, а не гарантия совместимости любых
reflection-сериализаторов с Native AOT. Для JSON штатный путь AOT — source
generation; reflection API имеют предупреждения о trimming и AOT.
См. [рекомендации разработчиков .NET](https://devblogs.microsoft.com/dotnet/creating-aot-compatible-libraries/).

Рядом находится [запускаемый пример](examples/ReflectionSerialization/ReflectionSerialization.csproj),
который использует библиотеку моделей проекта, но не использует BenchmarkDotNet
или JSON source generation. Ниже описаны настройки после правок и результаты
проверок от 20.09.2026.

## Что установлено в текущем проекте

| Файл | Фактическая настройка и её значение |
| --- | --- |
| `global.json` | SDK `10.0.100`, `rollForward=latestFeature`, без prerelease. Допускается установленный более новый feature band .NET 10; это не фиксация точного SDK. |
| `Benchmarks/BenchmarksAOT.csproj` | `net10.0`, `PublishAot=true`, ссылка на библиотеку DTO и `TrimmerRootAssembly Include="ClassLibraryForDeserialization"`. |
| Тот же файл | Явно заданы `TrimMode=full`, `JsonSerializerIsReflectionEnabledByDefault=false`, `TrimmerSingleWarn=false`. |
| `ClassLibrary1/ClassLibraryForDeserialization.csproj` | Сохранением DTO управляет root в приложении. |
| `JsonSerializationBench.Setup` | Явно создаёт `DefaultJsonTypeInfoResolver`; одновременно использует source-generated контекст и сравнивает строки JSON. |
| Оба `Setup` | Сохраняют три закрытых `List<T>` через `DynamicDependency(All, ...)`.|
| `XmlSerializationBench.Setup` | Создаёт `XmlSerializer(typeof(PnrList))`, выполняет XML round trip и проверяет количество записей. |
| `Benchmarks/Program.cs` | Последовательно запускает `JsonSerializationBench` и `XmlSerializationBench` с заданиями JIT и Native AOT base. |

При оценке `BenchmarksAOT.csproj` через MSBuild с установленным SDK 10.0.401
получены `PublishTrimmed=true` и `JsonSerializerIsReflectionEnabledByDefault=false`.
Первое значение получается из настроек SDK при `PublishAot=true`, второе теперь
явно записано в проекте.

## Установка и публикация

Для Windows установите .NET 10 SDK и Visual Studio 2022 или новее с workload
**Desktop development with C++**, MSVC и Windows SDK. Для `win-arm64` нужны
компоненты C++ для ARM64. Выбирайте RID под целевую платформу; бинарник проверяйте
на соответствующей архитектуре. Для Linux и macOS нужны их native toolchain;
инструкции приведены в [требованиях Native AOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/).

`System.Text.Json`, `System.Xml.Serialization` и атрибуты
`System.Diagnostics.CodeAnalysis` входят в .NET 10. Отдельные NuGet-пакеты для
них, ручная установка `Microsoft.DotNet.ILCompiler` и `AllowUnsafeBlocks` этому
примеру не нужны. Пакеты BenchmarkDotNet, Newtonsoft.Json не являются условиями reflection-сериализации.

Команды из корня репозитория (PowerShell):

```powershell
dotnet --info
dotnet publish docs/examples/ReflectionSerialization/ReflectionSerialization.csproj -c Release -r win-x64 -o docs/examples/ReflectionSerialization/bin/native
& ./docs/examples/ReflectionSerialization/bin/native/ReflectionSerialization.exe
```

Для ARM64 замените RID на `win-arm64` и каталог результата на отдельный,
например `bin/native-arm64`. Запустите результат на ARM64.
`dotnet build` и `dotnet run` сами по себе не проверяют native-бинарник:
необходимы `dotnet publish` и запуск полученного исполняемого файла.

## Свойства проекта: что необходимо, а что нет

Настройки размещаются в **публикуемом executable-проекте**. Пример для приложения,
уже ссылающегося на библиотеку моделей:

```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <TargetFramework>net10.0</TargetFramework>
  <PublishAot>true</PublishAot>
  <TrimMode>full</TrimMode>
  <JsonSerializerIsReflectionEnabledByDefault>false</JsonSerializerIsReflectionEnabledByDefault>
</PropertyGroup>
<ItemGroup>
  <TrimmerRootAssembly Include="ClassLibraryForDeserialization" />
</ItemGroup>
```

Здесь `false` выбран намеренно: ниже resolver передаётся явно. `TrimMode=full`
явно фиксирует режим проверочного примера; сохранением DTO управляет отдельный
root. Не копируйте путь `ProjectReference` из другого проекта без адаптации.

| Настройка | Назначение и необходимость |
| --- | --- |
| `PublishAot=true` | Включает native-компиляцию при публикации и AOT-анализ. Требуется для этой модели публикации. |
| `PublishTrimmed` | Уже включается при `PublishAot`; `false` не является способом отключить trimming в Native AOT. |
| `TrimmerRootAssembly` | **MSBuild item в `ItemGroup`**, не свойство. Сохраняет указанную сборку как корень. Указывается assembly name без `.dll`, не namespace. Это один из способов сохранения DTO, а не обязательное условие при эквивалентных точечных аннотациях. |
| `TrimMode=partial` | В публикуемом приложении включает более широкое сохранение сборок, не помеченных как trimmable. Не отключает AOT-анализ и не создаёт JIT. Здесь не нужен при явном сохранении DTO. |
| `IsTrimmable` | Объявляет библиотеку пригодной для trimming. `false` не заменяет явные корни при полном trimming. |
| `IsAotCompatible` | Объявление совместимости библиотеки и включение анализаторов; подразумевает `IsTrimmable=true`. Не исправляет reflection API. |
| `EnableTrimAnalyzer`, `EnableAotAnalyzer` | Помогают найти проблемы. В AOT-приложении необходимые анализаторы включаются через SDK. |
| `TrimmerSingleWarn=false` | Показывает подробные trimming-предупреждения зависимостей. При `true` (по умолчанию) предупреждения для NuGet-сборок сворачиваются до одного на сборку. На сохранение типов не влияет. |
| `NoWarn`, `SuppressTrimAnalysisWarnings` | Скрывают диагностику. Не обеспечивают работоспособность. Не используйте как основной рецепт. |
| `IlcGenerateCompleteTypeMetadata` | Низкоуровневая настройка полноты метаданных. Не заменяет сохранение нужных методов и generic-инстанцирований; не требуется в примере. |

Механизмы roots, descriptors и `partial` видны в
[targets Native AOT .NET 10](https://github.com/dotnet/runtime/blob/v10.0.0/src/coreclr/nativeaot/BuildIntegration/Microsoft.NETCore.Native.targets).
Назначение анализаторов описано в
[документации Native AOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/),
диагностических свойств — в
[trimming options](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trimming-options).

### Почему `partial` в библиотеке недостаточно

Обычное MSBuild-свойство в `.csproj` зависимости не становится свойством
родительского приложения. При публикации важны
настройки конечного приложения, его roots и аннотации типов.

`TrimmerRootAssembly` для DTO не сохраняет целиком `System.Private.CoreLib`:
`List<FlightSegment>`, `List<HotelStay>`, `List<CarRental>` объявлены именно там.
Поэтому зависимости на закрытые коллекции рассматриваются отдельно. Не добавляйте
в roots весь framework ради трёх коллекций.

## Атрибуты и сохранение графа моделей

| Атрибут | Что делает | Чего не делает |
| --- | --- | --- |
| `DynamicDependency` | Сохраняет указанные члены, если метод/член с атрибутом сам сохранён. | Не включает runtime code generation и не устраняет автоматически IL2026/IL3050. |
| `DynamicallyAccessedMembers` | Описывает требования к типам, передаваемым через `Type`, generic-параметры и другие допустимые позиции. | Не означает «сериализовать весь граф объектов». |
| `RequiresUnreferencedCode` | Обозначает небезопасный для trimming API; предупреждение переносится к вызывающему коду. | Не сохраняет DTO. |
| `RequiresDynamicCode` | Обозначает API, которому может потребоваться динамическое создание кода. | Не добавляет JIT в Native AOT. |
| `UnconditionalSuppressMessage` | Точечно подавляет диагностическое сообщение, включая анализ публикации. | Не исправляет причину; требует проверенного обоснования. |

Описание механизмов:
[подготовка библиотек к trimming](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/prepare-libraries-for-trimming)
и [предупреждения AOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/fixing-warnings).

В этом проекте DTO образуют граф:

```text
PnrList -> Pnr[] -> Pnr
  Pnr -> Traveler -> ContactInfo
  Pnr -> List<FlightSegment> -> FlightSegment -> Airport
  Pnr -> List<HotelStay> -> HotelStay
  Pnr -> List<CarRental> -> CarRental
```

Сохранение только `PnrList` не означает сохранения всех членов типов его свойств.
При этом `All` имеет важную особенность: сохраняет также все члены **вложенных
типов**. Все DTO здесь вложены в `TestModel`, поэтому альтернативный атрибут
`DynamicDependency(All, typeof(TestModel))` охватывает их. В текущих бенчмарках
он не нужен и удалён: используется root сборки. Перенос DTO из
`TestModel` в самостоятельные классы изменит результат. Вложенность классов
и связи через свойства — разные отношения.
См. [семантику All](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/fixing-warnings).

Вариант без сохранения всей сборки для **текущей структуры** моделей:

```csharp
using System.Diagnostics.CodeAnalysis;
using static TestModel;

internal static class SerializationRoots
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TestModel))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(List<FlightSegment>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(List<HotelStay>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(List<CarRental>))]
    internal static void Preserve() { }
}
```

Вызовите `SerializationRoots.Preserve()` из достижимого кода, например `Main`,
до сериализации. Не оставляйте атрибуты на никогда не вызываемом вспомогательном
методе. При сохранённой сборке DTO первый атрибут дублирует её root и не нужен.
Список коллекций — консервативный набор из бенчмарков, не доказанный минимальный
набор для каждого сериализатора. Не удаляйте аннотации только по результату
одного успешного запуска.

Для самостоятельных DTO вместо `TestModel` перечислите каждый тип графа.
Можно сузить `All` до требуемых конструкторов, свойств, полей и методов, но это
отдельная оптимизация с повторной проверкой. Одного `PublicProperties` может
не хватить для создания объектов и наполнения коллекций.

Альтернатива без атрибутов на DTO — linker descriptor:

```xml
<!-- roots.xml; путь задаётся относительно executable-проекта -->
<linker>
  <assembly fullname="ClassLibraryForDeserialization" preserve="all" />
</linker>
```

```xml
<ItemGroup>
  <TrimmerRootDescriptor Include="roots.xml" />
</ItemGroup>
```

Для этого случая он заменяет root сборки, но не отдельный анализ коллекций.
Формат `roots.xml` отличается от старого `rd.xml`. Не переносите
`Serialize="Required All"` из старого, теперь удалённого `rd.xml` в linker descriptor.

## JSON: явная рефлексия и значение feature switch

```csharp
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using static TestModel;

var options = new JsonSerializerOptions
{
    TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
    WriteIndented = true
};
var source = new PnrList { Pnrs = PnrFactory.CreateMany(3).ToArray() };
string json = JsonSerializer.Serialize(source, options);
PnrList result = JsonSerializer.Deserialize<PnrList>(json, options)
    ?? throw new InvalidOperationException("JSON returned null");
```

Это reflection-вызовы; код предполагает roots из предыдущих разделов.
`JsonSerializerIsReflectionEnabledByDefault=false` отключает **неявный resolver
по умолчанию**, но не запрещает явно переданный `DefaultJsonTypeInfoResolver`.
Поэтому в текущем бенчмарке `true` не требуется.

Если приложение вызывает `JsonSerializer.Serialize(value)` без resolver/контекста,
для такого default reflection-пути потребуется
`JsonSerializerIsReflectionEnabledByDefault=true`. Сам переключатель не сохраняет
модели и не делает этот путь безопасным для AOT. Предпочтительнее явный resolver:
его область использования видна в коде.
См. [reflection defaults в System.Text.Json](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation#disable-reflection-defaults).

`[JsonSerializable]` и `[JsonSourceGenerationOptions]` относятся к другому,
source-generated пути. Вызовы с `PnrListSourceGenerationContext.Default.PnrList`
не являются reflection-бенчмарками. Генератор в том же приложении добавляет
статические зависимости на модели, поэтому проверочный пример намеренно его
не использует. Текст `Requires*` влияет только на диагностику.

## XML: reflection fallback и его границы

```csharp
using System.Xml.Serialization;
using static TestModel;

var source = new PnrList { Pnrs = PnrFactory.CreateMany(3).ToArray() };
var serializer = new XmlSerializer(typeof(PnrList));
using var writer = new StringWriter();
serializer.Serialize(writer, source);
string xml = writer.ToString();
var result = (PnrList?)serializer.Deserialize(new StringReader(xml))
    ?? throw new InvalidOperationException("XML returned null");
```

В .NET 10 `XmlSerializer.Mode` выбирает `ReflectionOnly`, когда
`RuntimeFeature.IsDynamicCodeSupported` равен `false`. Специально включать
генерацию кода или скрытый AppContext switch не требуется.
Однако конструкторы и операции сериализации сохраняют аннотации `Requires*`:
fallback не является обещанием поддержки всех XML-контрактов.
Это подтверждается [исходным кодом XmlSerializer .NET 10](https://github.com/dotnet/runtime/blob/v10.0.0/src/libraries/System.Private.Xml/src/System/Xml/Serialization/XmlSerializer.cs).

Текущие DTO публичные, имеют публичные конструкторы без параметров и свойства
с getter/setter. При расширении контракта отдельно проверяйте коллекции,
наследников, `XmlInclude`, overrides и пользовательские типы.
`[XmlElement]`, `[XmlAttribute]`, `[XmlArray]`, `[XmlIgnore]` задают XML-контракт,
но не заменяют roots. Аналогично `[JsonInclude]`/`[JsonConstructor]` описывают
JSON-контракт, а не общую стратегию сохранения типов.

Не считайте `sgen` или `Microsoft.XmlSerializer.Generator` автоматической заменой
JSON source generation для Native AOT. Генерация отдельной serializer-сборки
не гарантирует её использование в native-приложении;
см. [обсуждение разработчиков runtime](https://github.com/dotnet/runtime/issues/106580).
Если reflection-путь для нужного контракта не работает, варианты — явный код
на `XmlReader`/`XmlWriter`, подходящий проверенный генератор или публикация с JIT.

## Проверка и диагностика

Проверочный пример сохраняет предупреждения IL2026 и IL3050 видимыми. Отсутствие
`Requires*` на его `Main` намеренно: добавление этих атрибутов не улучшило бы
работоспособность. В публичной библиотеке ими можно честно обозначить ограничения
API; в конечном приложении это не заменяет аудит предупреждений.

| Симптом | Что проверить |
| --- | --- |
| `Reflection-based serialization has been disabled` | Передан ли явный resolver/контекст; если нужен default reflection — значение JSON feature switch. |
| Пустой JSON/XML, потеря полей, значения по умолчанию | Сохранены ли члены каждого DTO и обе стороны round trip; проверить данные, а не только отсутствие исключения. |
| Нет конструктора/метода, ошибки reflection metadata | Roots, конструкторы, accessors и методы коллекций; проверить также `InnerException` XML-ошибки. |
| Ошибка об отсутствии native code / `MakeGenericType` / `MakeGenericMethod` | Все ли закрытые generic-типы доступны компилятору. Дополнительные метаданные не гарантируют наличие машинного кода. |
| IL2026 | API нельзя автоматически доказать безопасным для trimming; проверить сохранение моделей. |
| IL3050 | API потенциально требует динамический код; проверить доступный AOT-путь. Подавление сообщения не создаёт его. |

При добавлении DTO, value-type generic-аргументов, словарей, converters,
полиморфизма или типов, выбираемых из входных данных, заново публикуйте и
проверяйте весь используемый набор контрактов. Проверяйте также десериализацию
внешних JSON/XML-образцов: round trip собственных данных не покрывает все входы.

В текущем `Program.cs` включены оба класса сериализации. Запускайте обычный
host и позволяйте NativeAotToolchain публиковать отдельные приложения:

```powershell
dotnet run --project Benchmarks/BenchmarksAOT.csproj -c Release -f net10.0
```

Проверяйте сгенерированные проекты и аргументы ILC в артефактах BenchmarkDotNet:
roots конечного native-приложения должны реально присутствовать. Не предполагайте,
что произвольные MSBuild items исходного проекта автоматически перенесены в
проект, который публикует toolchain. Сейчас оба вызова `BenchmarkRunner.Run`
передают только `config`; для обработки аргументов командной строки добавьте
`args` вторым аргументом. Фильтр не выбирает классы, которые не переданы runner.

## Объём выполненной проверки

### Отдельный пример без JSON source generation

Проверено 20.09.2026 до последующих правок бенчмарков: Windows x64, SDK 10.0.401, ILCompiler 10.0.12,
`net10.0`, `Release`, RID `win-x64`. Публикация завершилась успешно с ожидаемыми
IL2026/IL3050. Запущен именно опубликованный `.exe`; код возврата — 0:

```text
Dynamic code supported: False
JSON reflection default: False
PASS: JSON and XML round trips, 3 cases, all DTO properties compared.
```

В файле аргументов ILC присутствует `--root:ClassLibraryForDeserialization`,
а `--defaultrooting` отсутствует. Это подтверждает, что пример не опирается
на `TrimMode=partial` библиотеки.

Пример покрывает текущий граф DTO, три входных случая (заполненный граф, пустой
корень и пустые коллекции/null с Unicode и XML-спецсимволами), JSON/XML в обе
стороны и сравнение всех объявленных свойств DTO. Это не доказательство
совместимости произвольных моделей или будущих версий runtime.

