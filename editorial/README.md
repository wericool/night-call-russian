# Как продолжить перевод Night Call

Рабочая база соответствует тексту выпуска `v2026.11.01`. Основа русского текста — локализация Artem Lytkin (4RH1T3CT0R). Условия использования и атрибуция приведены в [LICENSE](../LICENSE).

## Подготовка

Нужны Windows, Python 3.13 и чистая Steam-версия Night Call 1.3.7. В командах ниже замените путь на папку `Night Call_Data` вашей чистой копии. Извлечение читает ресурсы игры и сверяет их по SHA-256. Полученные оригиналы и DLL остаются локально в рабочей папке.

```powershell
cd editorial
py -3.13 -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r requirements.txt
.\.venv\Scripts\python.exe -X utf8 scripts/prepare_sources.py 'X:\путь к чистой игре\Night Call_Data'
```

## Редактура

- Начните с [правил проекта](AGENTS.md), [контекста](LOCALIZATION_CONTEXT_RU.md), [глоссария](GLOSSARY_RU.md) и очередей `localization/progress.json` и `localization/french_priority_audit.json`. Для известных расхождений FR и EN открывайте связанные сцены и записи в `localization/contexts/`.
- Полные редакции сцен лежат в [translation/reviewed](../translation/reviewed). Сохраняйте их через `save_scene` из `scripts/full_review.py` с явным `french_review`. Для заказов такси используйте `localization/passenger_calls.json`.
- Ключи интерфейса лежат в [translation/ui_ru.json](../translation/ui_ru.json). Контекст, резервная карта по исходным строкам и новые ключи улик находятся в `localization/contexts/`, `revision/data/Russian_UI/full_translation_mapping.json` и `localization/native_ui_extensions.json`.
- Редакторские записи Пассидекса и титров лежат в `localization/passidex_reviewed.json` и `localization/credits_reviewed.json`. После правки связанной сцены пересверьте её привязки в Пассидексе.

Выполняйте редакторский цикл из `editorial/`.

```powershell
$python = '.\.venv\Scripts\python.exe'
$steps = 'full_review','check_consistency','test_consistency_guards','corpus',
         'write_glossary','audit_french_priority','audit_ui_editorial_coverage',
         'write_context_progress','write_reports','export_reviewed_engine',
         'validate_editorial_input'
foreach ($step in $steps) {
    & $python -X utf8 "scripts/$step.py"
    if ($LASTEXITCODE -ne 0) { throw "Ошибка в $step" }
}
```

Обновлённые сценарии и экспорт диалогов появятся в `localization/native_scripts/` и `localization/reviewed_engine_input.json`. Корпус, отчёты и XLIFF появятся в `review/`. Файлы в [translation](../translation) вне редакторских записей сцен и таблицы интерфейса служат снимком выпуска. При подготовке нового выпуска сверяйте его с результатом актуального экспорта.

## Проверки движка

После небольших правок можно запустить ветки затронутой сцены через `--story-scope`. Варианты перечислены в `scripts/FullContextStoryChecks.cs`. Перед новой нативной сборкой или после существенных изменений логики выполните полный прогон без параметров. Каждый запуск заново компилирует C# через .NET Framework 4.x и записывает результат с SHA-256 ввода. Архив `runtime-fixtures.tar.gz` содержит эталонные ветки для этих проверок.

```powershell
& $python -X utf8 scripts/check_runtime.py --story-scope=--myriam-only
& $python -X utf8 scripts/check_runtime.py
& $python -X utf8 scripts/validate_editorial_input.py
```

Для двух файлов с одинаковой структурой можно отдельно вызвать `scripts/check_runtime.py --script ОРИГИНАЛ ПЕРЕВОД`. Если меняются команды или переходы, сверяйте точные ожидаемые изменения с FR и проверяйте соответствующие ветки. Этот набор доводит редактуру до проверенного ввода для штатного движка. Сборка игровых ресурсов и нового установочного архива выполняется отдельным циклом.
